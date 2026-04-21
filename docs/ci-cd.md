# Monorepo CI/CD Layout

## Purpose

This repository uses a monorepo-friendly GitLab CI/CD structure:

- the root `.gitlab-ci.yml` is the only GitLab entrypoint
- the root pipeline triggers one child pipeline per assignment instead of merging all assignment jobs into one global stage flow
- assignment-specific CI lives next to the assignment it builds or deploys
- Docker files stay with the assignment they belong to
- GitLab Runner host configuration stays outside the repository

This keeps unrelated assignments from building when only one assignment changes and prevents one assignment's failed stage chain from blocking another assignment's pipeline from running.

## Current Layout

```text
/
  .gitlab-ci.yml
  docs/
    ci-cd.md
  courses/
    javascript/
      assignment-03-ci-cd-1/
        .gitlab-ci.yml
        docker-compose.prod.yml
        dockerfiles/
        scripts/deploy.sh
      assignment-04-vue-secure-todo/
        .gitlab-ci.yml
        Dockerfile
        docker-compose.prod.yml
        nginx/default.conf
        scripts/deploy.sh
    webapp-csharp/
      assignment-18-dental-clinic-platform/
        .gitlab-ci.yml
        Dockerfile
        docker-compose.yml
        docker-compose.prod.yml
        scripts/deploy.sh
```

## GitLab CI Responsibility Split

### Root pipeline

The root `.gitlab-ci.yml` should contain only monorepo orchestration concerns:

- shared orchestration stages
- shared variables/defaults
- top-level workflow rules
- child-pipeline trigger jobs with `trigger: include`

It should not contain assignment-specific build or deploy commands.

### Assignment pipeline

Each assignment-level `.gitlab-ci.yml` should describe only that assignment's child pipeline jobs, for example:

- restore/build
- tests
- Docker image build validation
- deployment
- the full `stages:` list needed by those jobs

Child pipelines do not inherit `stages:` from the root `.gitlab-ci.yml`. If an assignment job uses a non-default stage such as `package`, that stage must be declared inside the assignment-local CI file or GitLab rejects the child pipeline as invalid YAML.

Use `rules: changes` so those jobs run only when files under that assignment change.

The intended separation is:

- root pipeline decides which assignment child pipelines should start;
- each assignment child pipeline owns its own `build -> test -> package -> deploy` chain;
- if Assignment A fails, Assignment B child pipeline can still start and run because they are no longer sharing one global root pipeline stage progression.

## Runner Model

Runner setup files such as `config.toml`, registration tokens, service definitions, and SSH private keys must stay on the runner host or VPS. Do not commit them to the repository.

Current repository setup uses a single runner tag:

- `shared`: restore, build, test, Docker build, and deployment jobs

If you later split responsibilities across multiple runners, you can move back to specialized tags such as separate build, Docker, and deploy runners.

## JavaScript Assignment 03 Deployment Model

`courses/javascript/assignment-03-ci-cd-1` is the deployable delivery layer for the first two JavaScript assignments:

- `docker-compose.prod.yml` starts two nginx containers on the VPS
- Assignment 01 is exposed on host port `81`
- Assignment 02 is exposed on host port `82`
- each image is built from its source assignment folder with an assignment-specific Dockerfile
- `scripts/deploy.sh` is the deployment entrypoint used by the GitLab deploy job

The assignment pipeline runs:

- Assignment 01 regression tests in a `node:20-alpine` container
- Assignment 02 `npm ci`, strict TypeScript validation, and regression tests in a `node:20-alpine` container
- Docker Compose image build validation
- deployment on the default branch or tags only

To keep the shell runner stable:

- do not set `GIT_CLONE_PATH` unless the runner host explicitly enables GitLab Runner `custom_build_dir`, because shell jobs fail during checkout otherwise;
- Assignment 01 tests mount the app read-only into the Node container;
- Assignment 02 tests start in `/tmp`, create their own `/tmp/app` work directory and `/tmp/npm-cache` as the mapped non-root user, set `npm_config_cache=/tmp/npm-cache`, and then copy the app there before running `npm ci`, so the runner checkout is not polluted with root-owned `node_modules` and npm does not try to write into unwritable `/.npm` on the shell runner.

## JavaScript Assignment 03 Variables

For JavaScript Assignment 03 deployment, these values can be configured as GitLab CI/CD variables or on the VPS runner host:

- `JAVASCRIPT_A01_PORT` to override the default host port `81`
- `JAVASCRIPT_A02_PORT` to override the default host port `82`
- `JAVASCRIPT_A01_IMAGE` to override the Assignment 01 image name
- `JAVASCRIPT_A02_IMAGE` to override the Assignment 02 image name
- `COMPOSE_PROJECT_NAME` to override the default Docker Compose project name `javascript-assignment-03`

The JavaScript Assignment 03 proxy mappings are:

- `https://mtiker-js-js.proxy.itcollege.ee` => `http://192.168.181.122:81`
- `https://mtiker-js-ts.proxy.itcollege.ee` => `http://192.168.181.122:82`

## JavaScript Assignment 04 Deployment Model

`courses/javascript/assignment-04-vue-secure-todo` is a self-contained Vue 3 deployable frontend:

- `Dockerfile` builds the Vite app and serves it with nginx
- `docker-compose.prod.yml` exposes the app on host port `84` by default
- `nginx/default.conf` keeps history-mode routing refresh-safe with `try_files`
- `scripts/deploy.sh` starts the container and smoke-checks both `/healthz` and `/`

The assignment pipeline runs:

- `npm ci`
- strict Vue/TypeScript validation
- Vitest unit and component/integration tests
- Docker Compose image build validation
- deployment on the default branch or tags only

To keep the shell runner stable:

- the test job uses the same `/tmp/app` + `/tmp/npm-cache` copy strategy as JavaScript Assignment 03 instead of writing into the runner checkout
- the Docker build runs from the assignment folder so Docker context and nginx config stay assignment-local

## JavaScript Assignment 04 Variables

For JavaScript Assignment 04 deployment, these values can be configured as GitLab CI/CD variables or on the VPS runner host:

- `JAVASCRIPT_A04_PORT` to override the default host port `84`
- `JAVASCRIPT_A04_IMAGE` to override the image name
- `COMPOSE_PROJECT_NAME` to override the default Docker Compose project name `javascript-assignment-04`
- `VITE_API_BASE_URL` to override the default backend base URL `https://taltech.akaver.com/api/v1` at Docker image build time

The JavaScript Assignment 04 proxy mapping is:

- `https://mtiker-js-vue.proxy.itcollege.ee` => `http://192.168.181.122:84`

## Assignment 03 Multi-Gym Deployment Model

`courses/webapp-csharp/assignment-03-multi-gym-management-system` is treated as a self-contained deployable app:

- `Dockerfile` builds the ASP.NET Core application image and the React client
- `docker-compose.yml` is for local development database startup
- `docker-compose.prod.yml` is for VPS deployment with the app and PostgreSQL
- `scripts/deploy.sh` is the deployment entrypoint used by the GitLab deploy job
- the production React client is served by the backend at `/client`

The deploy job is intentionally limited to the default branch or tags.

The Assignment 03 proxy mapping is:

- `https://mtiker-cweb-4.proxy.itcollege.ee` => `http://192.168.181.122:83`

## Assignment 03 Multi-Gym Variables

For Assignment 03 production deployment, configure these values in GitLab CI/CD variables or on the VPS runner host:

- `JWT__Key`
- `CORS_ALLOWED_ORIGIN` if you want to override the default `https://mtiker-cweb-4.proxy.itcollege.ee`
- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `WEBAPP_PORT` if you need to override the default host port `83`
- `JWT__Issuer`
- `JWT__Audience`
- `JWT__AccessTokenMinutes`
- `DATA_INIT_MIGRATE_DATABASE`
- `DATA_INIT_SEED_DATA`
- `MULTI_GYM_IMAGE` if you want a custom local image name
- `COMPOSE_PROJECT_NAME` if you want a non-default Docker Compose project name

At minimum, `JWT__Key` must be set. `CORS_ALLOWED_ORIGIN` defaults to `https://mtiker-cweb-4.proxy.itcollege.ee` for Assignment 03.

## Assignment 18 Deployment Model

`courses/webapp-csharp/assignment-18-dental-clinic-platform` is treated as a self-contained deployable app:

- `Dockerfile` builds the ASP.NET Core application image
- `docker-compose.yml` is for local development database startup
- `docker-compose.prod.yml` is for VPS deployment with the app and PostgreSQL
- `scripts/deploy.sh` is the deployment entrypoint used by the GitLab deploy job

The deploy job is intentionally limited to the default branch or tags.

## Required CI/CD Variables

For Assignment 18 production deployment, configure these values in GitLab CI/CD variables or on the VPS runner host:

- `JWT__Key`
- `CORS_ALLOWED_ORIGIN` if you want to override the default `https://mtiker-cweb-a3.proxy.itcollege.ee`
- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `WEBAPP_PORT` if you need to override the default host port `80`
- `JWT__Issuer`
- `JWT__Audience`
- `JWT__ExpiresInSeconds`
- `JWT__RefreshTokenExpiresInSeconds`
- `DATA_INIT_MIGRATE_DATABASE`
- `DATA_INIT_SEED_IDENTITY`
- `DATA_INIT_SEED_DATA`
- `DENTAL_CLINIC_IMAGE` if you want a custom local image name
- `COMPOSE_PROJECT_NAME` if you want a non-default Docker Compose project name

At minimum, `JWT__Key` must be set. `CORS_ALLOWED_ORIGIN` now defaults to `https://mtiker-cweb-a3.proxy.itcollege.ee` for Assignment 18.

## Validation Expectations

The intended behavior is:

- a change only under `assignment-01`, `assignment-02`, or JavaScript `assignment-03` triggers only the JavaScript Assignment 03 pipeline
- a change only under JavaScript `assignment-04` triggers only the JavaScript Assignment 04 pipeline
- JavaScript Assignment 01 is served successfully on host port `81`
- JavaScript Assignment 02 is served successfully on host port `82`
- JavaScript Assignment 04 is served successfully on host port `84`
- Web App C# Assignment 03 is served successfully on host port `83`
- the JavaScript deploy script smoke-checks both local host ports after container startup
- the JavaScript Assignment 04 deploy script smoke-checks `/healthz` and `/` after container startup
- a change only under `assignment-18` triggers only the Assignment 18 pipeline
- a change only in root documentation does not trigger Assignment 18 build/test/deploy jobs
- a Docker or deployment-script change triggers the Docker build job
- deployment runs only on the default branch or tags
- unrelated JavaScript assignment changes do not trigger the Assignment 18 pipeline
- the deployed app answers `GET /health` after container startup
