# Assignment 03 SaaS Plan

## Current Delivery Shape

Assignment 03 is implemented as one SaaS backend plus two UI approaches:
- ASP.NET Core MVC admin UX
- ASP.NET Core MVC client UX under `/mvc-client`
- a separate React + TypeScript SaaS client in `client/`

The project keeps the ASP.NET Core monolith for backend responsibilities while adding the separate client required for REST API consumption. The production Docker image now builds that client separately and serves it from the backend at `/client`; the MVC client area uses `/mvc-client` to avoid route collision with the React bundle.
Admin handoff now routes `/Admin/Gyms`, `/Admin/Memberships`, `/Admin/Sessions`, and `/Admin/Operations` into the React `/client` workflows to avoid read-only duplication.

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
- member workspace aggregation (profile, memberships, payments, bookings, attendance, outstanding actions)
- coaching plans and coaching plan items
- membership packages, memberships, and payments with expanded membership lifecycle states
- finance workspace with invoices, invoice lines, payment history, refunds/credits, overdue and outstanding balances
- opening hours and exceptions
- equipment and maintenance tasks
- recurring maintenance task generation, assignment history, completion notes, and downtime tracking
- subscription-tier limits for starter/growth/enterprise creation workflows

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

Backend structure follows the Assignment 18 reference layout:
- domain entities are one public class per file
- DTOs are grouped by resource folder and namespace
- BLL service interfaces sit beside service implementations
- startup/setup concerns are split into focused WebApp extension files

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
- `Invoice`
- `InvoiceLine`
- `InvoicePayment`
- `OpeningHours`
- `OpeningHoursException`
- `EquipmentModel`
- `Equipment`
- `MaintenanceTask`
- `MaintenanceTaskAssignmentHistory`
- `CoachingPlan`
- `CoachingPlanItem`

## Multi-Tenant Rules

- `Gym` is the tenant root.
- Tenant-owned entities carry `GymId`.
- Tenant API routes use `/api/v1/{gymCode}/...`.
- `GymResolutionMiddleware` resolves route `gymCode` early in the API pipeline and rejects unknown/inactive gyms before controller/BLL execution.
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
- `/api/v1/{gymCode}/member-workspace/me`
- `/api/v1/{gymCode}/member-workspace/members/{memberId}`
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
- `/api/v1/{gymCode}/memberships/{id}/status`
- `/api/v1/{gymCode}/payments`
- `/api/v1/{gymCode}/coaching-plans`
- `/api/v1/{gymCode}/coaching-plans/{id}`
- `/api/v1/{gymCode}/coaching-plans/{id}/status`
- `/api/v1/{gymCode}/coaching-plans/{id}/items/{itemId}/decision`
- `/api/v1/{gymCode}/finance-workspace/me`
- `/api/v1/{gymCode}/finance-workspace/members/{memberId}`
- `/api/v1/{gymCode}/invoices`
- `/api/v1/{gymCode}/invoices/{id}`
- `/api/v1/{gymCode}/invoices/{id}/payments`
- `/api/v1/{gymCode}/invoices/{id}/refunds`
- `/api/v1/{gymCode}/opening-hours`
- `/api/v1/{gymCode}/opening-hours-exceptions`
- `/api/v1/{gymCode}/equipment-models`
- `/api/v1/{gymCode}/equipment`
- `/api/v1/{gymCode}/maintenance-tasks`
- `/api/v1/{gymCode}/maintenance-tasks/{id}/assignment`
- `/api/v1/{gymCode}/maintenance-tasks/{id}/assignment-history`
- `/api/v1/{gymCode}/maintenance-tasks/generate-due`
- `/api/v1/{gymCode}/gym-settings`
- `/api/v1/{gymCode}/gym-users`

## Separate Client Plan

The separate client now combines focused workflow pages with a broad SaaS function console.

Implemented v1 scope:
- login/logout through the backend API
- refresh-token based session continuation
- `sessionStorage` auth persistence
- language selection with `Accept-Language` for localized API responses
- translated React login/shell labels for English and Estonian
- system-role platform console for analytics, gym onboarding/activation/snapshots, subscriptions, support tickets, and impersonation
- SystemAdmin active-tenant picker in the React shell
- assigned multi-gym user active tenant and role picker in the React shell
- tenant owner/admin function console exposing staff, contracts, vacations, sessions, shifts, bookings, memberships, payments, facilities, equipment, maintenance, settings, and gym-user actions
- 3 admin CRUD areas:
  - members
  - training categories
  - membership packages
- sessions list/detail
- owner/admin training-session scheduling from existing categories
- member/admin booking with payment reference
- duplicate member-code, personal-code, and session-booking validation through API `ProblemDetails`
- trainer attendance updates
- attendance responses include member/session display names for client UIs
- maintenance task scheduling from equipment with optional staff assignment
- caretaker maintenance task status updates
- member workspace page for aggregated member context
- trainer coaching workspace for coaching-plan CRUD/status/item-decision workflows
- finance workspace page for invoices, payments, refunds, and outstanding balance
- maintenance workspace extensions for assignment updates/history and recurring due-task generation
- role-specific landing routes:
  - `Member` -> `/member-workspace`
  - `Trainer` -> `/coaching-workspace`
  - `Caretaker` -> `/maintenance`
  - owner/admin -> `/finance-workspace` and `/console`
- single active gym per session, with SystemAdmin able to select any active tenant

Not implemented in this pass:
- deployment of the client to a separate public server; it is instead deployed under the ASP.NET Core host at `/client`

## Security Rules

- MVC forms use anti-forgery.
- API requests require JWTs.
- Identity password policy requires minimum 8 chars with digit, uppercase, lowercase, and non-alphanumeric.
- Seed/demo users now use a strong default password (`GymStrong123!`).
- JWT bearer metadata requires HTTPS outside development.
- forwarded headers are processed before HTTPS/auth middleware for reverse-proxy deployments.
- production CORS startup fails fast without explicit safe origins and rejects localhost/wildcard origins.
- tenant route `gymCode` resolution fails early for unknown/inactive gyms before handler execution.
- public API controllers advertise `ProblemDetails` responses for `400`, `401`, `403`, `404`, and `409`.
- APIs return `ProblemDetails` for unhandled JSON/API failures.
- production HTML failures render `/Home/Error`.
- members can access only their own member data.
- tenant-only users cannot use system routes.
- platform-role access is separated from tenant-role access; SystemAdmin can intentionally enter an active tenant context as `GymOwner` for support/demo work.
- trainers can update only attendance for sessions assigned to them.
- caretakers can update only maintenance tasks assigned to them.
- subscription-tier limits are enforced in BLL before member/staff/session/equipment creation.

## Architecture Boundary Notes

- tenant members, training, memberships/payments, facilities, and client workspace reads are handled through BLL service interfaces
- coaching-plan, finance-workspace, and member-workspace workflows are service-first and controller-thin
- BLL services depend on `IAppDbContext` rather than the concrete EF `AppDbContext`
- membership workflow internals are split into focused package, membership, payment, and booking-pricing services behind `IMembershipWorkflowService`
- authorization internals are split into current-actor resolution, tenant-access checks, and resource-specific checks behind `IAuthorizationService`
- API controllers are thin boundary adapters and do not expose direct `AppDbContext` access through `ApiControllerBase`
- API controller actions accept request cancellation tokens and pass them through BLL services to EF async calls
- remaining direct `AppDbContext` usage is documented as pragmatic read composition in broad MVC/admin surfaces and application infrastructure

## Test Plan

Backend:
- unit tests for translation fallback and membership overlap logic
- unit tests for membership lifecycle status transitions and invalid transitions
- runtime-configuration tests for strict password policy, JWT HTTPS metadata behavior, and production CORS fail-fast rules
- EF behavior tests for tenant soft-delete filtering and audit-log writes
- authorization-service unit tests for active gym checks, role checks, member self access, trainer session assignment, and caretaker assignment
- controller unit tests for members, bookings, memberships, training sessions, maintenance tasks, staff, identity, platform, subscriptions, support, and impersonation, including response-shape and cancellation-token forwarding
- controller unit tests for member workspace, finance workspace, and coaching-plan routes
- subscription-tier limit unit tests for starter-limit rejection and enterprise allowance
- API-contract metadata unit tests for required `ProblemDetails` response documentation on public controllers
- integration tests for login, register, multi-gym user switch, SystemAdmin tenant-context switch, refresh-token rotation, expired/reused refresh tokens, cross-gym denial, member self-only denial, system-route denial, unknown/inactive gym early rejection, API `ProblemDetails`, MVC HTML error handling, `/client` fallback serving, MVC Admin/Client layout rendering, member roster denial, nullable session descriptions, member duplicate validation, booking payment-reference and duplicate-booking enforcement, trainer attendance authorization, caretaker task authorization, and impersonation actor/target/reason/claim/audit/refresh-token behavior
- a focused PostgreSQL Testcontainers slice validates provider-realistic behavior (tenant query filtering, unique constraints, `LangStr`/JSONB persistence); run when `RUN_POSTGRES_TESTS=1` and Docker is available

Frontend:
- auth guard tests
- logout cleanup tests
- system-role routing to the React SaaS console
- assigned multi-gym user shell tenant/role switching
- refresh-on-`401` tests
- selected language is sent through `Accept-Language`
- production/development API base default tests
- CRUD happy/error tests for:
  - members
  - training categories
  - membership packages
- sessions detail and booking test
- session scheduling test
- trainer attendance update test with member display names
- caretaker maintenance task status update test
- maintenance scheduling test
- trainer role landing route test (`/coaching-workspace`)
- finance workspace invoice-payment mutation test
- coaching workspace item-decision mutation test

Verification commands:
- `dotnet build multi-gym-management-system.slnx`
- `dotnet test multi-gym-management-system.slnx`
- `cd client && npm test`
- `cd client && npm run build`

## Delivery Notes

- Deployment artifacts stay in the repository and target the `cweb-a4` proxy route.
- The public URL is `https://mtiker-cweb-4.proxy.itcollege.ee`, which maps to VPS port `83`.
- This plan must stay aligned with README, API docs, testing docs, CI configuration, and AI logs whenever the implementation changes.
