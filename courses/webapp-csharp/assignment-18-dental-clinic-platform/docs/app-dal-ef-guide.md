# App.DAL.EF oppematerjal

## Uldpilt

`DAL` ehk `Data Access Layer` on kiht, mis vastutab andmebaasiga suhtlemise eest. Selles projektis on `App.DAL.EF` DAL-i teostus, mis kasutab `Entity Framework Core`-i ja PostgreSQL-i.

See projekt teeb kuus peamist asja:

1. hoiab keskset `AppDbContext` klassi;
2. mapib `App.Domain` entiteedid andmebaasitabeliteks;
3. rakendab tenant-isolatsiooni query filtritega;
4. rakendab soft delete'i ja audit logimist `SaveChangesAsync` tasemel;
5. hoiab EF migratsioone;
6. loob arendus- ja demokeskkonna seed-andmed.

Lihtsustatult:

- `App.Domain` kirjeldab, millised objektid rakenduses olemas on;
- `App.DAL.EF` kirjeldab, kuidas need objektid andmebaasis elavad ja kuidas neid salvestada;
- `WebApp` ja `App.BLL` kasutavad seda kihti, et ise mitte SQL detaile hallata.

## Kausta struktuur

```text
src/App.DAL.EF/
  App.DAL.EF.csproj
  AppDbContext.cs
  Tenant/
    ITenantProvider.cs
  Design/
    AppDbContextDesignTimeFactory.cs
  Seeding/
    InitialData.cs
    AppDataInit.cs
    AppDataInit.RichSeed.Helpers.cs
    AppDataInit.RichSeed.Primary.cs
    AppDataInit.RichSeed.Secondary.cs
  Migrations/
    20260305140128_InitialCreate.cs
    20260305140128_InitialCreate.Designer.cs
    20260313120000_ConvertTreatmentPlanStatusToEnum.cs
    20260313120000_ConvertTreatmentPlanStatusToEnum.Designer.cs
    20260317081146_RefactorTreatmentPlanFinanceWorkflow.cs
    20260317081146_RefactorTreatmentPlanFinanceWorkflow.Designer.cs
    AppDbContextModelSnapshot.cs
```

## Kuidas runtime voog kaib?

1. `TenantResolutionMiddleware` loeb URL-ist tenant slugi.
2. Middleware leiab `Company` kirje ja kutsub `ITenantProvider.SetTenant(...)`.
3. `AppDbContext` saab `ITenantProvider` DI kaudu konstruktorisse.
4. `AppDbContext` query filtrid piiravad paringud aktiivse `CompanyId` jargi.
5. `SaveChangesAsync` lisab audit valjad, muudab kustutused soft delete'iks ja kirjutab audit logi.
6. Rakenduse stardis kutsub `WebApp/Setup/AppDataInitExtensions.cs` vajadusel migratsioone ja seedingu meetodeid siit projektist.

See on oluline, sest tenant-turvalisus ei ole ainult controllerites, vaid suuresti andmekihis endas.

## Failid uksikasjalikult

### `App.DAL.EF.csproj`

See fail on projekti build- ja pakettikonfiguratsioon.

Mille jaoks:

- seab `TargetFramework` vaartuseks `net10.0`;
- lubab `ImplicitUsings` ja `Nullable`;
- viitab `App.Domain` projektile;
- toob sisse vajalikud NuGet paketid.

Olulisemad paketid:

- `Microsoft.EntityFrameworkCore.Design`
  - vajalik `dotnet ef` toolinguks;
  - `PrivateAssets=all`, et toolingupaketid ei voolaks tarbijaprojektidesse.
- `Microsoft.EntityFrameworkCore.Tools`
  - EF CLI tugi.
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
  - lubab kasutada `IdentityDbContext` baasklassi.
- `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`
  - lubab data protection votmeid andmebaasis hoida.
- `Npgsql.EntityFrameworkCore.PostgreSQL`
  - EF provider PostgreSQL jaoks.

Kus kasutatakse:

- buildimisel kogu lahenduse poolt;
- `dotnet ef` kaskude ajal;
- `WebApp` tarbib seda projekti `AppDbContext` kaudu.

### `AppDbContext.cs`

See on kogu projekti keskne andmekihi klass. Ta parib `IdentityDbContext<AppUser, AppRole, Guid>`-st ja implementeerib `IDataProtectionKeyContext`.

Mida see tahendab:

- lisaks oma entiteetidele haldab ta ka ASP.NET Core Identity tabeleid;
- sama kontekst hoiab `DataProtectionKeys` tabelit;
- ta on nii andmebaasi skeemi kui ka salvestusreeglite keskpunkt.

#### Konstruktor ja sõltuvused

`AppDbContext` saab kolm sisendit:

- `DbContextOptions<AppDbContext>`
  - provider, connection string ja EF seaded.
- `ITenantProvider`
  - aktiivse tenanti info query filtrite jaoks.
- `IHttpContextAccessor`
  - praeguse kasutaja ID audit valjade jaoks.

#### DbSet-id

Infra:

- `DataProtectionKeys`
- `RefreshTokens`
- `AuditLogs`

Tenant ja admin:

- `Companies`
- `CompanySettings`
- `Subscriptions`
- `AppUserRoles`

Kliiniline pool:

- `Patients`
- `ToothRecords`
- `TreatmentTypes`
- `Treatments`
- `Appointments`
- `TreatmentPlans`
- `PlanItems`
- `Xrays`
- `Dentists`
- `TreatmentRooms`

Finants ja kindlustus:

- `InsurancePlans`
- `PatientInsurancePolicies`
- `CostEstimates`
- `Invoices`
- `InvoiceLines`
- `Payments`
- `PaymentPlans`
- `PaymentPlanInstallments`

#### `SaveChangesAsync`

See on faili koige tahtsam meetod.

Ta teeb enne tegelikku salvestust kolm sammu:

1. `ApplyAuditFields()`
2. `ApplySoftDelete()`
3. `BuildAuditLogEntries()`

Parast esimest `base.SaveChangesAsync(...)` kontrollib ta, kas audit logisid tekkis. Kui tekkis, lisab need `AuditLogs` kogusse ja teeb teise salvestuse.

Miks `_isSavingAuditLog` vajalik on:

- ilma selleta prooviks audit logi enda salvestus omakorda uusi audit logisid tekitada;
- see tekitaks rekursiooni.

#### `OnModelCreating`

See meetod kirjeldab mudeli reegleid.

Ta teeb siin mitu olulist asja:

- kutsub `base.OnModelCreating(builder)`, et Identity skeem alles jaaks;
- kutsub `ConfigureDateTimeAsUtc(builder)`, et koik `DateTime` valjad normaliseeritaks UTC-ks;
- muudab koik foreign key-d `DeleteBehavior.Restrict` peale;
- lisab indeksid ja one-to-one seosed;
- seab raha- ja kogusevaljade `HasPrecision(...)`;
- lisab tenant ja tenant+soft-delete query filtrid.

Naiteid konfiguratsioonist:

- `Company.Slug` on unikaalne;
- `CompanySettings.CompanyId` on unikaalne ja one-to-one `Company`-ga;
- `AppUserRole` kombinatsioon `(AppUserId, CompanyId, RoleName)` on unikaalne;
- `Invoice.InvoiceNumber` on tenanti sees unikaalne;
- `PaymentPlan` on one-to-one `Invoice`-ga.

#### `ConfigureTenantFilter<TEntity>`

Rakendab filtri entiteetidele, mis implementeerivad `ITenantEntity`.

Loogika:

- kui `IgnoreTenantFilter` on `true`, ei piirata tenantit;
- muul juhul peab `entity.CompanyId == _tenantProvider.CompanyId`.

Seda kasutatakse entiteetidel, millel tenant on olemas, aga soft delete'i mitte.

#### `ConfigureTenantSoftDeleteFilter<TEntity>`

Rakendab filtri entiteetidele, millel on korraga:

- `ITenantEntity`
- `ISoftDeleteEntity`

Filter nõuab:

- tenant peab klappima;
- `IsDeleted` peab olema `false`.

See peidab soft-delete read automaatselt.

#### `ApplyAuditFields()`

See meetod kaib enne salvestust labi koik `IAuditableEntity` kirjed.

Ta:

- seab `CreatedAtUtc` ja `CreatedByUserId`, kui kirje on uus;
- seab `ModifiedAtUtc` ja `ModifiedByUserId`, kui kirje on uus voi muudetud.

Lisaks taidab ta uutele `ITenantEntity` kirjetele automaatselt `CompanyId`, kui see on veel `Guid.Empty`.

See on kasulik kaitse, et tenant ei jaaks kogemata lisamata.

#### `ApplySoftDelete()`

Leiab koik `ISoftDeleteEntity` kirjed, mille olek on `Deleted`, ja muudab need:

- olek `Modified`;
- `IsDeleted = true`;
- `DeletedAtUtc = nowUtc`.

See tahendab, et `Remove(...)` kutse loppeb tegelikult pehme kustutamisena.

#### `BuildAuditLogEntries()`

See meetod teeb `ChangeTracker`-i pohjal `AuditLog` kirjed tenant-entiteetide kohta.

Iga logi sisaldab:

- `CompanyId`
- `ActorUserId`
- `EntityName`
- `EntityId`
- `Action`
- `ChangedAtUtc`
- `ChangesJson`

`ChangesJson` luuakse `SerializeChangeSet(...)` abil.

#### `SerializeChangeSet(EntityEntry entry)`

Koostab JSON-i, kus iga property kohta on:

- `OldValue`
- `NewValue`

`Added` korral on vana vaartus `null`, `Deleted` korral uus vaartus `null`.

#### `GetCurrentUserId()`

Loeb `HttpContext` kasutaja `ClaimTypes.NameIdentifier` claimi ja proovib selle `Guid`-iks parse'ida.

Kui kasutaja puudub voi claim ei parse'i, tagastab `null`.

#### `ConfigureDateTimeAsUtc(ModelBuilder builder)`

Lisab koikidele `DateTime` ja `DateTime?` valjadele `ValueConverter`-id, et:

- `Unspecified` ajad markeeritaks UTC-ks;
- lokaalsed ajad teisendataks UTC-ks;
- tagasi loetud ajad markeeritaks UTC-ks.

#### Kus kasutatakse?

- `WebApp/Setup/DatabaseExtensions.cs` registreerib selle DI-s;
- `WebApp/Setup/IdentitySetupExtensions.cs` kasutab sama konteksti Identity store'ina;
- `WebApp/Setup/AppDataInitExtensions.cs` kasutab migratsioonide ja seedingu ajal;
- `WebApp/Middleware/TenantResolutionMiddleware.cs` kasutab `Companies` tabelit tenant slugi lahendamiseks;
- tenant controllerid ja BLL teenused teevad paringuid selle kaudu.

### `Tenant/ITenantProvider.cs`

See interface kirjeldab tenant-konteksti abstraktsiooni.

Omadused:

- `CompanyId`
- `CompanySlug`
- `IgnoreTenantFilter`

Meetodid:

- `SetTenant(Guid companyId, string companySlug)`
- `SetIgnoreTenantFilter(bool ignore)`
- `ClearTenant()`

Mille jaoks see kasulik on:

- DAL saab tenant infot kasutada ilma `WebApp` konkreetset klassi tundmata;
- runtime implementatsioon saab elada `WebApp` projektis;
- design-time jaoks saab teha eraldi lihtsa implementatsiooni.

Kus kasutatakse:

- `AppDbContext` query filtrites;
- `WebApp/Helpers/RequestTenantProvider.cs` realiseerib selle;
- `TenantResolutionMiddleware` taidab selle vaartused requesti alguses.

### `Design/AppDbContextDesignTimeFactory.cs`

See fail on vajalik EF Core design-time toolingule.

Mille jaoks:

- `dotnet ef` peab oskama `AppDbContext` luua ka ilma `WebApplication` bootstrapita;
- see klass annab EF-ile selleks valmis retsepti.

#### `CreateDbContext(string[] args)`

Meetod:

- loeb connection stringi env var-ist `DENTAL_SAAS_CONNECTION_STRING`;
- kui seda ei ole, kasutab localhost Postgresi vaikimisi uhendust;
- loob `DbContextOptionsBuilder<AppDbContext>`;
- kutsub `UseNpgsql(connectionString)`;
- tagastab `AppDbContext` instantsi.

#### Sisemine `DesignTimeTenantProvider`

See privaatne klass implementeerib `ITenantProvider`.

Tema roll:

- `CompanyId` ja `CompanySlug` on `null`;
- `IgnoreTenantFilter = true`.

See on tahtlik, sest migratsioonide loomisel ei tohi tenant filter mudeli ehitamist segada.

### `Seeding/InitialData.cs`

See fail hoiab lihtsat identity algandmestikku.

Sisu:

- `DefaultPassword`
- `Roles`
- `Users`

`Users` massiivis on tuple'd kujul:

- email
- password
- globaalsed system rollid

Miks osadel demo kasutajatel rollid puuduvad:

- nende tenant-rollid lisatakse hiljem `AppUserRole` kirjetena `AppDataInit.cs` kaudu.

Kus kasutatakse:

- `AppDataInit.SeedIdentityAsync(...)`

### `Seeding/AppDataInit.cs`

See on seedingu peamine orkestreerija. Klass on `partial`, sest suur seedingu loogika on jagatud mitme faili vahel.

#### Konstandid

Siin on:

- kahe demo tenant'i slugid;
- peamiste demo kasutajate emailid.

#### `SeedAppDataAsync(...)`

Peamine demodata loomise meetod.

Ta:

1. tagab kahe `Company` kirje olemasolu;
2. tagab `CompanySettings` olemasolu;
3. tagab aktiivsed `Subscription` kirjed;
4. laeb vajalikud identity kasutajad;
5. viskab vea, kui eeldatud seed-userid puuduvad;
6. loob `AppUserRole` tenant-seosed;
7. salvestab vahetulemuse;
8. kaivitab `SeedPrimaryCompanyDataAsync(...)` ja `SeedSecondaryCompanyDataAsync(...)`;
9. salvestab lopptulemuse.

#### `MigrateDatabase(AppDbContext context)`

Wrapper `context.Database.Migrate()` umber.

#### `DeleteDatabase(AppDbContext context)`

Wrapper `context.Database.EnsureDeleted()` umber.

#### `SeedIdentityAsync(...)`

Ta:

- loob vajadusel puuduolevad rollid;
- loob vajadusel puuduolevad kasutajad;
- lisab kasutajatele vajalikud globaalsed system rollid.

Meetod on idempotentne, ehk seda saab kaivitada mitu korda ilma duplikaate tekitamata.

#### `EnsureCompany(...)`

Leiab tenant'i slugi jargi. Kui ei leia, loob uue `Company`.

#### `EnsureCompanySettings(...)`

Tagab, et tenant'il on yks `CompanySettings` kirje.

#### `EnsureActiveSubscription(...)`

Tagab, et tenant'il oleks aktiivne subscription.

#### `EnsureCompanyRoleLink(...)`

Tagab konkreetse kasutaja, tenant'i ja rolli kombinatsiooni olemasolu `AppUserRoles` tabelis.

#### `SeedPrimaryCompanyDataAsync(...)` ja `SeedSecondaryCompanyDataAsync(...)`

Need meetodid koondavad kahe tenant'i kliinilise ja finants demoandmestiku loomise.

### `Seeding/AppDataInit.RichSeed.Helpers.cs`

See fail hoiab suurema seemingu abiloogika.

#### `SeedClinicDataAsync(...)`

Kaib kahes faasis:

1. loob baasandmed: dentistid, ruumid, ravityybid, kindlustusplaanid, patsiendid;
2. salvestab need ja lisab seejarel seotud andmed: tooth chart, completed visitid, tulevased appointmentid, xrayd.

See kahefaasiline lahenemine on oluline, sest seotud kirjed vajavad juba olemasolevaid ID-sid.

#### `EnsureDentist(...)`

- leiab arsti `LicenseNumber` jargi;
- vajadusel loob uue;
- uuendab nime, eriala ja eemaldab soft-delete margistuse.

#### `EnsureTreatmentRoom(...)`

- leiab ruumi `Code` jargi;
- vajadusel loob uue;
- uuendab nime, aktiivsuse ja soft-delete staatuse.

#### `EnsureTreatmentType(...)`

- leiab ravityybi nime jargi;
- vajadusel loob;
- uuendab kestuse, hinna, kirjelduse ja aktiivse staatuse.

#### `EnsureInsurancePlan(...)`

- leiab kindlustusplaani nime jargi;
- vajadusel loob;
- uuendab riigi, katvuse tyybi, endpointi ja aktiivsuse.

#### `EnsurePatient(...)`

- leiab patsiendi isikukoodi jargi;
- vajadusel loob;
- uuendab profiiliandmed;
- rakendab vajadusel soft delete'i seemingu kaudu.

#### `EnsurePatientToothChart(...)`

See meetod ehitab patsiendi hambakaardi visiidiajaloo pohjal.

Loogika:

- votab koik visiitide tooth state'id;
- grupeerib need hambanumbri jargi;
- valib iga hamba kohta koige viimase oleku;
- tagab, et koik püsihambad oleksid `ToothRecord` tabelis olemas;
- kirjutab leitud viimase seisu vastavale hambale.

See on hea naide sellest, kuidas seed andmed tuletatakse kliinilisest loost, mitte ei kirjutata käsitsi 32 eraldi kirjet.

#### `EnsureCompletedVisit(...)`

Tagab minevikus toimunud visiidi olemasolu.

Ta:

- leiab voi loob `Appointment` kirje;
- loob vajadusel visiidi juurde `Treatment` kirjed.

Duplikaatide vältimiseks kasutatakse stabiilset note-stringi, mis ehitatakse `BuildVisitNote(...)` abil.

#### `EnsureUpcomingAppointment(...)`

Tagab tulevase appointmenti olemasolu. Loob ainult aja, mitte treatment kirjeid.

#### `EnsureXray(...)`

Tagab xray metaandmete kirje olemasolu kindla `StoragePath` vaartuse alusel.

#### `EnsurePrimaryFinancialArtifactsAsync(...)`

Loob esimese tenant'i finantsworkflow demo:

- treatment plan
- plan items
- patient insurance policy
- cost estimate
- invoice'id
- invoice line'id
- payment'id
- payment plan
- installments

Meetod salvestab vaheetappe mitu korda, et uued ID-d oleksid kindlalt olemas enne jargmiste seoste loomist.

#### `EnsureSecondaryFinancialArtifactsAsync(...)`

Teeb sama mustri teise tenant'i jaoks, aga teistsuguse demosisuga.

#### `BuildCommonTreatmentTypes()`

Tagastab molema tenant'i jaoks jagatud standardse ravityypide loendi.

#### `BuildVisitNote(...)` ja `BuildUpcomingAppointmentNote(...)`

Loovad stabiilsed unikaalsed note-stringid, et seedingu korduskaivitused ei looks duplikaate.

#### `SeedMoment(...)`

Abimeetod suhteliste kuupaevade loomiseks. See hoiab demodata "varskena", sest kuupaevad arvutatakse `now` suhtes.

#### Faili lopus olevad `record`-id

Need kirjeldavad seed-andmete kuju:

- `ClinicSeed`
- `DentistSeed`
- `TreatmentRoomSeed`
- `TreatmentTypeSeed`
- `InsurancePlanSeed`
- `PatientSeed`
- `VisitSeed`
- `ScheduledAppointmentSeed`
- `VisitItemSeed`
- `XraySeed`
- `ToothStatusSeed`

Need ei ole runtime entiteedid, vaid seemingu sisemised andmekandjad.

### `Seeding/AppDataInit.RichSeed.Primary.cs`

See fail sisaldab esimese demo tenant'i konkreetset kliinilist andmestikku.

Peamine meetod:

- `BuildPrimaryClinicSeed(DateTime now)`

Ta tagastab suure `ClinicSeed` recordi, mis kirjeldab:

- arstid;
- ruumid;
- kindlustusplaanid;
- patsiendid;
- minevikuvisitid;
- tulevased appointmentid;
- xrayd;
- yhe soft-delete patsiendi.

Kus kasutatakse:

- `SeedPrimaryCompanyDataAsync(...)`

Selle faili roll ei ole "algoritm", vaid realistliku demodata hoidmine eraldi failis, et peamine seedingu loogika jaaks loetavaks.

### `Seeding/AppDataInit.RichSeed.Secondary.cs`

See fail teeb sama asja teise demo tenant'i jaoks.

Peamine meetod:

- `BuildSecondaryClinicSeed(DateTime now)`

Ta tagastab teise `ClinicSeed` recordi teistsuguste:

- arstide;
- ruumide;
- patsientide;
- kliiniliste lugude;
- tulevaste aegadega.

See on multi-tenant projekti jaoks oluline, sest molemad tenant'id ei ole lihtsalt teineteise koopiad.

### `Migrations/20260305140128_InitialCreate.cs`

See on esimene migratsioon, mis loob algse skeemi.

#### `Up(...)`

Loob koik peamised tabelid:

- Identity tabelid;
- `Companies`, `CompanySettings`, `Subscriptions`;
- kliinilised tabelid;
- finantstabelid;
- `DataProtectionKeys`, `RefreshTokens`, `AuditLogs`.

Lisaks loob indeksid ja foreign key-d.

#### `Down(...)`

Kustutab tabelid vastupidises jarjekorras, et rollback oleks voimalik.

Kus kasutatakse:

- `context.Database.Migrate()` ajal;
- uue andmebaasi esmasel loomisel.

### `Migrations/20260305140128_InitialCreate.Designer.cs`

See on esimese migratsiooni juurde kuuluv EF genereeritud designer fail.

Peamine roll:

- hoida selle migratsiooni "target model" seisu.

Tavaliselt sisaldab ta `BuildTargetModel(ModelBuilder modelBuilder)` meetodit.

Arendaja seda faili tavaliselt kasitsi ei muuda.

### `Migrations/20260313120000_ConvertTreatmentPlanStatusToEnum.cs`

See migratsioon muudab `TreatmentPlans.Status` veeru tekstist integeriks.

#### `Up(...)`

Kasutab raw SQL-i ja `CASE` avaldist, et:

- olemasolevad tekstvaartused kaardistada enum-numbriteks.

#### `Down(...)`

Teeb vastupidise teisenduse:

- integer tagasi tekstiks.

Miks SQL vajalik on:

- see ei ole ainult skeemi muutus;
- olemasolevad andmed tuleb samuti umber map'ida.

### `Migrations/20260313120000_ConvertTreatmentPlanStatusToEnum.Designer.cs`

Sama migratsiooni designer fail.

Tema roll:

- hoida mudeli seisu parast staatuse valja typimuutust;
- aidata EF-il jargmist migratsiooni arvutada.

### `Migrations/20260317081146_RefactorTreatmentPlanFinanceWorkflow.cs`

See on suurem refaktor-migratsioon raviplaani ja finantsworkflow jaoks.

#### `Up(...)`

Ta:

- lisab `Treatments` tabelisse `PlanItemId`;
- lisab `TreatmentPlans` tabelisse `SubmittedAtUtc`;
- lisab `CostEstimates` tabelisse `CoverageAmount`, `PatientEstimatedAmount`, `PatientInsurancePolicyId`;
- loob uued tabelid `InvoiceLines`, `PatientInsurancePolicies`, `PaymentPlanInstallments`, `Payments`.

Lisaks teeb ta andmemigratsiooni:

- lubab `pgcrypto`, et kasutada `gen_random_uuid()`;
- muudab `CostEstimates.Status` tekstist integeriks;
- backfill'ib `SubmittedAtUtc`;
- arvutab `CoverageAmount` ja `PatientEstimatedAmount`;
- loob olemasolevatele invoice'idele vaikimisi `InvoiceLine` read;
- teisendab vanad payment plan'id detailseteks installments'iteks.

Parast seda eemaldab vana kokkuvotliku payment plan mudeli valjad:

- `InstallmentAmount`
- `InstallmentCount`

#### `Down(...)`

Rollback:

- kustutab uued tabelid ja indeksid;
- eemaldab lisatud valjad;
- taastab vana payment plan skeemi.

Oluline tahelepanek:

- `Down` taastab struktuuri;
- ta ei taasta kogu vana detailset andmesisu kaotuseta. See on migratsioonides tavaline.

### `Migrations/20260317081146_RefactorTreatmentPlanFinanceWorkflow.Designer.cs`

See on eelneva suure migratsiooni designer fail.

Tema roll:

- peegeldada mudeli seisu parast invoice line'ide, payment'ide, insurance policy'de ja uue finantsworkflow lisamist.

### `Migrations/AppDbContextModelSnapshot.cs`

See fail on projekti praeguse mudeli koond-snapshot.

#### `BuildModel(ModelBuilder modelBuilder)`

Kirjeldab kogu viimase migratsiooni jarel kehtivat skeemi:

- entiteedid;
- valjad;
- seosed;
- indeksid;
- piirangud.

Kus kasutatakse:

- EF CLI võrdleb seda faili jooksva `AppDbContext` mudeliga, kui luuakse uus migratsioon.

Kui snapshot on vale voi katki, voivad jargmised migratsioonid tulla vigased.

## Koostoime teiste projektidega

Koige olulisemad kasutuskohad valjaspool `App.DAL.EF` projekti:

- `WebApp/Setup/DatabaseExtensions.cs`
  - registreerib `AppDbContext` ja `UseNpgsql(...)`.
- `WebApp/Setup/AppDataInitExtensions.cs`
  - kutsub `MigrateDatabase`, `SeedIdentityAsync`, `SeedAppDataAsync`.
- `WebApp/Helpers/RequestTenantProvider.cs`
  - runtime implementatsioon `ITenantProvider` interface'ile.
- `WebApp/Middleware/TenantResolutionMiddleware.cs`
  - taidab tenant-konteksti requesti pohjal.
- tenant controllerid ja `App.BLL` teenused
  - teevad paringuid ja salvestusi `AppDbContext` kaudu.

## Mida sellest kaustast eksamiks meelde jatta?

1. `AppDbContext` ei ole siin ainult tabelite nimekiri, vaid rakendab tenant-filtreid, auditit ja soft delete'i.
2. `ITenantProvider` hoiab DAL-i lahti Web-kihi detailidest.
3. Design-time factory on vajalik `dotnet ef` toolingule.
4. Migratsioonid voivad muuta nii skeemi kui ka olemasolevaid andmeid.
5. Seeding on tehtud idempotentseks ja jagatud `partial class` failidesse, et kood pysiks loetav.

## Uhe lausega kokkuvote

`App.DAL.EF` on selle projekti andmekiht, mis seob domeeni entiteedid PostgreSQL andmebaasiga, rakendab tenant-isolatsiooni, soft delete'i ja audit logimist ning haldab migratsioone ja demoandmestikku.
