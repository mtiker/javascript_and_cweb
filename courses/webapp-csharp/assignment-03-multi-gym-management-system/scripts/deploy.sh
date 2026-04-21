#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

: "${JWT__Key:?JWT__Key must be set before running deployment.}"
PROJECT_NAME="${COMPOSE_PROJECT_NAME:-multi-gym-management-system}"

docker compose --project-name "$PROJECT_NAME" -f docker-compose.prod.yml up -d --build --remove-orphans
