# Architecture Overview

## Goal

Assignment 03 is implemented as a multi-tenant SaaS platform for gym operations.

The solution combines:
- one ASP.NET Core backend host for MVC, REST API, Swagger, and health checks
- a separate React + TypeScript admin client that consumes the REST API
- shared domain, data-access, and business layers under the same solution

## Runtime Surfaces

Backend host:
- MVC admin area
- MVC client area mounted at `/mvc-client`
- versioned REST API under `/api/v1/...`
- Swagger
- `/health`
- production React client mounted at `/client`

Separate client:
- `client/`
- Vite + React + TypeScript
- JWT + refresh-token flow
- focused workflows for members, scheduling, bookings, memberships, payments, maintenance, settings, and gym users
- focused admin CRUD for members, training categories, and membership packages
- session list/detail and booking through the REST API
- trainer attendance and caretaker maintenance task updates through the REST API
- language selector that sends `Accept-Language` to the API

## Layered Structure

Projects:
- `Base.Contracts`: reusable entity/audit/soft-delete contracts
- `Base.Domain`: reusable base domain primitives such as `BaseEntity` and `LangStr`
- `Base.Helpers`: reserved for reusable helper primitives; no migrated helpers currently exist
- `App.Domain`: gym domain entities, enums, role names, claim types, and app-specific domain abstractions
- `App.BLL.Contracts`: application service contracts and supporting application contract records used by WebApp and tests
- `App.DAL.Contracts`: transitional generic repository and Unit of Work
  contracts
- `App.DAL.EF`: legacy `AppDbContext`, mappings, migrations, tenant filters,
  seeding, repository implementations, EF Unit of Work, and shared transitional
  module-DbContext EF behavior
- `App.BLL`: empty compatibility project retained until final Phase 10 cleanup
- `App.DTO`: public API contracts
- `App.Resources`: root-level `.resx` localization resources
- `Modules.Users`, `Modules.Gyms`, `Modules.Memberships`, `Modules.Training`,
  `Modules.Maintenance`: Final2 module projects that progressively own their
  API/application/infrastructure slices. Phase 9 adds module-owned DbContext
  types under each module's `Infrastructure/Persistence` folder; shared
  `AppDbContext` runtime persistence remains transitional until the Phase 10
  App.* dependency cleanup.
- `Shared.Contracts`: cross-module contracts and projection APIs such as
  `IUsersModuleApi`, `IGymsModuleApi`, `IMembershipsModuleApi`, and
  `ITrainingModuleApi`
- `SharedKernel`: modular-monolith shared primitives and mediator foundation
- `WebApp`: root-level API controllers, MVC controllers, middleware, views, startup
- `WebApp.Tests`: unit and integration tests
- `client`: separate React client and frontend tests

Code organization mirrors the Assignment 18 backend style: each domain entity has its own file, DTOs are split by API resource namespace, service contracts live in root `App.BLL.Contracts` transitionally, concrete application services and mappers live in owning modules or WebApp presentation query folders, seed data is split through partial files, and `WebApp/Setup` separates database, identity, service registration, web API, middleware, and data initialization.

The MVC shell also mirrors the local LabRent/LabTrack reference project:
Admin and Client areas have their own Bootstrap-based layouts, sidebar
navigation, breadcrumbs, language/workspace controls, logout actions, and
TempData alerts. The gym-specific mapping and deliberate deviations from that
reference are tracked in
[reference-architecture-parity.md](reference-architecture-parity.md).

## Boundary Posture

The active architecture is Final2 transitional modular monolith. `WebApp`
composes modules and still owns MVC areas, host setup, Swagger, middleware, and
static client hosting. Users, Gyms, Memberships, Training, and Maintenance now
own their public API entry points, with Memberships, Training, and Maintenance
also owning EF repository implementations for the defended slices.

`App.BLL.Contracts`, `App.DAL.Contracts`, `App.DAL.EF`, and `App.Domain` remain
transitional shared contracts/persistence/domain projects. The concrete
`App.BLL` implementation dependency has been removed from WebApp/modules, but
the compatibility project remains until final cleanup. Phase 9 establishes
module DbContext ownership for Users, Gyms, Memberships, Training, and
Maintenance, but the legacy `AppDbContext` still owns active migrations,
seeding, Identity setup, and UOW-backed runtime persistence until Phase 10 can
remove the old dependency center without a project-reference cycle.
Cross-module staff assignment checks from Maintenance use `ITrainingModuleApi`
from `Shared.Contracts`, not a direct module reference.

## Request Flows

MVC flow:
1. request enters `WebApp`
2. cookie authentication resolves the signed-in user
3. MVC controller uses view models and server-side rendering
4. Admin routes use the standard area route; Client routes use `/mvc-client` to avoid colliding with the React `/client` mount
5. shared localization and anti-forgery protections apply

API flow:
1. request enters `WebApp`
2. JWT authentication resolves the caller
3. tenant access is checked against route gym code and claims
4. tenant workflow controllers delegate to module-owned or transitional BLL
   contract services
5. DTOs from `App.DTO.v1` are returned

Separate client flow:
1. React client logs in through `/api/v1/account/login`
2. auth state, including the refresh token, is stored in `sessionStorage`
3. `ApiClient` adds the bearer token and selected `Accept-Language` to API requests
4. a `401` triggers one refresh attempt through `/api/v1/account/renew-refresh-token`
5. refresh failure clears auth state and sends the user back to login

Auth application flow:
1. account-session endpoints delegate from `AccountController` to `IAccountAuthService`
2. refresh-token persistence goes through `IAppUnitOfWork.RefreshTokens`
3. `EfRefreshTokenRepository` performs EF-specific refresh-token lookup, add, and remove operations
4. `AuthResponseMapper` maps tokens, active tenant context, system roles, and tenant assignments into `JwtResponse`

Production client flow:
1. Docker builds `client/` with Node 20
2. `client/dist` is copied into `WebApp/wwwroot/client`
3. ASP.NET Core serves `/client` and `/client/{route}` from `client/index.html`
4. production API calls default to same-origin when `VITE_API_BASE_URL` is not set

Standalone client flow:
1. `client/Dockerfile` builds the same React client into an nginx image
2. production Compose includes the service behind the opt-in `client` profile
3. `VITE_API_BASE_URL` is baked into the standalone bundle at build time
4. backend CORS must include the standalone client origin
5. this flow is validated by client build and Compose config, but not by a
   live public smoke test in the 2026-05-11 readiness pass

## Multi-Tenancy Model

Tenant root:
- `Gym`

Isolation rules:
- every tenant-owned entity carries `GymId`
- tenant API routes use `/api/v1/{gymCode}/...`
- the route gym code must match the active gym in claims
- system APIs live under `/api/v1/system/...`

Implementation details:
- `SharedKernel.Persistence.IGymContext` exposes active gym data for WebApp,
  module DbContexts, and the legacy EF compatibility bridge
- `AppDbContext` applies query filters for `ITenantEntity`
- business entities use soft delete through `TenantBaseEntity`
- authorization helpers block cross-gym access and self-only violations

## Authentication and Authorization

Auth modes:
- ASP.NET Core Identity for user management
- JWT access tokens for the REST API
- refresh-token rotation for API sessions
- MVC cookie auth for the server-rendered UX

Runtime configuration:
- `Jwt:Key`, `Jwt:Issuer`, and `Jwt:Audience` are required; the app fails startup if any are missing
- local development should provide these through user secrets
- production should provide them through environment variables such as `JWT__Key`
- ASP.NET Core Data Protection keys are persisted in the database through `AppDbContext`

Platform roles:
- `SystemAdmin`

Tenant roles:
- `GymOwner`
- `GymAdmin`
- `Member`
- `Trainer`
- `Caretaker`

Important rules:
- members can access only their own member data
- trainers can update attendance only for assigned sessions
- caretakers can update only assigned maintenance tasks
- gym admins and owners can manage tenant-wide business data
- the React client admits `SystemAdmin`, `GymAdmin`, `GymOwner`, `Member`, `Trainer`, and `Caretaker` sessions

## Localization

UI localization:
- `.resx` files in `App.Resources`
- request localization for `et-EE` and `en`
- shared MVC culture switcher
- React language selector persisted in `localStorage`

DB localization:
- `LangStr` value object
- stored through EF value conversion
- used for multilingual business-owned text such as category and package names

## Error Handling

API/JSON requests:
- `ProblemDetailsMiddleware` maps unhandled exceptions to `application/problem+json`

HTML/MVC requests:
- production uses `UseExceptionHandler("/Home/Error")`
- `/Home/Error` renders a localized MVC error view

This keeps API failures machine-readable while still giving browser users a normal error page.

## Separate Client Design

Current frontend scope:
- single active gym per login session
- shell-level gym and role picker for assigned multi-gym users, plus SystemAdmin tenant picking
- switch-gym and switch-role actions remain available in the React shell
- CRUD coverage for:
  - members
  - training categories
  - membership packages
- session list/detail coverage
- member booking with payment reference support
- trainer attendance updates
- caretaker maintenance task status updates

Frontend structure:
- `client/app/lib/apiClient.ts`: HTTP client, refresh retry logic, DTO mapping
- `client/app/lib/auth.tsx`: auth context, route protection, session persistence
- `client/app/lib/language.tsx`: language state and API localization header source
- `client/app/pages/*`: CRUD pages and login
- `client/app/components/*`: shell and notice components

Boundary note:
- tenant members, training, membership, payment, and maintenance workflows now run through BLL services backed by repository contracts, Unit of Work, and BLL mappers
- account login, logout, and refresh-token renewal now run through `IAccountAuthService`, `IRefreshTokenRepository`, `IAppUnitOfWork`, and `AuthResponseMapper`
- `TenantAccessChecker` now uses `IAuthorizationQueryRepository` for route-gym lookup, keeping active-gym and role decisions in BLL while EF query details stay in DAL
- MVC controllers and view components delegate to page/query services and do not inject concrete `AppDbContext`; `Final1PresentationBoundaryTests` guards this boundary
- staff, resource authorization, and platform workflows use focused BLL services; remaining `IAppDbContext` usage is intentionally limited to BLL infrastructure and framework integration such as Identity/EF setup

## CORS

The backend exposes a named `ClientApp` policy.

Default allowed origins:
- `http://localhost:5173`
- `https://localhost:5173`
- `http://127.0.0.1:5173`

Production Compose sets the first allowed origin to `https://mtiker-cweb-a4.proxy.itcollege.ee` by default.
The list is configurable through `Cors:AllowedOrigins` or `Cors__AllowedOrigins__0`.

## Design Choices

Why keep the backend as one ASP.NET Core host:
- it preserves the course-aligned monolith for MVC, API, Swagger, and business logic
- it keeps deployment complexity lower while the public deploy remains pending

Why add a separate client instead of replacing MVC:
- the assignment requires both working MVC UX and a real API client
- keeping MVC plus React makes the domain easier to demo from multiple angles

Why migrate one vertical slice at a time instead of a larger rewrite:
- the course assignment needs a defendable layered monolith, not a framework-heavy architecture migration
- focused service splits preserve existing routes, DTOs, and business rules while moving concrete persistence outward
- repositories and Unit of Work are introduced per slice where they remove concrete persistence coupling without forcing a speculative rewrite of every query path at once

Why use a console for the broad SaaS surface:
- the dental clinic reference exposes platform and tenant operations from a client UI, so Assignment 03 now does the same without replacing the existing focused pages
- the console keeps every REST function reachable while the higher-use workflows still have dedicated pages
