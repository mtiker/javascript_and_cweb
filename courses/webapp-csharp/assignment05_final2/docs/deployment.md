# Deployment Guide

This guide covers local runtime, production Docker modes, GitLab CI/CD, and
repeatable smoke checks for Assignment 05 Final2.

## Local Runtime

Prerequisites:
- .NET 10 SDK
- Node.js 20+
- Docker Desktop or another local Docker engine

Local database:
- `docker-compose.yml`
- host `127.0.0.1`
- port `5432`
- database `multi_gym_management_system`

Useful scripts:
- `scripts/start-db.ps1`
- `scripts/migrate-db.ps1`
- `scripts/start-app.ps1`
- `scripts/stop-db.ps1`

Backend endpoints:
- HTTP: `http://localhost:5107`
- HTTPS: `https://localhost:7245`
- Swagger: `https://localhost:7245/swagger`
- health: `https://localhost:7245/health`
- MVC Admin: `https://localhost:7245/Admin`
- MVC Client: `https://localhost:7245/mvc-client`

React dev client:

```powershell
cd client
npm install
npm run dev
```

JavaScript Assignment 07 client against this backend:

```powershell
cd ..\..\javascript\assignment-07-client
npm install
$env:VITE_API_BASE_URL = "http://localhost:5107"
npm run dev
```

Default React dev URL:
- `http://localhost:5173`

## Production Modes

Mode A, embedded client:
- one backend image
- React client is built by the backend Dockerfile
- built files are copied to `WebApp/wwwroot/client`
- backend serves `/client`
- browser API calls are same-origin

Mode B, standalone client:
- backend image remains on the backend host/port
- React client runs in its own nginx image
- production Compose enables it through the `client` profile
- backend CORS must allow the standalone client origin

Current documented public backend route:
- `https://mtiker-cweb-a4.proxy.itcollege.ee`

Current documented backend proxy target:
- `http://192.168.181.122:83`

Suggested standalone client route:
- `https://mtiker-cweb-a4-client.proxy.itcollege.ee`

JavaScript Assignment 07 standalone route:
- `https://mtiker-js-a07.proxy.itcollege.ee`
- already pre-allowed via `Cors__AllowedOrigins__2` in
  `docker-compose.prod.yml` (default value is the public a07 host; override
  via `CORS_ALLOWED_ORIGIN_A07`)
- the a07 client lives in
  `courses/javascript/assignment-07-client/` and has its own
  `Dockerfile`, `docker-compose.yml`, and `scripts/deploy.sh`
- suggested host port for the a07 container: `90`

Suggested legacy in-repo standalone client host port:
- `8081`

## Production Files

- `Dockerfile` - backend image and embedded client build
- `client/Dockerfile` - standalone React/nginx image
- `client/nginx.conf` - static SPA nginx config
- `docker-compose.prod.yml` - backend, PostgreSQL, and optional client profile
- `scripts/deploy.sh` - backend deployment
- `scripts/deploy-client.sh` - standalone client deployment
- `scripts/smoke-deploy.sh` - backend/client/API smoke check
- assignment `.gitlab-ci.yml` - child pipeline

## Required Production Variables

Mode A:
- `JWT__Key`
- `POSTGRES_PASSWORD`
- `CORS_ALLOWED_ORIGIN`

Mode B adds:
- `CORS_ALLOWED_ORIGIN_CLIENT`
- `VITE_API_BASE_URL`
- `CLIENT_PORT`
- `MULTI_GYM_CLIENT_IMAGE` when pulling a prebuilt client image

JS Assignment 07 standalone client:
- `CORS_ALLOWED_ORIGIN_A07` — defaults to `https://mtiker-js-a07.proxy.itcollege.ee`.
  Set to a different origin (or blank) to override the third CORS slot. The
  default lets the a07 client talk to the backend without any extra setup once
  it is deployed at the documented URL.

Optional:
- `JWT__Issuer`
- `JWT__Audience`
- `JWT__AccessTokenMinutes`
- `DATA_INIT_MIGRATE_DATABASE`
- `DATA_INIT_SEED_DATA`
- `POSTGRES_DB`
- `POSTGRES_USER`
- `WEBAPP_PORT`
- `COMPOSE_PROJECT_NAME`
  (defaults to `assignment05-final2` in deploy scripts)

Rules:
- `JWT__Key` must be long and must not be committed
- `POSTGRES_PASSWORD` has no production default
- production CORS origins must be explicit public origins
- no production wildcard, localhost, or loopback CORS origins
- see [security-and-access.md](security-and-access.md) for CORS and token
  security details

## CI/CD

Root `.gitlab-ci.yml` triggers the assignment child pipeline.

Assignment child pipeline stages:
1. `assignment05_final2_client`: `npm ci`, `npm test`, `npm run build`
2. `assignment05_final2_build`: `dotnet build --configuration Release`
3. `assignment05_final2_test`: `dotnet test --configuration Release --no-build`
4. `assignment05_final2_postgresql_tests`: manual optional PostgreSQL/Testcontainers
   test slice
5. `assignment05_final2_docker_build`: backend image
6. `assignment05_final2_client_image`: standalone client image
7. `assignment05_final2_deploy`: backend deploy
8. `assignment05_final2_deploy_client`: manual standalone client deploy

Final2 architecture tests are part of the normal backend test project and run
in the standard test stage.

## Compose Validation

Development:

```bash
docker compose config
```

Production backend:

```bash
POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key \
  VITE_API_BASE_URL=https://api.example.test \
  docker compose -f docker-compose.prod.yml config
```

Production backend plus standalone client:

```bash
POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key \
  VITE_API_BASE_URL=https://api.example.test \
  docker compose --profile client -f docker-compose.prod.yml config
```

## Standalone Client

Build:

```bash
docker build \
  --build-arg VITE_API_BASE_URL=https://mtiker-cweb-a4.proxy.itcollege.ee \
  -t multi-gym-management-system-client:local \
  -f client/Dockerfile \
  client
```

Run with Compose profile:

```bash
VITE_API_BASE_URL=https://mtiker-cweb-a4.proxy.itcollege.ee \
CORS_ALLOWED_ORIGIN_CLIENT=https://mtiker-cweb-a4-client.proxy.itcollege.ee \
JWT__Key=... \
POSTGRES_PASSWORD=... \
docker compose --profile client -f docker-compose.prod.yml up -d --build
```

Client container routes:
- `GET /healthz` returns `200 ok`
- `GET /` redirects to `/client/`
- `GET /client/*` serves the SPA

## Deployment Smoke Check

Use after deploying backend and standalone client:

```bash
export BACKEND_URL="https://<backend-host>"
export CLIENT_URL="https://<client-host>"
export SMOKE_EMAIL="<smoke-user@example.com>"
export SMOKE_PASSWORD="<smoke-password>"
export SMOKE_GYM_CODE="<gym-code>"
# Optional when container URLs differ from the browser origin being tested:
export SMOKE_CORS_ORIGIN="https://<client-public-origin>"

bash scripts/smoke-deploy.sh
```

The script verifies:
- backend `/health`
- Swagger/OpenAPI JSON at `/swagger/v1/swagger.json`
- client `/healthz`
- CORS preflight from `SMOKE_CORS_ORIGIN` when set, otherwise from
  `CLIENT_URL`, to the backend account API
- API login
- refresh-token renewal
- authenticated tenant API read using the renewed JWT

Manual public checks:
- backend root loads
- `/health` returns success
- `/swagger` loads when enabled
- `/client` loads in Mode A
- standalone `/client/` loads in Mode B
- `/client/members`, `/client/member-workspace`, `/client/sessions`, and
  `/client/maintenance` deep links load
- login succeeds with a seeded or smoke user
- one tenant mutation succeeds
- Mode B CORS preflight from client host to backend succeeds

## Latest Recorded Validation

Latest Phase 12 validation recorded in the docs is from 2026-05-25:
- client tests and client production build passed
- focused CORS/backend tests passed
- production Compose config rendered for backend and standalone client profile
- live deployment smoke checks were not run from this machine

Phase 14 partial deployment validation from 2026-05-25:
- `C:\Program Files\Git\bin\bash.exe -n scripts/smoke-deploy.sh
  scripts/deploy-client.sh scripts/deploy.sh` passed
- `docker compose -f docker-compose.prod.yml config` passed with explicit
  backend/client production CORS origins
- `docker compose --profile client -f docker-compose.prod.yml config` passed
- `docker compose --profile client -f docker-compose.prod.yml build` passed
  after Docker Desktop was started
- local production-stack smoke passed with backend on `http://localhost:18083`,
  standalone client on `http://localhost:18081`, and
  `SMOKE_CORS_ORIGIN=https://mtiker-cweb-a4-client.proxy.itcollege.ee`
- `scripts/smoke-deploy.sh` was attempted against
  `https://mtiker-cweb-a4.proxy.itcollege.ee` and
  `https://mtiker-cweb-a4-client.proxy.itcollege.ee`; it failed at backend
  `/health` with HTTP 404
