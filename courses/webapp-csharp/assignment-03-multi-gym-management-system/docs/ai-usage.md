# AI Usage Log

## 2026-04-23 - Batch 4 Client Workspaces, REST Semantics, and Study Docs

Task:
- implement Batch 4 by adding dedicated React workspace pages, aligning selected endpoint REST semantics with client compatibility, and adding defense-study documentation artifacts

Files affected:
- `client/src/App.tsx`
- `client/src/components/AppShell.tsx`
- `client/src/pages/{MemberWorkspacePage.tsx,TrainerCoachingWorkspacePage.tsx,FinanceWorkspacePage.tsx,MaintenanceTasksPage.tsx}`
- `client/src/lib/{apiClient.ts,types.ts}`
- frontend tests (`client/src/App.test.tsx`, `client/src/pages/{CrudPages.test.tsx,OperationsPages.test.tsx,WorkspacePages.test.tsx}`)
- selected tenant controllers returning workflow-compatible `201`/`204` responses
- `tests/WebApp.Tests/Helpers/ControllerTestHelpers.cs`
- controller unit tests (`TenantControllerTests.cs`, `AdditionalControllerTests.cs`)
- assignment docs (`README.md`, `docs/{a3-saas-plan.md,api.md,testing.md,deployment.md}`)
- new study docs:
  - `docs/request-flow-diagram.md`
  - `docs/study-guide-domain.md`
  - `docs/study-guide-dal-ef.md`
  - `docs/study-guide-bll.md`
  - `docs/study-guide-dto-api.md`
  - `docs/study-guide-auth-tenant-flow.md`
  - `docs/study-guide-deployment.md`

What AI helped with:
- added role-specific React workflow pages for member workspace, trainer coaching workspace, finance workspace, and expanded maintenance workspace
- wired role landing routes and shell navigation for workspace-first UX
- aligned create/delete/cancel semantics for workflow endpoints to return `201` and `204` where client handling was updated
- extended controller tests and frontend Vitest coverage for routing and workspace mutations
- produced assignment-specific study guides and a request-flow Mermaid diagram for defense explanations

What needed manual review or correction:
- UTF-8 display in terminal output was unreliable for one localized example string; docs were normalized to an ASCII fallback example text
- response-shape assertions in controller unit tests were updated together with the endpoint semantics to avoid stale expectations

Alternatives considered:
- converting every create/delete endpoint in one pass, but only workflow-covered endpoints were switched to avoid breaking unchanged consumers

## 2026-04-23 - Batch 3 Functional Depth (Workspaces, Coaching, Finance, Lifecycle, Limits, Maintenance)

Task:
- implement Batch 3 functional depth while preserving the Assignment 03 service-first architecture and existing route structure

Files affected:
- domain entities (`CoachingPlan`, `CoachingPlanItem`, `Invoice`, `InvoiceLine`, `InvoicePayment`, `MaintenanceTaskAssignmentHistory`) and enum updates
- DTO resource folders under `src/App.DTO/v1/{CoachingPlans,Finance,MemberWorkspace,MaintenanceTasks,Memberships}`
- BLL service contracts/implementations:
  - `MemberWorkspaceService`
  - `CoachingPlanService`
  - `FinanceWorkspaceService`
  - `SubscriptionTierLimitService`
  - updates in `MembershipWorkflowService` and `MaintenanceWorkflowService`
- EF context + migration:
  - `src/App.DAL.EF/AppDbContext.cs`
  - `src/App.DAL.EF/Migrations/20260422204122_Batch3WorkspacesAndFinance*`
- tenant controllers:
  - `MemberWorkspaceController`
  - `CoachingPlansController`
  - `FinanceController`
  - extended `MembershipsController` and `MaintenanceTasksController`
- tests:
  - `tests/WebApp.Tests/Unit/SubscriptionTierLimitServiceTests.cs`
  - `tests/WebApp.Tests/Unit/MembershipWorkflowServiceTests.cs`
  - controller-forwarding coverage in `AdditionalControllerTests.cs`

What AI helped with:
- added member workspace aggregation APIs
- added coaching-plan workflow with plan statuses and item decisions
- added finance workspace APIs with invoices, lines, payments, refunds, overdue/outstanding logic
- expanded membership status lifecycle handling in BLL
- added starter/growth/enterprise subscription-limit enforcement service with tests
- expanded maintenance workflow with recurring due-generation, assignment history, completion notes, and downtime fields

What needed manual review or correction:
- migration and seed updates were reviewed to ensure existing gym-domain data stayed coherent and tenant-isolated
- public routes were kept stable; no route-shape migration was introduced during Batch 3

Alternatives considered:
- embedding workflow read logic directly in controllers, but service-first BLL orchestration was kept for consistency and defense clarity

## 2026-04-22 - Batch 2 Tenant Resolution And API Contract Polish

Task:
- implement Assignment 03 Batch 2 API foundation work: tenant gym-code middleware resolution, API error contract metadata, and broader controller verification coverage without route changes

Files affected:
- `src/WebApp/Middleware/GymResolutionMiddleware.cs`
- `src/WebApp/Setup/HttpGymContext.cs`
- `src/WebApp/Setup/MiddlewareExtensions.cs`
- `src/WebApp/ApiControllers/ApiControllerBase.cs`
- `src/WebApp/ApiControllers/Identity/AccountController.cs`
- `src/WebApp/ApiControllers/System/{GymsController.cs,PlatformController.cs,SubscriptionsController.cs,SupportController.cs,ImpersonationController.cs}`
- `tests/WebApp.Tests/Helpers/ControllerTestHelpers.cs`
- `tests/WebApp.Tests/Unit/{AdditionalControllerTests.cs,ApiContractMetadataTests.cs}`
- `tests/WebApp.Tests/Integration/AuthSecurityAndErrorTests.cs`
- assignment README and docs (`a3-saas-plan.md`, `api.md`, `testing.md`)

What AI helped with:
- added middleware that resolves `/api/v{version}/{gymCode}/...` gym codes early in the pipeline, rejecting unknown gyms with `404` and inactive gyms with `403` before controller/BLL execution
- stored resolved gym data in request items and surfaced it through `HttpGymContext` without changing BLL authorization flow
- standardized public API error metadata to advertise `ProblemDetails` for `400`, `401`, `403`, `404`, and `409`
- expanded unit tests from members/bookings/memberships to training sessions, maintenance, staff, identity, platform, support, subscriptions, and impersonation controllers
- added integration tests that prove early unknown/inactive gym rejection behavior
- added metadata regression tests so future API controllers keep the same documented error contract

What needed manual review or correction:
- initial enum values in new unit tests (`Scheduled`, `Inspection`, `Completed`) did not match Assignment 03 domain enums and were corrected to `Published`, `Scheduled`, and `Done`

Alternatives considered:
- adding repetitive `[ProducesResponseType]` attributes to every action, but class-level metadata plus base-controller inheritance keeps the contract explicit with less duplication

## 2026-04-22 - Batch 1 Security Hardening And Credential Upgrade

Task:
- implement Assignment 03 Batch 1 hardening priorities for startup security, tenant authorization verification depth, audit/soft-delete verification, and impersonation workflow evidence
- replace weak seeded/demo passwords with a strong default credential compatible with strict password policy

Files affected:
- `src/WebApp/Program.cs`
- `src/WebApp/Setup/IdentitySetupExtensions.cs`
- `src/WebApp/Setup/WebApiExtensions.cs`
- `src/WebApp/Setup/MiddlewareExtensions.cs`
- `src/WebApp/appsettings.json`
- `src/App.Domain/Security/AppClaimTypes.cs`
- `src/App.BLL/Services/ITokenService.cs`
- `src/App.BLL/Services/TokenService.cs`
- `src/App.BLL/Services/PlatformService.cs`
- `src/App.DTO/v1/System/StartImpersonationRequest.cs`
- `src/App.DTO/v1/System/StartImpersonationResponse.cs`
- `src/App.DAL.EF/Seeding/AppDataInit.cs`
- `src/App.DAL.EF/Seeding/AppDataInit.Helpers.cs`
- `src/WebApp/Views/Home/Index.cshtml`
- `client/src/pages/LoginPage.tsx`
- `client/src/pages/SaasConsolePage.tsx`
- `tests/WebApp.Tests/CustomWebApplicationFactory.cs`
- `tests/WebApp.Tests/Unit/RuntimeConfigurationTests.cs`
- `tests/WebApp.Tests/Unit/AppDbContextBehaviorTests.cs`
- `tests/WebApp.Tests/Unit/AuthorizationServiceTests.cs`
- `tests/WebApp.Tests/Integration/ImpersonationTests.cs`
- updated integration tests using seeded credentials
- assignment README and docs (`a3-saas-plan.md`, `testing.md`, `deployment.md`)

What AI helped with:
- tightened Identity password policy to require minimum length, digit, uppercase, lowercase, and symbol
- enforced `RequireHttpsMetadata` outside development
- enabled forwarded-header processing before HTTPS/auth middleware for reverse-proxy hosting
- added production CORS fail-fast validation for missing/unsafe origins
- deepened impersonation behavior to include reason validation, actor/target metadata, refresh-token creation, JWT impersonation claims, and audit-log writes
- added targeted unit/integration coverage for runtime config, authorization invariants, EF audit/soft-delete behavior, and impersonation evidence
- upgraded weak seeded/demo/test passwords to `GymStrong123!` and synchronized UI/test references

What needed manual review or correction:
- initial strict policy broke seeded credentials; seed defaults and test credentials were then upgraded to a strong value instead of keeping legacy weak passwords
- production-only CORS fail-fast required test host environment configuration updates

Alternatives considered:
- preserving `Gym123!` via seed-only password-hash bypass, but this was removed after switching all seed/demo/test credentials to a strong password baseline

## 2026-04-22 - Backend Structure Alignment With Assignment 18

Task:
- refactor the Assignment 03 backend organization to match the clearer Assignment 18 style
- preserve existing HTTP routes, DTO JSON shapes, EF model, seed data, authentication, tenant isolation, and deployment behavior

Files affected:
- `src/App.Domain/Entities/*`
- `src/App.DTO/v1/*`
- `src/App.BLL/Contracts/*`
- `src/App.BLL/Services/*`
- `src/App.BLL/Exceptions/*`
- `src/App.DAL.EF/Seeding/*`
- `src/WebApp/ApiControllers/*`
- `src/WebApp/Setup/*`
- `src/WebApp/Program.cs`
- `src/WebApp/Middleware/ProblemDetailsMiddleware.cs`
- `tests/WebApp.Tests/Integration/SmokeTests.cs`
- `README.md`
- `docs/*.md`

What AI helped with:
- splitting grouped domain entity files into one public entity per file
- splitting grouped DTO files into Assignment 18-style resource folders and namespaces
- moving BLL service interfaces beside implementations and infrastructure contracts under `App.BLL.Contracts.Infrastructure`
- splitting infrastructure services, WebApp setup extensions, and seed initialization into focused files
- standardizing BLL exception names to `NotFoundException`, `ForbiddenException`, and `ValidationAppException`
- splitting broad tenant controllers into resource controllers while keeping the existing `/api/v1/{gymCode}/...` URLs
- adding route smoke coverage for one endpoint from each newly split tenant controller

What needed manual review or correction:
- mechanical namespace updates initially left duplicate `using` directives, which were cleaned before the final build
- EF migration files were restored after incidental encoding/formatting churn so the refactor does not commit a migration change
- route templates were reviewed after the controller split to ensure external API URLs stayed unchanged

Alternatives considered:
- leaving the grouped files in place and documenting the exception, but that would not address the Assignment 18 alignment request
- adding per-entity EF configuration files, but the existing `AppDbContext` configuration was kept to reduce model-drift risk during a structure-only refactor

## 2026-04-22 - A3 Translation, Tenant Switch, and Demo Data Pass

Task:
- fix mixed English/Estonian labels in MVC and React client surfaces
- let SystemAdmin select an active tenant context
- expand seed data into a realistic gym demo dataset

Files affected:
- `client/src/components/AppShell.tsx`
- `client/src/lib/language.tsx`
- `client/src/pages/LoginPage.tsx`
- `client/src/pages/SaasConsolePage.tsx`
- `src/App.BLL/Services/IdentityService.cs`
- `src/App.DAL.EF/Seeding/AppDataInit.cs`
- `src/App.Resources/SharedResources*.resx`
- `src/WebApp/Controllers/HomeController.cs`
- `src/WebApp/ViewComponents/WorkspaceSwitcherViewComponent.cs`
- MVC dashboard/profile/operations views
- integration tests and assignment documentation

What AI helped with:
- adding localized resource keys for active gym/role, operations, profile, bookings, maintenance, and empty states
- adding SystemAdmin tenant-context switching for MVC cookies and API JWT sessions
- adding a React SystemAdmin tenant picker and translated shell/login labels
- enriching seeded demo data with members, staff, sessions, bookings, memberships, payments, opening hours, equipment, and maintenance tasks
- adding a regression test for SystemAdmin switching into `north-star`

What needed manual review or correction:
- existing Estonian strings render differently depending on terminal encoding, so localized files need browser/test verification rather than console-only inspection
- the React console still exposes developer-style API action titles in English, but the shell, login, and main workflow pages now have Estonian labels for normal demo use

Alternatives considered:
- making persistent `AppUserGymRole` rows for SystemAdmin in every tenant, but transient tenant claims keep platform ownership explicit and avoid seeding access rows for future gyms
- building every CRUD page for every entity, but the existing function console already exposes the full API surface and the focused pages cover the core real-life workflows

## 2026-04-21 - SaaS Console, Localization, and Favicon Parity

Task:
- make Assignment 03 language switching work across MVC and the React API client
- expose the gym SaaS functions through the React client similarly to the dental clinic demo console
- add browser-tab logo/favicon branding

Files affected:
- `client/index.html`
- `client/public/gym-logo.svg`
- `client/src/App.tsx`
- `client/src/components/AppShell.tsx`
- `client/src/lib/*`
- `client/src/pages/LoginPage.tsx`
- `client/src/pages/SaasConsolePage.tsx`
- `client/src/styles.css`
- `src/App.DAL.EF/Seeding/AppDataInit.cs`
- `src/App.Resources/SharedResources*.resx`
- `src/WebApp/Controllers/HomeController.cs`
- `src/WebApp/Setup/ServiceCollectionExtensions.cs`
- `src/WebApp/Views/Shared/_Layout.cshtml`
- `src/WebApp/wwwroot/assets/gym-logo.svg`
- `tests/WebApp.Tests/Integration/AuthSecurityAndErrorTests.cs`
- `README.md`
- `docs/*.md`

What AI helped with:
- allowing system-role sessions in the React client
- adding a language provider, selector, and `Accept-Language` API header
- adding a broad React SaaS console for platform, billing, support, onboarding, account, and tenant actions
- validating MVC culture values before writing the culture cookie
- correcting seeded Estonian `LangStr` values and adding localization regression tests
- wiring SVG favicon/logo assets into MVC and React

What needed manual review or correction:
- the existing worktree already had unrelated JavaScript Assignment 04 changes, so this pass avoided touching or reverting them
- the console keeps destructive actions available but parameterized; reviewers should run safe GET actions first during demos

Alternatives considered:
- copying the dental clinic static UI directly, but expanding the existing React API client kept Assignment 03's separate-client architecture intact
- leaving platform flows in MVC only, but system-role React access is closer to the dental clinic SaaS demo behavior

## 2026-04-21 - CWEB A4 Proxy Deployment Alignment

Task:
- align Assignment 03 deployment documentation and production Compose defaults with the `cweb-a4` proxy route
- keep adjacent JavaScript Assignment 04 port documentation from conflicting with the same VPS port

Files affected:
- `README.md`
- `docker-compose.prod.yml`
- `docs/deployment.md`
- `docs/a3-saas-plan.md`
- `docs/architecture.md`
- root `README.md`
- root `docs/ci-cd.md`
- JavaScript Assignment 04 deployment docs and port defaults

What AI helped with:
- mapping `https://mtiker-cweb-4.proxy.itcollege.ee` to VPS port `83`
- setting Assignment 03 production Compose to expose host port `83`
- adding the production CORS origin default for the public proxy route
- updating repository-level deployment documentation to reserve JavaScript Vue for port `84`

What needed manual review or correction:
- the first assumption used the older `cweb-a3` route; the user corrected the target to `cweb-a4`
- the proxy table showed that JavaScript Vue uses port `84`, so the adjacent JavaScript A04 defaults had to be corrected as part of the same deployment alignment

Alternatives considered:
- reusing the existing `cweb-a3` route, but that remains documented for the older Assignment 18 deployment
- leaving JavaScript A04 defaults on port `83`, but that would conflict with the active `cweb-a4` route

## 2026-04-21 - A3 Readiness Gap Closure

Task:
- implement the readiness fixes from the assignment review
- package the React client into production at `/client`
- move proposal-critical workflows behind BLL services
- add missing proposal happy paths and null-safety regression coverage

Files affected:
- `Dockerfile`
- `client/*`
- `src/App.BLL/*`
- `src/App.DAL.EF/AppDbContext.cs`
- `src/WebApp/ApiControllers/Tenant/*`
- `src/WebApp/Areas/Client/*`
- `src/WebApp/Models/*`
- `src/WebApp/Setup/*`
- `tests/WebApp.Tests/Integration/ProposalWorkflowTests.cs`
- `README.md`
- `docs/*.md`

What AI helped with:
- adding the Node client build stage and `/client` ASP.NET Core fallback
- switching BLL services to an `IAppDbContext` abstraction and refactoring tenant API controllers to service calls
- adding MVC session booking/cancellation, trainer attendance, caretaker task status, and opening-hours views
- adding React sessions/detail/booking workflow and API-base tests
- replacing nullable session description projections with null-safe translation
- adding backend integration tests for `/client`, nullable descriptions, paid booking validation, trainer assignment checks, and caretaker assignment checks

What needed manual review or correction:
- bare `/client` initially fell through to MVC and needed an exact static endpoint
- Vitest setup had to stay in `vitest.config.ts` so `vite.config.ts` still type-checked for production builds
- the architecture cleanup was intentionally pragmatic; broad MVC/admin read composition and the staff API slice still use direct `AppDbContext`

Alternatives considered:
- deploying the React client as a separate container, but serving it from the same ASP.NET Core host is simpler and still proves a real built API client
- doing a full repository/unit-of-work rewrite, but a narrow `IAppDbContext` boundary is safer for defense timing
- moving all role-specific workflows into React immediately, but MVC already covers trainer and caretaker happy paths with less risk

## 2026-04-21 - React Role Screens and NuGet Audit Fix

Task:
- add React trainer/caretaker screens
- remove the `System.Security.Cryptography.Xml` NU1903 vulnerability warnings from build/test output

Files affected:
- `client/src/App.tsx`
- `client/src/components/AppShell.tsx`
- `client/src/lib/apiClient.ts`
- `client/src/lib/auth.tsx`
- `client/src/lib/types.ts`
- `client/src/pages/AttendancePage.tsx`
- `client/src/pages/MaintenanceTasksPage.tsx`
- `client/src/pages/OperationsPages.test.tsx`
- `client/src/styles.css`
- `tests/WebApp.Tests/WebApp.Tests.csproj`
- `README.md`
- `docs/*.md`

What AI helped with:
- extending client role admission to `Trainer` and `Caretaker`
- adding role-aware navigation for Attendance and Maintenance
- implementing attendance updates through `/bookings/{id}/attendance`
- implementing caretaker task status updates through `/maintenance-tasks/{id}/status`
- adding Vitest coverage for both role screens
- pinning `System.Security.Cryptography.Xml` to 10.0.6 in the test project to override the vulnerable transitive 10.0.2 resolution

What needed manual review or correction:
- NuGet audit had to be checked after the package pin to confirm the warning disappeared instead of being suppressed
- the React role pages were kept focused on proposal-critical updates rather than broad staff/equipment CRUD

Sources checked:
- NuGet Gallery for `System.Security.Cryptography.Xml` 10.0.6
- GitHub advisories `GHSA-37gx-xxp4-5rgx` and `GHSA-w3x6-4m5h-cxqf`

## 2026-04-09 - Assignment 03 Full SaaS Buildout

Task:
- implement a new A3 project as a full SaaS multi-gym management system
- keep Assignment 18 untouched
- finish the implementation until it builds, tests, documents, and fits the agreed plan

Files affected:
- solution and project scaffolding under `src/`, `tests/`, `scripts/`, and assignment root infra files
- domain, DAL, BLL, DTO, MVC, API, resources, tests, Docker, CI, and docs

What AI helped with:
- scaffolding the new A3 solution and project structure
- modeling the platform and tenant entities
- implementing `AppDbContext`, tenant filtering, localization, Swagger, auth, API controllers, and MVC areas
- adding seed/demo data and the multi-gym SaaS context-switching flow
- fixing `LangStr` EF/test mapping issues
- fixing the gym-user upsert bug
- adding migrations
- rewriting assignment docs and CI/deploy files to the correct A3 project

What needed manual review or correction:
- several copied files still referenced the old dental-clinic assignment and had to be renamed or rewritten
- the first `LangStr` implementation caused EF/InMemory test failures and required redesign into a clearer value object
- CI, Docker, and README files needed a final correctness pass to remove old project names and paths
- the MVC layer initially covered only a shell and had to be extended with workspace switching and additional pages

Alternatives considered:
- reusing Assignment 18 directly as the A3 deliverable
- keeping a lighter single-tenant gym app without the SaaS platform layer
- postponing migrations and relying on runtime schema creation

Final decision:
- keep A3 as a separate assignment folder
- implement full SaaS gym tenancy on top of the proposal domain
- keep one ASP.NET Core app for MVC + API to stay course-aligned and easier to defend

## 2026-04-16 - Separate Client, Error Handling, Security Tests, and CI Sync

Task:
- implement the A3 correction pass without changing deployment
- add a separate React client that consumes the existing REST API
- fix production HTML vs API error handling
- extend security-focused automated coverage
- bring README, architecture, API, testing, CI, and AI logs back into sync

Files affected:
- `client/*`
- `src/App.DTO/v1/Tenant/TenantDtos.cs`
- `src/WebApp/ApiControllers/Tenant/MembersController.cs`
- `src/WebApp/Controllers/HomeController.cs`
- `src/WebApp/Middleware/ProblemDetailsMiddleware.cs`
- `src/WebApp/Setup/ServiceCollectionExtensions.cs`
- `src/WebApp/Setup/ApplicationBuilderExtensions.cs`
- `src/WebApp/Views/Home/Error.cshtml`
- `src/WebApp/appsettings.json`
- `src/App.Resources/SharedResources*.resx`
- `tests/WebApp.Tests/Integration/AuthSecurityAndErrorTests.cs`
- `.gitlab-ci.yml`
- `README.md`
- `docs/architecture.md`
- `docs/api.md`
- `docs/testing.md`
- `docs/a3-saas-plan.md`
- `docs/ai-usage.md`
- `docs/ai-prompts.md`

What AI helped with:
- scaffolding the separate React + TypeScript client
- adding auth state, refresh-token retry logic, and CRUD pages for members, training categories, and membership packages
- adding backend CORS configuration for the local client
- extending the member API with a detail DTO for edit flows
- fixing production error handling so HTML requests render `/Home/Error` while API requests return `ProblemDetails`
- adding frontend Vitest coverage plus backend integration tests for refresh rotation, cross-gym denial, member self-only denial, system-route denial, and error-shape behavior
- updating assignment CI so the client is built and tested before the .NET stages

What needed manual review or correction:
- the frontend build initially needed a split Vite/Vitest config to avoid TypeScript config collisions
- the backend `ProblemDetails` response had to be written explicitly so the content type stayed `application/problem+json`
- the HTML error test needed a production-style test host because MVC exception handling is disabled in development
- the `/Home/Error` route had to allow exception-handler re-execution of failed `POST` requests

Alternatives considered:
- treating the existing MVC shell as sufficient client coverage
- adding a second backend or changing the deployed runtime shape during the same pass
- broadening the React client to all tenant entities immediately

Final decision:
- keep the current ASP.NET Core monolith as the backend
- add a focused separate React client for the minimum required API-consumer coverage
- keep deployment status explicitly pending while still finishing code, tests, CI, and documentation for the correction pass

## 2026-04-22 - A3 SaaS Plan Completion

Task:
- implement the remaining local `a3-saas-plan.md` gap after validating the current backend and React client state
- make the React shell support assigned multi-gym tenant and role switching for non-system users

Files affected:
- `src/App.DTO/v1/Identity/JwtResponse.cs`
- `src/App.BLL/Services/IdentityService.cs`
- `client/src/components/AppShell.tsx`
- `client/src/lib/types.ts`
- `client/src/lib/language.tsx`
- `client/src/App.test.tsx`
- `client/src/test/testUtils.tsx`
- `tests/WebApp.Tests/Integration/SmokeTests.cs`
- `README.md`
- `docs/a3-saas-plan.md`
- `docs/api.md`
- `docs/architecture.md`
- `docs/testing.md`
- `docs/ai-usage.md`

What AI helped with:
- added `availableTenants` to auth responses as a backward-compatible session contract
- populated assigned active gym memberships and roles from `AppUserGymRole`
- replaced the SystemAdmin-only React shell picker with a shell tenant/role switcher that also works for assigned multi-gym users
- added backend and frontend regression coverage for the new session metadata and shell switching behavior

What needed manual review or correction:
- verified that the new auth response metadata does not replace backend authorization; tenant access still depends on JWT claims and BLL/controller checks
- kept separate public client hosting out of scope because the documented deployment model intentionally serves the built Vite client from the ASP.NET Core host at `/client`

Alternatives considered:
- leaving non-system users to switch through the function console only
- introducing a separate tenant directory endpoint, but the session contract already has the required user-specific assignment context

Final decision:
- expose assigned tenant/role choices in the login/refresh/switch responses
- use the React shell as the primary workspace switcher
