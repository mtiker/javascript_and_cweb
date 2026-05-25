# JavaScript Assignment 06 — Express.js Todo Backend

Author: Martin Tikerpäe — UNI-ID `mtiker` — Code `232786IADB`.

This assignment reimplements the TalTech ToDo API (`https://taltech.akaver.com/api/v1`) in **Express.js + TypeScript + Postgres** and redeploys the existing Vue (A04) and React/Next.js (A05) clients against the new backend.

**Course brief:** *Reimplement `https://taltech.akaver.com/` API backend in express.js (Todo Tasks, auth. No swagger needed). Deploy app into your VPS as separate docker container. Reuse your already existing VUE and REACT clients for frontend — just change the base url and redeploy them as well into new docker containers.*

---

## Public URLs

| Service              | Host port | Container | Public URL                                              |
| -------------------- | --------- | --------- | ------------------------------------------------------- |
| Express API          | **86**    | `:3001`   | `https://mtiker-js-express.proxy.itcollege.ee`          |
| Vue client (A04)     | **87**    | `:80`     | `https://mtiker-js-vue-a6.proxy.itcollege.ee`           |
| React client (A05)   | **89**    | `:3000`   | `https://mtiker-js-react-a6.proxy.itcollege.ee`         |
| Postgres (internal)  | —         | `:5432`   | not exposed                                             |

> The proxy URLs above follow the same `mtiker-js-*.proxy.itcollege.ee` pattern as A05. Update them here and in [`.env.example`](.env.example) once the actual proxy entries are provisioned.

API health: `GET /api/v1/health` → `{ "status": "ok", "db": "postgres" }`.

---

## What the Express API exposes

Auth (no Bearer token required):

| Method | Path                              | Body                                       | Returns                  |
| ------ | --------------------------------- | ------------------------------------------ | ------------------------ |
| POST   | `/api/v1/Account/Register`        | `{ email, password, firstName, lastName }` | `IJwtResponse` (200)     |
| POST   | `/api/v1/Account/Login`           | `{ email, password }`                      | `IJwtResponse` (200/404) |
| POST   | `/api/v1/Account/RefreshToken`    | `{ jwt, refreshToken }`                    | `IJwtResponse` (200/400) |

Todo entities (Bearer JWT required):

| Method | Path                                 | Notes                              |
| ------ | ------------------------------------ | ---------------------------------- |
| GET    | `/api/v1/TodoTasks`                  | List user's tasks                  |
| GET    | `/api/v1/TodoTasks/:id`              | Read one                           |
| POST   | `/api/v1/TodoTasks`                  | Create                             |
| PUT    | `/api/v1/TodoTasks/:id`              | Update (partial body OK; at least one task field required) |
| DELETE | `/api/v1/TodoTasks/:id`              | 200 with no body on success        |
| GET / POST / PUT / DELETE | `/api/v1/TodoCategories[/:id]` | Same CRUD pattern           |
| GET / POST / PUT / DELETE | `/api/v1/TodoPriorities[/:id]` | Same CRUD pattern           |

Endpoint shapes match the original TalTech `IJwtResponse` and `ITodoTask` / `ITodoCategory` / `ITodoPriority` DTOs so the existing clients work unchanged except for the base URL.

`ListItems` and Swagger are intentionally not implemented (brief: *Todo Tasks, auth. No swagger needed*).

---

## Layout

```
assignment-06-express-api/
├── api/                       Express + Postgres backend (TypeScript, no swagger)
│   ├── src/
│   │   ├── index.ts           startup + graceful shutdown
│   │   ├── app.ts             middleware + routers + error handler
│   │   ├── auth.ts            bcrypt + JWT helpers
│   │   ├── routes/            Account, TodoTasks, TodoCategories, TodoPriorities
│   │   ├── middleware/        authenticate, errorHandler
│   │   ├── db/                pg pool + migrations + repositories
│   │   └── types/             Public DTOs (matches A04/A05 client shapes)
│   ├── Dockerfile             multi-stage build → node:22-alpine
│   └── package.json
├── vue-client/                Vue 3 client (copy of A04 with base URL swapped)
├── react-client/              Next.js client (copy of A05 with base URL swapped)
├── docker-compose.yml         All 4 services
└── .env.example
```

---

## Local development

```powershell
cd courses\javascript\assignment-06-express-api
Copy-Item .env.example .env
# Edit .env: pick a strong JWT_SECRET, decide POSTGRES_PASSWORD, etc.

docker compose up --build -d
```

That brings up:

- `mtiker-js-a06-postgres` (no host port)
- `mtiker-js-a06-express` on host port `86`
- `mtiker-js-a06-vue`     on host port `87`
- `mtiker-js-a06-react`   on host port `89`

Smoke-test:

```powershell
Invoke-RestMethod http://localhost:86/api/v1/health
# → status=ok, db=postgres
```

Run backend validation:

```powershell
cd api
npm ci
npm run build
npm test
```

The API tests use Vitest + supertest against the real Express routes, SQL migrations, repositories, bcrypt, and JWT helpers with an in-process Postgres-compatible test database (`pg-mem`). Covered flows include register/login/refresh-token rotation and reuse rejection, authenticated task CRUD, empty-update validation, and cross-user task read rejection.

Run the API outside Docker (e.g. against a local Postgres):

```powershell
cd api
npm install
Copy-Item .env.example .env
npm run dev
```

---

## VPS deployment

1. Provision DNS/proxy entries for `mtiker-js-express.proxy.itcollege.ee`, `mtiker-js-vue-a6.proxy.itcollege.ee`, `mtiker-js-react-a6.proxy.itcollege.ee` pointing at host ports `86`, `87`, `89` respectively.
2. Drop the production env file at `/home/gitlab-runner/mtiker-js-a06.env` on the VPS shared runner. Use [`.env.example`](.env.example) as the template — at minimum set `JWT_SECRET` and `POSTGRES_PASSWORD`.
3. Push to `main`. The CI pipeline ([.gitlab-ci.yml](.gitlab-ci.yml)) runs three parallel test jobs (API `tsc`, Vue `vue-tsc + vitest`, React `vitest`), then `docker compose build --pull`, then `scripts/deploy.sh` which copies the runner-side env file into `.env` and `docker compose up -d --build --remove-orphans`. The deploy script health-checks all three services before returning.
4. The clients are built with their proxy URL inlined via `VITE_API_BASE_URL` / `NEXT_PUBLIC_API_BASE_URL` — rebuild them after changing the public hostname.

Manual deploy if needed:

```bash
git pull
cd courses/javascript/assignment-06-express-api
cp /home/gitlab-runner/mtiker-js-a06.env .env  # or `cp .env.example .env` and edit
docker compose up --build -d
```

---

## Why each client only needed a base-URL swap

A04 (Vue) reads the API base URL from a single `import.meta.env.VITE_API_BASE_URL` in [`vue-client/src/config/env.ts`](vue-client/src/config/env.ts). A05 (Next.js) reads `process.env.NEXT_PUBLIC_API_BASE_URL` in [`react-client/src/services/apiClient.ts`](react-client/src/services/apiClient.ts) and [`react-client/src/services/AccountService.ts`](react-client/src/services/AccountService.ts). The Express backend serves the exact same `/api/v1/...` paths and DTO shapes as the TalTech backend, so no other client code had to change.

---

## Tech stack

- **Backend:** Express 5 · TypeScript (NodeNext modules) · `pg` (raw SQL, migrations runner) · bcrypt · `jsonwebtoken` (15-minute access tokens, single-use 256-bit refresh tokens, 7-day refresh TTL) · Helmet · `express-rate-limit`
- **Database:** Postgres 16 (`gen_random_uuid()` for primary keys)
- **Backend tests:** Vitest · supertest · `pg-mem`
- **Vue client:** Vue 3 · Vite · Pinia · axios · vee-validate + zod
- **React client:** Next.js 16 App Router · React 19 · axios · react-hook-form
- **Container runtime:** Docker Compose with a shared `pgdata` volume and Postgres healthcheck; the API auto-applies SQL migrations from `dist/db/migrations` on startup

Security notes:

- Refresh-token rotation is atomic: the old token is deleted with `DELETE ... RETURNING` inside the refresh transaction before a replacement token is stored. A reused token is rejected.
- CORS is allow-list driven by `CORS_ORIGIN`; if that env var is missing, cross-origin browser requests are denied instead of reflected.
- Login and register routes have a conservative per-IP rate limit. Password rules intentionally remain compatible with the course/API contract: required and at least 8 characters.
