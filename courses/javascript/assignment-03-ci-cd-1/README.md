# JavaScript Assignment 03 - CI/CD 1

Public URL for Assignment 01 app: `https://mtiker-js-js.proxy.itcollege.ee`
Public URL for Assignment 02 app: `https://mtiker-js-ts.proxy.itcollege.ee`

Deploy both earlier JavaScript assignments to the VPS as Dockerized nginx containers and automate deployment with GitLab CI/CD.

## Scope

- Assignment 01 app is deployed as its own container on VPS host port `81`
- Assignment 02 app is deployed as its own container on VPS host port `82`
- GitLab CI runs tests before the Docker build/deploy flow
- Deployment uses Docker Compose on the VPS shell runner
- The final public URLs must be added at the top of this README and in the repository root README after the proxy routes are created

## Internal VPS Targets

- Assignment 01 container target: `http://192.168.181.122:81`
- Assignment 02 container target: `http://192.168.181.122:82`

These are the current proxy targets configured in `admin.proxy.itcollege.ee`.

## Files

```text
assignment-03-ci-cd-1/
  .gitlab-ci.yml
  docker-compose.prod.yml
  dockerfiles/
    assignment-01.Dockerfile
    assignment-02.Dockerfile
  scripts/
    deploy.sh
  README.md
```

## Docker Design

### Assignment 01

- Pure browser JavaScript app
- Multi-stage image with a lightweight file-prep stage and final `nginx:alpine`
- Copies `index.html`, `styles.css`, and `src/` into the final static image

### Assignment 02

- TypeScript app
- Multi-stage image with `node:20-alpine` build stage and `nginx:alpine` runtime stage
- Runs `npm ci` and `npm run build` inside Docker
- Final image contains only `index.html`, `styles.css`, and compiled `dist/`

### nginx

- Both images write the same nginx config during the image build
- `try_files $uri $uri/ /index.html;` keeps browser refreshes safe for SPA-style routing if paths are later extended

## GitLab CI/CD

The monorepo root pipeline starts this assignment as its own child pipeline, so failures in another assignment do not block these jobs from running.

Stages used inside this assignment pipeline:

- `test`
- `package`
- `deploy`

Assignment 03 jobs:

1. `javascript_assignment03_a01_test`
2. `javascript_assignment03_a02_test`
3. `javascript_assignment03_docker_build`
4. `javascript_assignment03_deploy`

The deploy job runs only on the default branch or tags.

The test jobs intentionally avoid writing dependencies into the runner checkout:

- Assignment 01 mounts the source folder read-only and runs the built-in Node test runner directly.
- Assignment 02 copies the source into a temporary in-container work directory before `npm ci`, `npm run check`, and `npm test`.
- This prevents root-owned `node_modules` leftovers from breaking later GitLab checkout steps on the shell runner.

## VPS Setup Notes

1. Install and register a GitLab Runner on the VPS with the `shared` tag and shell executor.
2. Make sure `gitlab-runner` belongs to the `docker` group.
3. Confirm Docker Compose works on the VPS.
4. Keep the proxy routes pointed at host port `81` for Assignment 01 and host port `82` for Assignment 02.
5. If the proxy hostnames change later, update the URLs in this README and in the root `README.md`.

## Local Validation

From this folder:

```bash
docker compose -f docker-compose.prod.yml build
docker compose -f docker-compose.prod.yml up -d
docker compose -f docker-compose.prod.yml ps
```

Open:

- `http://localhost:81`
- `http://localhost:82`

Stop the local containers with:

```bash
docker compose -f docker-compose.prod.yml down
```

## Related Assignment Docs

- Assignment 01 app: `../assignment-01-task-manager/README.md`
- Assignment 02 app: `../assignment-02-ts-task-manager/README.md`
- Monorepo CI/CD notes: `../../../docs/ci-cd.md`
- AI usage log: `../../../docs/ai-prompts.md`
