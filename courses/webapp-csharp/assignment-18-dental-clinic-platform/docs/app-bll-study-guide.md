# App.BLL Study Guide

## Mis on BLL?

`BLL` ehk `Business Logic Layer` on kiht, kus elavad rakenduse arireeglid ja use-case'id.

Selles projektis jaguneb tootus nii:

- `App.Domain` kirjeldab domeeniobjekte, enum'e ja rolle.
- `App.DAL.EF` annab `AppDbContext`-i ja andmebaasi ligipaasu.
- `App.BLL` otsustab, mida tohib teha, mida peab valideerima ja mis tulemus tagasi anda.
- `WebApp` tegeleb HTTP request/response, authi ja route'idega.

Hea vaimne mudel:

- controller loeb requesti
- controller teeb requestist BLL command'i
- teenus teeb ariloogika
- teenus tagastab BLL result'i voi viskab BLL exception'i
- controller voi middleware muudab selle HTTP vastuseks

## Kuidas App.BLL selles projektis tootab?

`App.BLL` kasutab `App.Domain` ja `App.DAL.EF` projekte. Ta ei soltu `WebApp`-ist.

Peamised seosed:

- `WebApp/Setup/ServiceExtensions.cs` registreerib BLL teenused DI konteinerisse.
- `WebApp/Middleware/GlobalExceptionMiddleware.cs` muudab BLL exception'id HTTP vastusteks.
- `WebApp/Middleware/TenantResolutionMiddleware.cs` ja `WebApp/Helpers/RequestTenantProvider.cs` seavad aktiivse tenanti.
- `AppDbContext` query filter piirab tenant-entiteetide paringud aktiivse company peale.

Oluline aus markus:

- suurem osa keerukamatest use-case'idest on teenustes
- finance- ja treatment-plan CRUD vood on samuti tostetud teenustesse
- osa lihtsamaid CRUD controllereid kasutab siiski praegu `AppDbContext`-i otse
- see tahendab, et arhitektuur on praktiline hybrid, mitte 100% `thin controller` lahendus

## Kaustad

- `Contracts`: teenuste sisend- ja valjundmudelid
- `Exceptions`: teenusekihi veatyybid
- `Services`: interface'id, teenuste klassid ja staatilised abiklassid

---

## 1. Projekti fail

### `App.BLL.csproj`

Mille jaoks:

- deklareerib `App.BLL` eraldi .NET projektina
- seab `net10.0`, `ImplicitUsings`, `Nullable`
- lisab project reference'id `App.Domain` ja `App.DAL.EF` suunas

Mida see sisuliselt tahendab:

- BLL saab kasutada entiteete ja enum'e
- BLL saab kasutada `AppDbContext`-i
- BLL ei tea midagi controlleritest ega DTO-dest

Kus kasutatakse:

- `WebApp/WebApp.csproj` referentseerib seda projekti

---

## 2. Exceptions

### `Exceptions/ValidationAppException.cs`

Mille jaoks:

- markida arireegli rikkumine

Mida teeb:

- lihtne `Exception` alamklass message'iga

Kus kasutatakse:

- `AppointmentService`, `CompanyOnboardingService`, `CompanySettingsService`, `CompanyUserService`, `ImpersonationService`, `PatientService`, `SubscriptionPolicyService`, `TreatmentPlanService`

Kuidas flow tootab:

- teenus viskab `ValidationAppException`
- `GlobalExceptionMiddleware` tagastab HTTP `400 Bad Request`

### `Exceptions/NotFoundException.cs`

Mille jaoks:

- markida, et vajalik ressurss puudub

Mida teeb:

- lihtne `Exception` alamklass

Kus kasutatakse:

- appointment, patient, treatment plan ja impersonation voogudes

Kuidas flow tootab:

- middleware muudab selle HTTP `404 Not Found` vastuseks

### `Exceptions/ForbiddenException.cs`

Mille jaoks:

- markida ligipaasu voi tegevuse keelu

Mida teeb:

- lihtne `Exception` alamklass

Kus kasutatakse:

- tenant rollikontrollis, owner-only voogudes ja impersonation turvakontrollis

Kuidas flow tootab:

- middleware muudab selle HTTP `403 Forbidden` vastuseks

---

## 3. Contracts

`Contracts` on BLL-i "leping". Controllerid ei anna teenustele otse DTO-sid ja teenused ei tagasta controlleritele EF entiteete.

### Appointments

#### `Contracts/Appointments/CreateAppointmentCommand.cs`

Mille jaoks:

- kirjeldab appointmenti loomise sisendit

Valjad:

- patient, dentist, treatment room, algus, lopp, notes

Kus kasutatakse:

- luuakse `AppointmentsController.Create` sees
- loeb `AppointmentService.CreateAsync`

#### `Contracts/Appointments/AppointmentResult.cs`

Mille jaoks:

- kirjeldab appointmenti teenuse valjundit

Kus kasutatakse:

- tagastavad `AppointmentService.ListAsync` ja `CreateAsync`
- controller mapib selle `AppointmentResponse` DTO-ks

#### `Contracts/Appointments/RecordAppointmentClinicalCommand.cs`

Mille jaoks:

- kirjeldab appointmenti kliinilise too salvestamise sisendit

Selles failis on kaks record'it:

- `RecordAppointmentClinicalCommand`: yldine operation
- `RecordAppointmentClinicalItemCommand`: yks protseduuri/hamba rida

Kus kasutatakse:

- controller ehitab requestist command'i
- `AppointmentService.RecordClinicalWorkAsync` tootab selle peal

#### `Contracts/Appointments/AppointmentClinicalRecordResult.cs`

Mille jaoks:

- kirjeldab kliinilise salvestuse kokkuvotet

Valjad:

- `AppointmentId`, `Status`, `RecordedItemCount`

Kus kasutatakse:

- `AppointmentService.RecordClinicalWorkAsync`
- `AppointmentsController.RecordClinicalWork`

### CompanySettings

#### `Contracts/CompanySettings/UpdateCompanySettingsCommand.cs`

Mille jaoks:

- company seadete uuendamise sisend

Kus kasutatakse:

- `CompanySettingsController.Update`
- `CompanySettingsService.UpdateAsync`

#### `Contracts/CompanySettings/CompanySettingsResult.cs`

Mille jaoks:

- company seadete teenuse valjund

Kus kasutatakse:

- `CompanySettingsService.GetAsync`
- `CompanySettingsService.UpdateAsync`
- `CompanySettingsController`

### CompanyUsers

#### `Contracts/CompanyUsers/UpsertCompanyUserCommand.cs`

Mille jaoks:

- tenant kasutaja lisamise voi uuendamise sisend

Valjad:

- email, role name, isActive, temporary password

Kus kasutatakse:

- `CompanyUsersController.Upsert`
- `CompanyUserService.UpsertAsync`

#### `Contracts/CompanyUsers/CompanyUserResult.cs`

Mille jaoks:

- tenant kasutaja valjundmudel

Kus kasutatakse:

- `CompanyUserService.ListAsync`
- `CompanyUserService.UpsertAsync`

### Impersonation

#### `Contracts/Impersonation/StartImpersonationCommand.cs`

Mille jaoks:

- impersonation algatamise sisend

Valjad:

- target user email, company slug, reason

Kus kasutatakse:

- `ImpersonationController.Start`
- `ImpersonationService.StartAsync`

#### `Contracts/Impersonation/StartImpersonationResult.cs`

Mille jaoks:

- tagastada impersonationi jaoks vajalik kontekst

Valjad:

- target user, active company, active company role, actor user, reason

Kus kasutatakse:

- `ImpersonationService.StartAsync`
- `ImpersonationController`, mis teeb selle pealt JWT, refresh tokeni ja audit logi

### Patients

#### `Contracts/Patients/CreatePatientCommand.cs`

Mille jaoks:

- patsiendi loomise sisend

Kus kasutatakse:

- `PatientsController.Create`
- `PatientService.CreateAsync`

#### `Contracts/Patients/UpdatePatientCommand.cs`

Mille jaoks:

- patsiendi uuendamise sisend

Erinevus create command'iga:

- sisaldab ka `PatientId`

Kus kasutatakse:

- `PatientsController.Update`
- `PatientService.UpdateAsync`

#### `Contracts/Patients/PatientResult.cs`

Mille jaoks:

- patsiendi lihtne valjund list/get/create/update jaoks

Kus kasutatakse:

- `PatientService.ListAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`

#### `Contracts/Patients/PatientToothHistoryItemResult.cs`

Mille jaoks:

- yhe hamba raviajaloo yhe rea valjund

Kus kasutatakse:

- `PatientService.GetProfileAsync` koostab selle treatment'ite pealt

#### `Contracts/Patients/PatientToothResult.cs`

Mille jaoks:

- yhe hamba hetkeseis + ajalugu

Kus kasutatakse:

- `PatientProfileResult` sees
- `PatientsController.GetProfile`

#### `Contracts/Patients/PatientProfileResult.cs`

Mille jaoks:

- patsiendi detailne profiil koos hambakaardiga

Kus kasutatakse:

- `PatientService.GetProfileAsync`

### Root contracts

#### `Contracts/RegisterCompanyCommand.cs`

Mille jaoks:

- onboarding sisend uue company registreerimiseks

Kus kasutatakse:

- `OnboardingController.RegisterCompany`
- `CompanyOnboardingService.RegisterCompanyAsync`

#### `Contracts/RegisterCompanyResult.cs`

Mille jaoks:

- onboarding teenuse tulemus

Kus kasutatakse:

- `CompanyOnboardingService`
- `OnboardingController`

#### `Contracts/RecordPlanItemDecisionCommand.cs`

Mille jaoks:

- yhe raviplaani rea otsuse sisend

Kus kasutatakse:

- `TreatmentPlanService.RecordPlanItemDecisionAsync`

#### `Contracts/PlanDecisionResult.cs`

Mille jaoks:

- raviplaani rea otsuse salvestamise tulemus

Kus kasutatakse:

- `TreatmentPlanService.RecordPlanItemDecisionAsync`

#### `Contracts/SubmitTreatmentPlanResult.cs`

Mille jaoks:

- raviplaani submit'imise tulemus

Kus kasutatakse:

- `TreatmentPlanService.SubmitAsync`

---

## 4. Services

Siin on BLL-i koige olulisem osa. Interface annab lepingu, klass teeb tegeliku too.

### Appointment teenus

#### `Services/IAppointmentService.cs`

Mille jaoks:

- kirjeldab appointment use-case lepingu

Meetodid:

- `ListAsync`
- `CreateAsync`
- `RecordClinicalWorkAsync`

Kus kasutatakse:

- `AppointmentsController`
- DI sidumine toimub `ServiceExtensions.AddAppServices`

#### `Services/AppointmentService.cs`

Mille jaoks:

- appointmentide ajastamine ja kliinilise too salvestamine

Constructori soltuvused:

- `AppDbContext`
- `ITenantAccessService`

##### `ListAsync`

Toimimine:

1. kontrollib ligipaasu
2. laeb tenant appointmentid
3. sorteerib algusaja jargi
4. mapib `AppointmentResult`-iks

##### `CreateAsync`

Toimimine:

1. kontrollib ligipaasu
2. valideerib ajad
3. kontrollib, et patient, dentist ja room eksisteerivad aktiivses tenantis
4. kontrollib arsti aja kattuvust
5. kontrollib ruumi aja kattuvust
6. loob `Appointment` entiteedi staatusega `Scheduled`
7. salvestab
8. tagastab tulemuse

Peamine arireegel:

- sama arst ega sama ruum ei tohi olla samal ajal mitmes aktiivses appointmentis

##### `RecordClinicalWorkAsync`

See on faili koige olulisem meetod.

Toimimine:

1. kontrollib ligipaasu
2. valideerib sisendi
3. laeb appointmenti
4. keelab tÃ¼histatud appointmenti kasutamise
5. laeb vajalikud treatment type'id
6. laeb optional plan item'id koos patsiendi infoga
7. laeb vajalike hammaste olemasolevad `ToothRecord` kirjed
8. iga itemi puhul kontrollib:
   - kas plan item kuulub samale patsiendile
   - kas treatment type sobib plan item'iga
9. uuendab voi loob `ToothRecord` kirje
10. lisab uue `Treatment` rea
11. vajadusel markeerib appointmenti `Completed`
12. salvestab
13. tagastab kokkuvotte

Miks see meetod on hea BLL naide:

- seob kokku appointmenti, raviplaani, treatment type'i, hammaste seisu ja tehtud protseduurid

##### Abimeetodid

- `EnsureAccessAsync`: owner/admin/manager/employee rollid
- `Validate(CreateAppointmentCommand)`: algus enne loppu, minevikupiir
- `Validate(RecordAppointmentClinicalCommand)`: vÃ¤hemalt 1 rida, kehtiv hambanumber, hind >= 0
- `ToResult`: entiteedi maping result'iks
- `GetOrCreateToothRecord`: leiab olemasoleva voi loob puuduva hambakirje
- `NormalizeOptional`: trim + tyhi -> `null`

### Company onboarding

#### `Services/ICompanyOnboardingService.cs`

Mille jaoks:

- kirjeldab uue company loomise use-case lepingu

Meetod:

- `RegisterCompanyAsync`

#### `Services/CompanyOnboardingService.cs`

Mille jaoks:

- luua uus tenant company koos owner useri, settingsi, subscriptioni ja owner rolliga

Constructori soltuvused:

- `AppDbContext`
- `UserManager<AppUser>`
- `RoleManager<AppRole>`

##### `RegisterCompanyAsync`

Toimimine:

1. normaliseerib slug'i
2. kontrollib slugi olemasolu ja unikaalsust
3. loob `Company`
4. loob `CompanySettings`
5. loob tasuta `Subscription`
6. leiab voi loob owner kasutaja
7. tagab, et rollid eksisteerivad
8. kontrollib owner role linki
9. lisab vajalikud entiteedid
10. salvestab
11. tagastab `RegisterCompanyResult`

Miks siin kasutatakse `IgnoreQueryFilters()`:

- onboarding toimub enne aktiivse tenanti olemasolu

##### `EnsureRolesExistAsync`

Toimimine:

- kaib labi `RoleNames.All`
- kontrollib `RoleManager` kaudu rolli olemasolu
- loob puuduva rolli

### Company settings

#### `Services/ICompanySettingsService.cs`

Mille jaoks:

- kirjeldab company seadete lugemise ja uuendamise lepingu

Meetodid:

- `GetAsync`
- `UpdateAsync`

#### `Services/CompanySettingsService.cs`

Mille jaoks:

- haldada aktiivse tenanti seadeid

Constructori soltuvused:

- `AppDbContext`
- `ITenantAccessService`
- `ITenantProvider`

##### `GetAsync`

Toimimine:

1. kontrollib owner rolli
2. votab aktiivse company id
3. laeb `CompanySettings`
4. kui kirjet pole, loob selle
5. tagastab `CompanySettingsResult`

##### `UpdateAsync`

Toimimine:

1. kontrollib owner rolli
2. votab aktiivse company id
3. valideerib command'i
4. laeb voi loob settings kirje
5. kirjutab normalized valjad
6. salvestab
7. tagastab tulemuse

##### Abimeetodid

- `EnsureOwnerAccessAsync`: lubab ainult `CompanyOwner`
- `RequireCompanyId`: kui tenant context puudub, viskab `ForbiddenException`
- `Validate`: kontrollib country/currency/timezone/X-ray intervalli reegleid
- `ToResult`: maping

### Company users

#### `Services/ICompanyUserService.cs`

Mille jaoks:

- kirjeldab tenant kasutajate halduse lepingu

Meetodid:

- `ListAsync`
- `UpsertAsync`

#### `Services/CompanyUserService.cs`

Mille jaoks:

- haldada tenant membership'e, rolle ja seotud subscription limiite

Constructori soltuvused:

- `AppDbContext`
- `ITenantAccessService`
- `ITenantProvider`
- `UserManager<AppUser>`
- `ISubscriptionPolicyService`

##### `ListAsync`

Toimimine:

1. kontrollib management access'i
2. laeb `AppUserRoles` koos `AppUser` navigeerimisega
3. mapib `CompanyUserResult`-iteks
4. sorteerib emaili ja rolli jargi

##### `UpsertAsync`

Toimimine:

1. kontrollib management access'i
2. normaliseerib emaili
3. kontrollib rolli lubatavust
4. kontrollib, kas tegija on owner
5. keelab adminil owner/admin rolle jagada
6. leiab voi loob target kasutaja
7. votab aktiivse company id
8. leiab olemasoleva role linki
9. kui aktiveeritakse uus aktiivne membership, kontrollib subscription user limiiti
10. kui ownerit deaktiveeritakse, kontrollib et viimane owner ei kaoks
11. loob voi uuendab `AppUserRole` kirje
12. salvestab
13. tagastab `CompanyUserResult`

##### Abimeetodid

- `EnsureNotRemovingLastOwnerAsync`: tenantile peab jaama vÃ¤hemalt 1 aktiivne owner
- `EnsureManagementAccessAsync`: owner voi admin
- `IsOwnerAsync`: peenem kontroll owner eriolukordadeks
- `RequireCompanyId`: tenant context peab olemas olema
- `IsCompanyRole`: whitelistib ainult tenant rollid

### Finance utiliit

#### `Services/FinanceMath.cs`

Mille jaoks:

- hoida rahalised arvutusreeglid yhes kohas

Kus kasutatakse:

- `CostEstimatesController`
- `InvoicesController`
- `PaymentPlansController`

See ei ole DI-teenus, vaid staatiline utiliit.

##### `CalculateEstimate`

Toimimine:

1. ymardab kogusumma
2. kui policy puudub, jatab kogu summa patsiendi kanda
3. arvestab deductible'i
4. rakendab coverage protsendi
5. piirab annual maximum'iga
6. arvutab patsiendi osa
7. tagastab `EstimateBreakdown`

##### `NormalizeInvoiceLine`

- arvutab line total'i
- piirab coverage amount'i rea kogusummaga
- arvutab patient amount'i

##### `ApplyInvoiceState`

- arvutab invoice total'i, patsiendi vastutuse, makstud summa ja balance'i
- kui invoice on cancelled, ei muuda staatust
- muidu seab `Paid`, `Overdue` voi `Issued`

##### `ApplyPaymentPlanState`

- jagab makstud raha installmentide peale
- markeerib installmentid `Paid`, `Overdue` voi `Scheduled`
- arvutab payment plan staatuse `Completed`, `Defaulted` voi `Active`

##### `RoundAmount`

- ymardab 2 komakohani, `AwayFromZero`

##### `AmountsMatch`

- lubab kuni 0.01 ymardusvahe

##### `EstimateBreakdown`

- vaikene result-struct total/coverage/patient amount jaoks

### Impersonation

#### `Services/IImpersonationService.cs`

Mille jaoks:

- kirjeldab impersonation use-case lepingu

Meetod:

- `StartAsync`

#### `Services/ImpersonationService.cs`

Mille jaoks:

- kontrollida, kas `SystemAdmin` tohib alustada impersonationi ja mis company kontekst aktiivseks saab

Constructori soltuvused:

- `AppDbContext`
- `UserManager<AppUser>`

##### `StartAsync`

Toimimine:

1. kontrollib actor user id olemasolu
2. kontrollib reason pikkust
3. laeb actor useri
4. kontrollib, et actor oleks `SystemAdmin`
5. laeb target kasutaja
6. laeb company slugi jargi company
7. kontrollib, et company oleks aktiivne
8. laeb target kasutaja aktiivsed tenant rollid selles companys
9. kui membership puudub, viskab vea
10. leiab peamise rolli `ResolveRolePriority` abil
11. tagastab `StartImpersonationResult`

##### `ResolveRolePriority`

- valib rollide seast prioriteetseima:
  `CompanyOwner`, `CompanyAdmin`, `CompanyManager`, `CompanyEmployee`

Miks teenus ei loo JWT-d:

- JWT, refresh token ja audit log on WebApp/auth infrastruktuur
- teenus annab vaid valideeritud konteksti

### Patients

#### `Services/IPatientService.cs`

Mille jaoks:

- kirjeldab patsiendi use-case lepingu

Meetodid:

- `ListAsync`
- `GetAsync`
- `GetProfileAsync`
- `CreateAsync`
- `UpdateAsync`
- `DeleteAsync`

#### `Services/PatientService.cs`

Mille jaoks:

- hallata patsiente ja nende hambaprofiili tenant kontekstis

Constructori soltuvused:

- `AppDbContext`
- `ITenantAccessService`
- `ISubscriptionPolicyService`

##### `ListAsync`

- kontrollib ligipaasu
- laeb patsiendid
- sorteerib nime jargi
- mapib `PatientResult`-iks

##### `GetAsync`

- kontrollib ligipaasu
- laeb patsiendi
- puudumisel viskab `NotFoundException`

##### `GetProfileAsync`

See on faili koige sisukam lugemismeetod.

Toimimine:

1. kontrollib ligipaasu
2. laeb patsiendi
3. tagab, et kogu hambakaart on olemas
4. laeb `ToothRecords`
5. laeb `Treatments` koos `TreatmentType` navigeerimisega
6. ehitab dictionary'd hamba numbri jargi
7. grupib raviajaloo hamba kaupa
8. kaib labi kogu pÃ¼sihammaste kaardi
9. loob iga hamba jaoks `PatientToothResult`
10. tagastab `PatientProfileResult`

##### `CreateAsync`

Toimimine:

1. kontrollib ligipaasu
2. valideerib nimed
3. normaliseerib isikukoodi
4. kontrollib isikukoodi saadavust
5. kontrollib subscription patient limiiti
6. loob `Patient`
7. loob kohe vaikimisi `ToothRecord` kirjed koigile pÃ¼sihammastele
8. salvestab
9. tagastab `PatientResult`

##### `UpdateAsync`

Toimimine:

1. kontrollib ligipaasu
2. valideerib nimed
3. kontrollib isikukoodi unikaalsust
4. laeb patsiendi
5. uuendab valjad
6. salvestab
7. tagastab tulemuse

##### `DeleteAsync`

Toimimine:

1. kontrollib ligipaasu
2. laeb patsiendi
3. kutsub `SoftDeletePatientRelationsAsync`
4. eemaldab patsiendi
5. salvestab

Oluline detail:

- meetodi nimi viitab soft delete'ile, kuid ta kasutab seotud kogumitel `RemoveRange`
- seega tasub koodi lugedes alati kontrollida ka entiteetide ja `DbContext`-i konfiguratsiooni

##### Abimeetodid

- `EnsureAccessAsync`: owner/admin/manager/employee
- `Validate`: eesnimi ja perenimi peavad olemas olema
- `EnsurePersonalCodeIsAvailableAsync`: kontrollib aktiivset konflikti
- `ToResult`: maping
- `BuildDefaultToothRecords`: loob vaikimisi tervisliku hambakaardi
- `EnsureFullToothChartAsync`: lisab puuduolevad hambakirjed
- `SoftDeletePatientRelationsAsync`: eemaldab patsiendiga seotud read mitmest tabelist
- `NormalizeOptional`: trim + tyhi -> `null`

### Subscription policy

#### `Services/ISubscriptionPolicyService.cs`

Mille jaoks:

- kirjeldab subscription tieri piirangute kontrolli

Meetodid:

- `EnsureCanCreatePatientAsync`
- `EnsureCanAddActiveMembershipAsync`
- `EnsureTierAtLeastAsync`

#### `Services/SubscriptionPolicyService.cs`

Mille jaoks:

- kontrollida tenant paketiga seotud limiite

Constructori soltuvused:

- `AppDbContext`
- `ITenantProvider`

##### `EnsureCanCreatePatientAsync`

- laeb efektiivse policy
- kontrollib patientide arvu entity limiidi vastu

Kus kasutatakse:

- `PatientService.CreateAsync`

##### `EnsureCanAddActiveMembershipAsync`

- laeb policy
- kontrollib aktiivsete kasutajate arvu user limiidi vastu

Kus kasutatakse:

- `CompanyUserService.UpsertAsync`

##### `EnsureTierAtLeastAsync`

- kontrollib, kas current tier on piisav mingi feature jaoks

Kus kasutatakse:

- `CostEstimatesController`
- `PaymentPlansController`

##### `ResolveEffectivePolicyAsync`

See on teenuse keskne private meetod.

Mida teeb:

1. kui tenant context puudub, eeldab free tieri
2. muul juhul laeb viimase aktiivse subscriptioni
3. kui subscription puudub, eeldab free tieri
4. rakendab vaike- voi custom limiidid
5. premium korral loeb piirangud sisuliselt puuduvaks
6. tagastab sisemise `SubscriptionPolicyState`

##### Muud abimeetodid

- `HasEntityLimit`
- `HasUserLimit`
- `ResolveDefaultLimits`
- private `SubscriptionPolicyState`

### Tenant access

#### `Services/ITenantAccessService.cs`

Mille jaoks:

- kirjeldab tenant rollikontrolli lepingut

Meetod:

- `EnsureCompanyRoleAsync`

#### `Services/TenantAccessService.cs`

Mille jaoks:

- koondada tenant rollikontroll yhte kohta

Constructori soltuvus:

- `AppDbContext`

##### `EnsureCompanyRoleAsync`

Toimimine:

1. kontrollib, et vajalikke rolle anti kaasa vÃ¤hemalt 1
2. kysib `AppUserRoles` tabelist, kas kasutajal on moni nouutud aktiivne roll
3. kui pole, viskab `ForbiddenException`

Miks see oluline on:

- sama kontrolli ei pea koigis teenustes dubleerima

### Treatment plans

#### `Services/ITreatmentPlanService.cs`

Mille jaoks:

- kirjeldab raviplaani workflow teenuse lepingu

Meetodid:

- `SubmitAsync`
- `RecordPlanItemDecisionAsync`

#### `Services/TreatmentPlanService.cs`

Mille jaoks:

- hallata raviplaani CRUD-i ja workflow'd yhes teenuses

Constructori soltuvused:

- `AppDbContext`
- `ITenantAccessService`

Meetodid:

- `ListAsync`
- `GetAsync`
- `CreateAsync`
- `UpdateAsync`
- `SubmitAsync`
- `DeleteAsync`
- `ListOpenItemsAsync`
- `RecordPlanItemDecisionAsync`

##### `SubmitAsync`

Toimimine:

1. kontrollib owner/admin/manager rolli
2. laeb plani koos itemitega
3. kui puudub, viskab vea
4. kui itemeid pole, viskab valideerimisvea
5. seab `SubmittedAtUtc`, kui vaja
6. kutsub `TreatmentPlanWorkflow.ApplyDerivedState`
7. salvestab
8. tagastab `SubmitTreatmentPlanResult`

##### `RecordPlanItemDecisionAsync`

Toimimine:

1. kontrollib role
2. laeb plani koos itemitega
3. kontrollib, et plan oleks submit'itud
4. leiab itemi
5. salvestab decisioni, aja ja notesi
6. arvutab kogu plani derived state uuesti
7. salvestab
8. tagastab `PlanDecisionResult`

### Treatment plan workflow utiliit

#### `Services/TreatmentPlanWorkflow.cs`

Mille jaoks:

- hoida raviplaani oleku reeglid yhes kohas

Kus kasutatakse:

- `TreatmentPlanService`
- `TreatmentPlansController`
- `FinanceController`

##### `ResolveStatus(TreatmentPlan plan)`

- convenience overload, mis loeb info otse planist

##### `ResolveStatus(DateTime? submittedAtUtc, IEnumerable<PlanItemDecision> decisions)`

Peamine loogika:

- pole itemeid voi pole submititud -> `Draft`
- koik accepted -> `Accepted`
- accepted + muud olekud koos -> `PartiallyAccepted`
- koik deferred/rejected -> `Deferred`
- muu -> `Pending`

##### `ApplyDerivedState`

- arvutab plani staatuse
- seab voi nullib `ApprovedAtUtc`

##### `IsLockedForItemReplacement`

- tagastab `true`, kui plan on submititud voi itemitel on otsuse ajalugu

##### `HasDecisionHistory`

- kontrollib, kas item on rohkem kui lihtsalt "Pending ilma ajalooga"

---

## 5. Kuidas seda kausta oppimiseks lugeda?

Hea lugemisjarjekord:

1. `App.BLL.csproj`
2. `Exceptions`
3. `Contracts`
4. `ITenantAccessService` + `TenantAccessService`
5. `PatientService`
6. `AppointmentService`
7. `TreatmentPlanWorkflow` + `TreatmentPlanService`
8. `CompanyUserService`
9. `FinanceMath`
10. `ImpersonationService`

## 6. Koige olulisemad oppetunnid

- BLL peaks hoidma arireeglid controlleritest eemal.
- `Command` ja `Result` mudelid annavad teenusekihi oma selge API.
- Exception'ite kasutamine hoiab teenusekihi puhtana: teenus ei tegele HTTP staatustega.
- Tenant-aware rakenduses tuleb alati koos moelda:
  tenant context + query filter + role control.
- Derived state loogika tasub koondada eraldi helperisse, nagu `TreatmentPlanWorkflow`.
- Raha arvutamise reeglid tasub koondada yhte kohta, nagu `FinanceMath`.

## 7. Luhihinnang App.BLL-ile

Tugevad kohad:

- selge teenusepohine struktuur
- tenant ja role-aware loogika
- command/result muster on jargipidev
- workflow ja finance reeglid on korduskasutatavad

Jargmine loomulik areng:

- laiendaks teenusekihi testikatvust eriti finance ja treatment-plan voogude ymber

Praegune BLL on oppimise jaoks hea, sest siin on naha juba selge teenusekiht ning samas ruumi testide ja teenusepiiride veel paremaks lihvimiseks.

