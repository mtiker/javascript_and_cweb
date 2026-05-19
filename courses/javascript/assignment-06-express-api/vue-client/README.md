# Assignment 06 — Vue Client

Vue 3 + TypeScript frontend reused from [`assignment-04-vue-secure-todo`](../../assignment-04-vue-secure-todo/), rebuilt against the **A06 Express.js backend** instead of `taltech.akaver.com`.

Only the API base URL changes — see [src/config/env.ts](src/config/env.ts).

## Build / run

```powershell
npm install
npm run dev
# Or:
VITE_API_BASE_URL=http://localhost:86/api/v1 npm run build
```

## Docker

This client is built and orchestrated from the top-level [docker-compose.yml](../docker-compose.yml).
Host port `87` → container nginx port `80`.

See [../README.md](../README.md) for full deploy instructions and public URLs.
