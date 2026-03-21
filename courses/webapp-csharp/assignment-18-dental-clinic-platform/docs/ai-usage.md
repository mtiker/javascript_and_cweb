# AI Usage Log

## Eesmärk

AI-d kasutati:

- nõuete analüüsiks ja arhitektuuri valikuks
- C#/.NET skeletoni loomiseks
- tenant-isolatsiooni, authi, middleware ja teenuseloogika implementatsiooniks
- testide lisamiseks
- dokumentatsiooni koostamiseks
- jätkuiteratsioonides patient/appointment voogude ja impersonation flow lisamiseks

## Töövoo logi (kronoloogiline)

- 2026-03-21 / deployment readiness review
  - Paluti: kontrollida assignmenti deploy-valmidus üle loengumaterjali põhjal ning parandada puuduvad Docker/production detailid.
  - Tulemus: lisati `.dockerignore`, ASP.NET Core `/health` endpoint, deployment smoke test, production CORS muutuja nõue ja täiendatud deploy script `--remove-orphans` + stabiilse compose project name toega.
  - Käsitsi muudatused: kontrolliti, et production CORS nõue oleks dokumentatsioonis, compose failis ja deploy skriptis ühtemoodi kirjas.

- 2026-03-21 / production proxy CORS default
  - Paluti: panna production CORS vaikimisi väärtuseks päris proxy host `mtiker-cweb-a3.proxy.itcollege.ee`.
  - Tulemus: `docker-compose.prod.yml` kasutab nüüd vaikimisi `https://mtiker-cweb-a3.proxy.itcollege.ee`, deploy skript ei nõua enam eraldi CORS muutujat ning README/CI-CD docs viitavad samale aadressile.
  - Käsitsi muudatused: säilitati võimalus `CORS_ALLOWED_ORIGIN` vajadusel keskkonnamuutujaga override'ida.

- 2026-03-21 / shared runner tag alignment
  - Paluti: muuta Assignment 18 pipeline kasutama tegelikku GitLab runneri tagi `shared`.
  - Tulemus: kõik Assignment 18 CI/CD jobid kasutavad nüüd tagi `shared` ning README/monorepo CI-CD juhend kirjeldavad sama runneri mudelit.
  - Käsitsi muudatused: kontrolliti GitLabis nähtavat runneri tagi ja viidi dokumentatsioon päriskeskkonnaga kooskõlla.

- 2026-03-04 / FAAS 1
  - Paluti: analüüs + arhitektuur + andmemudel + endpoint disain.
  - Tulemus: nõuete kaart, küsimused/eeldused, architecture/data/api plaan.
  - Käsitsi muudatused: nõuete prioriteetide kinnitamine (kasutada soovitatud variante).

- 2026-03-04 / FAAS 2
  - Paluti: skeleton-koodi genereerimine.
  - Tulemus: layered lahendus (`Domain/DAL/BLL/DTO/WebApp/Tests`), Identity+JWT, tenant middleware, onboarding + treatment decision use-case.
  - Käsitsi muudatused: kompileerimisvigade parandused (OpenAPI API muutused, EF extension usingud, DTO sõltuvuste korrigeerimine).

- 2026-03-04 / FAAS 3
  - Paluti: testid + docs + traceability kontroll.
  - Tulemus: 5 testi (3 unit + 2 integration), README + docs/*.md + AI usage log.
  - Käsitsi muudatused: integration factory DB provider override parandamine (Npgsql/InMemory konflikt + püsiv DB nimi testisessioonis).

- 2026-03-04 / jätkuiteratsioon A
  - Paluti: `JÄTKA`.
  - Tulemus: lisati Patient CRUD endpointid, Appointment list/create endpointid, overlap-kontroll ja lisatestid.
  - Käsitsi muudatused: dokumentatsiooni täpsustused ning testikatvuse uuendus 7 testini.

- 2026-03-04 / jätkuiteratsioon B
  - Paluti: `JÄTKA`.
  - Tulemus: lisati SystemAdmin impersonation endpoint, BLL context resolver, JWT impersonation claimid, audit log kirje ja integration test.
  - Käsitsi muudatused: kihipiiride korrigeerimine (BLL ei sõltu WebApp kihist).

- 2026-03-05 / käivituse stabiilsuse parandus
  - Paluti: parandada käivitus, kuna rakendus ei saa ühendust `127.0.0.1:5432` PostgreSQL-ga.
  - Tulemus: lisati `docker-compose.yml` + PowerShell skriptid (`start-db`, `start-app`, `migrate-db`, `stop-db`), vähendati EF debug-müra ja lisati tundliku logimise konfi lülitid.
  - Käsitsi muudatused: DB init loogikasse lisati fallback `EnsureCreated`, kui migratsioonifaile pole veel genereeritud.

- 2026-03-05 / migratsioonide hardening
  - Paluti: kasutada production/SaaS varianti; lisada päris EF migratsioonid ja eemaldada `EnsureCreated` fallback.
  - Tulemus: lisati `InitialCreate` migratsioon + snapshot, loodi design-time `AppDbContext` factory ning `MigrateDatabase` kasutab ainult `context.Database.Migrate()`.
  - Käsitsi muudatused: käivitusskript täiendati nii, et proovib Docker Desktopi käivitada ja ootab engine valmimist.

- 2026-03-05 / esitluse UI
  - Paluti: teha korralik veebiliides esitamiseks, sarnaselt eksamiprojektile.
  - Tulemus: lisati `wwwroot` põhine UI (`index.html`, `css/app.css`, `js/app.js`) onboarding/login/switch-company/patient CRUD voogudega.
  - Käsitsi muudatused: middleware täiendati `UseDefaultFiles()`-ga, et `/` avaks kohe UI.

- 2026-03-20 / App.DTO oppematerjal
  - Paluti: teha `App.DTO` kohta juhend samas stiilis nagu olemasolevad `App.Domain`, `App.DAL.EF` ja `App.BLL` materjalid.
  - Tulemus: lisati `docs/app-dto-guide.md` ning README dokumentatsiooni loendisse viide uuele juhendile.
  - Kasitsi muudatused: kontrolliti juhendi sisu vastu tegelikku `App.DTO` projekti struktuuri, controllerite kasutust ja valideerimismustreid.

- 2026-03-20 / finance UI raviplaani builder
  - Paluti: lisada demo-UI-sse patsiendile treatment plani loomine ning parandada Finance vaate `Refresh decisions` viga.
  - Tulemus: lisati Finance workspace'i raviplaani drafti koostamise vorm mitme itemi toega, ühendati see olemasoleva `treatmentplans` create endpointiga ning muudeti decisions refresh nii, et see sünkroniseerib ka sama patsiendi finance workspace'i.
  - Kasitsi muudatused: muudatus kontrolliti `node --check`, `dotnet build` ja `dotnet test dental-clinic-platform.slnx --no-build` abil.

- 2026-03-20 / invoice payment flow hardening
  - Paluti: parandada olukord, kus invoice payment submit ei töötanud usaldusväärselt.
  - Tulemus: finance UI hakkab valitud invoice id-d salvestama kohe invoice klikil ning payment submit kasutab detailvaate või state'i kinnitatud invoice konteksti.
  - Kasitsi muudatused: kontrolliti JavaScripti süntaksit ning jooksutati uuesti build/testid.

- 2026-03-20 / treatment type UI resources vaates
  - Paluti: lahendada olukord, kus treatment plan itemite loomiseks vajalikud treatment type flow'd olid backendis olemas, kuid demo-UI-s puudusid.
  - Tulemus: Resources vaates lisati treatment type create/edit/delete vorm ja tabel ning need seoti appointment/finance valikute sünkroniseerimisega.
  - Kasitsi muudatused: kontrolliti frontend wiring üle ning verifitseeriti muudatus build/testidega.

- 2026-03-20 / refresh decisions EF translation fix
  - Paluti: parandada `Refresh decisions` viga, kus EF Core ei suutnud `OpenPlanItemResult.PatientName` järgi sortivat LINQ päringut SQL-i tõlkida.
  - Tulemus: avatud plan itemite sortimine viidi DTO/record projektsioonist ettepoole anonüümse SQL-transleeritava kujuni ning `OpenPlanItemResult` mapitakse nüüd pärast `ToListAsync` in-memory.
  - Kasitsi muudatused: unit testid jooksid edukalt; täis build oli lokaalselt blokeeritud, sest `WebApp` protsess hoidis assembly't lukus.

- 2026-03-20 / record work plan item selection sync
  - Paluti: teha nii, et appointmentide `Record worked teeth` vormis saaks valida raviplaani plan item'i ka pärast patsiendi otsuse salvestamist.
  - Tulemus: `Refresh decisions` voog värskendab nüüd lisaks consent queue'le ja finance workspace'ile ka `treatmentPlans` state'i, mille pealt `Record worked teeth` plan item dropdown ehitatakse; accepted/deferred filter muudeti case-insensitive'iks.
  - Kasitsi muudatused: kontrolliti muudetud `app-finance.js` faili süntaksit `node --check` abil.

## Promptide logi (kokkuvõte)

- Master prompt: ehitada production-ready SaaS C#/.NET lahendus, järgides näidisprojekti stiili, loengumaterjale ja antud väljundstruktuuri.
  - Mõju: kogu lahenduse struktuur ja faaside töövoog.

- Alam-prompt: laienda küsimusi ja anna arhitektuuriline hinnang (turvalisus/funktsionaalsus/laienemine).
  - Mõju: valiti shared schema + tugev tenant isolation, põhjendatud eeldused.

- Alam-prompt: “kasuta enda antud soovitusi, JÄTKA”.
  - Mõju: kinnitati tehniline suund ja liiguti koodi implementatsiooni.

- Alam-prompt: korduvad “JÄTKA” käsud.
  - Mõju: lisati järgmised funktsionaalsed viilud (Patient/Appointment, Impersonation).

- Alam-prompt: lisada UI-sse raviplaani loomine ja parandada refresh behavior Finance vaates.
  - Mõju: valmis raviaplaani builder ning värskenduse loogika muudeti sama patsiendi konteksti suhtes järjepidevaks.

## Otsuste logi

1. Shared schema multi-tenancy (`CompanyId`) query filtritega.
- Põhjus: kiire MVP + kontrollitav isolatsioon.

2. Tenant resolution middleware path segmentist.
- Põhjus: vastab nõudele `/{companySlug}`.

3. Identity + JWT auth API jaoks.
- Põhjus: rollipõhine autoriseerimine ja mitme ettevõtte kasutaja toetus.

4. Soft delete tenant-entiteetidele.
- Põhjus: andmete säilitamine ja audit.

5. Audit log SaveChanges tasemel.
- Põhjus: järjepidev muutuste logimine.

6. Onboarding kui eraldi BLL use-case.
- Põhjus: tenanti loomine on kriitiline ärivoog.

7. Treatment plan decision kui eraldi BLL use-case.
- Põhjus: kliiniline workflow nõuab ühtset äriloogika kontrolli.

8. Integration testides EF InMemory custom factoryga.
- Põhjus: kiire ja sõltumatu HTTP taseme verifitseerimine.

9. Patient CRUD teenus eraldi BLL kihis.
- Põhjus: tenant-äriloogika hoidmine controlleritest väljas.

10. Appointment overlap kontroll teenusekihis.
- Põhjus: kriitiline ajaplaneerimise terviklusreegel.

11. Impersonation ainult SystemAdmin rollile koos reason + audit kirjega.
- Põhjus: kõrgendatud turvariski kontroll ja läbipaistvus.

12. Arenduskeskkonna DB käivitus Docker Compose kaudu koos skriptidega.
- Põhjus: vähendab setup-vigu ja tagab korratava lokaalset käivituse.

13. Sensitive data logging eraldi konfiga juhitavaks (vaikimisi false).
- Põhjus: turvalisem vaikeseade ja väiksem PII lekkimise risk logides.

14. Päris EF migratsioonid kohustuslikuks; `EnsureCreated` fallback eemaldatud.
- Põhjus: production andmeskeemi evolutsioon peab käima ainult migratsioonidega.

15. API-first backendile lisati eraldi demo-web UI `wwwroot` all.
- Põhjus: hindamisel ja esitlusel on vaja kohe kasutatavat brauseri vaadet, mitte ainult Swaggerit.

16. Finance workspace seoti raviplaani loomisega otse UI tasemel.
- Põhjus: olemasolev backend toetas create flow'd, kuid demo-UI ei võimaldanud seda patsiendi töövoos kasutada.

17. Plan decision refresh värskendab nüüd lisaks consent queue'le ka valitud patsiendi finance workspace'i.
- Põhjus: sama vaate andmed peavad pärast plaani submit'i või otsuse muutmist jääma sünkrooni ega tohi jätta kasutajat vigase või vananenud seisuga.

18. Plan decision refresh värskendab nüüd ka appointment clinical vormi jaoks kasutatavat treatment plan state'i.
- Põhjus: `Record worked teeth` dropdown peab kohe pärast patsiendi otsust näitama valitavaid accepted/deferred plan item'e, ilma et kasutaja peaks eraldi täisvärskendust tegema.

## Riskid ja kontroll

- Risk: tenant lekked valesti filtreeritud päringute tõttu.
  - Kontroll: globaalne query filter + integration testid.

- Risk: auth/token voogude regressioon.
  - Kontroll: register/login integration test.

- Risk: äriloogika regressioon raviplaani staatuse arvutuses.
  - Kontroll: unit testid.

- Risk: appointment topeltbroneeringud.
  - Kontroll: unit test overlap juhule + integration flow.

- Risk: impersonation väärkasutus.
  - Kontroll: ainult `SystemAdmin`, kohustuslik reason, audit log entry, integration test.

## Autorlus / aususe märge

Lõplik tehniline vastutus, kontroll ja valideerimine jääb arendajale. AI oli abivahend analüüsi, skeletoni, testide ja dokumentatsiooni kiirendamiseks.
