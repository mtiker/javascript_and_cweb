# Phase 10 audit — modular monolithi viimine "tegelikuks"

Audit ulatus: lugesin läbi kõik moodulites olevad `AppDbContext`-i kasutajad, võrdlesin Module API katet vajadustega, kaardistasin cross-module andmevood. Koodi ei muudetud. Eesmärk: anda sulle aus pilt, kui suur Phase 10 tegelikult on ja millises järjekorras tasub seda võtta.

## TL;DR

- **32 mooduli-faili süstivad praegu jagatud `AppDbContext`-i.** Neist 12 on Infrastructure (repod + persistence-context), 20 on Application-kihi service'id ja handlerid.
- **Iga moodul loeb otse vähemalt ühe teise mooduli tabeleid.** Maintenance loeb 4 mooduli omasid, Users loeb 3, Gyms loeb 4.
- **Module API-d olemas 4/5 mooduli kohta** (puudu: `IMaintenanceModuleApi`), aga olemasolevad katavad ainult ~25% praegustest cross-module readidest. Suurem osa loogikast eeldab uusi API-meetodeid.
- **Mooduli DbContext-idel pole migratsioone.** Praegu kõik tabelid `public` skeemis, mooduli DbContext-id ootavad `users`/`gyms`/`memberships`/`training`/`maintenance` skeeme.
- **Test-fixture'id (~50+ kohta) süstivad repodesse `AppDbContext`-i otse** ([`WebApp.Tests/Unit/*`](../WebApp.Tests/Unit/)). Need vajavad kõik ümberseadistust.
- **Aus koguhinnang: 4–6 nädalat täiskoha tööd.** Per moodul ~3–5 päeva, sõltuvalt cross-module readide arvust.

**Soovitan järjekorda:** Maintenance → Users → Gyms → Memberships → Training. Põhjendus all p. 5.

---

## 1. Inventar — kes süstib `AppDbContext`-i

32 faili 5 mooduli kohta:

| Moodul | Infrastructure (repod) | Application (service'id, handlerid) | Kokku |
|---|---|---|---|
| Gyms | 1 | 4 | 5 |
| Maintenance | 2 | 0 | 2 |
| Memberships | 4 | 6 | 10 |
| Training | 4 | 3 | 7 |
| Users | 1 | 5 | 6 |
| **Kokku** | **12** | **18** | **32** (+ `GymResolutionMiddleware`) |

Täielik loetelu: `grep -rln AppDbContext Modules.*/` (audit-skripti väljund vastab raporti aluseks).

## 2. Omandiplaan (kes peab omama mida)

Mooduli DbContext-id juba defineerivad omanduse — see langeb õnneks suuresti loomuliku ärilise jaotuse järgi kokku:

| Entiteet | Omanik (mooduli DbContext) | Märkused |
|---|---|---|
| `AppUser`, `AppRole`, kõik Identity-tabelid | Users | `IdentityDbContext` baasis |
| `AppRefreshToken` | Users | |
| `DataProtectionKeys` | Users | |
| `Person` | **vaidlusalune** | Praegu kõigi 3 mooduli DbSet-is (Users, Memberships, Training) |
| `Gym`, `GymSettings`, `AppUserGymRole`, `GymContact` | Gyms | |
| `Member`, `MembershipPackage`, `Membership`, `Payment` | Memberships | |
| `Contact`, `PersonContact` | **kodutu** | Pole üheski mooduli DbContext-is |
| `Staff` | Training | |
| `TrainingCategory`, `TrainingSession`, `Booking` | Training | |
| `EquipmentModel`, `Equipment`, `MaintenanceTask` | Maintenance | |

**Lahtised disain-otsused enne tehnilist tööd:**

1. **Person omanik.** Loomulik valik on Users (sest `AppUser.PersonId` viitab sellele ja `UsersDbContext` on juba ainus, mis omab nii `AppUser` kui `Person`). Memberships ja Training peaks Person-ile viitama ainult `PersonId`-iga (Guid) ja kasutama `IUsersModuleApi`-d nime/personalCode'i kuvamiseks.
2. **Contact + PersonContact paigutus.** Kandidaadid: (a) Users (kuna PersonContact viitab Person-ile) või (b) Memberships (kuna ainus aktiivne kasutus on `MemberWorkspaceService.cs`). Soovitus: **Users**, et hoida kogu "kes on see inimene" loogika ühes moodulis.
3. **Cross-schema FK-d?** Postgres lubab cross-schema FK-d (nt `memberships.Members.PersonId → users.People.Id`), aga see seob skeemid jälle kokku. "Puhas" lahendus: cross-module ID-d ilma FK-ta, valideerimine application-kihis.

## 3. Per-moodul detail

### 3.1 Maintenance

**`AppDbContext` kasutajad:** `EfMaintenanceRepository`, `EfMaintenancePersistenceContext`.

**Own DbSets (vajavad ainult `MaintenanceDbContext`-i):**
- `EquipmentModels`, `Equipment`, `MaintenanceTasks`

**Cross-module readid:**

| Tabel / Include | Omanik | Kasutus repos | Kuidas asendada |
|---|---|---|---|
| `dbContext.GymSettings` | Gyms | [`EfMaintenanceRepository.cs:214-217`](../Modules.Maintenance/Infrastructure/EfMaintenanceRepository.cs#L214-L217) `FindGymSettingsAsync` | `IGymsModuleApi.GetSettingsAsync` — **olemas**, lihtne switch |
| `dbContext.AppUserGymRoles` | Gyms | [`EfMaintenanceRepository.cs:219-243`](../Modules.Maintenance/Infrastructure/EfMaintenanceRepository.cs#L219-L243) `ListGymUsersAsync`/`AddGymUserRoleAsync`/`RemoveGymUserRole` | Vaja **uut** `IGymsModuleApi.ListUserRolesAsync` + mutatsioonimeetodid `GrantRole/RevokeRole`. Kahtlane, et need üldse Maintenance-isse kuuluvad — Gyms-i admin-ala? |
| `dbContext.Users.Email` | Users | [`EfMaintenanceRepository.cs:245-251`](../Modules.Maintenance/Infrastructure/EfMaintenanceRepository.cs#L245-L251) `FindUserEmailAsync` | `IUsersModuleApi.GetUserSummaryAsync` — **olemas**, sisaldab `Email`-i |
| `Include(AssignedStaff).ThenInclude(Person)` | Training | [`EfMaintenanceRepository.cs:144-145, 158-159, 256-257`](../Modules.Maintenance/Infrastructure/EfMaintenanceRepository.cs#L144-L145) | `ITrainingModuleApi.GetStaffSummaryAsync` (StaffSummary peab sisaldama Person-i kuvanime) — **olemas**, kontrolli kas StaffSummary-s on vaja täiendada |
| `Include(AppUser)` (AppUserGymRole) | Users | [`EfMaintenanceRepository.cs:222`](../Modules.Maintenance/Infrastructure/EfMaintenanceRepository.cs#L222) | Loobutakse koos `AppUserGymRoles` reidiga (cf. ülemine rida) |

**Hinnatud effort:** 3–4 päeva.
- 0,5 päeva: cross-module readide ümberkirjutamine (5 meetodit eemaldatakse repo'st, asendatakse workflow service'is API-kutsetega).
- 0,5 päeva: 2 uut API-meetodit (Gyms-i user-role haldus) — kui need üldse Maintenance-isse jäävad.
- 0,5 päeva: `MaintenanceDbContext` Initial migration + skeem.
- 0,5 päeva: seemnedamine `MaintenanceDbContext`-ile.
- 1 päev: `WebApp.Tests/Unit/MaintenanceWorkflowServiceTests.cs` (~600 rida) ümberseadistus.
- 0,5 päeva: lokaalne Postgres verify + smoke.

**Põhirisk:** `ListGymUsersAsync` / `AddGymUserRoleAsync` / `RemoveGymUserRole` lõhnab nagu "vale moodul" — need haldavad Gyms-i resource'i. Tasub uurida, kas need on kontrolleritest kutsutavad päriselt Maintenance-i kaudu või on lihtsalt liisile pandud.

### 3.2 Users

**`AppDbContext` kasutajad:** `EfRefreshTokenRepository`, `AccountAuthService`, `CurrentActorResolver`, `IdentityService`, `MemberAccountService`, `UsersModuleApiService`.

`UsersModuleApiService` kasutab juba `UserManager`-it, mitte AppDbContext-i — see on heas seisus. Aga ülejäänud viis on AppDbContext-i peal.

**Own DbSets:** `Users` (Identity), `RefreshTokens`, `Person` (eeldades, et Users saab Person omanikuks).

**Cross-module readid:**

| Tabel | Omanik | Kus | Kuidas asendada |
|---|---|---|---|
| `dbContext.AppUserGymRoles` | Gyms | `AccountAuthService`, `CurrentActorResolver`, `IdentityService` | `IGymsModuleApi.ListGymsForUserAsync` — **olemas** (tagastab gymCode + roles) |
| `dbContext.Gyms` | Gyms | sama | Sisaldub `GymAccess` DTO-s |
| `dbContext.Members` | Memberships | `MemberAccountService` | `IMembershipsModuleApi.FindMemberForUserAsync` — **olemas** |
| `dbContext.Staff` | Training | `CurrentActorResolver` (võimalik) | Vaja **uut** `ITrainingModuleApi.FindStaffForUserAsync` |

**Hinnatud effort:** 3–4 päeva.
- 1 päev: 5 service'i ümberkirjutamine Module API peale.
- 0,5 päeva: 1 uus API-meetod (`FindStaffForUserAsync`).
- 0,5 päeva: `UsersDbContext` Initial migration. **NB:** see on `IdentityDbContext`, mistõttu migration sisaldab kõiki Identity-tabeleid (`AspNetUsers`, `AspNetRoles`, jne). Need tuleb `AppDbContext`-ist sünkroonselt eemaldada.
- 0,5 päeva: Identity DI ümberseadistus — `services.AddIdentity<AppUser, AppRole>().AddEntityFrameworkStores<AppDbContext>()` → `<UsersDbContext>`. Üks rida, aga seemnedamine ja deploy peab tühjas DB-s puhtalt startima.
- 0,5–1 päev: testide ümberseadistus (Identity testid).

**Põhirisk:** Identity tabelite skeemi-vahetus + andmemigratsioon (fresh-start otsus aitab siin).

### 3.3 Gyms

**`AppDbContext` kasutajad:** `EfAuthorizationQueryRepository`, `ResourceAuthorizationChecker`, `GymsModuleApiService`, `PlatformService`, `WorkspaceContextService`, `GymResolutionMiddleware`.

**Own DbSets:** `Gyms`, `GymSettings`, `AppUserGymRoles`, `GymContacts`.

**Cross-module readid:**

| Tabel | Omanik | Kus | Kuidas asendada |
|---|---|---|---|
| `dbContext.MaintenanceTasks` | Maintenance | `PlatformService` (dashboard?) | Vaja **uut** `IMaintenanceModuleApi.CountOpenTasksForGymAsync` (või sarnane) |
| `dbContext.Members` | Memberships | `PlatformService` | Vaja **uut** `IMembershipsModuleApi.CountActiveMembersForGymAsync` |
| `dbContext.TrainingSessions` | Training | `PlatformService` | Vaja **uut** `ITrainingModuleApi.CountUpcomingSessionsForGymAsync` |
| `dbContext.Users` | Users | võimalik | `IUsersModuleApi.GetUserSummaryAsync` — olemas |

**Hinnatud effort:** 4–5 päeva.
- 1 päev: 6 service'i ümberkirjutamine.
- 1 päev: **uus `IMaintenanceModuleApi`** (interface + DTO-d + implementatsioon + DI registreering Maintenance-is). See on uus liides nullist, mille mainsin et puudu on.
- 0,5 päeva: 2–3 uut counting-meetodit Memberships ja Training API-desse.
- 0,5 päeva: `GymsDbContext` Initial migration.
- 0,5 päeva: seemnedamine.
- 1 päev: testide ümberseadistus.

**Põhirisk:** `PlatformService` võib olla kõigi moodulite "armatuurlaud", mis koondab arvutusi. Module API-kutsete kuhjamine teeb dashboard'i päringud aeglasemaks (N round trips JOIN-i asemel). Tasub mõõta.

### 3.4 Memberships

**`AppDbContext` kasutajad:** 4 repot + `BookingConfirmedHandler`, `MemberWorkflowService`, `MemberWorkspaceService`, `MembershipPackageService`, `MembershipService`, `MembershipsModuleApiService`, `PaymentService`.

**Own DbSets:** `Members`, `MembershipPackages`, `Memberships`, `Payments` (+ `Person` kui omanduseks valitakse, + `Contact`/`PersonContact` kui mainitud disain-otsuse järgi siia tulevad).

**Cross-module readid:**

| Tabel | Omanik | Kus | Kuidas asendada |
|---|---|---|---|
| `dbContext.Bookings` | Training | `BookingConfirmedHandler`, `PaymentService` | `ITrainingModuleApi.GetBookingSummaryAsync` — **olemas** |
| `dbContext.People` | Users (uus omanik) | mitmel pool | `IUsersModuleApi.GetPersonSummaryAsync` — **vaja luua** |

**Hinnatud effort:** 4–5 päeva.
- See on suurim moodul (10 AppDbContext-kasutajat).
- 2 päeva: 10 faili ümberkirjutamine.
- 0,5 päeva: 1 uus API-meetod Users-is (Person summary).
- 0,5 päeva: `MembershipsDbContext` migration.
- 0,5 päeva: seemnedamine (sh Contact / PersonContact kui sinna kolib).
- 1–1,5 päeva: testide ümberseadistus (~3 suurt test-faili).

### 3.5 Training

**`AppDbContext` kasutajad:** 4 repot + `BookingPricingService`, `StaffWorkflowService`, `TrainingModuleApiService`.

**Own DbSets:** `Staff`, `TrainingCategories`, `TrainingSessions`, `Bookings`.

**Cross-module readid:**

| Tabel / Include | Omanik | Kus | Kuidas asendada |
|---|---|---|---|
| `dbContext.Memberships` | Memberships | `BookingPricingService` (hinnaarvutuse pakett) | `IMembershipsModuleApi.GetActiveMembershipForMemberAsync` — **vaja luua** |
| `Include(Member).ThenInclude(Person)` | Memberships + Users | `EfBookingRepository.cs:151-152` | `IMembershipsModuleApi.GetMemberSummaryAsync` + `IUsersModuleApi.GetPersonSummaryAsync`, paaride kaupa |
| `Include(TrainerStaff).ThenInclude(Person)` | Training (own) + Users | Mitmes kohas | Person-kuvand kuulub `IUsersModuleApi`-sse |

**Hinnatud effort:** 4–5 päeva.
- 1,5 päeva: 7 faili ümberkirjutamine, eriti `BookingPricingService` mis võib vajada disainimuudatust (hind sõltub member-i aktiivsest paketist → ühes päringus tehtud JOIN muutub kaheks round-tripiks).
- 0,5 päeva: 1–2 uut API-meetodit.
- 0,5 päeva: migration.
- 0,5 päeva: seemnedamine.
- 1–1,5 päeva: testid.

**Põhirisk:** `BookingPricingService` ja `Booking → Member` mustrid teevad praegu palju JOIN-e. Pärast Module API-le minekut tuleb iga päring "patchida" mitme round-tripiga. Kui broneeringuid on palju (loendid), tuleb teha **bulk-API** (`GetMembersByIdsAsync(IEnumerable<Guid>)`) muidu N+1.

---

## 4. Cross-cutting küsimused

### 4.1 Person omanduse otsus
Praegu `App.Domain/Entities/Person.cs` on üks klass, mida 3 mooduli DbContext-i mapivad. Kui omanikuks valid Users, siis Memberships/Training DbContext-id ei tohi enam `DbSet<Person>`-i sisaldada. Module API-d (`UserSummary` + uus `PersonSummary`) annavad teistele moodulitele lugemis-projektsiooni. **Disain-otsus tuleb teha enne tehnilist tööd.**

### 4.2 Migratsioonid
0 migratsiooni mooduli DbContext-ide kohta. Iga moodul vajab oma Initial migrationi:
```
dotnet ef migrations add Initial \
  --project Modules.X \
  --startup-project WebApp \
  --context XxxDbContext \
  --output-dir Infrastructure/Persistence/Migrations
```
Plus `AppDataInitExtensions.cs:23` tuleb laiendada, et see ka mooduli DbContext-id migreeriks. Iga uus moodul = 1 uus `MigrateAsync` kutse startupis.

### 4.3 Seemnedamine
`App.DAL.EF/Seeding/AppDataInit*.cs` praegu loob kõik entiteedid läbi `AppDbContext`-i. Fresh-start otsus tähendab, et need failid jagatakse moodulite kaupa (igale moodulile oma `*DataInit.cs` Infrastructure-is, mis võtab oma `*DbContext`-i). See on ~500–800 rida laiali jaotamist, mitte raske aga aeganõudev.

### 4.4 Testid
`WebApp.Tests/Unit/*WorkflowServiceTests.cs` failides (5 suuremat) kõik test-fixture'id ehitavad repod kujul `new EfXxxRepository(appDbContext)`. Need vajavad iga mooduli puhul:
- DbContext-i vahetamine `new XxxDbContext(...)`-iks.
- Module API-de jaoks mock'imine (kuna teste ei taha ehitada kõiki moodulite DbContext-e korraga).

`Architecture.Tests` jääb suuresti puutumata — see test moduli DbContext-ide registreerimist.

### 4.5 Include-strateegia
Suurim performance-risk on Include-laride asendamine API-kutsetega. Praegu 1 SQL query + JOIN annab kogu objekti. Pärast: 1 query own-tabelisse + N kutset Module API-le, mis on N päringut sihtmooduli DB-sse. Vajadusel:
- **Bulk API-d** (`GetMembersByIdsAsync(ids)`) one-roundtripiks.
- **Pagination + projection** workflow service'is.

### 4.6 Test-fikstuurides ühe DB jagamine
Praegu kõik testid jagavad `AppDbContext` InMemory-DB-d. Mitme DbContext-i puhul InMemory-baas on per-context, mistõttu cross-context andmed ei jaga state'i. See teeb integration-tüüpi testid keerukamaks — vajab mock'imist Module API-de tasemel.

---

## 5. Soovitatud järjekord ja koguhinnang

Järjekord on valitud nii, et **iga moodul, mille võtad, sõltub ainult moodulitest, mis on juba migreeritud või mille API-d sa just loonud**:

| # | Moodul | Põhjus, miks selles järjekorras | Effort |
|---|---|---|---|
| 1 | **Maintenance** | Vajab Gyms + Users + Training API-sid, mis on **olemas**. Cross-module readid on enamasti olemasolevate API-de katte all. Hea koht voo (migration + DI + testid + seemnedamine) sissetöötamiseks. | 3–4 päeva |
| 2 | **Users** | Vajab Gyms + Memberships + Training API-sid, kõik olemas (välja arvatud 1 uus). Identity wiring on oluline aga isoleeritud. | 3–4 päeva |
| 3 | **Gyms** | Vajab Maintenance API-d (mille piloodina lood — selles järjekorras saab juba luua selle uue API peale). Üks suurim ümberkirjutus (`PlatformService`). | 4–5 päeva |
| 4 | **Memberships** | Vajab Users API laiendust Person-iga + Training API olemasolu. | 4–5 päeva |
| 5 | **Training** | Suurim cross-module nõue (Member + Person ühes päringus). Tee viimasena, kui kõik teised API-d on stabilised. | 4–5 päeva |

**Koguhinnang: 18–23 päeva täiskoha tööd.** ~4–6 nädalat normaalse tempoga ühe inimese jaoks. CI auto-deploy main-ile tähendab, et iga moodul vajab oma feature-haru ja MR-i, et prod ei läheks vahepeal katki.

## 6. Otsused, mida sa pead tegema enne tehnilist tööd

1. **Person omanik** (vt p. 2.1). Soovitus: Users.
2. **Contact + PersonContact paigutus** (vt p. 2.2). Soovitus: Users.
3. **Cross-schema FK-d lubada või mitte?** Postgres tehniliselt lubab. "Puhas" modular monolith ütleks ei. (Soovitus: alguses lubada FK-d cross-schema, et migratsioon oleks väiksem; hiljem eemaldada, kui taluvus on käes.)
4. **AppDbContext täielik kustutamine?** Pärast 5 moodulit ei jää sellele midagi. Soovitus: jah, kustutame koos Phase 10 lõpetamisega. Sellega kaob ka `App.DAL.EF` projekt.
5. **`App.Domain` entiteedi-klassid jäävad jagatuks või kolivad moodulitesse?** Eksami-skoobi jaoks soovitan jätta jagatuks (POCO-d on lubatud "shared kernel"). Klasside relokatsioon moodulitesse on lisa 1–2 nädala töö, mis ei mõjuta runtime modulaarsust.
6. **Branching-strateegia.** Praegu auto-deploy main-ilt prodi. Soovitan iga moodulipass eraldi feature-haru + MR. Kui see ei lähe, siis vähemalt enne pushi prodi tühjendada eelmise mooduli moodulis tagasi-rollbackitavad migratsioonid.
7. **Test-strateegia.** Kas WebApp.Tests jääb kõik in-memory + Module API mock'idega, või tuleb teine "integration"-tase, mis ehitab kõik 5 mooduli DbContext-id korraga (real Postgres)?

---

## Lisa A: 32 AppDbContext-kasutajat (täislooler)

### Modules.Gyms (5)
- `Application/Authorization/ResourceAuthorizationChecker.cs`
- `Application/GymsModuleApiService.cs`
- `Application/Platform/PlatformService.cs`
- `Application/Platform/WorkspaceContextService.cs`
- `Infrastructure/EfAuthorizationQueryRepository.cs`
- `Infrastructure/GymResolutionMiddleware.cs`

### Modules.Maintenance (2)
- `Infrastructure/EfMaintenancePersistenceContext.cs`
- `Infrastructure/EfMaintenanceRepository.cs`

### Modules.Memberships (10)
- `Application/Mediator/BookingConfirmedHandler.cs`
- `Application/MemberWorkflowService.cs`
- `Application/MemberWorkspaceService.cs`
- `Application/MembershipPackageService.cs`
- `Application/MembershipService.cs`
- `Application/MembershipsModuleApiService.cs`
- `Application/PaymentService.cs`
- `Infrastructure/EfMemberRepository.cs`
- `Infrastructure/EfMembershipPackageRepository.cs`
- `Infrastructure/EfMembershipRepository.cs`
- `Infrastructure/EfPaymentRepository.cs`

### Modules.Training (7)
- `Application/BookingPricingService.cs`
- `Application/StaffWorkflowService.cs`
- `Application/TrainingModuleApiService.cs`
- `Infrastructure/EfBookingRepository.cs`
- `Infrastructure/EfTrainingCategoryRepository.cs`
- `Infrastructure/EfTrainingPersistenceContext.cs`
- `Infrastructure/EfTrainingSessionRepository.cs`

### Modules.Users (6)
- `Application/AccountAuthService.cs`
- `Application/CurrentActorResolver.cs`
- `Application/IdentityService.cs`
- `Application/MemberAccountService.cs`
- `Application/UsersModuleApiService.cs` (kasutab juba `UserManager`-it, kuid süstib siiski `AppDbContext`-i — auditi tasemel kontrollida, kas see kasutus on tegelikult vajalik)
- `Infrastructure/EfRefreshTokenRepository.cs`

## Lisa B: Olemasolevad Module API-d

- `IGymsModuleApi`: `ResolveAccessAsync`, `ListGymsForUserAsync`, `GetSettingsAsync`
- `IMembershipsModuleApi`: `GetMemberSummaryAsync`, `FindMemberForUserAsync`
- `ITrainingModuleApi`: `GetStaffSummaryAsync`, `GetTrainingSessionSummaryAsync`, `GetBookingSummaryAsync`, `ListBookingIdsForMemberAsync`
- `IUsersModuleApi`: `GetUserSummaryAsync`, `FindUserByEmailAsync`
- **`IMaintenanceModuleApi`: puudub** — loomine on osa Gyms-i Phase 10 passist.
