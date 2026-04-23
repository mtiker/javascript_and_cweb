# Study Guide: Docker and Deployment

## Runtime Shape
One ASP.NET Core host serves:
- MVC admin and MVC client areas
- versioned REST API
- Swagger and health endpoint
- built React client under `/client`

## Container Build
`Dockerfile` uses multi-stage build:
1. Node 20 stage builds `client/` assets
2. .NET stage builds and publishes backend
3. Final image copies React build output into `WebApp/wwwroot/client`

## Compose Layout
`docker-compose.prod.yml` runs:
- `postgres`
- `web`

The public proxy target is mapped to VPS port `83`.

## Required Production Inputs
- `JWT__Key` (must be provided securely)
- `CORS_ALLOWED_ORIGIN`
- `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`

## Reverse Proxy Readiness
- forwarded headers are processed before HTTPS/auth middleware
- CORS startup validation blocks unsafe production origins

## Verification Checklist
- container health endpoint responds
- `/client` serves React shell
- login, tenant switch, and at least one tenant workflow action succeed
- API `ProblemDetails` shape remains intact for forbidden/invalid paths
