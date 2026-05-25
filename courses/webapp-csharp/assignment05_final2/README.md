Configured public URL: `https://mtiker-cweb-4.proxy.itcollege.ee`

Do not claim the public deployment as live for defense. The 2026-05-25 Phase
14 smoke attempt reached the public host, but `/health` returned HTTP 404, so
the current build is not verified as deployed. A local Docker production-stack
smoke with backend, PostgreSQL, and standalone client containers does pass.

# Assignment 05 Final2 - Multi-Gym Management System (Modular Monolith)

This is the **Final2** evolution of the Final1 Multi-Gym Management System.
The Final1 source remains untouched at `courses/webapp-csharp/assignment-03-multi-gym-management-system`; Final2 lives in this folder (`courses/webapp-csharp/assignment05_final2`) and will be refactored into a modular monolith across the phases described in [`assignment05_final2_codex_prompts.md`](assignment05_final2_codex_prompts.md). Track phase progress in [`PHASE_STATUS.md`](PHASE_STATUS.md).

Phase 0 (baseline copy) is complete. All subsequent modularization happens here, not in Final1.

Assignment 03 Final1 is implemented as a SaaS multi-gym management system under `courses/webapp-csharp/assignment-03-multi-gym-management-system`. The earlier module architecture has been removed from the active codebase and is not part of the current Final1 structure.

The project now has three user-facing surfaces:
- MVC admin UX inside `WebApp/Areas/Admin`
- MVC client UX inside `WebApp/Areas/Client`, served under `/mvc-client`
- a separate React + TypeScript SaaS client under `client/`

Admin route note:
- `/Admin/Gyms`, `/Admin/Members`, `/Admin/Memberships`, `/Admin/Sessions`, `/Admin/Operations`, `/Admin/TrainingCategories`, and `/Admin/MembershipPackages` render focused MVC pages backed by strongly typed view models.
- MVC Admin now has full create/update/delete form flows for members, training categories, and membership packages. The broader admin dashboards and operational pages remain focused read/action surfaces.

The backend remains one ASP.NET Core host that serves the MVC areas, Swagger, the versioned REST API, and the production React client at `/client`. The MVC client area uses `/mvc-client` so it does not collide with the React client mount.

## Requirement Coverage

This assignment currently covers:
- multi-tenant gym SaaS domain design with more than 10 meaningful entities
- versioned REST API controllers under `/api/v1/...`
- public DTOs in `Shared.Contracts/Dtos/v1`
- Swagger
- JWT authentication with refresh-token rotation
- MVC admin UX, including tenant-scoped CRUD for members, training categories, and membership packages
- MVC client UX
- MVC Admin compliance evidence for role access, strongly typed view models, no `ViewBag`/`ViewData`, tenant CRUD authorization, and anti-forgery regression coverage
- a separate React client that uses the REST API with JWT + refresh tokens
- React client for members, sessions/bookings, memberships/packages, payments, equipment maintenance, and member workspace flows
- React language switching for the client shell/login/workflow labels and `Accept-Language` for localized API data
- production image packaging for the React client under `/client`
- SVG favicon/logo branding for MVC and React browser tabs
- UI translations with `.resx`
- DB translations with `LangStr`
- IDOR protection through active-gym, role, and self-only checks
- configurable CORS for the separate client app
- fail-fast production CORS validation with explicit non-localhost origins
- forwarded-header handling for reverse-proxy HTTPS deployments
- tenant route gym-resolution middleware for early unknown/inactive gym rejection
- standardized `ProblemDetails` error metadata (`400`, `401`, `403`, `404`, `409`) across public API controllers
- member, training, membership/payment, and maintenance workspace APIs and React workflow pages
- member CRUD documentation and tests across REST API, MVC Admin, and React client (`docs/domain-workflows.md`, `docs/testing.md`)
- membership package CRUD, validation, unused-package soft delete, used-package conflict, and tenant-isolation documentation/tests (`docs/domain-workflows.md`, `docs/testing.md`)
- root-level Final1 backend structure evolved into module-owned application
  services, shared service contracts, EF persistence, resources, WebApp, and
  WebApp tests
- expanded membership lifecycle statuses (`Pending`, `Active`, `Paused`, `Expired`, `Cancelled`, `Refunded`, `Renewed`)
- preserved Final2 scope reduction from the earlier enterprise model to the defended multi-gym operations + memberships product
- expanded maintenance workflow with recurring due-task generation, assignment notes, completion notes, and equipment downtime fields
- workflow-aligned REST semantics for React paths (`201` on create, `204` on delete/cancel where compatibility is handled)
- Docker, GitLab child pipeline jobs, and deploy scripts
- unit and integration tests plus frontend Vitest coverage
- Final2 modular monolith extraction through Phase 13 coverage and
  traceability hardening, plus partial Phase 14 deployment-smoke hardening,
  with
  Users, Gyms, Memberships, Training, and Maintenance API/service ownership
  moved into module projects, module-owned `*DbContext` types introduced, and
  the concrete `App.BLL` dependency removed from WebApp/modules. Shared module
  persistence plumbing now lives in `SharedKernel/Persistence`, while legacy
  `AppDbContext`/UOW runtime persistence remains transitional. Phase 13 adds
  explicit Final2 architecture and traceability evidence, API versioning
  metadata checks, and Swagger/JWT OpenAPI smoke coverage. Phase 14 extends
  the deployment smoke script to check Swagger JSON and refresh-token renewal.
  A local Docker production-stack smoke passes, but the public deployment smoke
  has not passed yet.

Deployment infrastructure exists for the `cweb-a4` proxy route.

## Main Business Scope

Platform scope:
- gym onboarding and activation
- platform analytics

Tenant scope:
- members
- staff profiles
- training categories, sessions, and bookings
- member workspace aggregation (profile, memberships, payments, bookings, attendance, outstanding actions)
- membership packages, memberships, and payments
- equipment, maintenance intervals, and maintenance tasks
- maintenance assignment notes, recurring due generation, completion notes, and downtime tracking

## Roles

Platform roles:
- `SystemAdmin`

Tenant roles:
- `GymOwner`
- `GymAdmin`
- `Member`
- `Trainer`
- `Caretaker`

## Solution Structure

```text
assignment05_final2/
  client/
  docs/
  scripts/
  App.BLL.Contracts/
  App.BLL/
  App.DAL.Contracts/
  App.DAL.EF/
  App.Domain/
  App.Resources/
  Base.Contracts/
  Base.Domain/
  Base.Helpers/
  Modules.Users/
  Modules.Gyms/
  Modules.Memberships/
  Modules.Training/
  Modules.Maintenance/
  Shared.Contracts/
  SharedKernel/
  WebApp/
  WebApp.Tests/
  .gitlab-ci.yml
  Dockerfile
  docker-compose.yml
  docker-compose.prod.yml
  multi-gym-management-system.slnx
```

Backend organization now follows the Assignment 18 reference style:
- `App.Domain/Entities` keeps one public entity per file.
- public API DTOs live in `Shared.Contracts/Dtos/v1` by API resource.
- BLL service contracts still live in root `App.BLL.Contracts/Services`
  transitionally, while concrete application services and mappers now live in
  the owning modules or WebApp presentation query folders.
- `App.BLL.Contracts/Infrastructure/IAppDbContext.cs` is a temporary
  EF-shaped compatibility contract used while direct DbContext consumers are
  replaced with module-owned repositories/services.
- `WebApp/Setup` is split into focused database, identity, service, API, middleware, and data-initialization extension files.
- `App.DAL.EF/AppDbContext` keeps legacy runtime migrations, seeding, Identity,
  and UOW-backed persistence during the transition. Phase 9 adds
  module-owned `UsersDbContext`, `GymsDbContext`, `MembershipsDbContext`,
  `TrainingDbContext`, and `MaintenanceDbContext` under each module's
  `Infrastructure/Persistence` folder. Those contexts use default schemas
  `users`, `gyms`, `memberships`, `training`, and `maintenance`, with
  architecture tests guarding module registration, schema intent, and foreign
  module DbContext usage.
- membership/payment workflows use module-owned repository contracts, module
  mappers, and focused services for package, membership, and payment
  responsibilities.
- Users refresh-token persistence is owned by `Modules.Users` through
  `Modules.Users/Application/Persistence/IRefreshTokenRepository`; the
  implementation still writes through the active `AppDbContext` table until
  the module migration cutover.
- API controllers live in modules and depend on transitional
  `App.BLL.Contracts` service contracts; concrete implementations are
  registered by their owning module. `WebApp` no longer references the
  concrete `App.BLL` project.
- MVC Admin and MVC Client area shells now follow the local LabRent/LabTrack
  reference project pattern: Bootstrap layout, sidebar navigation,
  breadcrumbs, language/workspace controls, logout, and TempData alerts, with
  gym-specific routes and labels. See
  [docs/reference-architecture-parity.md](docs/reference-architecture-parity.md).

## Local Run

Prerequisites:
- .NET 10 SDK
- Node.js 20+
- Docker Desktop or another local Docker engine

Recommended one-time tools:

```powershell
dotnet tool update -g dotnet-ef
dotnet dev-certs https --trust
```

Configure local WebApp secrets:

```powershell
cd WebApp
dotnet user-secrets set "Jwt:Key" "<long-local-jwt-secret>"
dotnet user-secrets set "Jwt:Issuer" "MultiGymManagementSystem"
dotnet user-secrets set "Jwt:Audience" "MultiGymManagementSystem"
cd ../..
```

`Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience` are required at runtime. The JWT key is intentionally not stored in `appsettings.json`.

Start PostgreSQL:

```powershell
.\scripts\start-db.ps1
```

Apply migrations:

```powershell
.\scripts\migrate-db.ps1
```

Start the ASP.NET Core backend:

```powershell
.\scripts\start-app.ps1
```

Start the separate React client in another terminal:

```powershell
cd client
npm install
npm run dev
```

Stop local PostgreSQL:

```powershell
.\scripts\stop-db.ps1
```

Default local endpoints:
- backend HTTP: `http://localhost:5107`
- backend HTTPS: `https://localhost:7245`
- Swagger: `https://localhost:7245/swagger`
- health: `https://localhost:7245/health`
- MVC admin area: `https://localhost:7245/Admin`
- MVC client area: `https://localhost:7245/mvc-client`
- React client: `http://localhost:5173`
- production-served React client path after Docker publish: `/client`

Client environment notes:
- the client reads `VITE_API_BASE_URL`
- default value is `https://localhost:7245` in Vite development
- production default is same-origin, so `/client` calls the deployed backend host without another environment variable
- example file: `client/.env.example`
- backend CORS defaults allow `http://localhost:5173`, `https://localhost:5173`, and `http://127.0.0.1:5173`
- outside development, `Cors:AllowedOrigins` must be explicitly configured with safe public origins (no localhost or wildcard entries)

## Separate Client Scope

The separate client is the main API-consuming Final2 client for the assignment.

Current scope:
- login and logout through the REST API
- automatic access-token refresh with the refresh token endpoint
- auth state persisted in `sessionStorage`, including the refresh token; this is
  documented as an accepted phase tradeoff in `docs/security-and-access.md`
- language selection persisted in `localStorage` and sent as `Accept-Language`
- tenant owner/admin access for members, sessions, training categories, membership packages, payments, equipment maintenance, settings, and gym users
- one active gym at a time based on `ActiveGymCode`, with shell tenant/role picking for assigned multi-gym users, SystemAdmin tenant picking, and switch-gym/switch-role actions available in the console
- CRUD for 3 admin entities:
  - members
  - training categories
  - membership packages
- session list and detail view
- owner/admin training-session scheduling from existing training categories
- booking action for member accounts and admin/owner demo users selecting a member
- member and booking duplicate validation returns tenant API `ProblemDetails` instead of database errors
- trainer attendance updates
- attendance lists show member/session names instead of raw identifiers
- maintenance task scheduling from active equipment with optional staff assignment
- caretaker maintenance task status updates
- member workspace page with aggregated profile/memberships/payments/bookings/attendance summaries
- maintenance workspace extensions for assignment updates and due-task generation
- role-based landing routes (`/members`, `/sessions`, `/member-workspace`, `/maintenance`)

The client accepts `SystemAdmin`, `GymAdmin`, `GymOwner`, `Member`, `Trainer`, and `Caretaker` sessions.

## Seed Demo Users

All seeded demo users use password `GymStrong123!`.

Platform users:
- `systemadmin@gym.local`

Tenant users:
- `admin@peakforge.local`
- `multigym.admin@gym.local`
- `member@peakforge.local`
- `trainer@peakforge.local`
- `caretaker@peakforge.local`

Seeded gyms:
- `peak-forge`
- `north-star`

`peak-forge` includes realistic demo operating data: multiple members and staff profiles, training categories and upcoming sessions, bookings, memberships, payments, cardio/strength equipment, and open/in-progress maintenance tasks.

Recommended React client demo user:
- `admin@peakforge.local`
- `member@peakforge.local`
- `trainer@peakforge.local`
- `caretaker@peakforge.local`

`multigym.admin@gym.local` is still the main MVC demo user for the active-gym switch flow.

## API and UX Highlights

Identity API:
- `POST /api/v1/account/register`
- `POST /api/v1/account/login`
- `POST /api/v1/account/logout`
- `POST /api/v1/account/renew-refresh-token`
- `POST /api/v1/account/switch-gym`
- `POST /api/v1/account/switch-role`

Platform API:
- `GET /api/v1/system/gyms`
- `POST /api/v1/system/gyms`
- `PUT /api/v1/system/gyms/{gymId}/activation`
- `GET /api/v1/system/gyms/{gymId}/snapshot`
- `GET /api/v1/system/platform/analytics`

React client API coverage:
- `POST /api/v1/account/login`
- `POST /api/v1/account/logout`
- `POST /api/v1/account/renew-refresh-token`
- `POST /api/v1/account/switch-gym`
- `POST /api/v1/account/switch-role`
- `POST /api/v1/account/forgot-password`
- `POST /api/v1/account/reset-password`
- `GET|POST|PUT|DELETE /api/v1/{gymCode}/members`
- `GET /api/v1/{gymCode}/members/me`
- `GET /api/v1/{gymCode}/member-workspace/me`
- `GET /api/v1/{gymCode}/member-workspace/members/{memberId}`
- `GET|POST|PUT|DELETE /api/v1/{gymCode}/training-categories`
- `GET /api/v1/{gymCode}/training-sessions`
- `GET /api/v1/{gymCode}/training-sessions/{id}`
- `GET /api/v1/{gymCode}/bookings`
- `POST /api/v1/{gymCode}/bookings`
- `PUT /api/v1/{gymCode}/bookings/{id}/attendance`
- `GET /api/v1/{gymCode}/maintenance-tasks`
- `PUT /api/v1/{gymCode}/maintenance-tasks/{id}/status`
- `PUT /api/v1/{gymCode}/maintenance-tasks/{id}/assignment`
- `POST /api/v1/{gymCode}/maintenance-tasks/generate-due`
- `GET|POST|PUT|DELETE /api/v1/{gymCode}/membership-packages`
- `PUT /api/v1/{gymCode}/memberships/{id}/status`

REST semantics note:
- create actions used by the React workflow pages return `201` (`Created` / `CreatedAtAction`)
- delete/cancel actions used by the React workflow pages return `204 NoContent`

MVC areas:
- `Areas/Admin`: platform dashboards and tenant admin pages
- `Areas/Client`: member profile/history, session detail/booking/cancellation, trainer roster attendance, and caretaker task status. Client session page composition now goes through `IClientSessionsPageService` and BLL query/workflow services instead of direct controller EF access.

## Verification

Backend:

```powershell
dotnet format multi-gym-management-system.slnx --verify-no-changes
dotnet build multi-gym-management-system.slnx
dotnet test multi-gym-management-system.slnx
dotnet test multi-gym-management-system.slnx --filter Admin -- xUnit.ParallelizeTestCollections=false
```

Latest local validation snapshot, 2026-05-25, for Final2 Phase 13:

| Command | Result |
|---|---|
| `dotnet format multi-gym-management-system.slnx --verify-no-changes` | Pass; workspace-load warnings reported, no formatting changes required |
| `dotnet build multi-gym-management-system.slnx` | Pass, 0 errors; known transitive NuGet advisories on `Microsoft.AspNetCore.DataProtection` and `System.Security.Cryptography.Xml` |
| `dotnet test multi-gym-management-system.slnx -- xUnit.ParallelizeTestCollections=false` | Pass, Architecture.Tests 17 passed / 1 skipped, WebApp.Tests 199 passed / 3 skipped PostgreSQL/Testcontainers tests |
| `cd client && npm test -- --run` | Pass, 6 files / 32 tests; React Router v7 future warnings only |
| `cd client && npm run build` | Pass, Vite production build completed |

Phase 14 deployment-smoke snapshot, 2026-05-25:

| Command | Result |
|---|---|
| `C:\Program Files\Git\bin\bash.exe -n scripts/smoke-deploy.sh scripts/deploy-client.sh scripts/deploy.sh` | Pass |
| `docker compose -f docker-compose.prod.yml config` | Pass with explicit backend/client production CORS origins |
| `docker compose --profile client -f docker-compose.prod.yml config` | Pass with standalone client profile |
| `docker compose --profile client -f docker-compose.prod.yml build` | Pass after starting Docker Desktop; backend and standalone client images built |
| `docker compose --project-name assignment05-final2-smoke --profile client -f docker-compose.prod.yml up -d --no-build --wait` | Pass on local smoke ports `18083` and `18081`; backend, PostgreSQL, and standalone client containers healthy |
| `scripts/smoke-deploy.sh` against local production stack | Pass with `BACKEND_URL=http://localhost:18083`, `CLIENT_URL=http://localhost:18081`, and `SMOKE_CORS_ORIGIN=https://mtiker-cweb-4-client.proxy.itcollege.ee` |
| `scripts/smoke-deploy.sh` against `https://mtiker-cweb-4.proxy.itcollege.ee` and `https://mtiker-cweb-4-client.proxy.itcollege.ee` | Failed at backend `/health`; public host returned HTTP 404 |

PostgreSQL provider-integration slice:
- the default `dotnet test` run keeps fast coverage and skips Testcontainers-based PostgreSQL tests
- the PostgreSQL tests start a real `postgres:16-alpine` container through Testcontainers, so they require a reachable Docker engine/socket
- normal CI keeps these tests skipped so runners without Docker/Testcontainers support do not fail the required test stage
- to execute the PostgreSQL slice before defense, run with Docker available and set `RUN_POSTGRES_TESTS=1`

```powershell
$env:RUN_POSTGRES_TESTS = "1"
dotnet test multi-gym-management-system.slnx --filter PostgreSql
Remove-Item Env:\RUN_POSTGRES_TESTS
```

Bash/GitLab equivalent:

```bash
RUN_POSTGRES_TESTS=1 dotnet test multi-gym-management-system.slnx --filter PostgreSql
```

GitLab also exposes `assignment05_final2_postgresql_tests` as an optional manual job. It uses the same opt-in flag and filter, and should only be started on a runner where Docker is available.

Separate client:

```powershell
cd client
npm test
npm run build
```

## Docker and CI/CD

Local development database:
- `docker-compose.yml`

Production deployment assets:
- `Dockerfile`
- `docker-compose.prod.yml`
- `scripts/deploy.sh`
- `scripts/deploy-client.sh`
- `scripts/smoke-deploy.sh`

The production Dockerfile builds the Vite client with Node 20 and copies `client/dist` into `WebApp/wwwroot/client`, so the deployed backend serves the REST client at `/client`.

Required production secrets/configuration:
- `JWT__Key`
- `POSTGRES_PASSWORD`
- `JWT__Issuer`, defaulted by Compose to `MultiGymManagementSystem`
- `JWT__Audience`, defaulted by Compose to `MultiGymManagementSystem`
- `CORS_ALLOWED_ORIGIN`, defaulted by Compose to `https://mtiker-cweb-4.proxy.itcollege.ee`
- `POSTGRES_DB` and `POSTGRES_USER`, defaulted by Compose unless overridden

`POSTGRES_PASSWORD` has no production default. `docker-compose.prod.yml` and
`scripts/deploy.sh` fail fast when it is missing so production cannot silently
start with the development `postgres` password.

Deployment smoke verification:

```bash
export BACKEND_URL="https://<backend-host>"
export CLIENT_URL="https://<client-host>"
export SMOKE_EMAIL="<smoke-user@example.com>"
export SMOKE_PASSWORD="<smoke-password>"
export SMOKE_GYM_CODE="<gym-code>"
# Optional when container URLs differ from the browser origin being tested:
export SMOKE_CORS_ORIGIN="https://<client-public-origin>"

bash scripts/smoke-deploy.sh
```

The smoke script checks backend `/health`, Swagger JSON, standalone client
`/healthz`, CORS preflight from the standalone client origin, API login,
refresh-token renewal, and one authenticated tenant API read using the renewed
JWT. See `docs/deployment.md` for the full deployment and Compose validation
commands.

Smoke status on 2026-05-25:
- production Compose configuration rendered for backend-only and standalone
  client profile
- Docker image build passed after Docker Desktop was started
- local Docker production-stack smoke passed on ports `18083` and `18081`
- public backend/client smoke did not pass because the public backend returned
  HTTP 404 for `/health`

ASP.NET Core Data Protection keys are persisted through the application database so MVC/cookie protection keys survive container restarts.

Repository CI integration:
- root `.gitlab-ci.yml` triggers this assignment as an isolated child pipeline
- assignment-local `.gitlab-ci.yml` now runs:
  - separate React client install/test/build
  - .NET build
  - .NET test
  - optional manual PostgreSQL/Testcontainers test slice
  - Docker package
  - deploy

## Documentation Map

Start here:
- docs index: [docs/README.md](docs/README.md)
- Final2 architecture: [docs/final2-architecture.md](docs/final2-architecture.md)
- Final2 traceability: [docs/final2-traceability.md](docs/final2-traceability.md)
- Final1 architecture reset notes: [docs/final1-structure-reset.md](docs/final1-structure-reset.md)
- A3 SaaS scope plan: [docs/a3-saas-plan.md](docs/a3-saas-plan.md)

Reference docs:
- architecture: [docs/architecture.md](docs/architecture.md)
- domain workflows: [docs/domain-workflows.md](docs/domain-workflows.md)
- security and access: [docs/security-and-access.md](docs/security-and-access.md)
- data model and ERD: [docs/data-model.md](docs/data-model.md)
- API overview: [docs/api.md](docs/api.md)
- testing: [docs/testing.md](docs/testing.md)
- deployment: [docs/deployment.md](docs/deployment.md)

Defense and logs:
- Final1 defense pack: [docs/final1-defense.md](docs/final1-defense.md)
- Final2 defense pack: [docs/final2-defense.md](docs/final2-defense.md)
- AI usage: [docs/ai-usage.md](docs/ai-usage.md)

## Known Limitations

- The public deployment URL is documented, but the latest Phase 14 public smoke
  attempt returned HTTP 404 for backend `/health`; live deployment is not
  currently verified.
- Separate client hosting artifacts, image build, and a local standalone-client
  production smoke pass; the separate public client host is not verified until
  the backend proxy/deployment issue is corrected and the public smoke reruns.
- The React client works with one active gym context at a time; assigned multi-gym users and SystemAdmin can switch active context from the shell.
- The React client currently stores the refresh token in JavaScript-readable
  `sessionStorage`; rotation, reuse rejection, logout invalidation, server-side
  token lookup, and configurable access-token lifetime are the current
  compensating controls. A future security-hardening phase should migrate the
  refresh token to an `HttpOnly`, `Secure`, `SameSite` cookie with the required
  CSRF/CORS changes.
- Payments are internal records only; no external payment provider is integrated.
- Support tickets stay inside the same monolith and are intentionally lightweight.
