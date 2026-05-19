# Final1 / Final2 Development Roadmap

This is the development-facing checklist for finishing Assignment 03. It
replaces the older phase audits and slice plans.

Official requirement sources:
- Final1: https://courses.taltech.akaver.com/web-applications-with-csharp/assignments/home-assignments/Personal%20Project%20Final1
- Final2: https://courses.taltech.akaver.com/web-applications-with-csharp/assignments/home-assignments/Personal%20Project%20Final2

## Current Position

Final1 is the current completion target and is defensible as a Clean/Onion-style ASP.NET Core SaaS monolith:
- domain, DTO, BLL, DAL.EF, WebApp, tests, and React client projects exist
- API controllers are thin and do not inject `AppDbContext`
- MVC controllers and view components do not inject concrete `AppDbContext`
- repository, Unit of Work, mapper, and workflow-service patterns exist for
  key slices
- MVC Admin has tested CRUD for members, training categories, and membership
  packages
- React client consumes the REST API with JWT and refresh-token rotation
- Docker, CI, deployment scripts, and smoke-check scripts exist
- the Final2 defended domain has been reduced to multi-gym operations,
  training, memberships/payments, and equipment maintenance

Final2 is partially implemented:
- `BuildingBlocks` contains the in-process mediator and module abstractions
- `Modules.Users`, `Modules.GymManagement`, `Modules.Training`, and
  `Modules.MembershipFinance` exist
- module projects do not reference each other directly
- WebApp dispatches migrated slices through `IMediator`
- Training category CRUD and membership package CRUD have real module-owned
  handlers
- several other module handlers are still adapters over shared BLL services
- all modules still share one `AppDbContext`
- optional enterprise contexts were physically pruned by the
  `PruneFinal2Scope` migration

Do not claim full module isolation, separate schemas, per-module DbContexts, or
microservice extraction.

## Final1 Checklist

| Area | Current evidence | Remaining work |
|---|---|---|
| Clean/Onion dependency direction | `ArchitectureTests`, `architecture.md`, repository/UOW contracts, DAL repository implementations | Keep tightening architecture tests as each remaining `IAppDbContext` usage is migrated. |
| Domain model | `data-model.md`, `src/App.Domain/Entities`, EF migrations | Final2 scope is intentionally reduced; keep ERD and entity notes in sync when business entities change. |
| REST API and DTOs | `api.md`, `src/App.DTO/v1`, `ApiContractMetadataTests` | Keep route and error docs aligned when endpoints or DTOs change. |
| JWT and refresh tokens | `security-and-access.md`, `AuthSecurityAndErrorTests`, `IRefreshTokenRepository` | Future hardening: move refresh token storage from JavaScript-readable `sessionStorage` to an `HttpOnly` cookie if scope allows. |
| Tenant isolation and roles | `security-and-access.md`, `AuthorizationServiceTests`, `TenantIsolationAndIdorTests`, `Admin*CrudTests` | Add targeted tests when adding a new tenant-owned mutation path. |
| MVC Admin | Tested CRUD for members, training categories, membership packages | For a stronger defense, add one more mutable admin workflow that demonstrates a business process, such as membership sale or equipment CRUD. |
| MVC Client | Member/trainer/caretaker routes exist and render | Profile, maintenance, home/workspace switcher paths now delegate to page/query services. |
| React client | Vitest coverage for auth, CRUD, sessions, attendance, maintenance, and API client behavior | Keep adding component tests for any new user-facing React workflow. |
| Deployment | Dockerfile, production Compose, CI child pipeline, smoke script | Run and record a real public smoke check before claiming live availability. |
| PostgreSQL validation | Optional Testcontainers suite exists | Run `RUN_POSTGRES_TESTS=1 dotnet test --filter PostgreSql` on a Docker-capable machine before defense. |

## Final2 Checklist

| Area | Current evidence | Remaining work |
|---|---|---|
| Module skeletons | `src/Modules.*`, `src/BuildingBlocks`, `ModuleArchitectureTests` | Keep one DI extension and one marker type per module. |
| No direct module references | Enforced by `ModuleArchitectureTests` | Do not add project references between `Modules.*` projects. Use mediator contracts. |
| Users module | Login, refresh, logout, switch-gym, and switch-role mediator messages | Move `UsersSessionService` off `IAppDbContext`; add Users mediator tests for happy and failure paths. |
| GymManagement module | Member, staff, equipment, settings, and maintenance mediator contracts and handlers | Move member, maintenance, staff, equipment, and gym-user workflows from shared BLL into module application handlers. |
| Training module | Training category handlers own orchestration directly; sessions/bookings are mediated | Move sessions, bookings, and attendance into module-owned handlers. |
| MembershipFinance module | Membership package handlers own orchestration directly; memberships/payments are mediated | Move membership sale/status and payments into module-owned handlers. |
| Data ownership | `module-boundaries.md` documents entity ownership | Add architecture tests or repository facades that prevent accidental cross-module table access. |
| API stability | Route metadata tests lock public route templates | Preserve route and DTO compatibility while moving internals. |
| Deployment continuity | Same WebApp host and client build remain active | Treat module migration as internal refactoring; no public URL changes. |

## Known Architecture Debt

The following direct EF or EF-abstraction usage is still intentional technical
debt. Remove it one slice at a time and add an architecture test for each
migrated boundary.

Presentation-layer concrete `AppDbContext`:
- migrated for MVC `HomeController`, `WorkspaceSwitcherViewComponent`,
  `ProfileController`, `MaintenanceController`, and Admin package/category page
  services
- guarded by `Final1PresentationBoundaryTests`

BLL or module `IAppDbContext` usage:
- `AccountAuthService`
- `IdentityService`
- `PlatformService`
- `StaffWorkflowService`
- `MemberWorkspaceService`
- `BookingPricingService`
- `SubscriptionTierLimitService`
- `CurrentActorResolver`
- `ResourceAuthorizationChecker`
- `GymResolutionMiddleware`
- `Modules.Users.Application.Auth.UsersSessionService`

Priority order:
1. Presentation-layer direct `AppDbContext` in MVC Client and Home/workspace
   switching, because graders can inspect these quickly.
2. Users auth/session repository boundary, because it improves both Final1 and
   Final2.
3. Staff, member workspace, and platform services, because they carry broad
   business behavior.
4. Smaller query helpers such as booking pricing, actor resolution, and
   subscription tier counts.

## Recommended Work Order

Before any implementation batch:
1. Run `dotnet format multi-gym-management-system.slnx --verify-no-changes`.
2. Run `dotnet build multi-gym-management-system.slnx`.
3. Run `dotnet test multi-gym-management-system.slnx`.
4. Run `cd client && npm test && npm run build`.
5. Check `git status --short` and avoid mixing unrelated changes.

Final1 hardening order:
1. Keep the new presentation-boundary tests green.
2. Run PostgreSQL/Testcontainers tests on a Docker-capable machine.
3. Run a public deployment smoke test before claiming live availability.
4. Avoid adding optional admin workflows for Final1 unless a defect requires it.

Final2 hardening order:
1. Move Users session queries to repository contracts and add Users mediator
   tests.
2. Move GymManagement member workflow logic from shared BLL into module
   handlers.
3. Move Training session/booking/attendance handlers into the Training module.
4. Move MembershipFinance membership/payment handlers into the
   MembershipFinance module.
5. Move maintenance/facility and staff workflows into GymManagement.
6. Tighten architecture tests so newly migrated module handlers cannot inject
   shared workflow services or `IAppDbContext`.

## Validation Evidence Policy

Only record a validation result when the command was actually run.

Latest Final1 validation recorded in the docs is from 2026-05-19:
- `dotnet build multi-gym-management-system.slnx --no-restore` passed
- `dotnet test multi-gym-management-system.slnx --no-restore` passed with 202
  passed and 3 skipped PostgreSQL/Testcontainers tests
- `cd client && npm test` passed with 32 tests
- `cd client && npm run build` passed
- Docker was unavailable, so PostgreSQL/Testcontainers and live deployment
  smoke checks remain unverified
