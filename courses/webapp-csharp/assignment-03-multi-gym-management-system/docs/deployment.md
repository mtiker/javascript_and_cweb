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

The system can run in **two equivalent modes**:

### Mode A — embedded client (default, single host)

One image, one host. The React bundle is built into the backend Docker image
and served from `WebApp/wwwroot/client`. CORS is not exercised in the browser
because backend and SPA share the public origin.

Public route:
- `https://mtiker-cweb-4.proxy.itcollege.ee` — backend, MVC, REST API, Swagger,
  health check, embedded React app at `/client`

Proxy target:
- `http://192.168.181.122:83`

### Mode B — separate client host (Phase 8)

Two images on two hosts (or two ports on the same host behind two proxy entries).
The backend stays on `:83` exactly as in Mode A. The client runs in its own
nginx container on `:8081` (host) / `:8080` (container) and is proxied at a
second hostname.

Suggested layout:

| Component | Origin | Container port | Host port |
|---|---|---|---|
| Backend (REST + MVC + Swagger + health + legacy `/client`) | `https://mtiker-cweb-4.proxy.itcollege.ee` | `8080` | `83` |
| React client (nginx) | `https://mtiker-cweb-4-client.proxy.itcollege.ee` | `8080` | `8081` |

Mode A continues to work unchanged in Mode B — the embedded `/client` path on
the backend image is kept as a fallback. Mode B simply adds the second host.

Production deployment files:
- `Dockerfile` — backend image (still includes the embedded client build stage)
- `client/Dockerfile` — standalone client image (Node 20 build → nginx 1.27)
- `client/nginx.conf` — SPA static config served on `:8080`
- `docker-compose.prod.yml` — backend, postgres, and an opt-in `client` profile
- `scripts/deploy.sh` — backend stack
- `scripts/deploy-client.sh` — standalone client container (run after the
  backend image is up if Mode B is desired)

The backend Dockerfile has a dedicated Node 20 build stage for `client/`. It
runs `npm ci` and `npm run build`, then copies `client/dist` into
`WebApp/wwwroot/client` in the final ASP.NET Core runtime image. Removing this
embedded copy is **not** required for Mode B and is intentionally left in
place so single-host deployments still work.

## Required Environment Variables

Minimum production variables (Mode A, embedded client):
- `JWT__Key`
- `CORS_ALLOWED_ORIGIN`
- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `WEBAPP_PORT` when overriding the default host port `83`

Additional production variables (Mode B, separate client host):
- `CORS_ALLOWED_ORIGIN_CLIENT` — the public origin of the client container,
  e.g. `https://mtiker-cweb-4-client.proxy.itcollege.ee`. Bound to
  `Cors__AllowedOrigins__1` in `docker-compose.prod.yml`.
- `VITE_API_BASE_URL` — absolute backend URL baked into the client bundle at
  build time. Set as a CI/CD variable so the client image build picks it up.
- `CLIENT_PORT` — host port for the client container. Defaults to `8081`.
- `MULTI_GYM_CLIENT_IMAGE` — optional, lets the client service pull a
  pre-built image tag from a registry instead of building locally.

Optional variables:
- `JWT__Issuer`
- `JWT__Audience`
- `JWT__AccessTokenMinutes`
- `DATA_INIT_MIGRATE_DATABASE`
- `DATA_INIT_SEED_DATA`
- `COMPOSE_PROJECT_NAME`

`JWT__Key` must be a long secret value and must not be committed to
`appsettings.json` or any tracked documentation. `docker-compose.prod.yml`
refuses to start without it. `JWT__Issuer` and `JWT__Audience` default to
`MultiGymManagementSystem` in production Compose, but can be set explicitly in
GitLab CI/CD variables.

`CORS_ALLOWED_ORIGIN` must be a safe absolute public origin in production (no
`localhost`, loopback IPs, or wildcard origins). The backend fails fast on
startup if production CORS origins are missing or unsafe — see
`docs/cors-audit.md` and `docs/production-cors-audit.md`.

Set `CORS_ALLOWED_ORIGIN_CLIENT` whenever the client is hosted on a separate
public origin from the API. Empty values are filtered out, so leaving it unset
in Mode A is safe.

For local development, store JWT values as ASP.NET Core user secrets from
`src/WebApp`:

```powershell
dotnet user-secrets set "Jwt:Key" "<long-local-jwt-secret>"
dotnet user-secrets set "Jwt:Issuer" "MultiGymManagementSystem"
dotnet user-secrets set "Jwt:Audience" "MultiGymManagementSystem"
```

Data Protection keys are persisted in the application database via
`AppDbContext`, so protected MVC/cookie payloads remain valid across container
restarts as long as the database volume is preserved.

Forwarded headers are enabled in the middleware pipeline before HTTPS
redirection and authentication so reverse-proxy deployments keep correct
scheme/client metadata for auth and redirect behavior.

## Standalone client container (Mode B)

Build:

```bash
cd courses/webapp-csharp/assignment-03-multi-gym-management-system
docker build \
  --build-arg VITE_API_BASE_URL=https://mtiker-cweb-4.proxy.itcollege.ee \
  -t multi-gym-management-system-client:local \
  -f client/Dockerfile \
  client
```

Run via Compose profile:

```bash
VITE_API_BASE_URL=https://mtiker-cweb-4.proxy.itcollege.ee \
CORS_ALLOWED_ORIGIN_CLIENT=https://mtiker-cweb-4-client.proxy.itcollege.ee \
JWT__Key=... \
docker compose --profile client -f docker-compose.prod.yml up -d --build
```

Or, after a backend deploy, run only the client:

```bash
VITE_API_BASE_URL=https://mtiker-cweb-4.proxy.itcollege.ee \
./scripts/deploy-client.sh
```

The container exposes:
- `GET /healthz` — `200 ok`, used by Docker `HEALTHCHECK`
- `GET /` → `302 /client/`
- `GET /client/*` — SPA, with hashed assets cached for one year and
  `index.html` served `no-cache, must-revalidate`

## GitLab CI/CD

Repository level:
- root `.gitlab-ci.yml` triggers this assignment as a child pipeline

Assignment level:
- `courses/webapp-csharp/assignment-03-multi-gym-management-system/.gitlab-ci.yml`

Pipeline stages:
1. `client` — `assignment03_client`: `npm ci && npm test && npm run build`
2. `build` — `assignment03_build`: `dotnet build --Release`
3. `test` — `assignment03_test`: `dotnet test --Release --no-build`
4. `package`
   - `assignment03_docker_build` — backend image (with embedded client)
   - `assignment03_client_image` — standalone client image, build-arg
     `VITE_API_BASE_URL` from a CI/CD variable
5. `deploy`
   - `assignment03_deploy` — runs `scripts/deploy.sh` (default branch / tags)
   - `assignment03_deploy_client` — runs `scripts/deploy-client.sh`,
     `when: manual`, `allow_failure: true` (keeps Mode A working unconditionally)

Backend deploy script:

```bash
./scripts/deploy.sh
```

Client deploy script:

```bash
./scripts/deploy-client.sh
```

## Recommended VPS Smoke Check

Mode A (embedded client):

```bash
curl http://127.0.0.1:83/health
curl http://127.0.0.1:83/client
```

Mode B (separate client host) — run on the deployment host:

```bash
# Backend
curl -i http://127.0.0.1:83/health
curl -i http://127.0.0.1:83/client            # legacy embedded fallback still works

# Standalone client
curl -i http://127.0.0.1:8081/healthz         # 200 "ok"
curl -i http://127.0.0.1:8081/                # 302 -> /client/
curl -i http://127.0.0.1:8081/client/         # 200 text/html

# CORS preflight from client → backend
curl -i -X OPTIONS https://mtiker-cweb-4.proxy.itcollege.ee/api/v1/account/login \
  -H "Origin: https://mtiker-cweb-4-client.proxy.itcollege.ee" \
  -H "Access-Control-Request-Method: POST" \
  -H "Access-Control-Request-Headers: content-type, authorization, accept-language"
# Expect 204 with Access-Control-Allow-Origin matching the client host
```

Also verify, regardless of mode:
- `https://mtiker-cweb-4.proxy.itcollege.ee` loads
- Swagger loads
- The SPA shell loads at `https://<client-host>/client/` for routes
  `/client/`, `/client/members`, `/client/member-workspace`,
  `/client/coaching-workspace`, and `/client/finance-workspace`
- a seeded user can log in
- `multigym.admin@gym.local` can switch gyms
- one workflow mutation succeeds (for example invoice payment POST,
  coaching item decision PUT, or maintenance assignment PUT)
