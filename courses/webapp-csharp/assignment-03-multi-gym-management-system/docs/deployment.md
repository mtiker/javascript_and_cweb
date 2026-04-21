# Deployment Guide

## Local Development Runtime

Local database:
- `docker-compose.yml`

Local startup scripts:
- `scripts/start-db.ps1`
- `scripts/migrate-db.ps1`
- `scripts/start-app.ps1`
- `scripts/stop-db.ps1`

Default local database:
- host: `127.0.0.1`
- port: `5432`
- database: `multi_gym_management_system`

## Production Runtime

Public route:
- `https://mtiker-cweb-4.proxy.itcollege.ee`

Proxy target:
- `http://192.168.181.122:83`

Production deployment files:
- `Dockerfile`
- `docker-compose.prod.yml`
- `scripts/deploy.sh`

Production container layout:
- `postgres` service
- `web` service hosting the ASP.NET Core app, MVC UI, REST API, Swagger, health checks, and the built React client at `/client`

The Dockerfile has a dedicated Node 20 build stage for `client/`. It runs `npm ci` and `npm run build`, then copies `client/dist` into `WebApp/wwwroot/client` in the final ASP.NET Core runtime image.

## Required Environment Variables

Minimum production variables:
- `JWT__Key`
- `POSTGRES_DB`
- `POSTGRES_USER`
- `POSTGRES_PASSWORD`
- `WEBAPP_PORT` when overriding the default host port `83`

Optional variables:
- `CORS_ALLOWED_ORIGIN`
- `JWT__Issuer`
- `JWT__Audience`
- `JWT__AccessTokenMinutes`
- `DATA_INIT_MIGRATE_DATABASE`
- `DATA_INIT_SEED_DATA`
- `COMPOSE_PROJECT_NAME`

## GitLab CI/CD

Repository level:
- root `.gitlab-ci.yml` triggers this assignment as a child pipeline

Assignment level:
- `courses/webapp-csharp/assignment-03-multi-gym-management-system/.gitlab-ci.yml`

Pipeline stages:
1. `build`
2. `test`
3. `package`
4. `deploy`

Deploy script entrypoint:

```bash
./scripts/deploy.sh
```

## Recommended VPS Smoke Check

After deploy:

```bash
curl http://127.0.0.1:83/health
curl http://127.0.0.1:83/client
```

Also verify:
- `https://mtiker-cweb-4.proxy.itcollege.ee` loads
- Swagger loads
- `/client/members` serves the React app shell
- a seeded user can log in
- `multigym.admin@gym.local` can switch gyms
