# Functionality Differences: Assignment 03 vs Assignment 18

Date: 2026-04-22

This report compares the full application scope, safety posture, and defense-readiness of:

- `courses/webapp-csharp/assignment-03-multi-gym-management-system`
- `courses/webapp-csharp/assignment-18-dental-clinic-platform`

It complements `backend-differences-assignment-03-vs-18.md`, which focuses on controller-layer differences. This report is broader: domain workflows, SaaS operations, tenant safety, API/client completeness, tests, deployment, and documentation.

Course lens used for this review:

- Web Applications with C# requires architecture progression from N-tier MVC into Clean/Onion Architecture, REST API + client app, Docker/CI/CD, and later modular/microservice phases when taught.
- The course states that a real client application consuming the backend REST API is mandatory; Swagger, Bruno, and Postman do not count as the client app.
- Testing is cross-cutting from the start and should be maintained across later assignments.
- Defense readiness depends on a compiling, runnable, deployed application with explainable architecture, security/authentication, data isolation, tests, and documentation.

Sources reviewed:

- official course syllabus: `https://courses.taltech.akaver.com/web-applications-with-csharp/`
- official first-project assignment page: `https://courses.taltech.akaver.com/web-applications-with-csharp/assignments/home-assignments/first-project`
- both assignment READMEs
- both assignment `docs/api.md`, `docs/data-model.md`, and `docs/testing.md`
- A03 `docs/a3-saas-plan.md`
- A18 `docs/architecture.md`
- controller, service, middleware, identity, and test inventories in both codebases

## Executive Summary

Assignment 03 is not a weak copy of Assignment 18. In several architecture areas it is already the better long-term base:

- A03 has a more consistent service-first API boundary. Tenant controllers mostly delegate to BLL workflow services instead of directly using `AppDbContext`.
- A03 has more user-facing surfaces: MVC admin, MVC client, and a separate React + TypeScript client served in production under `/client`.
- A03 has a gym-domain model with platform, tenant, staff, scheduling, membership, payment, equipment, maintenance, audit, soft-delete, localization, and multi-gym role-switching concepts already in place.
- A03 has current controller unit coverage for representative tenant controllers and passes cancellation tokens through those controller actions.

Assignment 18 is still more mature in several areas A03 should learn from:

- A18 has deeper workflow examples: treatment plans, plan-item decisions, appointment clinical records, finance workspace, invoices, payment plans, insurance policies, and resource catalogs.
- A18 has stronger API documentation discipline, with more `[ProducesResponseType]` metadata and more explicit role attributes.
- A18 has broader backend test evidence: 61 `[Fact]` tests compared with 34 in A03 at the time of this review.
- A18 has stricter password policy settings and JWT bearer HTTPS metadata enabled.
- A18 has a dedicated tenant-resolution middleware before controller execution.
- A18 has richer study and defense documentation, including BLL, DAL, DTO, domain, and Docker/deploy study guides.

The right target is not "make A03 identical to A18." The right target is:

1. Keep A03's gym SaaS domain and service-first backend separation.
2. Adopt A18's stronger workflow depth, security defaults, API metadata, test breadth, and defense evidence.
3. Translate dental concepts into gym concepts only when they improve A03's actual gym product.

## Inventory Snapshot

| Area | Assignment 03 | Assignment 18 | Meaning for A03 |
| --- | --- | --- | --- |
| Domain | Multi-gym SaaS: members, staff, sessions, bookings, memberships, payments, equipment, maintenance | Dental SaaS: patients, appointments, treatment plans, finance, insurance, resources | A03 has a valid domain; it needs deeper end-to-end workflows, not a domain replacement. |
| UI surfaces | MVC admin, MVC client, separate React client | Static `wwwroot` demo UI under `/app/*` | A03 has stronger client-app evidence, but must keep all surfaces synchronized. |
| API controllers | Service-first tenant API plus system/identity APIs | Hybrid service-backed and direct-DbContext APIs | Preserve A03's BLL boundary; copy A18 API polish, not direct persistence. |
| `[ProducesResponseType]` metadata | 92 occurrences | 147 occurrences | A03 has improved, but A18 still documents API outcomes more thoroughly. |
| Controller cancellation tokens | 92 occurrences | 89 occurrences | A03 has caught up on controller action cancellation plumbing. |
| Backend tests | 34 `[Fact]` tests | 61 `[Fact]` tests | A03 needs broader service/controller/integration coverage before claiming parity. |
| Frontend tests | React Vitest coverage exists | No comparable separate frontend test project | A03 is stronger here because it has a real separate client test suite. |
| Password policy | 6+ chars, digit required, uppercase/non-alphanumeric not required | 8+ chars, digit, upper, lower, non-alphanumeric required | A03 should tighten Identity password policy unless course/demo constraints prevent it. |
| Tenant resolution | Active gym checks through claims/BLL and EF gym context | Dedicated tenant middleware plus EF tenant provider and route matching | A03 should evaluate middleware-level route tenant resolution for earlier rejection and clearer defense story. |
| Defense docs | Architecture/API/data/testing/deployment/A3 SaaS plan | Architecture/API/data/testing plus domain/DAL/BLL/DTO/Docker study guides | A03 should add study-guide style docs for the parts a TA will question. |

## Functional Difference Map

### 1. Domain and Workflow Completeness

A03 currently covers the main gym SaaS shape:

- platform onboarding, activation, subscriptions, support, analytics, impersonation, audit logging
- tenant members and staff
- job roles, contracts, vacations, work shifts
- training categories, training sessions, bookings, and attendance
- membership packages, memberships, and payments
- opening hours, exceptions, equipment, and maintenance tasks

A18 goes deeper in workflow orchestration:

- treatment plan draft/update/submit/delete
- plan-item decisions and open-item tracking
- appointment clinical records connected to treatment items
- patient finance workspace
- invoice generation from procedures
- payment plans and installment schedules
- insurance plan and patient policy workflows

Gym-domain translation for A03:

- Add a training-plan or coaching-plan workflow that mirrors A18 treatment plans: draft, update, publish/approve, active items, member/trainer decisions, and progress tracking.
- Expand finance beyond simple payment records: invoices, invoice lines, outstanding balance, refunds/credits, overdue state, and payment schedule where relevant.
- Add membership lifecycle states with defensible transitions: pending, active, paused, expired, cancelled, refunded, and renewed.
- Add maintenance evidence: generated due tasks, completion notes, downtime, staff assignment history, and recurring maintenance schedules.
- Add subscription-tier enforcement for gym features and limits instead of documenting subscriptions as mostly informational.

Do not copy dental entities directly. A gym platform does not need teeth, treatment plans, x-rays, or insurance unless the gym domain explicitly grows into health/medical services. The useful pattern is "multi-step, audited, role-controlled workflow," not the dental vocabulary.

### 2. SaaS Platform Operations

A03 and A18 both have platform-level concepts:

- onboarding
- tenant activation
- subscriptions
- support views/tickets
- analytics
- impersonation
- audit logging

A18 is stronger as a reference for system backoffice shape because its system API is split into clearer operational areas: onboarding, platform, support, billing, and impersonation. A03 has comparable concepts, but the reportable maturity target should be:

- system dashboards that show tenant health, billing/subscription status, support status, and recent audit activity
- explicit subscription feature limits enforced in BLL and covered by tests
- support snapshots that are useful for debugging tenant state without exposing sensitive data unnecessarily
- billing operations that are more than a subscription update endpoint
- clear audit entries for platform actions such as activation changes, impersonation starts, subscription updates, and support-ticket creation

Recommended A03 target:

- Keep `IPlatformService` as the orchestration boundary unless it becomes too broad.
- Split platform services only when there is real complexity: onboarding, billing/subscriptions, support, impersonation, analytics.
- Add test coverage for each platform role: `SystemAdmin`, `SystemSupport`, and `SystemBilling`.

### 3. Tenant Isolation and Security

Both projects implement shared-schema multi-tenancy:

- A03 tenant rows use `GymId`.
- A18 tenant rows use `CompanyId`.
- Both apply EF query filters.
- Both apply soft delete to tenant business entities.
- Both create audit logs during `SaveChangesAsync`.

A03 strengths:

- Active gym and route gym checks are tied to BLL authorization and role/self/assignment rules.
- Member self-access, trainer attendance assignment, caretaker task assignment, wrong active gym, and system-route denial are already covered by integration tests.
- The separate React client uses JWTs and refresh-token rotation.

A18 strengths:

- `TenantResolutionMiddleware` resolves the route tenant before controller work.
- Tenant provider and route tenant mismatch behavior are easy to explain from the request pipeline.
- A18 has dedicated `UnitTestTenantAccessService`.
- A18 documents tenant query filtering, soft delete, and audit behavior in a more study-guide oriented way.

Recommended A03 target:

- Keep BLL authorization as the final enforcement point.
- Evaluate a dedicated `GymResolutionMiddleware` or equivalent request-pipeline tenant resolver so wrong/unknown `gymCode` requests fail earlier and consistently.
- Add a tenant-access service test suite comparable to A18's `UnitTestTenantAccessService`.
- Add explicit tests for audit and soft-delete behavior, not only endpoint denial.
- Ensure platform users entering tenant context do so through an intentional switch or impersonation flow, never implicitly.

### 4. Auth, Roles, and Impersonation

A03 supports:

- Identity registration/login/logout
- refresh-token rotation
- forgot/reset password demo flow
- switch-gym and switch-role
- system roles and tenant roles
- impersonation through platform service

A18 supports similar identity flows and has stronger security defaults in setup:

- password required length 8
- digit, uppercase, lowercase, and non-alphanumeric required
- JWT bearer `RequireHttpsMetadata = true`
- impersonation command/result contracts in BLL
- impersonation response includes refresh token and audit-friendly metadata

Recommended A03 target:

- Tighten password policy to A18's level or document why the demo/course seed policy is weaker.
- Require HTTPS metadata for JWT bearer in non-development environments.
- Move impersonation into a dedicated command/result boundary if `PlatformService` continues to grow.
- Add impersonation tests that prove actor, target, reason, tenant context, claims, refresh token behavior, and audit log entry.
- Review seed-user password reset behavior for production demos; A18 has an explicit production reset option, while A03 should document its intended behavior.

### 5. API Maturity and Error Behavior

The existing controller comparison report already covers API-controller details. The functionality-level conclusion is:

- A03 has caught up on cancellation-token usage in API controllers.
- A03 still has less Swagger/response metadata than A18.
- A03 has some current REST response gaps: several create/delete workflow endpoints still return `Ok(...)` or message payloads where `Created` or `NoContent` may be cleaner.
- A18 is more consistent in documenting HTTP status outcomes and route-level authorization intent.

Recommended A03 target:

- Finish `[ProducesResponseType]` coverage for all public API actions.
- Keep `ProblemDetails` as the standard error contract.
- Make response semantics consistent by workflow slice, not in a single risky broad edit.
- Preserve existing public routes unless the React client, MVC consumers, tests, Swagger, and docs are updated in the same change.
- Continue adding controller unit tests for route values, result shapes, cancellation-token forwarding, and DTO-to-service mapping.

### 6. UI and Client Completeness

A03 is stronger than A18 in client-app architecture:

- MVC admin UX
- MVC client UX under `/mvc-client`
- separate React + TypeScript client under `client/`
- production serving of the built React client under `/client`
- frontend Vitest coverage

A18 has a simpler static UI under `/app/*`, but it demonstrates deeper workflow screens in the dental domain:

- finance workspace
- treatment plan draft and submit flow
- plan-item decision updates
- appointment clinical record entry
- treatment type catalog reuse

Recommended A03 target:

- Keep the separate React client as the primary REST client evidence for defense.
- Make the React client workflows richer instead of only adding endpoint console coverage.
- Add gym equivalents of A18's workspace-style screens:
  - member profile/workspace with memberships, payments, bookings, attendance, and notes
  - trainer workspace with assigned sessions, attendance, member plan progress, and upcoming tasks
  - owner/admin finance workspace with invoices, overdue payments, membership revenue, and refunds
  - maintenance workspace with due tasks, assignments, completion evidence, and equipment downtime
- Keep MVC areas stable for defense smoke coverage, but avoid building new complex features twice unless the course specifically expects both.

### 7. Tests and Verification Evidence

A03 current evidence:

- backend unit tests for `LangStr`, membership overlap, runtime configuration, and representative tenant controllers
- integration tests for auth/security/error handling, route preservation, staff workflows, proposal workflows, and smoke paths
- React Vitest coverage for auth, API client refresh, routing, CRUD, sessions, attendance, maintenance, and package operations

A18 current evidence:

- broader backend unit tests for tenant access, patients, appointments, treatment plans, finance, identity seeding, and tenant API controllers
- integration tests for identity, onboarding, tenant operations, impersonation, and deployment health
- documented 61 `[Fact]` tests

Recommended A03 target:

- Add dedicated unit tests for authorization/tenant access service behavior.
- Add service tests for training/session conflict rules, booking rules, maintenance task generation, staff scheduling, payments, and subscription limits.
- Add controller tests beyond members/bookings/memberships: training sessions, maintenance tasks, staff/contracts/vacations, platform endpoints, account, and impersonation.
- Add audit/soft-delete tests.
- Add E2E/browser smoke tests only if they are stable enough to defend; otherwise document manual smoke flows clearly.
- Keep frontend tests aligned with every meaningful new React workflow.

### 8. Deployment, CI, and Operability

Both assignments have Docker, production compose files, health checks, deployment scripts, and child GitLab pipeline integration.

A03 strengths:

- production Dockerfile builds the React client and serves it through the backend
- assignment pipeline verifies React install/test/build before backend build/test/package/deploy
- README documents local run, test, Docker, CI/CD, public URL, and production secrets

A18 strengths:

- stronger Docker/deploy study guide documentation
- forwarded headers are explicitly used in middleware
- production seed password reset behavior is documented
- CORS setup rejects missing production allowed origins outside development
- JWT bearer HTTPS metadata is enabled

Recommended A03 target:

- Add forwarded-header handling if the VPS/proxy setup requires it.
- Fail fast when production CORS origins are missing or unsafe.
- Add a deployment study guide similar to A18's Docker/deploy guide.
- Document production seed/demo account behavior explicitly.
- Add a production smoke checklist that includes `/health`, `/client`, login, token refresh, one platform route, one tenant route, and one write operation.

### 9. Documentation and Defense Readiness

A03 has the core documentation:

- README
- architecture
- data model and ERD
- API overview
- testing guide
- deployment guide
- A3 SaaS plan
- AI usage

A18 has additional study/defense guides:

- domain guide
- DAL/EF guide
- BLL guide
- DTO guide
- Docker/deploy study guide
- how-it-works diagram

Recommended A03 target:

- Add study-guide style docs for A03 domain, DAL/EF, BLL, DTO/API, auth/tenant flow, and deployment.
- Add a request-flow diagram showing JWT, active gym, route `gymCode`, BLL authorization, EF query filters, audit, and soft delete.
- Keep `docs/a3-saas-plan.md` aligned with every future functionality change.
- Keep README and API docs synchronized with actual routes and response semantics.
- Continue logging AI assistance in `docs/ai-prompts.md` for implemented changes.

## Prioritized A03 Upgrade Roadmap

### P0: Defense Confidence and Safety

These should be addressed before claiming A03 is as safe and well-made as A18:

- Tighten or explicitly justify the weaker A03 password policy.
- Add tests for audit logging and soft delete.
- Add tenant-access/authorization service unit tests.
- Add impersonation tests covering reason, actor, target, claims, refresh token, and audit entry.
- Fail fast on unsafe/missing production CORS configuration.
- Decide whether A03 needs middleware-level gym resolution; if yes, implement it with tests.
- Finish API response metadata for public endpoints.
- Add study-guide documentation for auth, tenant isolation, BLL boundaries, and deployment.

### P1: Functional Depth and API/Client Polish

These move A03 from broad CRUD coverage toward a richer defendable product:

- Add a member workspace that aggregates profile, memberships, payments, bookings, attendance, and outstanding actions.
- Add training/coaching plan workflows inspired by A18 treatment plans: draft, publish, member/trainer decisions, progress, and completion.
- Expand membership lifecycle and payment rules with invoices or invoice-like records.
- Enforce subscription-tier limits in BLL and tests.
- Add richer maintenance workflows: recurring generation, assignment, completion evidence, overdue state, and equipment downtime.
- Convert remaining create/delete API result shapes to consistent `Created`/`NoContent` semantics where clients allow it.
- Add controller/service tests for training, maintenance, staff, platform, account, and impersonation flows.

### P2: Optional A18-Inspired Product Depth

These are useful if time remains after the safety and workflow foundations:

- Add finance reports: monthly revenue, overdue balances, package revenue, trainer/session revenue, and churn indicators.
- Add member contract/document metadata if the gym domain requires evidence similar to A18 legal previews.
- Add staff capacity planning from shifts, vacations, sessions, and maintenance assignments.
- Add support/operator views that combine tenant snapshot, audit events, subscription state, and recent errors.
- Add a high-level architecture/request-flow SVG like A18's `assignment-18-how-it-works.svg`.

## Do Not Copy Blindly

Do not copy these A18 patterns into A03 as-is:

- direct `AppDbContext` CRUD inside tenant API controllers where A03 already has BLL workflow services
- dental-specific entities that do not fit the gym domain
- static `wwwroot` UI as a replacement for A03's separate React client
- route changes that would break existing React/MVC consumers
- controller-local business validation that belongs in BLL services
- broad service splitting without a concrete complexity or testability reason

The strongest future A03 architecture is A03's current service-first gym SaaS foundation plus A18's workflow depth, metadata discipline, stronger security defaults, test evidence, and defense documentation.

## Definition of Done for Future A03 Parity Work

A future A03 improvement should not be called complete until:

- backend builds and relevant backend tests pass
- frontend tests pass when the React client is affected
- the changed workflow has positive, negative, and authorization tests
- tenant isolation is tested for wrong-gym and wrong-role cases where relevant
- API docs and Swagger metadata match actual routes and response statuses
- README, `docs/api.md`, `docs/testing.md`, and `docs/a3-saas-plan.md` are updated when behavior changes
- deployment notes are updated when runtime configuration, CORS, Docker, or public URL behavior changes
- `docs/ai-prompts.md` records the AI-assisted work

## Bottom Line

A03 already has the better architectural direction for a maintainable gym SaaS: service-first APIs, a real separate client, MVC surfaces, tenant-aware BLL authorization, and a broad gym domain.

A18 is still the stronger reference for mature workflows, test breadth, security defaults, API documentation discipline, tenant-resolution clarity, and defense study material.

The next A03 work should focus on making existing gym workflows deeper, safer, better documented, and better tested. It should not replace A03's architecture with A18's hybrid direct-DbContext controller style.
