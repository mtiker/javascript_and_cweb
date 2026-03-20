# App.Domain oppematerjal

## Uldpilt

`App.Domain` on selle lahenduse domeenikiht. Siin elavad rakenduse pohiandmemudelid:

- entiteedid;
- enum'id;
- rollinimed;
- identity laiendused;
- baasliidesed ja baasklassid.

Lihtsustatult:

- `App.Domain` utleb, millised asjad rakenduses olemas on;
- `App.DAL.EF` utleb, kuidas need andmebaasis salvestatakse;
- `App.BLL` utleb, milliste reeglite jargi neid kasutada tohib;
- `WebApp` muudab selle HTTP API-ks ja UI-ks.

See tahendab, et `App.Domain` ei tegele:

- SQL-i;
- HTTP vastuste;
- controllerite;
- EF migratsioonide;
- business workflow detailidega.

Ta annab nende jaoks mudelid, mille peal ulejaanud kihid tootavad.

## Kausta struktuur

```text
src/App.Domain/
  App.Domain.csproj
  RoleNames.cs
  ToothChart.cs
  Common/
    BaseEntity.cs
    IBaseEntity.cs
    IAuditableEntity.cs
    ISoftDeleteEntity.cs
    ITenantEntity.cs
    TenantBaseEntity.cs
  Enums/
    DomainEnums.cs
  Identity/
    AppRole.cs
    AppRefreshToken.cs
    AppUser.cs
  Entities/
    AppUserRole.cs
    Appointment.cs
    AuditLog.cs
    Company.cs
    CompanySettings.cs
    CostEstimate.cs
    Dentist.cs
    InsurancePlan.cs
    Invoice.cs
    InvoiceLine.cs
    Patient.cs
    PatientInsurancePolicy.cs
    Payment.cs
    PaymentPlan.cs
    PaymentPlanInstallment.cs
    PlanItem.cs
    Subscription.cs
    ToothRecord.cs
    Treatment.cs
    TreatmentPlan.cs
    TreatmentRoom.cs
    TreatmentType.cs
    Xray.cs
```

## Kuidas see kiht projektis tootab?

1. `App.Domain` defineerib klassid ja enum'id.
2. `App.DAL.EF/AppDbContext` teeb nende peale `DbSet`-id, seosed, indeksid ja query filtrid.
3. `App.BLL` laeb neid entiteete, kontrollib arireegleid ja muudab nende olekut.
4. `WebApp` mapib requestid DTO-deks ja kutsub BLL teenuseid.
5. `Seeding` loob samade domeeniklasside pohjal demoandmed.

Oluline vaimne mudel:

- domeenikiht on "mis asi see objekt on";
- BLL on "mida selle objektiga teha tohib";
- DAL on "kuidas see objekt andmebaasis elab".

---

## Failid uksikasjalikult

### `App.Domain.csproj`

Mille jaoks:

- deklareerib `App.Domain` eraldi .NET projektina;
- seab `net10.0`, `ImplicitUsings`, `Nullable`;
- toob sisse `Microsoft.AspNetCore.Identity.EntityFrameworkCore` paketi.

Miks see pakett vajalik on:

- `AppUser` ja `AppRole` parivad Identity baasklassidest;
- see lubab sama domeeniprojekti kasutada nii authi kui tenant-rollide mudelis.

Kus kasutatakse:

- `App.DAL.EF`, `App.BLL` ja `WebApp` referentseerivad seda projekti.

### `Common/IBaseEntity.cs`

Mille jaoks:

- defineerida koigile baasentiteetidele yhte `Id` lepingut.

Mida sisaldab:

- `Guid Id`

Kus kasutatakse:

- `BaseEntity`;
- Identity laiendustes, kus vaja yhte tüübitaju.

### `Common/BaseEntity.cs`

Mille jaoks:

- anda lihtne uldine `Guid` identiteet koigile tavapärastele entiteetidele.

Mida teeb:

- implementeerib `IBaseEntity`;
- loob vaikimisi `Id = Guid.NewGuid()`.

Miks see kasulik on:

- entiteedid ei pea igas klassis eraldi `Id` defineerima;
- uued objektid saavad kohe identifikaatori.

### `Common/ITenantEntity.cs`

Mille jaoks:

- markida, et entiteet kuulub kindlasse tenantisse.

Mida sisaldab:

- `Guid CompanyId`

Kus kasutatakse:

- `AppDbContext` tenant query filtrites;
- tenant-andmete automaatsel seostamisel aktiivse companyga.

### `Common/IAuditableEntity.cs`

Mille jaoks:

- markida, et entiteedil on auditvaljad.

Valjad:

- `CreatedAtUtc`
- `ModifiedAtUtc`
- `CreatedByUserId`
- `ModifiedByUserId`

Kus kasutatakse:

- `AppDbContext.ApplyAuditFields()`

### `Common/ISoftDeleteEntity.cs`

Mille jaoks:

- markida, et kustutamine kaib pehme kustutamisena.

Valjad:

- `IsDeleted`
- `DeletedAtUtc`

Kus kasutatakse:

- `AppDbContext.ApplySoftDelete()`;
- tenant + soft delete query filtrites.

### `Common/TenantBaseEntity.cs`

See on uks koige olulisemaid faile kogu domeenikihis.

Mille jaoks:

- anda tenant-entiteetidele korraga:
  - `Id`
  - `CompanyId`
  - auditvaljad
  - soft delete valjad

Mida ta teeb:

- parib `BaseEntity`-st;
- implementeerib `ITenantEntity`, `IAuditableEntity`, `ISoftDeleteEntity`.

See on projekti jaoks hea muster, sest enamik tenant-andmeid vajavad tapselt sama tehnilist alust.

Kus kasutatakse:

- patsiendid;
- appointmentid;
- raviplaanid;
- raviandmed;
- finantsandmed;
- ressursid.

Oluline tahelepanek:

- koik tenantiga seotud klassid ei pari sellest baasist.
- naiteks `CompanySettings`, `Subscription` ja `AppUserRole` implementeerivad ainult `ITenantEntity`.
- see vihjab, et mitte koigil tenant-andmetel ei ole vaja sama audit/soft-delete kasti.

### `Enums/DomainEnums.cs`

Mille jaoks:

- hoida koik peamised workflow ja oleku enum'id yhes failis.

See fail sisaldab:

- `SubscriptionTier`
- `SubscriptionStatus`
- `ToothConditionStatus`
- `UrgencyLevel`
- `PlanItemDecision`
- `TreatmentPlanStatus`
- `AppointmentStatus`
- `CoverageType`
- `InvoiceStatus`
- `PaymentPlanStatus`
- `PaymentPlanInstallmentStatus`
- `PatientInsurancePolicyStatus`
- `CostEstimateStatus`

Miks see oluline on:

- BLL ja DAL kasutavad samu olekuid;
- migratsioonid saavad seostada DB vaartused kindla enumiga;
- controllerid ja DTO-d saavad liikuda stringide asemel tugeva tyybiga mudeli peal.

Hea naide:

- `TreatmentPlanStatus` ja `PlanItemDecision` annavad raviplaani workflow jaoks selge olekumasina;
- `InvoiceStatus`, `PaymentPlanStatus` ja `CostEstimateStatus` hoiavad finantsvood uhtses mudelis.

### `RoleNames.cs`

Mille jaoks:

- hoida globaalsed system-rollid ja tenant-rollid uhes kohas.

Konstandid:

- `SystemAdmin`
- `SystemSupport`
- `SystemBilling`
- `CompanyOwner`
- `CompanyAdmin`
- `CompanyManager`
- `CompanyEmployee`

Lisaks:

- `All` massiiv koigi rollinimedega.

Kus kasutatakse:

- Identity seedimisel;
- onboardingus rollide tagamisel;
- authorizationis;
- tenant ligipaasukontrollis;
- impersonation ja UI rolliloogikas.

### `ToothChart.cs`

Mille jaoks:

- hoida pysi-hammaste ametlikku nimekirja ja valideerimisabi uhes kohas.

Sisu:

- `PermanentToothNumbers`
- private `PermanentToothNumberSet`
- `IsValidPermanentToothNumber(int toothNumber)`

Kus kasutatakse:

- patsiendi hambakaardi vaikimisi loomisel;
- appointmenti kliinilise too valideerimisel;
- hambakaardi taielikkuse kontrollis.

Miks see hea on:

- hambanumbrite loogika ei ole laiali teenustes ega controllerites.

---

## Identity kaust

### `Identity/AppUser.cs`

Mille jaoks:

- laiendada ASP.NET Core Identity kasutajamudelit selle projekti vajadustele.

Mida teeb:

- parib `IdentityUser<Guid>`-st;
- implementeerib `IBaseEntity`.

Navigatsioonid:

- `RefreshTokens`
- `CompanyRoles`

Mida see sisuliselt tahendab:

- sama kasutaja voib omada mitut tenant-rolli eri companydes;
- kasutaja juurde saab siduda refresh tokeneid.

Kus kasutatakse:

- auth voogudes;
- onboardingus;
- tenant role halduses;
- impersonationis.

### `Identity/AppRole.cs`

Mille jaoks:

- kasutada projekti enda role-tyypi Identity sees.

Mida teeb:

- parib `IdentityRole<Guid>`-st;
- implementeerib `IBaseEntity`.

Miks see kasulik on:

- hoiab kasutaja ja rolli tyybid samal `Guid`-pohisel mudelil;
- vajadusel saab tulevikus rollile lisavalju juurde lisada.

### `Identity/AppRefreshToken.cs`

Mille jaoks:

- modelleerida kasutaja refresh token kirje.

Valjad:

- `RefreshToken`
- `Expiration`
- `PreviousRefreshToken`
- `PreviousExpiration`
- `UserId`

Navigatsioon:

- `User`

Miks `Previous...` valjad huvitavad on:

- need toetavad refresh token roteerimise voogu;
- saab tuvastada eelmist ahelat voi lubada sujuvamat tokeni uuendamist.

Kus kasutatakse:

- `AccountController` ja auth teenuste tokeniloogikas;
- `AppDbContext` hoiab seda eraldi tabelina.

---

## Tenanti ja platvormi entiteedid

### `Entities/Company.cs`

Mille jaoks:

- kirjeldada yht tenant-kliinikut platvormis.

Valjad:

- `Name`
- `Slug`
- `IsActive`
- `CreatedAtUtc`
- `DeactivatedAtUtc`

Navigatsioonid:

- `Settings`
- `Subscriptions`
- `UserRoles`

Miks see oluline on:

- tenant resolution middleware leiab requesti slugist just selle entiteedi;
- kogu tenant-isolatsioon toetub sellele, et muud andmed viitavad `CompanyId` kaudu companyle.

### `Entities/CompanySettings.cs`

Mille jaoks:

- hoida kliiniku tasemel seadistusi.

Valjad:

- `CompanyId`
- `CountryCode`
- `CurrencyCode`
- `Timezone`
- `DefaultXrayIntervalMonths`

Navigatsioon:

- `Company`

Oluline detail:

- see on tenant-objekt, aga ei pari `TenantBaseEntity`-st.
- siin pole audit- ega soft delete valju.

Kus kasutatakse:

- `CompanySettingsService`;
- X-ray ja lokaalseadete vaikimisi reeglites.

### `Entities/Subscription.cs`

Mille jaoks:

- kirjeldada tenant'i paketti ja limiite.

Valjad:

- `CompanyId`
- `Tier`
- `Status`
- `StartsAtUtc`
- `EndsAtUtc`
- `UserLimit`
- `EntityLimit`

Navigatsioon:

- `Company`

Kus kasutatakse:

- onboarding loob tasuta subscriptioni;
- `SubscriptionPolicyService` loeb siit tenant piirangud.

### `Entities/AppUserRole.cs`

Mille jaoks:

- modelleerida kasutaja tenant-spetsiifiline roll.

Valjad:

- `AppUserId`
- `CompanyId`
- `RoleName`
- `IsActive`
- `AssignedAtUtc`

Navigatsioonid:

- `AppUser`
- `Company`

Miks seda on vaja lisaks Identity rollidele:

- Identity rollid on globaalsed;
- see entiteet seob kasutaja konkreetse companyga.

See on multi-tenant rakenduse jaoks kriitiline tabel.

Kus kasutatakse:

- `switch-company` voos;
- tenant access kontrollis;
- team managementis;
- impersonationis.

### `Entities/AuditLog.cs`

Mille jaoks:

- talletada muutuste audit rada.

Valjad:

- `CompanyId`
- `ActorUserId`
- `EntityName`
- `EntityId`
- `Action`
- `ChangedAtUtc`
- `ChangesJson`

Oluline detail:

- `CompanyId` on nullable, mis lubab logida ka globaalsemaid muutusi.

Kus kasutatakse:

- `AppDbContext.BuildAuditLogEntries()`.

---

## Kliinilised entiteedid

### `Entities/Patient.cs`

Mille jaoks:

- kirjeldada patsienti tenant-kontekstis.

Pohivaljad:

- `FirstName`
- `LastName`
- `DateOfBirth`
- `PersonalCode`
- `Email`
- `Phone`

Navigatsioonid:

- `ToothRecords`
- `Appointments`
- `TreatmentPlans`
- `Treatments`
- `CostEstimates`
- `Invoices`
- `InsurancePolicies`

Miks see on keskne entiteet:

- suur osa kliinilisest ja finantsandmestikust ripub patsiendi kyljes.

### `Entities/ToothRecord.cs`

Mille jaoks:

- hoida patsiendi yhe hamba hetkeseisu.

Valjad:

- `PatientId`
- `ToothNumber`
- `Condition`
- `Notes`

Navigatsioon:

- `Patient`

Kus kasutatakse:

- patsiendi profiili koostamisel;
- kliinilise too sisestamisel;
- vaikimisi hambakaardi loomisel.

### `Entities/TreatmentType.cs`

Mille jaoks:

- kirjeldada kliinikus kasutatavat ravityypi.

Valjad:

- `Name`
- `DefaultDurationMinutes`
- `BasePrice`
- `Description`

Navigatsioon:

- `PlanItems`

Kus kasutatakse:

- raviplaani ridades;
- tehtud protseduuride sidumisel;
- hinnastuse vaikimisi alustes.

### `Entities/Treatment.cs`

Mille jaoks:

- kirjeldada reaalselt tehtud ravi voi protseduuri.

Valjad:

- `PatientId`
- `TreatmentTypeId`
- `PlanItemId`
- `AppointmentId`
- `DentistId`
- `ToothNumber`
- `PerformedAtUtc`
- `Price`
- `Notes`

Navigatsioonid:

- `Patient`
- `TreatmentType`
- `PlanItem`
- `Appointment`
- `Dentist`
- `InvoiceLines`

Miks see entiteet on oluline:

- see seob kokku kliinilise too, raviplaani ja hilisema arvelduse.

### `Entities/Appointment.cs`

Mille jaoks:

- kirjeldada visiidiaega.

Valjad:

- `PatientId`
- `DentistId`
- `TreatmentRoomId`
- `StartAtUtc`
- `EndAtUtc`
- `Status`
- `Notes`

Navigatsioonid:

- `Patient`
- `Dentist`
- `TreatmentRoom`
- `Treatments`

Kus kasutatakse:

- ajastamises;
- konfliktikontrollis;
- kliinilise too salvestamisel;
- UI kalendrilaadses vaates.

### `Entities/Dentist.cs`

Mille jaoks:

- kirjeldada arsti kui tenanti ressurssi.

Valjad:

- `AppUserId`
- `DisplayName`
- `LicenseNumber`
- `Specialty`

Navigatsioonid:

- `AppUser`
- `Appointments`

Oluline detail:

- `AppUserId` on nullable.
- see lubab arstikirjet hoida ka siis, kui ta pole otseselt login-kasutajaga seotud.

### `Entities/TreatmentRoom.cs`

Mille jaoks:

- kirjeldada kabinetti voi toolauda, kuhu appointment broneeritakse.

Valjad:

- `Name`
- `Code`
- `IsActiveRoom`

Navigatsioon:

- `Appointments`

Kus kasutatakse:

- aja kattuvuse kontrollis;
- ressursside halduses.

### `Entities/Xray.cs`

Mille jaoks:

- hoida rontgeni metaandmeid.

Valjad:

- `PatientId`
- `TakenAtUtc`
- `NextDueAtUtc`
- `StoragePath`
- `Notes`

Navigatsioon:

- `Patient`

Oluline detail:

- siin hoitakse faili teed ja metaandmeid, mitte binaarfaili ennast.

### `Entities/TreatmentPlan.cs`

Mille jaoks:

- kirjeldada patsiendi raviplaani koondobjekti.

Valjad:

- `PatientId`
- `DentistId`
- `Status`
- `SubmittedAtUtc`
- `ApprovedAtUtc`

Navigatsioonid:

- `Patient`
- `Dentist`
- `Items`

Miks need kuupaevad olulised on:

- `SubmittedAtUtc` eristab drafti ja workflow's liikuva plaani;
- `ApprovedAtUtc` aitab derived-state loogikat toetada.

### `Entities/PlanItem.cs`

Mille jaoks:

- kirjeldada raviplaani yksikut rida.

Valjad:

- `TreatmentPlanId`
- `TreatmentTypeId`
- `Sequence`
- `Urgency`
- `EstimatedPrice`
- `Decision`
- `DecisionAtUtc`
- `DecisionNotes`

Navigatsioonid:

- `TreatmentPlan`
- `TreatmentType`
- `Treatments`
- `InvoiceLines`

Miks see on oluline:

- plan item on sild raviplaani, tehtud ravi ja arvelduse vahel.

---

## Kindlustuse ja finantsi entiteedid

### `Entities/InsurancePlan.cs`

Mille jaoks:

- kirjeldada kindlustusplaani voi riikliku katvuse skeemi.

Valjad:

- `Name`
- `CountryCode`
- `CoverageType`
- `IsActivePlan`
- `ClaimSubmissionEndpoint`

Navigatsioonid:

- `PatientPolicies`
- `CostEstimates`

Kus kasutatakse:

- kindlustusplaanide CRUD-is;
- hinnangute ja claim workflow alusena.

### `Entities/PatientInsurancePolicy.cs`

Mille jaoks:

- siduda konkreetne patsient konkreetse kindlustusplaaniga.

Valjad:

- `PatientId`
- `InsurancePlanId`
- `PolicyNumber`
- `MemberNumber`
- `GroupNumber`
- `CoverageStart`
- `CoverageEnd`
- `AnnualMaximum`
- `Deductible`
- `CoveragePercent`
- `Status`

Navigatsioonid:

- `Patient`
- `InsurancePlan`
- `CostEstimates`

Miks see on eraldi entiteet:

- yldine kindlustusplaan ja konkreetse patsiendi poliis ei ole sama asi.

### `Entities/CostEstimate.cs`

Mille jaoks:

- kirjeldada raviplaani hinnangut enne arvet.

Valjad:

- `PatientId`
- `TreatmentPlanId`
- `InsurancePlanId`
- `PatientInsurancePolicyId`
- `EstimateNumber`
- `FormatCode`
- `TotalEstimatedAmount`
- `CoverageAmount`
- `PatientEstimatedAmount`
- `GeneratedAtUtc`
- `Status`

Navigatsioonid:

- `Patient`
- `TreatmentPlan`
- `InsurancePlan`
- `PatientInsurancePolicy`

Mida see peegeldab:

- hinnang voib olla puhtalt patsiendi kulu voi sisaldada kindlustuse osa;
- ta on sild ravi planeerimise ja arvelduse vahel.

### `Entities/Invoice.cs`

Mille jaoks:

- kirjeldada arvet patsiendile.

Valjad:

- `PatientId`
- `CostEstimateId`
- `InvoiceNumber`
- `TotalAmount`
- `BalanceAmount`
- `DueDateUtc`
- `Status`

Navigatsioonid:

- `Patient`
- `CostEstimate`
- `PaymentPlan`
- `Lines`
- `Payments`

Miks `BalanceAmount` oluline on:

- see lubab maksete ja staatuse muutusi kiiresti peegeldada ilma koike iga kord uuesti arvutamata ainult UI tasemel.

### `Entities/InvoiceLine.cs`

Mille jaoks:

- kirjeldada arve yksikut rida.

Valjad:

- `InvoiceId`
- `TreatmentId`
- `PlanItemId`
- `Description`
- `Quantity`
- `UnitPrice`
- `LineTotal`
- `CoverageAmount`
- `PatientAmount`

Navigatsioonid:

- `Invoice`
- `Treatment`
- `PlanItem`

Miks see oluline on:

- invoice ei ole ainult summa, vaid detailne koosseis;
- rida saab vajadusel viidata nii tehtud ravile kui ka plaani reale.

### `Entities/Payment.cs`

Mille jaoks:

- kirjeldada yht laekunud makset.

Valjad:

- `InvoiceId`
- `Amount`
- `PaidAtUtc`
- `Method`
- `Reference`
- `Notes`

Navigatsioon:

- `Invoice`

Kus kasutatakse:

- arve saldo arvutamisel;
- payment plan oleku tuletamisel.

### `Entities/PaymentPlan.cs`

Mille jaoks:

- kirjeldada osamaksegraafiku koondobjekti.

Valjad:

- `InvoiceId`
- `StartsAtUtc`
- `Status`
- `Terms`

Navigatsioonid:

- `Invoice`
- `Installments`

Kus kasutatakse:

- kui arve tasumine jaotatakse mitmeks maksegraafiku reaks.

### `Entities/PaymentPlanInstallment.cs`

Mille jaoks:

- kirjeldada yhest payment planist tulenevat yksikut osamakset.

Valjad:

- `PaymentPlanId`
- `DueDateUtc`
- `Amount`
- `Status`
- `PaidAtUtc`

Navigatsioon:

- `PaymentPlan`

Miks see eraldi entiteet vajalik on:

- lihtne `InstallmentCount + InstallmentAmount` ei kirjelda pariselu piisavalt hasti;
- iga osamakse voib olla eri staatuse ja tasumise ajaga.

---

## Mustrid, mida sellest kaustast oppida

### 1. Domeen hoiab andmestruktuuri, mitte HTTP-d

Ukski siinne klass ei tea midagi:

- controlleritest;
- `ProblemDetails`-ist;
- route'idest;
- authorization attribuutidest.

See on hea kihistus.

### 2. Tenant-andmed on selgelt eristatavad

Kui entiteet kuulub tenantisse, siis tal on `CompanyId`.

Enamasti toimub see:

- kas `TenantBaseEntity` kaudu;
- voi minimaalselt `ITenantEntity` kaudu.

See teeb hilisema tenant-filtreerimise lihtsaks.

### 3. Navigatsioonid on teadlikult rikkad

Paljudel entiteetidel on kogumikud ja viited naaberobjektidele.

See lubab:

- EF-il seoseid kaardistada;
- BLL-il laadida use-case jaoks vajalikke puid;
- UI ja API jaoks koondvastuseid kokku panna.

### 4. Enum'id hoiavad workflow puhtana

Selle asemel, et salvestada olekuid vabatekstina, on kasutusel tugevad enum'id.

See vahendab:

- typo-riski;
- ebauhtlast staatuste kasutust;
- kontrollimatut stringivordlust.

### 5. Domeeni klassides on minimaalselt kaitumist

Siin on peamiselt:

- omadused;
- vaikimisi vaartused;
- navigatsioonid.

Erandilaadne helper on `ToothChart`, sest see on puhas domeenireegel, mitte BLL workflow.

---

## Hea lugemisjarjekord

1. `App.Domain.csproj`
2. `Common/*`
3. `Enums/DomainEnums.cs`
4. `RoleNames.cs`
5. `ToothChart.cs`
6. `Identity/*`
7. `Company`, `Subscription`, `AppUserRole`
8. `Patient`, `Appointment`, `TreatmentPlan`, `PlanItem`, `Treatment`
9. finantsentiteedid `InsurancePlan` kuni `PaymentPlanInstallment`
10. loe seejarel `App.DAL.EF/AppDbContext.cs`, et naha kuidas domeen andmebaasiks mapitakse

## Koige olulisemad oppetunnid

- Hea domeenikiht annab uhtse keele kogu rakendusele.
- `TenantBaseEntity` on tugev viis korduva multi-tenant infrastruktuuri koondamiseks.
- Identity rollid ja tenant-rollid ei ole sama asi; siin on need teadlikult lahutatud.
- Workflow olekud tasub modelleerida enum'idega, mitte vabatekstiga.
- Domeen ei pea olema "tark" klasside kogum; ka puhas ja selge andmemudel on hea domeenikihi alus.

## Uhe lausega kokkuvote

`App.Domain` on selle projekti pohimudelikiht, mis defineerib koik olulised objektid, rollid, olekud ja seosed, mille peale DAL, BLL ja Web kiht oma loogika ehitavad.
