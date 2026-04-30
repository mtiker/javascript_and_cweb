# Current Deployment Inventory

**Audited:** 2026-04-27

---

## Environments

| Environment | URL | How started |
|-------------|-----|-------------|
| Production (school server) | `https://mtiker-cweb-4.proxy.itcollege.ee` | GitLab CI deploy stage → `scripts/deploy.sh` |
| Local production (Docker full-stack) | `http://localhost:83` | `docker compose -f docker-compose.prod.yml up` |
| Local development | `http://localhost:5107` (HTTP) / `https://localhost:7245` (HTTPS) | `dotnet run --project src/WebApp` |
| React dev server | `http://localhost:5173` | `cd client && npm run dev` |

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

Starts **PostgreSQL + ASP.NET Core backend**. React SPA is embedded in the backend image.

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
| `POSTGRES_DB` | No | `multi_gym_management_system` | |
| `POSTGRES_USER` | No | `postgres` | |
| `POSTGRES_PASSWORD` | No | `postgres` | Change in production |
| `WEBAPP_PORT` | No | `83` | Host port |
| `DATA_INIT_MIGRATE_DATABASE` | No | `true` | |
| `DATA_INIT_SEED_DATA` | No | `true` | |

**To start:**
```bash
# Minimum required variable:
JWT__Key=your-256-bit-secret docker compose -f docker-compose.prod.yml up -d
```

---

## 3. Dockerfile — Multi-stage build

```
Stage 1: node:20-alpine    client/  →  npm ci && npm run build  →  /client/dist
Stage 2: dotnet/sdk:10.0   src/     →  dotnet publish           →  /app/publish
Stage 3: dotnet/aspnet:10.0         →  copies publish + dist    →  image
```

**Key line:** `COPY --from=client-build /client/dist ./wwwroot/client`

React SPA is embedded in the backend image under `wwwroot/client/`. It is served by ASP.NET Core at `/client/*` — not by a separate server.

**Exposed port:** `8080` (mapped to host port via `WEBAPP_PORT`)

---

## 4. React hosting: production vs development

| | Development | Production |
|--|-------------|-----------|
| Server | Vite dev server on `localhost:5173` | ASP.NET Core on same host as API |
| Origin | `http://localhost:5173` — **different origin from API** | Same origin as API (e.g., `http://localhost:83/client`) |
| CORS active? | Yes — API must allow `localhost:5173` | No — same-origin request, CORS not triggered |
| API URL | `VITE_API_BASE_URL=https://localhost:7245` (from `.env.example`) | `window.location.origin` (auto-detected in `auth.tsx:resolveApiBaseUrl`) |

**How API URL is resolved** (`client/src/lib/auth.tsx` — `resolveApiBaseUrl`):
```ts
return explicitBaseUrl ?? (isProduction ? window.location.origin : "https://localhost:7245");
```
In production (`import.meta.env.PROD = true`) and no `VITE_API_BASE_URL` set, calls go to the same origin. No cross-origin, no CORS needed.

**Risk:** In production the React client is NOT separately hosted. It is a static file served by the backend. CORS configuration exists and is enforced, but in practice no cross-origin call is made in the Docker production stack. Only the dev environment crosses origins.

---

## 5. CI/CD — GitLab CI (`.gitlab-ci.yml`)

Trigger: any change under `courses/webapp-csharp/assignment-03-multi-gym-management-system/**/*`

| Stage | Job | Script | Condition |
|-------|-----|--------|-----------|
| `client` | `assignment03_client` | `npm ci && npm test && npm run build` (Docker, node:20-alpine) | Any push |
| `build` | `assignment03_build` | `dotnet restore && dotnet build --Release` | After client |
| `test` | `assignment03_test` | `dotnet test --Release --no-build` | After build |
| `package` | `assignment03_docker_build` | `docker build ...` | After test |
| `deploy` | `assignment03_deploy` | `./scripts/deploy.sh` | After package, main branch or tag only |

**Notes:**
- Runner tag: `shared`
- Deploy job requires environment secrets (JWT key, DB password, etc.) configured in GitLab CI/CD variables.
- PostgreSQL integration tests (`PostgreSqlPersistenceTests`) are **not** run in CI (require `RUN_POSTGRES_TESTS=1`).
- CI backend tests run against SQLite in-memory (via `CustomWebApplicationFactory`).

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
