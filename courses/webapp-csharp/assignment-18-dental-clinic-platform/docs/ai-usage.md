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

## Promptide logi (kokkuvõte)

- Master prompt: ehitada production-ready SaaS C#/.NET lahendus, järgides näidisprojekti stiili, loengumaterjale ja antud väljundstruktuuri.
  - Mõju: kogu lahenduse struktuur ja faaside töövoog.

- Alam-prompt: laienda küsimusi ja anna arhitektuuriline hinnang (turvalisus/funktsionaalsus/laienemine).
  - Mõju: valiti shared schema + tugev tenant isolation, põhjendatud eeldused.

- Alam-prompt: “kasuta enda antud soovitusi, JÄTKA”.
  - Mõju: kinnitati tehniline suund ja liiguti koodi implementatsiooni.

- Alam-prompt: korduvad “JÄTKA” käsud.
  - Mõju: lisati järgmised funktsionaalsed viilud (Patient/Appointment, Impersonation).

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
