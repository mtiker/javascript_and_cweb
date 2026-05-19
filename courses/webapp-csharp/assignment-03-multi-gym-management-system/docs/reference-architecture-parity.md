# Reference Architecture Parity

This document records how the local reference project
`C:\Users\marti\VS_Code_Projects\satiks-cweb-personal1-main.zip` maps onto the
multi-gym management assignment.

The reference context is LabRent/LabTrack. The target context remains gym
management.

Current status:
- Final1 is the active completion target.
- The gym project matches the reference at the architectural level that matters
  for Final1: layered Domain/DTO/BLL/DAL/WebApp/Test separation, EF repositories
  and Unit of Work, MVC areas, REST API controllers, Identity/JWT,
  localization, seeding/migrations, Docker/deployment artifacts, and automated
  tests.
- Final2 module projects are preserved as partial evidence only. This document
  does not claim Final2 completion or full module isolation.

## Source Reference Shape

The reference project contains:
- domain entities and Identity types in `App.Domain`
- public API contracts in `App.DTO/v1`
- repository contracts and Unit of Work in `App.DAL.Contracts`
- EF Core persistence, repositories, migrations, and seeding in `App.DAL.EF`
- BLL service contracts in `App.BLL.Contracts`
- BLL services and DTO mappers in `App.BLL`
- MVC Admin, Technician, and User areas in `WebApp/Areas`
- versioned REST API controllers in `WebApp/ApiControllers`
- Bootstrap-based Razor shells with sidebars, breadcrumbs, language switching,
  logout, TempData alerts, and reusable list/filter/page-header partials
- Identity/JWT, Swagger, localization, Docker Compose, and test coverage

## Gym Mapping

| Reference part | Gym equivalent |
| --- | --- |
| LabRent tenant/domain context | Multi-gym SaaS tenant context, rooted at `Gym` |
| Laboratories, departments, locations | Gyms, gym settings, staff, members, equipment |
| Equipment bookings | Training sessions, bookings, attendance, maintenance |
| Training certifications | Training categories, trainer assignment, member booking rules |
| Admin area | `src/WebApp/Areas/Admin` |
| User area | `src/WebApp/Areas/Client` mounted under `/mvc-client` |
| Technician area | Caretaker maintenance flows in MVC Client and React maintenance pages |
| `App.DAL.Contracts/IAppUOW` | `App.BLL/Contracts/Persistence/IAppUnitOfWork` |
| EF repositories | `src/App.DAL.EF/Repositories/Ef*Repository.cs` |
| BLL services and mappers | `src/App.BLL/Services` and `src/App.BLL/Mapping` |
| API controllers | `src/WebApp/ApiControllers/Identity`, `System`, and `Tenant` |
| Bootstrap Admin shell | Added to gym Admin and Client MVC area layouts |
| MVC tests | Existing `WebApp.Tests` MVC compliance and rendering tests |

## Adopted From Reference

The MVC shell now follows the reference UI pattern:
- area-specific Razor layouts for Admin and Client
- dark sidebar navigation with route-active state
- Bootstrap and Bootstrap Icons in the MVC area shells
- breadcrumb trail based on current area/controller/action
- language switcher, workspace switcher, React-client shortcut, and logout in
  the area toolbar
- TempData success/error alerts in both area layouts
- responsive mobile shortcut navigation when the sidebar is hidden

The existing custom gym Razor page markup is preserved, with `site.css` updated
to make the current `panel`, `metric-card`, `data-table`, `primary`,
`secondary`, and `form-grid` classes fit the reference-style shell.

## Deliberate Differences

These differences are intentional:
- The gym project keeps the Final2 modular-monolith projects
  (`Modules.Users`, `Modules.GymManagement`, `Modules.Training`,
  `Modules.MembershipFinance`) as preserved partial Final2 evidence. They are
  intentionally not treated as complete module isolation for the Final1
  defense.
- The gym project keeps its separate React + TypeScript client because the
  official personal project requirements expect a separate API-consuming client
  with JWT and refresh-token flow.
- Persistence contracts remain under `App.BLL/Contracts/Persistence` rather
  than a separate `App.DAL.Contracts` project. This keeps EF implementation
  details outward while the BLL owns the contracts it depends on.
- The target remains multi-tenant gym management. Lab-specific terms such as
  laboratory, calibration, manufacturer, and training certification are not
  copied as domain concepts.
- Existing routes and DTOs are preserved to avoid breaking the React client,
  tests, Swagger contracts, and deployment docs.

## Remaining Parity Work

Potential follow-up work, if more exact parity is required:
- add reusable MVC page-header, filter, pagination, and table partials for the
  current gym views instead of keeping per-view table markup
- broaden MVC Admin CRUD beyond members, training categories, and membership
  packages in a future phase if the defense scope changes; this is not required
  for the current Final1 target
- continue moving module-owned workflows from shared BLL services into module
  handlers while preserving the public API, if Final2 is resumed later

Already completed for the current Final1 target:
- MVC `HomeController`, `WorkspaceSwitcherViewComponent`, Client
  `ProfileController`, Client `MaintenanceController`, and Admin
  package/category page services no longer reference concrete `AppDbContext`.
- `Final1PresentationBoundaryTests` guards MVC controllers, view components,
  and Admin/Client page services against concrete `AppDbContext` references.

## Validation

Latest Final1 validation after the presentation-boundary completion:
- `dotnet build multi-gym-management-system.slnx --no-restore` passed
- `dotnet test multi-gym-management-system.slnx --no-restore` passed with
  202 tests passed and 3 PostgreSQL/Testcontainers tests skipped
- `npm test` passed with 32 frontend tests
- `npm run build` passed for the React client
- Docker was unavailable in this environment, so PostgreSQL/Testcontainers tests
  and live public deployment smoke checks remain unverified.
