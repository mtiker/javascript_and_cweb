# MVC Client Audit

**Audited:** 2026-04-28

## Scope

This audit covers the ASP.NET Core MVC Client area in `src/WebApp/Areas/Client`.

The MVC Client area is served under `/mvc-client` so it does not collide with the production React client mounted at `/client`.

## Route Coverage

| Route | Role coverage | MVC evidence |
| --- | --- | --- |
| `/mvc-client` and `/mvc-client/Dashboard` | `Member`, `Trainer`, `Caretaker`, tenant admins | active gym dashboard with upcoming sessions, bookings, and assigned tasks |
| `/mvc-client/Profile` | `Member` primary, non-member tenant roles get a no-profile state | member profile, memberships, bookings, and payments |
| `/mvc-client/Sessions` | `Member`, `Trainer`, tenant admins | session list with opening-hours context |
| `/mvc-client/Sessions/Details/{id}` | `Member`, `Trainer`, tenant admins | session detail, booking state, and role-aware roster link |
| `/mvc-client/Sessions/Roster/{id}` | assigned `Trainer`, `GymOwner`, `GymAdmin` | roster and attendance update form |
| `/mvc-client/Maintenance` | `Caretaker`, tenant admins with staff context | assigned maintenance list |
| `/mvc-client/Maintenance/Details/{id}` | `Caretaker`, tenant admins with allowed task access | task details and status update form |

## Role Behavior

- Members can use the dashboard, profile, sessions list/detail, and booking/cancel forms.
- Trainers can access sessions and roster management only when assigned to the session.
- Caretakers can access maintenance pages for assigned tasks.
- Tenant admins can use MVC Client pages for demo/support views, while React remains the richer admin workflow UI.

## Form Security

MVC Client POST actions use `[ValidateAntiForgeryToken]`:
- `SessionsController.Book`
- `SessionsController.CancelBooking`
- `SessionsController.UpdateAttendance`
- `MaintenanceController.UpdateStatus`

## Dashboard Boundary

`Areas/Client/Controllers/DashboardController` delegates dashboard composition
to `IClientDashboardPageService`. The page service uses user context,
authorization, and `IClientDashboardQueryService`; the BLL query service reads
through `IAppUnitOfWork`. The controller has no direct `AppDbContext`
dependency.

## Tests

Covered by `MvcComplianceTests.MvcClientRoute_Works_ForTenantRoles`:
- member dashboard route works
- member profile route works
- trainer sessions route works
- caretaker maintenance route works

Additional dashboard-specific coverage:
- `ClientDashboardPageServiceTests` maps active-gym dashboard snapshots and the no-active-gym redirect state.
- `ClientDashboardTests.ClientDashboard_RendersSeededMvcDashboard` verifies seeded `/mvc-client` HTML rendering through the test host.
- `ArchitectureTests.ClientMvcDashboard_UsesPageAndBllContractsWithoutDirectEf` locks the no-direct-EF dashboard boundary.

Existing workflow integration tests cover related role boundaries:
- member booking payment-reference validation
- trainer attendance limited to assigned sessions
- caretaker status updates limited to assigned tasks

## Remaining Limitations

- The MVC Client is intentionally smaller than the React client. It is defendable Razor evidence and role-demo coverage, while the React client remains the main full-stack API-consuming application.
