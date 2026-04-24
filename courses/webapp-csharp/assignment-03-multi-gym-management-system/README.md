Public URL: `https://mtiker-cweb-4.proxy.itcollege.ee`

# Assignment 03 - Multi-Gym Management System

Assignment 03 is implemented as a SaaS multi-gym management system under `courses/webapp-csharp/assignment-03-multi-gym-management-system`.

The project now has three user-facing surfaces:
- MVC admin UX inside `src/WebApp/Areas/Admin`
- MVC client UX inside `src/WebApp/Areas/Client`, served under `/mvc-client`
- a separate React + TypeScript SaaS client under `client/`

Admin route note:
- `/Admin/Gyms`, `/Admin/Memberships`, `/Admin/Sessions`, and `/Admin/Operations` now redirect into `/client/*` SaaS routes so admin users land on functional write workflows instead of read-only summary pages.

The backend remains one ASP.NET Core host that serves the MVC areas, Swagger, the versioned REST API, and the production React client at `/client`. The MVC client area uses `/mvc-client` so it does not collide with the React client mount.

## Requirement Coverage

This assignment currently covers:
- multi-tenant gym SaaS domain design with more than 10 meaningful entities
- versioned REST API controllers under `/api/v1/...`
- public DTOs in `src/App.DTO/v1`
- Swagger
- JWT authentication with refresh-token rotation
- MVC admin UX
- MVC client UX
- a separate React client that uses the REST API with JWT + refresh tokens
- React platform/tenant SaaS console for system, billing, support, onboarding, account, and tenant operations
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
- member/coaching/finance/maintenance workspace APIs and React workflow pages
- coaching-plan workflow states and item-decision lifecycle
- finance workflow records for invoices, lines, payment history, refunds/credits, overdue state, and outstanding balances
- expanded membership lifecycle statuses (`Pending`, `Active`, `Paused`, `Expired`, `Cancelled`, `Refunded`, `Renewed`)
- subscription-tier enforcement for starter/growth/enterprise limits in BLL with explicit unit tests
- expanded maintenance workflow with recurring due-task generation, assignment history, completion notes, and equipment downtime fields
- workflow-aligned REST semantics for React paths (`201` on create, `204` on delete/cancel where compatibility is handled)
- Docker, GitLab child pipeline jobs, and deploy scripts
- unit and integration tests plus frontend Vitest coverage

Deployment infrastructure exists for the `cweb-a4` proxy route.

## Main Business Scope

Platform/SaaS scope:
- gym onboarding and activation
- subscriptions
- support tickets
- platform analytics
- impersonation
- audit logging

Tenant scope:
- members
- staff, job roles, contracts, vacations, and shifts
- training categories, sessions, and bookings
- member workspace aggregation (profile, memberships, payments, bookings, attendance, outstanding actions)
- coaching plans and coaching plan items
- membership packages, memberships, and payments
- finance workspace with invoices, invoice lines, payment history, refunds/credits, overdue and outstanding balances
- opening hours and exceptions
- equipment, maintenance intervals, and maintenance tasks
- maintenance assignment history, recurring due generation, completion notes, and downtime tracking

## Roles

Platform roles:
- `SystemAdmin`
- `SystemSupport`
- `SystemBilling`

Tenant roles:
- `GymOwner`
- `GymAdmin`
- `Member`
- `Trainer`
- `Caretaker`

## Solution Structure

```text
assignment-03-multi-gym-management-system/
  client/
  docs/
  scripts/
  src/
    App.BLL/
    App.DAL.EF/
    App.Domain/
    App.DTO/
    App.Resources/
    WebApp/
  tests/
    WebApp.Tests/
  .gitlab-ci.yml
  Dockerfile
  docker-compose.yml
  docker-compose.prod.yml
  multi-gym-management-system.slnx
```

Backend organization now follows the Assignment 18 reference style:
- `App.Domain/Entities` keeps one public entity per file.
- `App.DTO/v1` is split by API resource instead of grouped DTO files.
- BLL service interfaces live beside their implementations in `App.BLL/Services`; infrastructure-only contracts live under `App.BLL/Contracts`.
- `WebApp/Setup` is split into focused database, identity, service, API, middleware, and data-initialization extension files.
- `App.DAL.EF/AppDbContext` keeps cross-cutting behavior while entity mapping/index/precision rules are split into grouped `IEntityTypeConfiguration<T>` classes under `App.DAL.EF/Configurations`.
- membership and authorization workflows keep the existing public contracts, but are internally split into focused services for package/membership/payment/pricing and actor/tenant/resource authorization responsibilities.

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
cd src/WebApp
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

The separate client is the main API-consuming SaaS console for the assignment.

Current scope:
- login and logout through the REST API
- automatic access-token refresh with the refresh token endpoint
- auth state persisted in `sessionStorage`
- language selection persisted in `localStorage` and sent as `Accept-Language`
- system-role access for platform analytics, gym onboarding, activation, snapshots, support tickets, subscriptions, and impersonation
- tenant owner/admin access to a function console for staff, contracts, vacations, sessions, work shifts, bookings, memberships, payments, facilities, maintenance, settings, and gym users
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
- trainer coaching workspace page with coaching-plan CRUD, status transitions, and per-item decisions
- finance workspace page with invoice creation, payment posting, refund posting, and outstanding-balance visibility
- maintenance workspace extensions for assignment updates/history and due-task generation
- role-based landing routes (`/member-workspace`, `/coaching-workspace`, `/finance-workspace`, `/maintenance`)

The client accepts `SystemAdmin`, `SystemSupport`, `SystemBilling`, `GymAdmin`, `GymOwner`, `Member`, `Trainer`, and `Caretaker` sessions.

## Seed Demo Users

All seeded demo users use password `GymStrong123!`.

Platform users:
- `systemadmin@gym.local`
- `support@gym.local`
- `billing@gym.local`

Tenant users:
- `admin@peakforge.local`
- `multigym.admin@gym.local`
- `member@peakforge.local`
- `trainer@peakforge.local`
- `caretaker@peakforge.local`

Seeded gyms:
- `peak-forge`
- `north-star`

`peak-forge` includes realistic demo operating data: full weekly opening hours, multiple members and staff profiles, training categories and upcoming sessions, bookings, memberships, payments, cardio/strength equipment, and open/in-progress maintenance tasks.

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
- `GET /api/v1/system/subscriptions`
- `PUT /api/v1/system/subscriptions/{gymId}`
- `GET /api/v1/system/support`
- `POST /api/v1/system/support/{gymId}/tickets`
- `GET /api/v1/system/platform/analytics`
- `POST /api/v1/system/impersonation`

React client API coverage:
- `POST /api/v1/account/login`
- `POST /api/v1/account/logout`
- `POST /api/v1/account/renew-refresh-token`
- `POST /api/v1/account/switch-gym`
- `POST /api/v1/account/switch-role`
- `POST /api/v1/account/forgot-password`
- `POST /api/v1/account/reset-password`
- all documented platform API endpoints
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
- `GET|POST /api/v1/{gymCode}/coaching-plans`
- `GET|PUT|DELETE /api/v1/{gymCode}/coaching-plans/{id}`
- `PUT /api/v1/{gymCode}/coaching-plans/{id}/status`
- `PUT /api/v1/{gymCode}/coaching-plans/{id}/items/{itemId}/decision`
- `GET /api/v1/{gymCode}/maintenance-tasks`
- `PUT /api/v1/{gymCode}/maintenance-tasks/{id}/status`
- `PUT /api/v1/{gymCode}/maintenance-tasks/{id}/assignment`
- `GET /api/v1/{gymCode}/maintenance-tasks/{id}/assignment-history`
- `POST /api/v1/{gymCode}/maintenance-tasks/generate-due`
- `GET|POST|PUT|DELETE /api/v1/{gymCode}/membership-packages`
- `PUT /api/v1/{gymCode}/memberships/{id}/status`
- `GET /api/v1/{gymCode}/finance-workspace/me`
- `GET /api/v1/{gymCode}/finance-workspace/members/{memberId}`
- `GET|POST /api/v1/{gymCode}/invoices`
- `GET /api/v1/{gymCode}/invoices/{id}`
- `POST /api/v1/{gymCode}/invoices/{id}/payments`
- `POST /api/v1/{gymCode}/invoices/{id}/refunds`
- the `/platform` and `/console` routes expose the remaining tenant endpoints listed in `docs/api.md`

REST semantics note:
- create actions used by the React workflow pages return `201` (`Created` / `CreatedAtAction`)
- delete/cancel actions used by the React workflow pages return `204 NoContent`

MVC areas:
- `Areas/Admin`: platform dashboards and tenant admin pages
- `Areas/Client`: member profile/history, session detail/booking/cancellation, trainer roster attendance, caretaker task status, and opening-hours visibility

## Verification

Backend:

```powershell
dotnet build multi-gym-management-system.slnx
dotnet test multi-gym-management-system.slnx
```

PostgreSQL provider-integration slice:
- the default `dotnet test` run keeps fast coverage and skips Testcontainers-based PostgreSQL tests
- to execute the PostgreSQL slice, run with Docker available and set `RUN_POSTGRES_TESTS=1`

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

The production Dockerfile builds the Vite client with Node 20 and copies `client/dist` into `WebApp/wwwroot/client`, so the deployed backend serves the REST client at `/client`.

Required production secrets/configuration:
- `JWT__Key`
- `JWT__Issuer`, defaulted by Compose to `MultiGymManagementSystem`
- `JWT__Audience`, defaulted by Compose to `MultiGymManagementSystem`
- `CORS_ALLOWED_ORIGIN`, defaulted by Compose to `https://mtiker-cweb-4.proxy.itcollege.ee`
- `POSTGRES_DB`, `POSTGRES_USER`, and `POSTGRES_PASSWORD` when overriding database defaults

ASP.NET Core Data Protection keys are persisted through the application database so MVC/cookie protection keys survive container restarts.

Repository CI integration:
- root `.gitlab-ci.yml` triggers this assignment as an isolated child pipeline
- assignment-local `.gitlab-ci.yml` now runs:
  - separate React client install/test/build
  - .NET build
  - .NET test
  - Docker package
  - deploy

## Documentation Map

- architecture: [docs/architecture.md](docs/architecture.md)
- data model and ERD: [docs/data-model.md](docs/data-model.md)
- API overview: [docs/api.md](docs/api.md)
- testing: [docs/testing.md](docs/testing.md)
- deployment: [docs/deployment.md](docs/deployment.md)
- A3 scope plan: [docs/a3-saas-plan.md](docs/a3-saas-plan.md)
- request-flow diagram: [docs/request-flow-diagram.md](docs/request-flow-diagram.md)
- study guide (domain): [docs/study-guide-domain.md](docs/study-guide-domain.md)
- study guide (DAL/EF): [docs/study-guide-dal-ef.md](docs/study-guide-dal-ef.md)
- study guide (BLL): [docs/study-guide-bll.md](docs/study-guide-bll.md)
- study guide (DTO/API): [docs/study-guide-dto-api.md](docs/study-guide-dto-api.md)
- study guide (auth/tenant flow): [docs/study-guide-auth-tenant-flow.md](docs/study-guide-auth-tenant-flow.md)
- study guide (deployment): [docs/study-guide-deployment.md](docs/study-guide-deployment.md)
- AI usage: [docs/ai-usage.md](docs/ai-usage.md)

## Known Limitations

- The public deployment URL is documented, but live availability still depends on the VPS/proxy/container state at review time.
- The React client works with one active gym context at a time; assigned multi-gym users and SystemAdmin can switch active context from the shell.
- Payments are internal records only; no external payment provider is integrated.
- Support tickets stay inside the same monolith and are intentionally lightweight.
