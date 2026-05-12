# Final1 / Final2 Gap Audit

## Audit Metadata

- **Date:** 2026-05-11
- **Branch / commit:** `main` @ `e548b50` (`feat(webapp-csharp/a03): finalize defense readiness`)
- **Working tree:** dirty (many `M docs/*.md` edits pending); no production-code modifications were made by this audit.
- **Commands run (all succeeded):**
  - `dotnet build multi-gym-management-system.slnx` → `Build succeeded. 0 Warning(s) 0 Error(s)` (10s).
  - `dotnet test multi-gym-management-system.slnx --no-build` → `Passed! Failed: 0, Passed: 250, Skipped: 3, Total: 253` (10s). Skipped tests are the Postgres testcontainer suite (`PostgreSqlPersistenceTests.*`) gated by `RequiresDockerFactAttribute`.
  - `dotnet format multi-gym-management-system.slnx --verify-no-changes` → exit 0 (no formatting drift).
  - Source greps for `IAppDbContext`, `AppDbContext`, `Microsoft.EntityFrameworkCore`, `ViewBag|ViewData`, `[HttpPost]`, `ValidateAntiForgeryToken`, `Authorize|AllowAnonymous`, `IMediator|IRequest|IRequestHandler`, `HasQueryFilter`, etc.
- **Commands failed / skipped:**
  - Postgres testcontainer suite (3 skipped). Not exercised because Docker was not invoked.
  - Vitest (`npm test` in `client/`) was not invoked in this audit; React inventory below is from file listings only.
  - No `npm run build` was executed.
- **Important limitations:**
  - No live environment was started; CSRF, auth, deployment, and pipeline behavior are inferred from source + tests.
  - The audit is read-only: I did not edit, format, or run code generators.
  - Reflection-based architecture tests guard much of the boundary surface; I relied on the existing tests as ground truth where they were specific.

---

## Executive Summary

- **Final1 readiness (CLEAN/ONION):** PARTIAL — defensible, but with named leaks.
- **Final2 readiness (Modular Monolith):** PARTIAL — module skeleton + mediator are real, but most module handlers are still pass-throughs over shared `App.BLL.Services.*WorkflowService` and one shared `AppDbContext`.

Top 10 blockers, ordered by grading risk:

1. **MVC Client controllers (`ProfileController`, `MaintenanceController`) inject `AppDbContext` directly** — `src/WebApp/Areas/Client/Controllers/ProfileController.cs:14`, `…/MaintenanceController.cs:16`. Directly contradicts Clean/Onion claims for the MVC client surface; not covered by `ArchitectureTests.AdminMvcControllers_AreThinAndDoNotDependOnDbContext` (Admin only).
2. **WebApp `HomeController` and `WorkspaceSwitcherViewComponent` inject `AppDbContext`** — `src/WebApp/Controllers/HomeController.cs:17`, `src/WebApp/ViewComponents/WorkspaceSwitcherViewComponent.cs:14`. Cookie-based login/gym-switch flow lives in the presentation layer, bypassing the auth slice.
3. **Two Admin page services still inject `AppDbContext`** — `AdminMembershipPackagesPageService` (`AdminViewModelServices.cs:359`) and `AdminTrainingCategoriesPageService` (`AdminViewModelServices.cs:563`). Architecture-tested whitelist (`MigratedAdminPageServices_DoNotDependOnDbContext`) only covers `AdminOperationsPageService` and `AdminSessionsPageService`.
4. **9 BLL services still take `IAppDbContext` and call EF directly** — `IdentityService`, `AccountAuthService`, `PlatformService`, `MemberWorkspaceService`, `StaffWorkflowService`, `CoachingPlanService`, `BookingPricingService`, `SubscriptionTierLimitService`, `CurrentActorResolver`, `ResourceAuthorizationChecker` (+ `Modules.Users.Application.Auth.UsersSessionService` and `WebApp.Middleware.GymResolutionMiddleware`). `App.BLL.csproj` still references `Microsoft.EntityFrameworkCore` (allowed by the `BllAssembly_DoesNotReferenceEfCoreProviderOrRelational` test, but the EF abstractions dependency remains).
5. **Modules are skeletons over shared BLL.** Of ~70 mediator handlers, only Training-category and Membership-package handlers do real work via `IAppUnitOfWork` + module mapper; everything else (members, sessions, bookings, maintenance, equipment, opening hours, memberships, payments, finance, gym users, gym settings) is a 1–3 line wrapper over `I*WorkflowService` from `App.BLL.Services`.
6. **`Modules.Users.Application.Auth.UsersSessionService` depends on `IAppDbContext`** — `src/Modules.Users/Application/Auth/UsersSessionService.cs:33`. The only "real" module service still reaches into the shared DbContext for `AppUserGymRoles` and `Gyms`. There is no Users-owned repository.
7. **All four module projects reference `App.BLL`, `App.Domain`, and `App.DTO`** — `Modules.Users/Modules.Users.csproj:18` (and three identical .csproj files). The architecture tests only block module-to-module references; they do *not* block module dependency on shared BLL, which is the whole point of Final2.
8. **`AppDbContext` is centralized; no schema separation, no per-module DbContext.** `IAppDbContext` exposes 30+ `DbSet<>`s for every owned entity. Ownership is documented (`docs/module-data-ownership.md`) but not enforced in code beyond logical convention. Acceptable, but state it as a conscious design choice in defense.
9. **No automated tests cover MVC Admin authorization / IDOR for migrated mutate paths.** `AdminMembersCrudTests`, `AdminMembershipPackagesCrudTests`, `AdminTrainingCategoriesCrudTests` exist but the existing `TenantIsolationAndIdorTests` is API-only — no MVC Admin tenant-isolation regression for the new Create/Edit/Delete flows beyond what the Crud tests assert.
10. **Admin UX is functionally CRUD-light:** Members, MembershipPackages, and TrainingCategories have full CRUD. Memberships, Sessions, Operations (equipment/opening hours/maintenance), and Gyms list pages are read-only. Sessions/Bookings/Memberships sales/Staff are administered only via the React SPA and the API — not via MVC. Defensible as "Full Admin UX" only if you explicitly scope MVC Admin to the three migrated CRUD areas.

---

## Final1 CLEAN/ONION Requirements Matrix

| # | Requirement | Status | Evidence | Missing / Weak Area | Risk | Recommended Fix Phase |
|---|---|---|---|---|---|---|
| 1 | Project direction: Domain → none; DTO → Domain; BLL → Domain+DTO; DAL.EF → BLL+Domain; WebApp → composition root | PASS | `App.Domain/App.Domain.csproj` references nothing except `Microsoft.AspNetCore.Identity.EntityFrameworkCore`; `App.DTO.csproj` only references `App.Domain`; `App.BLL.csproj` references `App.Domain`+`App.DTO`; `App.DAL.EF.csproj` references `App.BLL`+`App.Domain`; `WebApp.csproj` references everything. `ArchitectureTests.DomainAssembly_DoesNotReferenceForbiddenAssemblies`, `DtoAssembly_DoesNotReferenceForbiddenAssemblies`, `BllAssembly_DoesNotReferenceDalOrWebApp`, `DalEfAssembly_DoesNotReferenceWebApp`. | Domain still pulls `Microsoft.AspNetCore.Identity.EntityFrameworkCore` because `AppUser : IdentityUser<Guid>`, `AppRole : IdentityRole<Guid>`. Common in ASP.NET Identity, defensible. | Low | — |
| 2 | Domain purity (no EF, no Web, no Infrastructure) | PARTIAL | Pure POCOs except `App.Domain/Identity/AppUser.cs:7` and `AppRole.cs` inheriting from `IdentityUser<Guid>`/`IdentityRole<Guid>`. No `Microsoft.EntityFrameworkCore` in domain. `LangStr`, `TenantBaseEntity`, soft-delete/audit interfaces are clean. | Identity coupling is conventional but not strictly Onion. No anemic-domain explanation in code; business rules live almost entirely in BLL services. | Low | Defense: explicitly state "Identity-on-Domain is a deliberate ASP.NET Identity concession". |
| 3 | BLL does not directly use EF Core / `AppDbContext` | FAIL | `IAppDbContext` interface in `src/App.BLL/Contracts/Infrastructure/IAppDbContext.cs` is still injected into 9 BLL services (see "Remaining `IAppDbContext` usages" below). `App.BLL.csproj:13` references `Microsoft.EntityFrameworkCore`. | Auth, identity, platform, staff, coaching, member-workspace, booking-pricing, subscription-tier, current-actor, resource-authorization, all still talk to EF through `IAppDbContext`. Architecture test `BllAssembly_DoesNotReferenceEfCoreProviderOrRelational` explicitly grandfathers the EF abstractions package. | High | Phases below: one slice per phase (auth → platform → staff → coaching → identity-register → member-workspace → resource/current-actor → subscription-tier). |
| 4 | Repositories + UnitOfWork exist and are consistently used | PARTIAL | `IAppUnitOfWork`, `IRepository<,>` and 13 specialized repositories in `App.BLL/Contracts/Persistence/`; EF implementations in `App.DAL.EF/Repositories/`. Architecture test `RepositoryAndUnitOfWork_AreDeclaredInBllContractsPersistence` enforces location. `MemberWorkflowService`, `MembershipPackageService`, `MembershipService`, `PaymentService`, `FinanceWorkspaceService`, `TrainingWorkflowService`, `MaintenanceWorkflowService` use the UoW. | Services in row 3 still bypass via `IAppDbContext`. Mixed pattern. `IClientSessionsQueryService` and `IClientDashboardQueryService` are query-only services that consume `IAppUnitOfWork` (acceptable). | Medium | Removing each `IAppDbContext` user closes this gap. |
| 5 | Web/API: thin, DTO/service/mediator-only, ProblemDetails | PARTIAL | API controllers in `src/WebApp/ApiControllers/` consume `IMediator`, `IStaffWorkflowService`, `ICoachingPlanService`, `IMemberWorkspaceService`, `IPlatformService`. None inject `DbContext`/`IAppDbContext`. Enforced by `ArchitectureTests.ApiControllers_DoNotDependOnDbContext`. `WebApp/Middleware/ProblemDetailsMiddleware.cs` is in the pipeline (`MiddlewareExtensions.cs:18`). | `IPlatformService`, `IStaffWorkflowService`, `ICoachingPlanService`, `IMemberWorkspaceService` are still shared BLL services bound to `IAppDbContext`. API stays thin but service layer is impure. | Medium | Same fix as row 3. |
| 6 | MVC: no direct EF, no ViewBag/ViewData, anti-forgery, strong view models | PARTIAL | No `ViewBag`/`ViewData` anywhere under `src/WebApp/`. All Admin POST actions have `[ValidateAntiForgeryToken]` (`MembersController.cs:44,86,130`; `MembershipPackagesController.cs:47,89,133`; `TrainingCategoriesController.cs:43,85,129`; Client `SessionsController.cs:35,49,74`; Client `MaintenanceController.cs:80`; Home `Login/Logout/SetCulture/SwitchGym/SwitchRole`). All Admin controllers carry `[Authorize]`. Strong view models in `WebApp/Models/`. | Direct EF leaks: `ProfileController` (line 14), `MaintenanceController` (line 16), `HomeController` (line 17), `WorkspaceSwitcherViewComponent` (line 14), and Admin page services `AdminMembershipPackagesPageService` (line 359) + `AdminTrainingCategoriesPageService` (line 563). | High (graders look here first) | One slice per controller/page-service migration. |
| 7 | Admin UX completeness — defensible "Full Admin UX" | PARTIAL | Admin CRUD pages: Members, MembershipPackages, TrainingCategories. Read-only Admin pages: Dashboard, Gyms, Operations (equipment/maintenance summary), Sessions, Memberships (packages + active list). Views exist under `src/WebApp/Areas/Admin/Views/`. | No Admin CRUD for: TrainingSessions, Bookings/attendance, Memberships sales/status changes, Equipment, OpeningHours, MaintenanceTasks, Staff/Contracts/Vacations/JobRoles, GymUsers, Subscriptions. These are React-only. The MVC Admin story is "three areas of full CRUD + reporting"; not "Full Admin UX". | High (defense framing) | Add one MVC Admin slice that mutates: either Memberships sales (recommended) or Equipment CRUD. |
| 8 | Validation: present, single-layer, tested | PARTIAL | `App.BLL.Exceptions.ValidationAppException` carries multi-error lists; controllers translate via `ProblemDetailsMiddleware`. MVC Admin services use `ModelState.IsValid` + `ValidationAppException` → form re-render with errors (`AdminViewModelServices.cs` `*OperationResult` types). Workflow services have `MembershipWorkflowMapping`, `MembershipPackageWorkflow`, `TrainingCategoryWorkflow.ValidateRequest`. | Validation rules live partly in DataAnnotations, partly inside `*WorkflowService`/`*Workflow` statics, partly inside mediator handlers (`TrainingCategoryHandlers`, `MembershipPackageHandlers`). No FluentValidation or single validation pipeline. Tests cover happy/sad paths per workflow service but no negative test of `ValidationProblemDetails` shape end-to-end for MVC. | Medium | Add a focused "ValidationAppException → ProblemDetails" integration test on one API endpoint and one MVC Admin POST. |
| 9 | Authorization + tenant isolation, IDOR tests | PARTIAL | `TenantAccessChecker` (BLL) and `ResourceAuthorizationChecker` (BLL) used everywhere. `AppDbContext.ConfigureTenantFilter<TEntity>` applies global query filters by `GymId` (`App.DAL.EF/AppDbContext.cs:152-164`). `[Authorize]` on every Admin/Client controller; `ApiControllerBase` carries auth. `TenantIsolationAndIdorTests` covers API IDOR. `MvcComplianceTests` exists. | IDOR coverage for newer MVC Admin CRUD (Members/MembershipPackages/TrainingCategories edit/delete by a different-tenant admin) is partly covered by `Admin*CrudTests` but does not assert "an Admin from tenant B cannot reach tenant A's edit page". | Medium-High | Add a single test class `AdminCrossTenantTests` with 3 cross-tenant probes (one per migrated area). |
| 10 | Testing breadth (unit / integration / arch / persistence / React) | PARTIAL | 250 passing tests, 3 skipped (postgres). Unit (~21 files), Integration (~17 files including `Final1CriticalE2ETests`), Architecture (`ArchitectureTests.cs`, `ModuleArchitectureTests.cs`), Persistence (`PostgreSqlPersistenceTests`, gated). React tests: `CrudPages.test.tsx`, `OperationsPages.test.tsx`, `SessionsPage.test.tsx`, `WorkspacePages.test.tsx`, `App.test.tsx`. | Postgres tests skipped in CI of this run (Docker not present); architecture rules are scoped (whitelists for migrated controllers/services); MVC tests focus on the migrated trio. | Medium | Wire `PostgreSqlPersistenceTests` for the defense environment; tighten architecture rules slice-by-slice. |

---

## Final1 Direct Dependency Findings

### Remaining `IAppDbContext` usages

Source: `Grep "IAppDbContext" *.cs` (deduplicated; production-code only; architecture-test references intentionally excluded).

| # | File | Class | Used in | Why it is a Clean/Onion risk | Suggested replacement | Priority |
|---|------|-------|---------|-------------------------------|------------------------|----------|
| 1 | `src/App.BLL/Services/IdentityService.cs:14` | `IdentityService` | `RegisterAsync` (`Gyms.OrderBy…FirstOrDefaultAsync`, `Members.Add`, `AppUserGymRoles.Add`, `SaveChangesAsync`), `SwitchGymAsync`, `SwitchRoleAsync`, `BuildJwtResponseAsync` (Gyms + AppUserGymRoles + RefreshTokens) | BLL service uses EF DbSet `Include`/`Where`/`FirstOrDefaultAsync` and `dbContext.SaveChangesAsync` directly — bypasses UoW. | New `IGymQueryRepository`/`IAppUserGymRoleRepository` on UoW; use existing `IRefreshTokenRepository`. Move registration into a Users-module command handler. | HIGH |
| 2 | `src/App.BLL/Services/AccountAuthService.cs:18` | `AccountAuthService` | Same auth surface as `IdentityService` plus refresh-token rotation. Uses `IAppUnitOfWork.RefreshTokens` for tokens but `IAppDbContext.AppUserGymRoles`/`Gyms` for tenant resolution. | Same as 1. Two services (`AccountAuthService` + `IdentityService`) overlap; one is now mediated via `Modules.Users.Application.Auth.AuthSessionHandlers`. | Add `IAppUserGymRoleRepository` (+ `IGymQueryRepository`) and migrate active-link resolution there. Eventually retire `IdentityService` once register flow has a mediator command. | HIGH |
| 3 | `src/App.BLL/Services/PlatformService.cs:21` | `PlatformService` | `RegisterGymAsync`, `GetGymsAsync`, `GetAnalyticsAsync`, `GetGymSnapshotAsync`, support/subscription queries — `Gyms`, `Subscriptions`, `SupportTickets`, `Members`, `TrainingSessions`, `MaintenanceTasks`, `AuditLogs`. | The system-admin/SaaS console reaches across nearly every aggregate via EF. | Two repositories: `IPlatformQueryRepository` (analytics + snapshot) and `ISupportTicketRepository`. RegisterGym is a Users/GymManagement cross-cutting command — best moved to a `GymManagement` mediator command. | HIGH |
| 4 | `src/App.BLL/Services/StaffWorkflowService.cs:16` | `StaffWorkflowService` | Staff/JobRole/EmploymentContract/Vacation reads + writes. | Big surface, EF-coupled, owned by GymManagement module but lives in shared BLL. | `IStaffRepository`/`IJobRoleRepository`/`IEmploymentContractRepository`/`IVacationRepository`, and move handlers into `Modules.GymManagement`. | HIGH |
| 5 | `src/App.BLL/Services/CoachingPlanService.cs:12` | `CoachingPlanService` | `CoachingPlans` + `CoachingPlanItems` (with deep `Include`s). | Single largest non-migrated workflow in Training; should be module-owned. | `ICoachingPlanRepository` + move into `Modules.Training` handlers. | MEDIUM |
| 6 | `src/App.BLL/Services/MemberWorkspaceService.cs:16` | `MemberWorkspaceService` | Cross-aggregate member workspace read (Members, Memberships, Bookings, Payments). | Read model that should be a query service over UoW or a module-owned query handler. | Convert to use existing `IMemberRepository`/`IMembershipRepository`/`IBookingRepository`/`IPaymentRepository`, or define an `IMemberWorkspaceQueryRepository`. | MEDIUM |
| 7 | `src/App.BLL/Services/BookingPricingService.cs:7` | `BookingPricingService` | `Memberships` query with `Include(MembershipPackage)` to compute price. | Pure read-side logic; trivial to move. | Use `IMembershipRepository.GetActiveForMemberAsync` (new method) and remove DbContext dependency. | MEDIUM |
| 8 | `src/App.BLL/Services/SubscriptionTierLimitService.cs:10` | `SubscriptionTierLimitService` | `Subscriptions`, plus `IgnoreQueryFilters().CountAsync` over `Members/Staff/TrainingSessions/Equipment`. | Counts entities by gym across `DbSet` references, using `IgnoreQueryFilters` + `EF.Property<>`. | New `ISubscriptionTierLimitRepository` exposing `CountAsync(gymId, resource)` per supported resource. | MEDIUM |
| 9 | `src/App.BLL/Services/CurrentActorResolver.cs:9` | `CurrentActorResolver` | Member/Staff lookup by `(GymId, PersonId)`. | Small but in the auth path; should not need DbContext. | Add `IMemberRepository.GetByGymPersonAsync` and `IStaffRepository.GetByGymPersonAsync` (the latter does not exist yet — add it). | MEDIUM |
| 10 | `src/App.BLL/Services/ResourceAuthorizationChecker.cs:11` | `ResourceAuthorizationChecker` | Booking/Member ownership checks. | Same shape as 9. | Reuse `IBookingRepository`/`IMemberRepository`; small refactor. | LOW–MEDIUM |
| 11 | `src/Modules.Users/Application/Auth/UsersSessionService.cs:33` | `UsersSessionService` | `AppUserGymRoles`, `Gyms` queries inside Users module. | This is the only "real" module-owned service and it still reaches into the shared DbContext rather than a repository. | Introduce `IAppUserGymRoleRepository` + `IGymQueryRepository` on the UoW (or on a Users-local UoW), inject in handlers. | HIGH (this is the Final2 story) |
| 12 | `src/WebApp/Middleware/GymResolutionMiddleware.cs:13` | `GymResolutionMiddleware` | Resolves `gymCode` route value to a `Gym` row. | Middleware uses BLL's `IAppDbContext`. Better than reaching into DAL but still couples the pipeline to EF abstractions. | Define `IGymContextResolver` in BLL or a focused `IGymQueryRepository.GetByCodeAsync`. | MEDIUM |

`ArchitectureTests` already encodes whitelists that **forbid** new `IAppDbContext` usage inside migrated services (`TenantAccessChecker`, `MemberWorkflowService`, `TrainingWorkflowService`, `MembershipPackageService`, `MembershipService`, `PaymentService`, `FinanceWorkspaceService`, `MaintenanceWorkflowService`, migrated Admin page services, Client page services). Add the next service to the whitelist as soon as it migrates.

### Remaining direct `AppDbContext` usages outside DAL

| # | File | Class | Layer | Risk | Suggested fix |
|---|------|-------|-------|------|----------------|
| 1 | `src/WebApp/Controllers/HomeController.cs:17` | `HomeController` | WebApp / MVC | Cookie auth flow and gym/role switching does EF directly in the controller. High visibility in grading; not covered by `AdminMvcControllers_AreThinAndDoNotDependOnDbContext` (Admin only). | Replace `AppDbContext` with `IMediator` + add Users-module `CookieLoginCommand`/`CookieSwitchGymCommand`/`CookieSwitchRoleCommand`. |
| 2 | `src/WebApp/ViewComponents/WorkspaceSwitcherViewComponent.cs:14` | `WorkspaceSwitcherViewComponent` | WebApp / MVC | Layout-level component reads `AppUserGymRoles` + `Gyms` directly. | Inject a `WorkspaceSwitcherViewModelBuilder` (or send a `GetWorkspaceLinksQuery` through `IMediator`); should never hold DbContext. |
| 3 | `src/WebApp/Areas/Client/Controllers/ProfileController.cs:14` | `ProfileController` | WebApp / MVC Client | Direct EF for member / memberships / bookings / payments. Bypasses BLL entirely. | Add `IClientProfilePageService` (mirror of `ClientDashboardPageService`) that consumes `IMemberRepository`/`IMembershipRepository`/`IBookingRepository`/`IPaymentRepository` via UoW. |
| 4 | `src/WebApp/Areas/Client/Controllers/MaintenanceController.cs:16` | `MaintenanceController` | WebApp / MVC Client | `dbContext.Equipment` lookup for label rendering. | Add `IMaintenanceRepository.GetEquipmentLabelAsync` or a thin `IClientMaintenancePageService`. |
| 5 | `src/WebApp/Areas/Admin/Services/AdminViewModelServices.cs:359` | `AdminMembershipPackagesPageService` | WebApp / Admin page service | Direct EF for index/find; mutations already go through `IMembershipPackageService`. | Add `IMembershipPackageRepository.ListAdminSummariesAsync`/`GetForAdminAsync` and remove DbContext. Add to the `MigratedAdminPageServices_DoNotDependOnDbContext` whitelist. |
| 6 | `src/WebApp/Areas/Admin/Services/AdminViewModelServices.cs:563` | `AdminTrainingCategoriesPageService` | WebApp / Admin page service | Direct EF for index/find. | Reuse `ITrainingCategoryRepository.ListByGymAsync` (already exists in UoW); inject UoW instead. |
| 7 | `src/WebApp/Setup/AppDataInitExtensions.cs:14` | DI bootstrap | WebApp / startup | `services.GetRequiredService<AppDbContext>()` to seed at boot. Acceptable for a composition root. | No fix needed; keep. |
| 8 | `src/WebApp/Setup/DatabaseExtensions.cs:21,39,41` | DI registration | WebApp / startup | `AddDbContext<AppDbContext>` and `AddDataProtection().PersistKeysToDbContext<AppDbContext>()`. | Acceptable; composition root is where DbContext belongs. |
| 9 | `src/WebApp/Setup/IdentitySetupExtensions.cs:23` | Identity DI | WebApp / startup | `.AddEntityFrameworkStores<AppDbContext>()`. | Acceptable. |

### EF Core package references outside DAL

| Project | EF / persistence package | Justified? | Risk | Suggested fix |
|---|---|---|---|---|
| `src/App.BLL/App.BLL.csproj` | `Microsoft.EntityFrameworkCore` 10.0.2 | Partially — exists so `IAppDbContext` can expose `DbSet<>` and so services can use EF query operators. Explicitly grandfathered by `ArchitectureTests.BllAssembly_DoesNotReferenceEfCoreProviderOrRelational`. | Medium. Strict Clean/Onion would remove this. | Remove once all 12 `IAppDbContext` users above are gone (then delete `IAppDbContext`). |
| `src/App.Domain/App.Domain.csproj` | `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.2 | Yes — Identity entity inheritance pulls it transitively. | Low. Conventional. | None. |
| `src/WebApp/WebApp.csproj` | `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore`, `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | Yes — composition root, migrations tools, dev exception page, data-protection keys. | Low. | None. |
| `src/App.DAL.EF/App.DAL.EF.csproj` | EF + Npgsql + Identity EF | Yes — this is the DAL. | — | — |
| `tests/WebApp.Tests/WebApp.Tests.csproj` | `Microsoft.EntityFrameworkCore.InMemory` | Yes — in-memory provider for `CustomWebApplicationFactory`. | Low. | None. |

Modules (`Modules.Users`, `Modules.GymManagement`, `Modules.Training`, `Modules.MembershipFinance`) do **not** reference EF Core packages directly. The only EF `using` in a module is `Modules.Users.Application.Auth.UsersSessionService.cs:14`, which transitively gets EF through `App.BLL`. That is a Final2 boundary smell, not a build break.

---

## Final2 Modular Monolith Requirements Matrix

| # | Requirement | Status | Evidence | Missing / Weak Area | Risk | Recommended Fix Phase |
|---|---|---|---|---|---|---|
| 1 | Module projects exist (Users, GymManagement, Training, MembershipFinance) | PASS | `src/Modules.Users`, `src/Modules.GymManagement`, `src/Modules.Training`, `src/Modules.MembershipFinance`. `ModuleArchitectureTests.EveryModule_ExposesExactlyOneIModuleMarker`, `EveryModule_ExposesAddModuleDIExtension`. | — | — | — |
| 2 | Modules have clear public APIs/contracts; no calls into another module's internals | PASS (cross-module rule) / PARTIAL (contract usefulness) | `ModuleArchitectureTests.EveryModule_DoesNotReferenceAnyOtherModule`, `NonUsersModules_DoNotReferenceUsersInternals`, `TrainingModule_DoesNotReferenceUsersOrGymManagementInternals`. Contracts in `Modules.*/Contracts/*Messages.cs`. | The "internal" workflows still live in shared BLL — the module *is* its contracts. There is no `Modules.Users.Domain` or `Modules.GymManagement.Application` ownership of entities. The forbidden-prefixes test passes vacuously for `Modules.Users.Domain`/`Modules.Users.Infrastructure` because neither directory exists. | Medium (defense framing) | — |
| 3 | Modules own real workflows (not pass-throughs) | PARTIAL | Real workflow ownership (handler uses UoW + module mapper directly): `Modules.Training.Application.{List,Create,Update,Delete}TrainingCategoryCommandHandler` and `Modules.MembershipFinance.Application.{List,Create,Update,Delete}MembershipPackageCommandHandler`. Guarded by `ModuleArchitectureTests.TrainingCategoryWorkflow_IsOwnedByTrainingModuleHandlers` and `MembershipPackageWorkflow_IsOwnedByMembershipFinanceModuleHandlers`. | Everything else in `Modules.GymManagement/Application/Members/MemberHandlers.cs`, `…/Maintenance/MaintenanceHandlers.cs`, `Modules.Training/Application/TrainingHandlers.cs`, `Modules.MembershipFinance/Application/FinanceHandlers.cs` is a 1–3 line wrapper over `App.BLL.Services.*WorkflowService`. `Modules.Users.Application.Auth.UsersSessionService` is module-internal but still uses `IAppDbContext`. | High (Final2 grading risk) | One slice per phase: move members, then sessions, then memberships, then maintenance, etc. |
| 4 | Module dependency direction (csproj) | PARTIAL | All four `Modules.*.csproj` reference `BuildingBlocks` + `App.Domain` + `App.DTO` + `App.BLL`. None reference each other, `App.DAL.EF`, or `WebApp` (verified). `ModuleArchitectureTests.EveryModule_DoesNotReferenceAnyOtherModule`. | The shared `App.BLL` reference is the structural reason workflows can still be wrappers. Acceptable as transitional; not acceptable as the final shape. | Medium | Long-term: move per-module BLL into `Modules.*/Application/`, leave `App.BLL` only as a host for shared contracts/mappers (or split into `BuildingBlocks` plus per-module). |
| 5 | Shared kernel / BuildingBlocks contains abstractions only | PASS | `src/BuildingBlocks/` contains `Mediator/{IMediator,IRequest,IRequestHandler,Mediator,MediatorRegistration}.cs`, `Modules/IModule.cs`, `BuildingBlocksServiceCollectionExtensions.cs`, and an empty `Contracts/README.md`. `BuildingBlocks.csproj` references nothing application-specific. `ModuleArchitectureTests.MediatorAbstractions_LiveInBuildingBlocks` and `BuildingBlocks_DoesNotReferenceAnyModuleOrWebApp`. | None. | Low | — |
| 6 | Cross-module communication via mediator (not direct service calls) | PASS for controllers, PARTIAL inside modules | API/MVC controllers consume `IMediator` (`MembersController`, `TrainingSessionsController`, `MembershipsController`, `MaintenanceTasksController`, `FinanceController`, `EquipmentController`, etc.). `AccountController` uses `IMediator` for login/refresh/logout/switch. | Inside `Modules.GymManagement.Application.Members.MemberHandlers` the handler still calls `App.BLL.Services.IMemberWorkflowService` directly. That is *not* cross-module mediator; it's intra-monolith via a shared service contract. | Medium | Same fix as row 3. |
| 7 | Data ownership (documented + enforced) | PARTIAL | Documented in `docs/module-data-ownership.md` (extensive entity → module map). Logically: Users owns `AppUser/AppRole/AppUserGymRole/AppRefreshToken/Person/Contact/PersonContact`; GymManagement owns `Gym/GymContact/GymSettings/OpeningHours/Equipment/MaintenanceTask/Staff/JobRole/EmploymentContract/Vacation/AuditLog/Subscription/SupportTicket`; Training owns `TrainingCategory/TrainingSession/Booking/WorkShift/CoachingPlan/CoachingPlanItem`; MembershipFinance owns `MembershipPackage/Membership/Payment/Invoice/InvoiceLine/InvoicePayment`. | Not enforced in code. `IAppDbContext` exposes every `DbSet<>` to anyone who can resolve it. No per-module schema, no per-module DbContext. Acceptable, but you can't currently say "module X owns these tables". | Medium (defense framing) | Per-module read-only `*QueryRepository`-style facades that scope access to owned tables. Keep one Postgres schema for now. |
| 8 | Module tests (architecture + boundary + workflow) | PARTIAL | `ArchitectureTests.cs` + `ModuleArchitectureTests.cs`. Mediator-level tests: `TrainingModuleMediatorTests`, `MembershipFinanceModuleMediatorTests`, `MaintenanceModuleMediatorTests`. Slice tests: `MembershipFinanceCleanSliceTests`. Architecture tests assert "this handler exists in this module's namespace" for several flows. | No tests for Users module mediator (login/refresh/switch); no tests asserting that pass-through handlers correctly delegate. No test that prevents a module from gaining a new dependency on another module's internal types beyond the existing whitelist. | Medium | Add `UsersModuleMediatorTests` and a test that fails if any `Modules.*/Application/*.cs` file injects `IAppDbContext`. |
| 9 | Composition root cleanly registers modules | PASS | `WebApp/Setup/ModuleExtensions.cs` calls `AddBuildingBlocks` + `AddUsersModule` + `AddGymManagementModule` + `AddTrainingModule` + `AddMembershipFinanceModule`. `WebApp/Program.cs` calls `AddAppModules`. `ModuleArchitectureTests.Mediator_IsResolvableFromCompositionRoot`. | DI registration of every BLL service still happens in `ServiceExtensions.cs` (`AddAppServices`) — the bulk of "modular" services are wired here, not by their module. `GymManagementModule`, `TrainingModule`, `MembershipFinanceModule` only register the mediator scan; comments explicitly note "services move into this module in Phase 18/19/20". | Medium | Move the relevant `services.AddScoped<I*WorkflowService, *WorkflowService>()` calls into each module's extension method as you migrate them. |
| 10 | Defense readiness: convincing Users + ≥2 business modules | PARTIAL | Users: login/refresh/logout/switch via `IMediator` → `Modules.Users.Application.Auth` (real module ownership). MembershipFinance: package CRUD genuinely owned + mediator. Training: training-category CRUD genuinely owned + mediator. | GymManagement is the weakest demo — only Members and Maintenance handlers; both wrap shared services. | Medium | For defense, lead with **Users (auth via mediator)**, **MembershipFinance (package CRUD)**, **Training (category CRUD)**. Mention GymManagement as the next slice. |

---

## Module-by-Module Findings

### Module: Users

- **Current responsibilities:** Cookie + JWT auth (login, refresh, logout), gym switching, role switching. Exposes contracts: `LoginCommand`, `RefreshSessionCommand`, `LogoutCommand`, `SwitchGymCommand`, `SwitchRoleCommand` (`Modules.Users/Contracts/AuthSessionMessages.cs`).
- **Workflows owned:** `Modules.Users.Application.Auth.UsersSessionService` implements `LoginAsync`, `LogoutAsync`, `RefreshAsync`, `SwitchGymAsync`, `SwitchRoleAsync` (the handlers in `AuthSessionHandlers.cs` are thin delegates over this service). DI registration: `AddScoped<IUsersSessionService, UsersSessionService>()`.
- **Workflows still in shared BLL/Web:**
  - `App.BLL.Services.IdentityService` (`RegisterAsync` for first-user registration, plus duplicate `SwitchGymAsync`/`SwitchRoleAsync` used by `AccountController`).
  - `App.BLL.Services.AccountAuthService` (parallel auth path, predates the module).
  - `WebApp.Controllers.HomeController` cookie login + gym/role switching for MVC.
  - `WebApp.ViewComponents.WorkspaceSwitcherViewComponent` workspace dropdown query.
- **Dependencies (csproj):** `BuildingBlocks`, `App.Domain`, `App.DTO`, `App.BLL`.
- **Boundary violations:** `UsersSessionService` uses `App.BLL.Contracts.Infrastructure.IAppDbContext` to query `AppUserGymRoles` + `Gyms` (no Users-owned repository). The duplicate auth paths in `App.BLL.Services.{IdentityService,AccountAuthService}` are not yet retired.
- **Tests:** No dedicated `UsersModuleMediatorTests`. Module wiring is exercised through `Final1CriticalE2ETests`, `AuthSecurityAndErrorTests`, and `ArchitectureTests.AccountAuthSlice_IsMediatedThroughUsersModule`.
- **Readiness:** PARTIAL.
- **Required next fixes:** (a) introduce `IAppUserGymRoleRepository` + `IGymQueryRepository` (or similar) and migrate `UsersSessionService` off `IAppDbContext`; (b) move `RegisterAsync` into a Users-module command; (c) replace `HomeController` + `WorkspaceSwitcherViewComponent` direct EF with mediator calls; (d) add `UsersModuleMediatorTests` for login/refresh/logout/switch happy/error paths.

### Module: GymManagement

- **Current responsibilities:** Members listing/CRUD (mediator → shared BLL), maintenance tasks, equipment, equipment models, opening hours + exceptions, gym settings, gym users.
- **Workflows owned:** None natively. All handlers delegate to `IMemberWorkflowService` (`Modules.GymManagement/Application/Members/MemberHandlers.cs`) and `IMaintenanceWorkflowService` (`Modules.GymManagement/Application/Maintenance/MaintenanceHandlers.cs`). DI: only `AddModuleMediatorHandlersFromAssembly`.
- **Workflows still in shared BLL/Web:** `App.BLL.Services.MemberWorkflowService` (uses UoW correctly), `App.BLL.Services.MaintenanceWorkflowService` (uses UoW correctly), `App.BLL.Services.StaffWorkflowService` (still on `IAppDbContext`, accessed only via `IStaffWorkflowService` directly in WebApp), `App.BLL.Services.PlatformService` (system-admin platform service, on `IAppDbContext`).
- **Dependencies (csproj):** `BuildingBlocks`, `App.Domain`, `App.DTO`, `App.BLL`.
- **Boundary violations:** None at the assembly level (architecture tests pass). Logical violation: all handlers pass through shared BLL; the module currently provides no domain logic of its own.
- **Tests:** `MaintenanceModuleMediatorTests` covers mediator dispatch. `AdminMembersCrudTests`, `MemberCrudTests`, `StaffWorkflowTests`, `Final1CriticalE2ETests` exercise the area indirectly.
- **Readiness:** PARTIAL.
- **Required next fixes:** (a) move `MemberWorkflowService` logic into module handlers (`ListMembersQueryHandler`, `CreateMemberCommandHandler`, …) consuming `IAppUnitOfWork` directly; (b) repeat for `MaintenanceWorkflowService`; (c) migrate `StaffWorkflowService` off `IAppDbContext` and into module handlers; (d) carve out `Subscription`/`SupportTicket`/`Gym` portions of `PlatformService` (the rest is system-admin reporting that may stay in WebApp).

### Module: Training

- **Current responsibilities:** Training categories (CRUD), training sessions (CRUD), bookings (create / cancel / attendance), work-shift list, with mediator entry points.
- **Workflows owned:** Training category CRUD — `Modules.Training.Application.TrainingCategoryHandlers.cs` uses `IAppUnitOfWork.TrainingCategories` + `ITrainingMapper` directly; `ModuleArchitectureTests.TrainingCategoryWorkflow_IsOwnedByTrainingModuleHandlers` enforces this.
- **Workflows still in shared BLL/Web:** Training sessions, bookings, work shifts, coaching plans all flow through `App.BLL.Services.TrainingWorkflowService` / `App.BLL.Services.CoachingPlanService`.
- **Dependencies (csproj):** `BuildingBlocks`, `App.Domain`, `App.DTO`, `App.BLL`.
- **Boundary violations:** Architecture passes; logical pass-through is the issue.
- **Tests:** `TrainingModuleMediatorTests`, `TrainingWorkflowServiceTests`, `TrainingCategoryLocalizationTests`, `AdminTrainingCategoriesCrudTests`.
- **Readiness:** PARTIAL — best business-module example after MembershipFinance.
- **Required next fixes:** Move `UpsertTrainingSessionAsync`, `CreateBookingAsync`, `UpdateAttendanceAsync`, `CancelBookingAsync` into module handlers backed by `ITrainingSessionRepository`/`IBookingRepository`. Then migrate `CoachingPlanService` off `IAppDbContext` (`ICoachingPlanRepository` does not yet exist).

### Module: MembershipFinance

- **Current responsibilities:** Membership packages (CRUD), memberships (sell / status / delete), payments (list / create), finance workspace queries.
- **Workflows owned:** Membership package CRUD — `Modules.MembershipFinance.Application.MembershipPackageHandlers.cs` uses `IAppUnitOfWork.MembershipPackages` + `IMembershipFinanceMapper` directly; `ModuleArchitectureTests.MembershipPackageWorkflow_IsOwnedByMembershipFinanceModuleHandlers` enforces this. Invoice command handlers (`CreateInvoiceCommand`, `PostInvoicePaymentCommand`, `PostInvoiceRefundCommand`) referenced by architecture tests.
- **Workflows still in shared BLL/Web:** Memberships sell/status/delete and Payments still go through `App.BLL.Services.MembershipWorkflowService` and `App.BLL.Services.FinanceWorkspaceService` (both consume UoW correctly, so Clean/Onion is fine — but they live outside the module).
- **Dependencies (csproj):** `BuildingBlocks`, `App.Domain`, `App.DTO`, `App.BLL`.
- **Boundary violations:** None at assembly level.
- **Tests:** `MembershipFinanceModuleMediatorTests`, `MembershipFinanceCleanSliceTests`, `MembershipWorkflowServiceTests`, `AdminMembershipPackagesCrudTests`, `MembershipPackageCrudTests`.
- **Readiness:** PARTIAL — strongest business-module example.
- **Required next fixes:** Move `SellMembershipAsync`, `UpdateMembershipStatusAsync`, `DeleteMembershipAsync`, `GetPaymentsAsync`, `CreatePaymentAsync` into module-internal handlers (the workflow services already use UoW, so this is mostly relocating + DI). Finance workspace already has invoice handlers — extend the same pattern.

---

## Admin UX Gap Audit

Source: `src/WebApp/Areas/Admin/{Controllers,Views,Services}/`.

| Page / area | Controller | Mutable? | Create / Edit / Delete | Strong view model? | Anti-forgery on POST? | Service boundary or direct EF? | Tenant isolation evidence | Tests | Status |
|---|---|---|---|---|---|---|---|---|---|
| Dashboard | `Admin/DashboardController` | No | — | Yes (`AdminDashboardViewModel`) | n/a | `IAdminDashboardPageService` → `IPlatformService` (shared BLL) | Role gate on action + `IUserContextService` | — | PASS (read-only) |
| Gyms (system list) | `Admin/GymsController` | No | — | Yes (`AdminGymsPageViewModel`) | n/a | `IAdminGymsPageService` → `IPlatformService` | `User.IsInRole(SystemAdmin/SystemSupport/SystemBilling)` else `Forbid` | — | PASS (read-only) |
| Members | `Admin/MembersController` | Yes | Create / Edit / Delete | Yes (`AdminMemberFormViewModel`, `*DeleteViewModel`) | Yes (lines 44/86/130) | `IAdminMembersPageService` → `IMemberWorkflowService` (BLL, UoW-backed) | `TryGetTenantAdminGymCode`; `MemberWorkflowService.EnsureTenantAccessAsync` | `AdminMembersCrudTests`, `AdminMembersPageTests`, `MemberCrudTests` | PASS |
| MembershipPackages | `Admin/MembershipPackagesController` | Yes | Create / Edit / Delete | Yes (`AdminMembershipPackageFormViewModel`) | Yes (lines 47/89/133) | `IAdminMembershipPackagesPageService` reads through **`AppDbContext`** for index/find; mutations call `IMembershipPackageService`. | `EnsureTenantAccessAsync` inside both | `AdminMembershipPackagesCrudTests`, `MembershipPackageCrudTests` | PARTIAL (mutates OK, page service still direct-EF) |
| Memberships (packages + active list) | `Admin/MembershipsController` | No | — | Yes (`AdminMembershipsPageViewModel`) | n/a | Direct calls to `IMembershipPackageService` + `IMembershipService` | Role gate + `IUserContextService` | — | PASS (read-only) |
| Operations (equipment / opening hours / maintenance summary) | `Admin/OperationsController` | No | — | Yes (`AdminOperationsPageViewModel`) | n/a | `IAdminOperationsPageService` → `IAdminOperationsQueryService` (UoW) | Role gate + `IUserContextService` | `AdminOperationsPageServiceTests` | PASS (read-only) |
| Sessions (training sessions list) | `Admin/SessionsController` | No | — | Yes (`AdminSessionsPageViewModel`) | n/a | `IAdminSessionsPageService` → `IAdminSessionsQueryService` (UoW) | Role gate + `IUserContextService` | `AdminSessionsPageServiceTests` | PASS (read-only) |
| TrainingCategories | `Admin/TrainingCategoriesController` | Yes | Create / Edit / Delete | Yes (`AdminTrainingCategoryFormViewModel`) | Yes (lines 43/85/129) | `IAdminTrainingCategoriesPageService` reads through **`AppDbContext`** for index/find; mutations call `ITrainingWorkflowService`. | `EnsureTenantAccessAsync` inside both | `AdminTrainingCategoriesCrudTests` | PARTIAL (mutates OK, page service still direct-EF) |

**Minimum Admin UX still missing for Final1 defense:**
- Anything that lets a grader watch an admin **change** state through MVC: only Members, MembershipPackages, TrainingCategories qualify. That is defensible only if you frame "Full Admin UX" as a focused trio.
- Two of those three page services still inject `AppDbContext` — the MVC story is told well in controllers but undercut in their backing page services.

**Minimum Admin UX still missing for Final2 defense:**
- An MVC Admin slice that exercises **two modules** in one user flow (e.g., Admin creates a Member, sells a Membership, books a Session) — would showcase mediator + cross-module work. Today this is React-only.

**Best 3 Admin workflows to implement next (small, vertical):**
1. **Admin Memberships sales page (`Admin/Memberships/Sell`)** — visualizes MembershipFinance module beyond the read-only page; reuses existing `SellMembershipAsync` workflow.
2. **Admin Equipment CRUD (`Admin/Operations/Equipment/{Create,Edit,Delete}`)** — gives Operations a mutable face; uses existing `IMaintenanceWorkflowService.*Equipment*` workflows.
3. **Admin OpeningHours editor** — small surface, high visual impact, already mediated end-to-end via `Modules.GymManagement.Contracts`.

---

## Architecture Tests Gap Audit

### Currently enforced

`tests/WebApp.Tests/Architecture/ArchitectureTests.cs`:
- `DomainAssembly_DoesNotReferenceForbiddenAssemblies` (BLL / DAL / DTO / WebApp).
- `DtoAssembly_DoesNotReferenceForbiddenAssemblies` (BLL / DAL / WebApp).
- `BllAssembly_DoesNotReferenceDalOrWebApp`.
- `BllAssembly_DoesNotReferenceEfCoreProviderOrRelational` (BLL may use EF abstractions but not a provider).
- `DalEfAssembly_DoesNotReferenceWebApp`.
- `ApiControllers_DoNotDependOnDbContext` (and `IAppDbContext`).
- `Mappers_LiveOnlyInBllMappingOrServicesNamespace`.
- `RepositoryAndUnitOfWork_AreDeclaredInBllContractsPersistence` (and on the BLL assembly).
- `TenantAccessChecker_UsesAuthorizationQueryRepositoryInsteadOfDbContext`.
- `MemberSlice_UsesDedicatedRepositoryAndMapperBoundaries`.
- `AccountAuthSlice_IsMediatedThroughUsersModule`.
- `TrainingSlice_UsesDedicatedRepositoryAndMapperBoundaries`.
- `MembershipFinanceSlice_UsesDedicatedRepositoryAndMapperBoundaries`.
- `MaintenanceSlice_UsesDedicatedRepositoryAndMapperBoundaries`.
- `MigratedAdminCrudControllers_DependOnPageServicesNotEf` (Members, MembershipPackages, TrainingCategories only).
- `MigratedAdminPageServices_DoNotDependOnDbContext` (AdminOperationsPageService + AdminSessionsPageService only).
- `AdminMvcControllers_AreThinAndDoNotDependOnDbContext` (all Admin controllers).
- `ClientMvcDashboard_UsesPageAndBllContractsWithoutDirectEf`.
- `ClientMvcSessions_UsesPageAndBllContractsWithoutDirectEf`.

`tests/WebApp.Tests/Architecture/ModuleArchitectureTests.cs`:
- `EveryModule_DoesNotReferenceAnyOtherModule`.
- `NonUsersModules_DoNotReferenceUsersInternals`.
- `TrainingModule_DoesNotReferenceUsersOrGymManagementInternals`.
- `TrainingCategoryWorkflow_IsOwnedByTrainingModuleHandlers`.
- `MembershipPackageWorkflow_IsOwnedByMembershipFinanceModuleHandlers`.
- `EveryModule_ReferencesBuildingBlocks`.
- `BuildingBlocks_DoesNotReferenceAnyModuleOrWebApp`.
- `WebApp_ReferencesEveryModule`.
- `EveryModule_ExposesExactlyOneIModuleMarker`.
- `EveryModule_ExposesAddModuleDIExtension`.
- `MediatorAbstractions_LiveInBuildingBlocks`.
- `Mediator_IsResolvableFromCompositionRoot`.

### Currently permissive (intentionally — by comment or by omission)

- BLL is allowed to reference `Microsoft.EntityFrameworkCore` abstractions.
- `IAppDbContext` is allowed in 9 BLL services + 1 module service + 1 middleware.
- `AppDbContext` is allowed in 2 MVC Client controllers, `HomeController`, `WorkspaceSwitcherViewComponent`, and 2 Admin page services.
- No rule says "no module may inject `IAppDbContext`" — would currently fail because `Modules.Users.Application.Auth.UsersSessionService` does.
- No rule says "no controller (Admin **or** Client **or** Home **or** ViewComponent) may inject `AppDbContext`" — Admin controllers are covered but Home/Client/ViewComponent are not.
- No rule says "POST actions must be `[ValidateAntiForgeryToken]`" — currently true by inspection but unprotected against regression.
- No rule says "no `ViewBag`/`ViewData`" — currently true by inspection.
- No rule says "modules cannot reference `App.BLL.Services.*WorkflowService`" — would catch the pass-through layer.

### Bad dependencies still allowed by these tests

- `AdminMembershipPackagesPageService` and `AdminTrainingCategoriesPageService` injecting `AppDbContext`.
- Any MVC component outside `Areas/Admin/Controllers/` and the two whitelisted Client controllers injecting `AppDbContext`.
- Any BLL service except the migrated trio + tenant/refresh/member/training/finance/maintenance slices injecting `IAppDbContext`.
- New `App.BLL.Services.*WorkflowService` calls from module handlers (pass-through pattern can spread without breaking a test).

### Tests that would fail today if turned on as-is

1. "No `IAppDbContext` parameter in any Modules.* type" → fails on `UsersSessionService`.
2. "No `AppDbContext` parameter in any WebApp non-Setup type" → fails on `HomeController`, `ProfileController`, `MaintenanceController`, `WorkspaceSwitcherViewComponent`, `AdminMembershipPackagesPageService`, `AdminTrainingCategoriesPageService`.
3. "No module handler depends on `App.BLL.Services.I*WorkflowService`" → fails on `MemberHandlers`, `MaintenanceHandlers`, `TrainingHandlers` (sessions/bookings), `FinanceHandlers` (memberships/payments/finance).
4. "Every `[HttpPost]` action has `[ValidateAntiForgeryToken]` unless its class carries `[ApiController]`" → currently passes by inspection; would lock behavior in.

### Suggested future architecture tests (matching the prompt's list)

1. `WebApp_NonSetup_DoesNotInjectAppDbContext` — scan every type in `WebApp.csproj` except `WebApp.Setup.*` and assert no constructor parameter is `AppDbContext`.
2. `AdminAndClientPageServices_DoNotInjectAppDbContext` — generalize the existing migrated-service whitelist to "every `*PageService` and `*QueryService` type".
3. `NewBllServices_DoNotInjectIAppDbContext` — whitelist current users; fail on any new addition.
4. `Modules_DoNotInjectIAppDbContext_NorReferenceWorkflowServices` — fails today on `UsersSessionService` + all pass-through handlers. Useful as the Phase 17/18/19/20 target.
5. `ModuleHandlers_DoNotCall_AppBllServicesWorkflowServices` — narrower variant of #4, doubles as the "moved workflow stays moved" guard.
6. `WebApp_HasNoBusinessLogic` — verifies controllers/page-services contain only delegation (count `for/while/if` in classes vs delegation count, or simpler: forbid any non-public method longer than N statements). Heuristic, but useful.
7. `AdminViews_DoNotUseViewBagOrViewData` — Roslyn or reflection over compiled views (Razor source scan is simpler; can be a string-based test on `Areas/**/*.cshtml`).
8. `PostActions_RequireAntiForgery` — reflect all `Controller`-derived types in `Areas`, find methods with `[HttpPost]` (or `[AcceptVerbs("POST")]`), assert presence of `[ValidateAntiForgeryToken]` or `[IgnoreAntiforgeryToken]` or the class has `[ApiController]`.

---

## Test Coverage Gap Audit

| Area | Existing coverage | Missing coverage | Risk | Recommended tests |
|---|---|---|---|---|
| Domain logic | `LangStrTests` (value object); `App.Domain` entities are mostly POCO. | No targeted tests for `TenantBaseEntity` invariants (audit / soft-delete), no tests for `LangStr` JSON edge cases beyond what `LangStrTests` covers. | Low | One xUnit class per non-trivial domain rule (when those rules move into Domain). |
| BLL / application services | `MemberWorkflowServiceTests`, `MembershipWorkflowServiceTests`, `TrainingWorkflowServiceTests`, `MaintenanceWorkflowServiceTests`, `AuthorizationServiceTests`, `SubscriptionTierLimitServiceTests`, `AdminOperationsPageServiceTests`, `AdminSessionsPageServiceTests`, `ClientDashboardPageServiceTests`, `ClientSessionsPageServiceTests`, `ClientSessionsQueryServiceTests`. | No tests for `IdentityService`, `AccountAuthService`, `PlatformService`, `MemberWorkspaceService`, `StaffWorkflowService`, `CoachingPlanService`, `BookingPricingService`, `CurrentActorResolver`, `ResourceAuthorizationChecker`. | Medium | One unit suite per service, especially the ones still on `IAppDbContext` (validates behavior before migration). |
| API integration | `Final1CriticalE2ETests`, `AuthSecurityAndErrorTests`, `SmokeTests`, `TenantIsolationAndIdorTests`, `ImpersonationTests`, `ProposalWorkflowTests`, `StaffWorkflowTests`, `MemberCrudTests`, `MembershipPackageCrudTests`, `TrainingCategoryLocalizationTests`. | No end-to-end test for `ValidationProblemDetails` shape, refresh-token revocation across logout, or platform/system-admin endpoints. | Medium | Single `ProblemDetailsContractTests` covering API + MVC. |
| MVC Admin | `AdminMembersCrudTests`, `AdminMembersPageTests`, `AdminMembershipPackagesCrudTests`, `AdminTrainingCategoriesCrudTests`. | No cross-tenant IDOR test through MVC Admin; no anti-forgery negative tests at MVC level. | Medium | `AdminCrossTenantTests` with 3 cross-tenant probes. |
| MVC Client | `ClientDashboardTests`. | No tests for `ProfileController`, `MaintenanceController` (status updates), or the cookie auth path through `HomeController`. | Medium | One MVC test class per Client controller. |
| React | `App.test.tsx`, `CrudPages.test.tsx`, `OperationsPages.test.tsx`, `SessionsPage.test.tsx`, `WorkspacePages.test.tsx`. | Not exercised in this audit; coverage of finance workspace, trainer coaching workspace, member workspace not verified. | Low–Medium | Run `npm test --prefix client` before defense. |
| Auth / refresh / logout | `AuthSecurityAndErrorTests` + `Final1CriticalE2ETests` (login + refresh). | Logout token revocation test, multi-tab refresh-token rotation, switch-gym/switch-role mediator tests. | Medium | `UsersModuleMediatorTests`. |
| Tenant isolation / IDOR | `TenantIsolationAndIdorTests` (API). | MVC Admin cross-tenant + MVC Client cross-tenant; `ProfileController` cross-member access; impersonation isolation beyond the existing class. | Medium-High | See "MVC Admin" + "MVC Client" rows. |
| Module boundaries | `ArchitectureTests`, `ModuleArchitectureTests`, `MaintenanceModuleMediatorTests`, `TrainingModuleMediatorTests`, `MembershipFinanceModuleMediatorTests`. | No `UsersModuleMediatorTests`. No "module handler is not a pass-through" test. | Medium | `UsersModuleMediatorTests` + the rules suggested above. |
| Persistence / Postgres | `PostgreSqlPersistenceTests` (3 tests, skipped without Docker), `AppDbContextBehaviorTests` (in-memory). | Not exercised in this audit run. | Medium | Run with `Testcontainers.PostgreSql` in CI before defense. |
| Deployment smoke | `SmokeTests` (in-process). | No smoke test against the production-like `docker-compose.prod.yml`. | Medium | Manual smoke per `docs/deployment.md`. |
| Localization | `TrainingCategoryLocalizationTests`, `LangStrTests`. | No localization assertions for MVC view labels or React strings. | Low | None required for grading. |

---

## Prioritized Remaining Work

### Must fix before Final1 defense

| # | Objective | Files likely touched | Tests to add first | Risk | Phase size | Suggested split |
|---|---|---|---|---|---|---|
| F1-1 | Move MVC Client `ProfileController` off `AppDbContext` | `src/WebApp/Areas/Client/Controllers/ProfileController.cs`, new `IClientProfilePageService` + impl in `src/WebApp/Areas/Client/Services/`, possibly add methods on `IMemberRepository`/`IMembershipRepository`/`IBookingRepository`/`IPaymentRepository` | `ClientProfilePageServiceTests` (in-memory UoW); integration test that 200s for owner & 403s for someone else | Medium | Medium | — |
| F1-2 | Move MVC Client `MaintenanceController` off `AppDbContext` (just the `Equipment` label lookup) | `…/Client/Controllers/MaintenanceController.cs`, `IMaintenanceRepository` | Reuse `MaintenanceWorkflowServiceTests`; add one MVC integration test for `Details` | Low | Small | — |
| F1-3 | Move `HomeController` cookie login / switch-gym / switch-role / claims-builder off `AppDbContext` (route through Users module) | `src/WebApp/Controllers/HomeController.cs`, possibly new commands `CookieLoginCommand`, `CookieSwitchGymCommand`, `CookieSwitchRoleCommand` in `Modules.Users.Contracts`, new handlers, supporting helper in BLL/Users | New mediator handler tests + `UsersModuleMediatorTests`; integration test covering `/login` + `/switch-gym` + `/switch-role` | High | Medium | Split: (a) extract claims-build helper to BLL; (b) add commands + handlers; (c) controller uses `IMediator` only. |
| F1-4 | Move `WorkspaceSwitcherViewComponent` off `AppDbContext` | `src/WebApp/ViewComponents/WorkspaceSwitcherViewComponent.cs`, new `IWorkspaceSwitcherViewModelBuilder` or mediator query in Users module | New unit test for the view-model builder | Medium | Small | — |
| F1-5 | Migrate `AdminMembershipPackagesPageService` and `AdminTrainingCategoriesPageService` off `AppDbContext` | `src/WebApp/Areas/Admin/Services/AdminViewModelServices.cs`, add list/find methods to `IMembershipPackageRepository`/`ITrainingCategoryRepository`, update `ArchitectureTests.MigratedAdminPageServices_DoNotDependOnDbContext` whitelist | Existing `Admin*CrudTests` already cover happy path; add architecture test entry | Low | Small | — |
| F1-6 | Add `AdminCrossTenantTests` for the migrated Admin CRUD trio | New `tests/WebApp.Tests/Integration/AdminCrossTenantTests.cs` | The new tests themselves | Low | Small | — |
| F1-7 | Defense framing: state explicitly that "Full Admin UX" means Members + MembershipPackages + TrainingCategories in MVC plus read-only Operations/Sessions/Memberships dashboards, and that mutate-heavy areas (sessions/bookings/staff) are administered via the React SPA + API | `docs/final1-defense.md` (documentation only) | — | Low | Small | — |

### Must fix before Final2 defense

| # | Objective | Files likely touched | Tests to add first | Risk | Phase size | Suggested split |
|---|---|---|---|---|---|---|
| F2-1 | Move `UsersSessionService` off `IAppDbContext` (add `IAppUserGymRoleRepository` + `IGymQueryRepository` to UoW, or to a Users-local UoW) | `src/App.BLL/Contracts/Persistence/IAppUserGymRoleRepository.cs` (new), `src/App.DAL.EF/Repositories/EfAppUserGymRoleRepository.cs` (new), `src/App.BLL/Contracts/Persistence/IAppUnitOfWork.cs`, `src/App.DAL.EF/Repositories/EfAppUnitOfWork.cs`, `src/Modules.Users/Application/Auth/UsersSessionService.cs` | `UsersModuleMediatorTests` covering login / refresh / switch-gym / switch-role | High | Medium | — |
| F2-2 | Move `MemberHandlers` off pass-through (handlers consume UoW + `IMemberMapper` directly) | `src/Modules.GymManagement/Application/Members/MemberHandlers.cs`, possibly retire `IMemberWorkflowService` after callers are gone | Existing `MemberWorkflowServiceTests` → port to handler tests | Medium | Medium | One handler per phase (List → Get → Create → Update → Delete). |
| F2-3 | Move `TrainingHandlers` (sessions/bookings) off pass-through | `src/Modules.Training/Application/TrainingHandlers.cs`, reuse `ITrainingSessionRepository`/`IBookingRepository` | Port `TrainingWorkflowServiceTests` to handler tests | Medium | Large | Split per aggregate: sessions, bookings, work-shifts. |
| F2-4 | Move `FinanceHandlers` (memberships/payments/finance) off pass-through | `src/Modules.MembershipFinance/Application/FinanceHandlers.cs`, reuse existing repositories | Port `MembershipWorkflowServiceTests` to handler tests | Medium | Large | Split per aggregate. |
| F2-5 | Move `MaintenanceHandlers` (equipment + openings + tasks + gym settings + gym users) off pass-through | `src/Modules.GymManagement/Application/Maintenance/MaintenanceHandlers.cs` | Port `MaintenanceWorkflowServiceTests` to handler tests | Medium | Large | Split per sub-aggregate. |
| F2-6 | Move DI registration of each migrated service from `WebApp.Setup.ServiceExtensions` into the owning `Add*Module` extension | `src/WebApp/Setup/ServiceExtensions.cs`, `src/Modules.*/{Module}ServiceCollectionExtensions.cs` | None new | Low | Small | One module per phase. |
| F2-7 | Add architecture test: "no Modules.* type injects `IAppDbContext` or `App.BLL.Services.I*WorkflowService`" | `tests/WebApp.Tests/Architecture/ModuleArchitectureTests.cs` | The test itself | Medium | Small | Land last, after F2-2..F2-5. |
| F2-8 | Add `UsersModuleMediatorTests` | `tests/WebApp.Tests/Unit/UsersModuleMediatorTests.cs` (new) | The tests themselves | Low | Small | — |

### Nice to have

| # | Objective | Why | Phase size |
|---|---|---|---|
| N-1 | Remove `Microsoft.EntityFrameworkCore` from `App.BLL.csproj` and delete `IAppDbContext` interface | Closes the last EF dependency in BLL; possible only after every service in row 3 of the F1 matrix has migrated. | Small (mechanical) |
| N-2 | Per-module Postgres schema (`users.*`, `gym.*`, `training.*`, `finance.*`) | Lets you say "module X owns table Y" at the database level. Optional defense story. | Large |
| N-3 | One MVC Admin slice that crosses two modules in a single user flow (member → membership sale → first booking) | Strong Final2 narrative without changing the API surface. | Medium |
| N-4 | FluentValidation across BLL workflows | Single validation pipeline; replaces ad-hoc `ValidateRequest` statics. | Medium |
| N-5 | Restrict `ApiControllerBase` to `[Authorize]` by default; add `[AllowAnonymous]` only on `AccountController.Login`/`Register` | Reduces blast radius if a developer forgets the attribute. | Small |

---

## Recommended Next Codex Phases

Each phase is vertical and test-first. Validation commands are the same across phases unless noted: `dotnet build multi-gym-management-system.slnx`, `dotnet test multi-gym-management-system.slnx`, `dotnet format multi-gym-management-system.slnx --verify-no-changes`.

### Phase A — Admin page services off DbContext (Final1)
- **Objective:** F1-5 above.
- **Flow:** Admin opens Membership Packages or Training Categories list/edit/delete pages.
- **Files touched:** `src/WebApp/Areas/Admin/Services/AdminViewModelServices.cs`, `src/App.BLL/Contracts/Persistence/IMembershipPackageRepository.cs`, `ITrainingCategoryRepository.cs`, `src/App.DAL.EF/Repositories/EfMembershipPackageRepository.cs`, `EfTrainingCategoryRepository.cs`, `tests/WebApp.Tests/Architecture/ArchitectureTests.cs` (whitelist).
- **Tests to add first:** A new entry in `MigratedAdminPageServices_DoNotDependOnDbContext` — should fail before edit, pass after.
- **Implementation summary:** Add `ListAdminSummariesAsync(gymId, ct)` / `GetForAdminAsync(gymId, id, ct)` on the two repositories; replace direct `dbContext.MembershipPackages.…` / `dbContext.TrainingCategories.…` reads with repository calls; remove `AppDbContext` from constructors.
- **Done when:** Architecture test passes; `AdminMembershipPackagesCrudTests` + `AdminTrainingCategoriesCrudTests` still green; no new `AppDbContext` usage anywhere under `Areas/Admin/Services/`.

### Phase B — MVC Client Profile slice off DbContext (Final1)
- **Objective:** F1-1.
- **Flow:** Authenticated member opens `/mvc-client/Profile`.
- **Files touched:** `src/WebApp/Areas/Client/Controllers/ProfileController.cs`, new `src/WebApp/Areas/Client/Services/ClientProfilePageService.cs` + interface, possibly add methods on `IMembershipRepository`/`IBookingRepository`/`IPaymentRepository`.
- **Tests to add first:** `ClientProfilePageServiceTests` (in-memory UoW); a new architecture test `ClientMvcProfile_UsesPageAndBllContractsWithoutDirectEf` (mirror of existing dashboard/sessions tests).
- **Implementation summary:** Same shape as `ClientDashboardPageService`. Controller takes only `IClientProfilePageService`; page service composes view model from authorization service + repositories.
- **Validation commands:** Plus run the new architecture test.
- **Done when:** Architecture test passes; `ProfileController` constructor has only the new page-service dependency; existing manual flow still renders.

### Phase C — MVC Client Maintenance Details slice off DbContext (Final1)
- **Objective:** F1-2.
- **Flow:** Caretaker opens a maintenance task detail page.
- **Files touched:** `src/WebApp/Areas/Client/Controllers/MaintenanceController.cs`, `IMaintenanceRepository`, `EfMaintenanceRepository`.
- **Tests to add first:** Architecture test `ClientMvcMaintenance_DoesNotInjectAppDbContext`; reuse `MaintenanceWorkflowServiceTests`.
- **Implementation summary:** Add `IMaintenanceRepository.GetEquipmentLabelAsync(gymId, equipmentId)` and call it from the controller; drop `AppDbContext`.
- **Done when:** Architecture test passes; existing maintenance flow renders the equipment label.

### Phase D — Cookie auth: move `HomeController` switch-gym/switch-role through mediator (Final1)
- **Objective:** F1-3 (split point 1).
- **Flow:** Authenticated user clicks the workspace switcher; cookie claims are rebuilt without the controller seeing EF.
- **Files touched:** `src/WebApp/Controllers/HomeController.cs`, new `CookieSwitchGymCommand`/`CookieSwitchRoleCommand` in `Modules.Users/Contracts/AuthSessionMessages.cs`, new handlers in `Modules.Users/Application/Auth/AuthSessionHandlers.cs`, BLL helper that returns the rebuilt `IEnumerable<Claim>` from a user + active link.
- **Tests to add first:** `UsersModuleMediatorTests.SwitchGym_ReturnsClaimsForOwnedLink`, …`RejectsUnauthorizedLink`. Integration test for `/switch-gym` POST.
- **Implementation summary:** Controller resolves `IMediator`, builds the command, calls `signInManager.SignInWithClaimsAsync` with the returned claim set. Direct EF reads are gone from the controller.
- **Done when:** `HomeController` no longer injects `AppDbContext`; new mediator tests green; integration test green.

### Phase E — `WorkspaceSwitcherViewComponent` off DbContext (Final1)
- **Objective:** F1-4.
- **Flow:** Layout renders the workspace dropdown.
- **Files touched:** `src/WebApp/ViewComponents/WorkspaceSwitcherViewComponent.cs`, new mediator query `GetWorkspaceLinksQuery` in `Modules.Users.Contracts`.
- **Tests to add first:** `UsersModuleMediatorTests.GetWorkspaceLinks_ReturnsActiveLinksForUser`.
- **Implementation summary:** View component takes `IMediator`; dispatches `GetWorkspaceLinksQuery` and projects to the existing view model.
- **Done when:** No `AppDbContext` in `WorkspaceSwitcherViewComponent`; layout still renders identically.

### Phase F — `UsersSessionService` off `IAppDbContext` (Final2)
- **Objective:** F2-1.
- **Flow:** Login + refresh + switch-gym + switch-role JWT path (the API auth surface).
- **Files touched:** New `IAppUserGymRoleRepository.cs` in BLL, EF impl in DAL, UoW property addition, `Modules.Users/Application/Auth/UsersSessionService.cs`.
- **Tests to add first:** Architecture test `Modules_DoNotInjectIAppDbContext` (whitelisted to allow `UsersSessionService` *only until this phase*, then remove the entry to make it real).
- **Implementation summary:** Replace every `dbContext.AppUserGymRoles.…FirstOrDefaultAsync` with `unitOfWork.AppUserGymRoles.GetActiveLinkAsync(...)` (etc.). Optionally introduce a Users-local UoW.
- **Done when:** `UsersSessionService` no longer takes `IAppDbContext`; new architecture test rejects future reintroduction; `AuthSecurityAndErrorTests` + `Final1CriticalE2ETests` green.

### Phase G — Members workflow moves into Modules.GymManagement (Final2)
- **Objective:** F2-2.
- **Flow:** Every member-related API call (`/api/v1/{gymCode}/members/...`).
- **Files touched:** `src/Modules.GymManagement/Application/Members/MemberHandlers.cs`, possibly retire `App.BLL.Services.MemberWorkflowService` after callers migrate (Admin page service + Client profile service).
- **Tests to add first:** Port `MemberWorkflowServiceTests` to handler-level tests; add `ModuleHandlers_DoNotCall_AppBllServicesWorkflowServices` test (initial whitelist will need only the not-yet-migrated handlers).
- **Implementation summary:** Each handler injects `IAppUnitOfWork` + `IMemberMapper` + `IAuthorizationService`. Move logic from `MemberWorkflowService` into the handlers (one handler per command/query).
- **Done when:** No handler in `Modules.GymManagement/Application/Members/` depends on `IMemberWorkflowService`; existing CRUD tests green.

### Phase H — Training sessions + bookings workflow moves into Modules.Training (Final2)
- **Objective:** F2-3.
- **Flow:** Training session CRUD + booking create/cancel/attendance.
- **Files touched:** `src/Modules.Training/Application/TrainingHandlers.cs`; possibly retire `TrainingWorkflowService`.
- **Tests to add first:** Architecture test entry (extend the test from Phase G); port `TrainingWorkflowServiceTests`.
- **Implementation summary:** Move logic into handlers consuming `ITrainingSessionRepository`/`IBookingRepository`/`IWorkShiftRepository` + `ITrainingMapper`.
- **Done when:** No handler in `Modules.Training/Application/` depends on `ITrainingWorkflowService` (Bookings/Sessions); architecture test passes.

### Phase I — MembershipFinance memberships + payments workflows move into module (Final2)
- **Objective:** F2-4.
- **Flow:** `SellMembership`, `UpdateMembershipStatus`, `CreatePayment`, finance workspace queries.
- **Files touched:** `src/Modules.MembershipFinance/Application/FinanceHandlers.cs`; possibly retire `MembershipWorkflowService`/`FinanceWorkspaceService` once callers migrate.
- **Tests to add first:** Port `MembershipWorkflowServiceTests`; extend the architecture test from Phase G.
- **Implementation summary:** Move logic into handlers consuming the existing membership/payment/finance repositories + `IMembershipFinanceMapper`.
- **Done when:** No handler in `Modules.MembershipFinance/Application/` depends on `IMembershipWorkflowService`; architecture test passes.

### Phase J — GymManagement maintenance workflows move into module (Final2)
- **Objective:** F2-5.
- **Flow:** Equipment, opening hours, maintenance tasks, gym settings, gym users.
- **Files touched:** `src/Modules.GymManagement/Application/Maintenance/MaintenanceHandlers.cs`; possibly retire `MaintenanceWorkflowService` once callers migrate.
- **Tests to add first:** Architecture test entry; port `MaintenanceWorkflowServiceTests`.
- **Implementation summary:** Move logic into handlers consuming `IMaintenanceRepository` + supporting repositories.
- **Done when:** No handler in `Modules.GymManagement/Application/Maintenance/` depends on `IMaintenanceWorkflowService`.

### Phase K — DI registration moves into owning modules; ServiceExtensions slims down (Final2)
- **Objective:** F2-6.
- **Flow:** App boot.
- **Files touched:** `src/WebApp/Setup/ServiceExtensions.cs` (delete migrated registrations), `Modules.GymManagement/GymManagementModuleServiceCollectionExtensions.cs` and the three siblings.
- **Tests to add first:** `Mediator_IsResolvableFromCompositionRoot` already exists and will fail if anything is dropped accidentally.
- **Implementation summary:** Move `services.AddScoped<I*WorkflowService, *WorkflowService>()` calls into each module's extension method as each module gains ownership.
- **Done when:** `ServiceExtensions` shrinks; module extensions become non-empty.

### Phase L — Architecture rules ratchet (Final2)
- **Objective:** F2-7.
- **Flow:** None (test-only).
- **Files touched:** `tests/WebApp.Tests/Architecture/ModuleArchitectureTests.cs`, possibly `ArchitectureTests.cs`.
- **Tests to add first:** The rules themselves: `Modules_DoNotInjectIAppDbContext`, `ModuleHandlers_DoNotCall_AppBllServicesWorkflowServices`, `WebApp_NonSetup_DoesNotInjectAppDbContext`, `PostActions_RequireAntiForgery`, `AdminViews_DoNotUseViewBagOrViewData`.
- **Implementation summary:** Each rule lands with a minimal whitelist that matches today's reality, then whitelist entries are removed in Phases F–K.
- **Done when:** All five rules are in CI and pass.

---

## Final Judgment

### 1. Is this project defense-ready for Final1?

Mostly yes, with documented gaps. The CLEAN/ONION shape is real: dependency direction is correct, BLL/DAL/DTO/Domain assemblies enforce direction with architecture tests, repositories + UoW exist and are used by the migrated slices, API controllers are thin, MVC controllers are clean, anti-forgery is present, no `ViewBag`/`ViewData`. The defensible weak spots are (a) two MVC Client controllers + `HomeController` + a ViewComponent still touching `AppDbContext`, (b) two Admin page services still touching `AppDbContext`, and (c) nine BLL services + one middleware still consuming `IAppDbContext`. Walked through honestly, these are migrate-as-we-go items, not architectural failures.

### 2. Minimum fixes to make Final1 defensible

Land Phase A (Admin page services), Phase B (`ProfileController`), Phase C (`MaintenanceController` label lookup), and the documentation framing in F1-7. Phases D/E (Home + view component) are the higher-impact polish if time allows. Add `AdminCrossTenantTests` for the migrated trio.

### 3. Is this project defense-ready for Final2?

Partially. The skeleton is genuinely modular: four module projects, mediator in `BuildingBlocks`, module-to-module references blocked by architecture tests, two real workflows owned by their module (Training categories, MembershipFinance packages), and mediator-resolved auth in `Modules.Users`. But the bulk of mediator handlers are 1–3 line wrappers over `App.BLL.Services.*WorkflowService`, and `UsersSessionService` (the only "real" module service) still uses `IAppDbContext`. A skeptical grader can show that today.

### 4. Minimum fixes to make Final2 defensible

Land Phase F (Users module owns its data access) and at least Phase G + Phase I (Members + MembershipFinance memberships move into their modules). That gives you three honest module workflows (Users auth, Training categories + MembershipFinance packages + memberships, GymManagement members) plus a credible "Maintenance is next" story. Add `UsersModuleMediatorTests`. Tighten architecture rules (Phase L) so the boundaries can't regress.

### 5. Highest-risk things a grader will likely notice

1. The four Client/Home MVC components directly injecting `AppDbContext` — the single most contradictory thing in the repo against any Clean/Onion claim.
2. The 9 `IAppDbContext` users in BLL (the architecture test grandfathers this; a careful grader will read the test and notice).
3. The pass-through pattern in module handlers — easy to spot if a grader reads `MemberHandlers.cs` after reading the Final2 plan.
4. The MVC Admin surface being mutate-light (3 of 8 controllers).
5. No tests for `IdentityService`, `AccountAuthService`, `PlatformService`, or `UsersSessionService` specifically.

### 6. Parts that are strong and should be emphasized in defense

- Architecture tests are real and specific: tenant isolation, repository placement, mapper placement, controller dependencies, module boundaries, mediator resolution.
- Tenant isolation is layered: EF global query filters (`AppDbContext.ConfigureTenantSoftDeleteFilter<T>`), service-level `EnsureTenantAccessAsync`, resource-level `ResourceAuthorizationChecker`, and `TenantIsolationAndIdorTests` to back it up.
- The auth flow has both a JWT (`/api/v1/account/...`) and a cookie (`/login`, `/switch-gym`) story, both ending in claims that include `GymId`, `GymCode`, and `ActiveRole`.
- Migrated CRUD slices are clean end-to-end: Admin controller → Admin page service → BLL workflow service → repository → mapper → DTO, with `ValidationAppException` → `ProblemDetails` translation and anti-forgery on every form.
- Postgres-specific persistence concerns (`LangStr` as JSONB, tenant filters, unique indexes per gym) have dedicated container-backed tests.
- React SPA covers the mutate-heavy surfaces (sessions/bookings, finance workspace, memberships sales, member workspace) that MVC Admin does not, with Vitest tests.

### 7. What should not be claimed because evidence is insufficient

- Do **not** claim "BLL has no EF dependency" — `App.BLL.csproj:13` references `Microsoft.EntityFrameworkCore` and the architecture test explicitly grandfathers it.
- Do **not** claim "WebApp never touches `AppDbContext`" — six places do.
- Do **not** claim "all module workflows are module-owned" — only training-category and membership-package CRUD are.
- Do **not** claim "module data ownership is enforced" — it is documented, but enforced only by convention and by the cross-module reference test, not by schema or by per-module DbContext.
- Do **not** claim Postgres tests ran in CI for this build — they were skipped (no Docker).
- Do **not** claim "Full Admin UX" without immediately listing the three CRUD areas plus the read-only dashboards as the scope.
