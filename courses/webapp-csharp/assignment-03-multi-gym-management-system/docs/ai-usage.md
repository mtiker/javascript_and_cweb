# AI Usage Log

## 2026-04-30 - Phase 14 Final1 Maintenance/Admin Clean Slice

Task:
- finish Final1 migration for maintenance/facilities/platform admin and complete Admin UX requirements without adding new SaaS features

Files affected:
- `src/App.BLL/Contracts/Persistence/IMaintenanceRepository.cs`
- `src/App.BLL/Mapping/{IMaintenanceMapper.cs,MaintenanceMapper.cs}`
- `src/App.BLL/Services/MaintenanceWorkflowService.cs`
- `src/App.DAL.EF/Repositories/{EfAppUnitOfWork.cs,EfMaintenanceRepository.cs}`
- `src/App.DAL.EF/PersistenceServiceExtensions.cs`
- `src/WebApp/Areas/Admin/Controllers/{DashboardController.cs,GymsController.cs,OperationsController.cs,SessionsController.cs}`
- `src/WebApp/Areas/Admin/Services/AdminViewModelServices.cs`
- `src/WebApp/Setup/ServiceExtensions.cs`
- `tests/WebApp.Tests/Architecture/ArchitectureTests.cs`
- `tests/WebApp.Tests/Integration/{AuthSecurityAndErrorTests.cs,MvcComplianceTests.cs}`
- `tests/WebApp.Tests/Unit/MaintenanceWorkflowServiceTests.cs`
- `docs/{final1-maintenance-admin-slice-plan.md,maintenance-rules-audit.md,admin-ux-completion-audit.md,platform-role-audit.md}`

What AI helped with:
- added tests first for caretaker assigned/unassigned maintenance updates, due-task generation, assignment history, equipment downtime/status transitions, platform-role access, Admin view-model rendering, and no Admin dynamic data bags
- migrated maintenance/facilities persistence to `IMaintenanceRepository` behind `IAppUnitOfWork.Maintenance`
- moved maintenance/facility DTO projection into `MaintenanceMapper`
- moved Admin dashboard/gyms/sessions/operations page composition out of controllers and into view-model services
- documented maintenance rules, Admin UX completion, platform role boundaries, and the Final1 slice plan

What needed manual review or correction:
- the first Admin controller compliance assertion still expected controllers to mention concrete view-model class names; after moving composition to services, the assertion was corrected to require typed `View(...)` rendering and keep view-model verification in Razor source tests

Alternatives considered:
- adding MVC Admin write forms, but that would duplicate the REST/React mutation surface and was outside the clean-slice goal
- fully migrating platform service persistence, but this slice preserved the existing platform service boundary and focused the requested repository migration on maintenance/facilities

## 2026-04-28 - Phase 7 MVC Admin and MVC Client Compliance Pass

Task:
- make Assignment 03 MVC UX defendable by proving Admin/Client role access, replacing Admin React redirects with small functional MVC pages, and documenting view-model/no-ViewBag compliance

Files affected:
- `src/WebApp/Areas/Admin/Controllers/GymsController.cs`
- `src/WebApp/Areas/Admin/Controllers/MembershipsController.cs`
- `src/WebApp/Areas/Admin/Controllers/SessionsController.cs`
- `src/WebApp/Areas/Admin/Controllers/OperationsController.cs`
- `src/App.BLL/Services/IdentityService.cs`
- `src/WebApp/Areas/Admin/Views/Gyms/Index.cshtml`
- `src/WebApp/Models/AdminGymsPageViewModel.cs`
- `tests/WebApp.Tests/Integration/MvcComplianceTests.cs`
- `tests/WebApp.Tests/Integration/SmokeTests.cs`
- `docs/mvc-admin-audit.md`
- `docs/mvc-client-audit.md`
- `docs/viewmodel-audit.md`
- `docs/no-viewbag-viewdata-audit.md`
- related README/testing/route-inventory documentation

What AI helped with:
- added tests first for anonymous/wrong-role Admin denial, `GymAdmin`/`GymOwner` tenant Admin access, MVC Client routes for member/trainer/caretaker, Admin no-`ViewBag`/`ViewData`, Admin anti-forgery guardrails, and Admin view-model usage
- replaced tenant Admin React redirects with read-only Razor summary pages for memberships, sessions, and operations
- replaced `/Admin/Gyms` direct domain-entity view usage with `AdminGymsPageViewModel`
- documented why React remains the write-heavy API client while MVC Admin provides Razor evidence

What needed manual review or correction:
- the first helper version treated login `302` as a failure when redirects were disabled; the helper was corrected to accept redirect as successful cookie sign-in
- older smoke tests still asserted Admin-to-React redirects and were updated to assert MVC rendering
- the full backend suite also exposed an existing malformed-JWT refresh-token error-shaping regression; `IdentityService` now converts malformed JWT parser exceptions into the documented validation response

Alternatives considered:
- keeping Admin redirects and documenting them as acceptable, but the assignment specifically needs MVC Admin evidence, so small functional MVC pages are stronger and easier to defend
- duplicating full write forms in MVC Admin, but this would duplicate API/React mutation flows and increase regression risk

## 2026-04-28 - Phase 6 Membership Packages CRUD Vertical Slice

Task:
- complete the third required separate-client CRUD entity by hardening membership package CRUD, validation, soft-delete behavior, tenant isolation, React page states, and package-specific documentation

Files affected:
- `src/App.BLL/Services/MembershipPackageService.cs`
- `src/App.DTO/v1/MembershipPackages/MembershipPackageUpsertRequest.cs`
- `tests/WebApp.Tests/Integration/MembershipPackageCrudTests.cs`
- `client/src/pages/MembershipPackagesPage.tsx`
- `client/src/pages/CrudPages.test.tsx`
- `docs/membership-package-audit.md`
- `docs/membership-package-contract.md`
- `docs/package-validation-rules.md`
- `README.md`
- `docs/a3-saas-plan.md`
- `docs/ai-usage.md`
- root `docs/ai-prompts.md`

What AI helped with:
- added package-first backend integration tests for list, create `201`, invalid price/duration/currency `ProblemDetails`, update, unused soft delete, used-package safe soft delete, and wrong-gym ID manipulation
- added React package page tests for loading, create, update, delete, local validation, and API validation errors
- added BLL package validation/normalization and explicit `GymId` predicates for update/delete
- documented package API contract, validation rules, and soft-delete behavior without adding external payment logic or changing finance

What needed manual review or correction:
- the first wrong-gym package test exposed the same test-host risk as earlier slices: EF filters are disabled in integration tests, so package mutations must scope by active `GymId` directly
- omitted currency initially hit ASP.NET Core model validation before BLL validation; the DTO boundary was made nullable so the application layer owns the package validation message
- the React validation test showed native number constraints could block the custom validation banner; the package form now uses its explicit validation path

Alternatives considered:
- returning `409 Conflict` for deleting used packages was initially deferred in Phase 6, but Phase 13 adopts it to make package lifecycle rules clearer while retaining membership price/currency snapshots
- expanding package localization editing, but that is separate from the CRUD and validation slice

## 2026-04-28 - Phase 4 Member CRUD Vertical Slice

Task:
- make member CRUD defensible through REST API, MVC Admin, React client, and tests without touching unrelated entities

Files affected:
- `src/App.BLL/Services/MemberWorkflowService.cs`
- `src/WebApp/ApiControllers/Tenant/MembersController.cs`
- `src/WebApp/Areas/Admin/Controllers/MembersController.cs`
- `src/WebApp/Areas/Admin/Views/Members/Index.cshtml`
- `src/WebApp/Areas/Admin/Views/Dashboard/Index.cshtml`
- `src/WebApp/Models/AdminMembersPageViewModel.cs`
- `tests/WebApp.Tests/Integration/MemberCrudTests.cs`
- `tests/WebApp.Tests/Integration/AdminMembersPageTests.cs`
- `tests/WebApp.Tests/Unit/TenantControllerTests.cs`
- `client/src/pages/CrudPages.test.tsx`
- `docs/member-crud-audit.md`
- `docs/member-contract.md`
- `docs/member-tests-map.md`
- `README.md`
- `docs/a3-saas-plan.md`
- `docs/ai-usage.md`
- root `docs/ai-prompts.md`

What AI helped with:
- added member API integration coverage for list, detail, create `201`, update `200`, delete `204`, soft-delete evidence, duplicate `ProblemDetails`, and cross-gym ID manipulation
- added a strongly typed MVC Admin Members directory with no `ViewBag` or `ViewData`
- added React Members page tests for loading, validation-before-submit, and delete success
- aligned member create `Location` with the documented `/api/v1/...` route shape
- hardened member detail/update/delete service lookups with explicit `GymId` predicates
- documented the member contract, test map, and soft-delete semantics

What needed manual review or correction:
- the first cross-gym member update test exposed that the test host disables EF tenant filters; the service was corrected to enforce `GymId` explicitly rather than relying on query filters
- initial member docs incorrectly described delete as hard delete; the implemented and verified behavior is soft delete through `TenantBaseEntity`

Alternatives considered:
- duplicating full member write forms in MVC Admin, but that would split workflow behavior; MVC Admin now provides a read-only directory while REST + React remain the mutation surfaces
- changing public member routes broadly, but only the create `Location` route value was aligned to existing `/api/v1/...` examples

## 2026-04-28 - Phase 5 Training Category Localization Slice

Task:
- prove database translations through `LangStr` and UI translations through `.resx` for the training-category/localization vertical slice

Files affected:
- `src/App.BLL/Services/TrainingWorkflowService.cs`
- `src/App.DTO/v1/TrainingCategories/TrainingCategoryUpsertRequest.cs`
- `tests/WebApp.Tests/Integration/TrainingCategoryLocalizationTests.cs`
- `client/src/pages/CrudPages.test.tsx`
- `docs/training-category-audit.md`
- `docs/localization-audit.md`
- `docs/langstr-contract.md`
- `README.md`
- `docs/a3-saas-plan.md`
- `docs/ai-usage.md`
- root `docs/ai-prompts.md`

What AI helped with:
- added tests for training-category CRUD, `Accept-Language` `en`/`et`/`et-EE`, missing translation fallback, MVC `.resx` label rendering, React language header behavior, and validation `ProblemDetails`
- added minimal training-category validation and tenant-scoped category update/delete lookups
- documented the current localization boundaries and `LangStr` read/write contract

What needed manual review or correction:
- the first validation regression exposed that whitespace names were accepted; the fix used the existing BLL validation exception path instead of changing the localization system

Alternatives considered:
- adding a multi-culture category edit DTO, but that would broaden the public API beyond this vertical slice
- moving to a new architecture layer, but the current Assignment 03 structure already supports this slice

## 2026-04-24 - Restore Functional SaaS Admin Routing After Mirror Rollback

Task:
- restore functional SaaS operations in Assignment 03 after rollback left Admin routes on read-only MVC summaries

Files affected:
- `src/WebApp/Helpers/ClientAppUrlResolver.cs`
- `src/WebApp/Views/Shared/_Layout.cshtml`
- `src/WebApp/Areas/Admin/Views/Dashboard/Index.cshtml`
- `src/WebApp/Areas/Admin/Controllers/{GymsController.cs,MembershipsController.cs,SessionsController.cs,OperationsController.cs}`
- `tests/WebApp.Tests/Integration/SmokeTests.cs`
- `README.md`
- `docs/a3-saas-plan.md`
- `docs/ai-usage.md`

What AI helped with:
- added a shared client-route resolver and rewired Admin quick links/layout to `/client` SaaS routes
- changed read-only Admin resource controllers to role-gated redirects into functional React workspace routes
- added smoke tests proving quick-link presence and redirect behavior for system and tenant admins
- synchronized assignment README and SaaS plan docs with the new admin handoff behavior

What needed manual review or correction:
- ensured role gates stayed intact while redirecting to React routes so non-admin users are still denied/redirected safely

Alternatives considered:
- reintroducing the old MVC SaaS mirror controller, but it was intentionally not brought back; route handoff to the real client is simpler and less error-prone

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

## 2026-04-28 - Final1 Auth Clean Slice

Task:
- move Account login, refresh-token rotation/reuse rejection, and logout invalidation behind Clean/Onion-style service, repository, Unit of Work, and mapper boundaries without changing public endpoint paths or DTOs

Files affected:
- `src/App.BLL/Services/IAccountAuthService.cs`
- `src/App.BLL/Services/AccountAuthService.cs`
- `src/App.BLL/Services/IIdentityService.cs`
- `src/App.BLL/Services/IdentityService.cs`
- `src/App.BLL/Contracts/Persistence/IRefreshTokenRepository.cs`
- `src/App.BLL/Contracts/Persistence/IAppUnitOfWork.cs`
- `src/App.BLL/Mapping/IAuthResponseMapper.cs`
- `src/App.BLL/Mapping/AuthResponseMapper.cs`
- `src/App.DAL.EF/Repositories/EfRefreshTokenRepository.cs`
- `src/App.DAL.EF/Repositories/EfAppUnitOfWork.cs`
- `src/App.DAL.EF/PersistenceServiceExtensions.cs`
- `src/WebApp/ApiControllers/Identity/AccountController.cs`
- `src/WebApp/Setup/ServiceExtensions.cs`
- `tests/WebApp.Tests`
- `docs/final1-auth-slice-plan.md`
- `docs/auth-service-boundary-audit.md`
- `docs/refresh-token-repository-contract.md`
- `docs/architecture.md`
- `docs/a3-saas-plan.md`

What AI helped with:
- extracted session use cases into `IAccountAuthService`
- added a refresh-token repository contract and EF implementation
- exposed refresh-token persistence through the Unit of Work
- moved auth response DTO projection into `AuthResponseMapper`
- updated controller tests, API route/DTO contract tests, architecture boundary tests, and documentation evidence

What needed manual review or correction:
- verified that register, switch-gym, switch-role, password reset, and unrelated profile/user features were not migrated in this slice
- kept public `/api/v1/account/*` paths and DTOs unchanged

Alternatives considered:
- leaving login/refresh/logout in the broader `IdentityService`, but splitting session behavior gives cleaner Final1 defense evidence while keeping unrelated identity features stable

## 2026-04-30 - Phase 12 Final1 Training Clean Slice

Task:
- migrate training categories, training sessions, bookings, and trainer attendance into the Clean/Onion repository + Unit of Work + mapper pattern while preserving public API contracts

Files affected:
- `src/App.BLL/Contracts/Persistence/ITrainingCategoryRepository.cs`
- `src/App.BLL/Contracts/Persistence/ITrainingSessionRepository.cs`
- `src/App.BLL/Contracts/Persistence/IBookingRepository.cs`
- `src/App.BLL/Contracts/Persistence/IWorkShiftRepository.cs`
- `src/App.BLL/Contracts/Persistence/IAppUnitOfWork.cs`
- `src/App.BLL/Mapping/ITrainingMapper.cs`
- `src/App.BLL/Mapping/TrainingMapper.cs`
- `src/App.BLL/Services/TrainingWorkflowService.cs`
- `src/App.DAL.EF/Repositories/EfTrainingCategoryRepository.cs`
- `src/App.DAL.EF/Repositories/EfTrainingSessionRepository.cs`
- `src/App.DAL.EF/Repositories/EfBookingRepository.cs`
- `src/App.DAL.EF/Repositories/EfWorkShiftRepository.cs`
- `src/App.DAL.EF/Repositories/EfAppUnitOfWork.cs`
- `src/App.DAL.EF/PersistenceServiceExtensions.cs`
- `src/WebApp/Setup/ServiceExtensions.cs`
- `tests/WebApp.Tests/Architecture/ArchitectureTests.cs`
- `docs/final1-training-slice-plan.md`
- `docs/training-repository-contract.md`
- `docs/booking-rules-audit.md`
- `docs/trainer-authorization-audit.md`

What AI helped with:
- added dedicated BLL persistence contracts and EF repositories for the training slice
- moved training response projection into `TrainingMapper`
- refactored `TrainingWorkflowService` away from direct `IAppDbContext` usage
- added architecture regression coverage for the training repository/mapper boundary
- aligned Phase 12 repository, booking-rule, trainer-authorization, and slice-plan documentation with the implementation

What needed manual review or correction:
- verified the public tenant API controllers remained thin and retained their existing route/DTO shapes
- kept membership finance migration out of scope; booking payment creation still uses the existing `Payment` entity path via the generic Unit of Work repository

Alternatives considered:
- using only the generic repository for all training entities, but dedicated repositories give clearer tenant-scoped query contracts for booking and trainer-assignment rules

## 2026-04-30 - Phase 13 Final1 Membership And Finance Clean Slice

Task:
- migrate membership packages, memberships, payments, invoices, refunds, and finance workspace into the Clean/Onion repository + Unit of Work + mapper pattern without adding an external payment provider

Files affected:
- `src/App.BLL/Contracts/Persistence/IMembershipPackageRepository.cs`
- `src/App.BLL/Contracts/Persistence/IMembershipRepository.cs`
- `src/App.BLL/Contracts/Persistence/IPaymentRepository.cs`
- `src/App.BLL/Contracts/Persistence/IFinanceRepository.cs`
- `src/App.BLL/Contracts/Persistence/IAppUnitOfWork.cs`
- `src/App.BLL/Mapping/IMembershipFinanceMapper.cs`
- `src/App.BLL/Mapping/MembershipFinanceMapper.cs`
- `src/App.BLL/Services/MembershipPackageService.cs`
- `src/App.BLL/Services/MembershipService.cs`
- `src/App.BLL/Services/PaymentService.cs`
- `src/App.BLL/Services/FinanceWorkspaceService.cs`
- `src/App.DAL.EF/Repositories/EfMembershipPackageRepository.cs`
- `src/App.DAL.EF/Repositories/EfMembershipRepository.cs`
- `src/App.DAL.EF/Repositories/EfPaymentRepository.cs`
- `src/App.DAL.EF/Repositories/EfFinanceRepository.cs`
- `src/App.DAL.EF/Repositories/EfAppUnitOfWork.cs`
- `src/WebApp/Areas/Admin/Controllers/MembershipsController.cs`
- `tests/WebApp.Tests`
- `docs/final1-membership-finance-slice-plan.md`
- `docs/membership-repository-contract.md`
- `docs/finance-rules-audit.md`
- `docs/membership-status-transition-audit.md`

What AI helped with:
- added dedicated repository contracts and EF implementations for membership and finance persistence
- exposed the repositories through Unit of Work and DI
- moved membership/finance DTO projection into `MembershipFinanceMapper`
- refactored services away from direct `IAppDbContext` usage
- added conflict handling for deleting packages already used by memberships
- updated architecture, API, package, testing, and Final1 slice documentation

What needed manual review or correction:
- verified public API routes and DTO shapes stayed stable
- kept external payment provider integration out of scope
- updated the earlier package delete documentation because used-package delete now returns `409 Conflict`

Alternatives considered:
- continuing to soft-delete used packages, but returning a conflict is easier to defend because package lifecycle and historical membership references stay explicit until a separate deactivate endpoint exists

## 2026-04-30 - Phase 15 Final1 Defense Pack

Task:
- prepare Final1 mandatory coverage and defense evidence without adding features or starting modular monolith work

Files affected:
- `docs/final1-defense.md`
- `docs/final1-coverage-audit.md`
- `docs/final1-test-traceability.md`
- `docs/final1-architecture-diagram.md`
- `tests/WebApp.Tests/Integration/Final1CriticalE2ETests.cs`
- `docs/testing.md`
- `docs/current-test-inventory.md`
- `README.md`

What AI helped with:
- audited the existing Clean/Onion, repository/UOW/service/mapper, Admin UX, DTO/API, auth, IDOR, i18n, React CRUD, and CI evidence
- added an explicit Final1 critical API-level E2E integration test class for login, member CRUD, training category CRUD, membership package CRUD, and IDOR negative coverage
- created the requested defense, coverage, traceability, and architecture diagram documents
- linked the new evidence from the assignment README and testing docs

What needed manual review or correction:
- the project does not currently include Playwright, so the defense documents state that Final1 E2E evidence is HTTP/API-level integration coverage through `WebApplicationFactory` plus React Vitest coverage

Alternatives considered:
- adding a new browser E2E dependency, but the existing backend integration and frontend Vitest infrastructure provided the missing evidence with less risk and no feature changes
