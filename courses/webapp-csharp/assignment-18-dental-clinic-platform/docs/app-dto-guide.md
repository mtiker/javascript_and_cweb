# App.DTO oppematerjal

## Uldpilt

`DTO` ehk `Data Transfer Object` on mudel, mida kasutatakse andmete liigutamiseks ule kihtide voi ule HTTP piiri.

Selles projektis on `App.DTO` eraldi projekt, mis hoiab API request- ja response-mudeleid.

Lihtsustatult:

- `App.Domain` utleb, mis objektid rakenduses olemas on;
- `App.DAL.EF` utleb, kuidas need andmebaasis elavad;
- `App.BLL` utleb, milliste arireeglite jargi neid kasutada tohib;
- `App.DTO` utleb, milline JSON kuju liigub API kaudu sisse ja valja;
- `WebApp` seob need koik kokku controllerites.

See on oluline, sest API leping ei pea olema 1:1 sama mis andmebaasi entiteet voi BLL result.

`App.DTO` aitab:

- hoida HTTP lepingu stabiilsena;
- peita siseimplementatsiooni detaile;
- lisada requestidele valideerimisreegleid;
- koondada Swaggeri jaoks selged request/response skeemid;
- hoida controllerite sisendid ja valjundid loetavad.

## Mida `App.DTO` ei tee

`App.DTO` ei tohiks kanda endas:

- EF Core entiteete;
- andmebaasi mapingut;
- tenant query filtreid;
- keerukat ariloogikat;
- teenuste workflow kontrolli;
- controlleri infrastruktuuri.

Kui klass hakkab tegema otsuseid stiilis "kas tohib", "mis staatus peaks olema" voi "millist entiteeti muuta", siis see ei ole enam DTO roll.

## Kausta struktuur

```text
src/App.DTO/
  App.DTO.csproj
  v1/
    Message.cs
    Appointments/
    CompanySettings/
    CompanyUsers/
    CostEstimates/
    Dentists/
    Finance/
    Identity/
    InsurancePlans/
    Invoices/
    PatientInsurancePolicies/
    Patients/
    PaymentPlans/
    Payments/
    Subscriptions/
    System/
      Billing/
      Platform/
      Support/
    ToothRecords/
    TreatmentPlans/
    TreatmentRooms/
    TreatmentTypes/
    Xrays/
```

Praeguses seisus on siin kokku umbes 90 C# faili.

Koige olulisem korraldusreegel on see, et DTO-d on versioonitud namespace'i all `App.DTO.v1.*`.

See tahendab:

- API versioon `v1` on ka koodis naha;
- sama valdkonna DTO-d on koos yhes kaustas;
- tulevikus saab vajadusel lisada `v2`, ilma et vana leping kohe katki laheks.

## Kuidas runtime voog kaib

Lihtsustatud request-response voog naeb selles projektis valja nii:

1. klient saadab JSON requesti;
2. ASP.NET Core mapib JSON-i `App.DTO.v1.*Request` klassi;
3. `[ApiController]` ja `DataAnnotations` kontrollivad pohivalideerimise;
4. controller teeb DTO-st BLL command'i voi kasutab DTO andmeid otse lihtsas CRUD voos;
5. BLL voi `AppDbContext` annab tagasi andmed;
6. controller mapib tulemuse `App.DTO.v1.*Response` klassi;
7. ASP.NET Core serialiseerib response DTO uuesti JSON-iks.

Selle projekti oluline praktiline detail:

- keerukamad vood kasutavad DTO -> BLL command -> BLL result -> DTO response mustrit;
- osa lihtsamaid tenant CRUD controllereid kasutab DTO -> entity ja entity -> DTO mapingut otse `WebApp` kihis.

Seega `App.DTO` teenindab korraga kahte stiili:

- service-first controllerid;
- otse `AppDbContext`-i kasutavad controllerid.

## Projekti fail

### `App.DTO.csproj`

Mille jaoks:

- deklareerib `App.DTO` eraldi .NET projektina;
- seab `net10.0`, `ImplicitUsings`, `Nullable`;
- ei lisa projektiviiteid `App.Domain`, `App.DAL.EF` ega `App.BLL` suunas.

See viimane punkt on tahtis.

See tahendab, et DTO projekt on teadlikult kerge:

- siin ei ole andmebaasi soltuvusi;
- siin ei ole ariloogika soltuvusi;
- siin ei ole Identity voi EF pakettide koormust;
- see projekt hoiab ainult API lepingut.

Praeguse lahenduse sees referentseerib `App.DTO` projekti otse `WebApp`.

## Pohi-mustrid selles projektis

### 1. Request ja response on eraldi klassid

Naiteks:

- `CreateAppointmentRequest`
- `AppointmentResponse`
- `CreateInvoiceRequest`
- `InvoiceResponse`
- `UpdateInvoiceRequest`

See on hea praktika, sest sisend ja valjund ei ole tavaliselt sama kuju.

### 2. Request klassidel on `DataAnnotations`

Request DTO-d kasutavad tihti atribuute nagu:

- `[Required]`
- `[MaxLength(...)]`
- `[MinLength(...)]`
- `[Range(...)]`
- `[EmailAddress]`
- `[RegularExpression(...)]`

Naiteks:

- `System/RegisterCompanyRequest` piirab `CompanySlug` kuju regexiga;
- `Identity` requestid piiravad emaili ja parooli kuju;
- `Appointments/RecordAppointmentClinicalRequest` nouab vahemalt yht kirjet.

Need reeglid ei asenda BLL arireegleid, vaid annavad HTTP piiril esimese kaitsekihi.

### 3. Response DTO-d on enamasti lamedad ja serialiseeritavad

Response klassid kannavad tavaliselt:

- identifikaatoreid;
- nimesid ja koode;
- staatuseid;
- kuupaevi;
- summasid;
- nested response kogusid, kui API peab andma koondvaate.

Siin ei tehta tavaliselt meetodeid ega keerukat arvutusloogikat.

### 4. Koondvaated kasutavad nested DTO-sid

Head naited:

- `Finance/FinanceWorkspaceResponse`
- `Invoices/InvoiceDetailResponse`
- `Patients/PatientProfileResponse`

See muster on kasulik siis, kui yks endpoint peab andma korraga mitu seotud vaadet.

### 5. Yks yhte error-response klass

`v1/Message.cs` on lihtne yldine vea voi infoteate konteiner.

See annab controlleritele yhtlase viisi tagastada:

- yks viga;
- mitu viga;
- lihtne inimloetav sonum.

## Failid ja valdkonnad

### `v1/Message.cs`

Mille jaoks:

- yldine lihtne sonumivastus.

Mida sisaldab:

- `ICollection<string> Messages`
- konstruktorid tyhja ja `params string[]` sisendiga

Kus kasutatakse:

- `AccountController`
- tenant CRUD controllerid
- kohtades, kus on vaja tagastada `BadRequest`, `NotFound` voi muu lihtne tekstiline vastus

Miks see kasulik on:

- ei pea igale controllerile eraldi error DTO-d leiutama;
- Swaggeris on vea kuju yhtlane.

### `v1/Identity/*`

See kaust hoiab autentimise ja sessiooni DTO-sid:

- `Register`
- `Login`
- `JWTResponse`
- `RefreshTokenModel`
- `ForgotPasswordRequest`
- `ForgotPasswordResponse`
- `ResetPasswordRequest`
- `SwitchCompanyRequest`
- `SwitchRoleRequest`

Siin on naha hea API lepingu piir:

- request DTO kirjeldab, mida klient peab saatma;
- response DTO kirjeldab, millise tokeni- ja aktiivse company info klient tagasi saab.

`JWTResponse` ei tea midagi sellest, kuidas JWT tegelikult genereeritakse. Ta lihtsalt kirjeldab valjundi kuju.

### `v1/System/*`

See ala hoiab system/backoffice endpointide DTO-sid:

- tenant onboarding
- impersonation
- platform analytics ja feature flagid
- billing kokkuvotted
- support vaated

See on hea naide, et DTO kaustad voivad olla seotud API pinna, mitte tingimata andmebaasi tabelitega.

Naiteks:

- `PlatformAnalyticsResponse` on puhas API vaade koondnumbrite jaoks;
- see ei ole omaette domeeni entiteet.

### `v1/Appointments/*`

Siin on appointment API lepingu mudelid:

- `CreateAppointmentRequest`
- `RecordAppointmentClinicalRequest`
- `AppointmentResponse`
- `AppointmentClinicalRecordResponse`

Oluline detail:

- `RecordAppointmentClinicalRequest` sisaldab nested `RecordAppointmentClinicalItemRequest` klassi;
- `Condition` liigub stringina, mitte enum'ina;
- controller valideerib selle ja mapib seejarel domeeni/BLL enum'iks.

See on tahtis disainivalik:

- valine API ei pea olema .NET enum nimega liiga tihedalt seotud;
- controller piiril saab anda parema veateate.

### `v1/Patients/*`

Siin on naha lihtsat parimist ja response laiendamist:

- `PatientResponse` on baasvaade;
- `PatientProfileResponse` parib sellest ja lisab `Teeth`;
- `PatientToothResponse` ja `PatientToothHistoryResponse` annavad nested detailid.

See muster hoiab korduse vaikesena:

- lihtne list/detail ei vaja koiki lisavalju;
- profiilivaade saab olemasolevat response'i laiendada.

### `v1/Finance/*`, `v1/Invoices/*`, `v1/PaymentPlans/*`, `v1/Payments/*`

Need kaustad on hea naide keerukamast response koostamisest.

Naiteks:

- `FinanceWorkspaceResponse` koondab patsiendi, kindlustuse, raviplaanid, hinnangud, protseduurid ja arved;
- `InvoiceDetailResponse` parib `InvoiceResponse`-st ja lisab read, maksed ning voimaliku maksegraafiku;
- `PaymentPlanResponse` sisaldab installments response kogumit.

See aitab hoida kliendirakenduse jaoks kasulikke "read model" kujusid, ilma et peaks lekkima siseentiteetide navigatsioonipuud otse API-sse.

### Ressursi-pohised CRUD kaustad

Kaustad nagu:

- `Dentists`
- `TreatmentRooms`
- `TreatmentTypes`
- `InsurancePlans`
- `Xrays`
- `ToothRecords`

jargivad yldiselt lihtsat mustrit:

- create voi upsert request
- update request voi olemasoleva requesti taaskasutus
- response

See on hea koht naha, et DTO disain voib olla pragmaatiline.

Kui create ja update vajavad sama sisendkuju, ei pea tingimata looma kahte erinevat klassi.

## Kuidas `App.DTO` sobitub teiste kihtidega

### Suhe `WebApp` kihiga

`WebApp` on selle projekti peamine DTO tarbija.

Controllerid:

- votavad `[FromBody]` request DTO;
- deklareerivad `ProducesResponseType(...)` response DTO-dega;
- mapivad DTO-d BLL command'ideks voi entiteetideks;
- tagastavad DTO vastused.

Ilma `App.DTO` projektita peaksid controllerid kasutama kas:

- EF entiteete otse API-s;
- voi BLL contract'e otse HTTP lepinguna.

Molemad seoksid kihid liiga tugevalt kokku.

### Suhe `App.BLL` kihiga

Oluline piir:

- `App.BLL` ei referentseeri `App.DTO` projekti.

See on hea arhitektuuriline otsus, sest BLL ei pea teadma, kas tema tarbijaks on:

- HTTP API;
- background job;
- CLI;
- test helper;
- muu adapter.

Controller on see koht, kus toimub maping:

- DTO request -> BLL command
- BLL result -> DTO response

### Suhe `App.Domain` ja `App.DAL.EF` kihiga

`App.DTO` ei tohiks olla entiteetide peegelkoopia.

Hea pohjus:

- domeeni klassil voivad olla navigatsioonid, audit valjad voi tenant detailid, mida API ei peaks valja andma;
- API response voib vajada hoopis teistsugust koondkuju;
- request DTO voib olla palju kitsam kui tegelik entiteet.

## Valideerimise kihtide jaotus

Selles projektis tasub meeles pidada kolme eri taset:

1. DTO valideerimine
- kontrollib kuju ja lihtsat sisendi korrektsust.

2. Controlleri piiri valideerimine
- teeb vahel lisakontrolli, eriti siis kui tuleb string mapida enum'iks voi domeenireegliks.

3. BLL valideerimine
- kontrollib arireegleid, tenant-reegleid ja workflow tingimusi.

Naiteks appointment clinical record voos:

- DTO kontrollib, et `Items` pole tyhi ja valjad on olemas;
- controller kontrollib, et `Condition` parsitakse enum'iks ja hambanumber on lubatud;
- BLL kontrollib juba workflow reegleid ja seotud andmete olemasolu.

## Mis on selles projektis hea `App.DTO` naide

1. `v1` namespace on jarjepidev.
2. Request ja response on lahutatud.
3. `DataAnnotations` annavad kiire sisendikontrolli.
4. Nested response mudelid teevad koond-endpointid loetavaks.
5. `WebApp` kannab mapingu vastutuse, mitte DTO ise.
6. `App.BLL` ei ole HTTP detailidega seotud.

## Mille suhtes peab ettevaatlik olema

1. Ara pane DTO-sse ariloogikat.
2. Ara lase BLL-il soltuda `App.DTO` projektist.
3. Ara tagasta EF entiteete otse controllerist, kui response kuju peab olema avalik leping.
4. Ara kasuta request DTO-d vaikimisi andmebaasientiteedina.
5. Kui kasutad stringe enum'ide asemel, valideeri need alati piiril ara.
6. Kui response laheb suureks, jaga see nested DTO-deks, mitte ara topi koike yhte lamedasse klassi.

## Kuidas lisada uus DTO siia projekti

Praktiline tootusammude jada voiks olla:

1. vali oige API valdkonna kaust `v1` all;
2. lisa request ja/või response klass;
3. lisa `DataAnnotations`, kui HTTP sisend vajab pohivalideerimist;
4. kasuta uut DTO-d controlleri signatuuris ja `ProducesResponseType` atribuutides;
5. maping tee controlleris voi eraldi mapper-helperis, mitte DTO klassi sees;
6. uuenda testid, et uus request/response kuju oleks kaetud;
7. hoia namespace jarjepidev kujul `App.DTO.v1.<Area>`.

## Soovituslik lugemisjarg

Kui tahad seda projekti oppimise vaatest kiiresti moista, siis loe selles jargnevuses:

1. `App.DTO.csproj`
2. `v1/Message.cs`
3. `v1/Identity/*`
4. `v1/Patients/*`
5. `v1/Appointments/*`
6. `v1/TreatmentPlans/*`
7. `v1/Finance/*` ja `v1/Invoices/*`
8. seejarel vaata `WebApp/ApiControllers`, et naha kuidas DTO-d tegelikult kasutusse jouavad

## Luhihinnang `App.DTO` kihile

`App.DTO` on siin projektis selge API lepingukiht, mis hoiab request/response mudelid eraldi nii domeenist kui ka BLL ariloogikast. Koige tugevamad kohad on versioonitud struktuur, lihtne valideerimine ja nested response mudelite kasutamine keerukamate koondvaadete jaoks.

Koige olulisem opikoht on see, et DTO ei ole lihtsalt "suvaline klass", vaid teadlik piir valise HTTP maailma ja sisemise rakenduse vahel.
