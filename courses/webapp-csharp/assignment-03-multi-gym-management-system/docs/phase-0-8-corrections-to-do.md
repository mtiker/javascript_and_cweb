# Phase 0-8 Corrections To Do

## Audit Summary

- Audit date: 2026-04-28
- Branch name if detectable: `main`
- Commit hash if detectable: `96a328f`
- Working tree state: dirty, with many pre-existing modified and untracked Assignment 3 files. This report audits the current working tree, not only the committed hash.
- Overall status: Partial
- Highest-risk remaining issues:
  - Phase 0 evidence and contract-freeze documents are stale or contradict the current implementation.
  - Separate React deployment is only partially defensible: build artifacts exist, but live separate-host smoke evidence was not found and CI deploy is optional/manual.
  - React standalone subpath routing is risky because Vite builds under `/client/` while React Router is configured without a matching basename.
  - PostgreSQL provider tests are skipped by default, leaving production database behavior weaker than the in-memory/SQLite test evidence.
  - Several audit documents still describe pre-Phase 7 MVC and IDOR gaps as current even though code/tests appear to have moved on.
- Whether public API contracts appear stable: Partially. The implemented DTOs and tests appear stable, but `docs/api-contract-freeze.md` is out of sync with the actual request/response models.
- Whether Assignment 3 evidence appears defensible: Partially. The implementation and automated tests are stronger than the current evidence documents, but stale docs would be difficult to defend as-is.
- Whether separate React deployment appears defensible: Partially. `client/Dockerfile`, `client/nginx.conf`, `docker-compose.prod.yml`, and CI jobs exist, and `npm run build` passes, but deployed separate-host behavior was not verified and routing/CI gaps remain.
- Whether automated tests are sufficient or have gaps: Mostly strong for backend auth, tenant isolation, CRUD, MVC, and React component/API-client behavior. Gaps remain for production deployment, separate-client deep links, live CORS, Swagger/health smoke checks, and default PostgreSQL execution.

Validation commands run:

```powershell
dotnet build multi-gym-management-system.slnx
dotnet test multi-gym-management-system.slnx --no-restore
npm test
npm run build
```

Observed results:

- `dotnet build multi-gym-management-system.slnx`: passed, 0 warnings, 0 errors.
- `dotnet test multi-gym-management-system.slnx --no-restore`: passed, 130 passed, 3 skipped. Skipped tests were PostgreSQL persistence tests gated by `RUN_POSTGRES_TESTS=1`.
- `npm test`: passed, 7 files, 34 tests. React Router v7 future warnings were emitted.
- `npm run build`: passed.

Not verified:

- Running backend server locally.
- Swagger reachable in a live browser/session.
- Health endpoint reachable from a live process.
- Docker Compose production deployment.
- Separate client container runtime behavior.
- Public proxy URLs.
- Production CORS preflight against a deployed backend.

## Severity Legend

- Critical: likely assignment failure, security issue, broken build, broken deployment, or broken core flow
- High: major missing evidence, missing automated tests, serious compliance risk
- Medium: incomplete docs, weak test coverage, inconsistent behavior, maintainability risk
- Low: cleanup, clarity, minor docs, small DX improvement

## Phase-by-Phase Corrections

### Phase 0: Assignment 3 evidence and contract freeze

#### Status

Partial

#### Findings

##### Finding 0.1: Assignment compliance document contradicts current MVC evidence

- Severity: High
- Area: Assignment evidence documentation
- Evidence: `docs/assignment-compliance.md` still describes `R8 Admin MVC area` as Partial because most Admin pages redirect to React, and `R9 ViewBag/ViewData` as Missing. Current code and tests include MVC Admin controllers/views and ViewBag/ViewData guard tests, including `src/WebApp/Areas/Admin/Controllers/MembersController.cs`, `src/WebApp/Areas/Admin/Views/Members/Index.cshtml`, `src/WebApp/Models/AdminMembersPageViewModel.cs`, and `tests/WebApp.Tests/Integration/MvcComplianceTests.cs`.
- Why this matters: Defense evidence would appear to admit missing MVC Admin compliance even though later work added it.
- Likely cause: Phase 0 evidence docs were not updated after Phase 7.
- Recommended correction: Update `docs/assignment-compliance.md` to reflect the current MVC Admin pages, MVC Client routes, view-model usage, and ViewBag/ViewData tests. Keep any remaining limitations explicit.
- Files likely involved: `docs/assignment-compliance.md`, `docs/mvc-admin-audit.md`, `docs/mvc-client-audit.md`, `docs/viewmodel-audit.md`, `docs/no-viewbag-viewdata-audit.md`
- Tests to add or update: None required if existing MVC compliance tests remain accurate.
- Validation commands:
  - `dotnet test multi-gym-management-system.slnx --no-restore --filter MvcComplianceTests`
  - `rg "React|ViewBag|ViewData|Admin MVC" docs/assignment-compliance.md`
- Estimated Codex phase size:
  - Small: 1-3 files
- Blocks Assignment 3:
  - Maybe

##### Finding 0.2: API contract freeze does not match current DTOs

- Severity: High
- Area: Public API contract documentation
- Evidence: `docs/api-contract-freeze.md` documents register request fields as `email`, `password`, and `displayName`, but `src/App.DTO/v1/Identity/RegisterRequest.cs` uses `Email`, `Password`, `FirstName`, and `LastName`. The same document omits `expiresInSeconds` and `availableTenants` from `JwtResponse`, while `src/App.DTO/v1/Identity/JwtResponse.cs` includes them. It also documents `switch-gym` with `{ gymCode, role }`, but `src/App.DTO/v1/Identity/SwitchGymRequest.cs` has only `GymCode`; `switch-role` is documented with `{ role }`, but `src/App.DTO/v1/Identity/SwitchRoleRequest.cs` uses `RoleName`.
- Why this matters: The contract-freeze document is supposed to prevent endpoint drift. If it is stale, it becomes misleading evidence and can cause client/API mismatch during defense.
- Likely cause: Auth DTOs evolved after the contract freeze document was written.
- Recommended correction: Update the contract freeze document from the actual DTOs, controllers, and tests. Do not change the DTOs unless a separate product decision requires a breaking API change.
- Files likely involved: `docs/api-contract-freeze.md`, `src/App.DTO/v1/Identity/RegisterRequest.cs`, `src/App.DTO/v1/Identity/JwtResponse.cs`, `src/App.DTO/v1/Identity/SwitchGymRequest.cs`, `src/App.DTO/v1/Identity/SwitchRoleRequest.cs`
- Tests to add or update: Add contract assertions only if current tests do not already verify serialized login/switch responses.
- Validation commands:
  - `dotnet test multi-gym-management-system.slnx --no-restore --filter Account`
  - `rg "displayName|expiresInSeconds|availableTenants|switch-gym|switch-role" docs/api-contract-freeze.md src/App.DTO tests`
- Estimated Codex phase size:
  - Small: 1-3 files
- Blocks Assignment 3:
  - Maybe

##### Finding 0.3: Deployment inventory is stale after separate-client work

- Severity: Medium
- Area: Deployment evidence documentation
- Evidence: `docs/current-deployment-inventory.md` is dated 2026-04-27 and still emphasizes embedded React under the backend. Current Phase 8 artifacts include `client/Dockerfile`, `client/nginx.conf`, `.gitlab-ci.yml` client jobs, `scripts/deploy-client.sh`, and a `client` service in `docker-compose.prod.yml`.
- Why this matters: The inventory is used as baseline evidence. It currently understates the separate-client work and can conflict with `docs/separate-client-hosting-audit.md`.
- Likely cause: Phase 8 added deployment artifacts without updating Phase 0 inventory.
- Recommended correction: Update the inventory to distinguish legacy embedded `/client` hosting from separate static-client hosting and document what was actually verified.
- Files likely involved: `docs/current-deployment-inventory.md`, `docs/separate-client-hosting-audit.md`, `docs/deployment.md`
- Tests to add or update: None.
- Validation commands:
  - `rg "embedded|separate|client" docs/current-deployment-inventory.md docs/separate-client-hosting-audit.md docs/deployment.md`
- Estimated Codex phase size:
  - Small: 1-3 files
- Blocks Assignment 3:
  - No

### Phase 1: Build, runtime, and local startup rescue

#### Status

Partial

#### Findings

##### Finding 1.1: Local run audit has stale command results and backend URLs

- Severity: Medium
- Area: Local startup documentation
- Evidence: `docs/local-run-audit.md` records older test totals such as 67 backend tests and 25 React tests. Current observed results are 130 backend tests passed with 3 skipped, and 34 React tests passed. The same document describes default backend URLs as `http://localhost:5000` and `https://localhost:5001`, while `src/WebApp/Properties/launchSettings.json` uses `http://localhost:5107` and `https://localhost:7245;http://localhost:5107`.
- Why this matters: Defense startup instructions should not require hidden knowledge or contradict the actual launch profile.
- Likely cause: Tests and launch settings changed after the local-run audit was written.
- Recommended correction: Refresh `docs/local-run-audit.md` with current command outputs, launch profile URLs, Swagger URL, health URL, and React URL.
- Files likely involved: `docs/local-run-audit.md`, `src/WebApp/Properties/launchSettings.json`
- Tests to add or update: None.
- Validation commands:
  - `dotnet build multi-gym-management-system.slnx`
  - `dotnet test multi-gym-management-system.slnx --no-restore`
  - `npm test`
  - `npm run build`
- Estimated Codex phase size:
  - Small: 1-3 files
- Blocks Assignment 3:
  - Maybe

##### Finding 1.2: Secret and environment documentation needs one clear source of truth

- Severity: Low
- Area: Local configuration documentation
- Evidence: `docs/dev-secrets-audit.md` says required values are present in committed configuration and no user secrets are needed for local audit. Other setup/deployment documents still reference user secrets and environment variables for JWT, database, CORS, and admin seed values.
- Why this matters: Mixed guidance increases setup risk during defense.
- Likely cause: Local development and production deployment guidance were written in separate phases.
- Recommended correction: Make the docs explicit: committed development values are only for local non-production use, while production/staging values must come from environment variables or secrets.
- Files likely involved: `docs/dev-secrets-audit.md`, `README.md`, `docs/deployment.md`
- Tests to add or update: None.
- Validation commands:
  - `rg "user-secrets|JWT|ConnectionStrings|CORS_ALLOWED" docs README.md src/WebApp/appsettings*.json`
- Estimated Codex phase size:
  - Small: 1-3 files
- Blocks Assignment 3:
  - No

### Phase 2: Auth, refresh token, logout, and CORS rescue

#### Status

Pass

#### Findings

##### Finding 2.1: Auth-flow audit wording is slightly stale for malformed JWT handling

- Severity: Low
- Area: Auth evidence documentation
- Evidence: `docs/auth-flow-audit.md` describes the refresh-token fix as catching `SecurityTokenException`. Current `src/App.BLL/Services/IdentityService.cs` catches `SecurityTokenException or ArgumentException` around expired-token principal extraction.
- Why this matters: The code appears stronger than the document, but the audit evidence should precisely describe the implemented failure path.
- Likely cause: Error handling was broadened after the doc text was written.
- Recommended correction: Update the audit wording to include malformed-token `ArgumentException` handling.
- Files likely involved: `docs/auth-flow-audit.md`, `src/App.BLL/Services/IdentityService.cs`
- Tests to add or update: None if invalid refresh-token tests already cover malformed tokens.
- Validation commands:
  - `dotnet test multi-gym-management-system.slnx --no-restore --filter Refresh`
  - `rg "ArgumentException|SecurityTokenException|renew-refresh-token" docs/auth-flow-audit.md src tests`
- Estimated Codex phase size:
  - Small: 1-3 files
- Blocks Assignment 3:
  - No

### Phase 3: Tenant IDOR and role authorization rescue

#### Status

Partial

#### Findings

##### Finding 3.1: IDOR and tenant isolation documents still report a fixed member lookup gap

- Severity: Medium
- Area: Security evidence documentation
- Evidence: `docs/idor-audit.md` and `docs/tenant-isolation-audit.md` still describe member get/update/delete lookups as fetching by primary key only. Current `src/App.BLL/Services/MemberWorkflowService.cs` filters member get/update/delete queries by both `GymId` and `Id`, and `tests/App.Tests/Integration/MemberCrudTests.cs` includes `UpdateMember_ForeignGymMemberId_Returns404`.
- Why this matters: Defense docs currently make tenant isolation look weaker than the implementation and tests.
- Likely cause: Security audit docs were not updated after the member workflow fix.
- Recommended correction: Update the IDOR and tenant-isolation audits with current code and test evidence. Keep any remaining tenant gaps separate and concrete.
- Files likely involved: `docs/idor-audit.md`, `docs/tenant-isolation-audit.md`, `src/App.BLL/Services/MemberWorkflowService.cs`, `tests/App.Tests/Integration/MemberCrudTests.cs`
- Tests to add or update: Consider adding get/delete foreign-gym member tests if only update is currently explicit.
- Validation commands:
  - `dotnet test multi-gym-management-system.slnx --no-restore --filter MemberCrudTests`
  - `rg "GetMemberAsync|UpdateMemberAsync|DeleteMemberAsync|ForeignGym" docs src/App.BLL tests`
- Estimated Codex phase size:
  - Small: 1-3 files
- Blocks Assignment 3:
  - Maybe

##### Finding 3.2: PostgreSQL tenant-isolation evidence is skipped by default

- Severity: Medium
- Area: Production database validation
- Evidence: `dotnet test multi-gym-management-system.slnx --no-restore` passed with 3 skipped tests: PostgreSQL persistence tests gated by `RUN_POSTGRES_TESTS=1`.
- Why this matters: Assignment 3 deployment uses PostgreSQL, so provider-specific query, collation, migration, and persistence behavior is weaker than the default test result suggests.
- Likely cause: PostgreSQL tests require Docker/Testcontainers or a local database and are intentionally opt-in.
- Recommended correction: Either run and document the PostgreSQL test command before defense, or add a CI/manual job that executes it with required environment support.
- Files likely involved: `tests/App.Tests/Persistence/PostgreSqlPersistenceTests.cs`, `.gitlab-ci.yml`, `docs/database-migration-audit.md`, `docs/current-test-inventory.md`
- Tests to add or update: No new test may be required; the existing skipped tests need a verified execution path.
- Validation commands:
  - `$env:RUN_POSTGRES_TESTS='1'; dotnet test multi-gym-management-system.slnx --no-restore --filter PostgreSqlPersistenceTests`
- Estimated Codex phase size:
  - Medium: 4-6 files
- Blocks Assignment 3:
  - Maybe

### Phase 4: Members CRUD vertical slice

#### Status

Partial

#### Findings

##### Finding 4.1: Member CRUD audit still says MVC Admin member page is missing

- Severity: High
- Area: CRUD evidence documentation
- Evidence: `docs/member-crud-audit.md` still states that there is no Admin Members page and no MVC Admin member tests. Current files include `src/WebApp/Areas/Admin/Controllers/MembersController.cs`, `src/WebApp/Areas/Admin/Views/Members/Index.cshtml`, `src/WebApp/Models/AdminMembersPageViewModel.cs`, and `tests/WebApp.Tests/Integration/AdminMembersPageTests.cs`.
- Why this matters: Members CRUD is one of the required React CRUD entities and also contributes to MVC Admin evidence. The stale audit makes the completed vertical slice look incomplete.
- Likely cause: Phase 4 documentation was not refreshed after Phase 7 MVC work.
- Recommended correction: Update the member audit to include API, React, MVC Admin, ViewModel, ViewBag/ViewData, and authorization evidence.
- Files likely involved: `docs/member-crud-audit.md`, `docs/member-tests-map.md`, `docs/mvc-admin-audit.md`
- Tests to add or update: None required unless the audit identifies missing delete or validation state coverage.
- Validation commands:
  - `dotnet test multi-gym-management-system.slnx --no-restore --filter "MemberCrudTests|AdminMembersPageTests"`
  - `npm test -- members`
  - `rg "Admin Members|MembersController|AdminMembersPageTests" docs src tests`
- Estimated Codex phase size:
  - Small: 1-3 files
- Blocks Assignment 3:
  - Maybe

### Phase 5: Training categories + DB i18n vertical slice

#### Status

Partial

#### Findings

##### Finding 5.1: Localization boundary is not strict enough for defense claims

- Severity: Medium
- Area: Localization evidence
- Evidence: `docs/localization-audit.md` notes that some older workflow copy remains English. Current tests verify DB localization and React language behavior, but the audit does not clearly separate fully localized demo surfaces from known English-only surfaces.
- Why this matters: Assignment evidence requires UI i18n and DB i18n. Any broad claim of complete UI localization is risky while active UI copy remains English.
- Likely cause: DB i18n and route-level language support were prioritized over exhaustive UI string cleanup.
- Recommended correction: Either localize the active defense/demo surfaces or document the exact UI localization boundary with file-level evidence and known limitations.
- Files likely involved: `docs/localization-audit.md`, `client/src`, `src/WebApp/Resources`
- Tests to add or update: Add focused UI assertions for the required localized pages if not already present.
- Validation commands:
  - `dotnet test multi-gym-management-system.slnx --no-restore --filter Localization`
  - `npm test -- localization`
  - `rg '"[A-Za-z][^"]*"' client/src src/WebApp/Areas/Admin/Views src/WebApp/Views`
- Estimated Codex phase size:
  - Medium: 4-6 files
- Blocks Assignment 3:
  - Maybe

### Phase 6: Membership packages CRUD vertical slice

#### Status

Pass

#### Findings

No correction items found with current evidence.

### Phase 7: MVC admin and MVC client compliance pass

#### Status

Partial

#### Findings

##### Finding 7.1: Admin anti-forgery evidence is currently a guardrail, not proof of POST behavior

- Severity: Low
- Area: MVC security tests
- Evidence: `tests/WebApp.Tests/Integration/MvcComplianceTests.cs` contains source-scan coverage for Admin POST anti-forgery attributes. Current Admin controllers appear to be GET/read-only pages, so the test can pass without exercising a real Admin POST action.
- Why this matters: The Phase 7 requirement says Admin POST actions use anti-forgery. If there are no Admin POST actions, the evidence should say that explicitly rather than implying runtime POST coverage.
- Likely cause: MVC Admin pages were implemented as read-only evidence pages.
- Recommended correction: Update `docs/mvc-admin-audit.md` and `docs/no-viewbag-viewdata-audit.md` to state that current Admin controllers have no mutation POST actions, and that the source-scan test is a regression guard for future POST additions.
- Files likely involved: `docs/mvc-admin-audit.md`, `docs/no-viewbag-viewdata-audit.md`, `tests/WebApp.Tests/Integration/MvcComplianceTests.cs`
- Tests to add or update: None unless Admin mutation POST actions are later added.
- Validation commands:
  - `dotnet test multi-gym-management-system.slnx --no-restore --filter MvcComplianceTests`
  - `rg "\[HttpPost\]|ValidateAntiForgeryToken|AutoValidateAntiforgeryToken" src/WebApp/Areas/Admin tests/WebApp.Tests`
- Estimated Codex phase size:
  - Small: 1-3 files
- Blocks Assignment 3:
  - No

##### Finding 7.2: GymOwner MVC Admin access evidence may depend on a system-admin seeded user

- Severity: Medium
- Area: Role authorization tests
- Evidence: `MvcComplianceTests.GymAdminOrGymOwner_CanAccess_TenantAdminPages` uses `systemadmin@gym.local` for the GymOwner case. This can be valid if that seeded user has an active tenant GymOwner role, but it is weaker evidence than a dedicated tenant-only GymOwner login.
- Why this matters: The requirement distinguishes tenant Admin access for GymAdmin/GymOwner from system-level access. A system-admin identity can make the role boundary harder to defend verbally.
- Likely cause: Existing seed users were reused for MVC role coverage.
- Recommended correction: Add or document a dedicated tenant GymOwner test user, or add a test assertion proving the active tenant role context is GymOwner and not relying on SystemAdmin privileges.
- Files likely involved: `tests/WebApp.Tests/Integration/MvcComplianceTests.cs`, `tests/WebApp.Tests/Integration/CustomWebAppFactory.cs`, seed data under `src/App.DAL.EF`
- Tests to add or update: Add a non-system GymOwner MVC Admin access test if seed data supports it.
- Validation commands:
  - `dotnet test multi-gym-management-system.slnx --no-restore --filter GymAdminOrGymOwner`
  - `rg "systemadmin@gym.local|GymOwner|GymAdmin" tests src`
- Estimated Codex phase size:
  - Medium: 4-6 files
- Blocks Assignment 3:
  - Maybe

### Phase 8: Separate client deployment and CI/CD split

#### Status

Partial

#### Findings

##### Finding 8.1: Standalone React subpath routing is risky

- Severity: High
- Area: Separate React deployment
- Evidence: `client/vite.config.ts` sets `base: "/client/"`, `client/src/App.tsx` uses `BrowserRouter` without a `basename`, and `client/nginx.conf` serves the SPA under `/client/`. Deployment docs ask for smoke checks such as `/client/members`. With this combination, direct browser loads at `/client/login` or `/client/members` can reach React as `/client/login` or `/client/members`, while the route table is defined as `/login`, `/members`, and similar root-relative paths.
- Why this matters: A separate client deployment can pass `npm run build` while still failing direct deep links, refreshes, or bookmarked routes.
- Likely cause: The client supports embedded backend hosting under `/client/` and separate hosting, but React Router was not given an explicit deployment basename strategy.
- Recommended correction: In a later fix phase, choose one route strategy and verify it with a container/browser smoke test: either add a matching basename for `/client`, change the static server to rewrite to root paths consistently, or build separate artifacts for root-hosted and `/client`-hosted modes.
- Files likely involved: `client/src/App.tsx`, `client/vite.config.ts`, `client/nginx.conf`, `docs/separate-client-hosting-audit.md`, `docs/deployment.md`
- Tests to add or update: Add a Playwright or static-server smoke test for direct loads at `/client/login`, `/client/members`, and `/client/member-workspace`.
- Validation commands:
  - `npm run build`
  - `docker build -f client/Dockerfile -t multi-gym-client-test client`
  - `docker run --rm -p 8081:8080 multi-gym-client-test`
  - `curl -I http://localhost:8081/client/login`
  - Browser smoke test direct deep links under `/client/*`
- Estimated Codex phase size:
  - Medium: 4-6 files
- Blocks Assignment 3:
  - Maybe

##### Finding 8.2: Separate client deployment job is optional and allowed to fail

- Severity: High
- Area: CI/CD evidence
- Evidence: `.gitlab-ci.yml` defines `assignment03_deploy_client` as `when: manual` and `allow_failure: true`.
- Why this matters: A passing pipeline does not prove that separate React deployment works or even ran.
- Likely cause: Client deployment was added conservatively to avoid blocking backend deployment.
- Recommended correction: For defense, either make the separate client deployment a required pipeline path for the deployment environment or provide a documented manual deployment run with command output and smoke evidence.
- Files likely involved: `.gitlab-ci.yml`, `docs/cicd-audit.md`, `docs/deployment.md`
- Tests to add or update: Add a CI smoke step for the built client image or deployed client URL.
- Validation commands:
  - GitLab pipeline run including `assignment03_client_image`
  - GitLab manual run of `assignment03_deploy_client`
  - `curl -I <client-url>/client/`
- Estimated Codex phase size:
  - Medium: 4-6 files
- Blocks Assignment 3:
  - Maybe

##### Finding 8.3: Client image job is not clearly consumed by deployment

- Severity: Medium
- Area: CI/CD and deployment
- Evidence: `.gitlab-ci.yml` builds a client Docker image, while `scripts/deploy-client.sh` runs Docker Compose with `--build` on the deployment host. No evidence was found that the CI-built image is pushed to a registry and pulled by deployment.
- Why this matters: The CI package step may not represent the artifact that is actually deployed.
- Likely cause: Deployment relies on building from the checked-out repository on the host instead of promoting a CI artifact.
- Recommended correction: Either document that host-side build is the intended deployment model, or change the pipeline later to push/pull a versioned client image.
- Files likely involved: `.gitlab-ci.yml`, `scripts/deploy-client.sh`, `docker-compose.prod.yml`, `docs/cicd-audit.md`, `docs/deployment.md`
- Tests to add or update: Add image smoke validation after the exact artifact build path.
- Validation commands:
  - `docker compose -f docker-compose.prod.yml --profile client build client`
  - `docker compose -f docker-compose.prod.yml --profile client up -d client`
- Estimated Codex phase size:
  - Medium: 4-6 files
- Blocks Assignment 3:
  - No

##### Finding 8.4: No live separate-client URL smoke evidence was found

- Severity: High
- Area: Deployment evidence
- Evidence: `docs/deployment.md` and Phase 8 docs reference separate backend/client deployment concepts, but this audit did not find concrete successful command output for the public client URL, backend health URL, Swagger URL, or production CORS preflight.
- Why this matters: Assignment 3 deployment evidence needs to show that the separated client can actually reach the deployed API.
- Likely cause: Deployment documentation was prepared before or without a final live smoke run.
- Recommended correction: Add a dated smoke-test section with exact commands and results after running against the real deployment.
- Files likely involved: `docs/deployment.md`, `docs/separate-client-hosting-audit.md`, `docs/production-cors-audit.md`
- Tests to add or update: Optional shell smoke script for health, Swagger, client shell, and CORS preflight.
- Validation commands:
  - `curl -I <backend-url>/health`
  - `curl -I <backend-url>/swagger/index.html`
  - `curl -I <client-url>/client/`
  - `curl -i -X OPTIONS <backend-url>/api/v1/account/login -H "Origin: <client-url>" -H "Access-Control-Request-Method: POST"`
- Estimated Codex phase size:
  - Small: 1-3 files
- Blocks Assignment 3:
  - Maybe

##### Finding 8.5: Production CORS environment variable documentation is inconsistent

- Severity: Medium
- Area: Deployment configuration documentation
- Evidence: `docs/cicd-audit.md` describes `CORS_ALLOWED_ORIGIN` as a comma-separated list, while `docker-compose.prod.yml` exposes both `CORS_ALLOWED_ORIGIN` and `CORS_ALLOWED_ORIGIN_CLIENT`. `docs/production-cors-audit.md` also mixes these concepts.
- Why this matters: Incorrect CORS environment guidance can break the separate React deployment even when the application code validates origins correctly.
- Likely cause: CORS support evolved from one origin to separate backend/client origins during Phase 8.
- Recommended correction: Choose and document the exact production environment variable contract, including examples for backend origin, client origin, and any allowed origin list.
- Files likely involved: `docs/cicd-audit.md`, `docs/production-cors-audit.md`, `docs/deployment.md`, `docker-compose.prod.yml`, `src/WebApp/Setup/WebApiExtensions.cs`
- Tests to add or update: Keep or add startup validation tests for multiple configured origins.
- Validation commands:
  - `dotnet test multi-gym-management-system.slnx --no-restore --filter Cors`
  - `rg "CORS_ALLOWED_ORIGIN" docs docker-compose.prod.yml src tests`
- Estimated Codex phase size:
  - Small: 1-3 files
- Blocks Assignment 3:
  - Maybe

## Cross-Phase Regression Risks

- API contract drift: Present in documentation. `docs/api-contract-freeze.md` does not match current identity DTOs, although implementation/tests appear stable.
- Auth or refresh-token regression: No functional regression found in current automated tests. Minor auth-flow documentation wording is stale.
- Tenant isolation regression: Implementation appears improved for member workflow tenant filtering, but security docs still describe old gaps. PostgreSQL provider behavior remains skipped by default.
- Role authorization regression: MVC and API role tests pass, but GymOwner MVC evidence may rely on a system-admin seeded user unless documented more clearly.
- MVC Admin accidentally replaced by React-only UX: Current code includes MVC Admin pages and focused Admin CRUD for members, training categories, and membership packages. `docs/assignment-compliance.md` was refreshed on 2026-05-11.
- ViewBag/ViewData reintroduced: Current tests scan Admin views/controllers and pass. No ViewBag/ViewData reintroduction was found with current evidence.
- React client coupled back into backend deployment: Legacy embedded `/client` support remains. Separate client artifacts exist, but deploy is optional/manual and separate-host behavior was not live-verified.
- CORS too permissive or too restrictive: Code appears to reject wildcard/localhost production origins, but production environment variable documentation is inconsistent and deployed preflight was not verified.
- Tests passing locally but not in CI: Risk remains because PostgreSQL tests are skipped by default, client deploy is manual/allowed to fail, Docker Compose deployment was not run here, and React Router warnings are not failing tests.
- Docs claiming features not backed by code/tests: 2026-05-11 readiness docs now distinguish verified local build/test/Compose evidence from unverified public deployment and separate-client smoke evidence.

## Required Follow-Up Codex Fix Phases

### Correction Phase A: Refresh Phase 0 evidence docs

Objective:
Bring top-level Assignment 3 evidence back in sync with the current implementation and tests.

User flows protected:
MVC Admin defense, MVC Client defense, auth route evidence, CRUD route evidence, deployment explanation.

Scope:
Documentation only.

Files likely touched:
`docs/assignment-compliance.md`, `docs/current-route-inventory.md`, `docs/current-test-inventory.md`, `docs/current-deployment-inventory.md`

Out of scope:
Production code, routes, DTOs, CI behavior.

Implementation steps:
Update stale statuses, remove obsolete claims about missing MVC Admin/ViewBag evidence, add current command outputs, and distinguish verified from unverified deployment evidence.

Automated tests:
No new tests required.

Validation commands:
`dotnet test multi-gym-management-system.slnx --no-restore`
`npm test`
`rg "Missing|Partial|React-only|ViewBag|ViewData" docs/assignment-compliance.md docs/current-*.md`

Done when:
Phase 0 docs no longer contradict current MVC, test, auth, CRUD, or deployment evidence.

### Correction Phase B: Correct API contract freeze

Objective:
Make `docs/api-contract-freeze.md` match the actual public DTOs and stable endpoint behavior.

User flows protected:
Login, register, refresh token, logout, switch gym, switch role, React API client compatibility.

Scope:
Contract documentation and optional contract assertions.

Files likely touched:
`docs/api-contract-freeze.md`, possibly identity API tests if serialized contract assertions are missing.

Out of scope:
Breaking API changes or DTO renames.

Implementation steps:
Compare each documented request/response against `src/App.DTO/v1/Identity` and controller tests, then update examples and response shapes.

Automated tests:
Add or update identity contract tests only if current tests do not assert the serialized fields.

Validation commands:
`dotnet test multi-gym-management-system.slnx --no-restore --filter Account`
`rg "displayName|firstName|lastName|expiresInSeconds|availableTenants|RoleName|GymCode" docs/api-contract-freeze.md src/App.DTO tests`

Done when:
The contract-freeze document can be used as a reliable source for the React client and defense discussion.

### Correction Phase C: Verify and fix separate-client routing

Objective:
Make direct loads and refreshes work for standalone React routes under the intended production path.

User flows protected:
Login, members, member workspace, trainer workspace, caretaker workspace, packages, training categories.

Scope:
One routing/deployment strategy for the React client plus smoke coverage.

Files likely touched:
`client/src/App.tsx`, `client/vite.config.ts`, `client/nginx.conf`, `docs/separate-client-hosting-audit.md`, `docs/deployment.md`

Out of scope:
Backend API contract changes and feature redesign.

Implementation steps:
Choose root-hosted or `/client`-hosted routing, configure React Router and static server consistently, build the client container, and smoke-test direct deep links.

Automated tests:
Add a Playwright or static-server smoke test for direct route loads if the project supports it.

Validation commands:
`npm run build`
`docker build -f client/Dockerfile -t multi-gym-client-test client`
`docker run --rm -p 8081:8080 multi-gym-client-test`
`curl -I http://localhost:8081/client/login`
`curl -I http://localhost:8081/client/members`

Done when:
Direct navigation, refresh, and bookmarked routes work for the documented standalone client URL.

### Correction Phase D: Harden separate-client CI/CD evidence

Objective:
Make separate React deployment evidence defensible instead of optional.

User flows protected:
Production React startup, API connectivity from the separate client origin, deployment repeatability.

Scope:
CI/deploy documentation and, if approved later, CI job behavior.

Files likely touched:
`.gitlab-ci.yml`, `scripts/deploy-client.sh`, `docker-compose.prod.yml`, `docs/cicd-audit.md`, `docs/deployment.md`

Out of scope:
Application features and public API contracts.

Implementation steps:
Decide whether CI deploys a pushed image or the host builds from source; document that path; remove misleading package claims; add smoke commands and captured results.

Automated tests:
Add a CI smoke job or manual checklist that validates the exact client artifact being deployed.

Validation commands:
`docker compose -f docker-compose.prod.yml --profile client build client`
`docker compose -f docker-compose.prod.yml --profile client up -d client`
`curl -I <client-url>/client/`

Done when:
The pipeline/deploy path proves how the separate client is built, deployed, and smoke-tested.

### Correction Phase E: Run production smoke and CORS verification

Objective:
Produce dated evidence that deployed backend and separate frontend URLs work together.

User flows protected:
Health, Swagger, login, CORS preflight, React shell loading.

Scope:
Documentation plus optional smoke script.

Files likely touched:
`docs/deployment.md`, `docs/production-cors-audit.md`, possibly `scripts/smoke-production.*`

Out of scope:
Changing CORS policy unless the smoke test fails.

Implementation steps:
Run health, Swagger, client shell, and CORS preflight checks against the real deployment and record exact commands/results.

Automated tests:
Optional scripted smoke checks.

Validation commands:
`curl -I <backend-url>/health`
`curl -I <backend-url>/swagger/index.html`
`curl -I <client-url>/client/`
`curl -i -X OPTIONS <backend-url>/api/v1/account/login -H "Origin: <client-url>" -H "Access-Control-Request-Method: POST"`

Done when:
Deployment docs include current command evidence for backend, Swagger, client, and production CORS.

### Correction Phase F: Add PostgreSQL test execution evidence

Objective:
Close the gap between default passing tests and production PostgreSQL behavior.

User flows protected:
Tenant filtering, migrations, persistence, relational constraints.

Scope:
Test execution path and documentation.

Files likely touched:
`.gitlab-ci.yml`, `docs/database-migration-audit.md`, `docs/current-test-inventory.md`

Out of scope:
Replacing the persistence provider or rewriting tests.

Implementation steps:
Run the existing PostgreSQL tests with required environment support, then decide whether to add a CI/manual job or document a required pre-defense command.

Automated tests:
Use the existing PostgreSQL persistence tests; add only missing provider-specific tests if failures reveal coverage gaps.

Validation commands:
`$env:RUN_POSTGRES_TESTS='1'; dotnet test multi-gym-management-system.slnx --no-restore --filter PostgreSqlPersistenceTests`

Done when:
PostgreSQL tests either pass in CI/manual evidence or are explicitly documented as unverified with a mitigation.

### Correction Phase G: Clarify localization completion boundary

Objective:
Make UI and DB localization claims precise and defensible.

User flows protected:
Training categories, membership packages, members, MVC Admin labels, React language switching.

Scope:
Docs and targeted string/test cleanup if approved later.

Files likely touched:
`docs/localization-audit.md`, `client/src`, `src/WebApp/Resources`

Out of scope:
Large translation rewrite unrelated to Assignment 3 demo flows.

Implementation steps:
Inventory active demo strings, localize required surfaces or document remaining English-only surfaces, and add focused assertions for required localized flows.

Automated tests:
Add UI localization tests for the pages used in defense if missing.

Validation commands:
`dotnet test multi-gym-management-system.slnx --no-restore --filter Localization`
`npm test -- localization`
`rg '"[A-Za-z][^"]*"' client/src src/WebApp/Areas/Admin/Views src/WebApp/Views`

Done when:
Localization docs and tests clearly support the Assignment 3 i18n claim without overstatement.

## Final Readiness Checklist

- [x] Solution builds (`dotnet build multi-gym-management-system.slnx` passed)
- [x] Backend tests pass (`dotnet test multi-gym-management-system.slnx --no-restore` passed with 130 passed, 3 skipped)
- [x] React tests pass (`npm test` passed with 34 tests)
- [x] React production build passes (`npm run build` passed)
- [ ] Swagger reachable (configured/documented, but not live-verified in this audit)
- [ ] Health endpoint reachable (configured/documented, but not live-verified in this audit)
- [x] Login works (covered by backend/React tests)
- [x] Refresh-token rotation tested
- [x] Logout invalidation tested
- [x] IDOR tests exist and pass
- [x] Role authorization tests exist and pass
- [x] Members CRUD verified by tests
- [x] Training categories CRUD verified by tests
- [x] Membership packages CRUD verified by tests
- [x] DB localization verified by tests
- [x] UI localization verified for tested surfaces, with documented boundary still needed
- [x] MVC Admin verified by tests
- [x] MVC Client verified by tests
- [x] Admin views avoid ViewBag/ViewData
- [x] Anti-forgery verified for MVC POST actions as a source guardrail; current Admin POST runtime behavior is not applicable if no Admin POST actions exist
- [ ] Separate React deployment verified (artifacts/build pass, but live/container deep-link behavior not verified)
- [ ] Production CORS verified (startup validation/tests exist, deployed preflight not run)
- [ ] CI/CD split verified (client jobs exist, but deploy is manual/allowed to fail and artifact promotion is unclear)
- [ ] Assignment evidence docs match implementation

## Final Verdict

Almost ready, corrections required

- The core solution builds, backend tests pass, React tests pass, and the React production build passes.
- Auth, refresh-token rotation, logout invalidation, role authorization, IDOR coverage, CRUD slices, and MVC compliance have meaningful automated evidence.
- The most serious current weakness is evidence quality: multiple audit and contract documents are stale or contradict the implementation.
- Separate React deployment is not yet fully defendable because direct-route behavior, live deployment, production CORS, and CI deploy guarantees are not verified.
- PostgreSQL-specific tests are skipped by default, leaving a production-provider evidence gap.
- Before defense, prioritize documentation synchronization and separate-client deployment smoke evidence over broad refactoring.
