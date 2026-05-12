# Final2 Risk Report

## Current Risk Posture

The Final2 submission is defense-ready for modular-monolith evidence, with the
remaining risks limited to deployment-environment verification, opt-in
PostgreSQL provider coverage, and future module-internal cleanup. Local
format, build, backend tests, client tests, client build, and Compose config
validation passed on 2026-05-11.

## Risks

| Risk | Impact | Mitigation | Status |
|---|---|---|---|
| Module-to-module coupling reappears | Breaks Final2 core requirement | `ModuleArchitectureTests` fail on direct module references and selected internal type leaks | Controlled |
| Public API routes drift during module refactor | Breaks React/MVC/API clients | Route snapshot test locks all public API method/template pairs | Controlled |
| Module data ownership is only logical while one `AppDbContext` remains | Future extraction work could be harder | Entity owner map is documented; access is mediated at code boundary first | Accepted for Final2 |
| Module handlers still delegate to `App.BLL` services | Modules are not fully internally self-contained | Transitional architecture is documented; module ownership is claimed only for migrated workflows | Accepted for Final2 |
| PostgreSQL-specific behavior not run by default | Provider-specific regressions could be missed locally | Testcontainers tests remain available via `RUN_POSTGRES_TESTS=1`; CI/local default keeps fast feedback | Residual |
| Public VPS state not verified in this pass | Deployment may differ from local build | Deployment docs and scripts are current; run VPS smoke checklist before submission | Residual |
| Separate client public host not live-smoke-tested | Separate-host routing/CORS may differ from local build/config evidence | Client build and Compose profile validate; run `scripts/smoke-deploy.sh` plus CORS preflight after real host is available | Residual |
| React Router v7 future warnings in Vitest | Future upgrade noise, no current failure | Track as dependency-upgrade work, not Final2 blocker | Residual |

## Dependency Hardening

The local vulnerability audit initially reported a critical advisory for a
test-project transitive `Microsoft.AspNetCore.DataProtection` package. The
test project now pins:

- `Microsoft.AspNetCore.DataProtection` 10.0.7
- `Microsoft.AspNetCore.Mvc.Testing` 10.0.7
- `System.Security.Cryptography.Xml` 10.0.7

Verification:

```powershell
dotnet list multi-gym-management-system.slnx package --vulnerable --include-transitive
```

Result on 2026-05-08: no vulnerable packages reported.

## 2026-05-11 Validation Snapshot

| Command | Result |
|---|---|
| `dotnet format multi-gym-management-system.slnx --verify-no-changes` | Pass |
| `dotnet build multi-gym-management-system.slnx` | Pass, 0 warnings, 0 errors |
| `dotnet test multi-gym-management-system.slnx` | Pass, 250 passed, 3 skipped |
| `cd client && npm test` | Pass, 34 passed with React Router v7 future warnings |
| `cd client && npm run build` | Pass |
| `docker compose config` | Pass |
| `POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key VITE_API_BASE_URL=https://api.example.test docker compose -f docker-compose.prod.yml config` | Pass |
| `POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key VITE_API_BASE_URL=https://api.example.test docker compose --profile client -f docker-compose.prod.yml config` | Pass |

Deployment smoke was not run against public URLs in this pass.

## Pre-Submission Checklist

- [x] Backend build passes.
- [x] Backend tests pass.
- [x] React tests pass.
- [x] React production build passes.
- [x] Vulnerability audit reports no vulnerable packages.
- [x] Architecture tests are part of CI test project.
- [x] Development and production Compose config validation passes.
- [ ] Optional: run `RUN_POSTGRES_TESTS=1 dotnet test ...` with Docker.
- [ ] Required before claiming live deployment: run public VPS smoke checklist from `docs/deployment.md`.
- [ ] Required before claiming separate public client hosting: run standalone client health/deep-link/CORS smoke checks against the real client host.
