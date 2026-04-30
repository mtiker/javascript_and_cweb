#!/usr/bin/env bash
set -euo pipefail

# Deploys the standalone React client container alongside the backend stack.
# Required environment variables (typically supplied by GitLab CI/CD):
#   VITE_API_BASE_URL          Absolute backend URL baked into the JS bundle.
#                              Example: https://mtiker-cweb-4.proxy.itcollege.ee
#
# Optional:
#   CLIENT_PORT                Host port for the client container. Default: 8081.
#   COMPOSE_PROJECT_NAME       Compose project name. Default: multi-gym-management-system.
#   MULTI_GYM_CLIENT_IMAGE     Pre-built image tag to pull instead of building.

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

: "${VITE_API_BASE_URL:?VITE_API_BASE_URL must be set before deploying the client.}"
PROJECT_NAME="${COMPOSE_PROJECT_NAME:-multi-gym-management-system}"

docker compose --project-name "$PROJECT_NAME" \
  --profile client \
  -f docker-compose.prod.yml \
  up -d --build --remove-orphans client
