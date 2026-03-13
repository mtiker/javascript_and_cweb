# Dental Clinic Platform (SaaS) - Assignment 18

## Projekti eesmärk

See lahendus on multi-tenant SaaS veebiplatvorm hambakliinikutele. Iga kliinik (tenant) haldab enda patsiente, raviplaane, visiitide ajastust, kindlustust ja arveldust isoleeritud andmeruumis.

## Funktsionaalsus (hetkel implementeeritud)

- ASP.NET Core Identity + JWT autentimine (`register`, `login`, `refresh`, `logout`, `switch-company`)
- Password reset voog (`forgot-password`, `reset-password`)
- Tenant onboarding (`register-company`) koos:
  - `Company`
  - `CompanySettings`
  - `Subscription` (Free)
  - `CompanyOwner` rolliside
- Path-based tenant resolution middleware (`/api/v1/{companySlug}/...`)
- Tenant-isolatsioon DbContext query filtritega
- Soft delete + audit log SaveChanges tasemel
- Treatment plan item decision use-case (`Accepted/Deferred/...`) koos plaani staatuse uuendamisega
- Patient CRUD endpointid (`list/get/create/update/delete`)
- Appointment endpointid (`list/create`) koos konfliktikontrolliga (dentist + room overlap)
- Company user management endpointid (`list/upsert`) owner/admin rollidele
- Company settings endpointid (`get/update`) owner rollile
- Dentists endpointid (`list/create`) + Treatment rooms endpointid (`list/create`)
- Treatment plan pending items endpoint (`open-items`) otsuse workflow toetamiseks
- SystemAdmin impersonation endpoint (`/system/impersonation/start`) koos reason nõudega ja audit log kirjega
- Global exception middleware + `ProblemDetails`
- Swagger + API versioning
- Unit + integration testid

## Tehnoloogiad ja versioonid

- .NET SDK: 10.0.102
- Target framework: `net10.0`
- C#: latest (SDK default)
- ASP.NET Core Web API + MVC/Identity UI
- EF Core 10 + Npgsql provider
- PostgreSQL (runtime)
- xUnit + `Microsoft.AspNetCore.Mvc.Testing` + EF InMemory (testid)

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
    ai-usage.md
```

## Lokaalne käivitus

### Eeldused

- .NET 10 SDK
- Docker Desktop (soovituslik) voi kohalik PostgreSQL
- (soovituslik) `dotnet-ef` tööriist

```powershell
dotnet tool update -g dotnet-ef
```

### Konfiguratsioon

1. Mine WebApp projekti:

```powershell
cd src/WebApp
```

2. Sea salajane JWT võti user-secrets'i (ära hoia päris võtmeid `appsettings.json` sees):

```powershell
dotnet user-secrets set "JWT:Key" "YOUR_LONG_RANDOM_SECRET_KEY_MIN_64_CHARS"
```

3. Sea ühendusstring (kui ei kasuta vaikimisi):

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=127.0.0.1;Port=5432;Database=dental_saas;Username=postgres;Password=postgres"
```

### Soovitatud kiire kaivitus (Docker + skript)

Lahenduse juurkaustast:

```powershell
.\scripts\start-app.ps1
```

Kui Docker Desktop ei jookse, annab skript kohese vea:
`Docker engine is not running and Docker Desktop was not found. Install/start Docker Desktop or start local PostgreSQL.`

Kui Docker Desktop on paigaldatud, proovib skript selle ise kaivitada.
Esmakordsel kaivitusel voib Docker engine valmimine votta kuni umbes 5 minutit.

See teeb:

- kaivitab `docker-compose.yml` alusel PostgreSQL-i
- ootab, kuni `127.0.0.1:5432` on kuulatav
- kaivitab WebApp-i

Peatamine:

```powershell
.\scripts\stop-db.ps1
```

Koos andmemahu kustutamisega:

```powershell
.\scripts\stop-db.ps1 -RemoveVolume
```

### Migratsioonid

Kui kasutad EF migratsioone:

```powershell
.\scripts\migrate-db.ps1
```

### Kaivitamine ilma skriptita

```powershell
docker compose up -d postgres
dotnet run --project src/WebApp
```

Swagger: `https://localhost:5001/swagger`

Demo UI: `http://localhost:5107/`

### Testid

```powershell
dotnet test dental-clinic-platform.slnx
```

## Keskkonnad

- `Development`
  - `EnableSensitiveDataLogging` sisse lülitatud
  - detailsem logimine
- `Test`
  - integration testides EF InMemory
- `Production`
  - `UseExceptionHandler` + HSTS
  - secrets läbi env vars / secret manager

## Andmemudel

Lühikirjeldus ja ERD: vaata [docs/data-model.md](docs/data-model.md)

## API dokumentatsioon

Detailid: [docs/api.md](docs/api.md)

Peamised route'id:

- `POST /api/v1/account/register`
- `POST /api/v1/account/login`
- `POST /api/v1/account/forgotpassword`
- `POST /api/v1/account/resetpassword`
- `POST /api/v1/account/renewrefreshtoken`
- `POST /api/v1/account/logout`
- `POST /api/v1/account/switchcompany`
- `POST /api/v1/system/onboarding/registercompany`
- `GET /api/v1/system/onboarding/companies`
- `POST /api/v1/system/impersonation/start`
- `POST /api/v1/{companySlug}/treatmentplans/recorditemdecision`
- `GET /api/v1/{companySlug}/patients`
- `GET /api/v1/{companySlug}/patients/{patientId}`
- `POST /api/v1/{companySlug}/patients`
- `PUT /api/v1/{companySlug}/patients/{patientId}`
- `DELETE /api/v1/{companySlug}/patients/{patientId}`
- `GET /api/v1/{companySlug}/dentists`
- `POST /api/v1/{companySlug}/dentists`
- `GET /api/v1/{companySlug}/treatmentrooms`
- `POST /api/v1/{companySlug}/treatmentrooms`
- `GET /api/v1/{companySlug}/appointments`
- `POST /api/v1/{companySlug}/appointments`
- `GET /api/v1/{companySlug}/treatmentplans/openitems`
- `GET /api/v1/{companySlug}/companyusers`
- `POST /api/v1/{companySlug}/companyusers`
- `GET /api/v1/{companySlug}/companysettings`
- `PUT /api/v1/{companySlug}/companysettings`

## Esitlusvoog (UI)

1. Ava `http://localhost:5107/`
2. Kasuta **Onboarding** vormi, et luua uus kliinik + owner kasutaja
3. Logi sisse **Login** vormis
4. Vajadusel vali tenant **Company Switch** vormiga
5. Lisa kliiniku ressursid (**Dentists and Rooms**) resources ekraanil
6. Lisa ja kuva patsiente **Patients** sektsioonis
7. Loo vastuvõtte **Schedule** sektsioonis
8. Salvesta raviotsuseid **Plans** sektsioonis

## Veahaldus standard

Rakendus tagastab vigadel `application/problem+json` payloadi (global middleware).

Näide:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation failed",
  "status": 400,
  "detail": "Company slug is already in use.",
  "traceId": "0HND..."
}
```

## Turvalisus

- Identity + JWT
- rollipõhine `[Authorize(Roles=...)]`
- tenant isolation query filtritega
- soft delete tenant-entiteetidel
- audit log muutuste jaoks
- paroolipoliitika (`8+`, upper/lower/digit/special)
- logidesse ei kirjutata paroole

## Deploy ülevaade

Kohalikuks arenduseks on kaasas `docker-compose.yml` (PostgreSQL). Soovituslik minimaalne CI:

1. `dotnet restore`
2. `dotnet build --no-restore`
3. `dotnet test --no-build`
4. migratsiooni kontroll (`dotnet ef migrations script`)

## Known limitations / järgmised sammud

- CRUD endpointid kõigile dental entiteetidele pole veel lisatud (Patient valmis, teised osaliselt)
- country-specific insurance formaadid (sh DE legal output) on planeeritud, mitte lõplikult implementeeritud
- rate limiting ja CSP policy vajavad production-level täiendamist
