# Dental Clinic Platform (SaaS) - Assignment 18

## Projekti eesmärk

See lahendus on multi-tenant SaaS platvorm hambakliinikutele. Iga kliinik töötab samas rakenduses, kuid omaette tenant-kontekstis, kus hallatakse patsiente, raviplaane, visiite, ressursse, kindlustust ja arveldust.

## Mis on praegu implementeeritud

### Autentimine ja tenant-kontekst

- ASP.NET Core Identity + JWT
- `register`, `login`, `forgot-password`, `reset-password`, `renew-refresh-token`, `logout`
- `switch-company` ja `switch-role` mitme liikmesuse korral
- path-based tenant resolution kujul `/api/v1/{companySlug}/...`
- tenant query filtrid `AppDbContext` tasemel
- soft delete tenant-entiteetidel
- audit log `SaveChangesAsync` tasemel

### System / backoffice funktsioonid

- tenant onboarding koos `Company`, `CompanySettings`, `Subscription` ja owner-membershipiga
- SystemAdmin impersonation koos põhjuse, JWT claimide ja audit log kirjega
- platform analytics, feature flagid ja company activation
- support vaated: company snapshotid ja lihtsad support ticketid
- billing vaated: subscriptionite ja invoice staatuste haldus

### Tenant funktsioonid

- patsiendid: list, detail, profiil, create, update, delete
- hambakaart: tooth record list, upsert, delete
- röntgeni metaandmed: list, create, delete
- ressursid: dentists ja treatment rooms CRUD
- treatment types CRUD
- appointments: list, create, clinical record
- treatment plans: list, detail, create, update, submit, delete, open items, item decision
- insurance plans CRUD
- patient insurance policies CRUD
- cost estimates: list, create, legal preview
- invoices: list, detail, create, generate from procedures, update, delete, add payment
- payment plans CRUD
- finance workspace patsiendi lõikes
- company users list/upsert
- company settings get/update
- tenant subscription get/update

### UI ja infrastruktuur

- staatiline demo-UI `wwwroot` all, route'idega `/app/*`
- Finance workspace demo-UI-s saab patsiendi kontekstis nüüd koostada raviplaani drafti, selle submit'ida ja sama vaate sees patsiendi otsuseid värskendada
- Appointmentide `Record worked teeth` vorm sünkroniseerib end raviplaani otsustega ning lubab valida sama patsiendi accepted/deferred plan item'e
- Resources vaates on demo-UI-s nüüd eraldi treatment type catalog, mida kasutavad raviplaanid, appointment clinical entries ja finance hinnastamine
- browser refresh tugi `MapFallbackToFile("/app/{*path:nonfile}", "index.html")`
- global exception middleware + `ProblemDetails`
- Swagger + API versioning
- Docker Compose + PowerShell skriptid lokaalseks käivituseks
- monorepo GitLab CI/CD paigutus, kus assignmenti pipeline elab assignmenti kaustas ja root pipeline orkestreerib seda
- unit ja integration testid

## Tehnoloogiad

- .NET SDK `10.0.102`
- `net10.0`
- ASP.NET Core Web API
- ASP.NET Core Identity
- EF Core 10 + Npgsql
- PostgreSQL
- xUnit + `Microsoft.AspNetCore.Mvc.Testing` + EF InMemory testides

## Projekti struktuur

```text
assignment-18-dental-clinic-platform/
  dental-clinic-platform.slnx
  src/
    App.Domain/
    App.DAL.EF/
    App.BLL/
    App.DTO/
    WebApp/
  tests/
    WebApp.Tests/
  docs/
    architecture.md
    data-model.md
    api.md
    testing.md
    app-domain-guide.md
    app-dal-ef-guide.md
    app-bll-study-guide.md
    app-dto-guide.md
    ai-usage.md
```

## Lokaalne käivitus

### Eeldused

- .NET 10 SDK
- Docker Desktop või kohalik PostgreSQL
- soovituslikult `dotnet-ef`

```powershell
dotnet tool update -g dotnet-ef
```

### Konfiguratsioon

Sea `WebApp` projektile vajalikud saladused:

```powershell
cd src/WebApp
dotnet user-secrets set "JWT:Key" "YOUR_LONG_RANDOM_SECRET_KEY_MIN_64_CHARS"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1;Port=5432;Database=dental_saas;Username=postgres;Password=postgres"
```

### Kiire käivitus

Lahenduse juurkaustast:

```powershell
.\scripts\start-app.ps1
```

See skript:

- käivitab `docker-compose.yml` abil PostgreSQL-i
- ootab, kuni andmebaas on kuuldel
- käivitab `src/WebApp` projekti

Peatamine:

```powershell
.\scripts\stop-db.ps1
```

Koos volume eemaldamisega:

```powershell
.\scripts\stop-db.ps1 -RemoveVolume
```

Migratsioonid:

```powershell
.\scripts\migrate-db.ps1
```

### Käivitamine ilma skriptita

```powershell
docker compose up -d postgres
dotnet run --project src/WebApp
```

Liveness / smoke-check endpoint:

- `http://localhost:5107/health`
- `https://localhost:7245/health`

## CI/CD ja deployment

- monorepo root `.gitlab-ci.yml` ainult include'ib selle assignmenti pipeline'i
- assignmenti-spetsiifiline GitLab CI fail asub `assignment-18-dental-clinic-platform/.gitlab-ci.yml`
- `Dockerfile` jääb assignmenti juurkausta, sest build context on kogu assignment
- `docker-compose.yml` on lokaalseks arenduseks
- `docker-compose.prod.yml` on VPS deploy jaoks
- `scripts/deploy.sh` on Linux/VPS deploy entrypoint GitLab deploy jobile

Runneri hosti konfiguratsioon (`config.toml`, registration tokenid, SSH võtmed) ei kuulu reposse. Runner tagide ja monorepo paigutuse detailid on kirjas [docs/ci-cd.md](../../../docs/ci-cd.md).

Praegune GitLab pipeline eeldab, et sinu projektirunner kasutab tagi `shared`.

Production deploy eeldab vähemalt neid keskkonnamuutujaid:

- `JWT__Key`
- `CORS_ALLOWED_ORIGIN`
- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `WEBAPP_PORT`

`CORS_ALLOWED_ORIGIN` peab viitama sinu proxy või deploy URL-ile. Praegune vaikimisi production väärtus on `https://mtiker-cweb-a3.proxy.itcollege.ee`.

Käsitsi deploy kontroll VPS-is:

```bash
export JWT__Key="replace-with-long-random-secret"
export CORS_ALLOWED_ORIGIN="https://mtiker-cweb-a3.proxy.itcollege.ee"
docker compose -f docker-compose.prod.yml up -d --build
curl http://127.0.0.1/health
```

Vaikimisi launch profile'id:

- HTTP: `http://localhost:5107`
- HTTPS: `https://localhost:7245`
- Swagger: `https://localhost:7245/swagger`
- Health: `https://localhost:7245/health`

## Seed kasutajad

### System kasutajad

- `sysadmin@dental-saas.local` / `Dental.Saas.101`
- `support@dental-saas.local` / `Dental.Saas.101`
- `billing@dental-saas.local` / `Dental.Saas.101`

### Demo tenantid

- slugid: `smileworks-demo`, `nordic-smiles-demo`
- kõigil demo kasutajatel parool: `Dental.Saas.101`
- `owner.demo@dental-saas.local`
- `admin.demo@dental-saas.local`
- `manager.demo@dental-saas.local`
- `employee.demo@dental-saas.local`
- `multitenant.demo@dental-saas.local`

`multitenant.demo@dental-saas.local` on mõeldud `switch-company` ja `switch-role` voogude testimiseks.

## UI route'id

Peamised UI vaated on `/app/*` all:

- `SystemAdmin` -> `/app/platform`
- `SystemSupport` -> `/app/support`
- `SystemBilling` -> `/app/billing`
- `CompanyOwner` -> `/app/team`
- `CompanyAdmin` -> `/app/team`
- `CompanyManager` -> `/app/finance`
- `CompanyEmployee` -> `/app/appointments`
- fallback -> `/app/overview`

Märkus:

- vana alias `/app/plans` suunatakse samale finance/plans vaatele

## Rollid lühidalt

- `SystemAdmin`: platform, support, billing, onboarding, impersonation
- `SystemSupport`: support ja onboarding
- `SystemBilling`: billing vaated
- `CompanyOwner`: kõik tenant funktsioonid, sh settings ja subscription tier
- `CompanyAdmin`: enamik tenant haldusvooge, kuid mitte owner-only settings/subscription update
- `CompanyManager`: operatiivsed kliinilised ja finantsvood
- `CompanyEmployee`: patients, resources read, appointments, tooth records, xrays, finance workspace read

## Testid

```powershell
dotnet test dental-clinic-platform.slnx
```

Praegune testikomplekt katab:

- deployment smoke endpointi `/health`
- auth ja onboarding integration flow'd
- tenant patient/appointment HTTP vood
- impersonation flow
- patient, appointment, treatment plan ja finance teenuste unit testid
- tenant API controllerite unit testid

Detailsem ülevaade: [docs/testing.md](docs/testing.md)

## Dokumentatsioon

- arhitektuur: [docs/architecture.md](docs/architecture.md)
- andmemudel: [docs/data-model.md](docs/data-model.md)
- API ülevaade: [docs/api.md](docs/api.md)
- testimine: [docs/testing.md](docs/testing.md)
- `App.Domain` õppematerjal: [docs/app-domain-guide.md](docs/app-domain-guide.md)
- `App.DAL.EF` õppematerjal: [docs/app-dal-ef-guide.md](docs/app-dal-ef-guide.md)
- `App.BLL` õppematerjal: [docs/app-bll-study-guide.md](docs/app-bll-study-guide.md)
- `App.DTO` õppematerjal: [docs/app-dto-guide.md](docs/app-dto-guide.md)
- AI kasutuse logi: [docs/ai-usage.md](docs/ai-usage.md)
- monorepo CI/CD juhend: [../../../docs/ci-cd.md](../../../docs/ci-cd.md)

## Turvalisus

- JWT + role-based authorization
- tenant isolation query filtritega
- request route tenant peab klappima aktiivse tenant-kontekstiga
- soft delete tenant andmetel
- audit log muudatuste jälgimiseks
- password policy (`8+`, upper/lower/digit/special)

## Olulised piirangud

- X-ray osa haldab praegu metaandmeid, mitte failide päris storage/workflow'd
- support ticketid on lihtne audit-log põhine lahendus, mitte eraldi ticketing süsteem
- UI on demo/admin console stiilis kliendirakendus `wwwroot` all, mitte eraldi frontend projekt
