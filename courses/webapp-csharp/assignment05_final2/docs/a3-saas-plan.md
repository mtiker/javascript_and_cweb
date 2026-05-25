# Assignment 03 SaaS Plan

Assignment 03 is the cumulative Web Applications with C# student project: a
multi-tenant SaaS platform for gym management.

Official requirement sources:
- Final1: https://courses.taltech.akaver.com/web-applications-with-csharp/assignments/home-assignments/Personal%20Project%20Final1
- Final2: https://courses.taltech.akaver.com/web-applications-with-csharp/assignments/home-assignments/Personal%20Project%20Final2

## Product Scope

The product serves a platform operator and multiple gyms.

Platform scope:
- gym onboarding and activation
- platform analytics

Tenant scope:
- members
- staff profiles
- training categories, sessions, bookings, and attendance
- membership packages, memberships, payments, and balances
- equipment and maintenance tasks
- member, trainer, caretaker, and admin workspaces

Removed from the Final2 defended scope:
- subscriptions, support tickets, impersonation, and audit-log UI/API flows
- coaching plans
- invoices and refunds
- job roles, contracts, vacations, and work shifts
- opening hours and exceptions

## Runtime Surfaces

The project has three user-facing surfaces:
- MVC Admin under `/Admin`
- MVC Client under `/mvc-client`
- React + TypeScript API client under `client/`

The ASP.NET Core backend serves:
- MVC Admin and Client
- REST API under `/api/v1/...`
- Swagger in development
- `/health`
- built React client at `/client` in the default production image

The React client can also run as a standalone nginx container through the
production Compose `client` profile.

## Architecture Baseline

Projects:
- `Base.Contracts`
- `Base.Domain`
- `Base.Helpers`
- `App.Domain`
- `App.DTO`
- `App.BLL.Contracts`
- `App.BLL`
- `App.DAL.Contracts`
- `App.DAL.EF`
- `App.Resources`
- `WebApp`
- `WebApp.Tests`
- `client`

Current posture:
- Final1 Clean/Onion-style layering is implemented with root-level projects and
  stable WebApp, BLL, DAL, DTO, domain, resource, and base boundaries.
- The earlier module architecture was removed from active code. See
  [module-boundaries.md](module-boundaries.md) for the retained status note.
- The public API and client routes are intentionally reduced to the defended
  product while internals follow Final1 layer boundaries.
- The MVC Admin and MVC Client shells are aligned with the local LabRent
  reference project while preserving the gym domain, routes, DTOs, and React
  client. The mapping is documented in
  [reference-architecture-parity.md](reference-architecture-parity.md).

## Core Invariants

Multi-tenancy:
- `Gym` is the tenant root.
- Tenant-owned business rows carry `GymId`.
- Tenant API routes include `{gymCode}`.
- Route gym, active gym claims, role checks, resource ownership checks, and EF
  tenant filters all participate in isolation.

Security:
- platform roles and tenant roles are separate
- JWT access tokens back the REST API
- refresh tokens rotate and reject reuse
- MVC uses cookie auth and anti-forgery for POST actions
- API errors use `ProblemDetails`

Documentation:
- keep this plan aligned with README, architecture, deployment, testing, and
  defense docs whenever scope changes
- do not keep old one-off phase audits as living docs

## Development Priorities

Final1 hardening:
1. remove remaining direct `AppDbContext` usage from MVC Client, Home, workspace
   switcher, and remaining Admin page services
2. run PostgreSQL/Testcontainers tests with Docker available
3. run and record public deployment smoke checks
4. add one more MVC Admin mutation workflow if the defense needs a broader
   Admin UX story

Future architecture work:
1. continue reducing direct `IAppDbContext` use from BLL infrastructure where
   repository contracts are already available
2. keep controller orchestration thin and service-driven
3. add boundary tests when a new dependency rule is introduced

## Evidence To Keep Current

- [architecture.md](architecture.md)
- [reference-architecture-parity.md](reference-architecture-parity.md)
- [data-model.md](data-model.md)
- [api.md](api.md)
- [domain-workflows.md](domain-workflows.md)
- [security-and-access.md](security-and-access.md)
- [testing.md](testing.md)
- [deployment.md](deployment.md)
- [final1-defense.md](final1-defense.md)
- [final2-defense.md](final2-defense.md)

## Current Limitations

- Public deployment availability must be smoke-tested before claiming a live
  defense environment.
- Standalone public client hosting has build and Compose evidence, but needs a
  real deployed `/healthz`, deep-link, login, and CORS smoke check.
- PostgreSQL provider tests are opt-in and require Docker.
- React refresh tokens are currently JavaScript-readable in `sessionStorage`;
  this is documented as a phase tradeoff.
- Module boundaries are partial. Some handlers still delegate to shared BLL
  services and all modules share one `AppDbContext`.
- Payments are internal records only; no external payment provider is
  integrated.
