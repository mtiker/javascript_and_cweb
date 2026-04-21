#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

PROJECT_NAME="${COMPOSE_PROJECT_NAME:-javascript-assignment-04}"
A04_PORT="${JAVASCRIPT_A04_PORT:-84}"

docker compose --project-name "$PROJECT_NAME" -f docker-compose.prod.yml up -d --build --remove-orphans

if command -v curl >/dev/null 2>&1; then
  for url in "http://127.0.0.1:${A04_PORT}/healthz" "http://127.0.0.1:${A04_PORT}"; do
    for _ in $(seq 1 20); do
      if curl --fail --silent "$url" >/dev/null; then
        break
      fi

      sleep 2
    done

    curl --fail --silent "$url" >/dev/null
  done
fi
