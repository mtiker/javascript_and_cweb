# Final2 Traceability

This matrix maps the Final2 assignment requirements to the current
implementation, automated evidence, and remaining gaps.

## Requirement Matrix

| Requirement | Implementation | Automated evidence | Status |
|---|---|---|---|
| ASP.NET Core modular monolith | `WebApp` composes `Modules.Users`, `Modules.Gyms`, `Modules.Memberships`, `Modules.Training`, and `Modules.Maintenance` | `Architecture.Tests/ModuleBoundaryTests.cs` | Implemented with transitional shared persistence |
| At least 3 modules | 5 modules: Users, Gyms, Memberships, Training, Maintenance | `ExpectedModuleProjects_AllExist` | Implemented |
| No direct references between modules | Module communication is through `Shared.Contracts` and mediator abstractions | `Modules_DoNotReferenceOtherModules` | Implemented |
| Mediator communication between modules | `Shared.Contracts/Mediator`, `AddXxxModule` MediatR registration, sample Users handler | `Architecture.Tests/MediatorRegistrationTests.cs` | Implemented |
| Domain design with at least 10 meaningful entities | Gym, member, person, membership package, membership, payment, training category, session, booking, staff, equipment, equipment model, maintenance task, refresh token, user-role assignments | `ModuleDbContextOwnershipTests`, workflow tests, EF model tests | Implemented |
| REST API controllers | Module-owned API controllers under `Modules.*.Api`, routes under `/api/v1/...` | `ApiContractMetadataTests.PublicApiRoutes_RemainStableForFinal2Submission` | Implemented |
| API versioning | URL segment versioning with `[ApiVersion("1.0")]` and `api/v{version:apiVersion}` routes | `ApiContractMetadataTests.PublicApiControllers_UseV1UrlSegmentApiVersioning` | Implemented |
| Public DTOs | Public API DTOs in `Shared.Contracts/Dtos/v1` | route/contract metadata tests, integration tests deserialize DTOs | Implemented |
| Swagger | `AddAppSwagger`, versioned Swagger docs, bearer security definition | `SmokeTests.SwaggerJson_ExposesV1DocumentAndJwtBearerSecurity` | Implemented |
| JWT auth | Identity + JWT bearer setup, required JWT config, role claims | `AuthSecurityAndErrorTests.Login_ReturnsJwt_RefreshToken_Expiry_AndUserContext`, `RuntimeConfigurationTests` | Implemented |
| Refresh-token flow | refresh-token repository, rotation, reuse rejection, logout invalidation | `AuthSecurityAndErrorTests` and `SmokeTests.AccountPublicApi_LoginRefreshAndLogout_StillWorkThroughStableRoutes` | Implemented |
| MVC client UX | `WebApp/Areas/Client` mounted at `/mvc-client` | `SmokeTests.SeededMvcPages_RenderWithSharedLayoutAndStyles`, `MvcComplianceTests.MvcClientRoute_Works_ForTenantRoles` | Implemented |
| MVC Admin UX | `WebApp/Areas/Admin` with protected dashboards and CRUD pages | `MvcComplianceTests`, `AdminMembersCrudTests`, `AdminTrainingCategoriesCrudTests`, `AdminMembershipPackagesCrudTests` | Implemented |
| Admin area protected and no ViewBag/ViewData | Admin controllers use `[Authorize]`, strongly typed view models, anti-forgery on POST | `MvcComplianceTests` | Implemented |
| UI `.resx` localization | `App.Resources/SharedResources.resx` and `.et.resx`, request localization | `TrainingCategoryLocalizationTests.MvcLoginLabels_UseResxResourcesForRequestedCulture`, `AdminMembersPage_UsesResxResourcesForRequestedCulture` | Implemented |
| DB localization with LangStr | `Base.Domain.LangStr` persisted through EF conversion and projected by culture | `LangStrTests`, `TrainingCategoryLocalizationTests`, optional `PostgreSqlPersistenceTests.LangStr_UsesJsonbColumnAndRoundTripsTranslations` | Implemented |
| IDOR and tenant isolation | active-gym middleware, tenant filters, role checks, self-only member checks | `TenantIsolationAndIdorTests`, `AuthSecurityAndErrorTests`, `Final1CriticalE2ETests.IdorNegative_E2E_CrossTenantMemberUpdateReturns404` | Implemented |
| Repositories, UOW, services, BLL, mappers | module-owned repositories/services/mappers for defended slices, transitional `IAppUnitOfWork` for remaining save/generic bridge | workflow service unit tests, architecture tests | Implemented with documented transitional contracts |
| CI/CD deploy | root child pipeline, assignment-local pipeline, Docker, deploy scripts | Compose config validation, production image build, local production-stack smoke, `.gitlab-ci.yml` files | Implemented locally; public smoke pending |
| Separate client app | React + TypeScript client in `client/`, standalone Docker/nginx profile | `client` Vitest suite and `npm run build` | Implemented |
| Separate client JWT and refresh flow | `client/app/lib/apiClient.ts`, `auth.tsx`, protected routes | `client/app/lib/apiClient.test.ts`, `client/app/lib/auth.test.ts`, `client/app/App.test.tsx` | Implemented |
| Separate client CRUD for at least 3 entities | Members, training categories, membership packages | `client/app/pages/CrudPages.test.tsx` | Implemented |
| CORS handling for separate client | named `ClientApp` policy, production fail-fast origin validation | `CorsTests`, `RuntimeConfigurationTests.AddAppCors_*` | Implemented |
| Mandatory test coverage | backend xUnit unit/integration/architecture tests, frontend Vitest tests | `dotnet test`, `npm test` | Implemented |

## Validation Evidence

Phase 13 required validation commands:

- `dotnet format multi-gym-management-system.slnx --verify-no-changes`
- `dotnet build multi-gym-management-system.slnx`
- `dotnet test multi-gym-management-system.slnx`
- `cd client && npm test`
- `cd client && npm run build`

The latest command results are recorded in [testing.md](testing.md) and the
phase tracker after each phase run.

## Known Gaps Before Defense

- Phase 14 local Docker production-stack smoke passes, but the public
  backend/client URLs must not be claimed as live until the public smoke passes.
  The latest public check still returned HTTP 404 for backend `/health`.
- PostgreSQL/Testcontainers tests are opt-in because they require a Docker
  engine. They should be run on a Docker-capable machine before final defense.
- Browser Playwright tests are not present. The current E2E evidence is
  HTTP-level integration coverage plus React component/workflow tests.
- Full legacy `App.*` dependency removal from modules remains deferred until
  entity-shaped contracts are replaced or moved behind module-owned APIs.
