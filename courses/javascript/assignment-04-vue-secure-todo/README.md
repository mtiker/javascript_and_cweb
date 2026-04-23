Public URL: `https://mtiker-js-vue.proxy.itcollege.ee`

# JavaScript Assignment 04 - Vue Secure Todo

Assignment 04 is a Vue 3 + TypeScript Todo frontend built against the shared TalTech backend at `https://taltech.akaver.com/api/v1`.

The app is intentionally structured as a reusable secure frontend base:
- Vue Router with auth-aware redirects
- Pinia stores for auth, catalogs, tasks, and toasts
- JWT access-token handling with refresh-token retry on `401`
- route-level lazy loading plus base-URL-aware router history
- first-run onboarding for empty category/priority catalogs
- guarded category/priority deletion when tasks still reference catalog items
- UTF-8-safe Estonian demo data and a browser favicon/logo
- assignment-local Docker, CI/CD, and deployment files

The backend was overhauled on April 5, 2026, so fresh accounts start with empty tasks, categories, and priorities. This frontend explicitly guides the user through that first-run setup and can seed a richer manual-testing workspace from the Catalogs page.

## Requirement Coverage

- Vue 3 + TypeScript + Vite
- Vue Router with guest-only and protected routes
- Pinia state management
- JWT + refresh-token security flow against the shared backend
- Todo task CRUD
- Todo category CRUD
- Todo priority CRUD
- dashboard summary view
- first-run catalog onboarding and quick-start preset
- catalog delete protection for referenced categories and priorities
- one-click manual-testing seed data with categories, priorities, and varied task states
- UTF-8 text delivery for Estonian characters such as `ä`, `ö`, and `ü`
- responsive UI with loading, empty, filtered-empty, success, and error states
- Vitest unit and component/integration tests
- Docker, GitLab child pipeline, and VPS deployment files

## Route Map

- `/login`
- `/register`
- `/app/dashboard`
- `/app/tasks`
- `/app/catalogs`

Routing rules:
- anonymous users trying to open `/app/...` are redirected to `/login`
- signed-in users opening guest routes are redirected back into the app
- signed-in users without at least one category and one priority are redirected to `/app/catalogs`
- if auth is lost while still on `/app/...`, the app redirects immediately to `/login` with `redirect` preserved

## Security Design

- tokens are stored in `sessionStorage`, not `localStorage`
- every API request attaches `Authorization: Bearer ...`
- the Axios client performs a single-flight refresh-token retry on `401`
- refresh failure clears the session immediately
- logout is client-side session clearing because the public backend does not expose a logout/revoke endpoint
- only non-sensitive UI preferences are stored locally

## Todo Domain Notes

The backend DTOs use field names such as:
- `taskName`
- `taskSort`
- `createdDt`
- `todoCategoryId`
- `todoPriorityId`
- `syncDt`

The frontend maps those into cleaner client models so the UI is not tightly coupled to the backend naming quirks or undocumented extra fields.

## Quick Start

```bash
npm install
npm run check
npm test
npm run build
npm run dev
```

Default dev API target:

- `https://taltech.akaver.com/api/v1`

Optional override:

```bash
VITE_API_BASE_URL=https://taltech.akaver.com/api/v1 npm run dev
```

For Docker/VPS deployment, `VITE_API_BASE_URL` is consumed at image build time through the Compose build args.

## Manual Testing Seed Data

After registering or signing in, open `/app/catalogs` and choose `Seed demo workspace`.

The seed flow uses the real authenticated API and is safe to rerun because existing category, priority, and task names are skipped. It creates:

- categories: `Töö`, `Õppimine`, `Kodu`, `Üritused`
- priorities: `Kõrge`, `Keskmine`, `Madal`
- five tasks covering open, overdue, completed, archived, future-due, and no-due-date states

Use the seeded data to test dashboard metrics, search with `ä ö ü`, category and priority filters, status filters, create/edit/delete flows, completion toggles, archive/restore, and filtered-empty states.

## Test Coverage

Current automated coverage includes:
- DTO mapping and request-payload helpers
- token storage behavior
- refresh-token single-flight retry logic
- task filtering, sorting, and metrics
- auth store login/register flows
- router guard redirects, including expired-session recovery during protected setup
- catalog onboarding, quick-start setup, and full demo seed setup
- task create/edit/delete, empty-state behavior, and startup load-error states
- UTF-8 preservation for Estonian characters in mapped data and outgoing API payloads

Run:

```bash
npm test
npm run coverage
```

## Docker and Deployment

Files:

```text
assignment-04-vue-secure-todo/
  .gitlab-ci.yml
  Dockerfile
  docker-compose.prod.yml
  nginx/default.conf
  scripts/deploy.sh
```

Default production host port:

- `84`

Health endpoint:

- `/healthz`

Local container validation:

```bash
docker compose -f docker-compose.prod.yml build
docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml ps
```

Open:

- `http://localhost:84`
- `http://localhost:84/healthz`

Stop:

```bash
docker compose -f docker-compose.prod.yml down
```

## CI/CD Variables

Configure where needed:

- `JAVASCRIPT_A04_PORT`
- `JAVASCRIPT_A04_IMAGE`
- `COMPOSE_PROJECT_NAME`
- `VITE_API_BASE_URL`

## Known Limitations

- The shared backend provides no logout/revoke endpoint, so logout is client-side session clearing only.
- Search, filtering, and sorting are client-side UX features; the public backend does not expose server-side query parameters for them.
