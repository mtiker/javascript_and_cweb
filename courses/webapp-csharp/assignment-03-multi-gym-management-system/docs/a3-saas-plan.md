# Assignment 03 SaaS Plan

## Current Delivery Shape

Assignment 03 is implemented as one SaaS backend plus two UI approaches:
- ASP.NET Core MVC admin UX
- ASP.NET Core MVC client UX under `/mvc-client`
- a separate React + TypeScript admin client in `client/`

The project keeps the ASP.NET Core monolith for backend responsibilities while adding the separate client required for REST API consumption. The production Docker image now builds that client separately and serves it from the backend at `/client`; the MVC client area uses `/mvc-client` to avoid route collision with the React bundle.

## Scope

Platform layer:
- gym onboarding
- gym activation
- subscriptions
- support tickets
- platform analytics
- impersonation
- audit logging

Tenant layer:
- members
- staff, job roles, contracts, vacations, and shifts
- training categories, sessions, and bookings
- membership packages, memberships, and payments
- opening hours and exceptions
- equipment and maintenance tasks

## Roles

Platform roles:
- `SystemAdmin`
- `SystemSupport`
- `SystemBilling`

Tenant roles:
- `GymOwner`
- `GymAdmin`
- `Member`
- `Trainer`
- `Caretaker`

## Architecture

Backend projects:
- `src/App.Domain`
- `src/App.DAL.EF`
- `src/App.BLL`
- `src/App.DTO`
- `src/App.Resources`
- `src/WebApp`
- `tests/WebApp.Tests`

Separate client:
- `client/`
- Vite
- React
- TypeScript
- Vitest
- production mount: `/client`

MVC client:
- ASP.NET Core area: `src/WebApp/Areas/Client`
- route prefix: `/mvc-client`

Technical stack:
- ASP.NET Core MVC + REST API
- ASP.NET Core Identity
- JWT + refresh-token rotation
- EF Core + PostgreSQL
- Swagger + URL-segment API versioning
- Docker + GitLab CI/CD

## Domain Model

Platform and SaaS entities:
- `Gym`
- `GymSettings`
- `Subscription`
- `SupportTicket`
- `AuditLog`
- `AppUser`
- `AppRole`
- `AppRefreshToken`
- `AppUserGymRole`

Shared person/contact entities:
- `Person`
- `Contact`
- `PersonContact`
- `GymContact`

Tenant entities:
- `Member`
- `Staff`
- `JobRole`
- `EmploymentContract`
- `Vacation`
- `TrainingCategory`
- `TrainingSession`
- `WorkShift`
- `Booking`
- `MembershipPackage`
- `Membership`
- `Payment`
- `OpeningHours`
- `OpeningHoursException`
- `EquipmentModel`
- `Equipment`
- `MaintenanceTask`

## Multi-Tenant Rules

- `Gym` is the tenant root.
- Tenant-owned entities carry `GymId`.
- Tenant API routes use `/api/v1/{gymCode}/...`.
- Active gym claims must match the route gym.
- No cross-gym reads or writes are allowed.
- Business entities use soft delete where the domain model expects it.

## Localization

UI localization:
- `.resx` resources in `App.Resources`
- request localization for `et-EE` and `en`

Database localization:
- `LangStr` stored through EF value conversion
- used for business-owned translated content

## Public Interface Plan

Identity endpoints:
- `/api/v1/account/register`
- `/api/v1/account/login`
- `/api/v1/account/logout`
- `/api/v1/account/renew-refresh-token`
- `/api/v1/account/switch-gym`
- `/api/v1/account/switch-role`
- `/api/v1/account/forgot-password`
- `/api/v1/account/reset-password`

System endpoints:
- `/api/v1/system/gyms`
- `/api/v1/system/gyms/{gymId}/activation`
- `/api/v1/system/gyms/{gymId}/snapshot`
- `/api/v1/system/subscriptions/{gymId}`
- `/api/v1/system/support/{gymId}/tickets`
- `/api/v1/system/platform/analytics`
- `/api/v1/system/impersonation`

Tenant endpoints:
- `/api/v1/{gymCode}/members`
- `/api/v1/{gymCode}/staff`
- `/api/v1/{gymCode}/job-roles`
- `/api/v1/{gymCode}/contracts`
- `/api/v1/{gymCode}/vacations`
- `/api/v1/{gymCode}/training-categories`
- `/api/v1/{gymCode}/training-sessions`
- `/api/v1/{gymCode}/work-shifts`
- `/api/v1/{gymCode}/bookings`
- `/api/v1/{gymCode}/membership-packages`
- `/api/v1/{gymCode}/memberships`
- `/api/v1/{gymCode}/payments`
- `/api/v1/{gymCode}/opening-hours`
- `/api/v1/{gymCode}/opening-hours-exceptions`
- `/api/v1/{gymCode}/equipment-models`
- `/api/v1/{gymCode}/equipment`
- `/api/v1/{gymCode}/maintenance-tasks`
- `/api/v1/{gymCode}/gym-settings`
- `/api/v1/{gymCode}/gym-users`

## Separate Client Plan

The separate client intentionally stays narrow in this phase.

Implemented v1 scope:
- login/logout through the backend API
- refresh-token based session continuation
- `sessionStorage` auth persistence
- 3 admin CRUD areas:
  - members
  - training categories
  - membership packages
- sessions list/detail
- member/admin booking with payment reference
- trainer attendance updates
- caretaker maintenance task status updates
- single active gym per session

Not implemented in this pass:
- multi-gym switching inside the React client
- deployment of the client to a separate public server; it is instead deployed under the ASP.NET Core host at `/client`

## Security Rules

- MVC forms use anti-forgery.
- API requests require JWTs.
- APIs return `ProblemDetails` for unhandled JSON/API failures.
- production HTML failures render `/Home/Error`.
- members can access only their own member data.
- tenant-only users cannot use system routes.
- platform-role access is separated from tenant-role access.
- trainers can update only attendance for sessions assigned to them.
- caretakers can update only maintenance tasks assigned to them.

## Architecture Boundary Notes

- tenant members, training, memberships/payments, facilities, and client workspace reads are handled through BLL service interfaces
- BLL services depend on `IAppDbContext` rather than the concrete EF `AppDbContext`
- remaining direct `AppDbContext` usage is documented as pragmatic read composition in broad MVC/admin surfaces, the staff API slice, and application infrastructure

## Test Plan

Backend:
- unit tests for translation fallback and membership overlap logic
- integration tests for login, register, gym switch, refresh-token rotation, expired/reused refresh tokens, cross-gym denial, member self-only denial, system-route denial, API `ProblemDetails`, MVC HTML error handling, `/client` fallback serving, MVC Admin/Client layout rendering, member roster denial, nullable session descriptions, booking payment-reference enforcement, trainer attendance authorization, and caretaker task authorization

Frontend:
- auth guard tests
- logout cleanup tests
- refresh-on-`401` tests
- production/development API base default tests
- CRUD happy/error tests for:
  - members
  - training categories
  - membership packages
- sessions detail and booking test
- trainer attendance update test
- caretaker maintenance task status update test

Verification commands:
- `dotnet build multi-gym-management-system.slnx`
- `dotnet test multi-gym-management-system.slnx`
- `cd client && npm test`
- `cd client && npm run build`

## Delivery Notes

- Deployment artifacts stay in the repository and target the `cweb-a4` proxy route.
- The public URL is `https://mtiker-cweb-4.proxy.itcollege.ee`, which maps to VPS port `83`.
- This plan must stay aligned with README, API docs, testing docs, CI configuration, and AI logs whenever the implementation changes.
