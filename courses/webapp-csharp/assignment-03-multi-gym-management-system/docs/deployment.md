# Deployment Guide

## Local Development Runtime

Local database:
- `docker-compose.yml`

Local startup scripts:
- `scripts/start-db.ps1`
- `scripts/migrate-db.ps1`
- `scripts/start-app.ps1`
- `scripts/stop-db.ps1`

Default local database:
- host: `127.0.0.1`
- port: `5432`
- database: `multi_gym_management_system`

## Production Runtime

Public route:
- `https://mtiker-cweb-4.proxy.itcollege.ee`

Proxy target:
- `http://192.168.181.122:83`

Production deployment files:
- `Dockerfile`
- `docker-compose.prod.yml`
- `scripts/deploy.sh`

Production container layout:
- `postgres` service
- `web` service hosting the ASP.NET Core app, MVC UI, REST API, Swagger, health checks, and the built React client at `/client`

The Dockerfile has a dedicated Node 20 build stage for `client/`. It runs `npm ci` and `npm run build`, then copies `client/dist` into `WebApp/wwwroot/client` in the final ASP.NET Core runtime image.

## Required Environment Variables

Minimum production variables:
- `JWT__Key`
- `CORS_ALLOWED_ORIGIN`
- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `WEBAPP_PORT` when overriding the default host port `83`

Optional variables:
- `JWT__Issuer`
- `JWT__Audience`
- `JWT__AccessTokenMinutes`
- `DATA_INIT_MIGRATE_DATABASE`
- `DATA_INIT_SEED_DATA`
- `COMPOSE_PROJECT_NAME`

`JWT__Key` must be a long secret value and must not be committed to `appsettings.json` or any tracked documentation. `docker-compose.prod.yml` refuses to start without it. `JWT__Issuer` and `JWT__Audience` default to `MultiGymManagementSystem` in production Compose, but can be set explicitly in GitLab CI/CD variables.

`CORS_ALLOWED_ORIGIN` must be a safe absolute public origin in production (no `localhost`, loopback IPs, or wildcard origins). The backend now fails fast on startup if production CORS origins are missing or unsafe.

For local development, store JWT values as ASP.NET Core user secrets from `src/WebApp`:

```powershell
dotnet user-secrets set "Jwt:Key" "<long-local-jwt-secret>"
dotnet user-secrets set "Jwt:Issuer" "MultiGymManagementSystem"
dotnet user-secrets set "Jwt:Audience" "MultiGymManagementSystem"
```

Data Protection keys are persisted in the application database via `AppDbContext`, so protected MVC/cookie payloads remain valid across container restarts as long as the database volume is preserved.

Forwarded headers are enabled in the middleware pipeline before HTTPS redirection and authentication so reverse-proxy deployments keep correct scheme/client metadata for auth and redirect behavior.

## GitLab CI/CD

Repository level:
- root `.gitlab-ci.yml` triggers this assignment as a child pipeline

Assignment level:
- `courses/webapp-csharp/assignment-03-multi-gym-management-system/.gitlab-ci.yml`

Pipeline stages:
1. `build`
2. `test`
3. `package`
4. `deploy`

Deploy script entrypoint:

```bash
./scripts/deploy.sh
```

## Recommended VPS Smoke Check

After deploy:

```bash
curl http://127.0.0.1:83/health
curl http://127.0.0.1:83/client
```

Also verify:
- `https://mtiker-cweb-4.proxy.itcollege.ee` loads
- Swagger loads
- `/client/members`, `/client/member-workspace`, `/client/coaching-workspace`, and `/client/finance-workspace` serve the React app shell
- a seeded user can log in
- `multigym.admin@gym.local` can switch gyms
- one workflow mutation succeeds (for example invoice payment POST, coaching item decision PUT, or maintenance assignment PUT)
