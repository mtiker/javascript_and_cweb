# Final2 Architecture

This document describes the defended Assignment 05 Final2 architecture as of
2026-05-25. The implementation is a transitional modular monolith: one ASP.NET
Core deployment, explicit module projects, mediator-based cross-module
contracts, and shared EF runtime persistence that is still being separated.

## Official Scope

The Final2 assignment requires a personal project implemented in ASP.NET Core
as a modular monolith with at least three modules, mediator-based module
communication, REST API versioning, Swagger, JWT auth, MVC client/admin UX,
UI and DB localization, IDOR protection, CI/CD deployment, test coverage, and a
separate API-consuming client with JWT plus refresh-token flow.

The implemented domain is a multi-gym SaaS platform covering gym onboarding,
tenant administration, memberships, payments, training sessions, bookings,
staff, equipment, and maintenance.

## Runtime Shape

The backend remains a single ASP.NET Core host in `WebApp`:

- MVC Admin area under `/Admin`
- MVC Client area under `/mvc-client`
- versioned REST API under `/api/v1/...`
- Swagger under `/swagger`
- health check under `/health`
- production-served React bundle under `/client`

The separate client lives in `client/` and is deployable as its own nginx
container through the Compose `client` profile. In local development it calls
`https://localhost:7245`; in production it uses `VITE_API_BASE_URL` when built
as a separate deployment and same-origin when served from the backend image.

## Module Boundaries

Final2 module projects:

- `Modules.Users`
- `Modules.Gyms`
- `Modules.Memberships`
- `Modules.Training`
- `Modules.Maintenance`

Shared boundary projects:

- `Shared.Contracts`: public module APIs, cross-module DTO/projection records,
  and public REST DTOs.
- `SharedKernel`: mediator abstractions, shared module persistence primitives,
  and module-level infrastructure primitives.
- `App.Resources`: shared `.resx` localization resources.

Boundary rules enforced by `Architecture.Tests`:

- all expected module projects exist
- modules do not project-reference other modules
- modules only reference the allowed shared/transitional projects
- WebApp references all module projects
- WebApp/modules do not reference the concrete `App.BLL` implementation project
- module DbContexts live inside their owning module
- modules do not reference foreign module DbContext types
- shared module persistence infrastructure lives in `SharedKernel`

## Cross-Module Communication

Modules communicate through contracts in `Shared.Contracts` and mediator
messages in `Shared.Contracts/Mediator`.

Examples:

- Memberships validates booking/payment ownership through `ITrainingModuleApi`
  instead of referencing Training directly.
- Training validates member identity through `IMembershipsModuleApi` instead
  of importing Memberships repositories.
- Maintenance staff-assignment decisions go through Training module API
  projections.
- The architecture test `MediatorRegistrationTests` composes every module,
  resolves `IMediator`, publishes `ModulesReadyNotification`, and proves the
  Users handler receives the notification through the shared recorder.

## Persistence State

The current runtime still uses `App.DAL.EF/AppDbContext` for active migrations,
seeding, Identity, tenant filters, and most EF-backed runtime persistence.
Module-owned DbContexts already exist and are guarded:

- `UsersDbContext`
- `GymsDbContext`
- `MembershipsDbContext`
- `TrainingDbContext`
- `MaintenanceDbContext`

The module contexts define intended schema ownership and have save/read tests,
but the live migration cutover is intentionally deferred. This avoids a risky
schema split before the remaining entity-shaped contracts are replaced with
module-owned persistence contracts or module APIs.

## Security And Tenancy

Authentication and authorization:

- ASP.NET Core Identity for users
- JWT bearer auth for REST API calls
- refresh-token rotation with reuse rejection and logout invalidation
- MVC cookie auth for server-rendered pages
- SystemAdmin platform role
- tenant roles: GymOwner, GymAdmin, Member, Trainer, Caretaker

Tenant isolation:

- tenant-owned entities carry `GymId`
- tenant API routes include `{gymCode}`
- route gym code must match the active gym context in claims
- tenant query filters protect EF reads
- members can access only their own member record
- trainers and caretakers are scoped to assigned work

The tests cover active-gym mismatch, unknown/inactive gym rejection,
self-only member access, platform route denial for tenant users, and
cross-tenant ID manipulation.

## Localization

UI localization uses `App.Resources/SharedResources*.resx` with ASP.NET Core
request localization for `et-EE`, `et`, `en`, and `en-US`. MVC pages render
localized labels based on `Accept-Language` or the culture cookie.

DB localization uses `Base.Domain.LangStr`, EF conversion, and
`Accept-Language` based projection for business names such as training
categories and membership packages.

The React client stores the selected language in `localStorage` and sends it as
`Accept-Language` on API requests.

## Deployment And CI

Deployment assets:

- root `.gitlab-ci.yml` triggers the assignment child pipeline
- assignment-local `.gitlab-ci.yml` builds and tests backend/client, packages
  Docker images, exposes optional PostgreSQL tests, and deploys
- `Dockerfile` builds backend plus the served React client
- `client/Dockerfile` builds the standalone client image
- `docker-compose.prod.yml` includes backend, PostgreSQL, and optional
  standalone client profile
- `scripts/deploy.sh`, `scripts/deploy-client.sh`, and
  `scripts/smoke-deploy.sh` define the production path

Production CORS is fail-fast: outside Development the backend requires explicit
non-localhost origins and rejects wildcard/path/loopback origins.

## Remaining Architecture Risks

- Full removal of legacy `App.BLL.Contracts`, `App.DAL.EF`, and `App.Domain`
  references from modules remains incomplete because existing service and
  repository contracts still expose entity-shaped types.
- A local Docker production-stack smoke passed in Phase 14, but the public
  deployment URL must not be claimed as live until the public backend/client
  smoke passes; the latest public check still returned HTTP 404 for backend
  `/health`.
- Browser E2E tests are not present; current E2E evidence is HTTP-level
  integration coverage plus React Vitest interaction coverage.
