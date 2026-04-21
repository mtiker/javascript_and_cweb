# AI Usage Log

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
