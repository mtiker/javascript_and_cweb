# Final2 Phase Tracker

The full phase prompts live in `assignment05_final2_codex_prompts.md`. AI agents
picking up this work: read that file, then start the **next pending** phase
below. Update this tracker when a phase is finished.

Source of truth for status is this file. Each entry records what was done so a
fresh agent can resume without re-pasting prompts.

## Status

- [x] **Phase 0 â€” Copy Final1 to assignment05_final2**
  - Done 2026-05-19
  - Copied `assignment-03-multi-gym-management-system/` to `assignment05_final2/`
    at the same directory level (`courses/webapp-csharp/`).
  - Build artifacts excluded from copy: `bin/`, `obj/`, `node_modules/`,
    `dist/`, `.dotnet/`, `.dotnet-cli/`, `.vs/`, `*.log`. They will regenerate
    on first build/test.
  - README updated to identify as Final2.
  - Final1 (`assignment-03-multi-gym-management-system/`) untouched.
- [x] **Phase 1 â€” Baseline inventory and architecture safety net**
  - Done 2026-05-19
  - Added `docs/final2-module-map.md` mapping every entity, BLL service, DAL
    repo, API controller, MVC Admin/Client controller+views, and React route
    to one of the target modules (Users, Gyms, Memberships, Training,
    Maintenance) plus Shared.Contracts / SharedKernel / App.Resources.
  - Added a new `Architecture.Tests` project (xUnit, net10.0) registered in
    `multi-gym-management-system.slnx`. Tests use
    `[Trait("Category", "Architecture")]` so `--filter Architecture` catches
    them. Existing `WebApp.Tests/Architecture/Final1PresentationBoundaryTests`
    kept as-is.
  - Initial rules: `Modules.*` may not project-reference another `Modules.*`
    (vacuous today, active from Phase 2 onward). The
    no-legacy-App.*-from-modules rule is wired but `[Skip]`-marked until
    Phase 10, per Phase 1's "initially allow legacy App.* references".
  - Validation: `dotnet build` clean, `dotnet test` â‡’ 184 passed, 4 skipped
    (1 architecture skip-by-design + 3 PostgreSQL integration skips that
    require `RUN_POSTGRES_TESTS=1`), 0 failures.
  - No runtime behavior changed, no production code moved.
- [x] **Phase 2 â€” Create module shells**
  - Done 2026-05-19
  - Created 7 new projects: `SharedKernel`, `Shared.Contracts`, and
    `Modules.{Users,Gyms,Memberships,Training,Maintenance}`. All registered
    in `multi-gym-management-system.slnx` (20 projects total).
  - Each module has the required `Api / Application / Domain / Infrastructure`
    folders (empty folders carry a `.gitkeep` so git tracks them) plus a
    public `XxxModuleMarker` static class at the project root (for Phase 3
    mediator assembly scanning) and `Api/XxxModuleExtensions.cs` exposing
    `AddXxxModule(IServiceCollection, IConfiguration)`.
  - `WebApp/WebApp.csproj` now references all 7 new projects; `Program.cs`
    calls each `AddXxxModule(builder.Configuration)` after the existing
    `AddAppXxx(...)` calls. No services registered yet (shells are no-ops);
    real wiring lands in Phases 4â€“8.
  - `Modules.*` only reference `SharedKernel`, `Shared.Contracts`, and
    `App.Resources` â€” matches the Phase 2 allowlist.
  - Architecture.Tests now enforces: every expected module exists, no module
    project-references another module, modules only project-reference the
    allowed shared projects, and WebApp references all 5 modules. The
    legacy-App.* ban remains `[Skip]` until Phase 10.
  - Validation: `dotnet build` clean (20/20 projects). `dotnet test --filter
    Architecture` â‡’ 6 passed, 1 skipped, 0 failed. Full `dotnet test` (with
    `xUnit.ParallelizeTestCollections=false` to dodge a JIT OOM on this
    machine) â‡’ 186 passed, 4 skipped, 0 failed.
  - No production code moved, no API routes changed.
- [x] **Phase 3 â€” Add mediator foundation**
  - Done 2026-05-20
  - Added `MediatR` 12.4.1 package reference on `SharedKernel`; it flows
    transitively to `Shared.Contracts` and all `Modules.*` (which already
    reference `SharedKernel`), so no module project file needed a direct
    package reference â€” only the existing project graph was used.
  - Added cross-module mediator abstractions in `Shared.Contracts/Mediator/`:
    `IModuleNotification` (`: INotification`), `IModuleRequest<TResponse>`
    (`: IRequest<TResponse>`), and a `Diagnostics/` pair
    (`IModuleEventRecorder` + `InMemoryModuleEventRecorder` â€” a thread-safe
    in-memory sink for observing handler invocations). Sample event:
    `Events/ModulesReadyNotification(string Source) : IModuleNotification`.
  - Each `AddXxxModule(IServiceCollection, IConfiguration)` in
    `Modules.{Users,Gyms,Memberships,Training,Maintenance}/Api/` now calls
    `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(XxxModuleMarker).Assembly))`
    (the marker types are `static`, so the generic
    `RegisterServicesFromAssemblyContaining<T>` overload is not usable) and
    idempotently `TryAddSingleton<IModuleEventRecorder, â€¦>`. WebApp/Program.cs
    composition is unchanged â€” it already calls each `AddXxxModule` after
    legacy `AddAppXxx`.
  - Sample handler in Users:
    `Modules.Users/Application/Mediator/ModulesReadyHandler.cs` â€”
    `INotificationHandler<ModulesReadyNotification>` that records
    `Modules.Users<-{notification.Source}` on the shared recorder.
    `internal sealed` so it is not part of Users' public surface.
  - Architecture.Tests gained `Microsoft.Extensions.Configuration`,
    `Microsoft.Extensions.DependencyInjection` package refs and project refs
    to `SharedKernel`, `Shared.Contracts`, and all 5 `Modules.*` so it can
    compose the IoC container directly. New `MediatorRegistrationTests` adds
    two `[Trait("Category", "Architecture")]` tests:
    (1) compose every module, resolve `IMediator`, publish
    `ModulesReadyNotification`, and assert the Users handler ran via the
    recorder snapshot; (2) assert `IModuleEventRecorder` is the same
    singleton instance across modules (TryAddSingleton is idempotent).
  - No production code was moved between projects; no API routes changed.
    Existing BLL/DAL services still own all real work â€” mediator is only the
    new cross-module rail.
  - Validation: `dotnet build multi-gym-management-system.slnx` â‡’ 0 warnings,
    0 errors. `dotnet test --filter Architecture` â‡’ 8 passed, 1 skipped
    (legacy-App.* ban still `[Skip]` until Phase 10). Full
    `dotnet test multi-gym-management-system.slnx -- xUnit.ParallelizeTestCollections=false`
    â‡’ 188 passed, 4 skipped (1 architecture skip-by-design + 3 PostgreSQL
    integration skips needing `RUN_POSTGRES_TESTS=1`), 0 failures.
- [x] **Phase 4 â€” Extract Users module**
  - Done 2026-05-20
  - User chose the **delegate-and-relocate** strategy (vs full physical move
    or minimal-delegate): `AccountController` moves into the module,
    auth-logic stays in `App.BLL` and is wrapped behind a new
    `Modules.Users.Application.IUsersAuthService`, refresh-token EF
    persistence moves into the module, and identity entities stay in
    `App.Domain` until Phase 9. Justifies the architecture-test relaxation
    below.
  - **Shared.Contracts/ModuleApis/**: added `IUsersModuleApi`
    (`GetUserSummaryAsync` + `FindUserByEmailAsync`) and the public
    `UserSummary(Id, Email, DisplayName, SystemRoles)` record. Cross-module
    consumers depend only on these â€” never `AppUser`.
  - **Modules.Users/Application/Auth/`IUsersAuthService` (8 ops mirroring
    `AccountController` actions) + `UsersAuthService`**: Phase-4
    implementation delegates to `App.BLL.Contracts.IIdentityService` +
    `IAccountAuthService`. Internal â€” only `IUsersAuthService` is `public`.
  - **Modules.Users/Application/`UsersModuleApiService`**: implements
    `IUsersModuleApi` by wrapping `UserManager<AppUser>` (ASP.NET Identity).
    Projects to `UserSummary` and never leaks `AppUser`.
  - **Modules.Users/Api/`AccountController`**: relocated from
    `WebApp/ApiControllers/Identity/AccountController.cs` (deleted, empty
    folder removed). Same route attributes (`api/v{version:apiVersion}/account`)
    and the same 8 action signatures â€” only dependency changes to
    `IUsersAuthService`. Added explicit usings for ASP.NET types because
    `Microsoft.NET.Sdk` (not Sdk.Web) is the module SDK.
  - **Modules.Users/Infrastructure/`EfRefreshTokenRepository`**: relocated
    from `App.DAL.EF/Repositories/`. Still uses the shared `AppDbContext`
    until Phase 9 splits per-module persistence. `App.DAL.EF.AppUOW` now
    receives `IRefreshTokenRepository` via DI rather than `new`-ing it, so
    `AppUOW.RefreshTokens` (still on `IAppUnitOfWork` for transitional
    compatibility with `App.BLL.AccountAuthService`) resolves to the
    Users-owned implementation. `AddAppPersistence` no longer registers
    `IRefreshTokenRepository`; `AddUsersModule` does.
  - **`Modules.Users.csproj`**: added `Asp.Versioning.Mvc` 8.1.0 and
    `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.2 packages, plus
    transitional `ProjectReference`s to `App.BLL.Contracts`,
    `App.DAL.Contracts`, `App.DAL.EF`, `App.Domain`, `App.DTO`. Comment on
    the legacy refs marks them as Phase 4-9 transitional, to be removed in
    Phase 10.
  - **`Api/UsersModuleExtensions.AddUsersModule`** now registers
    `IRefreshTokenRepository â†’ EfRefreshTokenRepository`,
    `IUsersAuthService â†’ UsersAuthService`,
    `IUsersModuleApi â†’ UsersModuleApiService`, and calls
    `services.AddControllers().AddApplicationPart(typeof(UsersModuleMarker).Assembly)`
    so MVC discovers the controller that now lives outside `WebApp`.
  - **Architecture.Tests/ModuleBoundaryTests**:
    `AllowedNonModuleProjectRefsFromModules` widened to include
    `App.BLL`, `App.BLL.Contracts`, `App.DAL.Contracts`, `App.DAL.EF`,
    `App.Domain`, `App.DTO` as transitional Phase 4-9 dependencies.
    `LegacyAppProjectsBlockedAfterPhase10` got `App.DTO` added so the
    Phase-10 ban (still `[Skip]`-ed) covers the same set.
  - **WebApp.Tests** had to follow the moves:
    `Unit/ApiContractMetadataTests` now discovers controllers across both
    `typeof(Program).Assembly` and `typeof(AccountController).Assembly`
    (matching `WebApp.ApiControllers.*` **or** `Modules.*.Api`); the four
    `UnitOf*WorkflowServiceTests` files that `new AppUOW(...)` directly now
    pass `new EfRefreshTokenRepository(dbContext)` as the second arg.
    `WebApp.Tests.csproj` gained a project reference to `Modules.Users`.
  - Validation: `dotnet build multi-gym-management-system.slnx` â‡’ 0 errors,
    6 warnings (pre-existing transitive NuGet vulnerability advisories on
    `Microsoft.AspNetCore.DataProtection` and
    `System.Security.Cryptography.Xml`, surfaced now that
    `Architecture.Tests` indirectly pulls them â€” not introduced by this
    phase). `dotnet test --filter Account` â‡’ 3 passed
    (`ApiContractMetadataTests.AccountAuthPublicRoutesAndDtos_RemainStable`
    plus 2 more). `dotnet test --filter Architecture` â‡’ 8 passed, 1
    skipped â€” same as Phase 3. Full
    `dotnet test multi-gym-management-system.slnx -- xUnit.ParallelizeTestCollections=false`
    â‡’ **188 passed, 4 skipped, 0 failed** â€” identical pass count to
    Phase 3; no regressions in routes, login/logout, refresh-token rotation,
    or Swagger JWT.
- [x] **Phase 5 â€” Extract Gyms and tenancy module**
  - Done 2026-05-20
  - User chose the **delegate-and-relocate** strategy (mirroring Phase 4):
    controllers + tenant-resolution middleware move into `Modules.Gyms`,
    `IGymsModuleApi` exposes outward access resolution, but gym entities,
    EF configs, repositories, and `App.BLL` tenant/platform services stay in
    place until Phase 9. Architecture-test relaxation already permits the
    transitional `App.*` refs added in Phase 4.
  - **Shared.Contracts/ModuleApis/**: added `IGymsModuleApi`
    (`ResolveAccessAsync(userId, gymCode, allowedRoles?, ct)` +
    `ListGymsForUserAsync(userId, ct)`) and the public
    `GymAccess(GymId, GymCode, IsActive, Roles)` record. Cross-module
    consumers depend only on these â€” never `Gym` or `AppUserGymRole`.
  - **Modules.Gyms/Application/`GymsModuleApiService`**: implements
    `IGymsModuleApi` by querying the shared `AppDbContext` (`Gyms` +
    `AppUserGymRoles`). Filters out inactive gyms and roles, supports an
    `allowedRoles` allowlist for tenant-role checks. `internal sealed` â€”
    only `IGymsModuleApi` is the public surface. `AppDbContext` dependency
    is transitional (Phase 5-9), matching how Phase 4 keeps using the
    shared context until persistence is split per module.
  - **Modules.Gyms/Infrastructure/`GymResolutionMiddleware`**: relocated
    verbatim from `WebApp/Middleware/GymResolutionMiddleware.cs` (deleted).
    Switched the constructor parameter from `IAppDbContext` to
    `AppDbContext` (the concrete type is what DI resolves to anyway, both
    are registered as scoped), so the module only needs the App.DAL.EF
    transitional ref rather than App.BLL. The two exception types
    (`ForbiddenException`, `NotFoundException`) still come from
    `App.BLL.Exceptions`; that ties Modules.Gyms to App.BLL for now and is
    listed as transitional in the csproj (Phase 10 removes it).
  - **Modules.Gyms/Api/System/**: relocated `GymsController`
    (`api/v{version:apiVersion}/system/gyms`) and `PlatformController`
    (`api/v{version:apiVersion}/system/platform`) from
    `WebApp/ApiControllers/System/` (folder deleted). Both still inject
    `IPlatformService` from `App.BLL.Contracts` â€” the actual platform
    workflow stays in App.BLL until Phase 10.
  - **Modules.Gyms/Api/Tenant/**: relocated `GymSettingsController`
    (`{gymCode}/gym-settings`) and `GymUsersController`
    (`{gymCode}/gym-users`) from `WebApp/ApiControllers/Tenant/`. They
    inject `IMaintenanceWorkflowService` (a legacy bundle that owns gym
    settings + gym user-role persistence) â€” the route belongs to Gyms but
    the workflow service moves in Phase 8. All relocated controllers
    drop the WebApp `ApiControllerBase` base class and inline `[ApiController]`
    + the standard ProblemDetails attributes directly (matches the
    AccountController pattern from Phase 4).
  - **Modules.Gyms.csproj**: added `Asp.Versioning.Mvc` 8.1.0 +
    `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.2 packages, plus
    transitional `ProjectReference`s to `App.BLL`, `App.BLL.Contracts`,
    `App.DAL.Contracts`, `App.DAL.EF`, `App.Domain`, `App.DTO`. Comment on
    the legacy refs marks them as Phase 5-9 transitional, to be removed in
    Phase 10. App.BLL was added (vs. Phase 4's csproj) because the
    relocated middleware uses `App.BLL.Exceptions`.
  - **`Api/GymsModuleExtensions.AddGymsModule`** now registers
    `IGymsModuleApi â†’ GymsModuleApiService`, calls
    `services.AddControllers().AddApplicationPart(typeof(GymsModuleMarker).Assembly)`
    so MVC discovers the four relocated controllers, and the file also
    exposes `IApplicationBuilder.UseGymResolution()` which wraps
    `UseMiddleware<GymResolutionMiddleware>()`. `WebApp/Setup/MiddlewareExtensions.UseAppPipeline`
    swapped the legacy call for `app.UseGymResolution()` (same position in
    the pipeline â€” after `UseAuthentication`, before `UseAuthorization`).
    `WebApp/Setup/HttpGymContext` now reads
    `Modules.Gyms.Infrastructure.GymResolutionMiddleware.ResolvedGymId/CodeItemKey`
    (the constants kept the same names so existing call sites need only a
    `using` change).
  - **WebApp.Tests/Unit/ApiContractMetadataTests** had to follow the moves:
    `PublicApiAssemblies` now also includes the `Modules.Gyms` assembly
    (via `typeof(Modules.Gyms.Api.System.GymsController)`), and the
    namespace filter widened from `EndsWith(".Api")` to
    `EndsWith(".Api") || Contains(".Api.")` so nested namespaces like
    `Modules.Gyms.Api.System` and `Modules.Gyms.Api.Tenant` are picked up
    by `DiscoverPublicApiControllers`. `WebApp.Tests.csproj` gained a
    project reference to `Modules.Gyms`. The full
    `PublicApiRoutes_RemainStableForFinal2Submission` expected set is
    unchanged â€” gym/system/tenant routes still match verbatim.
  - Validation: `dotnet build multi-gym-management-system.slnx` â‡’ 0 errors,
    6 warnings (pre-existing transitive NuGet vulnerability advisories on
    `Microsoft.AspNetCore.DataProtection` and
    `System.Security.Cryptography.Xml` from Phase 3-4; not introduced by
    this phase). `dotnet test --filter Architecture` â‡’ 8 passed, 1
    skipped â€” same as Phase 4. `dotnet test --filter Gym` â‡’ 29 passed,
    2 skipped (PostgreSQL integration). Full
    `dotnet test multi-gym-management-system.slnx -- xUnit.ParallelizeTestCollections=false`
    â‡’ **188 passed, 4 skipped, 0 failed** â€” identical pass count to
    Phase 4; no route regressions, switch-gym/switch-role still work via
    the existing AccountController in Modules.Users, and tenant
    middleware-driven gym resolution is unchanged from Phase 4.
- [x] **Phase 6 â€” Extract Memberships module**
  - Done 2026-05-20
  - User chose the **delegate-and-relocate** strategy (mirroring Phase 4/5):
    the five tenant API controllers move into `Modules.Memberships`,
    `IMembershipsModuleApi` exposes outward member-identity resolution, but
    member/membership/payment entities, EF configs, repositories, BLL
    workflow/workspace services, mappers, and the three Admin MVC
    controllers stay in place until Phase 9/10. Architecture-test relaxation
    already permits the transitional `App.*` refs added in Phase 4.
  - **Shared.Contracts/ModuleApis/**: added `IMembershipsModuleApi`
    (`GetMemberSummaryAsync(gymId, memberId, ct)` +
    `FindMemberForUserAsync(gymId, userId, ct)`) and the public
    `MemberSummary(Id, GymId, MemberCode, FullName, Status)` record.
    `Status` is the enum *name* as a string so `Shared.Contracts` stays
    free of `App.Domain`. Cross-module consumers (Training will be the
    first in Phase 7) depend only on these â€” never `Member` or `Person`.
  - **Modules.Memberships/Application/`MembershipsModuleApiService`**:
    implements `IMembershipsModuleApi` by querying the shared
    `AppDbContext` (`Members` + `Person` + `AppUser`). Filters out
    soft-deleted/foreign-gym rows via the existing global query filter on
    `TenantBaseEntity`. `internal sealed` â€” only `IMembershipsModuleApi`
    is the public surface. `AppDbContext` dependency is transitional
    (Phase 6-9), matching how Phase 5 keeps using the shared context until
    persistence is split per module.
  - **Modules.Memberships/Api/Tenant/**: relocated five controllers from
    `WebApp/ApiControllers/Tenant/` (folder still owns booking/equipment/
    staff/training/maintenance routes for now):
    - `MembersController` (`{gymCode}/members[/{id}|/me|/{id}/status]`,
      injects `IMemberWorkflowService`).
    - `MembershipsController` (`{gymCode}/memberships[/{id}|/{id}/status]`,
      injects `IMembershipWorkflowService`).
    - `MembershipPackagesController` (`{gymCode}/membership-packages`,
      injects `IMembershipWorkflowService`).
    - `MemberWorkspaceController` (`{gymCode}/member-workspace/{me|members/{id}}`,
      injects `IMemberWorkspaceService`).
    - `PaymentsController` (`{gymCode}/payments[/{id}/refund]`, injects
      `IMembershipWorkflowService`).
    All relocated controllers drop the WebApp `ApiControllerBase` base
    class and inline `[ApiController]` + the standard JWT auth +
    ProblemDetails attributes directly (matches the Phase 4/5 pattern).
    Route templates are byte-identical to the originals; the public
    expected route set in
    `ApiContractMetadataTests.PublicApiRoutes_RemainStableForFinal2Submission`
    needed no change.
  - **Modules.Memberships.csproj**: added `Asp.Versioning.Mvc` 8.1.0 +
    `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.2 packages, plus
    transitional `ProjectReference`s to `App.BLL.Contracts`,
    `App.DAL.Contracts`, `App.DAL.EF`, `App.Domain`, `App.DTO`. Comment on
    the legacy refs marks them as Phase 6-9 transitional, to be removed in
    Phase 10. Unlike Modules.Gyms this csproj does **not** reference
    `App.BLL` directly â€” the relocated controllers only need the
    BLL.Contracts interfaces, which `App.BLL` already implements and DI
    in `WebApp.Setup.ServiceExtensions.AddAppServices` continues to wire.
  - **`Api/MembershipsModuleExtensions.AddMembershipsModule`** now
    registers `IMembershipsModuleApi â†’ MembershipsModuleApiService` and
    calls
    `services.AddControllers().AddApplicationPart(typeof(MembershipsModuleMarker).Assembly)`
    so MVC discovers the five relocated tenant controllers. Existing
    `WebApp/Program.cs` composition is unchanged â€” it already calls
    `AddMembershipsModule(builder.Configuration)`.
  - **WebApp.Tests/Unit/ApiContractMetadataTests** had to follow the
    moves: `PublicApiAssemblies` now also includes the
    `Modules.Memberships` assembly (via
    `typeof(Modules.Memberships.Api.Tenant.MembersController)`). The
    namespace filter `EndsWith(".Api") || Contains(".Api.")` from Phase 5
    already picks up nested namespaces like
    `Modules.Memberships.Api.Tenant`, so no other discovery change was
    needed. `WebApp.Tests/Unit/TenantControllerTests.cs` now also
    `using Modules.Memberships.Api.Tenant;` so the in-process controller
    construction tests bind to the relocated `MembersController` and
    `MembershipsController` types. `WebApp.Tests.csproj` gained a project
    reference to `Modules.Memberships`.
  - Validation: `dotnet build multi-gym-management-system.slnx` â‡’ 0 errors,
    6 warnings (pre-existing transitive NuGet vulnerability advisories on
    `Microsoft.AspNetCore.DataProtection` and
    `System.Security.Cryptography.Xml` from Phase 3-5; not introduced by
    this phase). `dotnet test --filter Architecture` â‡’ 6 passed, 1
    skipped â€” same as Phase 5. `dotnet test --filter Member` â‡’ 68
    passed, 1 skipped (PostgreSQL integration). `dotnet test --filter
    Membership` â‡’ 23 passed, 0 skipped. Full
    `dotnet test multi-gym-management-system.slnx -- xUnit.ParallelizeTestCollections=false`
    â‡’ **188 passed, 4 skipped, 0 failed** â€” identical pass count to
    Phase 5; no route regressions, member/membership/membership-package/
    member-workspace/payment API contracts unchanged, Admin
    `Members`/`Memberships`/`MembershipPackages` MVC pages still hit the
    same App.BLL services. `cd client && npm test` â‡’ **32 passed, 6
    test files, 0 failed** â€” React client (CRUD pages, sessions
    booking, app routing/auth, operations) sees an unchanged API.
- [x] **Phase 7 â€” Extract Training module**
  - Done 2026-05-20
  - **Modules.Training now owns the tenant API surface** for training scope:
    relocated `StaffController`, `TrainingCategoriesController`,
    `TrainingSessionsController`, and `BookingsController` from
    `WebApp/ApiControllers/Tenant/` into `Modules.Training/Api/Tenant/`.
    Routes and action signatures remain stable; the route snapshot expected
    set did not change. A module-local `TrainingApiControllerBase` carries the
    standard JWT auth + ProblemDetails metadata formerly inherited from
    `WebApp.ApiControllers.ApiControllerBase`.
  - **Application/service ownership moved** for the defended training slice:
    `TrainingWorkflowService`, `StaffWorkflowService`, `BookingPricingService`,
    `ITrainingMapper`, and `TrainingMapper` moved from `App.BLL` into
    `Modules.Training/Application/`. They still implement the transitional
    `App.BLL.Contracts.Services` interfaces so MVC/Admin, MVC/Client, React API
    flows, and membership pricing integration keep the same contracts.
  - **Repository ownership moved** for the training UOW pieces:
    `EfTrainingCategoryRepository`, `EfTrainingSessionRepository`, and
    `EfBookingRepository` moved from `App.DAL.EF/Repositories/` into
    `Modules.Training/Infrastructure/`. `App.DAL.EF.AppUOW` now receives those
    three repositories via DI, like the Phase 4 Users refresh-token repository,
    and exposes them through `IAppUnitOfWork` until Phase 9 splits persistence.
    `AddAppPersistence` no longer registers the training repositories;
    `AddTrainingModule` does.
  - **Shared.Contracts/ModuleApis/**: added `ITrainingModuleApi` plus
    `StaffSummary` and `TrainingSessionSummary` projections. The implementation
    (`TrainingModuleApiService`) lives in `Modules.Training/Application/` and
    uses the shared `AppDbContext` transitionally, returning shared projection
    records only. This prepares Phase 8 Maintenance for staff/session lookups
    without direct module references.
  - **Modules.Training.csproj**: added ASP.NET API packages and transitional
    refs to `App.BLL`, `App.BLL.Contracts`, `App.DAL.Contracts`,
    `App.DAL.EF`, `App.Domain`, and `App.DTO`. The csproj comment marks these
    as Phase 7-9 dependencies to be removed in Phase 10.
  - **WebApp.Setup.ServiceExtensions** no longer registers training workflow,
    staff workflow, booking pricing, or training mapper services; those
    registrations now live in `TrainingModuleExtensions.AddTrainingModule`.
  - **WebApp.Tests** now references `Modules.Training`; API metadata discovery
    includes the Training assembly, controller tests import
    `Modules.Training.Api.Tenant`, and direct `AppUOW` unit-test construction
    passes the Training-owned repositories explicitly.
  - Documentation updated: `README.md`, `docs/module-boundaries.md`,
    `docs/final2-module-map.md`, assignment `docs/ai-usage.md`, root
    `docs/ai-prompts.md`, and this tracker.
  - Validation: `dotnet build multi-gym-management-system.slnx` => 0 errors,
    6 warnings (same known transitive NuGet vulnerability advisories on
    `Microsoft.AspNetCore.DataProtection` and
    `System.Security.Cryptography.Xml`). `dotnet test --filter Training` =>
    35 passed, 0 failed. `dotnet test --filter Booking` => 9 passed, 0
    failed. `dotnet test --filter Architecture` => 8 passed total across
    projects (6 architecture + 2 WebApp), 1 skipped Phase-10 boundary test.
    Full `dotnet test multi-gym-management-system.slnx --
    xUnit.ParallelizeTestCollections=false` => 188 passed, 4 skipped (1
    Phase-10 architecture skip + 3 PostgreSQL/Testcontainers opt-in tests), 0
    failed. `cd client && npm test` => 32 passed, 6 test files, 0 failed
    (existing React Router v7 future-flag warnings only).
- [x] **Phase 8 â€” Extract Maintenance module**
  - Done 2026-05-20
  - **Modules.Maintenance now owns the tenant API surface** for maintenance
    scope: relocated `EquipmentController`, `EquipmentModelsController`, and
    `MaintenanceTasksController` from `WebApp/ApiControllers/Tenant/` into
    `Modules.Maintenance/Api/Tenant/`. Routes and action signatures remain
    stable; the route snapshot expected set did not change. A module-local
    `MaintenanceApiControllerBase` carries the standard JWT auth +
    ProblemDetails metadata.
  - **Application/service ownership moved** for the defended maintenance
    slice: `MaintenanceWorkflowService`, `IMaintenanceMapper`, and
    `MaintenanceMapper` moved from `App.BLL` into
    `Modules.Maintenance/Application/`. The service still implements the
    transitional `App.BLL.Contracts.Services.IMaintenanceWorkflowService` so
    MVC Client, Gyms-owned gym-settings/gym-users controllers, and React API
    flows keep the same contract.
  - **Repository ownership moved** for maintenance UOW pieces:
    `EfMaintenanceRepository` moved from `App.DAL.EF/Repositories/` into
    `Modules.Maintenance/Infrastructure/`. `App.DAL.EF.AppUOW` now receives
    `IMaintenanceRepository` via DI, like the Users refresh-token and Training
    repositories, and exposes it through `IAppUnitOfWork` until Phase 9 splits
    persistence. `AddAppPersistence` no longer registers
    `IMaintenanceRepository`; `AddMaintenanceModule` does.
  - **Cross-module staff validation now uses Shared.Contracts**:
    `MaintenanceWorkflowService` validates assigned/actor staff through
    `ITrainingModuleApi.GetStaffSummaryAsync(...)` instead of querying
    `Repository<Staff>()` directly. The service still reloads maintenance task
    aggregates through the transitional shared `AppDbContext` repository until
    Phase 9 removes cross-module EF navigation/query coupling.
  - **Modules.Maintenance.csproj**: added ASP.NET API packages and
    transitional refs to `App.BLL`, `App.BLL.Contracts`, `App.DAL.Contracts`,
    `App.DAL.EF`, `App.Domain`, and `App.DTO`. The csproj comment marks these
    as Phase 8-9 dependencies to be removed in Phase 10.
  - **WebApp.Setup.ServiceExtensions** no longer registers maintenance
    workflow or maintenance mapper services; those registrations now live in
    `MaintenanceModuleExtensions.AddMaintenanceModule`.
  - **WebApp.Tests** now references `Modules.Maintenance`; API metadata
    discovery includes the Maintenance assembly, and direct `AppUOW` unit-test
    construction passes the Maintenance-owned repository explicitly.
  - Documentation updated: `README.md`, `docs/module-boundaries.md`,
    `docs/final2-module-map.md`, `docs/architecture.md`,
    `docs/domain-workflows.md`, `docs/testing.md`, assignment
    `docs/ai-usage.md`, root `docs/ai-prompts.md`, and this tracker.
  - Validation: `dotnet build multi-gym-management-system.slnx` => 0 errors,
    6 warnings (same known transitive NuGet vulnerability advisories on
    `Microsoft.AspNetCore.DataProtection` and
    `System.Security.Cryptography.Xml`). `dotnet test --filter Maintenance`
    => 9 passed, 0 failed. `dotnet test --filter Architecture` => 8 passed
    total across projects, 1 skipped Phase-10 boundary test. Full
    `dotnet test multi-gym-management-system.slnx --
    xUnit.ParallelizeTestCollections=false` => 188 passed, 4 skipped (1
    Phase-10 architecture skip + 3 PostgreSQL/Testcontainers opt-in tests), 0
    failed. `cd client && npm test` => 32 passed, 6 test files, 0 failed
    (existing React Router v7 future-flag warnings only).
- [x] Phase 9 - Split persistence ownership
  - Done 2026-05-20
  - Module-owned persistence boundaries are now explicit for all five target
    modules: `UsersDbContext`, `GymsDbContext`, `MembershipsDbContext`,
    `TrainingDbContext`, and `MaintenanceDbContext`.
  - Each module DbContext declares a module default schema (`users`, `gyms`,
    `memberships`, `training`, `maintenance`) so future module migrations have
    a clear schema-per-module target.
  - `AddModuleDbContext<TContext>(configuration)` is used from every module
    registration when `DefaultConnection` is configured. Architecture tests
    now verify those registrations and schema names.
  - `ModuleDbContextOwnershipTests` proves all five module contexts can
    save/read owned data; `ModuleBoundaryTests` verifies module DbContext
    placement and prevents foreign module DbContext references.
  - Intentional transition boundary: active runtime migration, seeding,
    Identity, and `IAppUnitOfWork` paths still use legacy `AppDbContext`.
    Removing that dependency center belongs to Phase 10; doing it inside
    Phase 9 would create a project-reference cycle with the current structure.
  - Validation: `dotnet test Architecture.Tests/Architecture.Tests.csproj
    --filter Architecture` => 15 passed, 1 skipped Phase-10 boundary test.
    `dotnet test multi-gym-management-system.slnx --
    xUnit.ParallelizeTestCollections=false` => Architecture.Tests 15
    passed/1 skipped and WebApp.Tests 182 passed/3 skipped PostgreSQL opt-in
    tests. Known transitive NuGet vulnerability warnings remained on
    `Microsoft.AspNetCore.DataProtection` and `System.Security.Cryptography.Xml`.
    `RUN_POSTGRES_TESTS=1 dotnet test multi-gym-management-system.slnx
    --filter PostgreSql -- xUnit.ParallelizeTestCollections=false` could not
    verify PostgreSQL because Testcontainers could not connect to Docker at
    `npipe://./pipe/docker_engine`.
- [~] **Phase 10 â€” Remove legacy App.* dependencies** (split into sub-phases
  10aâ€“10f because the previous phases were "delegate-and-relocate" â€” most
  entities, EF configurations, migrations, seeding, repositories, mappers, and
  BLL services still live in `App.Domain/App.BLL/App.DAL.EF/App.DAL.Contracts/
  App.BLL.Contracts/App.DTO`. Phase 10 has to physically relocate them into the
  owning modules' `Domain/Application/Infrastructure` folders before the
  `LegacyAppProjectsBlockedAfterPhase10` architecture test can be unskipped.)
  - [x] **Phase 10a â€” Move shared exception types to SharedKernel** â€”
    Done 2026-05-20
    - Moved 4 classes from `App.BLL/Exceptions/` to
      `SharedKernel/Exceptions/`: `ForbiddenException`, `NotFoundException`,
      `ConflictAppException`, `ValidationAppException`. Namespace changed from
      `App.BLL.Exceptions` to `SharedKernel.Exceptions`. No behaviour change â€”
      both classes already used `Exception(string)` ctors and the validation
      class kept its `IReadOnlyCollection<string> Errors` API.
    - Updated 25 callers (`App.BLL` services, WebApp middleware, WebApp Admin,
      Modules.Gyms middleware, Modules.Training workflow/staff services,
      Modules.Maintenance workflow service, four WebApp.Tests files) to swap
      `using App.BLL.Exceptions;` for `using SharedKernel.Exceptions;`.
    - `App.BLL.csproj` and `WebApp.Tests.csproj` gained a `SharedKernel`
      project reference so their consumers can compile against the relocated
      types.
    - `Modules.Gyms.csproj` and `Modules.Maintenance.csproj` removed their
      direct `App.BLL` ProjectReference â€” they only needed it for the
      exception types. Both still keep `App.BLL.Contracts` because the
      relocated controllers/services delegate to the workflow contracts.
    - `Modules.Training.csproj` keeps its `App.BLL` ref for one remaining
      reason: `App.BLL.Infrastructure.IAppDbContext` (used by
      `StaffWorkflowService` and `BookingPricingService`) physically lives in
      the `App.BLL` project even though logically it is a contract. Moving
      that interface to `App.BLL.Contracts` (or eventually `SharedKernel`)
      lands in a later Phase 10 sub-phase along with the broader persistence
      consolidation.
    - Validation: `dotnet build multi-gym-management-system.slnx` => 0 errors,
      6 warnings (same known transitive NuGet vulnerability advisories on
      `Microsoft.AspNetCore.DataProtection` and
      `System.Security.Cryptography.Xml`). `dotnet test
      multi-gym-management-system.slnx --no-build --
      xUnit.ParallelizeTestCollections=false` => Architecture.Tests 15
      passed/1 skipped (Phase-10 boundary test still skipped â€” unskipped at
      the end of Phase 10 once all App.* refs are gone), WebApp.Tests 182
      passed/3 skipped (PostgreSQL opt-in). Identical pass count to Phase 9.
    - `LegacyAppProjectsBlockedAfterPhase10` architecture test still skipped
      â€” unskip at end of Phase 10f.
  - [x] **Phase 10b â€” Move DTOs from App.DTO to Shared.Contracts** â€”
    Done 2026-05-20
    - Moved all 14 domain enums from `App.Domain/Enums/DomainEnums.cs` to
      `Shared.Contracts/Enums/DomainEnums.cs`. Namespace changed from
      `App.Domain.Enums` to `Shared.Contracts.Enums`. No values changed â€” this
      is a pure rename + relocate so EF column values stay byte-identical and
      every consumer (entities, BLL services, DAL repos, DTOs, controllers,
      Admin/Client MVC views, React-facing contracts) keeps the same enum
      cases and underlying ints.
    - Moved all 69 DTO files (50 `App.DTO/v1/**/*.cs`, the 4 EquipmentModels/
      MaintenanceTasks/MemberWorkspace nested types, and `Message.cs`) from
      `App.DTO/v1/` into `Shared.Contracts/Dtos/v1/` with the same folder
      structure (Identity, Bookings, Equipment, EquipmentModels, GymSettings,
      GymUsers, MaintenanceTasks, Members, MembershipPackages, Memberships,
      MemberWorkspace, Payments, Staff, System/, System/Platform/,
      TrainingCategories, TrainingSessions). Namespace prefix changed from
      `App.DTO.v1.*` to `Shared.Contracts.Dtos.v1.*`. No field, attribute,
      validation, or JSON property changed â€” the API contract is byte-stable
      and the React client did not need a single update.
    - Bulk-rewrote callers across the assignment with two PowerShell -replace
      passes over all .cs/.cshtml files (excluding bin/obj):
      `App.Domain.Enums` â†’ `Shared.Contracts.Enums` (150 files), and
      `App.DTO.v1` â†’ `Shared.Contracts.Dtos.v1` (78 consumer files + 1
      cross-DTO reference inside `MemberWorkspaceResponse.cs`). The two
      pass-throughs covered all `using` directives plus the handful of
      fully-qualified references (`App.Domain.Enums.X`,
      `App.DTO.v1.Members.Y`).
    - Added direct `Shared.Contracts` project references to the legacy
      projects that previously consumed enums or DTOs only through transitive
      paths: `App.Domain.csproj` (entities now reference
      `Shared.Contracts.Enums`), `App.BLL.csproj`, `App.BLL.Contracts.csproj`,
      `App.DAL.Contracts.csproj`, `App.DAL.EF.csproj`, and
      `WebApp.Tests.csproj`. `WebApp.csproj` and every `Modules.*.csproj`
      already had the ref since Phase 2.
    - Deleted the `App.DTO/` project directory entirely. Removed the
      `<Project Path="App.DTO/App.DTO.csproj" />` entry from
      `multi-gym-management-system.slnx` (20 â†’ 19 projects). Removed the
      `<ProjectReference Include="..\App.DTO\App.DTO.csproj" />` line from
      9 csprojs: `App.BLL`, `App.BLL.Contracts`, `WebApp`, `WebApp.Tests`,
      `Modules.Users`, `Modules.Gyms`, `Modules.Memberships`,
      `Modules.Training`, `Modules.Maintenance`. The DTO contract is now a
      single project (`Shared.Contracts`) owned by the modular monolith
      shared layer.
    - **Architecture.Tests/ModuleBoundaryTests** `AllowedNonModuleProjectRefsFromModules`
      list dropped `App.DTO` (it no longer needs to be transitionally allowed
      â€” modules cannot reference it because it does not exist). The
      `LegacyAppProjectsBlockedAfterPhase10` list kept `App.DTO` so the
      Phase-10f unskip will still treat any future re-add as a violation.
    - Documentation/Dockerfile references to `App.DTO` are stale historical
      mentions left as-is for now â€” they describe the Final1 layered baseline
      and the per-phase transition. Phase 10f's "delete or shrink leftover
      App.* projects" sub-phase will sweep those.
    - Validation: `dotnet build multi-gym-management-system.slnx` â‡’ 0 errors,
      6 warnings (same known transitive NuGet vulnerability advisories on
      `Microsoft.AspNetCore.DataProtection` and
      `System.Security.Cryptography.Xml` from Phase 3-10a; not introduced by
      this phase). `dotnet test --filter Architecture --no-build` â‡’
      Architecture.Tests 15 passed/1 skipped (Phase-10 boundary test still
      skipped â€” unskipped at the end of Phase 10f). Full
      `dotnet test multi-gym-management-system.slnx --no-build --
      xUnit.ParallelizeTestCollections=false` â‡’ Architecture.Tests 15
      passed/1 skipped, WebApp.Tests **182 passed / 3 skipped (PostgreSQL
      opt-in)** â€” identical pass count to Phase 10a, zero regressions.
      `cd client && npm test` â‡’ **32 passed / 6 test files / 0 failed** â€”
      React client sees an unchanged API/JSON contract.
    - `LegacyAppProjectsBlockedAfterPhase10` architecture test still skipped
      â€” unskip at end of Phase 10f.
  - [~] **Phase 10c â€” Move entities from App.Domain into each module's Domain
    folder (Users, Gyms, Memberships, Training, Maintenance)** â€” *partial:
    SharedKernel prep landed 2026-05-20; entity move itself deferred pending
    architectural decision.*
    - **What landed (10c-prep, 2026-05-20):** Moved 4 cross-cutting base types
      out of `App.Domain` into `SharedKernel/`:
      - `App.Domain/Common/ITenantEntity.cs` â†’ `SharedKernel/Common/ITenantEntity.cs`
      - `App.Domain/Common/TenantBaseEntity.cs` â†’ `SharedKernel/Common/TenantBaseEntity.cs`
      - `App.Domain/Security/AppClaimTypes.cs` â†’ `SharedKernel/Security/AppClaimTypes.cs`
      - `App.Domain/RoleNames.cs` â†’ `SharedKernel/RoleNames.cs`
      Added `Base.Contracts` + `Base.Domain` project references to
      `SharedKernel.csproj` so `TenantBaseEntity` can keep its
      `BaseEntity`/`IAuditableEntity`/`ISoftDeleteEntity` inheritance. Bulk
      rewrote 56 `.cs` files (`App.Domain.Common` â†’ `SharedKernel.Common`,
      `App.Domain.Security` â†’ `SharedKernel.Security`, `App.Domain.RoleNames`
      â†’ `SharedKernel.RoleNames`) plus 3 Razor views (`@using App.Domain` â†’
      `@using SharedKernel`). `App.Domain/Common/` and `App.Domain/Security/`
      directories deleted; `App.Domain/RoleNames.cs` deleted.
    - **Validation of 10c-prep:** `dotnet build` â‡’ 0 errors. Full
      `dotnet test multi-gym-management-system.slnx --no-build --
      xUnit.ParallelizeTestCollections=false` â‡’ Architecture.Tests 15
      passed/1 skipped + WebApp.Tests 182 passed/3 skipped (PostgreSQL
      opt-in) â€” identical to Phase 10b, zero regressions.
    - **Why the full entity move was deferred:** The cycle-resolution work
      for the entity move turned out to span beyond 10c+10d. The blocker is
      `App.BLL.Contracts/Services/`: several interfaces declare entity types
      directly in their method signatures (`IAuthorizationService` â†’
      `Member?`, `Staff?`; `IBookingPricingService` â†’ `TrainingSession`;
      `ICurrentActorResolver` â†’ `Member?`, `Staff?`;
      `IResourceAuthorizationChecker` â†’ `Booking`, `TrainingSession`;
      `ITokenService` â†’ `AppUser`, `AppUserGymRole`, `AppRefreshToken`).
      Since `Modules.*` implement these interfaces, `Modules.* â†’
      App.BLL.Contracts` is mandatory. After moving entity types into
      `Modules.*/Domain/`, `App.BLL.Contracts` would need refs back to
      `Modules.*` for the entity-typed signatures â€” creating an unbreakable
      `App.BLL.Contracts â†” Modules.*` project-reference cycle. The cycle
      cannot be resolved without either (a) moving those interfaces into
      modules' `Application/` folders (Phase 10e territory) or (b)
      rewriting every entity-typed signature to use DTOs/`Guid` IDs only
      (substantial public-API change). User decision required before the
      full 10c+10d push can proceed safely.
    - **Inventory captured during exploration (kept for the next attempt):**
      All 18 entities mapped to target modules. Cross-module navigation
      properties identified for scalar-FK rewrite: `AppUser.Person`+`GymRoles`,
      `Person.AppUser`+`StaffProfiles`, `Member.Bookings`, `Payment.Booking`,
      `PersonContact.Contact`, `Contact.PersonLinks`, `AppUserGymRole.AppUser`,
      `Staff.Person`+`AssignedTasks`+`CreatedTasks`, `Booking.Member`+`Payments`,
      `MaintenanceTask.AssignedStaff`+`CreatedByStaff`. Query rewrites
      needed in `MembershipsModuleApiService.FindMemberForUserAsync`
      (`member.Person.AppUser.Id` chain) and any `.Include()` chain touching
      a removed nav.
    - **Users slice (impls only) landed 2026-05-20:** Per user direction to
      bite off one module at a time, moved the 6 Users-owned BLL
      *implementations* from `App.BLL/Services|Mappers/` into
      `Modules.Users/Application/` (interfaces still live in
      `App.BLL.Contracts` until a later slice):
      - `App.BLL/Services/AccountAuthService.cs` â†’
        `Modules.Users/Application/AccountAuthService.cs`
      - `App.BLL/Services/IdentityService.cs` â†’
        `Modules.Users/Application/IdentityService.cs`
      - `App.BLL/Services/TokenService.cs` â†’
        `Modules.Users/Application/TokenService.cs`
      - `App.BLL/Services/UserContextService.cs` â†’
        `Modules.Users/Application/UserContextService.cs`
      - `App.BLL/Services/CurrentActorResolver.cs` â†’
        `Modules.Users/Application/CurrentActorResolver.cs`
      - `App.BLL/Mappers/AuthResponseMapper.cs` â†’
        `Modules.Users/Application/Mappers/AuthResponseMapper.cs`
      Each file's namespace changed from `App.BLL.Services` /
      `App.BLL.Mappers` to `Modules.Users.Application` /
      `Modules.Users.Application.Mappers`. Each moved file gained an
      explicit `using App.BLL.Contracts.Services;` directive because the
      App.BLL `global using App.BLL.Contracts.Services;` (in
      `App.BLL/GlobalUsings.cs`) does not flow to Modules.Users.
      `AuthResponseMapper.cs` additionally gained `using App.BLL.Mappers;`
      so it can still bind to the unmoved `IAuthResponseMapper` interface.
    - **DI rewiring (Users slice):** the 6 service registrations moved from
      `WebApp/Setup/ServiceExtensions.AddAppServices` into
      `Modules.Users/Api/UsersModuleExtensions.AddUsersModule`. The DI
      registrations bind the same App.BLL.Contracts interface keys to the
      now-Modules.Users implementations, so every consumer compiles and
      resolves identically. `WebApp/Setup/ServiceExtensions` keeps the
      remaining 27 service registrations (Gyms/Memberships/Training/
      Maintenance and Admin/Client page services). A comment notes which 6
      moved out.
    - **Bulk consumer impact (Users slice):** Only 2 test files needed an
      added `using Modules.Users.Application;` directive
      (`WebApp.Tests/Unit/AuthorizationServiceTests.cs` and
      `MaintenanceWorkflowServiceTests.cs`) because they construct
      `CurrentActorResolver` directly in unit-test arrange code. All other
      callers (production code, integration tests) bind via the interface
      and saw no change.
    - **Validation (Users slice):** `dotnet build
      multi-gym-management-system.slnx` â‡’ 0 errors. Full `dotnet test
      multi-gym-management-system.slnx --no-build --
      xUnit.ParallelizeTestCollections=false` â‡’ Architecture.Tests 15
      passed / 1 skipped + WebApp.Tests **182 passed / 3 skipped** â€”
      identical to the pre-slice baseline, zero regressions.
    - **What still belongs to the Users slice for a future session** (before
      the Users module can drop App.* refs): move the 5 Users-owned BLL
      *interfaces* (IAccountAuthService, IIdentityService, ITokenService,
      IUserContextService + UserExecutionContext, ICurrentActorResolver) to
      `Modules.Users/Application/` (some need signature rewrites because
      they take cross-module entity types like `Member?`, `Staff?`,
      `AppUserGymRole?`); move the 3 identity entity types (`AppUser`,
      `AppRole`, `AppRefreshToken`) from `App.Domain/Identity/` to
      `Modules.Users/Domain/Identity/`; split the identity portion of
      `App.DAL.EF/Configurations/IdentityAndPlatformConfigurations.cs`
      into a Users-owned EF configuration; then Modules.Users can drop its
      transitional `App.BLL`, `App.BLL.Contracts`, `App.DAL.EF`, and
      `App.Domain` project references. `IRefreshTokenRepository` was moved
      into `Modules.Users/Application/Persistence/` and
      `IAuthResponseMapper` was moved into
      `Modules.Users/Application/Mappers/` in later Phase 10 slices. The same
      sub-slicing template should repeat for each of the remaining modules
      (Gyms, Memberships, Training, Maintenance) in subsequent sessions.
  - [~] **Phase 10d â€” Move EF configurations, repositories, AppDbContext,
    migrations, and seeding from App.DAL.EF/App.DAL.Contracts into each
    owning module's Infrastructure folder** â€” *partial: repository ownership
    slices landed; DbContext/migrations/seeding cutover remains blocked until
    Phase 10e moves remaining services/contracts out of the App.* dependency
    center.*
    - **Users refresh-token repository slice landed 2026-05-20:** moved
      `IRefreshTokenRepository` out of `App.DAL.Contracts/Persistence/` into
      `Modules.Users/Application/Persistence/`. `AccountAuthService` and
      `IdentityService` now inject that module-owned repository directly
      instead of going through `IAppUnitOfWork.RefreshTokens`.
    - Removed `RefreshTokens` from the transitional `IAppUnitOfWork` and
      removed the corresponding constructor dependency from `App.DAL.EF.AppUOW`.
      Four unit tests that instantiate `AppUOW` directly were updated to the
      new constructor shape.
    - `Modules.Users.csproj` dropped its direct `App.DAL.Contracts` reference.
      It still keeps transitional `App.BLL.Contracts`, `App.DAL.EF`, and
      `App.Domain` references because Users services still consume the legacy
      `IAppDbContext`, active `AppDbContext` migration path, and identity
      entity types.
    - Runtime compatibility note: `EfRefreshTokenRepository` remains backed by
      the active `AppDbContext` table for now. Switching it to
      `UsersDbContext` requires the later module migration/cutover step so the
      production table/schema exists.
    - Validation: `dotnet build multi-gym-management-system.slnx` => 0 errors
      with the known transitive NuGet vulnerability warnings. `dotnet test
      multi-gym-management-system.slnx --no-build --filter
      "Account|Auth|Architecture" -- xUnit.ParallelizeTestCollections=false`
      => Architecture.Tests 15 passed/1 skipped and WebApp.Tests 40 passed.
      Full `dotnet test multi-gym-management-system.slnx --no-build --
      xUnit.ParallelizeTestCollections=false` => Architecture.Tests 15
      passed/1 skipped, WebApp.Tests 182 passed/3 skipped PostgreSQL opt-in.
    - **IAppDbContext / App.BLL direct-reference slice landed 2026-05-20:**
      moved `IAppDbContext` from `App.BLL/Contracts/Infrastructure/` to
      `App.BLL.Contracts/Infrastructure/`. This is transitional: the interface
      still exposes EF `DbSet<App.Domain...>` types, so moving it to
      `SharedKernel` would create a `SharedKernel`/`App.Domain` cycle before
      the entity move is finished.
    - Updated all `App.BLL.Infrastructure` imports to
      `App.BLL.Contracts.Infrastructure`. `App.BLL.Contracts.csproj` now has
      the required `Microsoft.EntityFrameworkCore` reference for this
      temporary EF-shaped contract.
    - Removed `App.BLL` from `App.DAL.EF.csproj`; `AppDbContext` now depends
      only on `App.BLL.Contracts` for `IAppDbContext`. Removed the direct
      `App.BLL` reference from `Modules.Training.csproj`.
    - Build exposed a hidden Users transitive dependency on
      `App.BLL.Mappers.IAuthResponseMapper`, so that mapper interface moved to
      `Modules.Users/Application/Mappers/` beside its implementation. Modules
      no longer have direct `App.BLL` project references.
    - Validation: `dotnet build multi-gym-management-system.slnx` => 0 errors
      with the known transitive NuGet vulnerability warnings. Boundary scans
      found no module direct `App.BLL` project references. `dotnet test
      multi-gym-management-system.slnx --no-build --filter
      "Architecture|Training|Account|Auth" --
      xUnit.ParallelizeTestCollections=false` => Architecture.Tests 15
      passed/1 skipped and WebApp.Tests 71 passed. Full `dotnet test
      multi-gym-management-system.slnx --no-build --
      xUnit.ParallelizeTestCollections=false` => Architecture.Tests 15
      passed/1 skipped, WebApp.Tests 182 passed/3 skipped PostgreSQL opt-in.
    - **Gyms authorization-query repository slice landed 2026-05-20:**
      moved `IAuthorizationQueryRepository` and `AuthorizationGymLookup` from
      `App.DAL.Contracts/Persistence/` into
      `Modules.Gyms/Application/Persistence/`, and moved
      `EfAuthorizationQueryRepository` from `App.DAL.EF/Repositories/` into
      `Modules.Gyms/Infrastructure/`. `TenantAccessChecker` now consumes the
      Gyms-owned contract while the implementation still reads through the
      active transitional `AppDbContext`.
    - `AddGymsModule` now registers
      `IAuthorizationQueryRepository -> EfAuthorizationQueryRepository`;
      `AddAppPersistence` no longer registers that query repository.
      `Modules.Gyms.csproj` dropped its direct `App.DAL.Contracts` reference.
      `App.BLL.csproj` temporarily references `Modules.Gyms` while the
      remaining legacy BLL service lives in `App.BLL`.
    - Two unit-test files that directly construct the EF authorization query
      repository were updated to use `Modules.Gyms.Infrastructure`.
    - Validation: `dotnet build multi-gym-management-system.slnx` => 0 errors
      with the known transitive NuGet vulnerability warnings. `dotnet test
      multi-gym-management-system.slnx --no-build --filter
      "Architecture|Auth|Gym" -- xUnit.ParallelizeTestCollections=false` =>
      Architecture.Tests 15 passed/1 skipped and WebApp.Tests 61 passed/2
      skipped PostgreSQL opt-in. Full `dotnet test
      multi-gym-management-system.slnx --no-build --
      xUnit.ParallelizeTestCollections=false` => Architecture.Tests 15
      passed/1 skipped, WebApp.Tests 182 passed/3 skipped PostgreSQL opt-in.
    - **Training repository contract slice landed 2026-05-20:** moved
      `ITrainingCategoryRepository`, `ITrainingSessionRepository`, and
      `IBookingRepository` from `App.DAL.Contracts/Persistence/` into
      `Modules.Training/Application/Persistence/`. Training workflow,
      admin/client query services, and payment logic now inject the
      Training-owned repository contracts directly.
    - Removed `TrainingCategories`, `TrainingSessions`, and `Bookings` from
      the transitional `IAppUnitOfWork`; `App.DAL.EF.AppUOW` no longer
      accepts Training repository constructor dependencies, so `App.DAL.EF`
      does not need to reference `Modules.Training`.
    - `App.BLL.csproj` now temporarily references `Modules.Training` because
      some legacy BLL services still consume Training-owned repository
      contracts until their application-service ownership moves in Phase 10e.
      `Modules.Training` still keeps its transitional `App.DAL.Contracts`,
      `App.DAL.EF`, and `App.Domain` refs because it still uses the shared
      UOW save/generic-repository bridge, active `AppDbContext`, and unmoved
      training entities.
    - Runtime compatibility note: the Training EF repositories remain backed
      by the active `AppDbContext` tables. Switching to `TrainingDbContext`
      remains a later module migration/cutover step.
    - Validation: `dotnet build multi-gym-management-system.slnx` => 0
      errors with the known transitive NuGet vulnerability warnings.
      `dotnet test multi-gym-management-system.slnx --no-build --filter
      "Architecture|Training|Sessions|Payment" --
      xUnit.ParallelizeTestCollections=false` => Architecture.Tests 15
      passed/1 skipped and WebApp.Tests 41 passed. Solution-level full
      `dotnet test` timed out and left an orphaned `vstest.console` process,
      which was stopped. Retried by project: `dotnet test
      WebApp.Tests/WebApp.Tests.csproj --no-build --
      xUnit.ParallelizeTestCollections=false` => 182 passed/3 skipped
      PostgreSQL opt-in; `dotnet test
      Architecture.Tests/Architecture.Tests.csproj --no-build --
      xUnit.ParallelizeTestCollections=false` => 15 passed/1 skipped.
    - **Maintenance repository contract slice landed 2026-05-20:** moved
      `IMaintenanceRepository` from `App.DAL.Contracts/Persistence/` into
      `Modules.Maintenance/Application/Persistence/`. `MaintenanceWorkflowService`
      now injects the Maintenance-owned repository contract directly and keeps
      the transitional `IAppUnitOfWork` only for `SaveChangesAsync`.
    - Removed `Maintenance` from the transitional `IAppUnitOfWork`;
      `App.DAL.EF.AppUOW` no longer accepts an `IMaintenanceRepository`
      constructor dependency. Unit tests that construct `AppUOW` directly were
      updated to the new constructor shape. `AdminOperationsQueryService` and
      `ClientDashboardQueryService` now inject `IMaintenanceRepository`
      directly for their maintenance read models; `App.BLL.csproj` temporarily
      references `Modules.Maintenance` until those legacy BLL query services
      move in Phase 10e.
    - Runtime compatibility note: the Maintenance EF repository remains in
      `Modules.Maintenance/Infrastructure/` and remains backed by the active
      `AppDbContext` tables. Switching it to `MaintenanceDbContext` remains a
      later module migration/cutover step.
    - Validation: `dotnet build multi-gym-management-system.slnx` => 0
      errors with the known transitive NuGet vulnerability warnings.
      `dotnet test multi-gym-management-system.slnx --no-build --filter
      "Architecture|Maintenance" -- xUnit.ParallelizeTestCollections=false`
      => Architecture.Tests 15 passed/1 skipped and WebApp.Tests 11 passed.
      Full `dotnet test multi-gym-management-system.slnx --no-build --
      xUnit.ParallelizeTestCollections=false` => Architecture.Tests 15
      passed/1 skipped, WebApp.Tests 182 passed/3 skipped PostgreSQL opt-in.
    - **Memberships repository contract slice landed 2026-05-21:** moved
      `IMemberRepository`, `IMembershipPackageRepository`,
      `IMembershipRepository`, and `IPaymentRepository` from
      `App.DAL.Contracts/Persistence/` into
      `Modules.Memberships/Application/Persistence/`. Moved the matching EF
      implementations from `App.DAL.EF/Repositories/` into
      `Modules.Memberships/Infrastructure/`, still backed by the active shared
      `AppDbContext` tables until module migrations/schema cutover.
    - Removed `Members`, `MembershipPackages`, `Memberships`, and `Payments`
      from the transitional `IAppUnitOfWork`; `App.DAL.EF.AppUOW` now only
      exposes the generic repository bridge and `SaveChangesAsync`.
      `AddMembershipsModule` registers the Memberships-owned repositories, and
      `AddAppPersistence` no longer registers them.
    - Updated legacy Memberships-owned BLL services and the admin membership
      package page service to inject the Memberships-owned repository
      contracts directly while keeping `IAppUnitOfWork` only for
      `SaveChangesAsync` and generic transitional access. `App.BLL.csproj`
      temporarily references `Modules.Memberships` until those services move
      into the module in Phase 10e.
    - Updated `TrainingWorkflowService.CreateBookingAsync` to validate members
      via `IMembershipsModuleApi` instead of the old UOW/member repository
      path, preserving module boundaries. The immediate create response uses a
      mapper overload fed by `MemberSummary` so it does not reattach a
      cross-module `Member` entity navigation.
    - Validation: `dotnet build multi-gym-management-system.slnx` => 0
      errors with the known transitive NuGet vulnerability warnings. Boundary
      scans found no remaining `unitOfWork.Members*`,
      `unitOfWork.Membership*`, or `unitOfWork.Payments` calls and no
      `App.DAL.Contracts` reference from `Modules.Memberships`. Targeted
      `dotnet test multi-gym-management-system.slnx --no-build --filter
      "Architecture|Member|Membership|Payment|Training|Proposal" --
      xUnit.ParallelizeTestCollections=false` => Architecture.Tests 15
      passed/1 skipped and WebApp.Tests 107 passed/1 skipped PostgreSQL
      opt-in. Full `dotnet test multi-gym-management-system.slnx --no-build
      -- xUnit.ParallelizeTestCollections=false` => Architecture.Tests 15
      passed/1 skipped, WebApp.Tests 182 passed/3 skipped PostgreSQL opt-in.
      `RUN_POSTGRES_TESTS=1 dotnet test ... --filter PostgreSql` was
      attempted but could not connect to Docker/Testcontainers at
      `npipe://./pipe/docker_engine`, so PostgreSQL opt-in validation remains
      unverified on this machine.
  - [~] **Phase 10e — Move remaining BLL services from App.BLL into each
    owning module's Application folder; move IAppDbContext and other
    cross-cutting infrastructure abstractions to SharedKernel** — *partial:
    concrete BLL ownership landed 2026-05-21; IAppDbContext remains blocked
    until the entity-shaped contract is removed.*
    - Moved the remaining concrete `App.BLL` implementations into owning
      boundaries:
      - Gyms-owned platform/tenant/authorization services moved to
        `Modules.Gyms/Application/{Authorization,Platform}/`.
      - Membership/member/payment workflows plus membership mappers moved to
        `Modules.Memberships/Application/`.
      - MVC read-model query services moved to `WebApp/Areas/*/Queries/`
        because they aggregate module data for presentation rather than own
        domain behavior.
    - `Modules.Gyms.Api.GymsModuleExtensions` now registers
      `IPlatformService`, `IWorkspaceContextService`, `ITenantAccessChecker`,
      `IResourceAuthorizationChecker`, `IAuthorizationService`, and
      `ISubscriptionTierLimitService` against Gyms-owned implementations.
      `Modules.Memberships.Api.MembershipsModuleExtensions` now registers the
      member/membership/payment workflow services and mappers. `WebApp`
      registers only WebApp-owned MVC page/query services.
    - Removed `WebApp` and `WebApp.Tests` project references to the concrete
      `App.BLL` project and shrank `App.BLL.csproj` to an empty compatibility
      project. Added an architecture test proving `WebApp` and all
      `Modules.*` projects do not reference `App.BLL`.
    - Added `BookingSummary`, `GetBookingSummaryAsync`, and
      `ListBookingIdsForMemberAsync` to `ITrainingModuleApi` so
      `PaymentService` no longer imports Training repositories directly.
      Added scalar-id `EnsureBookingAccessAsync(...)` authorization overloads
      so Memberships can authorize a Training-owned booking projection without
      a direct module reference.
    - `IAppDbContext` was **not** moved to `SharedKernel`: the current
      interface exposes `DbSet<App.Domain...>` members. Moving it as-is would
      create a project-reference cycle (`SharedKernel -> App.Domain ->
      Shared.Contracts -> SharedKernel`). Phase 10f must either split this
      into module-owned persistence contracts or replace the remaining
      entity-shaped access with module APIs before unskipping the full
      legacy-App boundary test.
    - Validation: `dotnet build multi-gym-management-system.slnx` passed with
      the known transitive NuGet vulnerability warnings. `dotnet test
      multi-gym-management-system.slnx --filter Architecture --
      xUnit.ParallelizeTestCollections=false` passed: Architecture.Tests 16
      passed/1 skipped and WebApp.Tests 2 passed. Targeted backend regression
      `dotnet test ... --no-build --filter
      "Architecture|Auth|Gym|Member|Membership|Payment|Training|Maintenance|Dashboard|Sessions"
      -- xUnit.ParallelizeTestCollections=false` passed:
      Architecture.Tests 16 passed/1 skipped and WebApp.Tests 146 passed/2
      skipped PostgreSQL opt-in. Full backend `dotnet test ... --no-build --
      xUnit.ParallelizeTestCollections=false` passed: Architecture.Tests 16
      passed/1 skipped and WebApp.Tests 182 passed/3 skipped PostgreSQL
      opt-in.
  - [~] **Phase 10f — Unskip
    `LegacyAppProjectsBlockedAfterPhase10`, delete or shrink leftover App.*
    projects, update the solution file, tighten architecture tests, full
    backend + React client validation** — *partial: shared module persistence
    plumbing moved to SharedKernel 2026-05-21; full unskip remains blocked by
    remaining direct module references to `App.BLL.Contracts`, `App.DAL.EF`,
    `App.DAL.Contracts`, and `App.Domain`.*
    - Moved the cross-module persistence helpers from the legacy EF project
      into `SharedKernel/Persistence/`: `IGymContext`,
      `ModuleDbContextBase<TContext>`, and
      `ModuleDbContextRegistrationExtensions.AddModuleDbContext<TContext>()`.
      Module DbContexts, module registrations, `AppDbContext`, design-time
      factory, WebApp tenant setup, and affected tests now import
      `SharedKernel.Persistence` instead of `App.DAL.EF.Tenant` or
      `App.DAL.EF.ModularPersistence`.
    - Deleted the old `App.DAL.EF/Tenant` and
      `App.DAL.EF/ModularPersistence` source files so shared module
      persistence infrastructure is no longer owned by the compatibility EF
      bridge. Added a direct `App.DAL.EF -> SharedKernel` reference because
      the legacy bridge still consumes `IGymContext` during the transition.
    - Added an architecture test proving the shared persistence files live in
      `SharedKernel` and no longer exist under `App.DAL.EF`.
    - `LegacyAppProjectsBlockedAfterPhase10` was **not** unskipped. Current
      blockers are still real code dependencies, not stale references:
      module API/application/repository code still uses service contracts in
      `App.BLL.Contracts`, active EF repositories and module API query
      services still use `AppDbContext` from `App.DAL.EF`, Training and
      Maintenance workflows still use the transitional `IAppUnitOfWork`, and
      entity/repository contracts still expose `App.Domain` entity types.
      Repository cutover to module DbContexts was deliberately deferred
      because several current repositories include cross-module navigations
      that the module DbContexts intentionally ignore until module migrations
      and schema/table ownership are ready.
    - Validation so far: `dotnet build multi-gym-management-system.slnx`
      passed with the known transitive NuGet vulnerability warnings.
      `dotnet test multi-gym-management-system.slnx --no-build --filter
      Architecture -- xUnit.ParallelizeTestCollections=false` passed:
      Architecture.Tests 17 passed/1 skipped and WebApp.Tests 2 passed.
    - Full validation after docs update: `dotnet test
      multi-gym-management-system.slnx --no-build --
      xUnit.ParallelizeTestCollections=false` passed: Architecture.Tests 17
      passed/1 skipped and WebApp.Tests 182 passed/3 PostgreSQL opt-in
      skipped. `cd client && npm test -- --run` passed: 32 tests across 6
      files, with existing React Router v7 future-flag warnings only.
      `cd client && npm run build` passed (`tsc --noEmit` + Vite build).
- [x] **Phase 11 â€” Admin UX regression and no ViewBag/ViewData**
  - Done 2026-05-22.
  - Ran the full Admin MVC regression slice after the Phase 10 modularization
    work. Existing Admin CRUD flows for members, training categories, and
    membership packages still pass through the module-registered workflow
    services; no Admin controller uses `DbContext` directly.
  - Hardened `WebApp.Tests/Integration/MvcComplianceTests.cs` with additional
    Phase 11 guardrails:
    - anonymous access is blocked for every Admin index/create route covered by
      the regression slice;
    - every Admin controller must be `[Authorize]` protected and must not use
      `[AllowAnonymous]`;
    - Admin controllers must not import concrete `App.BLL`, `App.DAL`, or EF
      `DbContext` infrastructure;
    - the entire Admin area (`.cs` + `.cshtml`) must remain free of
      `ViewBag`/`ViewData`.
  - Transitional note: Admin controllers/page services still consume shared
    service contracts from `App.BLL.Contracts` where the concrete behavior is
    registered by modules. This phase deliberately did not move those
    contracts because Phase 10f still records broader dependency-center
    blockers around entity-shaped persistence contracts.
  - Validation: `dotnet test multi-gym-management-system.slnx --filter Admin
    -- xUnit.ParallelizeTestCollections=false` passed with WebApp.Tests 65
    passed, 0 skipped, 0 failed (Architecture.Tests has no Admin-filtered
    tests). Known NuGet vulnerability warnings on
    `Microsoft.AspNetCore.DataProtection` and
    `System.Security.Cryptography.Xml` still appear. `rg -n
    "ViewBag|ViewData" WebApp\Areas\Admin` returned no matches.
- [x] **Phase 12 â€” React client regression and CORS/deploy split**
  - Done 2026-05-25.
  - Ran the React regression suite and production build after the modularized
    backend changes; both passed with the existing React Router v7 future-flag
    warnings only.
  - Fixed Final2 deployment drift by adding a root child-pipeline trigger for
    `courses/webapp-csharp/assignment05_final2`, renaming the assignment-local
    jobs to `assignment05_final2_*`, and changing job paths/tags from the
    copied Assignment 03 folder to the Final2 folder.
  - Kept the API base URL contract stable: Vite still uses
    `VITE_API_BASE_URL` when configured, same-origin in production when it is
    absent, and `https://localhost:7245` during local development.
  - Preserved and revalidated JWT attachment, refresh-token retry,
    `Accept-Language`, 401 refresh/logout handling, and visible 403 API error
    handling through the existing client tests.
  - Hardened the separate-client deployment path: `docker-compose.prod.yml`
    renders the backend with both backend and standalone-client production CORS
    origins, the standalone client profile renders with
    `VITE_API_BASE_URL=https://mtiker-cweb-4.proxy.itcollege.ee`, deploy
    scripts now default `COMPOSE_PROJECT_NAME` to `assignment05-final2`, and
    `scripts/smoke-deploy.sh` now checks CORS preflight from `CLIENT_URL`.
  - Updated root and assignment deployment documentation plus the React login
    eyebrow label so Final2 no longer presents as Assignment 03 in the checked
    surfaces.
  - Validation: `npm test` passed 32 tests across 6 files; `npm run build`
    passed; `dotnet test multi-gym-management-system.slnx --filter Cors --
    xUnit.ParallelizeTestCollections=false` passed WebApp.Tests 8 tests;
    production `docker compose -f docker-compose.prod.yml config` passed;
    production `docker compose --profile client -f docker-compose.prod.yml
    config` passed; full backend `dotnet test ... --no-build --
    xUnit.ParallelizeTestCollections=false` passed Architecture.Tests 17
    passed/1 skipped and WebApp.Tests 197 passed/3 PostgreSQL opt-in skipped;
    `bash -n scripts/smoke-deploy.sh scripts/deploy-client.sh
    scripts/deploy.sh` passed with Git Bash. PyYAML/Ruby were unavailable, so
    standalone YAML parser validation was not run.
- [x] **Phase 13 â€” Final coverage and traceability hardening**
  - Done 2026-05-25.
  - Added `docs/final2-architecture.md` with the defended modular-monolith
    runtime shape, module boundaries, mediator communication, persistence
    state, security/tenancy model, localization, CI/CD posture, and remaining
    architecture risks.
  - Added `docs/final2-traceability.md` mapping each Final2 requirement to
    implementation files/areas, automated evidence, and remaining caveats.
    Updated `docs/final2-defense.md`, `docs/README.md`, assignment `README.md`,
    and `docs/testing.md` so defense and validation docs no longer describe
    Final1 as the active state.
  - Added API metadata coverage in
    `WebApp.Tests/Unit/ApiContractMetadataTests.cs` proving all public module
    API controllers expose `[ApiController]`, `[ApiVersion("1.0")]`, and
    URL-segment versioned routes. Added `[ApiController]` to
    `Modules.Users.Api.AccountController` to align the account API with the
    rest of the REST controller surface.
  - Added Swagger smoke coverage in
    `WebApp.Tests/Integration/SmokeTests.cs` proving `/swagger/v1/swagger.json`
    renders, exposes representative v1 API paths, and includes JWT bearer
    OpenAPI security metadata. This caught and fixed a real Swagger generation
    defect by marking MVC `HomeController` with
    `[ApiExplorerSettings(IgnoreApi = true)]`, keeping MVC routes out of the
    API explorer.
  - Validation: `dotnet format multi-gym-management-system.slnx
    --verify-no-changes` passed with workspace-load warnings only;
    `dotnet build multi-gym-management-system.slnx` passed with the known
    transitive NuGet advisories on `Microsoft.AspNetCore.DataProtection` and
    `System.Security.Cryptography.Xml`; full backend `dotnet test
    multi-gym-management-system.slnx -- xUnit.ParallelizeTestCollections=false`
    passed with Architecture.Tests 17 passed/1 skipped and WebApp.Tests 199
    passed/3 PostgreSQL opt-in skipped; `npm test -- --run` passed 32 tests
    across 6 files with existing React Router v7 future warnings; `npm run
    build` passed.
- [~] **Phase 14 - Final2 deployment smoke test** - *partial: local Docker smoke passes; public deployment still not passing*
  - Added Swagger/OpenAPI metadata and refresh-token renewal checks to
    `scripts/smoke-deploy.sh`. The smoke path now covers backend health,
    Swagger JSON, standalone client health, production CORS preflight, API
    login, refresh-token renewal, and an authenticated tenant API read using
    the renewed JWT.
  - Added optional `SMOKE_CORS_ORIGIN` support so local container smoke can
    probe `localhost` backend/client ports while still testing the public
    standalone-client origin configured in production CORS. Replaced the
    dumped-header `grep` check with a Python/Node parser after Git Bash `grep`
    aborted on the curl header file during local smoke.
  - Production Compose rendering validated for both backend-only and
    backend-plus-standalone-client profile with explicit backend/client CORS
    origins.
  - Docker Desktop was started successfully and
    `docker compose --profile client -f docker-compose.prod.yml build` passed,
    building both the backend image with embedded React client assets and the
    standalone nginx client image.
  - Local production-stack smoke passed with project
    `assignment05-final2-smoke`, backend on `http://localhost:18083`,
    standalone client on `http://localhost:18081`, PostgreSQL using the Compose
    volume, and `SMOKE_CORS_ORIGIN=https://mtiker-cweb-4-client.proxy.itcollege.ee`.
    The smoke covered backend `/health`, Swagger JSON, standalone client
    `/healthz`, production-origin CORS preflight, API login, refresh-token
    renewal, and authenticated `peak-forge` maintenance-task read.
  - Live smoke was attempted against
    `https://mtiker-cweb-4.proxy.itcollege.ee` and
    `https://mtiker-cweb-4-client.proxy.itcollege.ee` with the seeded
    `admin@peakforge.local` / `peak-forge` scenario after the local smoke
    passed. The script still stopped at backend `/health` because the public
    backend returned HTTP 404.
  - Validation completed:
    `C:\Program Files\Git\bin\bash.exe -n scripts/smoke-deploy.sh
    scripts/deploy-client.sh scripts/deploy.sh` passed;
    `docker compose -f docker-compose.prod.yml config` passed;
    `docker compose --profile client -f docker-compose.prod.yml config`
    passed; `docker compose --profile client -f docker-compose.prod.yml build`
    passed; local production-stack `scripts/smoke-deploy.sh` passed; public
    `scripts/smoke-deploy.sh` failed at backend `/health` with HTTP 404.
  - Remaining to complete Phase 14: deploy the current backend and standalone
    client images on the VPS or fix the public proxy target, then rerun
    `scripts/smoke-deploy.sh` until the public backend/client/API checks pass.

Risk-fix prompts (1â€“10) at the bottom of the prompts file are not phases;
apply them as needed when their problem surfaces.

## How to pick up

1. Read `assignment05_final2_codex_prompts.md` for the full phase definition.
2. Find the first `[~]` or `[ ]` entry above - that is your phase.
3. Do **only that one phase**, run the phase's validation commands, then
   update this file (check the box, add date and a one-line note).
4. Do not start the next phase in the same session unless the user asks.
