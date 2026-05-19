# JavaScript Assignment 05 — TaskFlow (React + Next.js)

**Live URL:** `https://mtiker-js-react.proxy.itcollege.ee`
*(proxy target: `http://192.168.181.122:88`)*

Author: Martin Tikerpäe — UNI-ID `mtiker` — Code `232786IADB`.

TaskFlow is a React + Next.js (App Router) client for the TalTech ToDo
backend (`https://taltech.akaver.com`). It implements JWT + refresh-token
authentication, uses Context + `useReducer` for all shared state (no prop
drilling), and is shipped as a single Docker container behind the VPS
reverse proxy on host port `88`.

Course brief: *Write a REACT/NEXT.JS app, against
`https://taltech.akaver.com/` backend, ToDo entities. Implement JWT and
refresh-token based security. Use context and reducers as needed. No
property drilling. Deploy app into your VPS as separate docker container.*

## Features

- Login + Register against `/api/v1/Account/Login` and `/api/v1/Account/Register`.
- Silent refresh on 401 via `/api/v1/Account/RefreshToken` (axios interceptor).
- ToDo task CRUD with filtering by category, priority, completion state.
- Category and Priority management screens.
- Protected routes via `ProtectedRoute`; root layout redirects unauthenticated
  visitors to `/login` automatically.
- ErrorBoundary at the root layout.
- Bootstrap 5 + custom CSS (see `src/styles/globals.css`) with a TaskFlow
  gradient navbar, accent-colored priority pills, and a footer linking to
  the backend Swagger.

## Tech stack

- Next.js 16 App Router · React 19 · TypeScript (strict)
- axios (request + response interceptors for refresh)
- react-hook-form for the auth and todo editor forms
- Bootstrap 5 styling + handwritten CSS variables
- Docker (multi-stage, `output: 'standalone'`)

## Project layout

```
src/
  app/                  # App Router routes
    layout.tsx          # AuthProvider + TodoProvider + NavBar + Footer
    page.tsx            # /  → /todos or /login based on auth state
    login/ register/    # public auth pages
    todos/              # protected list
    todos/editor/       # create/edit (mode by ?id= / ?mode=new)
    categories/ priorities/  # protected admin pages
  components/           # NavBar, ProtectedRoute, FormField, TodoRow, …
  context/              # AuthContext.tsx, TodoContext.tsx
  reducers/             # authReducer.ts, todoReducer.ts
  services/             # apiClient, tokenStore, Account/Todo services
  domain/               # ITodoTask, ITodoCategory, ITodoPriority, …
  utils/errorUtils.ts   # ProblemDetails → user message
  styles/globals.css    # TaskFlow theme overrides
docs/
  plan.md  auth-flow.md  deployment.md  ai-prompts.md
```

## Run locally

```powershell
cd courses/javascript/assignment-05-react-todo
cp .env.example .env.local
npm install
npm run dev
```

Visit <http://localhost:3000>. The app talks to
`https://taltech.akaver.com` by default — set `NEXT_PUBLIC_API_BASE_URL` in
`.env.local` if you want to point at a different backend.

> No shared demo account exists on the new backend (rebuilt 2026-04-05).
> Register a fresh user in the app — the registration flow works end to
> end.

## Build

```powershell
npm run build
node .next/standalone/server.js   # serves the production build on http://localhost:3000
```

`next.config.ts` enables `output: 'standalone'`, so the Docker runtime
image only needs `.next/standalone` + `.next/static`. Note: `next start`
prints a warning under `output: 'standalone'` — use the standalone
`server.js` (or the Docker image) for production-like runs.

## Test

```powershell
npm test            # Vitest unit + RTL component tests (12 files, 63 cases)
npm run test:watch  # Vitest watch mode
npm run test:e2e    # Playwright browser-driven E2E (1 file, 2 cases)
npm run test:e2e:ui # Playwright UI mode

# One-time browser download for Playwright on a fresh checkout:
npx playwright install chromium
```

### Unit + component coverage (Vitest)

- `tests/reducers/authReducer.test.ts` — every `AuthAction` branch.
- `tests/reducers/todoReducer.test.ts` — items / categories / priorities CRUD.
- `tests/utils/errorUtils.test.ts` — ProblemDetails / aggregated-messages / field-error precedence.
- `tests/services/apiClient.test.ts` — refresh-on-401 happy path,
  no-refresh-token rejects, Bearer attachment.
- `tests/components/FormField.test.tsx` — labeled input + error markup.
- `tests/components/ProtectedRoute.test.tsx` — loading / redirect / pass-through gating.
- `tests/components/TodoRow.test.tsx` — render, toggle, edit-push, delete-with-confirm.
- `tests/app/login.test.tsx` — form validation, successful submit, server-error alert,
  post-registration banner.
- `tests/app/register.test.tsx` — all fields render, required-field validation,
  password-confirm mismatch, successful registration redirects, server-error alert.
- `tests/app/todoEditor.test.tsx` — new-mode renders + create + navigate, edit-mode
  prefill from `state.items`, required-field validation, cancel returns to /todos.
- `tests/app/categories.test.tsx` — empty state, list rendering, add-flow,
  short-name guard, delete-with-confirm, delete-dismissed, inline edit + save.
- `tests/app/priorities.test.tsx` — empty state, list rendering, add (name + sort),
  short-name guard, delete-with-confirm, inline edit + save with rebuilt `syncDt`.

### E2E coverage (Playwright, real browser)

`e2e/auth-todo.spec.ts` runs against the production build (`next start`) on
port 3100. Calls to `https://taltech.akaver.com` are intercepted with
`page.route()`, so the test is deterministic and works offline.

- **register → login → create todo end-to-end** — fills the register form,
  follows the `?registered=true` redirect, logs in, lands on `/todos`,
  creates a new todo via the editor, and asserts the row + category + priority
  pill render correctly.
- **logout from the navbar redirects to /login automatically** — verifies
  `AuthContext.logout()` + `ProtectedRoute` together flip the URL back to
  `/login` without manual navigation.

## Docker

Local image:

```powershell
docker compose up --build
# → http://localhost:3000
```

VPS:

```powershell
docker compose -f docker-compose.prod.yml up --build -d
# → host port 88 ←→ container 3000
```

The image accepts the build arg `NEXT_PUBLIC_API_BASE_URL`
(defaults to `https://taltech.akaver.com`). The variable is inlined at
build time because Next.js public env vars are baked into the client
bundle.

## CI/CD

`.gitlab-ci.yml` defines a child pipeline with two stages:

1. **build** — `docker:27-dind` builds and pushes
   `$CI_REGISTRY_IMAGE/a05-react-todo:<sha>`.
2. **deploy** — pulls/builds the image on the VPS and runs
   `docker compose -f docker-compose.prod.yml up -d`.

The deploy job expects `/home/gitlab-runner/mtiker-js-react.env` on the
runner host to contain any sensitive overrides — for this assignment that
file may simply set `NEXT_PUBLIC_API_BASE_URL=https://taltech.akaver.com`.

Root `.gitlab-ci.yml` triggers this child pipeline alongside the existing
A01/A02/A03 pipelines.

## Documentation

- [`docs/plan.md`](docs/plan.md) — layered implementation plan + decisions.
- [`docs/auth-flow.md`](docs/auth-flow.md) — JWT + refresh sequence diagram.
- [`docs/deployment.md`](docs/deployment.md) — Docker + VPS + CI notes.
- [`docs/ai-prompts.md`](docs/ai-prompts.md) — AI-usage log.

## Known limitations / follow-ups

- Token storage uses `localStorage`. An httpOnly cookie variant is
  acknowledged in the plan as a future improvement, but the assignment
  brief asks for a context/reducer flow which `localStorage` supports
  cleanly.
- VPS env file `/home/gitlab-runner/mtiker-js-react.env` needs to be
  created on the runner host before the first deploy.
