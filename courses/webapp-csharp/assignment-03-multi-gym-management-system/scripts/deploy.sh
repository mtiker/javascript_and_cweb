#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

: "${JWT__Key:?JWT__Key must be set before running deployment.}"
: "${POSTGRES_PASSWORD:?POSTGRES_PASSWORD must be set before running deployment.}"
PROJECT_NAME="${COMPOSE_PROJECT_NAME:-multi-gym-management-system}"

docker compose --project-name "$PROJECT_NAME" -f docker-compose.prod.yml down --remove-orphans || true
docker compose --project-name "$PROJECT_NAME" -f docker-compose.prod.yml up -d --build --remove-orphans

echo "===== DEPLOY DIAGNOSTICS ====="
echo "--- WEBAPP_PORT env value seen by deploy.sh: ${WEBAPP_PORT:-<unset, compose default>}"
echo "--- docker compose ps ---"
docker compose --project-name "$PROJECT_NAME" -f docker-compose.prod.yml ps
echo "--- host port bindings for project ---"
docker ps --filter "name=${PROJECT_NAME}-web" --format 'table {{.Names}}\t{{.Status}}\t{{.Ports}}'
echo "--- web container last 60 lines of logs ---"
sleep 8
docker compose --project-name "$PROJECT_NAME" -f docker-compose.prod.yml logs --tail=60 web || true
echo "===== END DIAGNOSTICS ====="
