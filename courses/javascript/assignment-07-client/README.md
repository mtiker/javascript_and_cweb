# Assignment 07 — Full client app (Multi-Gym)

> Target deploy URL: `https://mtiker-js-a07.proxy.itcollege.ee`
> Backend (cweb Assignment 05 Final2): `https://mtiker-cweb-4.proxy.itcollege.ee`

React + Vite + TypeScript client for the C# Web Multi-Gym Management
backend (`courses/webapp-csharp/assignment05_final2`). Implements the
**user flow** (Member) and the **organiser flow** (GymAdmin) over the
versioned REST API at `/api/v1/...` with JWT + refresh-token rotation.

Built on the Lovable React template (TanStack Start + TanStack Router +
TanStack Query + Tailwind v4 + shadcn/ui). The client lives in
`courses/javascript/assignment-07-client` and is wired directly to the
cweb Assignment 05 Final2 REST API.

## Scope

User (Member):

- Register / login (JWT + refresh)
- Browse training sessions, filter by category / search
- View a session and book / cancel a spot
- View own memberships and payments (member-workspace)
- Profile (session + member profile)

Organiser (GymAdmin / GymOwner):

- Manage members (list, add, delete)
- Manage training categories (list, add, delete)
- Manage training sessions (list with booking counts, add)

Out of scope on purpose: maintenance, payments creation, SystemAdmin
platform views, full i18n. QR / checkpoint / map features in the
generic assignment-7 spec are for the `nutikas.akaver.com` orienteering
backend — not applicable to this gym backend.

## Endpoints used

All authenticated calls go through `src/lib/api/client.ts`. Tenant base:
`/api/v1/{gymCode}`.

| Purpose                     | Method + path                                           |
| --------------------------- | ------------------------------------------------------- |
| Login                       | POST `/api/v1/account/login`                            |
| Register                    | POST `/api/v1/account/register`                         |
| Refresh token               | POST `/api/v1/account/renew-refresh-token`              |
| Logout                      | POST `/api/v1/account/logout`                           |
| Switch gym / role           | POST `/api/v1/account/switch-gym` / `switch-role`       |
| Sessions                    | GET/POST/PUT `/api/v1/{gym}/training-sessions[...]`     |
| Categories                  | GET/POST/PUT/DELETE `/api/v1/{gym}/training-categories` |
| Members                     | GET/POST/PUT/DELETE `/api/v1/{gym}/members`             |
| Current member              | GET `/api/v1/{gym}/members/me`                          |
| Member workspace            | GET `/api/v1/{gym}/member-workspace/me`                 |
| Bookings                    | GET/POST `/api/v1/{gym}/bookings`                       |
| Booking attendance / cancel | PUT `/api/v1/{gym}/bookings/{id}/attendance`            |

## Run

```sh
npm install
npm run dev
```

`VITE_API_BASE_URL` overrides the backend URL (default:
`https://mtiker-cweb-4.proxy.itcollege.ee`).

```sh
npm run dev
```

On Windows PowerShell:

```powershell
$env:VITE_API_BASE_URL = "https://mtiker-cweb-4.proxy.itcollege.ee"
npm run dev
```

## Notes on tenancy

The client uses the `activeGymCode` from the session payload as the
tenant prefix for all `/api/v1/{gym}/...` calls. If your account has no
active gym (no membership/role assignment), the UI shows a notice on
each protected page and the user can ask a GymAdmin to add them.

Seeded local/demo accounts from the cweb backend:

- Gym admin: `admin@peakforge.local` / `GymStrong123!`
- Member: `member@peakforge.local` / `GymStrong123!`

## Deployment

The app ships as a single Node SSR container. The TanStack Start build target
is Node (the `@cloudflare/vite-plugin` is disabled in `vite.config.ts` via
`cloudflare: false` — `wrangler.jsonc` and the CF-shaped `src/server.ts`
wrapper remain in the tree but are unused in production). At runtime,
`start-node.mjs` boots [srvx](https://srvx.h3.dev) on `:3000`, mounts
`dist/client/` for hashed assets and images, and forwards everything else
to the TanStack Start SSR handler in `dist/server/server.js`.

Build the image:

```bash
docker build \
  --build-arg VITE_API_BASE_URL=https://mtiker-cweb-4.proxy.itcollege.ee \
  -t mtiker-js-a07-client:local .
```

Run it locally (mapped to host port 3090):

```bash
docker run --rm -p 3090:3000 mtiker-js-a07-client:local
# then:
curl http://127.0.0.1:3090/healthz   # -> "ok"
open  http://127.0.0.1:3090/         # -> SSR-rendered landing page
```

Deploy via Compose (uses `A07_PORT` from `.env`, default 90):

```bash
docker compose up -d --build
```

### Production wiring

- **Public URL:** `https://mtiker-js-a07.proxy.itcollege.ee` — the TalTech
  proxy is configured to forward to host port `90` on the runner host.
- **Backend CORS:** `assignment05_final2/docker-compose.prod.yml` exposes a
  third allowlist slot (`Cors__AllowedOrigins__2`) defaulting to
  `https://mtiker-js-a07.proxy.itcollege.ee`. Redeploy the backend after
  pulling the updated compose file so the new slot is honoured.
- **Env file on the runner:** `/home/gitlab-runner/mtiker-js-a07.env`
  (`A07_PORT`, `VITE_API_BASE_URL`, optional `A07_CLIENT_IMAGE`). See
  `.env.example`.
- **CI:** `courses/javascript/assignment-07-client/.gitlab-ci.yml` runs
  lint + build in `test`, `docker compose build` in `package`, and
  `scripts/deploy.sh` in `deploy` (default-branch / tag only).
- **Smoke test:** reuse the cweb script with the a07 origin —
  ```bash
  BACKEND_URL=https://mtiker-cweb-4.proxy.itcollege.ee \
  CLIENT_URL=https://mtiker-js-a07.proxy.itcollege.ee \
  SMOKE_CORS_ORIGIN=https://mtiker-js-a07.proxy.itcollege.ee \
  SMOKE_EMAIL=member@peakforge.local \
  SMOKE_PASSWORD=GymStrong123! \
  SMOKE_GYM_CODE=peakforge \
  bash ../../webapp-csharp/assignment05_final2/scripts/smoke-deploy.sh
  ```
  This verifies backend `/health`, Swagger, client `/healthz`, CORS preflight
  from the a07 origin, login + refresh-token rotation, and one authenticated
  tenant read.
