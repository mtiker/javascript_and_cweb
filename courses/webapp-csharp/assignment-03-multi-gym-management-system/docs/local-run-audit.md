# Local Run Audit

Audited: 2026-04-27

## Status summary

| Step | Result |
|---|---|
| `dotnet build` | Pass — 0 errors, 0 warnings |
| `dotnet test` | Pass — 67 passed, 3 skipped (opt-in Docker gate), 0 failed |
| `npm test` | Pass — 25 passed, 7 test files |
| `npm run build` | Pass — built in ~2 s |
| `scripts/start-db.ps1` | No issues — paths correct, idempotent |
| `scripts/migrate-db.ps1` | No issues — calls start-db then EF update |
| `scripts/start-app.ps1` | No issues — calls start-db then dotnet run |
| Health endpoint (`/health`) | Mapped |
| Swagger | Always on — `/swagger` |

No blocking issues found.

---

## Prerequisites

| Tool | Version required | Notes |
|---|---|---|
| .NET SDK | 10.0 | `net10.0` target |
| Node.js | 20 | Docker image `node:20-alpine`; local npm test also needs 20 |
| Docker Desktop | any current | Used by `start-db.ps1` to run PostgreSQL |
| `dotnet-ef` tool | 9+ | For manual EF migrations (`dotnet tool install -g dotnet-ef`) |

---

## Build

```
dotnet build multi-gym-management-system.slnx
```

Projects built (in order): `App.Domain`, `App.Resources`, `App.DTO`, `App.BLL`, `App.DAL.EF`, `WebApp`, `WebApp.Tests`.

---

## Tests

### .NET

```
dotnet test multi-gym-management-system.slnx
```

- 67 passed, 3 skipped, 0 failed.
- The 3 skipped tests are `PostgreSqlPersistenceTests` decorated with `[RequiresDockerFact]`. They use Testcontainers to spin up a real PostgreSQL container. To run them, set the environment variable:

```
$env:RUN_POSTGRES_TESTS = "1"
dotnet test multi-gym-management-system.slnx
```

### Client

```
cd client
npm test
```

25 tests across 7 files. Requires Node 20 and `node_modules` already present (`npm ci` if first run).

---

## Client production build

```
cd client
npm run build
```

Runs `tsc --noEmit` (type check) then `vite build`. Output lands in `client/dist/`.

---

## Start database

```powershell
.\scripts\start-db.ps1
```

- Checks whether port 5432 is already listening; if so, exits early (idempotent).
- If Docker engine is not running, starts Docker Desktop and waits up to 300 s.
- Runs `docker compose up -d postgres` from the repo root.
- Waits up to 60 s for port 5432 to be listening.

PostgreSQL credentials (dev): `postgres` / `postgres`, database `multi_gym_management_system`, port 5432.

---

## Run database migrations manually

```powershell
.\scripts\migrate-db.ps1
```

Calls `start-db.ps1` first, then:

```
dotnet ef database update --project src/App.DAL.EF --startup-project src/WebApp
```

Auto-migration also runs at application startup when `DataInitialization:MigrateDatabase` is `true` (default).

---

## Start the application

```powershell
.\scripts\start-app.ps1
```

- Calls `start-db.ps1` to ensure PostgreSQL is running.
- Runs `dotnet run --project src/WebApp`.
- Accepts `-NoBuild` switch to skip compilation.

Default listening URL (Kestrel default): `http://localhost:5000` / `https://localhost:5001`.

After startup the following endpoints are reachable:

| Endpoint | URL |
|---|---|
| Health check | `GET /health` |
| Swagger UI | `GET /swagger` |
| API (versioned) | `/api/v1/...` |
| MVC admin area | `/admin/...` |
| React client | `/client` |

---

## Stop database

```powershell
.\scripts\stop-db.ps1            # keep data volume
.\scripts\stop-db.ps1 -RemoveVolume  # destroy data volume
```

---

## Blocking issues found

None.
