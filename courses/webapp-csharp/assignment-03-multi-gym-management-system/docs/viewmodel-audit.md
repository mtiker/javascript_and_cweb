# ViewModel Audit

**Audited:** 2026-04-28

## Decision

MVC views use explicit view models at the boundary. Controllers may compose read models from EF/BLL services, but Razor views should not receive domain entities directly and should not depend on `ViewBag` or `ViewData`.

## Admin View Models

| MVC page | View model | Status |
| --- | --- | --- |
| `/Admin/Dashboard` | `AdminDashboardViewModel` | implemented |
| `/Admin/Gyms` | `AdminGymsPageViewModel` | implemented in this pass |
| `/Admin/Members` | `AdminMembersPageViewModel` | implemented |
| `/Admin/Memberships` | `AdminMembershipsPageViewModel` | implemented |
| `/Admin/Sessions` | `AdminSessionsPageViewModel` | implemented |
| `/Admin/Operations` | `AdminOperationsPageViewModel` | implemented |

The previous `/Admin/Gyms` view accepted `IReadOnlyCollection<Gym>`. That has been replaced with `AdminGymsPageViewModel` and `AdminGymSummaryViewModel` so the Admin area consistently uses view models.

## Client View Models

| MVC page | View model | Status |
| --- | --- | --- |
| `/mvc-client/Dashboard` | `ClientDashboardViewModel` | implemented |
| `/mvc-client/Profile` | `ClientProfilePageViewModel` | implemented |
| `/mvc-client/Sessions` | `SessionsPageViewModel` | implemented |
| `/mvc-client/Sessions/Details/{id}` | `SessionDetailPageViewModel` | implemented |
| `/mvc-client/Sessions/Roster/{id}` | `TrainerRosterPageViewModel` | implemented |
| `/mvc-client/Maintenance` | `MaintenancePageViewModel` | implemented |
| `/mvc-client/Maintenance/Details/{id}` | `MaintenanceTaskDetailPageViewModel` | implemented |

## Tests

`MvcComplianceTests.AdminControllers_ReturnStronglyTypedViewModels` verifies that Admin controllers:
- do not use `ViewBag`
- do not use `ViewData`
- do not redirect to React client routes
- return Admin-specific view models

`MvcComplianceTests.AdminViews_DoNotUse_ViewBagOrViewData` verifies all Admin Razor views avoid dynamic data bags.

## Remaining Limitations

- Some MVC controllers still use direct `AppDbContext` read composition for dashboard/summary pages. This is documented as pragmatic read composition and does not leak domain entities into views.
- Mutation workflows remain in BLL-backed REST APIs and React pages rather than duplicated in MVC forms.
