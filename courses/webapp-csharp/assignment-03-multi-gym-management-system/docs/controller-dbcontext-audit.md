# Controller → DbContext Audit

**Audited:** 2026-05-09

This audit lists every controller that reaches into `AppDbContext` (or
`IAppDbContext`) directly, so we can size the future migration to BLL ports
without doing it now.

---

## 1. API controllers — clean

`grep -r "AppDbContext\|IAppDbContext\|DbContext" src/WebApp/ApiControllers/` → **no matches.**

All 27 REST controllers under `WebApp.ApiControllers.{Identity,System,Tenant}`
already depend on `App.BLL.Services.*` ports. The architecture test introduced
in Phase 9 enforces this boundary going forward.

---

## 2. MVC controllers — leaks

| Controller | dbContext call sites | DbSets touched | Notes |
|---|---:|---|---|
| `Controllers/HomeController` | 5 | `AppUserGymRoles`, `Gyms` | Login/role-switch. Should move to a BLL `IUserGymMembershipService`. |
| `Areas/Admin/Controllers/DashboardController` | 4 | platform/tenant snapshot reads | Read-model duplication of the Saas console; candidate for shared BLL service. |
| `Areas/Admin/Controllers/GymsController` | 1 | `Gyms` | One LINQ projection. Trivial migration target. |
| `Areas/Admin/Controllers/MembershipsController` | 2 | `Memberships`, `MembershipPackages` | Already partly served by `IMembershipService`. |
| `Areas/Admin/Controllers/OperationsController` | 3 | bookings/invoices joins | Workflow page; would re-use `IBookingPricingService` + `IPaymentService`. |
| `Areas/Admin/Controllers/SessionsController` | 1 | `TrainingSessions` | Tiny — could fold into `ITrainingWorkflowService`. |
| `Areas/Client/Controllers/MaintenanceController` | 1 | `MaintenanceTasks` | Use `IMaintenanceWorkflowService`. |
| `Areas/Client/Controllers/ProfileController` | 4 | identity profile reads/writes | Use `IIdentityService` + new profile read-model. |
| `Areas/Client/Controllers/SessionsController` | 5 | sessions + bookings | Largest leak — booking flow. |
| **Total** | **26** | | All MVC; no API controller leaks. |

`Areas/Admin/Controllers/MembersController` already routes through
`IMemberWorkflowService` and is the reference shape MVC controllers should
match.

`Areas/Client/Controllers/DashboardController` now routes through
`IClientDashboardPageService`. The page service builds the Razor view model
from BLL/application contracts, and `IClientDashboardQueryService` composes the
dashboard snapshot through `IAppUnitOfWork`.

---

## 3. Other DbContext consumers in WebApp

Out of scope for the "controller" rule but listed here for completeness:

| File | Type | Justification |
|---|---|---|
| `Middleware/GymResolutionMiddleware.cs` | middleware | Uses `IAppDbContext` (port). Acceptable — middleware is allowed to talk to ports. |
| `ViewComponents/WorkspaceSwitcherViewComponent.cs` | view component | Direct `AppDbContext`. Same rule as MVC controllers — should migrate. |
| `Setup/DatabaseExtensions.cs` | composition root | Allowed — it registers `AppDbContext` and `IAppDbContext`. |
| `Setup/IdentitySetupExtensions.cs` | composition root | Allowed — `AddEntityFrameworkStores<AppDbContext>()`. |
| `Setup/AppDataInitExtensions.cs` | composition root | Allowed — bootstraps migrations and seed data. |

---

## 4. Phase 9 architecture test scope

The new `ControllerHasNoDbContextDependency` test scans `typeof(Program).Assembly`
for any `ControllerBase`-derived type whose namespace starts with
`WebApp.ApiControllers`, and asserts that none of them declare a constructor
parameter assignable to `DbContext` or `IAppDbContext`.

This locks the **clean side** in CI today. A scoped Client Dashboard regression
guard also asserts that the MVC controller depends only on
`IClientDashboardPageService`, and that the page/query service path does not
reference direct EF/DAL types. Widening the scope to all MVC controllers happens
after the migration backlog above is worked through, in a later phase.

---

## 5. Migration order recommendation (out of scope here)

Suggested ordering when the MVC migration is taken on:

1. `Areas/Admin/GymsController` — 1 call, easiest.
2. `Areas/Admin/SessionsController` — 1 call, BLL service exists.
3. `Areas/Client/MaintenanceController` — 1 call, BLL service exists.
4. `Areas/Admin/MembershipsController` — 2 calls, BLL service exists.
5. `Areas/Admin/DashboardController` — 4 calls.
6. `Areas/Admin/OperationsController` — 3 calls (joins).
7. `Areas/Client/ProfileController` — 4 calls.
8. `Controllers/HomeController` — 5 calls, role switching is sensitive.
9. `Areas/Client/SessionsController` — 5 calls, biggest surface.
10. `WorkspaceSwitcherViewComponent` — same shape as `HomeController.SwitchGym`.
