# Final2 Test Traceability

## Requirement Matrix

| Requirement | Evidence |
|---|---|
| At least 3 modules: Users + 2 business modules | `src/Modules.Users`, `src/Modules.GymManagement`, `src/Modules.Training`, `src/Modules.MembershipFinance`; `ModuleArchitectureTests.EveryModule_ExposesExactlyOneIModuleMarker` |
| Each module has clear data ownership | `docs/module-data-ownership.md`; `docs/final2-module-boundary-report.md` |
| Cross-module communication uses mediator | `src/BuildingBlocks/Mediator/*`; `ArchitectureTests.*Slice_*`; `tests/WebApp.Tests/Unit/*ModuleMediatorTests.cs` |
| No direct references between modules | `ModuleArchitectureTests.EveryModule_DoesNotReferenceAnyOtherModule`; `ModuleArchitectureTests.NonUsersModules_DoNotReferenceUsersInternals`; `ModuleArchitectureTests.TrainingModule_DoesNotReferenceUsersOrGymManagementInternals` |
| Training category API workflow is module-owned | `src/Modules.Training/Application/TrainingCategoryHandlers.cs`; `TrainingModuleMediatorTests.Mediator_DispatchesTrainingCategoryCrudMessagesThroughModuleOwnedWorkflow`; `ModuleArchitectureTests.TrainingCategoryWorkflow_IsOwnedByTrainingModuleHandlers` |
| Membership package API workflow is module-owned | `src/Modules.MembershipFinance/Application/MembershipPackageHandlers.cs`; `MembershipFinanceModuleMediatorTests.Mediator_DispatchesMembershipPackageCrudMessagesThroughModuleOwnedHandlers`; `ModuleArchitectureTests.MembershipPackageWorkflow_IsOwnedByMembershipFinanceModuleHandlers` |
| Public API routes remain stable | `ApiContractMetadataTests.PublicApiRoutes_RemainStableForFinal2Submission`; `ApiContractMetadataTests.AccountAuthPublicRoutesAndDtos_RemainStable` |
| MVC Admin still works | `MvcComplianceTests.GymAdminOrGymOwner_CanAccess_TenantAdminPages`; Admin view-model/no-ViewBag tests |
| MVC Client still works | `MvcComplianceTests.MvcClientRoute_Works_ForTenantRoles` |
| React client still works | `client` Vitest suite, especially `CrudPages`, `SessionsPage`, `OperationsPages`, `WorkspacePages`; `npm run build` |
| Auth tests still pass | `SmokeTests`, `AuthSecurityAndErrorTests`, account route/DTO metadata tests |
| IDOR tests still pass | `TenantIsolationAndIdorTests` |
| i18n tests still pass | `TrainingCategoryLocalizationTests` |
| CI runs architecture tests | assignment `.gitlab-ci.yml` `assignment03_test` runs the full backend test assembly |
| Deployment docs are current | `README.md`, `docs/deployment.md`, `docs/current-deployment-inventory.md` |
| MVC Admin CRUD works for required admin entities | `AdminMembersCrudTests`, `AdminTrainingCategoriesCrudTests`, `AdminMembershipPackagesCrudTests` |

## Local Verification Snapshot

2026-05-11:

| Command | Result |
|---|---|
| `dotnet format multi-gym-management-system.slnx --verify-no-changes` | Pass, no formatting changes required |
| `dotnet build multi-gym-management-system.slnx` | Pass, 0 warnings, 0 errors |
| `dotnet test multi-gym-management-system.slnx` | Pass: 250 passed, 3 skipped |
| `cd client && npm test` | Pass: 7 files / 34 tests; React Router v7 future warnings only |
| `cd client && npm run build` | Pass |
| `docker compose config` | Pass |
| `POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key VITE_API_BASE_URL=https://api.example.test docker compose -f docker-compose.prod.yml config` | Pass |
| `POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key VITE_API_BASE_URL=https://api.example.test docker compose --profile client -f docker-compose.prod.yml config` | Pass |

## Skipped / External Verification

| Area | Status |
|---|---|
| PostgreSQL Testcontainers tests | Skipped by existing `RUN_POSTGRES_TESTS=1` gate during this pass |
| Public VPS smoke test | Not performed in this local hardening pass |
| Separate public client host smoke test | Not performed in this local readiness pass |
| GitLab hosted pipeline | Not triggered locally; local commands mirror the assignment pipeline test/build path |

## CI Mapping

Root `.gitlab-ci.yml` triggers the assignment child pipeline when files under
`courses/webapp-csharp/assignment-03-multi-gym-management-system/**/*` change.

Assignment `.gitlab-ci.yml` stages:

1. `assignment03_client`: `npm ci && npm test && npm run build`
2. `assignment03_build`: `dotnet build --configuration Release`
3. `assignment03_test`: `dotnet test --configuration Release --no-build`
4. package backend and client images
5. deploy backend and optionally deploy standalone client
