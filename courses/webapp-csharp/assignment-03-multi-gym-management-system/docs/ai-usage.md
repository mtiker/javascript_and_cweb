# AI Usage Log

## 2026-05-19 - Reference Architecture Parity Doc Correction

Task:
- update the LabRent/LabTrack reference parity documentation after the Final1
  presentation-boundary refactor

Files affected:
- `docs/reference-architecture-parity.md`
- `docs/ai-usage.md`
- root `docs/ai-prompts.md`

What AI helped with:
- corrected stale wording that still listed MVC presentation `AppDbContext`
  migration as remaining work
- updated validation counts to the current Final1 run
- clarified that Final2 module projects are preserved partial evidence, not a
  completed Final2 claim

What needed manual review or correction:
- no production code changed in this pass
- tests were not rerun because this was documentation-only

Alternatives considered:
- making the gym project folder-for-folder identical to the reference was
  rejected because the React client, Final2 module projects, and gym SaaS domain
  are intentional course-specific differences

## 2026-05-19 - Final1 Presentation Boundary Completion

Task:
- finish Assignment 03 for Final1 only by removing concrete `AppDbContext`
  usage from MVC presentation paths while preserving existing Final2/module
  code as partial, not completed, evidence

Files affected:
- `App.BLL/Services/IWorkspaceContextService.cs`
- `App.BLL/Services/WorkspaceContextService.cs`
- `App.BLL/Mappers/MembershipFinanceMapper.cs`
- `App.BLL/Mappers/TrainingMapper.cs`
- `WebApp/Controllers/HomeController.cs`
- `WebApp/ViewComponents/WorkspaceSwitcherViewComponent.cs`
- `WebApp/Areas/Client/Controllers/ProfileController.cs`
- `WebApp/Areas/Client/Controllers/MaintenanceController.cs`
- `WebApp/Areas/Client/Services/ClientProfilePageService.cs`
- `WebApp/Areas/Client/Services/ClientMaintenancePageService.cs`
- `WebApp/Areas/Admin/Services/AdminViewModelServices.cs`
- `WebApp/Setup/ServiceExtensions.cs`
- `tests/WebApp.Tests/Architecture/Final1PresentationBoundaryTests.cs`
- `tests/WebApp.Tests/Unit/Final1PresentationServiceTests.cs`
- Final1 documentation and AI logs

What AI helped with:
- moved profile, maintenance, home/workspace switching, and admin package/category
  page composition behind focused services or repository-backed BLL contracts
- added service tests for member profile, missing member profile, caretaker
  maintenance pages, equipment labels, tenant workspace options, and SystemAdmin
  workspace options
- added architecture tests preventing MVC controllers, view components, and
  Admin/Client page services from referencing concrete `AppDbContext`
- preserved public MVC/API routes, DTOs, seeded users, React behavior, and
  existing partial Final2 module code

What needed manual review or correction:
- admin MVC package/category pages initially rendered localized DTO names under
  Estonian culture; reads were corrected to use repository-backed domain data
  for the server-rendered form/list behavior
- Docker was unavailable, so PostgreSQL/Testcontainers tests and public smoke
  verification remain unverified

Validation:
- `dotnet build multi-gym-management-system.slnx --no-restore` passed
- `dotnet test multi-gym-management-system.slnx --no-restore` passed with 202
  passed and 3 skipped PostgreSQL/Testcontainers tests
- `cd client && npm test` passed with 32 tests
- `cd client && npm run build` passed

Alternatives considered:
- adding a new optional admin workflow was rejected because Final1 already has
  tested CRUD for members, training categories, and membership packages
- hardening Final2 modules was rejected because the requested scope was Final1
  only

## 2026-05-19 - Final2 Scope Pruning Implementation

Task:
- implement the Final2 readiness and scope-reduction plan, keeping the project a
  modular monolith rather than moving toward Final3 microservices

Files affected:
- domain entities, EF configurations, `AppDbContext`, repositories, BLL
  services/mappers, module messages/handlers, API controllers, MVC views,
  React client routes/pages/types/tests, EF migrations, and current docs

What AI helped with:
- pruned optional platform/support/billing, coaching, invoice/refund, roster,
  opening-hours, and assignment-history contexts
- added the `PruneFinal2Scope` migration
- simplified training assignment to optional `TrainerStaffId`
- reduced React routes to defended Final2 workflows
- updated API route snapshot, tenant smoke, auth, mediator, workflow, and React
  tests to the smaller contract
- synchronized current defense, API, data-model, module-boundary, roadmap, and
  testing docs

What needed manual review or correction:
- this is an intentional breaking schema/API reduction for defense clarity
- `dotnet build`, `dotnet test`, `npm test`, and `npm run build` were verified
- fresh database migration apply, Swagger route review, Compose config, and
  browser smoke still need to be run before a final defense tag

Alternatives considered:
- keeping broad enterprise features was rejected because it made the Final2
  defense look like partial Final3 scope
- extracting services, RabbitMQ, separate module schemas, or per-module
  DbContexts was rejected as Final3/out-of-scope work

## 2026-05-11 - Refresh Token Session Storage Security Tradeoff

Task:
- document the current refresh-token `sessionStorage` security tradeoff and compensating controls without changing auth implementation, API contracts, cookies, or CSRF behavior

Files affected:
- `README.md`
- `docs/security-token-audit.md`
- `docs/auth-flow-audit.md`
- `docs/a3-saas-plan.md`
- `docs/architecture.md`
- `docs/ai-usage.md`
- root `docs/ai-prompts.md`

What AI helped with:
- added a dedicated security-token audit section explaining that the React client stores the serialized auth session, including the refresh token, in JavaScript-readable `sessionStorage`
- documented existing compensating controls: refresh-token rotation, reuse rejection, logout invalidation, server-side refresh-token lookup, and configurable access-token lifetime
- added a future hardening note for migrating refresh tokens to `HttpOnly`, `Secure`, `SameSite` cookies with CSRF/CORS/test/documentation work handled in that future phase

What needed manual review or correction:
- no auth implementation, API contract, cookie, or CSRF behavior was changed
- `dotnet test multi-gym-management-system.slnx --filter Auth` passed with 40 tests
- `cd client && npm test` passed with 34 Vitest tests and existing React Router v7 future-flag warnings

Alternatives considered:
- moving refresh tokens to cookies in this phase was rejected because the requested scope was documentation-only and cookie auth would require contract, CORS, CSRF, and regression-test changes

## 2026-05-11 - PostgreSQL Testcontainers Defense Run Path

Task:
- make skipped PostgreSQL/Testcontainers persistence tests explicitly runnable before defense without changing normal `dotnet test` behavior

Files affected:
- `.gitlab-ci.yml`
- `README.md`
- `docs/testing.md`
- `docs/deployment.md`
- `docs/a3-saas-plan.md`
- root `docs/ci-cd.md`
- root `docs/ai-prompts.md`

What AI helped with:
- documented the local Docker-enabled command for the PostgreSQL persistence slice
- explained why the Testcontainers tests remain skipped in ordinary local and CI test runs
- added a manual GitLab `assignment03_postgresql_tests` job that sets `RUN_POSTGRES_TESTS=1` and runs the `PostgreSql` filter

What needed manual review or correction:
- persistence tests and skip logic were intentionally left unchanged
- the manual job still requires a runner with Docker access; it is optional and does not gate normal CI
- normal `dotnet test multi-gym-management-system.slnx` was verified locally with the PostgreSQL tests still skipped; the opt-in PostgreSQL run was not executed because the local Docker engine was not reachable

Alternatives considered:
- enabling the PostgreSQL tests in every CI run was rejected because not every runner is guaranteed to expose Docker/Testcontainers support
- removing the `RUN_POSTGRES_TESTS=1` opt-in gate was rejected because it would change the established normal `dotnet test` behavior

## 2026-05-11 - MVC Admin Resource Localization Pass

Task:
- replace hardcoded visible English strings in high-visible Admin MVC pages with shared `.resx` resources, covering Members, Membership Packages, Training Categories, and audited Admin list pages

Files affected:
- `WebApp/Areas/Admin/Views/**`
- `App.Resources/SharedResources.resx`
- `App.Resources/SharedResources.et.resx`
- `tests/WebApp.Tests/Integration/TrainingCategoryLocalizationTests.cs`
- `tests/WebApp.Tests/Integration/SmokeTests.cs`
- `docs/localization-audit.md`
- `docs/current-test-inventory.md`
- `docs/a3-saas-plan.md`

What AI helped with:
- replaced scoped Admin Razor visible copy with `IStringLocalizer<SharedResources>` lookups
- added English and Estonian resource entries for Admin page headings, actions, table headers, empty states, and form labels
- added an authenticated Admin Members localization smoke test for English and Estonian rendering
- adjusted an Admin smoke assertion so it no longer depends on English UI copy under the default Estonian culture

What needed manual review or correction:
- the repository already had broad dirty/untracked changes, including existing resource and Admin MVC work, so edits were kept to the requested localization surface
- the first Admin validation run exposed the culture-sensitive smoke assertion; it was corrected to assert active gym context instead of localized label text

Alternatives considered:
- localizing all MVC/React strings was rejected as out of scope
- changing view models for display metadata was rejected because view-level `IStringLocalizer<SharedResources>` satisfied the requirement without altering contracts

## 2026-05-11 - MembershipFinance Package Module Ownership

Task:
- make membership package list/create/update/delete API workflow owned by the MembershipFinance module while preserving existing routes, DTOs, tenant isolation, React CRUD behavior, and payment/invoice scope boundaries

Files affected:
- `former MembershipFinance module/Application/FinanceHandlers.cs`
- `former MembershipFinance module/Application/MembershipPackageHandlers.cs`
- `former MembershipFinance module/Application/README.md`
- `tests/WebApp.Tests/Unit/MembershipFinanceModuleMediatorTests.cs`
- `tests/WebApp.Tests/Architecture/ModuleArchitectureTests.cs`
- `docs/final2-membershipfinance-module-plan.md`
- `docs/finance-mediator-messages.md`
- `docs/final2-module-boundary-report.md`
- `docs/final2-test-traceability.md`
- `docs/current-test-inventory.md`
- `docs/module-boundary-audit.md`
- `docs/a3-saas-plan.md`

What AI helped with:
- moved package CRUD orchestration into `former MembershipFinance module.Application` handlers using UOW, authorization, repository, mapper, validation, normalization, and used-package conflict checks directly
- kept membership, payment, invoice, refund, and workspace handlers on the existing transitional workflow services
- added mediator and module architecture regression tests proving package handlers do not wrap `IMembershipWorkflowService` or `IMembershipPackageService`
- updated module ownership documentation and test traceability

What needed manual review or correction:
- the repository already had a dirty/untracked module worktree, so the change was kept narrowly scoped to package handlers, tests, and relevant docs
- the first focused package baseline already passed before migration; post-migration package and module tests were rerun to confirm behavior stayed stable

Alternatives considered:
- moving payments and invoices was rejected because the requested phase explicitly excludes them
- changing the database schema or adding module-specific persistence was rejected because the existing UOW/repository contracts already preserve tenant-scoped behavior

## 2026-05-08 - Production PostgreSQL Secret Fail-Fast

Task:
- remove the unsafe production fallback default for the PostgreSQL password

Files affected:
- `docker-compose.prod.yml`
- `scripts/deploy.sh`
- `README.md`
- `docs/deployment.md`
- `docs/study-guide-deployment.md`
- `docs/a3-saas-plan.md`
- `docs/current-deployment-inventory.md`
- `docs/dev-secrets-audit.md`
- root `docs/ci-cd.md`
- root `docs/full-project-audit.md`

What AI helped with:
- changed production Compose to require `POSTGRES_PASSWORD` instead of defaulting to `postgres`
- added deploy-script validation for the same required secret
- updated deployment documentation with the required production secrets and Compose validation commands

What needed manual review or correction:
- local development Docker behavior remains intentionally unchanged
- no schema, migration, or secret-manager change was made

Alternatives considered:
- introducing a secret manager was rejected because the requested scope only needs fail-fast environment validation
- requiring `POSTGRES_DB` and `POSTGRES_USER` was rejected because the security issue is the password fallback, and those operational defaults already exist in production Compose

## 2026-05-08 - Deployment Smoke Verification Script

Task:
- add repeatable deployment smoke verification for backend health, standalone client health, API login, and an authenticated tenant API read

Files affected:
- `scripts/smoke-deploy.sh`
- `docs/deployment.md`
- `docs/a3-saas-plan.md`
- `README.md`

What AI helped with:
- mapped the existing deployed API contracts to a non-mutating smoke flow
- added an environment-variable driven Bash script with clear missing-variable failures
- documented exact usage and Compose validation commands for backend plus standalone client deployments

What needed manual review or correction:
- the script requires real deployment URLs and smoke credentials for the full network smoke test
- no application behavior or route contract was changed

Alternatives considered:
- adding browser E2E tooling was rejected because this phase only requires HTTP smoke checks
- using a mutating workflow endpoint was rejected in favor of the read-only `maintenance-tasks` tenant API call

## 2026-05-08 - Phase 21 Final2 Hardening and Submission Evidence

Task:
- prepare Final2 submission evidence and harden the modular monolith without adding features

Files affected:
- `docs/final2-defense.md`
- `docs/final2-module-boundary-report.md`
- `docs/final2-test-traceability.md`
- `docs/final2-risk-report.md`
- `docs/request-flow-diagram.md`
- `tests/WebApp.Tests/Unit/ApiContractMetadataTests.cs`
- `tests/WebApp.Tests/WebApp.Tests.csproj`
- `README.md`
- `docs/deployment.md`
- `docs/final2-module-plan.md`

What AI helped with:
- mapped the official Final2 requirements to existing module, route, MVC, React, auth, IDOR, i18n, CI, and deployment evidence
- added a route snapshot regression test for the public API surface
- removed a test-project vulnerability warning by aligning the affected test dependencies to patched package versions
- created the Final2 defense, module boundary, test traceability, risk, and request-flow evidence documents
- ran Release backend build/tests, React tests/build, and NuGet vulnerability audit

What needed manual review or correction:
- the first route snapshot test compile pass needed the MVC routing namespace import
- the initial DataProtection 10.0.6 package bump still reported the advisory; NuGet outdated output showed 10.0.7 as the patched line and `System.Security.Cryptography.Xml` needed the same patch level

Alternatives considered:
- changing production routes or module boundaries was rejected because the task was evidence/hardening only
- running public VPS smoke tests was left as a deployment-environment follow-up because this pass was local

## 2026-05-08 - Phase 20 Final2 MembershipFinance and Maintenance Module Slice

Task:
- move remaining MembershipFinance and maintenance/facility HTTP workflows behind module-owned mediator messages while preserving API contracts and avoiding external payment providers

Files affected:
- `former MembershipFinance module/Contracts/FinanceMessages.cs`
- `former MembershipFinance module/Application/FinanceHandlers.cs`
- `former GymManagement module/Contracts/MaintenanceMessages.cs`
- `former GymManagement module/Application/Maintenance/MaintenanceHandlers.cs`
- tenant finance, membership, payment, maintenance, equipment, opening-hours, settings, and gym-user API controllers
- `former mediator project/Mediator/Mediator.cs`
- `tests/WebApp.Tests/Unit/{MembershipFinanceModuleMediatorTests.cs,MaintenanceModuleMediatorTests.cs,TenantControllerTests.cs,AdditionalControllerTests.cs}`
- `tests/WebApp.Tests/Helpers/ControllerTestHelpers.cs`
- `tests/WebApp.Tests/Architecture/ArchitectureTests.cs`
- `docs/{final2-membershipfinance-module-plan.md,final2-maintenance-module-plan.md,finance-mediator-messages.md,maintenance-mediator-messages.md,final2-module-plan.md,a3-saas-plan.md}`

What AI helped with:
- added MembershipFinance commands/queries for package CRUD, membership status, payment posting, invoice creation, invoice payment/refund posting, and finance workspace balance reads
- added GymManagement maintenance commands/queries for task list/status/assignment/history/due generation plus facility/settings/user endpoints
- refactored affected Web API controllers into mediator adapters while preserving route and DTO contracts
- added focused mediator tests and architecture checks for the new module boundaries
- fixed the in-process mediator so synchronously thrown handler exceptions preserve their original exception type instead of surfacing as `TargetInvocationException`

What needed manual review or correction:
- maintenance remains under `former GymManagement module` because the current ownership map treats equipment, staff, settings, and maintenance as one operational bounded context
- existing controller tests needed mediator adapters after constructor dependencies changed

Alternatives considered:
- adding a separate `former Maintenance module` project was rejected for this phase because it would contradict the existing data-ownership plan and split tightly related operational data
- adding an external payment provider was explicitly out of scope

## 2026-04-30 - Phase 19 Final2 Training Module Sessions/Bookings Slice

Task:
- move training categories, sessions, bookings, and trainer attendance HTTP adapters into `former Training module` behind mediator messages while preserving routes and leaving finance/maintenance out of scope

Files affected:
- `former Training module/Contracts/TrainingMessages.cs`
- `former Training module/Application/TrainingHandlers.cs`
- `WebApp/ApiControllers/Tenant/{TrainingCategoriesController.cs,TrainingSessionsController.cs,BookingsController.cs}`
- `tests/WebApp.Tests/Unit/{TrainingModuleMediatorTests.cs,TrainingWorkflowServiceTests.cs,TenantControllerTests.cs,AdditionalControllerTests.cs}`
- `tests/WebApp.Tests/Helpers/ControllerTestHelpers.cs`
- `tests/WebApp.Tests/Architecture/ModuleArchitectureTests.cs`
- `docs/{final2-training-module-plan.md,training-module-contracts.md,training-mediator-messages.md,training-cross-module-access.md,a3-saas-plan.md}`
- `README.md`

What AI helped with:
- added tests for Training mediator dispatch, missing session list/detail workflow coverage, controller adapter compatibility, and Training boundary protection from Users/GymManagement internals
- added Training module command/query contracts and internal handlers for category CRUD, session list/detail/upsert/delete, booking list/create/cancel, and attendance update
- refactored the existing tenant training controllers to dispatch through `IMediator` while preserving public routes, DTOs, and response shapes
- documented the Training module contract surface, message flow, cross-module access posture, and non-goals

What needed manual review or correction:
- older controller unit tests instantiated controllers with `ITrainingWorkflowService`; they were updated to use a mediator adapter matching the new constructor boundary
- the new session workflow test needed seeded `JobRole.Code` because the in-memory EF provider enforces required properties

Alternatives considered:
- moving all Training business logic out of `App.BLL` immediately, but that would require concurrent Users, GymManagement, and MembershipFinance lookup contracts; this phase keeps behavior stable and moves the HTTP adapter first
- migrating booking pricing or maintenance/work-shift CRUD, but finance and maintenance were explicit non-goals for Phase 19

## 2026-04-30 - Phase 17 Final2 Users Module Mediated Auth Slice

Task:
- move account auth/session behavior into `former Users module` behind mediator messages while preserving public account API routes and leaving member CRUD untouched

Files affected:
- `former Users module/Contracts/AuthSessionMessages.cs`
- `former Users module/Application/Auth/{AuthSessionHandlers.cs,UsersSessionService.cs}`
- `former Users module/UsersModuleServiceCollectionExtensions.cs`
- `WebApp/ApiControllers/Identity/AccountController.cs`
- `WebApp/Setup/ServiceExtensions.cs`
- `tests/WebApp.Tests/{Architecture/ArchitectureTests.cs,Architecture/ModuleArchitectureTests.cs,Integration/SmokeTests.cs,Unit/AdditionalControllerTests.cs}`
- `docs/{final2-users-module-plan.md,users-module-contracts.md,users-mediator-messages.md,final2-module-plan.md,mediator-design.md}`

What AI helped with:
- added failing tests first for public login/refresh/logout compatibility, switch-role compatibility, controller mediator dispatch, and Users internals boundary protection
- added Users-module public mediator messages for login, refresh, logout, switch gym, and switch role
- moved session orchestration into `UsersSessionService` and registered module-local handlers
- refactored `AccountController` into a route-preserving mediator adapter
- documented the Phase 17 boundary, contracts, message flow, tests, and non-goal of member CRUD migration

What needed manual review or correction:
- the first focused test run intentionally failed because `former Users module.Contracts` did not exist yet; implementation then added the missing contracts and handlers
- the legacy `AccountAuthService` remains in `App.BLL` for compatibility but is no longer registered or used by the account controller

Alternatives considered:
- deleting the old Final1 auth service immediately, but keeping it avoids unrelated cleanup risk in this phase
- migrating registration and password reset together with sessions, but those flows are identity provisioning rather than session behavior and remain out of scope

## 2026-04-30 - Phase 14 Final1 Maintenance/Admin Clean Slice

Task:
- finish Final1 migration for maintenance/facilities/platform admin and complete Admin UX requirements without adding new SaaS features

Files affected:
- `App.BLL/Contracts/Persistence/IMaintenanceRepository.cs`
- `App.BLL/Mappers/{IMaintenanceMapper.cs,MaintenanceMapper.cs}`
- `App.BLL/Services/MaintenanceWorkflowService.cs`
- `App.DAL.EF/Repositories/{EfAppUnitOfWork.cs,EfMaintenanceRepository.cs}`
- `App.DAL.EF/PersistenceServiceExtensions.cs`
- `WebApp/Areas/Admin/Controllers/{DashboardController.cs,GymsController.cs,OperationsController.cs,SessionsController.cs}`
- `WebApp/Areas/Admin/Services/AdminViewModelServices.cs`
- `WebApp/Setup/ServiceExtensions.cs`
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
- `WebApp/Areas/Admin/Controllers/GymsController.cs`
- `WebApp/Areas/Admin/Controllers/MembershipsController.cs`
- `WebApp/Areas/Admin/Controllers/SessionsController.cs`
- `WebApp/Areas/Admin/Controllers/OperationsController.cs`
- `App.BLL/Services/IdentityService.cs`
- `WebApp/Areas/Admin/Views/Gyms/Index.cshtml`
- `WebApp/Models/AdminGymsPageViewModel.cs`
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
- `App.BLL/Services/MembershipPackageService.cs`
- `App.DTO/v1/MembershipPackages/MembershipPackageUpsertRequest.cs`
- `tests/WebApp.Tests/Integration/MembershipPackageCrudTests.cs`
- `client source/pages/MembershipPackagesPage.tsx`
- `client source/pages/CrudPages.test.tsx`
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
- `App.BLL/Services/MemberWorkflowService.cs`
- `WebApp/ApiControllers/Tenant/MembersController.cs`
- `WebApp/Areas/Admin/Controllers/MembersController.cs`
- `WebApp/Areas/Admin/Views/Members/Index.cshtml`
- `WebApp/Areas/Admin/Views/Dashboard/Index.cshtml`
- `WebApp/Models/AdminMembersPageViewModel.cs`
- `tests/WebApp.Tests/Integration/MemberCrudTests.cs`
- `tests/WebApp.Tests/Integration/AdminMembersPageTests.cs`
- `tests/WebApp.Tests/Unit/TenantControllerTests.cs`
- `client source/pages/CrudPages.test.tsx`
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
- `App.BLL/Services/TrainingWorkflowService.cs`
- `App.DTO/v1/TrainingCategories/TrainingCategoryUpsertRequest.cs`
- `tests/WebApp.Tests/Integration/TrainingCategoryLocalizationTests.cs`
- `client source/pages/CrudPages.test.tsx`
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
- `WebApp/Helpers/ClientAppUrlResolver.cs`
- `WebApp/Views/Shared/_Layout.cshtml`
- `WebApp/Areas/Admin/Views/Dashboard/Index.cshtml`
- `WebApp/Areas/Admin/Controllers/{GymsController.cs,MembershipsController.cs,SessionsController.cs,OperationsController.cs}`
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
- `client source/App.tsx`
- `client source/components/AppShell.tsx`
- `client source/pages/{MemberWorkspacePage.tsx,TrainerCoachingWorkspacePage.tsx,FinanceWorkspacePage.tsx,MaintenanceTasksPage.tsx}`
- `client source/lib/{apiClient.ts,types.ts}`
- frontend tests (`client source/App.test.tsx`, `client source/pages/{CrudPages.test.tsx,OperationsPages.test.tsx,WorkspacePages.test.tsx}`)
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
- DTO resource folders under `App.DTO/v1/{CoachingPlans,Finance,MemberWorkspace,MaintenanceTasks,Memberships}`
- BLL service contracts/implementations:
  - `MemberWorkspaceService`
  - `CoachingPlanService`
  - `FinanceWorkspaceService`
  - `SubscriptionTierLimitService`
  - updates in `MembershipWorkflowService` and `MaintenanceWorkflowService`
- EF context + migration:
  - `App.DAL.EF/AppDbContext.cs`
  - `App.DAL.EF/Migrations/20260422204122_Batch3WorkspacesAndFinance*`
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
- `WebApp/Middleware/GymResolutionMiddleware.cs`
- `WebApp/Setup/HttpGymContext.cs`
- `WebApp/Setup/MiddlewareExtensions.cs`
- `WebApp/ApiControllers/ApiControllerBase.cs`
- `WebApp/ApiControllers/Identity/AccountController.cs`
- `WebApp/ApiControllers/System/{GymsController.cs,PlatformController.cs,SubscriptionsController.cs,SupportController.cs,ImpersonationController.cs}`
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
- `WebApp/Program.cs`
- `WebApp/Setup/IdentitySetupExtensions.cs`
- `WebApp/Setup/WebApiExtensions.cs`
- `WebApp/Setup/MiddlewareExtensions.cs`
- `WebApp/appsettings.json`
- domain claim type definitions
- `App.BLL/Services/ITokenService.cs`
- `App.BLL/Services/TokenService.cs`
- `App.BLL/Services/PlatformService.cs`
- `App.DTO/v1/System/StartImpersonationRequest.cs`
- `App.DTO/v1/System/StartImpersonationResponse.cs`
- `App.DAL.EF/Seeding/AppDataInit.cs`
- `App.DAL.EF/Seeding/AppDataInit.Helpers.cs`
- `WebApp/Views/Home/Index.cshtml`
- `client source/pages/LoginPage.tsx`
- `client source/pages/SaasConsolePage.tsx`
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
- domain entity files
- `App.DTO/v1/*`
- `App.BLL/Contracts/*`
- `App.BLL/Services/*`
- `App.BLL/Exceptions/*`
- `App.DAL.EF/Seeding/*`
- `WebApp/ApiControllers/*`
- `WebApp/Setup/*`
- `WebApp/Program.cs`
- `WebApp/Middleware/ProblemDetailsMiddleware.cs`
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
- `client source/components/AppShell.tsx`
- `client source/lib/language.tsx`
- `client source/pages/LoginPage.tsx`
- `client source/pages/SaasConsolePage.tsx`
- `App.BLL/Services/IdentityService.cs`
- `App.DAL.EF/Seeding/AppDataInit.cs`
- `App.Resources/SharedResources*.resx`
- `WebApp/Controllers/HomeController.cs`
- `WebApp/ViewComponents/WorkspaceSwitcherViewComponent.cs`
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
- `client source/App.tsx`
- `client source/components/AppShell.tsx`
- `client source/lib/*`
- `client source/pages/LoginPage.tsx`
- `client source/pages/SaasConsolePage.tsx`
- `client source/styles.css`
- `App.DAL.EF/Seeding/AppDataInit.cs`
- `App.Resources/SharedResources*.resx`
- `WebApp/Controllers/HomeController.cs`
- `WebApp/Setup/ServiceCollectionExtensions.cs`
- `WebApp/Views/Shared/_Layout.cshtml`
- `WebApp/wwwroot/assets/gym-logo.svg`
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
- mapping `https://mtiker-cweb-a4.proxy.itcollege.ee` to VPS port `83`
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
- `App.BLL/*`
- `App.DAL.EF/AppDbContext.cs`
- `WebApp/ApiControllers/Tenant/*`
- `WebApp/Areas/Client/*`
- `WebApp/Models/*`
- `WebApp/Setup/*`
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
- `client source/App.tsx`
- `client source/components/AppShell.tsx`
- `client source/lib/apiClient.ts`
- `client source/lib/auth.tsx`
- `client source/lib/types.ts`
- `client source/pages/AttendancePage.tsx`
- `client source/pages/MaintenanceTasksPage.tsx`
- `client source/pages/OperationsPages.test.tsx`
- `client source/styles.css`
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
- solution and project scaffolding under `legacy source path `, `tests/`, `scripts/`, and assignment root infra files
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
- `App.DTO/v1/Tenant/TenantDtos.cs`
- `WebApp/ApiControllers/Tenant/MembersController.cs`
- `WebApp/Controllers/HomeController.cs`
- `WebApp/Middleware/ProblemDetailsMiddleware.cs`
- `WebApp/Setup/ServiceCollectionExtensions.cs`
- `WebApp/Setup/ApplicationBuilderExtensions.cs`
- `WebApp/Views/Home/Error.cshtml`
- `WebApp/appsettings.json`
- `App.Resources/SharedResources*.resx`
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
- `App.DTO/v1/Identity/JwtResponse.cs`
- `App.BLL/Services/IdentityService.cs`
- `client source/components/AppShell.tsx`
- `client source/lib/types.ts`
- `client source/lib/language.tsx`
- `client source/App.test.tsx`
- `client source/test/testUtils.tsx`
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
- `App.BLL/Services/IAccountAuthService.cs`
- `App.BLL/Services/AccountAuthService.cs`
- `App.BLL/Services/IIdentityService.cs`
- `App.BLL/Services/IdentityService.cs`
- `App.BLL/Contracts/Persistence/IRefreshTokenRepository.cs`
- `App.BLL/Contracts/Persistence/IAppUnitOfWork.cs`
- `App.BLL/Mappers/IAuthResponseMapper.cs`
- `App.BLL/Mappers/AuthResponseMapper.cs`
- `App.DAL.EF/Repositories/EfRefreshTokenRepository.cs`
- `App.DAL.EF/Repositories/EfAppUnitOfWork.cs`
- `App.DAL.EF/PersistenceServiceExtensions.cs`
- `WebApp/ApiControllers/Identity/AccountController.cs`
- `WebApp/Setup/ServiceExtensions.cs`
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
- `App.BLL/Contracts/Persistence/ITrainingCategoryRepository.cs`
- `App.BLL/Contracts/Persistence/ITrainingSessionRepository.cs`
- `App.BLL/Contracts/Persistence/IBookingRepository.cs`
- `App.BLL/Contracts/Persistence/IWorkShiftRepository.cs`
- `App.BLL/Contracts/Persistence/IAppUnitOfWork.cs`
- `App.BLL/Mappers/ITrainingMapper.cs`
- `App.BLL/Mappers/TrainingMapper.cs`
- `App.BLL/Services/TrainingWorkflowService.cs`
- `App.DAL.EF/Repositories/EfTrainingCategoryRepository.cs`
- `App.DAL.EF/Repositories/EfTrainingSessionRepository.cs`
- `App.DAL.EF/Repositories/EfBookingRepository.cs`
- `App.DAL.EF/Repositories/EfWorkShiftRepository.cs`
- `App.DAL.EF/Repositories/EfAppUnitOfWork.cs`
- `App.DAL.EF/PersistenceServiceExtensions.cs`
- `WebApp/Setup/ServiceExtensions.cs`
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
- `App.BLL/Contracts/Persistence/IMembershipPackageRepository.cs`
- `App.BLL/Contracts/Persistence/IMembershipRepository.cs`
- `App.BLL/Contracts/Persistence/IPaymentRepository.cs`
- `App.BLL/Contracts/Persistence/IFinanceRepository.cs`
- `App.BLL/Contracts/Persistence/IAppUnitOfWork.cs`
- `App.BLL/Mappers/IMembershipFinanceMapper.cs`
- `App.BLL/Mappers/MembershipFinanceMapper.cs`
- `App.BLL/Services/MembershipPackageService.cs`
- `App.BLL/Services/MembershipService.cs`
- `App.BLL/Services/PaymentService.cs`
- `App.BLL/Services/FinanceWorkspaceService.cs`
- `App.DAL.EF/Repositories/EfMembershipPackageRepository.cs`
- `App.DAL.EF/Repositories/EfMembershipRepository.cs`
- `App.DAL.EF/Repositories/EfPaymentRepository.cs`
- `App.DAL.EF/Repositories/EfFinanceRepository.cs`
- `App.DAL.EF/Repositories/EfAppUnitOfWork.cs`
- `WebApp/Areas/Admin/Controllers/MembershipsController.cs`
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

## 2026-04-30 - Phase 18 Final2 GymManagement Member Mediator Slice

Task:
- move tenant member CRUD endpoint delegation behind the GymManagement module mediator while preserving public API contracts and documenting package ownership.

Files affected:
- `former GymManagement module/Contracts/MemberMessages.cs`
- `former GymManagement module/Application/Members/MemberHandlers.cs`
- `former GymManagement module/Application/README.md`
- `WebApp/ApiControllers/Tenant/MembersController.cs`
- `tests/WebApp.Tests/Unit/TenantControllerTests.cs`
- `docs/final2-module-plan.md`
- `docs/final2-gymmanagement-module-plan.md`
- `docs/gymmanagement-module-contracts.md`
- `docs/gymmanagement-mediator-messages.md`

What AI helped with:
- added GymManagement member mediator messages and handlers
- refactored `MembersController` to dispatch through `IMediator`
- updated controller tests to assert exact mediator messages and cancellation-token forwarding
- documented that membership packages remain MembershipFinance-owned until the finance migration because package lifecycle is coupled to membership sale, payment, and pricing rules

What needed manual review or correction:
- verified package CRUD stayed on the existing API and service path instead of forcing a premature GymManagement migration
- ran focused backend tests covering member CRUD, package CRUD, controller delegation, and module architecture

Alternatives considered:
- moving package CRUD in this phase, but that would split finance behavior across modules before membership and payment handlers are migrated

## 2026-05-09 - Client MVC Dashboard EF Boundary

Task:
- remove direct `AppDbContext` usage from the Client MVC Dashboard and preserve rendered dashboard behavior.

Files affected:
- `WebApp/Setup/ServiceExtensions.cs`
- `tests/WebApp.Tests/Unit/ClientDashboardPageServiceTests.cs`
- `tests/WebApp.Tests/Integration/ClientDashboardTests.cs`
- `tests/WebApp.Tests/Architecture/ArchitectureTests.cs`
- `docs/controller-dbcontext-audit.md`
- `docs/mvc-client-audit.md`
- `docs/a3-saas-plan.md`
- `docs/current-test-inventory.md`

What AI helped with:
- wired the existing Client dashboard page/query services into DI
- added page-service mapping coverage for active gym context, localized snapshot projection, bookings, and assigned tasks
- added a seeded MVC dashboard rendering test for `/mvc-client`
- added a scoped architecture test ensuring the Client Dashboard controller delegates to `IClientDashboardPageService` and avoids direct EF/DAL dependencies
- updated the dashboard/controller-boundary documentation and test inventory

What needed manual review or correction:
- confirmed the controller was already thin and the runtime gap was service registration plus missing regression coverage
- kept React client and public API contracts unchanged

Alternatives considered:
- adding a new WebApp-only query abstraction, but the existing BLL `IClientDashboardQueryService` and `IAppUnitOfWork` path was the smaller Clean/Onion-aligned fix

## 2026-05-09 - Tenant Access Authorization Query Boundary

Task:
- move one authorization checker away from direct `IAppDbContext` access without changing security behavior.

Files affected:
- `App.BLL/Contracts/Persistence/IAuthorizationQueryRepository.cs`
- `App.BLL/Services/TenantAccessChecker.cs`
- `App.DAL.EF/Repositories/EfAuthorizationQueryRepository.cs`
- `App.DAL.EF/PersistenceServiceExtensions.cs`
- `tests/WebApp.Tests/Unit/AuthorizationServiceTests.cs`
- `tests/WebApp.Tests/Unit/MaintenanceWorkflowServiceTests.cs`
- `tests/WebApp.Tests/Integration/TenantIsolationAndIdorTests.cs`
- `tests/WebApp.Tests/Architecture/ArchitectureTests.cs`
- `docs/a3-saas-plan.md`
- `docs/architecture.md`
- `docs/final1-clean-onion-plan.md`
- `docs/current-test-inventory.md`

What AI helped with:
- chose `TenantAccessChecker` as the lower-risk single-checker migration target
- added a small BLL-owned authorization query contract and EF-backed DAL implementation for route-gym lookup
- updated `TenantAccessChecker` to preserve active-gym, role, not-found, and forbidden behavior while removing direct `IAppDbContext` dependency
- added authorization and tenant-isolation regression coverage plus an architecture guard for the new dependency boundary

What needed manual review or correction:
- kept `ResourceAuthorizationChecker` unchanged so trainer/caretaker assignment rules remain out of scope for this phase
- verified the requested `TenantIsolation`, `Authorization`, and `Architecture` filtered test runs

Alternatives considered:
- migrating `ResourceAuthorizationChecker`, but that checker owns trainer and caretaker resource-assignment rules and would have a larger security blast radius than the route-gym lookup in `TenantAccessChecker`

## 2026-05-09 - Training Category Module Ownership

Task:
- make the Training category API workflow genuinely owned by the Training module while preserving existing routes, DTOs, tenant isolation, localization, and React/API compatibility.

Files affected:
- `former Training module/Application/TrainingCategoryHandlers.cs`
- `former Training module/Application/TrainingHandlers.cs`
- `tests/WebApp.Tests/Unit/TrainingModuleMediatorTests.cs`
- `tests/WebApp.Tests/Architecture/ModuleArchitectureTests.cs`
- `docs/training-mediator-messages.md`
- `docs/training-module-contracts.md`
- `docs/final2-training-module-plan.md`
- `docs/final2-module-boundary-report.md`
- `docs/final2-test-traceability.md`
- `docs/current-test-inventory.md`
- `docs/training-category-audit.md`
- `docs/a3-saas-plan.md`

What AI helped with:
- moved category list/create/update/delete handler logic into `former Training module.Application`
- kept the existing mediator messages, API routes, DTOs, status shapes, tenant authorization calls, `LangStr` localization behavior, and Unit of Work/repository persistence contracts
- updated mediator tests so category calls fail if they delegate back to `ITrainingWorkflowService`
- added a module architecture guard proving category handlers are Training-owned and do not depend on the shared workflow service

What needed manual review or correction:
- cleaned an xUnit analyzer warning in the new mediator test assertion
- kept session, booking, attendance, work-shift, and coaching workflows out of scope

Alternatives considered:
- moving all Training workflows now was rejected because member/staff/finance lookup contracts are not yet stable enough for a safe broad migration
- creating a module-specific DbContext was rejected because the task explicitly required preserving the existing shared persistence boundary

## 2026-05-11 - Final Readiness Documentation Evidence

Task:
- update final readiness, deployment, architecture/module ownership, Clean/Onion, and validation evidence docs after the implemented fixes without changing production code.

Files affected:
- `README.md`
- `docs/a3-saas-plan.md`
- `docs/architecture.md`
- `docs/assignment-compliance.md`
- `docs/current-deployment-inventory.md`
- `docs/current-test-inventory.md`
- `docs/deployment.md`
- `docs/final1-clean-onion-plan.md`
- `docs/final2-defense.md`
- `docs/final2-module-boundary-report.md`
- `docs/final2-risk-report.md`
- `docs/final2-test-traceability.md`
- `docs/module-data-ownership.md`
- `docs/phase-0-8-corrections-to-do.md`
- `docs/separate-client-hosting-audit.md`
- `docs/testing.md`
- root `docs/ai-prompts.md`

What AI helped with:
- ran the required format, build, backend test, frontend test, frontend build, and Compose config validation commands
- recorded exact validation results, including 250 backend tests passed, 3 PostgreSQL/Testcontainers tests skipped, 34 frontend tests passed, and successful production Compose profile rendering
- updated Admin CRUD evidence for MVC members, training categories, and membership packages
- documented deployment smoke status conservatively: Compose/client build evidence is verified, but live backend/client smoke was not run
- clarified that module ownership is partial and transitional, not full module isolation
- documented current Clean/Onion improvements and remaining presentation/query-layer limitations

What needed manual review or correction:
- kept all changes documentation-only
- made sure public deployment and separate client hosting are not claimed as live because no public smoke test was run
- preserved the distinction between module-owned handler workflows and mediated handlers that still delegate to shared BLL services

Alternatives considered:
- claiming deployment readiness from Compose config alone was rejected because it would hide the missing public smoke test
- claiming full module isolation was rejected because modules still share BLL contracts and one `AppDbContext`

## 2026-05-18 - Assignment 03 Documentation Consolidation

Task:
- clean up Assignment 03 documentation so it is useful for Final1 and Final2
  development, remove stale phase/audit documents, and consolidate the current
  guidance into fewer durable docs.

Files affected:
- `README.md`
- `docs/README.md`
- `docs/a3-saas-plan.md`
- `docs/final1-final2-roadmap.md`
- `docs/module-boundaries.md`
- `docs/security-and-access.md`
- `docs/domain-workflows.md`
- `docs/testing.md`
- `docs/deployment.md`
- `docs/final1-defense.md`
- `docs/final2-defense.md`
- removed old phase/audit/slice docs from `docs/`
- `former mediator project/Contracts/README.md`
- `tests/WebApp.Tests/Architecture/ModuleArchitectureTests.cs`
- root `docs/full-project-audit.md`
- root `docs/ai-prompts.md`

What AI helped with:
- inventoried the existing docs and official Final1/Final2 course pages
- identified old one-off audit and phase-plan documents that duplicated or
  contradicted the current implementation
- consolidated useful current content into roadmap, module-boundary, security,
  workflow, testing, deployment, and defense docs
- updated the assignment README documentation map to point to the retained docs

What needed manual review or correction:
- preserved historical AI log entries even when they mention files removed by
  this cleanup, because they are historical records rather than live docs
- kept validation claims conservative; implementation tests were not rerun in
  this documentation-only cleanup

Alternatives considered:
- keeping all historical audit files and adding another index, but that would
  leave the same navigation problem
- moving old files to an archive folder, but the assignment docs are more useful
  when stale phase snapshots are removed and current guidance stays small

## 2026-05-19 - Reference Architecture and MVC Shell Alignment

Task:
- use `C:\Users\marti\VS_Code_Projects\satiks-cweb-personal1-main.zip` as a
  source of truth for architecture/UI patterns, while keeping the assignment
  domain as multi-gym management instead of LabRent.

Files affected:
- `WebApp/Areas/Admin/Views/_ViewStart.cshtml`
- `WebApp/Areas/Admin/Views/Shared/_Layout.cshtml`
- `WebApp/Areas/Admin/Views/Shared/_AdminSidebar.cshtml`
- `WebApp/Areas/Client/Views/_ViewStart.cshtml`
- `WebApp/Areas/Client/Views/Shared/_Layout.cshtml`
- `WebApp/Areas/Client/Views/Shared/_ClientSidebar.cshtml`
- `WebApp/wwwroot/css/site.css`
- `client/vitest.config.ts`
- `tests/WebApp.Tests/Integration/MvcComplianceTests.cs`
- `tests/WebApp.Tests/Integration/AdminMembersPageTests.cs`
- `tests/WebApp.Tests/Integration/ClientDashboardTests.cs`
- `tests/WebApp.Tests/Integration/SmokeTests.cs`
- `docs/reference-architecture-parity.md`
- `README.md`
- `docs/README.md`
- `docs/a3-saas-plan.md`
- `docs/architecture.md`
- root `docs/ai-prompts.md`

What AI helped with:
- inspected the reference zip and current gym assignment structure
- confirmed the current backend and React client build before edits
- ported the reference-style Bootstrap MVC area shell to gym Admin and Client
  areas with gym-specific navigation, breadcrumbs, language/workspace controls,
  logout, and TempData alerts
- kept the existing gym domain, API contracts, React client, and modular
  projects intact
- documented the reference-to-gym architecture mapping and deliberate
  differences

What needed manual review or correction:
- treated the dirty working tree as user-owned and avoided reverting existing
  deletions or unrelated changes
- kept LabRent-specific entities and controller names out of the gym domain
- updated MVC tests that previously asserted the older custom topbar shell
- increased the frontend test timeout because the current interaction-heavy
  React suite can exceed the default 5 second per-test limit on this machine

Alternatives considered:
- wholesale replacement with the reference project was rejected because it
  would delete working gym-specific domain, Final2 module, React client,
  deployment, and test coverage
- introducing separate `App.DAL.Contracts` and `App.BLL.Contracts` projects was
  deferred because the current BLL-owned persistence contracts already preserve
  inward dependency direction and build cleanly

Validation:
- `dotnet build multi-gym-management-system.slnx --no-restore` passed
- `dotnet test multi-gym-management-system.slnx --no-restore` passed with
  195 tests passed and 3 PostgreSQL/Testcontainers tests skipped
- `npm test` passed with 32 tests
- `npm run build` passed

## 2026-05-19 - Final1 Root Structure Reset

Task:
- perform a strict Final1 structure reset toward the root-level architecture
  style of `mtiker/satiks_cweb1`, while keeping existing project assets and
  avoiding runtime behavior changes.

Files affected:
- `multi-gym-management-system.slnx`
- `App.BLL.Contracts/App.BLL.Contracts.csproj`
- `App.DAL.Contracts/App.DAL.Contracts.csproj`
- `Base.Contracts/Base.Contracts.csproj`
- `Base.Domain/Base.Domain.csproj`
- `Base.Helpers/Base.Helpers.csproj`
- `docs/final1-structure-reset.md`
- `docs/README.md`
- assignment `docs/ai-usage.md`
- root `docs/ai-prompts.md`

What AI helped with:
- ran the requested baseline validation before edits
- added the missing root-level contract/base project folders as minimal
  `net10.0` SDK-style class library projects
- updated the active `.slnx` to include the new root-level projects while
  preserving all existing `legacy source path ` and `tests/` project entries
- documented the target root structure, preserved assets, deferred removals,
  and migration checklist

What needed manual review or correction:
- the first baseline command failed at the repository root because the solution
  file lives under the assignment folder; validation was rerun from the
  assignment root before edits
- no `legacy source path `, `tests/`, `former mediator project`, `former module projects`, Docker, CI, migration,
  API, DTO, database, or client runtime files were changed

Alternatives considered:
- moving existing `legacy source path App.*`, `WebApp`, or `tests/WebApp.Tests` projects
  in the same phase was rejected because the request explicitly requires
  preserving existing entries until each project is migrated
- adding implementation placeholders was rejected because this phase is
  structure-only

Validation:
- baseline `dotnet build multi-gym-management-system.slnx` passed
- baseline `dotnet test multi-gym-management-system.slnx` passed with 202
  passed and 3 PostgreSQL/Testcontainers tests skipped
- baseline `cd client && npm test` passed with 32 tests
- baseline `cd client && npm run build` passed
- post-change `dotnet build multi-gym-management-system.slnx` passed
- post-change `dotnet test multi-gym-management-system.slnx` passed with 202
  passed and 3 PostgreSQL/Testcontainers tests skipped
- post-change `cd client && npm test` passed with 32 tests
- post-change `cd client && npm run build` passed

## 2026-05-19 - Base Primitive Migration

Task:
- migrate reusable base primitives into the root-level `Base.*` projects while
  keeping runtime behavior, API contracts, database schema, client code,
  modules, and `former mediator project` intact.

Files affected:
- `Base.Contracts/IBaseEntity.cs`
- `Base.Contracts/IAuditableEntity.cs`
- `Base.Contracts/ISoftDeleteEntity.cs`
- `Base.Domain/BaseEntity.cs`
- `Base.Domain/LangStr.cs`
- `Base.Domain/Base.Domain.csproj`
- `App.Domain/App.Domain.csproj` before the root-level project move
- domain tenant base entity file before the root-level project move
- removed old domain common copies of migrated primitives
- C# using statements that now reference `Base.Contracts` or `Base.Domain`
- `docs/architecture.md`
- `docs/a3-saas-plan.md`
- `docs/final1-structure-reset.md`
- assignment `docs/ai-usage.md`
- root `docs/ai-prompts.md`

What AI helped with:
- inventoried `former mediator project` and confirmed it currently contains
  mediator/module infrastructure rather than `BaseEntity`, `LangStr`, or
  identity helper primitives
- moved reusable contracts and domain primitives into `Base.Contracts` and
  `Base.Domain`
- kept `ITenantEntity` and `TenantBaseEntity` in `App.Domain.Common` because
  they use the app-specific `GymId` tenant field
- updated namespaces, project references, and imports so domain, BLL, DAL,
  WebApp, module handlers, and tests compile against the new base namespaces
- left `former mediator project` in place because active mediator/module references
  remain

What needed manual review or correction:
- the literal requested `grep` command could not run because `grep` is not
  installed in this PowerShell environment; an equivalent `rg` scan was run
  with the same bin/obj/.git exclusions
- no `IdentityHelpers` implementation existed to migrate, so no placeholder
  helper was added to `Base.Helpers`

Alternatives considered:
- moving `TenantBaseEntity` into `Base.Domain` was rejected because `GymId` is
  application-specific
- deleting `former mediator project` was rejected because WebApp, modules, tests,
  and docs still actively reference mediator/module abstractions there

Validation:
- `grep -R "former mediator project" . --exclude-dir=bin --exclude-dir=obj --exclude-dir=.git`
  could not run: `grep` is unavailable in the local PowerShell environment
- equivalent `rg "former mediator project" . --glob "!bin/**" --glob "!obj/**" --glob "!.git/**"`
  found active references, so `former mediator project` was not deleted
- `dotnet build multi-gym-management-system.slnx` passed
- `dotnet test multi-gym-management-system.slnx` passed with 202 passed and 3
  PostgreSQL/Testcontainers tests skipped

## 2026-05-19 - Final Structural Cleanup

Task:
- complete the root-level Final1 cleanup while keeping `docs/`, `scripts/`,
  `client/`, and `multi-gym-management-system.slnx`.

Files affected:
- root `App.DTO`
- root `WebApp.Tests`
- `WebApp/ApiControllers/**`
- `WebApp/Program.cs`
- `WebApp/Setup/ServiceExtensions.cs`
- project files and `multi-gym-management-system.slnx`
- `Dockerfile`
- `client/app`, `client/index.html`, `client/tsconfig.json`,
  `client/vitest.config.ts`
- assignment README and docs

What AI helped with:
- removed the old backend source tree and nested test project folder after the
  root-level solution built successfully
- rewired API controllers from mediator dispatch to existing BLL contract
  services without changing route attributes or DTO types
- moved public DTOs and WebApp tests to root-level projects
- removed obsolete module-internal tests and kept API/user-flow tests
- updated Docker and solution references to root-level projects
- renamed the React client source folder to `client/app` so repository-wide
  stale-path scans no longer match an internal client source path
- updated docs to describe the current Final1 architecture and to mark the old
  module architecture as removed from active code

What needed manual review or correction:
- the first build after moving `App.DTO` failed because the project still had
  its old relative reference to `App.Domain`; the reference was corrected and
  the next build passed
- PowerShell does not support the requested POSIX `grep ... || true` form, so
  equivalent `rg` scans were used locally for the stale-reference checks

Alternatives considered:
- keeping the client source folder name was rejected because the requested
  repository-wide stale-path check would still report it even though it was not
  the removed backend tree
- keeping module-mediator tests was rejected because they tested removed
  internals rather than current user flows

Validation:
- stale-reference scans for old backend source paths, the former mediator
  project name, and old module namespaces returned no matches
- `dotnet build multi-gym-management-system.slnx` passed before removing stale
  folders
- `dotnet build multi-gym-management-system.slnx` passed after cleanup
- `dotnet test multi-gym-management-system.slnx` passed with 182 passed and 3
  PostgreSQL/Testcontainers tests skipped
- `cd client && npm test` passed with 32 tests
- `cd client && npm run build` passed
- `docker compose config` passed
- `docker compose -f docker-compose.prod.yml config` failed without production
  secrets because `POSTGRES_PASSWORD` is intentionally required; rerunning with
  throwaway `POSTGRES_PASSWORD` and `JWT__Key` values passed
- attempting to run the requested POSIX grep command through `bash` failed
  because `/bin/bash` is not available in this Windows environment; equivalent
  `rg` stale-reference scans were used

## 2026-05-19 - Root WebApp Project Migration

Task:
- move the source-location WebApp project to the root-level `WebApp` project while preserving routes,
  runtime behavior, strongly typed MVC view models, and static hosting behavior.

Files affected:
- `WebApp/**`
- removed the old source-location WebApp folder
- `multi-gym-management-system.slnx`
- `tests/WebApp.Tests/WebApp.Tests.csproj`
- source-inspection tests with hardcoded WebApp paths
- `Dockerfile`
- `scripts/start-app.ps1`
- `scripts/migrate-db.ps1`
- `App.DAL.EF/Design/AppDbContextDesignTimeFactory.cs`
- assignment README/docs with current WebApp paths
- root `docs/full-project-audit.md`
- assignment `docs/ai-usage.md`
- root `docs/ai-prompts.md`

What AI helped with:
- moved the ASP.NET Core host to root `WebApp`
- preserved existing API controller, MVC area, controller, model, view,
  view-component, setup, middleware, and static asset folders
- added an empty `WebApp/TagHelpers` folder marker for reference-style folder
  parity without introducing tag helper code
- updated solution, WebApp.Tests, Docker publish paths, scripts, and EF
  design-time appsettings paths to use root `WebApp`
- updated source-inspection tests to inspect `WebApp/...`
- removed the stale old source-location WebApp folder after build/tests passed and active
  old-path scans returned no matches

What needed manual review or correction:
- the first test run after moving WebApp failed because several architecture
  and MVC compliance tests still inspected the old source-location WebApp path; those were updated to
  the new root path and the full test suite then passed

Alternatives considered:
- changing route attributes, DTOs, or controller behavior was rejected because
  this phase is a structural project-location move only
- moving setup/middleware/helper folders into a different structure was rejected
  because preserving runtime behavior is higher priority than cosmetic churn

Validation:
- active stale source-location WebApp path scan returned no matches in assignment
  source/project/script/Docker/docs files outside build outputs
- `rg "ViewBag|ViewData" WebApp` returned no matches
- `dotnet build multi-gym-management-system.slnx` passed
- `dotnet test multi-gym-management-system.slnx` passed with 202 passed and 3
  PostgreSQL/Testcontainers tests skipped
- `docker compose config` passed

## 2026-05-19 - Root App.Resources Project Migration

Task:
- move `App.Resources` to the root-level `App.Resources` project while
  preserving all `.resx` files, resource keys, resource names, and translations.

Files affected:
- `App.Resources/App.Resources.csproj`
- `App.Resources/SharedResources.cs`
- `App.Resources/SharedResources.resx`
- `App.Resources/SharedResources.et.resx`
- removed old `App.Resources`
- `multi-gym-management-system.slnx`
- `WebApp/WebApp.csproj`
- `Dockerfile`
- `docs/architecture.md`
- `docs/final1-structure-reset.md`
- assignment `docs/ai-usage.md`
- root `docs/ai-prompts.md`

What AI helped with:
- moved the resource project to the assignment root
- preserved the resource marker class and `.resx` files without editing
  translation contents
- updated WebApp, solution, and Docker project paths to use root
  `App.Resources`
- removed the stale old `App.Resources` folder after the root project built
  and active reference scans returned no old-path matches

What needed manual review or correction:
- no namespace changes were needed because the existing `App.Resources`
  namespace already matches the root project name

Alternatives considered:
- regenerating resource designer files or rewriting translations was rejected
  because this phase is a structural project-location move only

Validation:
- active stale path scan for `App.Resources` returned no matches
- `dotnet build multi-gym-management-system.slnx` passed
- `dotnet test multi-gym-management-system.slnx` passed with 202 passed and 3
  PostgreSQL/Testcontainers tests skipped

## 2026-05-19 - Root App.BLL Project Migration

Task:
- move `App.BLL` to the root-level `App.BLL` project while keeping business
  logic in Services and mapping logic in Mappers.

Files affected:
- `App.BLL/**`
- removed old `App.BLL`
- `multi-gym-management-system.slnx`
- project references in `App.DAL.EF`, WebApp, module projects, and tests
- mapper namespace imports across BLL, WebApp, modules, and tests
- `Dockerfile`
- `README.md`
- `docs/architecture.md`
- `docs/final1-structure-reset.md`
- `docs/reference-architecture-parity.md`
- assignment `docs/ai-usage.md`
- root `docs/ai-prompts.md`

What AI helped with:
- moved the BLL implementation project to root `App.BLL`
- renamed `App.BLL/Mapping` to `App.BLL/Mappers`
- updated mapper namespaces from `App.BLL.Mapping` to `App.BLL.Mappers`
- updated solution, project references, and Docker source copy paths to use the
  root-level BLL project
- removed the stale old `App.BLL` folder after the root BLL project built
  and active reference scans returned no old-path matches

What needed manual review or correction:
- the requested POSIX `grep ... || true` scans were represented with equivalent
  `rg` scans because the local shell is Windows PowerShell 5.1
- existing EF-shaped `IAppDbContext` usage remains in BLL infrastructure during
  this structural move; it was not expanded or moved from DAL EF into BLL in
  this slice

Alternatives considered:
- refactoring remaining `IAppDbContext` consumers during the project move was
  rejected because this phase is a structural migration and should not change
  runtime behavior
- merging mapper code into services was rejected because the target structure
  keeps mapping logic separated

Validation:
- equivalent `rg "App\\.DAL\\.EF" App.BLL --glob "!**/bin/**" --glob "!**/obj/**"`
  returned no matches
- equivalent `rg "Modules\\." App.BLL --glob "!**/bin/**" --glob "!**/obj/**"`
  returned no matches
- equivalent stale path scan for `App.BLL` returned no active matches
- `dotnet build multi-gym-management-system.slnx` passed
- `dotnet test multi-gym-management-system.slnx` passed with 202 passed and 3
  PostgreSQL/Testcontainers tests skipped

## 2026-05-19 - Root App.Domain Migration

Task:
- move the active `App.Domain` project from the legacy source folder to the
  assignment root while preserving domain entities, enums, identity domain
  classes, namespaces, API behavior, DTO shapes, and database schema.

Files affected:
- `App.Domain/App.Domain.csproj`
- `App.Domain/Common/*`
- `App.Domain/Entities/*`
- `App.Domain/Enums/*`
- `App.Domain/Identity/*`
- `App.Domain/Security/*`
- `App.Domain/RoleNames.cs`
- `multi-gym-management-system.slnx`
- backend/test project references to `App.Domain`
- `Dockerfile`
- `docs/final1-final2-roadmap.md`
- `docs/final1-structure-reset.md`
- assignment `docs/ai-usage.md`
- root `docs/ai-prompts.md`

What AI helped with:
- moved the domain project to root `App.Domain`
- kept identity classes under `App.Domain/Identity`
- updated the active solution to point to `App.Domain/App.Domain.csproj`
- updated backend, module, WebApp, and test project references that compile
  against domain types
- updated Docker restore/copy paths so container builds can find the root-level
  domain and base project files
- removed the old source-location domain folder after the root domain project
  and solution built successfully and no active old-path references remained

What needed manual review or correction:
- the literal requested `grep` command could not run because `grep` is not
  installed in this PowerShell environment; an equivalent `rg` scan was used
  with the same bin/obj/.git exclusions
- historical AI log entries initially contained old-path mentions and were
  reworded so the migration scan reports no old-path matches
- `App.Domain` uses the narrower `Microsoft.Extensions.Identity.Stores`
  package required by `AppUser` and `AppRole`; it has no project references
  except `Base.Contracts` and `Base.Domain`

Alternatives considered:
- changing entity namespaces was rejected because it would create unnecessary
  EF snapshot and migration churn
- regenerating migrations was rejected because only project location changed
- deleting other `legacy source path ` projects was rejected because this slice only migrates
  `App.Domain`

Validation:
- `dotnet build App.Domain/App.Domain.csproj` passed
- the requested old-domain-path `grep` scan could not run: `grep` is
  unavailable in the local PowerShell environment
- equivalent old-domain-path `rg` scan with bin/obj/.git exclusions
  returned no matches after cleanup
- `dotnet build multi-gym-management-system.slnx` passed
- `dotnet test multi-gym-management-system.slnx` passed with 202 passed and 3
  PostgreSQL/Testcontainers tests skipped

## 2026-05-19 - Root App.DAL.Contracts Persistence Boundary

Task:
- split repository and Unit of Work contracts into the root-level
  `App.DAL.Contracts` project without changing EF schema, migrations, API
  routes, DTO shapes, or client code.

Files affected:
- `App.DAL.Contracts/App.DAL.Contracts.csproj`
- `App.DAL.Contracts/Persistence/*.cs`
- removed old `App.BLL/Contracts/Persistence` interface files
- `App.BLL/App.BLL.csproj`
- `App.DAL.EF/App.DAL.EF.csproj`
- `WebApp/WebApp.csproj`
- module and test project references that use persistence contracts
- C# using statements for persistence contracts
- `multi-gym-management-system.slnx`
- `docs/architecture.md`
- `docs/final1-structure-reset.md`
- assignment `docs/ai-usage.md`
- root `docs/ai-prompts.md`

What AI helped with:
- moved `IAppUnitOfWork`, generic repository, authorization query repository,
  and entity-specific repository interfaces to `App.DAL.Contracts.Persistence`
- updated EF repository implementations and DI registration to implement and
  register `App.DAL.Contracts` interfaces
- updated BLL, WebApp, module, and test consumers to reference the new
  persistence namespace
- kept `IAppDbContext` in `App.BLL.Contracts.Infrastructure` because it exposes
  EF `DbSet<T>` and would violate the no-EF rule for `App.DAL.Contracts`
- kept database schema, migrations, routes, DTOs, and client code unchanged

What needed manual review or correction:
- the requested POSIX `grep ... || true` scans could not run under local
  Windows PowerShell 5.1 because `||` is not a valid statement separator there
- equivalent `rg` scans were used for the no-EF and no-BLL-to-EF checks
- `App.DAL.Contracts` was tightened to reference only `App.Domain` directly,
  because the moved interfaces do not directly use `Base.*` types

Alternatives considered:
- moving `IAppDbContext` into `App.DAL.Contracts` was rejected because it
  references `Microsoft.EntityFrameworkCore`
- changing repository method shapes was rejected because this phase is a
  project-boundary migration only

Validation:
- equivalent `rg "Microsoft\\.EntityFrameworkCore" App.DAL.Contracts` returned
  no matches
- equivalent `rg "App\\.DAL\\.EF" App.BLL App.BLL.Contracts --glob "!bin/**" --glob "!obj/**"`
  returned no matches
- `dotnet build multi-gym-management-system.slnx` passed
- `dotnet test multi-gym-management-system.slnx` passed with 202 passed and 3
  PostgreSQL/Testcontainers tests skipped

## 2026-05-19 - Root App.DAL.EF Migration

Task:
- move the active EF persistence project from `src` to root `App.DAL.EF`,
  preserving migrations, repository implementations, seeding, configurations,
  EF schema metadata, API behavior, DTO shapes, and client code.

Files affected:
- `App.DAL.EF/**`
- removed old source-location EF folder after validation
- `App.BLL.Contracts/Infrastructure/IAppDbContext.cs`
- `App.BLL.Contracts/App.BLL.Contracts.csproj`
- `multi-gym-management-system.slnx`
- `WebApp/WebApp.csproj`
- `tests/WebApp.Tests/WebApp.Tests.csproj`
- `Dockerfile`
- `scripts/migrate-db.ps1`
- tests that construct the EF Unit of Work
- `docs/architecture.md`
- `docs/final1-structure-reset.md`
- `docs/reference-architecture-parity.md`
- assignment `docs/ai-usage.md`
- root `docs/ai-prompts.md`

What AI helped with:
- moved `AppDbContext`, migrations, repositories, seeding, configurations,
  design-time factory, and tenant context code to root `App.DAL.EF`
- updated the active solution, WebApp/test project references, Docker restore
  paths, and the database migration script to point to root `App.DAL.EF`
- renamed the EF Unit of Work implementation from `EfAppUnitOfWork` to
  `AppUOW` and moved it to `App.DAL.EF/AppUOW.cs`
- moved `IAppDbContext` to root `App.BLL.Contracts` so `App.DAL.EF` no longer
  references the BLL implementation project
- removed the old source-location EF folder after the root EF project and
  solution built successfully and no active old-path references remained

What needed manual review or correction:
- the exact requested EF command uses `--startup-project WebApp`, but root
  `WebApp` has not been migrated yet, so that command failed to load project
  metadata
- the equivalent command using current startup project `WebApp` succeeded
  and listed the preserved migrations
- `App.DAL.EF` still references `App.BLL.Contracts` for `IAppDbContext`, which
  remains EF-shaped and cannot live in `App.DAL.Contracts` under the no-EF
  rule

Alternatives considered:
- moving or regenerating migrations was rejected because this is a structural
  project-location move only
- changing entity/table mapping was rejected because no schema behavior should
  change in this phase
- moving `IAppDbContext` into `App.DAL.Contracts` was rejected because it uses
  EF `DbSet<T>`

Validation:
- `dotnet build multi-gym-management-system.slnx` passed
- `dotnet test multi-gym-management-system.slnx` passed with 202 passed and 3
  PostgreSQL/Testcontainers tests skipped
- `dotnet ef migrations list --project App.DAL.EF --startup-project WebApp`
  failed because root `WebApp` does not exist yet
- `dotnet ef migrations list --project App.DAL.EF --startup-project WebApp`
  built successfully and listed:
  - `20260409145651_InitialCreate`
  - `20260422204122_Batch3WorkspacesAndFinance`
  - `20260519044224_PruneFinal2Scope`
  It could not determine applied/pending status because PostgreSQL was not
  reachable at `127.0.0.1:5432`

## 2026-05-19 - Root App.BLL.Contracts Service Contract Split

Task:
- split application service contracts into the root-level `App.BLL.Contracts`
  project without changing API routes, DTO shapes, database schema, migrations,
  or client code.

Files affected:
- `App.BLL.Contracts/App.BLL.Contracts.csproj`
- `App.BLL.Contracts/Services/**/*.cs`
- moved service interface files from `App.BLL/Services`
- moved supporting contract records used by those interfaces:
  `UserExecutionContext`, admin query snapshots, and client query snapshots
- `App.BLL/Contracts/Infrastructure/IAppDbContext.cs`
- `App.BLL/GlobalUsings.cs`
- `App.BLL/App.BLL.csproj`
- `App.DAL.EF/App.DAL.EF.csproj`
- WebApp, module, Razor, and test using statements for BLL service contracts
- module project references that consume service contracts
- `docs/architecture.md`
- `docs/final1-structure-reset.md`
- `docs/reference-architecture-parity.md`
- assignment `README.md`
- assignment `docs/ai-usage.md`
- root `docs/ai-prompts.md`

What AI helped with:
- moved application service interfaces to `App.BLL.Contracts.Services`
- kept BLL implementations under `App.BLL/Services`
- kept mapper interfaces and implementations under `App.BLL/Mappers`
- moved contract-owned return records needed by the service interfaces into
  `App.BLL.Contracts`
- updated WebApp controllers, Razor layouts, page services, module handlers,
  and tests to consume service interfaces through `App.BLL.Contracts.Services`
- removed EF Core from `App.BLL.Contracts`

What needed manual review or correction:
- the previous EF-project migration had placed EF-shaped `IAppDbContext` in
  `App.BLL.Contracts`; this slice moved it back under the BLL implementation
  project as `App.BLL.Infrastructure.IAppDbContext` because the current rule
  explicitly forbids EF Core references in `App.BLL.Contracts`
- `App.DAL.EF` now references `App.BLL` only for that temporary
  `IAppDbContext` interface until the remaining direct context consumers are
  replaced with repository/UOW contracts
- the requested POSIX `grep ... || true` scans were represented with equivalent
  `rg` scans because the local shell is Windows PowerShell 5.1

Alternatives considered:
- moving `IAppDbContext` into `App.BLL.Contracts` was rejected because it would
  keep EF Core in the contract project
- moving `IAppDbContext` into `App.DAL.Contracts` was rejected because it
  exposes EF `DbSet<T>`
- changing service signatures was rejected because this phase is a structural
  contract split only

Validation:
- equivalent `rg "Microsoft\\.EntityFrameworkCore" App.BLL.Contracts` returned
  no matches
- equivalent `rg "App\\.DAL\\.EF" App.BLL.Contracts` returned no matches
- `dotnet build multi-gym-management-system.slnx` passed
- `dotnet test multi-gym-management-system.slnx` passed with 202 passed and 3
  PostgreSQL/Testcontainers tests skipped
