# MVC Admin Audit

**Audited:** 2026-04-28

## Scope

This audit covers the ASP.NET Core MVC Admin area in `src/WebApp/Areas/Admin`.

Assignment 03 still keeps the React client as the main API-consuming workflow UI, but MVC Admin is now defendable as real Razor evidence. Tenant Admin routes no longer depend on React-only redirects.

## Route Coverage

| Route | Controller | Access | MVC evidence |
| --- | --- | --- | --- |
| `/Admin` and `/Admin/Dashboard` | `DashboardController` | system roles, `GymOwner`, `GymAdmin` | admin metrics with `AdminDashboardViewModel` |
| `/Admin/Members` | `MembersController` | `GymOwner`, `GymAdmin` | tenant member directory with `AdminMembersPageViewModel` |
| `/Admin/Memberships` | `MembershipsController` | `GymOwner`, `GymAdmin` | packages and active memberships with `AdminMembershipsPageViewModel` |
| `/Admin/Sessions` | `SessionsController` | `GymOwner`, `GymAdmin` | session capacity/bookings/trainers with `AdminSessionsPageViewModel` |
| `/Admin/Operations` | `OperationsController` | `GymOwner`, `GymAdmin` | opening hours, equipment, maintenance with `AdminOperationsPageViewModel` |
| `/Admin/Gyms` | `GymsController` | `SystemAdmin`, `SystemSupport`, `SystemBilling` | platform gym list with `AdminGymsPageViewModel` |

## Access Rules

- Anonymous users are challenged and redirected to `/login`.
- Tenant-only non-admin roles are not allowed to land on Admin pages.
- `GymAdmin` and `GymOwner` can access tenant Admin pages for their active gym.
- System roles can access the Admin dashboard. `SystemAdmin` can enter tenant context through the existing switch flow and then access tenant Admin pages as `GymOwner`.
- `/Admin/Gyms` is intentionally platform-role only.

## React Redirect Decision

Previous Admin routes redirected `/Admin/Gyms`, `/Admin/Memberships`, `/Admin/Sessions`, and `/Admin/Operations` into React `/client/*` pages. That was functionally useful, but weak as MVC Admin evidence.

Current decision:
- keep React as the write-heavy API client
- keep MVC Admin as read-only but functional Razor evidence
- do not replace MVC Admin with React-only redirects

## Tests

Covered by `MvcComplianceTests`:
- anonymous user cannot access Admin
- wrong role cannot access Admin
- `GymAdmin` and `GymOwner` can access tenant Admin pages
- Admin views do not use `ViewBag` or `ViewData`
- Admin POST actions require anti-forgery when present
- Admin controllers return strongly typed view models and do not redirect to React

Also covered by `AdminMembersPageTests`:
- `/Admin/Members` renders a styled Razor page
- member data comes from a strongly typed view model
- the members controller/view avoid `ViewBag` and `ViewData`

## Remaining Limitations

- MVC Admin pages are read-only summaries. Create/update/delete workflows remain in the REST API and React client to avoid duplicating mutation logic.
- `/Admin/Gyms` is platform-scope evidence, not tenant Admin evidence.
