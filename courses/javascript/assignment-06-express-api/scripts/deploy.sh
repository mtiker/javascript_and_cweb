#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

PROJECT_NAME="${COMPOSE_PROJECT_NAME:-mtiker-js-a06}"
ENV_FILE="${A06_ENV_FILE:-/home/gitlab-runner/mtiker-js-a06.env}"

if [ -f "$ENV_FILE" ]; then
  cp "$ENV_FILE" .env
else
  echo "WARN: env file $ENV_FILE not found; using whatever .env is already next to docker-compose.yml" >&2
fi

API_PORT="${A06_API_PORT:-86}"
VUE_PORT="${A06_VUE_PORT:-87}"
REACT_PORT="${A06_REACT_PORT:-89}"

# TEMPORARY: wipe stale pgdata volume to recover from postgres password mismatch.
# Revert this line in a follow-up commit once CI redeploys cleanly.
docker compose --project-name "$PROJECT_NAME" -f docker-compose.yml down -v --remove-orphans || true

docker compose --project-name "$PROJECT_NAME" -f docker-compose.yml up -d --build --remove-orphans

dump_diagnostics() {
  echo "=== docker compose ps ===" >&2
  docker compose --project-name "$PROJECT_NAME" -f docker-compose.yml ps >&2 || true
  for svc in postgres express-api vue-client react-client; do
    echo "=== logs: $svc (tail 100) ===" >&2
    docker compose --project-name "$PROJECT_NAME" -f docker-compose.yml logs --tail 100 "$svc" >&2 || true
  done
}

if command -v curl >/dev/null 2>&1; then
  health_check() {
    local url="$1"
    echo "Health-checking $url"
    for _ in $(seq 1 30); do
      if curl --fail --silent --show-error "$url" >/dev/null; then
        echo "  OK: $url"
        return 0
      fi
      sleep 2
    done
    echo "  FAIL: $url did not become healthy; final attempt output:" >&2
    curl --fail --show-error "$url" || true
    dump_diagnostics
    return 1
  }

  health_check "http://127.0.0.1:${API_PORT}/api/v1/health"
  health_check "http://127.0.0.1:${VUE_PORT}/healthz"
  health_check "http://127.0.0.1:${REACT_PORT}/"
fi
