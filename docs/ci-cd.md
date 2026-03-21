# Monorepo CI/CD Layout

## Purpose

This repository uses a monorepo-friendly GitLab CI/CD structure:

- the root `.gitlab-ci.yml` is the only GitLab entrypoint
- assignment-specific CI lives next to the assignment it builds or deploys
- Docker files stay with the assignment they belong to
- GitLab Runner host configuration stays outside the repository

This keeps unrelated assignments from building when only one assignment changes.

## Current Layout

```text
/
  .gitlab-ci.yml
  docs/
    ci-cd.md
  courses/
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

- shared stages
- shared variables/defaults
- top-level workflow rules
- `include: local` statements for assignment pipelines

It should not contain assignment-specific build or deploy commands.

### Assignment pipeline

Each assignment-level `.gitlab-ci.yml` should describe only that assignment's jobs, for example:

- restore/build
- tests
- Docker image build validation
- deployment

Use `rules: changes` so those jobs run only when files under that assignment change.

## Runner Model

Runner setup files such as `config.toml`, registration tokens, service definitions, and SSH private keys must stay on the runner host or VPS. Do not commit them to the repository.

Current repository setup uses a single runner tag:

- `shared`: restore, build, test, Docker build, and deployment jobs

If you later split responsibilities across multiple runners, you can move back to specialized tags such as separate build, Docker, and deploy runners.

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

- a change only under `assignment-18` triggers only the Assignment 18 pipeline
- a change only in root documentation does not trigger Assignment 18 build/test/deploy jobs
- a Docker or deployment-script change triggers the Docker build job
- deployment runs only on the default branch or tags
- unrelated JavaScript assignment changes do not trigger the Assignment 18 pipeline
- the deployed app answers `GET /health` after container startup
