# TaskFlow Deployment

## Public URL

| Environment | URL | Notes |
|-------------|-----|-------|
| Production (VPS) | `https://mtiker-js-react.proxy.itcollege.ee` | Proxied to `http://192.168.181.122:88` |
| Local dev | `http://localhost:3000` | `npm run dev` |
| Local container | `http://localhost:3000` | `docker compose up --build` |

## Backend

The client points at `https://taltech.akaver.com` (the TalTech ToDo
backend, rebuilt 2026-04-05). Swagger:
`https://taltech.akaver.com/swagger/index.html`. No data needs to be
seeded by us — register a fresh user inside the app.

## Build artifact

`next.config.ts` enables `output: 'standalone'`. The build produces:

- `.next/standalone/` — node server with the trace-pruned dependency tree
- `.next/static/` — static client assets

Runtime image only carries those two folders plus `public/`.

## Dockerfile

Three stages: `deps` (only `npm ci`), `build` (full app build with the
public env var inlined), and `runner` (alpine + non-root user running
`node server.js` on port 3000). See `Dockerfile`.

Key build arg: `NEXT_PUBLIC_API_BASE_URL`. Next.js inlines public env
vars at build time, so the value must be passed via build arg, not
runtime env.

## docker-compose

| File | Purpose |
|------|---------|
| `docker-compose.yml` | Local: maps `3000:3000`. |
| `docker-compose.prod.yml` | VPS: maps `88:3000`, container name `mtiker-js-react`. |

Both files take `NEXT_PUBLIC_API_BASE_URL` from the environment and fall
back to `https://taltech.akaver.com`.

## CI/CD

`.gitlab-ci.yml` (child pipeline) has two stages:

```text
build : docker:27 + docker:27-dind
        docker build --build-arg NEXT_PUBLIC_API_BASE_URL=…
        docker push $CI_REGISTRY_IMAGE/a05-react-todo:$CI_COMMIT_SHORT_SHA

deploy: cp /home/gitlab-runner/mtiker-js-react.env .env
        docker compose -p mtiker-js-react -f docker-compose.prod.yml down --remove-orphans
        docker compose -p mtiker-js-react -f docker-compose.prod.yml up --build --remove-orphans --detach
```

The shared runner host already hosts the A01, A02, A03, and A04
deployments — port `88` was previously unallocated. Update the root
`.gitlab-ci.yml` to fan out to this child pipeline alongside the existing
ones.

## Health check

`GET /api/health` returns:

```json
{ "status": "ok", "service": "a05-react-todo", "timestamp": "…" }
```

The route is declared `force-dynamic` so the timestamp is live, not
build-time. The reverse proxy can probe this directly; the runner also
uses `docker compose ps` as a backup signal.

## Rollback

```powershell
docker compose -p mtiker-js-react -f docker-compose.prod.yml down
docker run --rm -d --name mtiker-js-react -p 88:3000 $CI_REGISTRY_IMAGE/a05-react-todo:<previous-sha>
```

## Troubleshooting

- **Login fails with "Network Error" on the deployed app** — confirm
  the build was created with `NEXT_PUBLIC_API_BASE_URL` set; the value
  is baked into the client bundle and cannot be changed at runtime.
- **Refresh loop on 401** — usually a stale `localStorage` token. Open
  DevTools → Application → Local Storage and clear keys starting with
  `auth_`.
- **`docker compose build` fails on Windows** — make sure Docker
  Desktop is using WSL2 and the project path is under a Linux-mountable
  drive (the build copies a lot of small files).
