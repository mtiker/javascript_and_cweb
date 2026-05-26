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

Build static assets:

```sh
npm run build
```

Serve the production build behind the `mtiker-js-a07.proxy.itcollege.ee`
proxy. Backend CORS in `assignment05_final2` already allows configured
non-localhost origins — add this client's published origin to the
backend's CORS allowlist before going live.
