# Final1 Coverage Audit

**Audited:** 2026-04-30  
**Project:** `courses/webapp-csharp/assignment-03-multi-gym-management-system`

## Coverage Summary

| Area | Status | Primary evidence |
|------|--------|------------------|
| Clean/Onion dependency direction | Covered | `ArchitectureTests.cs`; `docs/final1-architecture-diagram.md` |
| Repository/UOW/service/BLL/mapper usage | Covered | `ArchitectureTests.cs`; BLL contracts, DAL EF repositories, BLL mappers |
| Full Admin UX | Covered | MVC Admin Razor pages; `SmokeTests`; `MvcComplianceTests`; `AdminMembersPageTests` |
| Public DTO/API stability | Covered | `src/App.DTO/v1`; `ApiContractMetadataTests.cs`; `docs/api-contract-freeze.md` |
| Auth and refresh-token tests | Covered | `AuthSecurityAndErrorTests.cs`; `Final1CriticalE2ETests.cs` |
| IDOR tests | Covered | `AuthorizationServiceTests.cs`; `TenantIsolationAndIdorTests.cs`; CRUD wrong-gym tests |
| UI i18n tests | Covered | `TrainingCategoryLocalizationTests.MvcLoginLabels_UseResxResourcesForRequestedCulture`; React language tests |
| DB i18n tests | Covered | `LangStrTests.cs`; `TrainingCategoryLocalizationTests.cs`; PostgreSQL gated test |
| React 3-entity CRUD tests | Covered | `client/src/pages/CrudPages.test.tsx` |
| Critical E2E tests | Covered | `Final1CriticalE2ETests.cs` |
| CI commands | Covered | `.gitlab-ci.yml`; `docs/testing.md` |

## Backend Test Commands

```powershell
dotnet build multi-gym-management-system.slnx
dotnet test multi-gym-management-system.slnx
```

Optional PostgreSQL provider slice:

```powershell
$env:RUN_POSTGRES_TESTS='1'
dotnet test multi-gym-management-system.slnx
```

## Frontend Test Commands

```powershell
cd client
npm test
npm run build
```

## CI Commands

The assignment child pipeline runs:

```bash
npm ci
npm test
npm run build
dotnet restore multi-gym-management-system.slnx
dotnet build multi-gym-management-system.slnx --configuration Release --no-restore
dotnet test multi-gym-management-system.slnx --configuration Release --no-build
docker build --pull --tag "$CI_PROJECT_PATH_SLUG-assignment-03:$CI_COMMIT_SHORT_SHA" courses/webapp-csharp/assignment-03-multi-gym-management-system
docker build --pull --build-arg "VITE_API_BASE_URL=${VITE_API_BASE_URL:-}" --tag "$CI_PROJECT_PATH_SLUG-assignment-03-client:$CI_COMMIT_SHORT_SHA" -f courses/webapp-csharp/assignment-03-multi-gym-management-system/client/Dockerfile courses/webapp-csharp/assignment-03-multi-gym-management-system/client
```

Deploy jobs run:

```bash
./scripts/deploy.sh
./scripts/deploy-client.sh
```

## Gap Review

No product feature gaps were added in this phase.

Remaining limitations:
- No Playwright browser E2E suite exists. Final1 E2E coverage is API-level integration coverage through the full backend host plus React Vitest coverage for client workflows.
- PostgreSQL Testcontainers coverage is not part of the default test run and remains opt-in through `RUN_POSTGRES_TESTS=1`.
- The live public URL must still be smoke-tested at defense time because availability depends on the VPS/proxy/container state.
