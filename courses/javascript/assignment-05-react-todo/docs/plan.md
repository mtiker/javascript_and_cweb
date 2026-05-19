# Assignment 5 — React/Next.js Secure Todo Plan

Course brief: build a React/Next.js client against the TalTech ToDo backend
(`https://taltech.akaver.com/`), with JWT + refresh-token rotation, Context +
`useReducer` state (no prop drilling), Dockerized and deployed to the VPS as a
separate container.

## Stack decisions

- Next.js 16 App Router + React 19 + TypeScript (matches the reference zip).
- Axios for HTTP, with request/response interceptors driving the refresh-token
  retry flow.
- Bootstrap 5 + custom CSS accents for styling (kept minimal so the Docker
  image stays small).
- `react-hook-form` for ergonomic auth/todo forms.
- Vitest + React Testing Library for unit tests (follow-up batch).
- Multi-stage Docker build using `output: 'standalone'`; runtime image is
  `node:20-alpine` running `server.js`.

## Folder layout

```
courses/javascript/assignment-05-react-todo/
  README.md                       # live URL pinned at top
  .dockerignore .env.example .gitignore .gitlab-ci.yml
  Dockerfile docker-compose.yml docker-compose.prod.yml
  next.config.ts package.json tsconfig.json eslint.config.mjs
  src/
    app/
      layout.tsx page.tsx
      login/page.tsx
      register/page.tsx
      todos/page.tsx
      todos/editor/page.tsx
      categories/page.tsx
      priorities/page.tsx
    components/
      NavBar.tsx ProtectedRoute.tsx BootstrapActivation.tsx
      FormField.tsx TodoRow.tsx ErrorBoundary.tsx
    context/AuthContext.tsx context/TodoContext.tsx
    reducers/authReducer.ts reducers/todoReducer.ts
    services/
      apiClient.ts tokenStore.ts AccountService.ts
      TodoTaskService.ts TodoCategoryService.ts TodoPriorityService.ts
    domain/
      IJWTResponse.ts ILoginData.ts IRegisterData.ts IRefreshTokenData.ts
      IProblemDetails.ts IMessage.ts
      ITodoTask.ts ITodoCategory.ts ITodoPriority.ts index.ts
    utils/errorUtils.ts
    styles/globals.css
  docs/
    plan.md auth-flow.md deployment.md ai-prompts.md
```

## Auth flow

```
Browser ──login──▶ POST /api/v1/Account/Login ─► {token, refreshToken}
   │
   │  tokenStore.setTokens()       ◄── (mirror in module-scope store)
   │  dispatch LOGIN_SUCCESS       ◄── (AuthContext reducer)
   │  localStorage persist         ◄── (jwt + refresh + email)
   │
   │── any protected API call ──┐
   │                            │ apiClient request interceptor
   │                            │   adds  Authorization: Bearer <jwt>
   │                            ▼
   │                       backend → 401 expired
   │                            │
   │   apiClient response interceptor
   │     POST /api/v1/Account/RefreshToken { jwt, refreshToken }
   │       on success:
   │         tokenStore.setTokens()
   │         onTokenRefreshed callback → AuthContext dispatch TOKEN_REFRESHED
   │         retry original request with new Bearer
   │       on failure:
   │         tokenStore.clearTokens()
   │         redirect to /login
   └── logout: dispatch LOGOUT → clears tokenStore + localStorage
```

Invariant: `tokenStore` is the synchronous source of truth for the active
request. `AuthContext` mirrors it via the `setOnTokenRefreshed` callback —
this is what avoids prop drilling tokens into the axios interceptor.

## Layered checklist

- [x] Layer 1 — scaffold, domain, auth
- [x] Layer 2 — Todo CRUD + categories + priorities
- [x] Layer 3 — Vitest + RTL coverage of reducers, errorUtils, refresh flow,
      every component, and every user-facing page (12 files, 63 cases)
- [x] Layer 4 — Dockerfile, docker-compose.prod.yml, .gitlab-ci.yml (VPS deploy pending first push)
- [x] Layer 5 — root README/CI integration + AI usage log + auth-flow doc
- [x] `/api/health` Next route handler
- [x] Layer 6 — Playwright real-browser happy-path E2E (register → login →
      create todo) plus a logout-redirect spec

## Deploy slot

- Host port: `88` (continues A01=81, A02=82, A04=84 pattern; A03 docker-host
  reserves 83).
- Container port: `3000` (Next.js standalone server).
- Public URL (target): `https://mtiker-js-react.proxy.itcollege.ee`.
- Backend: `https://taltech.akaver.com` (Swagger
  `https://taltech.akaver.com/swagger/index.html`).

## UI tweaks vs reference

- App brand renamed to **TaskFlow** with an accent color from CSS variables.
- Navbar uses a `primary` gradient instead of plain `bg-dark`.
- Auth pages get a short tagline + softer card shadow.
- Priority badge palette retuned (info / primary / warning / danger).
- Subtle footer with backend link added to root layout.

## Open follow-ups

- Demo account: register a fresh user during defense (no shared seed account).
- E2E happy path: add Playwright later if time permits.
- VPS env file `/home/gitlab-runner/mtiker-js-react.env` needs to be created
  on the runner host before the first deploy.
