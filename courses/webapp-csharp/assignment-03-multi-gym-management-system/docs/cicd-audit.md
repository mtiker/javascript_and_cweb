# CI/CD Audit

**Audited:** 2026-04-28

This audit captures the GitLab pipeline shape before and after Phase 8 (separate
client deployment).

---

## 1. Repository-level pipeline (`/.gitlab-ci.yml`)

Single stage `orchestrate`. The assignment-03 trigger fires on changes under
`courses/webapp-csharp/assignment-03-multi-gym-management-system/**/*` and
includes the assignment's local pipeline as a child pipeline (`strategy:
depend`). No assignment-level changes are required at the repo root.

---

## 2. Pre-Phase-8 assignment pipeline (`assignment-03/.gitlab-ci.yml`)

| Stage | Job | Command (essence) | Notes |
|-------|-----|-------------------|-------|
| `client` | `assignment03_client` | Docker-in-Docker `node:20-alpine` runs `npm ci && npm test && npm run build` from a copy of `client/` | Output is discarded — only verifies the client builds |
| `build` | `assignment03_build` | `dotnet restore && dotnet build -c Release` | Needs `assignment03_client` |
| `test` | `assignment03_test` | `dotnet test -c Release --no-build` | |
| `package` | `assignment03_docker_build` | `docker build` of the multi-stage Dockerfile (which itself builds the client a second time) | Image tagged `<slug>-assignment-03:<sha>` |
| `deploy` | `assignment03_deploy` | `./scripts/deploy.sh` → `docker compose -f docker-compose.prod.yml up -d --build` | Default branch / tags only |

Issues this audit identified:

1. The client is built **twice**: once in the `client` stage and again in the
   Docker `client-build` stage. Acceptable but wasteful.
2. There is no separately published client artifact. A separate-host deployment
   has nothing to pull.
3. The `assignment03_docker_build` job tags only the backend image. There is no
   client image.
4. The deploy job only runs `docker-compose.prod.yml` for backend+postgres.

---

## 3. Phase-8 pipeline shape

Stages stay the same (`client → build → test → package → deploy`). What
changes is the **package** and **deploy** stages — both grow a client-image
counterpart.

| Stage | Job | What is new |
|-------|-----|-------------|
| `client` | `assignment03_client` | Unchanged — install + test + build verify only |
| `package` | `assignment03_client_image` | **New.** `docker build -f client/Dockerfile -t <slug>-assignment-03-client:<sha>` |
| `package` | `assignment03_docker_build` | Unchanged (still produces the backend image, which keeps an embedded `/client` fallback) |
| `deploy` | `assignment03_deploy` | Unchanged: still composes backend+postgres |
| `deploy` | `assignment03_deploy_client` | **New.** Optional: deploy the client container to the second host/port (manual gate by default) |

Both new client jobs reuse the existing `<<: *assignment03_job` /
`*assignment03_deploy_job` rule anchors so that branch / tag / changes filters
stay consistent across the pipeline.

---

## 4. CI variables required for Phase 8

| Variable | Required? | Where consumed | Notes |
|---|---|---|---|
| `JWT__Key` | Yes (already) | backend deploy | Long secret; never logged |
| `CORS_ALLOWED_ORIGIN` | Yes (already) | backend deploy | Now a comma-separated list of origins, e.g. `https://api-host,https://client-host` |
| `VITE_API_BASE_URL` | **New** | client image build (build-arg) | Absolute URL of the backend, e.g. `https://mtiker-cweb-4.proxy.itcollege.ee` |
| `CLIENT_PORT` | **New (optional)** | client deploy | Host port for the client container; defaults to `8081` to avoid clashing with backend `83` |
| `MULTI_GYM_CLIENT_IMAGE` | **New (optional)** | docker-compose | Image tag for the client service if pulling from a registry instead of building locally |

`VITE_*` values are baked into the JS bundle at build time, so they must be
present during the client `docker build` step — not at run time.

---

## 5. Backwards compatibility

The existing `assignment03_client → build → test → package(backend) → deploy(backend)`
chain is untouched. The new client-image and client-deploy jobs are additive
and gated by the same `changes:` rules. A pipeline run with zero changes under
`client/` still tags the backend image and deploys it unchanged.

If the school server only exposes one origin, the new client-deploy job can be
left disabled (`when: manual`) and the assignment continues to ship via the
embedded `/client` route on the backend image.
