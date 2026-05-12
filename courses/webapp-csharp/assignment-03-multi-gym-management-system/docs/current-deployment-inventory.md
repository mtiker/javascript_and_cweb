# Current Deployment Inventory

**Audited:** 2026-05-11

---

## Environments

| Environment | URL | How started |
|-------------|-----|-------------|
| Production backend / embedded client (school server) | `https://mtiker-cweb-4.proxy.itcollege.ee` | GitLab CI deploy stage to `scripts/deploy.sh`; not live-smoke-tested in the 2026-05-11 local pass |
| Production separate client candidate | expected separate proxy host, for example `https://mtiker-cweb-4-client.proxy.itcollege.ee` | `docker compose --profile client -f docker-compose.prod.yml ...` or `scripts/deploy-client.sh`; Compose config validates, public host not smoke-tested |
| Local production (Docker full-stack) | `http://localhost:83` | `docker compose -f docker-compose.prod.yml up` |
| Local development | `http://localhost:5107` (HTTP) / `https://localhost:7245` (HTTPS) | `dotnet run --project src/WebApp` |
| React dev server | `http://localhost:5173` | `cd client && npm run dev` |

Latest local deployment-related validation:
- `docker compose config` passed for development PostgreSQL.
- `POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key VITE_API_BASE_URL=https://api.example.test docker compose -f docker-compose.prod.yml config` passed for backend/PostgreSQL production config.
- `POSTGRES_PASSWORD=dummy JWT__Key=dummy-long-key VITE_API_BASE_URL=https://api.example.test docker compose --profile client -f docker-compose.prod.yml config` passed for backend/PostgreSQL plus standalone client config.
- `cd client && npm run build` passed.
- Public URL health, login, CORS preflight, and separate client health were not checked against a live deployment in this pass.

---

## 1. Docker — Development (`docker-compose.yml`)

Starts **PostgreSQL only**. Backend runs via `dotnet run`.

```yaml
services:
  postgres:
    image: postgres:16
    ports: "5432:5432"
    environment:
      POSTGRES_DB: multi_gym_management_system
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    volumes:
      - multi-gym-postgres-data:/var/lib/postgresql/data
    healthcheck: pg_isready every 5s, 15 retries
```

**To start:**
```bash
# From assignment root
docker compose up -d
```

**Then run backend:**
```bash
cd src/WebApp
dotnet run
```

Backend connects to Postgres at `Host=127.0.0.1;Port=5432;Database=multi_gym_management_system;Username=postgres;Password=postgres` (from `appsettings.json`).

`Jwt:Key` must be set — add to `appsettings.Development.json` (already done, see that file).

---

## 2. Docker — Production (`docker-compose.prod.yml`)

Starts **PostgreSQL + ASP.NET Core backend**. React SPA is embedded in the backend image. The optional `client` profile also renders a standalone nginx-hosted React client service.

```yaml
services:
  postgres:
    image: postgres:16
    volumes: multi-gym-postgres-data
    healthcheck: pg_isready

  web:
    image: ${MULTI_GYM_IMAGE:-multi-gym-management-system:local}
    build:
      context: .
      dockerfile: Dockerfile
    depends_on:
      postgres: { condition: service_healthy }
    ports: "${WEBAPP_PORT:-83}:8080"
```

**Required environment variables:**

| Variable | Required | Default | Notes |
|----------|----------|---------|-------|
| `JWT__Key` | **Yes** | none | Fails startup if missing |
| `JWT__Issuer` | No | `MultiGymManagementSystem` | |
| `JWT__Audience` | No | `MultiGymManagementSystem` | |
| `JWT__AccessTokenMinutes` | No | `60` | |
| `Cors__AllowedOrigins__0` | No | `https://mtiker-cweb-4.proxy.itcollege.ee` | Set to frontend origin if separately hosted |
| `CORS_ALLOWED_ORIGIN_CLIENT` / `Cors__AllowedOrigins__1` | No | empty | Set to the standalone client origin for separate hosting |
| `VITE_API_BASE_URL` | Required for separate client build | none | API base URL baked into the standalone client image |
| `POSTGRES_DB` | No | `multi_gym_management_system` | |
| `POSTGRES_USER` | No | `postgres` | |
| `POSTGRES_PASSWORD` | **Yes** | none | Required by production Compose and `scripts/deploy.sh` |
| `WEBAPP_PORT` | No | `83` | Host port |
| `DATA_INIT_MIGRATE_DATABASE` | No | `true` | |
| `DATA_INIT_SEED_DATA` | No | `true` | |

**To start:**
```bash
# Minimum required variables:
POSTGRES_PASSWORD=your-db-password JWT__Key=your-256-bit-secret docker compose -f docker-compose.prod.yml up -d
```

---

## 3. Dockerfile — Multi-stage build

```
Stage 1: node:20-alpine    client/  →  npm ci && npm run build  →  /client/dist
Stage 2: dotnet/sdk:10.0   src/     →  dotnet publish           →  /app/publish
Stage 3: dotnet/aspnet:10.0         →  copies publish + dist    →  image
```

**Key line:** `COPY --from=client-build /client/dist ./wwwroot/client`

React SPA is embedded in the backend image under `wwwroot/client/`. It is served by ASP.NET Core at `/client/*` in Mode A. Mode B adds a separate nginx client image from `client/Dockerfile`, but the embedded fallback remains.

**Exposed port:** `8080` (mapped to host port via `WEBAPP_PORT`)

---

## 4. React hosting: production vs development

| | Development | Production |
|--|-------------|-----------|
| Server | Vite dev server on `localhost:5173` | Mode A: ASP.NET Core on same host as API. Mode B: nginx standalone client container |
| Origin | `http://localhost:5173` - different origin from API | Mode A: same origin as API. Mode B: separate public client origin |
| CORS active? | Yes - API must allow `localhost:5173` | Mode A: no. Mode B: yes, backend must include the client origin |
| API URL | `VITE_API_BASE_URL=https://localhost:7245` (from `.env.example`) | Mode A: `window.location.origin`. Mode B: `VITE_API_BASE_URL` build arg |

**How API URL is resolved** (`client/src/lib/auth.tsx` — `resolveApiBaseUrl`):
```ts
return explicitBaseUrl ?? (isProduction ? window.location.origin : "https://localhost:7245");
```
In Mode A production (`import.meta.env.PROD = true`) and no `VITE_API_BASE_URL`
set, calls go to the same origin. In Mode B, `VITE_API_BASE_URL` must be set
when building the standalone client image.

**Risk:** The separate client hosting path now has Docker/Compose/build
evidence, but no public-host smoke evidence was captured in this pass. Do not
claim live separate-client deployment until `/healthz`, direct `/client/*`
routes, login, and production CORS preflight pass against the real host.

---

## 5. CI/CD — GitLab CI (`.gitlab-ci.yml`)

Trigger: any change under `courses/webapp-csharp/assignment-03-multi-gym-management-system/**/*`

| Stage | Job | Script | Condition |
|-------|-----|--------|-----------|
| `client` | `assignment03_client` | `npm ci && npm test && npm run build` (Docker, node:20-alpine) | Any push |
| `build` | `assignment03_build` | `dotnet restore && dotnet build --Release` | After client |
| `test` | `assignment03_test` | `dotnet test --Release --no-build` | After build |
| `package` | `assignment03_docker_build` | `docker build ...` | After test |
| `package` | `assignment03_client_image` | standalone client image build | After client |
| `deploy` | `assignment03_deploy` | `./scripts/deploy.sh` | After package, main branch or tag only |
| `deploy` | `assignment03_deploy_client` | `./scripts/deploy-client.sh` | Manual / optional |

**Notes:**
- Runner tag: `shared`
- Deploy job requires environment secrets (JWT key, DB password, etc.) configured in GitLab CI/CD variables.
- PostgreSQL integration tests (`PostgreSqlPersistenceTests`) are **not** run in CI (require `RUN_POSTGRES_TESTS=1`).
- CI backend tests run against SQLite in-memory (via `CustomWebApplicationFactory`).
- The separate client deploy job is manual/optional; a passing default pipeline
  alone does not prove separate public client hosting is live.

---

## 6. Ports summary

| Component | Dev port | Prod port | Protocol |
|-----------|----------|-----------|---------|
| ASP.NET Core HTTP | 5107 | — | HTTP |
| ASP.NET Core HTTPS | 7245 | 8080 (container) / 83 (host) | HTTPS (dev), HTTP (prod container) |
| React Vite dev | 5173 | n/a (embedded) | HTTP |
| PostgreSQL | 5432 | 5432 (internal only) | TCP |
| Swagger UI | 7245/swagger | 83/swagger | via backend |
| Health check | 7245/health | 83/health | via backend |

---

## 7. Seed data

Applied on startup when `DataInitialization:SeedData=true` (default).

The `EnsureDemoUsersAsync` method **always** resets demo user passwords on every startup, regardless of whether data already exists. This ensures passwords are correct even on a pre-seeded database.

**Demo users** (password: `GymStrong123!`):

| Email | Role | Gym |
|-------|------|-----|
| `systemadmin@gym.local` | SystemAdmin | Platform |
| `support@gym.local` | SystemSupport | Platform |
| `billing@gym.local` | SystemBilling | Platform |
| `admin@peakforge.local` | GymAdmin | peak-forge |
| `member@peakforge.local` | Member | peak-forge |
| `trainer@peakforge.local` | Trainer | peak-forge |
| `caretaker@peakforge.local` | Caretaker | peak-forge |
| `multigym.admin@gym.local` | GymAdmin | north-star |

**Seeded gyms:**
- `peak-forge` — full demo data (members, staff, sessions, bookings, memberships, equipment, maintenance tasks)
- `north-star` — basic gym

---

## 8. Local development quick-start

```bash
# 1. Start PostgreSQL
docker compose up -d

# 2. Run backend (migrates and seeds automatically)
cd src/WebApp
dotnet run

# 3. Open Swagger UI
# https://localhost:7245/swagger

# 4. (Optional) Run React dev server in another terminal
cd ../../client   # back to assignment root, then into client/
npm ci
npm run dev
# React at http://localhost:5173/
# Points to API at https://localhost:7245 (via VITE_API_BASE_URL in .env.development or vite.config)

# 5. Run all tests
cd ..   # back to assignment root
dotnet test multi-gym-management-system.slnx
cd client && npm test
```
