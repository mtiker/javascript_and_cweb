# Assignment 5 AI Usage Log

Tracks meaningful AI assistance used while building TaskFlow.
Format follows the repo rule in `AGENTS.md`.

## 2026-05-18 — Initial scaffolding pass

- **Context:** Course Assignment 5 — React/Next.js secure Todo against
  `https://taltech.akaver.com/` with JWT + refresh-token security,
  Context + reducers, no prop drilling, VPS Docker deployment.
- **Prompt summary:** "Build the React/Next.js app per the assignment.
  Reference material at `satiks-javascript-main.zip` may be copied; deploy
  port `88`; register a fresh user during defense; mirror the reference
  but adjust the UI a bit."
- **Files affected:** entire `courses/javascript/assignment-05-react-todo`
  tree — project config, `src/app/*`, `src/components/*`, `src/context/*`,
  `src/reducers/*`, `src/services/*`, `src/domain/*`, `src/utils/*`,
  `src/styles/globals.css`, `Dockerfile`, `docker-compose*.yml`,
  `.gitlab-ci.yml`, `docs/plan.md`, `docs/auth-flow.md`,
  `docs/deployment.md`, `README.md`.
- **What AI helped with:**
  - Translating the reference Next.js app into the assignment folder
    layout, verifying each service URL against the Swagger contract.
  - Auth flow: `tokenStore` + axios interceptor + `setOnTokenRefreshed`
    callback bridging the silent refresh back into `AuthContext`.
  - UI tweaks (TaskFlow brand, gradient navbar, custom priority badge
    palette, footer linking to Swagger) — kept Bootstrap as the base.
  - Production Dockerfile using Next.js `output: 'standalone'` with a
    non-root user.
  - GitLab CI child pipeline definition mirroring the existing A01/A02
    deployment pattern (port 88 on the shared runner host).
- **What AI got wrong / had to be corrected:**
  - Reference Dockerfile copied `/public*` which can fail if `public/`
    is absent; corrected to require `public/` to exist and document
    that.
  - Reference `axios` request interceptor used `config.headers.Authorization = …`
    directly; on the current axios typings this needs `headers.set?.(…)`
    to satisfy strict TS — adjusted in `apiClient.ts`.
  - Reference uses `localhost:3001` as a default API base URL. Updated
    the default to `https://taltech.akaver.com` so a fresh `npm run dev`
    works without `.env.local`.
  - Reference `categories/page.tsx` declared the form submit type as
    `SubmitEvent` (a DOM event), which conflicts with React's
    synthetic event — replaced with `FormEvent<HTMLFormElement>` in our
    copy.
- **Manual review:**
  - Compared all service URLs to the Swagger paths
    (`/api/v1/TodoTasks`, `/api/v1/TodoCategories`, `/api/v1/TodoPriorities`,
    `/api/v1/Account/{Login,Register,RefreshToken}`).
  - Walked through the refresh flow end to end to confirm no prop
    drilling (only `useAuth()` / `useTodo()` consumers).
- **Alternative solutions considered:**
  - httpOnly cookie session instead of `localStorage` — rejected for
    this assignment because the brief explicitly asks for a context +
    reducer JWT flow.
  - Vite + React + TS instead of Next.js — rejected at planning time;
    the reference is Next.js and the App Router matches the course
    brief.
  - Tailwind instead of Bootstrap — rejected to keep Docker image
    small and to match the reference styling primitives.
- **ADR/ERD notes:** None for this layer — entities (`ITodoTask`,
  `ITodoCategory`, `ITodoPriority`) are dictated by the backend
  contract.

## 2026-05-18 — Layer 3 + /api/health pass

- **Context:** Follow-up batch to add automated test coverage and a
  health-check endpoint the reverse proxy can hit.
- **Files affected:**
  - `src/app/api/health/route.ts` (new) — `force-dynamic` GET returning
    `{ status, service, timestamp }`.
  - `package.json` — added `test` / `test:watch` scripts and the Vitest
    + React Testing Library + jsdom + `@vitejs/plugin-react` dev deps.
  - `vitest.config.ts` (new) — jsdom env, `@/` alias, setup file.
  - `tests/setup.ts` — wires `@testing-library/jest-dom/vitest`.
  - `tests/reducers/authReducer.test.ts` — 7 cases covering every
    `AuthAction` branch.
  - `tests/reducers/todoReducer.test.ts` — 7 cases covering items +
    category + priority CRUD reducers.
  - `tests/utils/errorUtils.test.ts` — 7 cases for the
    ProblemDetails / aggregated-messages / field-errors precedence.
  - `tests/services/apiClient.test.ts` — 3 cases driving the axios
    interceptor: refresh-on-401 happy path (subscriber + retry +
    rotated tokens), no-refresh-token rejects, Bearer attachment.
  - `docs/plan.md` — layered checklist marked done.
  - `docs/ai-prompts.md` — this entry.
- **What AI got wrong / had to be corrected:**
  - First version of `apiClient.test.ts` returned a `{status: 401}`
    response from the fake adapter. Custom axios adapters must honor
    `validateStatus` themselves (the default xhr adapter calls
    `settle()`); the fix was to throw an `AxiosError` from the
    adapter on any non-2xx status so the response interceptor's error
    branch runs.
  - The `/api/health` route was originally `force-static`, which
    would freeze the timestamp at build time. Switched to
    `force-dynamic`.
- **Manual verification:**
  - `npm test` → 4 files, 24 tests, all green.
  - `npm run build` → still green, `/api/health` registered as
    `ƒ (Dynamic)`.
  - `npm run start` + `curl http://localhost:3000/api/health` →
    `{"status":"ok","service":"a05-react-todo","timestamp":"…"}`.
  - All public routes (`/`, `/login`, `/register`, `/todos`) return
    200 with HTML payloads (9–11 KB).

## 2026-05-18 — Component / RTL test pass

- **Context:** Extend Layer 3 coverage beyond pure reducers and the
  axios interceptor to the React components that actually render the
  user-facing flow.
- **Files affected:**
  - `tests/components/FormField.test.tsx` — labeled-input rendering
    and the `is-invalid` / `aria-invalid` / `invalid-feedback` markup
    when an error is supplied.
  - `tests/components/ProtectedRoute.test.tsx` — three branches:
    loading spinner, redirect to `/login`, render children when
    authenticated. Uses `vi.mock` for `next/navigation` and
    `@/context/AuthContext`.
  - `tests/components/TodoRow.test.tsx` — toggle, edit-pushes-to-editor,
    delete-with-confirm, and delete-dismissed-leaves-state. Uses
    `vi.spyOn(window, 'confirm')` to drive the confirmation prompt.
  - `tests/app/login.test.tsx` — form validation, successful submit
    (`router.push('/todos')`), server-error alert, and the
    `?registered=true` banner. Drives the form with
    `@testing-library/user-event`.
  - `package.json` — added `@testing-library/user-event` dev dep.
- **What AI got wrong:** First draft of the Login test paused on the
  validation step before realising RHF reports the first failing
  rule, so the assertion needed to use `findByText` rather than a
  synchronous `getByText` against the still-mounting form.
- **Manual verification:** `npm test` → 8 files, 40 tests, all green
  on the first full run.

## 2026-05-18 — Register + TodoEditor RTL pass

- **Context:** Final user-facing-page coverage gap. Register and the
  todo editor share the FormField + react-hook-form + context shape
  already exercised by the login test.
- **Files affected:**
  - `tests/app/register.test.tsx` — all five fields render, empty-form
    submit surfaces every required-field error, password-confirm
    mismatch blocks the submit, successful submit calls
    `auth.register(email, password, firstName, lastName)` and pushes
    to `/login?registered=true`, server rejection populates the
    alert.
  - `tests/app/todoEditor.test.tsx` — `?mode=new` renders the "New
    Todo" heading and category/priority options, empty submit blocks
    with three required errors, full submit calls `createTodo` with
    the expected payload then routes to `/todos`. `?id=task-1` with a
    seeded item prefills `taskName` and calls `updateTodo` with the
    merged values. Cancel returns to `/todos` without saving.
- **What AI got wrong / had to be corrected:**
  - First draft of the register test used `getByLabelText(/password/i)`
    which matched both the "Password" and "Confirm password" labels.
    Switched the primary password selector to the exact string
    `"Password"` and kept the regex for the confirm input.
  - The todo-editor test seeded `todoState.items = [EXISTING_TASK]`
    inside `beforeEach` *before* setting `searchParamsString`, so the
    page's prefill effect ran against a populated store. The mock
    factory captures `todoState` by reference at render time, which
    means mutating the seed after the first render does not back-fill
    the form.
- **Manual verification:** `npm test` → 10 files, 50 tests, all green.

## 2026-05-18 — Categories + Priorities RTL pass

- **Context:** Last user-facing page gap. Both admin pages use an
  inline edit pattern (Edit toggles the row's cells to inputs;
  Save/Cancel commit or discard) plus a top-of-page add form.
- **Files affected:**
  - `tests/app/categories.test.tsx` — empty state, list rendering, add
    flow, short-name guard, delete-with-confirm, delete-dismissed,
    inline edit + save (trims whitespace).
  - `tests/app/priorities.test.tsx` — empty state, list rendering,
    add flow (name + custom sort number), short-name guard,
    delete-with-confirm, inline edit + save (verifies rebuilt
    `syncDt` is sent).
- **What AI got wrong / had to be corrected:**
  - First draft mocked `fetchCategories` / `fetchPriorities` with bare
    `vi.fn()`. Both pages do `void fetchX().catch(() => undefined)`
    on mount, so the mock must return a Promise — fixed by setting
    the implementation to `() => Promise.resolve()` and using
    `mockClear` (not `mockReset`, which strips the implementation).
  - First draft of the inline-edit tests used
    `screen.getByRole("textbox")` after clicking Edit. The pages
    *also* render an add-form textbox at the top, so the query
    matched two elements and threw. Fixed by scoping with
    `within(screen.getByRole("row", { name: /Work|High/i }))`.
- **Manual verification:** `npm test` → 12 files, 63 tests, all green.

## 2026-05-19 — Playwright happy-path E2E

- **Context:** Last automated coverage tier — real-browser end-to-end
  pass that exercises register → login → create todo, plus a logout
  redirect check. The TalTech backend is intercepted with
  `page.route()` so the suite is deterministic and works offline.
- **Files affected:**
  - `package.json` — `@playwright/test` dev dep, `test:e2e` /
    `test:e2e:ui` scripts. Moved `@testing-library/user-event` to
    devDependencies where it belongs (had landed in `dependencies` by
    mistake on the earlier install).
  - `playwright.config.ts` — Chromium-only project, webServer on
    `next start --port 3100`, `retain-on-failure` traces.
  - `e2e/auth-todo.spec.ts` — two specs covering the full register →
    login → editor → list flow, and a navbar logout → /login redirect.
  - `.gitignore` — Playwright artifact directories.
  - `README.md` — Test section split into Vitest + Playwright with
    install instructions.
- **What AI got wrong / had to be corrected:**
  - First draft of the logout test used `page.addInitScript` to seed
    `localStorage` with auth tokens, then clicked logout, then
    navigated to `/todos` and expected `/login`. The redirect never
    happened because `addInitScript` runs on **every** navigation and
    re-seeded the tokens after logout. Fixed by running the actual
    login flow and relying on `ProtectedRoute`'s effect to redirect
    automatically when `isAuthenticated` flips.
  - First draft of the happy-path test used `page.getByText("High")`
    to find the priority badge. With Bootstrap's badge markup and
    other matches in the DOM that was flaky. Fixed by scoping to the
    row (`getByRole("row", { name: /write defense slides/i })`) and
    asserting on the `.tf-priority-badge` locator.
  - `next start` prints a warning under `output: 'standalone'`. It
    still serves correctly so the webServer config keeps using it —
    swapping to `node .next/standalone/server.js` would need a
    pre-step that copies `.next/static` and `public/` into the
    standalone directory (already done by the Dockerfile, not by the
    local build).
- **Manual verification:** `npx playwright test` → 2 passed (8.2 s
  total wall, Chromium headless).

## Follow-up batches

- VPS env file `/home/gitlab-runner/mtiker-js-react.env` on the runner
  host, then trigger the first pipeline run.
