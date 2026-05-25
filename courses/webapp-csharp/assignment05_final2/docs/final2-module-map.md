# Final2 Module Map (Phase 1 inventory)

This document is the Phase 1 deliverable for the Final2 modular-monolith
refactor. It is an **inventory only** — no code has moved yet. Every type
listed below still lives in the existing layered `App.*` projects. The map
records the target module that will *own* each piece once Phases 4–10 are
executed.

Target modules (per `assignment05_final2_codex_prompts.md`):

- `Modules.Users`
- `Modules.Gyms`
- `Modules.Memberships`
- `Modules.Training`
- `Modules.Maintenance`

Shared projects that all modules may depend on:

- `Shared.Contracts` — public DTOs and module API abstractions (`IUsersModuleApi`,
  `IGymsModuleApi`, …)
- `SharedKernel` — reusable base abstractions (currently spread across
  `Base.Contracts`, `Base.Domain`, `Base.Helpers`, `App.Domain/Common`,
  `App.Domain/Security`)
- `App.Resources` — `.resx` UI translations (kept as-is)

Conventions:

- Paths are relative to `courses/webapp-csharp/assignment05_final2/`.
- "Today" column = current location (Final1-style layered layout).
- "Target module" column = where ownership moves in later phases.
- Items marked **shared** stay outside any single module.

---

## 1. Entities and identity types

| Type | Today (file) | Target module |
| --- | --- | --- |
| `AppUser` | `App.Domain/Identity/AppUser.cs` | Users |
| `AppRole` | `App.Domain/Identity/AppRole.cs` | Users |
| `AppRefreshToken` | `App.Domain/Identity/AppRefreshToken.cs` | Users |
| `Gym` | `App.Domain/Entities/Gym.cs` | Gyms |
| `GymSettings` | `App.Domain/Entities/GymSettings.cs` | Gyms |
| `GymContact` | `App.Domain/Entities/GymContact.cs` | Gyms |
| `Contact` | `App.Domain/Entities/Contact.cs` | Gyms |
| `AppUserGymRole` | `App.Domain/Entities/AppUserGymRole.cs` | Gyms *(see Risk-fix 5: store `UserId` scalar, no `AppUser` navigation)* |
| `Person` | `App.Domain/Entities/Person.cs` | Memberships |
| `PersonContact` | `App.Domain/Entities/PersonContact.cs` | Memberships |
| `Member` | `App.Domain/Entities/Member.cs` | Memberships |
| `Membership` | `App.Domain/Entities/Membership.cs` | Memberships |
| `MembershipPackage` | `App.Domain/Entities/MembershipPackage.cs` | Memberships |
| `Payment` | `App.Domain/Entities/Payment.cs` | Memberships |
| `Staff` | `App.Domain/Entities/Staff.cs` | Training |
| `TrainingCategory` | `App.Domain/Entities/TrainingCategory.cs` | Training |
| `TrainingSession` | `App.Domain/Entities/TrainingSession.cs` | Training |
| `Booking` | `App.Domain/Entities/Booking.cs` | Training |
| `Equipment` | `App.Domain/Entities/Equipment.cs` | Maintenance |
| `EquipmentModel` | `App.Domain/Entities/EquipmentModel.cs` | Maintenance |
| `MaintenanceTask` | `App.Domain/Entities/MaintenanceTask.cs` | Maintenance |
| Common base types (`LangStr`, audit fields, etc.) | `App.Domain/Common/`, `Base.Domain/` | **SharedKernel** |
| Security primitives (claims/policies/role names) | `App.Domain/Security/`, `App.Domain/RoleNames.cs` | **SharedKernel** (constants) + Users (issuance) |
| Enums | `App.Domain/Enums/` | follow owning entity |

---

## 2. EF Core configurations and persistence

| Today | Splits into | Target module(s) |
| --- | --- | --- |
| `App.DAL.EF/AppDbContext.cs` | legacy runtime persistence bridge during Phase 9 | shared until Phase 10 removes App.* dependency center |
| `Modules.Users/Infrastructure/Persistence/UsersDbContext.cs` | `Modules.Users` | Users/identity/refresh-token persistence boundary |
| `Modules.Gyms/Infrastructure/Persistence/GymsDbContext.cs` | `Modules.Gyms` | gym/settings/user-role persistence boundary |
| `Modules.Memberships/Infrastructure/Persistence/MembershipsDbContext.cs` | `Modules.Memberships` | member/membership/payment persistence boundary |
| `Modules.Training/Infrastructure/Persistence/TrainingDbContext.cs` | `Modules.Training` | staff/training/booking persistence boundary |
| `Modules.Maintenance/Infrastructure/Persistence/MaintenanceDbContext.cs` | `Modules.Maintenance` | equipment/maintenance-task persistence boundary |
| `App.DAL.EF/Configurations/IdentityAndPlatformConfigurations.cs` | identity vs. gym/platform halves | Users + Gyms |
| `App.DAL.EF/Configurations/FacilityAndMaintenanceConfigurations.cs` | equipment + maintenance | Maintenance (+ Gyms for facility/gym links) |
| `App.DAL.EF/Configurations/PeopleAndStaffConfigurations.cs` | people/member vs. staff | Memberships + Training |
| `App.DAL.EF/Configurations/TrainingAndMembershipConfigurations.cs` | sessions/bookings vs. memberships/payments | Training + Memberships |
| `SharedKernel/Persistence/IGymContext.cs` | active tenant context contract shared by WebApp, modules, and the legacy EF bridge | **SharedKernel** |
| `App.DAL.EF/Seeding/AppDataInit*.cs` | per-module seeding | each module seeds its own data (composition stays in WebApp) |
| `App.DAL.EF/Migrations/` | retained during Phase 9 to keep existing deployment migration path working | legacy runtime bridge |
| `App.DAL.EF/AppUOW.cs` | replaced by per-module UoW or module application services | each module |

Phase 9 adds module-owned DbContext boundaries with default schemas:
`users`, `gyms`, `memberships`, `training`, and `maintenance`. The current
runtime migration path still uses `AppDbContext` as a compatibility bridge so
existing deployment and seed data behavior stay stable. Removing the old
`AppDbContext`/`IAppUnitOfWork` center is the Phase 10 dependency cleanup.
Phase 10f moved shared module DbContext plumbing (`IGymContext`,
`ModuleDbContextBase<TContext>`, and `AddModuleDbContext<TContext>`) from
`App.DAL.EF` to `SharedKernel/Persistence`; active repositories still use the
legacy bridge until module migrations and schema/table cutover are ready.

### Repository interfaces

| Interface | Target module |
| --- | --- |
| `IAppUnitOfWork` | split per module |
| `Modules.Users/Application/Persistence/IRefreshTokenRepository` | Users |
| `Modules.Gyms/Application/Persistence/IAuthorizationQueryRepository` | Gyms *(reads `AppUserGymRole`)* |
| `Modules.Memberships/Application/Persistence/IMemberRepository` | Memberships |
| `Modules.Memberships/Application/Persistence/IMembershipRepository` | Memberships |
| `Modules.Memberships/Application/Persistence/IMembershipPackageRepository` | Memberships |
| `Modules.Memberships/Application/Persistence/IPaymentRepository` | Memberships |
| `Modules.Training/Application/Persistence/ITrainingCategoryRepository` | Training |
| `Modules.Training/Application/Persistence/ITrainingSessionRepository` | Training |
| `Modules.Training/Application/Persistence/IBookingRepository` | Training |
| `Modules.Maintenance/Application/Persistence/IMaintenanceRepository` | Maintenance |
| `IRepository<,>` (generic base) | **SharedKernel** |

### Repository implementations

Each `Ef*Repository` moves to the same module as its matching contract above.
Phase 10d moved the Users, Gyms, Training, Maintenance, and Memberships
repository contracts into module-owned `Application/Persistence/` folders and
removed those repositories from the transitional `IAppUnitOfWork`; the
implementations remain backed by the active shared `AppDbContext` until the
module migration/cutover step.

---

## 3. BLL service contracts and implementations

`App.BLL.Contracts/Services/` remains the transitional contract location.
Concrete implementations now live in the owning module or WebApp presentation
query folder.

| Service | Target module |
| --- | --- |
| `IAccountAuthService` / `AccountAuthService` | Users |
| `IIdentityService` / `IdentityService` | Users |
| `ITokenService` / `TokenService` | Users |
| `IUserContextService` / `UserContextService` | Users |
| `ICurrentActorResolver` / `CurrentActorResolver` | Users |
| `IAuthorizationService` / `AuthorizationService` | Gyms *(tenant role authorization)* |
| `ITenantAccessChecker` / `TenantAccessChecker` | Gyms |
| `IResourceAuthorizationChecker` / `ResourceAuthorizationChecker` | Gyms |
| `IWorkspaceContextService` / `WorkspaceContextService` | Gyms |
| `IPlatformService` / `PlatformService` | Gyms *(system/platform analytics)* |
| `ISubscriptionTierLimitService` / `SubscriptionTierLimitService` | Gyms |
| `IMembershipService` / `MembershipService` | Memberships |
| `IMembershipPackageService` / `MembershipPackageService` | Memberships |
| `IMembershipWorkflowService` / `MembershipWorkflowService` (+ `MembershipWorkflowMapping`) | Memberships |
| `IMemberWorkflowService` / `MemberWorkflowService` | Memberships |
| `IMemberWorkspaceService` / `MemberWorkspaceService` | Memberships |
| `IPaymentService` / `PaymentService` | Memberships |
| `ITrainingWorkflowService` / `Modules.Training.Application.TrainingWorkflowService` | Training |
| `IStaffWorkflowService` / `Modules.Training.Application.StaffWorkflowService` | Training |
| `IBookingPricingService` / `Modules.Training.Application.BookingPricingService` | Training |
| `IMaintenanceWorkflowService` / `Modules.Maintenance.Application.MaintenanceWorkflowService` | Maintenance |
| `WebApp/Areas/Admin/Queries/AdminOperationsQueryService` | WebApp presentation query over Maintenance |
| `WebApp/Areas/Admin/Queries/AdminSessionsQueryService` | WebApp presentation query over Training |
| `WebApp/Areas/Client/Queries/ClientDashboardQueryService` | WebApp presentation query over Training/Maintenance |
| `WebApp/Areas/Client/Queries/ClientSessionsQueryService` | WebApp presentation query over Training |
| module `Application/Mappers/*` | follow owning module. Users auth, Memberships finance/member, Training, and Maintenance mappers now live in owning modules. |
| `App.BLL/Exceptions/*` | **SharedKernel** *(cross-cutting)* |
| `IAppDbContext` (`App.BLL.Contracts/Infrastructure/IAppDbContext.cs`) | transitional EF-shaped contract until Phase 10 removes direct DbContext consumers |
| `UserExecutionContext` (`App.BLL.Contracts/Services/UserExecutionContext.cs`) | **SharedKernel** *(cross-cutting request context)* |

Phase 10d moved `IAppDbContext` from the `App.BLL` implementation project to
`App.BLL.Contracts` so modules and persistence no longer need a direct
`App.BLL` project reference. This is intentionally transitional because the
interface still exposes EF `DbSet<App.Domain...>` types; it can only become a
proper shared abstraction after entity ownership and repository contracts are
split.

Phase 10e moved the remaining concrete `App.BLL` implementations out of the
`App.BLL` project. `App.BLL` is now only a compatibility project retained until
Phase 10f cleanup.

---

## 4. API controllers

Final2 phases relocate controllers into module assemblies while preserving route
templates. `WebApp` still composes those module assemblies through
`AddXxxModule(...)`.

All routes are preserved verbatim. Tenant controllers all sit under
`api/v{version}/{gymCode}`.

### Identity

| Controller | Route | Target module |
| --- | --- | --- |
| `AccountController` | `api/v{version}/account` | Users |

### System / platform

| Controller | Route | Target module |
| --- | --- | --- |
| `System/GymsController` | `api/v{version}/system/gyms` | Gyms |
| `System/PlatformController` | `api/v{version}/system/platform` | Gyms |

### Tenant (per gym)

| Controller | Route segment under `{gymCode}` | Target module |
| --- | --- | --- |
| `GymSettingsController` | `gym-settings` | Gyms |
| `GymUsersController` | `gym-users` | Gyms |
| `MembersController` | `members` | Memberships |
| `MembershipsController` | `memberships` | Memberships |
| `MembershipPackagesController` | `membership-packages` | Memberships |
| `PaymentsController` | `payments` | Memberships |
| `MemberWorkspaceController` | `member-workspace` | Memberships |
| `StaffController` | `staff` | Training |
| `TrainingCategoriesController` | `training-categories` | Training |
| `TrainingSessionsController` | `training-sessions` | Training |
| `BookingsController` | `bookings` | Training |
| `EquipmentController` | `equipment` | Maintenance |
| `EquipmentModelsController` | `equipment-models` | Maintenance |
| `MaintenanceTasksController` | `maintenance-tasks` | Maintenance |
| `ApiControllerBase` | base class | **SharedKernel** (or WebApp/composition) |

---

## 5. MVC areas

### Admin (`WebApp/Areas/Admin`)

| Controller / view folder | Target module |
| --- | --- |
| `DashboardController` + `Views/Dashboard` | Gyms |
| `GymsController` + `Views/Gyms` | Gyms |
| `OperationsController` + `Views/Operations` | Gyms |
| `MembersController` + `Views/Members` | Memberships |
| `MembershipsController` + `Views/Memberships` | Memberships |
| `MembershipPackagesController` + `Views/MembershipPackages` | Memberships |
| `SessionsController` + `Views/Sessions` | Training |
| `TrainingCategoriesController` + `Views/TrainingCategories` | Training |
| `Areas/Admin/Services/*` | follow the matching controller's module |

Phase 11 will enforce ViewModels-only and remove all `ViewBag`/`ViewData`.

### Client (`WebApp/Areas/Client`)

| Controller / view folder | Target module |
| --- | --- |
| `DashboardController` + `Views/Dashboard` | Memberships *(member dashboard)* |
| `ProfileController` + `Views/Profile` | Users *(profile)* + Memberships *(person/contact data)* — keep in Memberships; call `IUsersModuleApi` for identity bits |
| `SessionsController` + `Views/Sessions` | Training |
| `MaintenanceController` + `Views/Maintenance` | Maintenance |
| `Areas/Client/Services/*` | follow the matching controller's module |

---

## 6. React client routes (`client/app`)

| Route | Page component | Target module (API owner) |
| --- | --- | --- |
| `/login` | `pages/LoginPage.tsx` | Users |
| `/` (role landing redirect) | `App.tsx` | Users (uses identity claims) |
| `/members` | `pages/MembersPage.tsx` | Memberships |
| `/membership-packages` | `pages/MembershipPackagesPage.tsx` | Memberships |
| `/member-workspace` | `pages/MemberWorkspacePage.tsx` | Memberships |
| `/sessions` | `pages/SessionsPage.tsx` | Training |
| `/attendance` | `pages/AttendancePage.tsx` | Training |
| `/training-categories` | `pages/TrainingCategoriesPage.tsx` | Training |
| `/maintenance` | `pages/MaintenanceTasksPage.tsx` | Maintenance |
| `components/AppShell.tsx`, `components/NoticeBanner.tsx` | shell | shared (no module ownership) |
| `lib/*` (api client, auth helpers) | shared | shared (consumes module DTOs) |

Existing client tests:
- `App.test.tsx` — shell
- `pages/CrudPages.test.tsx` — Memberships + Training CRUD
- `pages/SessionsPage.test.tsx` — Training
- `pages/OperationsPages.test.tsx` — Gyms (platform/admin ops)

---

## 7. DTOs (`App.DTO`)

`App.DTO` will be sliced per module in later phases. Public DTOs that cross
module boundaries become `Shared.Contracts` types; module-internal request/
response types live in `Modules.<Name>/Application`. Phase 1 takes no action —
the existing single `App.DTO` project is still in use.

---

## 8. Cross-cutting / shared

| Concern | Today | Target |
| --- | --- | --- |
| JWT issuance / validation | `Modules.Users/Application/TokenService.cs`, WebApp `Setup` | Users (issuance) + WebApp (auth scheme registration) |
| Tenant resolution middleware | `Modules.Gyms/Infrastructure/GymResolutionMiddleware.cs`, `SharedKernel/Persistence/IGymContext.cs` | Gyms (resolution) + SharedKernel/WebApp (active tenant context composition) |
| Localization (`.resx`) | `App.Resources` | **stays as `App.Resources`** (every module may depend on it) |
| Localization (DB `LangStr`) | `App.Domain/Common`, `Base.Domain` | **SharedKernel** (merge/fallback helpers — see Risk-fix 7) |
| CORS configuration | `WebApp/Setup`, `Program.cs` | WebApp |
| Swagger / API versioning | `WebApp/ConfigureSwaggerOptions.cs`, `WebApp/Setup` | WebApp |
| Base abstractions (`IDomainEntityId`, `BaseEntity`, etc.) | `Base.Contracts`, `Base.Domain`, `Base.Helpers` | **SharedKernel** (merge in Phase 10) |

---

## 9. Module-API surface to be defined in `Shared.Contracts`

Phase 1 does **not** create these interfaces — it only declares intent so
later phases can refer back to this list.

| Interface | Purpose | Owner |
| --- | --- | --- |
| `IUsersModuleApi` | Lookup user identity (name, roles) by `UserId`; issue auth tokens | Users |
| `IGymsModuleApi` | Resolve gym access (`ResolveAccessAsync(userId, gymCode)`), check tenant role, list gyms for user | Gyms |
| `IMembershipsModuleApi` | Resolve member by user + gym, fetch membership status snapshot | Memberships |
| `ITrainingModuleApi` | Resolve staff and training-session summaries without exposing Training entities | Training |
| (Maintenance) | Likely no outward API in MVP; consumed only via mediator notifications | Maintenance |

---

## 10. Architecture safety net (Phase 1 scope)

- Add an `Architecture.Tests` project (or fold tests into the existing
  `WebApp.Tests/Architecture/` folder) that:
  - Verifies the rules listed in Risk-fix 3 once `Modules.*` projects exist.
  - Currently allows legacy `App.*` references because nothing has moved yet.
- Keeps the existing `Final1PresentationBoundaryTests` enforcing that MVC
  controllers, view components, and area services do not inject `AppDbContext`.

Phase 1 does **not** create any `Modules.*` project, does **not** move any
file, and does **not** change runtime behavior.
