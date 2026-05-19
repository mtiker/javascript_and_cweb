# Assignment 06 — React (Next.js) Client

Next.js + TypeScript frontend reused from [`assignment-05-react-todo`](../../assignment-05-react-todo/), rebuilt against the **A06 Express.js backend** instead of `taltech.akaver.com`.

Only the API base URL changes — see [src/services/apiClient.ts](src/services/apiClient.ts) and [src/services/AccountService.ts](src/services/AccountService.ts).

## Build / run

```powershell
npm install
npm run dev
# Or:
$env:NEXT_PUBLIC_API_BASE_URL = "http://localhost:86"
npm run build
```

## Docker

This client is built and orchestrated from the top-level [docker-compose.yml](../docker-compose.yml).
Host port `89` → container Next.js port `3000`.

See [../README.md](../README.md) for full deploy instructions and public URLs.
