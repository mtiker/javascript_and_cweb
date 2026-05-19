# Final1 Defense Pack

Final1 is defended as a Clean/Onion-style ASP.NET Core SaaS monolith with MVC,
REST API, JWT auth, a separate React client, tests, CI, Docker, and deployment
evidence.

Official requirement source:
https://courses.taltech.akaver.com/web-applications-with-csharp/assignments/home-assignments/Personal%20Project%20Final1

## Defense Position

What is claimed:
- layered solution with Domain, DTO, BLL, DAL.EF, WebApp, and client projects
- PostgreSQL/EF Core migrations and seeded demo data
- versioned REST API under `/api/v1`
- Swagger in development
- JWT access tokens with refresh-token rotation
- system and tenant roles
- tenant isolation through active gym context and `GymId` scoping
- MVC Admin and MVC Client Razor surfaces
- React + TypeScript client that consumes the API
- Docker and GitLab CI/CD artifacts
- automated backend and frontend tests

What is not claimed:
- browser Playwright E2E coverage
- full MVC Admin CRUD for every entity
- live deployment availability unless smoke-tested at defense time
- Final2 completion or full module isolation
- zero EF abstraction usage outside infrastructure

## Evidence Map

| Requirement area | Evidence |
|---|---|
| Architecture and layering | [architecture.md](architecture.md), `ArchitectureTests.cs` |
| Product/domain scope | [a3-saas-plan.md](a3-saas-plan.md), [domain-workflows.md](domain-workflows.md) |
| Data model and tenancy | [data-model.md](data-model.md), `AppDbContextBehaviorTests` |
| API and DTOs | [api.md](api.md), `ApiContractMetadataTests` |
| Security and roles | [security-and-access.md](security-and-access.md), `AuthSecurityAndErrorTests`, `AuthorizationServiceTests` |
| MVC Admin/Client | `src/WebApp/Areas/Admin`, `src/WebApp/Areas/Client`, `MvcComplianceTests`, `Admin*CrudTests` |
| React client | `client/`, Vitest coverage listed in [testing.md](testing.md) |
| Tests | [testing.md](testing.md) |
| Deployment and CI/CD | [deployment.md](deployment.md), assignment `.gitlab-ci.yml` |

## Demo Path

1. Open `/swagger` and `/health`.
2. Login to MVC Admin with `multigym.admin@gym.local`.
3. Show MVC Admin pages and one tested CRUD flow.
4. Open `/mvc-client` as member, trainer, or caretaker.
5. Start or open the React client and login as `admin@peakforge.local`.
6. Demonstrate React CRUD for members, training categories, or membership
   packages.
7. Demonstrate one workflow page: member workspace, sessions/bookings, or
   maintenance.
8. Show tests and CI/deployment docs.

Seed password for demo users:
- `GymStrong123!`

## Current Validation Snapshot

Latest Final1 completion validation was run on 2026-05-19:
- `dotnet build multi-gym-management-system.slnx --no-restore` passed with 0
  warnings and 0 errors
- `dotnet test multi-gym-management-system.slnx --no-restore` passed with 202
  passed and 3 skipped PostgreSQL/Testcontainers tests
- `cd client && npm test` passed with 32 Vitest tests
- `cd client && npm run build` passed
- Docker was checked with `docker info --format '{{.ServerVersion}}'`, but the
  Docker Desktop engine pipe was unavailable, so the opt-in PostgreSQL provider
  tests were not run

## Known Final1 Risks

- PostgreSQL/Testcontainers tests are skipped unless `RUN_POSTGRES_TESTS=1` is
  set and Docker is available.
- Public VPS/proxy availability must be smoke-tested before claiming the live
  URL is ready.
- MVC Admin CRUD is intentionally focused on three entities; broader admin
  areas are read/action surfaces or are covered by React/API workflows.
- MVC controllers and view components no longer inject concrete `AppDbContext`;
  architecture tests now guard that Final1 presentation boundary.
- Some BLL and module internals still use `IAppDbContext`; see
  [final1-final2-roadmap.md](final1-final2-roadmap.md).
- React refresh token storage remains in `sessionStorage`; compensating
  controls are documented in [security-and-access.md](security-and-access.md).
