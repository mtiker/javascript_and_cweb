# Current Test Inventory

**Audited:** 2026-05-11
**Test project:** `tests/WebApp.Tests/` (xUnit 2, .NET 10)
**React tests:** `client/src/` (Vitest 2)

---

## How to run

```bash
# Backend tests (from assignment root)
dotnet test multi-gym-management-system.slnx

# Backend tests with PostgreSQL (requires Docker)
RUN_POSTGRES_TESTS=1 dotnet test multi-gym-management-system.slnx

# React tests (from client/)
cd client && npm ci && npm test
```

## Latest validation result

2026-05-11 local run:

| Command | Result |
|---|---|
| `dotnet format multi-gym-management-system.slnx --verify-no-changes` | Pass |
| `dotnet build multi-gym-management-system.slnx` | Pass, 0 warnings, 0 errors |
| `dotnet test multi-gym-management-system.slnx` | Pass, 250 passed, 3 skipped |
| `cd client && npm test` | Pass, 34 frontend tests |
| `cd client && npm run build` | Pass |
| `docker compose config` | Pass |
| `POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key VITE_API_BASE_URL=https://api.example.test docker compose -f docker-compose.prod.yml config` | Pass |
| `POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key VITE_API_BASE_URL=https://api.example.test docker compose --profile client -f docker-compose.prod.yml config` | Pass |

The 3 skipped backend tests are the opt-in PostgreSQL/Testcontainers tests.
Public deployment smoke was not run in this pass.

---

## 1. Backend — Unit Tests

### `tests/WebApp.Tests/Unit/AuthorizationServiceTests.cs`

**Subject:** `AuthorizationService` (BLL) — IDOR protection logic

| Test method | What it covers | IDOR? |
|-------------|---------------|-------|
| `EnsureTenantAccessAsync_RejectsWhenActiveGymContextIsMissing` | 403 when JWT has no active gym | Yes |
| `EnsureTenantAccessAsync_RejectsWhenRouteGymDiffersFromActiveGym` | 403 when active gym ≠ route gymCode | Yes |
| `EnsureTenantAccessAsync_RejectsWhenAllowedRoleIsMissing` | 403 when role not in allowed list | Yes |
| `EnsureTenantAccessAsync_ReturnsActiveGymIdWhenContextAndRoleMatch` | Successful tenant access through the authorization query repository | Yes |
| `EnsureMemberSelfAccessAsync_AllowsOwnMemberAndRejectsOthers` | Member can read self, 403 on another member | Yes |
| `EnsureBookingAccessAsync_TrainerMustBeAssignedToSession` | Trainer can access booking only if assigned to session | Yes |
| `EnsureMaintenanceTaskAccessAsync_CaretakerMustBeAssigned` | Caretaker can access task only if assigned | Yes |

---

### `tests/WebApp.Tests/Unit/TenantControllerTests.cs`

**Subject:** Tenant API controllers — parameter forwarding and response shapes

| Test class / pattern | Coverage |
|---------------------|---------|
| Members: GET list, GET one, POST, PUT, DELETE | Response shape, BLL delegation |
| Bookings: GET, POST, PUT attendance, DELETE | Response shape |
| Coaching plans: GET, POST, PUT, DELETE | Response shape |

---

### `tests/WebApp.Tests/Unit/TrainingModuleMediatorTests.cs`

**Subject:** Training module mediator handlers

| Test method | What it covers |
|-------------|---------------|
| `Mediator_DispatchesTrainingCategoryCrudMessagesThroughModuleOwnedWorkflow` | Category list/create/update/delete through `Modules.Training.Application` handlers, with no `ITrainingWorkflowService` category delegation |
| `Mediator_TrainingCategoryUpdateAndDeleteRemainTenantScoped` | Foreign-gym category IDs return not-found through tenant-scoped repository lookup |
| Session and booking mediator tests | Remaining Training handlers still dispatch through the transitional workflow-service adapters |

---

### `tests/WebApp.Tests/Unit/MembershipFinanceModuleMediatorTests.cs`

**Subject:** MembershipFinance module mediator handlers

| Test method | What it covers |
|-------------|---------------|
| `Mediator_DispatchesMembershipPackageCrudMessagesThroughModuleOwnedHandlers` | Package list/create/update/delete through `Modules.MembershipFinance.Application` handlers, with no shared membership workflow registered |
| Finance mediator tests | Remaining membership, payment, invoice, refund, and workspace handlers still dispatch through the transitional workflow-service adapters |

---

### `tests/WebApp.Tests/Unit/AdditionalControllerTests.cs`

**Subject:** Additional tenant controllers

Covers: `TrainingSessionsController`, `StaffController`, `EmploymentContractsController`, `VacationsController`, `WorkShiftsController`, `JobRolesController`, `GymSettingsController`, `GymUsersController`, `EquipmentController`, `EquipmentModelsController`, `OpeningHoursController`, `PaymentsController`.

---

### `tests/WebApp.Tests/Unit/MembershipWorkflowServiceTests.cs`

**Subject:** `MembershipWorkflowService` (BLL)

| Test pattern | Coverage |
|-------------|---------|
| Create membership with valid package | Happy path |
| Membership status transitions | Active → Paused → Expired |
| Payment lifecycle | Pending → Completed |
| Package association | Correct package linked |

---

### `tests/WebApp.Tests/Unit/MaintenanceWorkflowServiceTests.cs`

**Subject:** `MaintenanceWorkflowService` (BLL)

| Test pattern | Coverage |
|-------------|---------|
| Assigned caretaker status update | Caretaker can update a task assigned to their staff profile |
| Unassigned caretaker status update | 403 and no status mutation |
| Due scheduled-task generation | Creates one due scheduled task and prevents duplicate open scheduled tasks |
| Assignment update history | Appends assignment history with assignee, actor, and notes |
| Breakdown downtime/status transitions | In-progress moves equipment to maintenance; done ends downtime and restores active equipment |

---

### `tests/WebApp.Tests/Unit/SubscriptionTierLimitServiceTests.cs`

**Subject:** `SubscriptionTierLimitService` (BLL)

| Test pattern | Coverage |
|-------------|---------|
| Starter tier: member count limit | Enforced |
| Growth tier: session limit | Enforced |
| Enterprise tier: no hard limits | Verified |
| Staff count limits per tier | Enforced |

---

### `tests/WebApp.Tests/Unit/AppDbContextBehaviorTests.cs`

**Subject:** EF Core query filter behaviour (multi-tenancy)

| Test pattern | Coverage |
|-------------|---------|
| Query filter applies GymId | Entities from other gyms not returned |
| IgnoreGymFilter bypasses filter | System admin access works |
| Soft-delete filter | Deleted records not returned |

---

### `tests/WebApp.Tests/Unit/ApiContractMetadataTests.cs`

**Subject:** API controller attributes (contract stability)

| Test pattern | Coverage |
|-------------|---------|
| All controllers have `[ApiVersion("1.0")]` | Versioning |
| Route templates include `{gymCode}` for tenant controllers | Routing |
| Response type attributes match return types | Contract documentation |

---

### `tests/WebApp.Tests/Unit/RuntimeConfigurationTests.cs`

**Subject:** Application startup and configuration

| Test pattern | Coverage |
|-------------|---------|
| Startup succeeds with valid config | Smoke |
| Missing `Jwt:Key` throws on startup | Fail-fast |
| CORS configured | Origins present |
| Localization registered | Supported cultures |

---

### `tests/WebApp.Tests/Unit/LangStrTests.cs`

**Subject:** `LangStr` (Domain) — bilingual string helper

| Test pattern | Coverage |
|-------------|---------|
| `Translate("et-EE")` returns Estonian | Localisation |
| `Translate("en")` returns English fallback | Fallback |
| `Translate(null)` uses current culture | Default |

---

### `tests/WebApp.Tests/Unit/ClientDashboardPageServiceTests.cs`

**Subject:** MVC Client dashboard page-service mapping

| Test method | What it covers |
|-------------|---------------|
| `BuildAsync_ReturnsNullWhenActiveGymContextIsMissing` | Controller redirect input state; no BLL snapshot query without active gym |
| `BuildAsync_MapsDashboardSnapshotToCurrentCultureViewModel` | Upcoming sessions, bookings, assigned tasks, active gym/role, and culture-aware `LangStr` projection |

---

## 2. Backend — Integration Tests

Uses `CustomWebApplicationFactory` (in-memory SQLite, seeded).

### `tests/WebApp.Tests/Integration/MembershipPackageCrudTests.cs`

**Subject:** Membership package CRUD vertical slice

| Test pattern | Coverage |
|-------------|---------|
| List packages | Active-gym package list and response invariants |
| Create package | `201 Created` response |
| Invalid price/duration/currency | `400 application/problem+json` |
| Update package | `200 OK` and returned updated package |
| Delete unused package | `204` and soft-delete marker |
| Delete used package | `409 Conflict`, package retained, membership price/currency snapshot retained |
| Wrong-gym package ID | `404`, proving explicit `GymId` predicates |

---

### `tests/WebApp.Tests/Integration/AuthSecurityAndErrorTests.cs`

**Subject:** Security, IDOR, error formatting

| Test method | What it covers | IDOR? |
|-------------|---------------|-------|
| `RenewRefreshToken_RotatesToken_AndRejectsReuse` | Refresh token rotation; 403 on reuse | No |
| `RenewRefreshToken_RejectsExpiredRefreshToken` | 403 on expired token | No |
| `MembersEndpoint_RejectsActiveGymMismatch` | Login to peak-forge, access north-star → 403 | **Yes** |
| `MembersEndpoint_RejectsUnknownGym_Early` | Unknown gym slug → 404 ProblemDetails | No |
| `MembersEndpoint_RejectsInactiveGym_Early` | Inactive gym → 403 ProblemDetails | No |
| `Member_CannotReadAnotherMember` | Member reads other member's `{id}` → 403 | **Yes** |
| `SystemPlatformAnalytics_RejectsTenantOnlyUser` | Tenant user hits system endpoint → 403 | No |
| `SystemPlatformAnalytics_AllowsPlatformRoles` | `SystemAdmin`, `SystemSupport`, and `SystemBilling` can access platform analytics | No |
| `ApiErrors_ReturnProblemDetailsJson` | 404 returns `application/problem+json` | No |
| `TenantApi_UsesAcceptLanguageForLangStrResponses` | `Accept-Language: et-EE` → Estonian text | No |
| `SetCulture_StoresOnlySupportedCultureCookie` | Unsupported culture → default `et-EE` cookie | No |
| `HtmlErrors_RenderHtmlErrorPage` | Non-API error in prod → HTML page, not JSON | No |

---

### `tests/WebApp.Tests/Integration/TenantIsolationAndIdorTests.cs`

**Subject:** Tenant isolation and IDOR regression paths through the API host

| Test method | What it covers | IDOR? |
|-------------|---------------|-------|
| `Trainer_CannotAccess_DifferentGym_TrainingSessions` | Active gym cannot read another route gym's sessions | **Yes** |
| `Caretaker_CannotAccess_DifferentGym_MaintenanceTasks` | Active gym cannot read another route gym's maintenance tasks | **Yes** |
| `Member_CanAccess_OwnWorkspace` | Member self-access still succeeds after authorization boundary changes | No |
| `Member_CannotAccess_AnotherMembersWorkspace` | Member cannot read another member workspace | **Yes** |
| `Trainer_CannotUpdateAttendance_ForUnassignedSession` | Trainer assignment rule blocks unassigned sessions | **Yes** |
| `Caretaker_CannotUpdateStatus_ForUnassignedTask` | Caretaker assignment rule blocks unassigned tasks | **Yes** |
| `SystemSupportUser_CannotAccess_TenantMembersEndpoint` | System support role cannot enter tenant API without tenant context | **Yes** |
| `SystemBillingUser_CannotAccess_TenantTrainingSessionsEndpoint` | System billing role cannot enter tenant API without tenant context | **Yes** |
| `SystemUser_WithNoActiveGymContext_CannotAccessTenantEndpoint` | System role cannot bypass active tenant requirement | **Yes** |
| `Unauthenticated_Returns401_OnTenantEndpoint` | Anonymous tenant API calls require authentication | No |
| `GymAdmin_AtNorthStar_CannotCancel_PeakForgeBooking_ViaIdManipulation` | Tenant-scoped booking lookup blocks cross-gym ID manipulation | **Yes** |
| `GymAdmin_AtNorthStar_CannotUpdateStatus_PeakForgeMaintenanceTask_ViaIdManipulation` | Tenant-scoped maintenance lookup blocks cross-gym ID manipulation | **Yes** |

---

### `tests/WebApp.Tests/Integration/ImpersonationTests.cs`

**Subject:** Admin user impersonation

| Test method | What it covers |
|-------------|---------------|
| `StartImpersonation_WritesAuditRefreshTokenAndClaims` | Impersonation token contains impersonated user claims; audit record written |

---

### `tests/WebApp.Tests/Integration/StaffWorkflowTests.cs`

**Subject:** Staff entity lifecycle

| Test pattern | Coverage |
|-------------|---------|
| Create staff | 201, linked to person |
| Create employment contract | Contract linked to staff |
| Create vacation | Vacation linked to contract |
| Create work shift | Shift linked to session |

---

### `tests/WebApp.Tests/Integration/ProposalWorkflowTests.cs`

**Subject:** Coaching plan workflow

| Test pattern | Coverage |
|-------------|---------|
| Create coaching plan | 201 |
| Add items | Items attached |
| Status transitions | Draft → Active → Completed |
| Item decision | Approve / Reject |

---

### `tests/WebApp.Tests/Integration/SmokeTests.cs`

**Subject:** End-to-end happy-path rendering

| Test method | What it covers |
|-------------|---------------|
| `HomePage_ReturnsSuccess` | GET `/` → 200 |
| `AdminDashboard_UsesSharedLayoutAndSiteStyles` | `/Admin` → HTML with shared CSS |
| `AdminDashboard_QuickLinks_ExposeFunctionalSaasRoutes` | `/Admin` HTML contains `/client/members`, `/client/training-categories`, `/client/membership-packages`, `/client/sessions`, `/client/finance-workspace`, `/client/maintenance` |
| `SeededMvcPages_RenderWithSharedLayoutAndStyles` | Member, trainer, caretaker pages render |
| `SystemAdmin_GymsRoute_RendersMvcPage` | `/Admin/Gyms` renders styled MVC page |
| `TenantAdmin_WorkspaceRoutes_RenderMvcPages` | `/Admin/Memberships`, `/Admin/Sessions`, `/Admin/Operations` render styled MVC pages |

### `tests/WebApp.Tests/Integration/TrainingCategoryLocalizationTests.cs`

**Subject:** Training category and MVC resource localization

| Test pattern | Coverage |
|-------------|---------|
| Training category CRUD | API create/read/update/delete through localized `LangStr` fields |
| `Accept-Language` variants | `en`, `et`, and `et-EE` category translation behavior |
| Missing translations | Safe fallback to available text |
| MVC `.resx` labels | Login and authenticated Admin Members labels render from `SharedResources` in English and Estonian |
| Invalid category create | Validation returns `ProblemDetails` |

### `tests/WebApp.Tests/Integration/ClientDashboardTests.cs`

**Subject:** MVC Client dashboard rendering

| Test method | What it covers |
|-------------|---------------|
| `ClientDashboard_RendersSeededMvcDashboard` | Authenticated member can render `/mvc-client` with shared layout/styles and active gym context |

### `tests/WebApp.Tests/Integration/Final1CriticalE2ETests.cs`

**Subject:** Final1 critical API-level E2E paths through the ASP.NET Core test host

| Test method | What it covers |
|-------------|---------------|
| `Login_E2E_ReturnsTenantSession` | Login returns JWT, refresh token, active gym, tenant assignment, and role context |
| `MemberCrud_E2E_CreateReadUpdateDelete` | Member create, read, update, delete, and post-delete 404 |
| `TrainingCategoryCrud_E2E_CreateReadUpdateDelete` | Training category create, list/read, update, and delete |
| `MembershipPackageCrud_E2E_CreateReadUpdateDelete` | Membership package create, list/read, update, and delete |
| `IdorNegative_E2E_CrossTenantMemberUpdateReturns404` | North Star admin context cannot update a Peak Forge member by ID |

### `tests/WebApp.Tests/Integration/MvcComplianceTests.cs`

**Subject:** MVC Admin and MVC Client compliance

| Test method | What it covers |
|-------------|---------------|
| `AnonymousUser_CannotAccess_Admin` | anonymous Admin access redirects to login |
| `WrongRole_CannotAccess_Admin` | tenant non-admin users cannot land on Admin |
| `GymAdminOrGymOwner_CanAccess_TenantAdminPages` | `GymAdmin` and `GymOwner` get 200 HTML from tenant Admin pages |
| `MvcClientRoute_Works_ForTenantRoles` | member, trainer, and caretaker MVC Client routes render |
| `AdminViews_DoNotUse_ViewBagOrViewData` | Admin Razor views avoid dynamic data bags |
| `AdminViews_RenderOnlyStronglyTypedViewModels` | Admin Razor views declare `Admin*ViewModel` and avoid `dynamic`/`Html.Raw` |
| `AdminPostActions_UseAntiForgery` | future Admin POST actions must use anti-forgery |
| `AdminControllers_ReturnStronglyTypedViewModels` | Admin controllers return typed `View(...)` results and avoid React redirects/dynamic data bags |

---

### `tests/WebApp.Tests/Integration/AdminMembersCrudTests.cs`

**Subject:** MVC Admin member CRUD

| Test pattern | Coverage |
|-------------|---------|
| Index and forms | Admin can open index, create, edit, and delete views |
| Create/edit/delete | Valid posts persist updates or remove the entity from active listing |
| Validation | Invalid create shows MVC validation errors |
| Authorization | Member role cannot access Admin member CRUD |
| Tenant isolation | Cross-tenant member id edit is rejected with redirect, 403, or 404 |

### `tests/WebApp.Tests/Integration/AdminTrainingCategoriesCrudTests.cs`

**Subject:** MVC Admin training category CRUD

| Test pattern | Coverage |
|-------------|---------|
| Index and forms | Admin can open index, create, edit, and delete views |
| Create/edit/delete | Valid posts persist localized category changes or remove the entity from active listing |
| Validation | Invalid create shows MVC validation errors |
| Localization | Estonian request culture renders the seeded Estonian `LangStr` value |
| Authorization | Member role cannot access Admin category CRUD |
| Tenant isolation | Cross-tenant category id edit is rejected with redirect, 403, or 404 |

### `tests/WebApp.Tests/Integration/AdminMembershipPackagesCrudTests.cs`

**Subject:** MVC Admin membership package CRUD

| Test pattern | Coverage |
|-------------|---------|
| Index and forms | Admin can open index, create, edit, and delete views |
| Create/edit/delete | Valid posts persist active-gym package changes or remove the entity from active listing |
| Validation | Invalid create shows MVC validation errors |
| Authorization | Member role cannot access Admin package CRUD |
| Tenant isolation | Cross-tenant package id edit is rejected with redirect, 403, or 404 |

---

### `tests/WebApp.Tests/Architecture/ArchitectureTests.cs`

**Subject:** Clean/Onion and controller boundary rules

| Test method | What it covers |
|-------------|---------------|
| `MaintenanceSlice_UsesDedicatedRepositoryAndMapperBoundaries` | Maintenance service uses `IAppUnitOfWork`, `IMaintenanceRepository`, and `IMaintenanceMapper` instead of `IAppDbContext` |
| `TenantAccessChecker_UsesAuthorizationQueryRepositoryInsteadOfDbContext` | Tenant access checker constructor depends on `IAuthorizationQueryRepository` and not `IAppDbContext`/`DbContext` |
| `AdminMvcControllers_AreThinAndDoNotDependOnDbContext` | Admin MVC controllers do not inject `DbContext`/`IAppDbContext` |
| `ClientMvcDashboard_UsesPageAndBllContractsWithoutDirectEf` | Client Dashboard controller delegates to a page service, and the page/query service path avoids direct EF/DAL dependencies |

---

### `tests/WebApp.Tests/Architecture/ModuleArchitectureTests.cs`

**Subject:** Final2 module boundaries

| Test method | What it covers |
|-------------|---------------|
| `EveryModule_DoesNotReferenceAnyOtherModule` | Prevents direct module-to-module project references |
| `TrainingModule_DoesNotReferenceUsersOrGymManagementInternals` | Prevents Training from reaching into Users/GymManagement internals |
| `TrainingCategoryWorkflow_IsOwnedByTrainingModuleHandlers` | Ensures category CRUD handlers live in `Modules.Training.Application`, use UOW/auth/mapper contracts, and do not depend on `ITrainingWorkflowService` |
| `MembershipPackageWorkflow_IsOwnedByMembershipFinanceModuleHandlers` | Ensures package CRUD handlers live in `Modules.MembershipFinance.Application`, use UOW/auth/mapper contracts, and do not depend on `IMembershipWorkflowService` or `IMembershipPackageService` |
| `Mediator_IsResolvableFromCompositionRoot` | Composition root can resolve `IMediator` with all modules registered |

---

### `tests/WebApp.Tests/Integration/PostgreSqlPersistenceTests.cs`

**Subject:** PostgreSQL-specific behaviour (Testcontainers)

**Gate:** Only runs when environment variable `RUN_POSTGRES_TESTS=1` is set.
**CI:** Skipped in GitLab CI (no Docker-in-Docker on shared runners).

| Test pattern | Coverage |
|-------------|---------|
| Migration applies cleanly | Schema created |
| Seed data persists | Demo users and gyms readable |
| EF Core query filters work on Postgres | Same as in-memory |

---

## 3. React Tests (Vitest)

Location: `client/src/`

### `client/src/pages/CrudPages.test.tsx`

**Subject:** Members, Training Categories, Membership Packages CRUD pages

| Test | Component | Action tested |
|------|-----------|--------------|
| "creates a member and reloads the list" | `MembersPage` | POST → success toast → list refresh |
| "shows a load error for members when the API fails" | `MembersPage` | 403 → error message from `detail` |
| "updates a training category" | `TrainingCategoriesPage` | PUT → success toast → list refresh |
| "shows an API save error for training categories" | `TrainingCategoriesPage` | 400 → "Could not save category" + `detail` |
| "shows the loading state while membership packages are fetched" | `MembershipPackagesPage` | Loading and empty state |
| "creates a membership package and reloads the list" | `MembershipPackagesPage` | POST → success toast → list refresh |
| "updates a membership package" | `MembershipPackagesPage` | PUT → success toast → list refresh |
| "surfaces validation errors when membership package fields are invalid" | `MembershipPackagesPage` | Local validation with no API call |
| "deletes a membership package" | `MembershipPackagesPage` | DELETE → success toast → list refresh |
| "shows an API save error for membership packages" | `MembershipPackagesPage` | 400 → "Could not save package" + `detail` |

---

### `client/src/pages/WorkspacePages.test.tsx`

**Subject:** Member workspace, Finance workspace, Trainer coaching workspace

| Test pattern | Coverage |
|-------------|---------|
| Member workspace renders profile and membership data | GET and display |
| Finance workspace renders invoices | GET and display |
| Coaching workspace renders plans | GET and CRUD |

---

### `client/src/pages/SessionsPage.test.tsx`

**Subject:** `SessionsPage`

| Test pattern | Coverage |
|-------------|---------|
| Sessions list renders | GET and display |
| Book session action | POST booking |

---

### `client/src/pages/OperationsPages.test.tsx`

**Subject:** `MaintenanceTasksPage`

| Test pattern | Coverage |
|-------------|---------|
| Tasks list renders | GET and display |
| Status update action | PUT status |

---

### `client/src/App.test.tsx`

**Subject:** App routing and authentication flow

| Test pattern | Coverage |
|-------------|---------|
| Unauthenticated user redirected to `/login` | Auth guard |
| Role-based landing redirect | SystemAdmin → `/platform`, Member → `/member-workspace` |

---

## 4. IDOR coverage matrix

| Scenario | Unit test | Integration test |
|---------|-----------|-----------------|
| No active gym in JWT | `EnsureTenantAccessAsync_RejectsWhenActiveGymContextIsMissing` | — |
| Active gym ≠ route gymCode | `EnsureTenantAccessAsync_RejectsWhenRouteGymDiffersFromActiveGym` | `MembersEndpoint_RejectsActiveGymMismatch`; `TenantIsolationAndIdorTests.Trainer_CannotAccess_DifferentGym_TrainingSessions`; `TenantIsolationAndIdorTests.Caretaker_CannotAccess_DifferentGym_MaintenanceTasks` |
| Role not in allowed list | `EnsureTenantAccessAsync_RejectsWhenAllowedRoleIsMissing` | `SystemPlatformAnalytics_RejectsTenantOnlyUser` |
| Member reads own member | `EnsureMemberSelfAccessAsync_AllowsOwnMemberAndRejectsOthers` | `TenantIsolationAndIdorTests.Member_CanAccess_OwnWorkspace` |
| Member reads other member | `EnsureMemberSelfAccessAsync_AllowsOwnMemberAndRejectsOthers` | `Member_CannotReadAnotherMember`; `TenantIsolationAndIdorTests.Member_CannotAccess_AnotherMembersWorkspace` |
| Trainer accesses unassigned booking | `EnsureBookingAccessAsync_TrainerMustBeAssignedToSession` | `TenantIsolationAndIdorTests.Trainer_CannotUpdateAttendance_ForUnassignedSession` |
| Caretaker accesses unassigned task | `EnsureMaintenanceTaskAccessAsync_CaretakerMustBeAssigned` | `TenantIsolationAndIdorTests.Caretaker_CannotUpdateStatus_ForUnassignedTask` |
| Unknown gym slug | — | `MembersEndpoint_RejectsUnknownGym_Early` |
| Inactive gym | — | `MembersEndpoint_RejectsInactiveGym_Early` |

**Remaining note:** IDOR coverage is API-level and service-level. There is no Playwright browser test suite in this repository.
