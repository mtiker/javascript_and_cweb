# Full Project Audit

Audit date: 2026-05-08

Scope: `courses/webapp-csharp/assignment-03-multi-gym-management-system`

## Executive Summary

- Overall readiness for Assignment 3: PARTIAL, close but not fully defense-ready. The backend, domain model, REST API, JWT/refresh-token flow, DTOs, Swagger, localization, tenant isolation, React client, Docker files, and CI jobs all have concrete implementation evidence. Main grading risks are the read-heavy MVC Admin UX, unverified live separate-client hosting, formatting check failure, and deployment/runtime verification gaps.
- Overall readiness for Final1: PARTIAL. CLEAN/ONION project structure, repositories, Unit of Work, services, mappers, DTOs, and architecture tests exist. Remaining risks are that several BLL services still depend on `IAppDbContext`, MVC client/admin page services still query `AppDbContext`, and some workflows remain in shared BLL instead of clean feature-owned application services.
- Overall readiness for Final2: PARTIAL. Module projects exist for Users, GymManagement, Training, and MembershipFinance, with BuildingBlocks mediator and module-boundary tests. However modules still depend on shared `App.BLL`, `App.Domain`, and `App.DTO`; module DI comments explicitly say some services still need to move into modules; data ownership is documented but not fully enforced by code ownership.
- Top 10 risks:
  1. `dotnet format multi-gym-management-system.slnx --verify-no-changes` fails on `src/App.DAL.EF/Seeding/AppDataInit.RichSeed.cs`; CI could fail if format is enforced.
  2. MVC Admin UX is mostly read-only dashboards/lists; "Full Admin UX" for Final1/Final2 is not proven.
  3. Separate React client deployment is configured but live separate-domain hosting was not verified.
  4. Several BLL services still use `IAppDbContext` directly, weakening CLEAN/ONION readiness.
  5. MVC Client controllers and Admin page services query `AppDbContext` directly.
  6. Final2 modules are skeletal wrappers over shared BLL services in several slices, not fully owned modules.
  7. Refresh tokens are stored in browser `sessionStorage`, which is acceptable for the assignment flow but remains XSS-sensitive.
  8. Production deploy was not smoke-tested from a live host; Docker config was validated only syntactically.
  9. PostgreSQL Testcontainers tests were skipped in the normal `dotnet test` run.
  10. Some localization coverage is focused on key flows, but full MVC/Admin/React text coverage is incomplete.

## Assignment 3 Compliance Matrix

| Requirement | Status | Evidence | Missing / Weak Area | Risk | Recommended Fix Phase |
|---|---|---|---|---|---|
| ASP.NET Core backend | PASS | `WebApp/WebApp.csproj`, `WebApp/Program.cs`, `dotnet build` PASS | None found | LOW | Keep stable |
| At least 10 meaningful DB entities | PASS | 33 entity files in `src/App.Domain/Entities`, plus Identity entities; `src/App.DAL.EF/AppDbContext.cs` exposes DbSets | None for count; business richness still needs demo verification | LOW | Keep stable |
| REST API controllers | PASS | 28 API controllers under `WebApp/ApiControllers`, including tenant/system/identity controllers | None found | LOW | Keep stable |
| API versioning | PASS | `WebApp/Setup/WebApiExtensions.cs` uses URL segment versioning; controllers use `[ApiVersion("1.0")]` and `api/v{version:apiVersion}` routes | Only v1 exists, which is acceptable | LOW | Keep stable |
| Public DTOs | PASS | 85 DTO files under `src/App.DTO/v1`; controllers return DTO response types | None found | LOW | Keep stable |
| Swagger | PASS | `WebApp/Setup/WebApiExtensions.cs`, `WebApp/ConfigureSwaggerOptions.cs`, `Swashbuckle.AspNetCore` package | Swagger UI not manually opened during audit | LOW | Manual smoke phase |
| JWT authentication | PASS | `WebApp/Setup/IdentitySetupExtensions.cs`, `src/App.BLL/Services/TokenService.cs`, `WebApp/ApiControllers/ApiControllerBase.cs` | None found | LOW | Keep stable |
| Refresh-token flow | PASS | `src/Modules.Users/Application/Auth/UsersSessionService.cs`, `src/App.DAL.EF/Repositories/EfRefreshTokenRepository.cs`, tests in `AuthSecurityAndErrorTests.cs` | Token storage is JS-accessible in React client | MEDIUM | Security hardening phase |
| MVC client UX | PARTIAL | `WebApp/Areas/Client/Controllers`, `WebApp/Areas/Client/Views`, smoke tests in `SmokeTests.cs` | Client MVC controllers query `AppDbContext`; UX coverage is narrower than React client | MEDIUM | MVC completion phase |
| MVC admin UX protected, usable, no ViewBag/ViewData, view models | PARTIAL | `WebApp/Areas/Admin`, `WebApp/Models/Admin*ViewModel.cs`, `[Authorize]`, `MvcComplianceTests.cs` | Admin pages are mostly read-only; no full admin CRUD/forms were found | HIGH | Admin UX phase |
| UI translations using `.resx` | PASS | `src/App.Resources/SharedResources.resx`, `SharedResources.et.resx`, MVC localizer injection | Several Admin page strings are still hardcoded in English | MEDIUM | Localization polish phase |
| DB translations using LangStr/equivalent | PASS | `src/App.Domain/Common/LangStr.cs`, EF conversion in `AppDbContext.cs`, tests in `TrainingCategoryLocalizationTests.cs` | Full entity translation coverage not exhaustive | LOW | Localization polish phase |
| IDOR protection | PASS | `TenantAccessChecker`, `ResourceAuthorizationChecker`, global tenant filters in `AppDbContext.cs`, `TenantIsolationAndIdorTests.cs` | Continue adding tests for every new endpoint | LOW | Keep as regression suite |
| CI/CD deploy for ASP app and DB | PARTIAL | `.gitlab-ci.yml`, `Dockerfile`, `docker-compose.prod.yml`, `scripts/deploy.sh` | Live deploy not verified; default DB password remains fallback if env not overridden | MEDIUM | Deployment verification phase |
| Separate client app | PASS | `client/package.json`, `client/src`, `client/Dockerfile`, `client/nginx.conf` | None found | LOW | Keep stable |
| Separate client hosted from separate web server/domain if JS-based | NEEDS MANUAL VERIFICATION | `client/Dockerfile`, `client/nginx.conf`, `assignment03_deploy_client` CI job, compose `client` profile | Separate live host/domain was not verified | HIGH | Deployment verification phase |
| CORS handling | PASS | `AddAppCors` validates production origins and blocks wildcard/localhost outside Development | Production origin values need live verification | MEDIUM | Deployment verification phase |
| React client login/logout, JWT + refresh token, 3 entity CRUD | PASS | `client/src/lib/auth.tsx`, `apiClient.ts`, pages for Members, TrainingCategories, MembershipPackages; `CrudPages.test.tsx` | Refresh token stored in `sessionStorage`; no browser E2E against live backend | MEDIUM | E2E phase |

## Backend Functionality Audit

Backend projects:

- `WebApp/WebApp.csproj`: ASP.NET Core host, API, MVC, Identity, Swagger, composition root.
- `src/App.Domain/App.Domain.csproj`: entities, enums, common base types, roles, claims.
- `src/App.BLL/App.BLL.csproj`: services, contracts, mappers, exceptions.
- `src/App.DAL.EF/App.DAL.EF.csproj`: EF Core PostgreSQL context, repositories, migrations, seeding.
- `src/App.DTO/App.DTO.csproj`: public API DTOs.
- `src/App.Resources/App.Resources.csproj`: `.resx` localization resources.
- `src/BuildingBlocks/BuildingBlocks.csproj`: mediator/module abstractions.
- `src/Modules.Users`, `src/Modules.GymManagement`, `src/Modules.Training`, `src/Modules.MembershipFinance`: Final2 module projects.

Routes and controllers:

- Identity: `api/v1/account/register`, `login`, `logout`, `renew-refresh-token`, `switch-gym`, `switch-role`, `forgot-password`, `reset-password` in `WebApp/ApiControllers/Identity/AccountController.cs`.
- System: `api/v1/system/gyms`, `system/platform`, `system/subscriptions`, `system/support`, `system/impersonation`.
- Tenant routes: `api/v1/{gymCode}/members`, `training-categories`, `training-sessions`, `bookings`, `membership-packages`, `memberships`, `payments`, `finance-workspace`, `invoices`, `maintenance-tasks`, `equipment`, `opening-hours`, `gym-settings`, `gym-users`, staff/contract/vacation/job-role endpoints.

Findings:

- API versioning: PASS. Configured in `WebApp/Setup/WebApiExtensions.cs` with `UrlSegmentApiVersionReader`.
- Swagger setup: PASS. `AddSwaggerGen`, `ConfigureSwaggerOptions`, and `UseSwaggerUI` are present.
- Auth endpoints: PASS. Account routes are stable and tested by `SmokeTests.cs` and `AuthSecurityAndErrorTests.cs`.
- JWT generation/validation: PASS. `TokenService` signs HMAC SHA-256 tokens and `IdentitySetupExtensions` validates issuer, audience, signing key, and lifetime.
- Refresh-token rotation/reuse protection: PASS. `UsersSessionService.RefreshAsync` removes the old refresh token, creates a replacement, and tests verify reuse rejection.
- Logout invalidation: PASS. `UsersSessionService.LogoutAsync` removes all refresh tokens for the current user; integration test verifies renewal fails after logout.
- DTO usage: PASS. Public API surface uses `App.DTO.v1` request/response types.
- Controller thinness: PARTIAL. API controllers are thin and architecture tests block direct DbContext in API controllers. `HomeController` and MVC client controllers still use `AppDbContext` directly.
- Validation and ProblemDetails: PARTIAL. BLL throws `ValidationAppException`, `ForbiddenException`, `NotFoundException`, etc.; `ProblemDetailsMiddleware` serializes API errors. Validation is service-level, not consistently expressed as reusable validators.
- Tenant/gym resolution: PASS. `GymResolutionMiddleware` resolves route `gymCode`; `HttpGymContext` provides filter context.
- IDOR protection: PASS. `TenantAccessChecker` checks active gym and allowed roles; `ResourceAuthorizationChecker` checks member self access, trainer assignment access, and caretaker assignment access.
- Role authorization: PASS/PARTIAL. System controllers use `[Authorize(Roles=...)]`; tenant role checks are in BLL authorization services. Some MVC redirects use local role checks rather than policies.
- DB migrations: PASS. Migrations exist in `src/App.DAL.EF/Migrations`.
- Seed data: PASS. Seeding exists in `src/App.DAL.EF/Seeding`; demo accounts are covered by integration tests.
- Health checks: PASS. `AddHealthChecks` and `/health` mapping exist. React nginx exposes `/healthz`.

Risky backend code:

- `src/App.BLL/Services/IdentityService.cs` and `src/Modules.Users/Application/Auth/UsersSessionService.cs` duplicate much of the auth/session logic. This creates drift risk.
- `src/App.BLL/Services/AccountAuthService.cs` appears unused in production DI after migration to Users module. It is still referenced by tests/helpers and should be removed only in a planned cleanup.
- `WebApp/Setup/WebApiExtensions.cs` allows broad forwarded headers by clearing known proxies and adding `IPAddress.Any/IPv6Any`. This is convenient behind course proxies but risky in uncontrolled production unless the reverse proxy boundary is trusted.
- `WebApp/Controllers/HomeController.cs` and Client MVC controllers use `AppDbContext` directly, which is a Final1 boundary risk.

## Domain Model Audit

Meaningful entity count:

- 33 domain entity files in `src/App.Domain/Entities`.
- Identity entities in `src/App.Domain/Identity`: `AppUser`, `AppRole`, `AppRefreshToken`.
- Meaningful business entities include `Gym`, `GymSettings`, `Subscription`, `SupportTicket`, `Person`, `Contact`, `Member`, `Staff`, `JobRole`, `EmploymentContract`, `Vacation`, `TrainingCategory`, `TrainingSession`, `WorkShift`, `Booking`, `MembershipPackage`, `Membership`, `Payment`, `CoachingPlan`, `CoachingPlanItem`, `Invoice`, `InvoiceLine`, `InvoicePayment`, `OpeningHours`, `OpeningHoursException`, `EquipmentModel`, `Equipment`, `MaintenanceTask`, `MaintenanceTaskAssignmentHistory`, `AuditLog`, and join/contact entities.

Aggregate/business areas:

- Platform and tenant management: `Gym`, `GymSettings`, `Subscription`, `SupportTicket`, `AppUserGymRole`.
- People and staffing: `Person`, `Contact`, `Member`, `Staff`, `JobRole`, `EmploymentContract`, `Vacation`.
- Training and booking: `TrainingCategory`, `TrainingSession`, `WorkShift`, `Booking`.
- Membership and finance: `MembershipPackage`, `Membership`, `Payment`, `Invoice`, `InvoiceLine`, `InvoicePayment`.
- Coaching: `CoachingPlan`, `CoachingPlanItem`.
- Facility and maintenance: `OpeningHours`, `OpeningHoursException`, `EquipmentModel`, `Equipment`, `MaintenanceTask`, `MaintenanceTaskAssignmentHistory`.

Constraints and safety:

- PASS: Tenant entities mostly inherit `TenantBaseEntity`, which supplies `GymId`, audit fields, and soft delete fields.
- PASS: `AppDbContext` globally sets `DeleteBehavior.Restrict`, reducing accidental cascade-delete damage.
- PASS: Tenant query filters and soft-delete filters are configured in `AppDbContext`.
- PASS: Unique/index constraints exist for gym code, gym settings, user/gym/role links, member code, person/member per gym, staff code, role code, booking uniqueness, invoice number, equipment asset/serial numbers, and other common queries.
- PARTIAL: `Gym`, `Person`, `Contact`, `PersonContact`, `AuditLog`, `Subscription`, `SupportTicket`, and some join/platform entities do not share the full tenant audit/soft-delete base. Some of that is intentional, but it should be defended explicitly.
- PARTIAL: Some entities are data-heavy with limited domain behavior. Business rules mostly live in services, which is acceptable for this course architecture but should be explained as service-oriented BLL rather than rich-domain DDD.
- NEEDS MANUAL VERIFICATION: ERD and diagrams in `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/data-model.md` were not exhaustively cross-checked against every current entity.

## MVC UX Audit

Client MVC UX:

- Evidence: `WebApp/Areas/Client/Controllers`, `WebApp/Areas/Client/Views`, models such as `ClientDashboardViewModel`, `SessionsPageViewModel`, `MaintenancePageViewModel`.
- Protected: PASS. Client area controllers use `[Authorize]`.
- Functional domain proof: PARTIAL/PASS. It supports dashboard, sessions, booking/cancel, attendance roster, profile, and maintenance status flows.
- Risk: Client MVC controllers use `AppDbContext` directly, for example `DashboardController`, `SessionsController`, `ProfileController`, and `MaintenanceController`. This is a Final1 boundary risk.

Admin MVC UX:

- Evidence: `WebApp/Areas/Admin/Controllers`, `WebApp/Areas/Admin/Views`, `WebApp/Models/Admin*ViewModel.cs`, `WebApp/Areas/Admin/Services/AdminViewModelServices.cs`.
- Protected: PASS. Admin controllers use `[Authorize]` and role checks redirect/forbid unauthorized users.
- View models: PASS. Admin views are strongly typed and tested by `MvcComplianceTests.cs`.
- ViewBag/ViewData: PASS. Static tests verify no Admin page use.
- Anti-forgery: PASS for detected Admin POST actions; currently Admin is mostly GET/read-only.
- Functional depth: PARTIAL. Admin pages render dashboards/lists for Gyms, Members, Memberships, Sessions, Operations. No full Admin CRUD/edit forms were found, so Final1/Final2 "Full Admin UX" remains a grading risk.
- Tenant exposure: PASS/PARTIAL. Admin controllers use active gym context checks; Admin page services query by `gymId`. Continue testing every new Admin mutation for tenant isolation.

## Localization Audit

- UI `.resx`: PASS. `src/App.Resources/SharedResources.resx` and `SharedResources.et.resx` exist with 114 resource entries total across both files.
- MVC handling: PASS. `AddLocalization`, `AddViewLocalization`, and `AddDataAnnotationsLocalization` are configured. `UseRequestLocalization` is in the pipeline. MVC layout and several views inject `IStringLocalizer<SharedResources>`.
- Culture switching: PASS. `HomeController.SetCulture` stores a supported culture cookie and falls back safely; tested in `AuthSecurityAndErrorTests.cs`.
- Accept-Language: PASS. Default localization provider chain is active; tests verify `Accept-Language` affects API LangStr responses.
- DB translations: PASS. `LangStr` supports exact, neutral, default, and first-value fallback; EF stores `LangStr` as JSONB; tests cover fallback and PostgreSQL round-trip, although PostgreSQL tests were skipped in normal `dotnet test`.
- React language handling: PASS/PARTIAL. `client/src/lib/language.tsx` stores language in localStorage, updates document language, and `apiClient.ts` sends `Accept-Language`.
- Gaps: Some Admin views still contain hardcoded English labels, for example `Areas/Admin/Views/Members/Index.cshtml` and `Areas/Admin/Views/Memberships/Index.cshtml`. React translation dictionary is local to the client and not `.resx`, which is acceptable for a separate SPA but should be explained.

## React Client Audit

Frontend/client projects:

- `client/package.json`: Vite + React + TypeScript scripts.
- `client/src`: React app, pages, API client, auth provider, language provider.
- `client/Dockerfile` and `client/nginx.conf`: separate static web server container.

Findings:

- Login/logout: PASS. `AuthProvider` calls `api.login` and `api.logout`; tests cover auth behavior.
- JWT storage and usage: PASS/PARTIAL. Session is stored in `sessionStorage` via `client/src/lib/storage.ts`; `Authorization: Bearer` is set by `ApiClient`. Security risk remains because refresh tokens are JS-accessible.
- Refresh-token flow: PASS. `ApiClient` retries one 401 after calling `/api/v1/account/renew-refresh-token`, deduplicates concurrent refreshes, and clears session on refresh failure. Covered by `apiClient.test.ts`.
- CRUD for at least 3 entities: PASS. Members, Training Categories, and Membership Packages have create/update/delete tests in `CrudPages.test.tsx`; additional workflows exist for sessions, maintenance, coaching, and finance.
- Loading/error/validation states: PASS/PARTIAL. Tests cover several loading, validation, and API error paths. Not every page has full negative-path coverage.
- CORS compatibility: PASS/PARTIAL. Client sends cross-origin requests to `VITE_API_BASE_URL`; backend CORS validates configured production origins. Live origin pairing not verified.
- Environment variables: PASS. `VITE_API_BASE_URL` is supported in `auth.tsx` and `client/Dockerfile`.
- Production build: PASS. `npm run build` completed successfully.
- Separate hosting: PARTIAL/NEEDS MANUAL VERIFICATION. A standalone nginx client image exists, and CI has a manual client deploy job. The backend Dockerfile also embeds the React client under `wwwroot/client`, so the repo supports both same-host and separate-host modes. A live separate domain was not verified.

## Test Coverage Audit

Existing tests:

- Domain/unit tests: `LangStrTests.cs`, `AppDbContextBehaviorTests.cs`, workflow service tests.
- Service/BLL tests: `MemberWorkflowServiceTests.cs`, `MembershipWorkflowServiceTests.cs`, `TrainingWorkflowServiceTests.cs`, `MaintenanceWorkflowServiceTests.cs`, `SubscriptionTierLimitServiceTests.cs`, `AuthorizationServiceTests.cs`, `MembershipFinanceCleanSliceTests.cs`.
- Repository/persistence tests: `PostgreSqlPersistenceTests.cs`, skipped in normal run because Docker-dependent.
- API integration tests: `SmokeTests.cs`, `MemberCrudTests.cs`, `MembershipPackageCrudTests.cs`, `TrainingCategoryLocalizationTests.cs`, `StaffWorkflowTests.cs`, `ProposalWorkflowTests.cs`, `Final1CriticalE2ETests.cs`.
- Auth/security tests: `AuthSecurityAndErrorTests.cs`, `TenantIsolationAndIdorTests.cs`, `ImpersonationTests.cs`.
- MVC tests: `MvcComplianceTests.cs`, `AdminMembersPageTests.cs`, `SmokeTests.cs`.
- React tests: `auth.test.ts`, `apiClient.test.ts`, `App.test.tsx`, `CrudPages.test.tsx`, `OperationsPages.test.tsx`, `SessionsPage.test.tsx`, `WorkspacePages.test.tsx`.
- E2E tests: No browser-level Playwright/Cypress E2E tests found. ASP.NET integration tests cover many end-to-end API/MVC flows in-process.
- CI commands: `.gitlab-ci.yml` runs React tests/build, `dotnet build`, `dotnet test`, backend Docker build, client Docker build, backend deploy, and manual client deploy.

Coverage numbers observed:

- `dotnet test`: 197 passed, 3 skipped, 200 total.
- React `npm test`: 34 passed across 7 test files.
- Static count: 186 xUnit `[Fact]`/`[Theory]` declarations found.

Critical missing or weak tests:

- Login: covered.
- Refresh-token rotation: covered.
- Refresh-token reuse rejection: covered.
- Logout invalidation: covered.
- Cross-tenant IDOR: covered for members, bookings, maintenance, system/tenant separation; continue expanding for every endpoint.
- Member self-only access: covered.
- Trainer assignment-only access: covered.
- Caretaker assignment-only access: covered.
- 3 React CRUD flows: covered for Members, Training Categories, Membership Packages.
- Localization: covered for LangStr, Accept-Language, MVC login labels; full UI text coverage incomplete.
- Admin authorization: covered.
- No ViewBag/ViewData: covered.
- Architecture boundaries: covered for Clean/Onion and module references, but tests intentionally allow current BLL EF abstractions and do not prove full module data ownership.

## Final1 CLEAN/ONION Readiness

Status: PARTIAL.

Evidence:

- Project separation exists: `App.Domain`, `App.DTO`, `App.BLL`, `App.DAL.EF`, `WebApp`.
- Repositories and Unit of Work exist in `src/App.BLL/Contracts/Persistence` and `src/App.DAL.EF/Repositories`.
- Services/BLL exist in `src/App.BLL/Services`.
- Mappers exist in `src/App.BLL/Mapping`.
- Architecture tests exist in `tests/WebApp.Tests/Architecture/ArchitectureTests.cs`.
- API controllers are thin and do not depend on DbContext.

Risks:

- `App.BLL.csproj` references `Microsoft.EntityFrameworkCore`, and several BLL services depend on `IAppDbContext`. This is cleaner than depending on `App.DAL.EF`, but it is still not a strict repository-only application layer.
- Direct `IAppDbContext` use found in `IdentityService`, `CurrentActorResolver`, `ResourceAuthorizationChecker`, `TenantAccessChecker`, `PlatformService`, `SubscriptionTierLimitService`, `BookingPricingService`, `MemberWorkspaceService`, `StaffWorkflowService`, and `CoachingPlanService`.
- MVC `HomeController` and Client MVC controllers depend directly on `AppDbContext`.
- Admin page services `AdminOperationsPageService` and `AdminSessionsPageService` depend directly on `AppDbContext`.
- Final1 "Full Admin UX" is only PARTIAL because Admin pages are mostly read-only.

Recommended readiness judgment:

- Defensible as an in-progress Clean/Onion migration with strong tests.
- Not yet defensible as a strict repository/UOW-only Clean/Onion implementation.

## Final2 Modular Monolith Readiness

Status: PARTIAL.

Evidence:

- Module projects exist: `src/Modules.Users`, `src/Modules.GymManagement`, `src/Modules.Training`, `src/Modules.MembershipFinance`.
- Building blocks exist: `src/BuildingBlocks/Modules/IModule.cs`, `src/BuildingBlocks/Mediator`.
- Composition root uses `WebApp/Setup/ModuleExtensions.cs`.
- Mediator usage exists in API controllers and module handlers.
- Boundary tests exist in `tests/WebApp.Tests/Architecture/ModuleArchitectureTests.cs`.
- Candidate modules satisfy "Users plus 2 business modules"; current set is Users plus three business modules.

Gaps:

- Modules reference shared `App.BLL`, `App.Domain`, and `App.DTO`. That may be acceptable as an intermediate modular monolith, but it is not full isolated module ownership.
- Module DI extension comments state some services still need to move into modules, for example Training, GymManagement, and MembershipFinance comments mention future movement.
- Data ownership per module is mostly documented in `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/module-boundaries.md`, but persistence remains centralized in `App.DAL.EF/AppDbContext.cs`.
- Cross-module communication uses mediator for several controller flows, but some business services still communicate through shared BLL/data abstractions.

Recommended readiness judgment:

- Passable as a Final2 skeleton/progression if the grading focus accepts incremental modularization.
- Risky if the grader expects fully internal module application layers, module-owned persistence boundaries, and no shared BLL business core.

## Deployment / CI/CD Audit

CI/CD files:

- Root-level `.gitlab-ci.yml`.
- Assignment-level `.gitlab-ci.yml`.

Docker/deployment files:

- `Dockerfile`: multi-stage backend image; builds React client and publishes WebApp.
- `client/Dockerfile`: standalone nginx-hosted React client image.
- `client/nginx.conf`: SPA hosting, cache headers, `/healthz`.
- `docker-compose.yml`: local PostgreSQL only.
- `docker-compose.prod.yml`: PostgreSQL, backend, optional client profile.
- `scripts/deploy.sh`: backend stack deploy.
- `scripts/deploy-client.sh`: standalone client deploy.
- `courses/webapp-csharp/assignment-03-multi-gym-management-system/docs/deployment.md`.

Findings:

- Backend Dockerfile: PASS. Builds .NET app and embeds React client into `wwwroot/client`.
- Client Dockerfile: PASS. Builds Vite app and serves from nginx.
- docker-compose files: PASS/PARTIAL. Local compose validates. Production compose validates with dummy required env vars. Runtime not smoke-tested.
- Database deployment/migration: PASS/PARTIAL. `DataInitialization__MigrateDatabase` and `SeedData` are configured; live migration execution not verified.
- GitLab CI: PASS/PARTIAL. Build/test/package/deploy jobs exist. Client deploy is manual and allow-failure.
- Environment variables: PASS/PARTIAL. Required JWT key is enforced in production compose and deploy script. Default PostgreSQL password fallback remains dangerous if not overridden.
- Secrets handling: PARTIAL. `Jwt:Key` is only in development settings locally; production uses env var. DB defaults should be overridden in deployment.
- CORS production config: PASS/PARTIAL. Production validation rejects missing, wildcard, localhost, and path-containing origins. Live deployed origin not verified.
- Health/smoke checks: PARTIAL. Backend `/health` and client `/healthz` exist. CI does not appear to run post-deploy smoke checks.

## Risk Register

| Risk | Area | Severity | Probability | Evidence | Mitigation | Suggested Phase |
|---|---|---:|---:|---|---|---|
| Format verification fails | Code quality/CI | HIGH | HIGH | `dotnet format ... --verify-no-changes` failed on `src/App.DAL.EF/Seeding/AppDataInit.RichSeed.cs` | Run formatter in a dedicated formatting-only change | Assignment 3 stabilization |
| Admin UX may be judged incomplete | MVC/Admin | HIGH | HIGH | Admin views are read-only dashboard/list pages | Add full Admin CRUD/workflows with view models and tests | Admin UX phase |
| Separate client hosting not proven live | Deployment/React | HIGH | MEDIUM | Client Dockerfile and deploy job exist; no live URL verified | Deploy client profile to separate host/domain and document URL | Deployment verification |
| BLL still uses DbContext abstraction | Clean/Onion | MEDIUM | HIGH | Multiple BLL services depend on `IAppDbContext` | Move remaining data access behind repositories/UOW | Final1 hardening |
| MVC controllers query EF directly | Clean/Onion/MVC | MEDIUM | HIGH | Client MVC controllers and `HomeController` inject `AppDbContext` | Introduce MVC page/query services using BLL contracts | Final1 hardening |
| Modules are not fully self-owned | Final2 | HIGH | HIGH | Modules reference shared BLL/Domain/DTO; DI comments mention future moves | Move handlers/services/contracts per module and enforce ownership tests | Final2 modularization |
| Refresh token stored in JS-accessible sessionStorage | Security/React | MEDIUM | MEDIUM | `client/src/lib/storage.ts` stores full session | Consider secure cookie or short refresh lifetime; document assignment tradeoff | Security hardening |
| PostgreSQL tests skipped in normal test run | Persistence | MEDIUM | MEDIUM | `dotnet test` skipped 3 Docker-dependent tests | Run Docker-enabled tests before defense and in CI if runner supports Docker | Test hardening |
| Localization not exhaustive | i18n | MEDIUM | MEDIUM | Some Admin views contain hardcoded English labels | Replace remaining hardcoded MVC labels with resx/localizer | Localization polish |

## Missing Parts Backlog

Must fix for Assignment 3:

- Fix formatting failure in `src/App.DAL.EF/Seeding/AppDataInit.RichSeed.cs`.
- Verify deployed backend and database migration path on target host.
- Verify and document live separate React client host/domain, or clearly state same-host mode if separate hosting is not required by active grading.
- Add or document production smoke checks for `/health`, Swagger/API login, React login, and one CRUD flow.
- Confirm Admin MVC UX expectations with grader; if "usable" means CRUD, add Admin create/edit/delete flows.

Must fix for Final1:

- Move remaining direct `IAppDbContext` BLL queries behind repositories/UOW where mandatory.
- Move MVC Client and Admin page data access out of controllers/page services into BLL/application services.
- Expand full Admin UX to support actual management actions, not only read-only dashboards.
- Keep architecture tests strict after each migration step.

Must fix for Final2:

- Move module-owned application services into their module projects.
- Reduce direct dependency on shared `App.BLL` business services from module implementations.
- Define and enforce module data ownership in code, not only documentation.
- Add architecture tests for forbidden shared-service shortcuts and module-owned handlers.
- Demonstrate mediator-only cross-module communication with at least one real cross-module business workflow.

Nice to have / bonus:

- Add Playwright E2E tests for login, refresh, three React CRUD flows, and role-specific workspaces.
- Add post-deploy smoke checks to CI.
- Replace production compose DB password defaults with required secrets.
- Add broader `.resx` coverage for Admin and Client MVC views.
- Add coverage reporting artifacts to CI.

## Recommended Next 5 Codex Phases

1. Objective: Stabilize Assignment 3 validation.
   Files likely touched: `src/App.DAL.EF/Seeding/AppDataInit.RichSeed.cs`, possibly `.editorconfig`.
   Tests to add first: none; this is formatting-only.
   Validation commands: `dotnet format multi-gym-management-system.slnx --verify-no-changes`, `dotnet build`, `dotnet test`.
   Done when: format/build/tests all pass without source behavior changes.

2. Objective: Prove deployment and separate-client hosting.
   Files likely touched: `docs/deployment.md`, assignment `README.md`, `docker-compose.prod.yml`, `scripts/deploy-client.sh` if needed.
   Tests to add first: CI smoke script or documented manual smoke checklist.
   Validation commands: `docker compose -f docker-compose.prod.yml config`, backend `/health`, client `/healthz`, API login, React login.
   Done when: backend and client URLs are documented and verified from a third device/network.

3. Objective: Complete Admin MVC UX for grading.
   Files likely touched: `WebApp/Areas/Admin/Controllers`, `Views`, `Models`, Admin page services, BLL services as needed.
   Tests to add first: failing MVC integration tests for Admin create/edit/delete and authorization.
   Validation commands: `dotnet test`, targeted MVC/Admin tests.
   Done when: Admin can manage core tenant entities using view models, anti-forgery, validation messages, and no ViewBag/ViewData.

4. Objective: Final1 boundary hardening.
   Files likely touched: `src/App.BLL/Contracts/Persistence`, `src/App.DAL.EF/Repositories`, BLL services using `IAppDbContext`, MVC page services.
   Tests to add first: architecture tests blocking new `IAppDbContext` dependencies in selected slices.
   Validation commands: `dotnet test`, architecture tests.
   Done when: selected slices use repositories/UOW/services/mappers consistently.

5. Objective: Final2 module ownership hardening.
   Files likely touched: `src/Modules.*`, `src/BuildingBlocks`, module contract docs, architecture tests.
   Tests to add first: module-boundary tests that prevent direct use of another module's internals and shared BLL shortcuts for moved slices.
   Validation commands: `dotnet test`, module architecture tests.
   Done when: Users plus at least two business modules own their handlers/services and communicate through mediator/contracts.

## Commands Run

| Command | Result | Notes |
|---|---|---|
| `git status --short` | PASS | Worktree had many pre-existing modified/untracked files. Audit did not revert them. |
| `rg --files` and targeted `rg`/`Get-Content` inspections | PASS | Used to map projects, controllers, DTOs, tests, localization, deployment, and module evidence. |
| `dotnet build multi-gym-management-system.slnx` | PASS | Build succeeded, 0 warnings, 0 errors. |
| `dotnet test multi-gym-management-system.slnx` | PASS/PARTIAL | 197 passed, 3 skipped, 200 total. Skipped tests were Docker/PostgreSQL-dependent persistence tests. |
| `dotnet format multi-gym-management-system.slnx --verify-no-changes` | FAIL | Whitespace formatting failures in `src/App.DAL.EF/Seeding/AppDataInit.RichSeed.cs`. No formatting was applied. |
| `npm test` in `client` | PASS | 7 test files passed, 34 tests passed. React Router future-flag warnings were printed. |
| `npm run build` in `client` | PASS | TypeScript no-emit and Vite production build succeeded. |
| `docker compose config` | PASS | Local compose config valid for PostgreSQL service. |
| `docker compose -f docker-compose.prod.yml config` with dummy `JWT__Key` and `VITE_API_BASE_URL` | PASS | Production backend/db config valid syntactically. |
| `docker compose --profile client -f docker-compose.prod.yml config` with dummy env vars | PASS | Production backend/db/client config valid syntactically. |
