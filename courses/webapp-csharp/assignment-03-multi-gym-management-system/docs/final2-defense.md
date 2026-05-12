# Final2 Defense Pack

## Scope

Final2 submission evidence for the multi-gym management system covers the
modular monolith hardening required by the Web Applications with C# Final2
assignment:

- ASP.NET Core modular monolith with at least three modules.
- Users module plus business modules.
- Clear module data ownership.
- Mediator-based cross-module communication.
- No direct project references between modules.
- Stable public REST API routes and DTO boundaries.
- MVC Admin, MVC Client, and separate React client remain operational.
- Auth, IDOR, i18n, architecture, and CI/deployment evidence are current.
- Admin MVC CRUD for members, training categories, and membership packages is
  verified by integration tests.

Official course requirement source:
`https://courses.taltech.akaver.com/web-applications-with-csharp/assignments/home-assignments/Personal%20Project%20Final2`

## Module Evidence

| Module | Type | Data owner summary | Evidence |
|---|---|---|---|
| Users | Required user module | `AppUser`, `AppRole`, `AppUserGymRole`, `RefreshToken`, `Person`, `Contact`, `PersonContact` | `src/Modules.Users`, `docs/users-module-contracts.md`, `docs/users-mediator-messages.md` |
| GymManagement | Business module | gyms, staff, equipment, maintenance, opening hours, gym settings, support, audit | `src/Modules.GymManagement`, `docs/gymmanagement-module-contracts.md`, `docs/maintenance-mediator-messages.md` |
| Training | Business module | members, training categories, sessions, bookings, coaching plans | `src/Modules.Training`, `docs/training-module-contracts.md`, `docs/training-mediator-messages.md` |
| MembershipFinance | Business module | packages, memberships, payments, invoices, refunds, tenant subscriptions | `src/Modules.MembershipFinance`, `docs/finance-mediator-messages.md` |

`BuildingBlocks` is not a domain module. It owns shared abstractions:
`IModule`, `IMediator`, request/handler interfaces, and handler registration.

## Boundary Position

- `WebApp` is the composition root and is allowed to reference every module.
- Module projects reference `BuildingBlocks`, `App.Domain`, `App.DTO`, and
  transitional `App.BLL` contracts/services during the modularization phase.
- Module projects do not reference each other.
- HTTP controllers dispatch migrated Final2 slices through `IMediator`.
- The single PostgreSQL database and `AppDbContext` remain an implementation
  detail for this phase; ownership is enforced at code/module level.
- Full module isolation is not claimed. Module ownership is partial and
  documented: the strongest migrated workflows own their orchestration inside
  module handlers, while remaining workflows still call shared BLL services.

## Stable Public Surfaces

The public API route surface is locked by
`ApiContractMetadataTests.PublicApiRoutes_RemainStableForFinal2Submission`.
The test snapshots all public API HTTP method + route templates under
`WebApp.ApiControllers`.

Existing response metadata is also locked:

- `ProblemDetails` metadata for `400`, `401`, `403`, `404`, and `409`.
- Account auth routes and DTOs for login, logout, and refresh.

## Clean/Onion Improvements

Current Clean/Onion evidence after the implemented fixes:

- API controllers and migrated MVC Admin controllers do not inject
  `AppDbContext` or `IAppDbContext`.
- Tenant access checks use the BLL-owned `IAuthorizationQueryRepository`
  instead of direct context access inside `TenantAccessChecker`.
- Client MVC Dashboard and Sessions controllers delegate to page services.
  Their page services use BLL/application contracts, while query services use
  `IAppUnitOfWork`.
- Membership, Training, Maintenance, and Finance services use repository,
  Unit of Work, mapper, and authorization contracts for the migrated slices.
- Architecture tests enforce the main dependency direction rules:
  Domain/DTO inward dependencies, BLL avoiding DAL/WebApp, API controller
  no-DbContext rules, Admin controller thinness, and migrated page-service
  boundaries.

Remaining Clean/Onion limitation:
- some Admin page services still perform pragmatic read composition through
  `AppDbContext`; this is documented and not presented as fully migrated
  application-layer query ownership.

## Module Ownership Improvements

Current module ownership evidence:

- Users module owns account-session mediator messages for login, refresh,
  logout, switch-gym, and switch-role.
- GymManagement owns member mediator messages and maintenance/facility
  adapter messages.
- Training owns training category CRUD orchestration inside
  `Modules.Training.Application`; session, booking, and attendance adapters
  still delegate to shared Training BLL services.
- MembershipFinance owns membership package CRUD orchestration inside
  `Modules.MembershipFinance.Application`; broader membership, payment,
  invoice, refund, and workspace handlers are mediated but still partly
  transitional.
- `ModuleArchitectureTests` verify no direct module-to-module project
  references, no non-Users module references to Users internals, Training
  does not reference Users/GymManagement internals, mediator resolvability,
  and module-owned handler rules for Training categories and membership
  packages.

Not claimed:
- separate database schemas
- per-module DbContexts
- full internal ownership for every workflow
- microservice extraction

## Admin CRUD Evidence

MVC Admin CRUD is now tested for three tenant entities:

| Area | Evidence |
|---|---|
| Members | `AdminMembersCrudTests` covers index, create form, invalid create validation, valid create persistence, edit form, valid edit persistence, delete removal from active listing, unauthorized access denial, and cross-tenant member id denial. |
| Training categories | `AdminTrainingCategoriesCrudTests` covers index, create/edit/delete, validation, localized Estonian `LangStr` rendering, unauthorized access denial, and cross-tenant category id denial. |
| Membership packages | `AdminMembershipPackagesCrudTests` covers index, create/edit/delete, validation, active-gym persistence, unauthorized access denial, and cross-tenant package id denial. |

## Local Verification

Last verified locally on 2026-05-11:

| Check | Result |
|---|---|
| `dotnet format multi-gym-management-system.slnx --verify-no-changes` | Pass, no formatting changes required |
| `dotnet build multi-gym-management-system.slnx` | Pass, 0 warnings, 0 errors |
| `dotnet test multi-gym-management-system.slnx` | Pass, 250 passed, 3 skipped PostgreSQL/Testcontainers tests |
| `cd client && npm test` | Pass, 7 files / 34 tests; React Router v7 future warnings only |
| `cd client && npm run build` | Pass, TypeScript check and Vite production build completed |
| `docker compose config` | Pass, development PostgreSQL Compose config rendered |
| `POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key VITE_API_BASE_URL=https://api.example.test docker compose -f docker-compose.prod.yml config` | Pass, production backend/PostgreSQL Compose config rendered |
| `POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key VITE_API_BASE_URL=https://api.example.test docker compose --profile client -f docker-compose.prod.yml config` | Pass, production backend/PostgreSQL plus standalone client Compose profile rendered |

Skipped backend tests are the existing Testcontainers PostgreSQL provider tests
gated by `RUN_POSTGRES_TESTS=1`. They were not executed in this readiness pass.

## Deployment Evidence

Verified:
- local Compose configuration renders the development PostgreSQL service
- production Compose configuration renders backend plus PostgreSQL when
  required secrets are provided
- production Compose with `--profile client` renders the standalone client
  service, including `VITE_API_BASE_URL=https://api.example.test`
- the React client production bundle builds successfully

Not verified:
- `scripts/smoke-deploy.sh` was not run because no real public backend/client
  URLs or smoke credentials were provided
- public VPS/proxy availability was not checked in this pass
- separate public client host reachability and CORS preflight were not checked
  against a live deployment

## Defense Demo Path

Recommended defense order:

1. Show `docs/final2-module-boundary-report.md` and the module projects under
   `src/Modules.*`.
2. Run or show CI output for `ModuleArchitectureTests` and
   `ArchitectureTests`.
3. Show `docs/module-data-ownership.md` for entity ownership.
4. Show controller examples that dispatch through `IMediator` while preserving
   route attributes.
5. Show `ApiContractMetadataTests.PublicApiRoutes_RemainStableForFinal2Submission`.
6. Demo MVC Admin at `/Admin` with an admin user.
7. Demo MVC Client at `/mvc-client` with member/trainer/caretaker users.
8. Demo React client login and one CRUD/workflow mutation.
9. Show `docs/deployment.md` and `.gitlab-ci.yml` for CI/CD evidence.

## Not Claimed

- No new business features were added in this hardening pass.
- Public VPS availability was not smoke-tested from this local run.
- Separate public client hosting was not live-smoke-tested from this local run.
- PostgreSQL Testcontainers tests were not run because they remain opt-in.
- Full module isolation is not claimed; module ownership is partial and
  transitional where handlers still use shared BLL services and one
  `AppDbContext`.
