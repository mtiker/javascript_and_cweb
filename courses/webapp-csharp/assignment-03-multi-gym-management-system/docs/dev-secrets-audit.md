# Dev Secrets Audit

Audited: 2026-04-27

## Status summary

No missing required secrets for local development. All required values are present in committed configuration files.

---

## Required configuration values

| Key | Layer | Required by | Dev value | Notes |
|---|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | `appsettings.json` | `DatabaseExtensions` | `Host=127.0.0.1;Port=5432;Database=multi_gym_management_system;Username=postgres;Password=postgres` | Matches `docker-compose.yml` |
| `Jwt:Key` | `appsettings.Development.json` | `IdentitySetupExtensions` — throws if missing | `dev-local-jwt-secret-key-for-development-only-do-not-use-in-production` | Dev placeholder; production must use a real secret via environment variable |
| `Jwt:Issuer` | `appsettings.json` | `IdentitySetupExtensions` — throws if missing | `MultiGymManagementSystem` | |
| `Jwt:Audience` | `appsettings.json` | `IdentitySetupExtensions` — throws if missing | `MultiGymManagementSystem` | |

All three JWT fields throw `InvalidOperationException` at startup if absent.

---

## Configuration files

| File | Committed | Purpose |
|---|---|---|
| `src/WebApp/appsettings.json` | Yes | Base values: connection string, JWT issuer/audience, CORS, data-init flags, logging |
| `src/WebApp/appsettings.Development.json` | Yes | Dev overrides: `Jwt:Key` (dev placeholder), verbose logging |

No `.env` file is needed for local development. No user-secrets file is required (the `UserSecretsId` in `WebApp.csproj` is registered but nothing is stored there by default).

---

## Production secrets

For production, the following must be injected via environment variables (never committed):

| Environment variable | Maps to | Required |
|---|---|---|
| `JWT__Key` | `Jwt:Key` | **Required** — `deploy.sh` validates with `: "${JWT__Key:?..."` |
| `POSTGRES_PASSWORD` | Docker postgres service | Optional (defaults to `postgres`; change for real deployments) |
| `ConnectionStrings__DefaultConnection` | Full override if needed | Optional if Postgres defaults are kept |

`docker-compose.prod.yml` also accepts `JWT__Issuer`, `JWT__Audience`, `CORS_ALLOWED_ORIGIN`, `WEBAPP_PORT`, and `DATA_INIT_*` via `.env` or shell exports.

---

## Security notes

- `appsettings.Development.json` contains a committed dev JWT key. This is intentional for zero-friction local onboarding. It must never be used in staging or production.
- `DataInitialization:MigrateDatabase` and `DataInitialization:SeedData` default to `true` in `appsettings.json`. For production, set `DATA_INIT_SEED_DATA=false` after initial deployment to avoid re-seeding.

---

## Blocking issues found

None.
