#!/usr/bin/env bash
set -euo pipefail

# Deploys the JS Assignment 07 client container.
# Required environment variables (typically supplied by GitLab CI/CD or via
# /home/gitlab-runner/mtiker-js-a07.env on the target host):
#   (none mandatory — sensible defaults below)
#
# Optional:
#   VITE_API_BASE_URL      Absolute backend URL baked into the JS bundle.
#                          Default: https://mtiker-cweb-a4.proxy.itcollege.ee
#   A07_PORT               Host port the TalTech proxy forwards to. Default: 90.
#   A07_CLIENT_IMAGE       Pre-built image tag to pull instead of building.
#   COMPOSE_PROJECT_NAME   Compose project name. Default: mtiker-js-a07.

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

PROJECT_NAME="${COMPOSE_PROJECT_NAME:-mtiker-js-a07}"
ENV_FILE="${A07_ENV_FILE:-/home/gitlab-runner/mtiker-js-a07.env}"

if [ -f "$ENV_FILE" ]; then
  cp "$ENV_FILE" .env
else
  echo "WARN: env file $ENV_FILE not found; using whatever .env is already next to docker-compose.yml" >&2
fi

A07_PORT_DEFAULT=90
A07_PORT="${A07_PORT:-$A07_PORT_DEFAULT}"

docker compose --project-name "$PROJECT_NAME" -f docker-compose.yml up -d --build --remove-orphans

if command -v curl >/dev/null 2>&1; then
  echo "Waiting for a07 client to become healthy on host port ${A07_PORT}..."
  for _ in $(seq 1 30); do
    if curl --fail --silent "http://127.0.0.1:${A07_PORT}/healthz" >/dev/null; then
      echo "a07 client is healthy."
      exit 0
    fi
    sleep 2
  done
  echo "ERROR: a07 client did not become healthy on http://127.0.0.1:${A07_PORT}/healthz" >&2
  docker compose --project-name "$PROJECT_NAME" -f docker-compose.yml ps >&2 || true
  docker compose --project-name "$PROJECT_NAME" -f docker-compose.yml logs --tail=80 a07-client >&2 || true
  exit 1
fi
