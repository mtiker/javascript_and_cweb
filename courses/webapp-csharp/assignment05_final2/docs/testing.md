# Testing Guide

This guide is the current source of truth for Assignment 05 Final2 validation.

## Automated Commands

Run from `courses/webapp-csharp/assignment05_final2`.

Format:

```powershell
dotnet format multi-gym-management-system.slnx --verify-no-changes
```

Backend build:

```powershell
dotnet build multi-gym-management-system.slnx
```

Backend tests:

```powershell
dotnet test multi-gym-management-system.slnx
```

PostgreSQL/Testcontainers tests:

```powershell
$env:RUN_POSTGRES_TESTS = "1"
dotnet test multi-gym-management-system.slnx --filter PostgreSql
Remove-Item Env:\RUN_POSTGRES_TESTS
```

Bash equivalent:

```bash
RUN_POSTGRES_TESTS=1 dotnet test multi-gym-management-system.slnx --filter PostgreSql
```

React client:

```powershell
cd client
npm test
npm run build
```

Compose validation:

```bash
docker compose config

POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key \
  VITE_API_BASE_URL=https://api.example.test \
  docker compose -f docker-compose.prod.yml config

POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key \
  VITE_API_BASE_URL=https://api.example.test \
  docker compose --profile client -f docker-compose.prod.yml config
```

## Latest Recorded Validation

Latest Final2 Phase 13 coverage and traceability validation was run on
2026-05-25:

| Command | Result |
|---|---|
| `dotnet format multi-gym-management-system.slnx --verify-no-changes` | Pass; workspace-load warnings reported, no formatting changes required |
| `dotnet build multi-gym-management-system.slnx` | Pass, 0 errors; known transitive NuGet advisories on `Microsoft.AspNetCore.DataProtection` and `System.Security.Cryptography.Xml` |
| `dotnet test multi-gym-management-system.slnx -- xUnit.ParallelizeTestCollections=false` | Pass, Architecture.Tests 17 passed / 1 skipped, WebApp.Tests 199 passed / 3 skipped PostgreSQL/Testcontainers tests |
| `cd client && npm test -- --run` | Pass, 6 files / 32 tests; React Router v7 future warnings only |
| `cd client && npm run build` | Pass, Vite production build completed |

Latest Final2 Phase 11 Admin regression validation was run on 2026-05-22:

| Command | Result |
|---|---|
| `dotnet test multi-gym-management-system.slnx --filter Admin -- xUnit.ParallelizeTestCollections=false` | Pass, WebApp.Tests 65 passed, 0 skipped, 0 failed; Architecture.Tests had no Admin-filtered tests |
| `rg -n "ViewBag\|ViewData" WebApp\Areas\Admin` | Pass, no matches |

Latest Final1 completion validation was run on 2026-05-19:

| Command | Result |
|---|---|
| `dotnet build multi-gym-management-system.slnx --no-restore` | Pass, 0 warnings, 0 errors |
| `dotnet test multi-gym-management-system.slnx --no-restore` | Pass, 202 passed, 3 skipped PostgreSQL/Testcontainers tests |
| `cd client && npm test` | Pass, 6 files / 32 tests; React Router v7 future warnings only |
| `cd client && npm run build` | Pass |
| `docker info --format '{{.ServerVersion}}'` | Failed: Docker Desktop engine pipe unavailable |

Latest Final2 Phase 14 local deployment smoke validation was run on
2026-05-25:

| Command | Result |
|---|---|
| `docker compose --profile client -f docker-compose.prod.yml build` | Pass, backend and standalone client images built |
| `docker compose --project-name assignment05-final2-smoke --profile client -f docker-compose.prod.yml up -d --no-build --wait` | Pass, backend/PostgreSQL/client containers healthy on local smoke ports |
| `scripts/smoke-deploy.sh` with `BACKEND_URL=http://localhost:18083`, `CLIENT_URL=http://localhost:18081`, and `SMOKE_CORS_ORIGIN=https://mtiker-cweb-a4-client.proxy.itcollege.ee` | Pass |
| `scripts/smoke-deploy.sh` against documented public backend/client URLs | Failed at backend `/health` with HTTP 404 |

Earlier 2026-05-11 validation also covered `dotnet format` and Compose config
rendering. PostgreSQL/Testcontainers and public deployment smoke checks still
need to be run on a Docker-capable, deployed environment before claiming them.

## Test Scope

Architecture:
- `ArchitectureTests`
- `ModuleArchitectureTests`
- `Final1PresentationBoundaryTests`

Security and auth:
- `AuthSecurityAndErrorTests`
- `AuthorizationServiceTests`
- `TenantIsolationAndIdorTests`
- `RuntimeConfigurationTests`

MVC:
- `MvcComplianceTests`
  - verifies Admin route protection, no Admin `ViewBag`/`ViewData`, strongly
    typed Admin views, anti-forgery on Admin POST actions, and no direct Admin
    controller dependency on EF `DbContext` or concrete legacy App.BLL/App.DAL
    infrastructure
- `AdminMembersCrudTests`
- `AdminTrainingCategoriesCrudTests`
- `AdminMembershipPackagesCrudTests`
- `AdminMembersPageTests`
- `ClientDashboardTests`

Workflow and domain:
- `Final1CriticalE2ETests`
- `MemberCrudTests`
- `MemberWorkflowServiceTests`
- `MembershipPackageCrudTests`
- `MembershipWorkflowServiceTests`
- `MaintenanceWorkflowServiceTests`
  - covers the module-owned `MaintenanceWorkflowService` and the
    `ITrainingModuleApi` staff-assignment boundary used by Phase 8
- `ProposalWorkflowTests`
- `LangStrTests`
- `ApiContractMetadataTests`
  - verifies stable public API routes, standard ProblemDetails metadata, and
    v1 URL-segment API versioning on module-owned controllers
- `SmokeTests.SwaggerJson_ExposesV1DocumentAndJwtBearerSecurity`
  - verifies Swagger JSON generation, v1 route exposure, and JWT bearer
    OpenAPI security metadata

Architecture and module persistence:
- `ModuleDbContextOwnershipTests`
  - verifies the five Phase 9 module DbContexts can save/read their owned EF
    persistence slices
- `ModuleBoundaryTests`
  - verifies module project boundaries and prevents modules from referencing a
    foreign module DbContext

Module/mediator:
- `TrainingModuleMediatorTests`
- `MembershipFinanceModuleMediatorTests`
- `MaintenanceModuleMediatorTests`

Optional provider-specific tests:
- `PostgreSqlPersistenceTests`, skipped unless `RUN_POSTGRES_TESTS=1`

Frontend Vitest coverage:
- auth guard and logout
- role-aware landing routes
- multi-gym tenant/role switching
- API refresh retry and refresh failure handling
- `Accept-Language` header behavior
- members, training categories, and membership packages CRUD behavior
- sessions detail and booking
- trainer attendance
- caretaker maintenance status
- maintenance task scheduling and assignment

## Manual Smoke Checklist

Before defense:
1. Start PostgreSQL and apply migrations.
2. Start the backend.
3. Open `/health` and `/swagger`.
4. Login to MVC Admin with `multigym.admin@gym.local`.
5. Verify `/Admin`, `/Admin/Members`, `/Admin/TrainingCategories`,
   `/Admin/MembershipPackages`, `/Admin/Sessions`, and `/Admin/Operations`.
6. Open `/mvc-client` as `member@peakforge.local`.
7. Start or open the React client.
8. Login as `admin@peakforge.local`.
9. Create, edit, and delete one member.
10. Create, edit, and delete one training category.
11. Create, edit, and delete one membership package.
12. Create a booking from the sessions page.
13. Login as `trainer@peakforge.local` and verify attendance/session flow.
14. Login as `caretaker@peakforge.local` and update an assigned maintenance
    task.
15. Login as `systemadmin@gym.local` and verify gym switching/admin access.
16. Switch React language to `ET` and verify localized seeded data.
17. If deployed, open `/client`, `/client/member-workspace`, `/client/sessions`,
    and `/client/maintenance`.
18. Run `scripts/smoke-deploy.sh` against real public backend/client URLs and
    record the result before claiming live deployment readiness.

Seed password:
- `GymStrong123!`

## Known Test Gaps

- Browser Playwright tests are not present.
- PostgreSQL provider tests require Docker and are opt-in.
- Public deployment smoke tests are not part of the normal local test suite.
- Standalone public client hosting requires manual live smoke verification.
- Full legacy `App.*` dependency removal is not fully test-enforced yet because
  remaining transitional contracts still expose entity-shaped types.
