# Final2 Defense Status

Assignment 05 Final2 is implemented as a transitional modular monolith for the
multi-gym SaaS project.

Use these documents for defense:

- [final2-architecture.md](final2-architecture.md)
- [final2-traceability.md](final2-traceability.md)
- [architecture.md](architecture.md)
- [module-boundaries.md](module-boundaries.md)
- [testing.md](testing.md)
- [deployment.md](deployment.md)
- [security-and-access.md](security-and-access.md)
- [api.md](api.md)

## Defendable Claims

- The backend is one ASP.NET Core host with five module projects: Users, Gyms,
  Memberships, Training, and Maintenance.
- Modules do not project-reference each other; cross-module interaction goes
  through `Shared.Contracts` module APIs and MediatR abstractions.
- REST controllers are versioned under `/api/v1/...`, discovered by Swagger,
  and protected with JWT where required.
- Admin MVC is protected, strongly typed, anti-forgery guarded, and free of
  `ViewBag`/`ViewData`.
- The separate React client logs in with JWT, handles refresh-token retry,
  sends `Accept-Language`, and covers the required CRUD surfaces.
- UI localization uses `.resx`; DB localization uses `LangStr`.
- Tenant isolation is enforced through active gym context, roles, query
  filters, and self-only member checks.
- CI/CD, Docker, Compose, backend deploy, standalone client deploy, and smoke
  scripts exist for the Final2 folder. The smoke script covers backend health,
  Swagger JSON, standalone client health, CORS preflight, login,
  refresh-token renewal, and one authenticated tenant API read.
- A local Docker production-stack smoke passes for backend, PostgreSQL,
  standalone client, public-origin CORS preflight, login, refresh-token
  renewal, and an authenticated tenant API read.

## Caveats To State Clearly

- `AppDbContext` remains the active runtime migration/seeding context.
  Module-owned DbContexts exist and are tested, but full schema cutover is not
  complete.
- Some modules still reference transitional `App.BLL.Contracts`,
  `App.DAL.EF`, and `App.Domain` contracts. The concrete `App.BLL`
  implementation dependency has been removed from WebApp/modules.
- Public deployment must not be claimed as currently live. Phase 14 local
  Docker smoke passed on 2026-05-25, but `scripts/smoke-deploy.sh` against the
  documented public backend/client URLs still returned HTTP 404 for backend
  `/health`; the current build is not verified as deployed publicly.
- Browser Playwright tests are not present; current E2E evidence is HTTP-level
  integration coverage plus React Vitest workflow coverage.
