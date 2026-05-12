#!/usr/bin/env bash

set -Eeuo pipefail

required_vars=(
  BACKEND_URL
  CLIENT_URL
  SMOKE_EMAIL
  SMOKE_PASSWORD
  SMOKE_GYM_CODE
)

fail() {
  printf 'ERROR: %s\n' "$*" >&2
  exit 1
}

info() {
  printf '[smoke] %s\n' "$*"
}

require_env() {
  local missing=()

  for name in "${required_vars[@]}"; do
    if [[ -z "${!name:-}" ]]; then
      missing+=("$name")
    fi
  done

  if (( ${#missing[@]} > 0 )); then
    printf 'ERROR: missing required environment variable(s): %s\n' "${missing[*]}" >&2
    printf 'Required: %s\n' "${required_vars[*]}" >&2
    exit 2
  fi
}

select_json_runtime() {
  if command -v python3 >/dev/null 2>&1; then
    JSON_RUNTIME_KIND=python
    JSON_RUNTIME_CMD=python3
    return
  fi

  if command -v python >/dev/null 2>&1; then
    JSON_RUNTIME_KIND=python
    JSON_RUNTIME_CMD=python
    return
  fi

  if command -v node >/dev/null 2>&1; then
    JSON_RUNTIME_KIND=node
    JSON_RUNTIME_CMD=node
    return
  fi

  fail "Python 3, Python, or Node.js is required for safe JSON payload handling."
}

trim_trailing_slash() {
  local value="$1"
  while [[ "$value" == */ ]]; do
    value="${value%/}"
  done
  printf '%s' "$value"
}

write_login_payload() {
  local output_file="$1"

  case "$JSON_RUNTIME_KIND" in
    python)
      "$JSON_RUNTIME_CMD" - "$output_file" <<'PY'
import json
import os
import sys

with open(sys.argv[1], "w", encoding="utf-8") as file:
    json.dump(
        {
            "email": os.environ["SMOKE_EMAIL"],
            "password": os.environ["SMOKE_PASSWORD"],
        },
        file,
        separators=(",", ":"),
    )
PY
      ;;
    node)
      "$JSON_RUNTIME_CMD" -e 'const fs = require("fs"); fs.writeFileSync(process.argv[1], JSON.stringify({ email: process.env.SMOKE_EMAIL, password: process.env.SMOKE_PASSWORD }));' "$output_file"
      ;;
    *)
      fail "Unsupported JSON runtime: $JSON_RUNTIME_KIND"
      ;;
  esac
}

read_json_field() {
  local input_file="$1"
  local field_name="$2"

  case "$JSON_RUNTIME_KIND" in
    python)
      "$JSON_RUNTIME_CMD" - "$input_file" "$field_name" <<'PY'
import json
import sys

with open(sys.argv[1], encoding="utf-8") as file:
    data = json.load(file)

value = data.get(sys.argv[2])
if value is None or value == "":
    sys.exit(1)

print(value)
PY
      ;;
    node)
      "$JSON_RUNTIME_CMD" -e 'const fs = require("fs"); const data = JSON.parse(fs.readFileSync(process.argv[1], "utf8")); const value = data[process.argv[2]]; if (value === undefined || value === null || value === "") process.exit(1); process.stdout.write(String(value));' "$input_file" "$field_name"
      ;;
    *)
      fail "Unsupported JSON runtime: $JSON_RUNTIME_KIND"
      ;;
  esac
}

curl_expect_success() {
  local label="$1"
  local method="$2"
  local url="$3"
  local output_file="$4"
  shift 4

  local status
  status="$(
    curl \
      --silent \
      --show-error \
      --location \
      --max-time 20 \
      --request "$method" \
      --output "$output_file" \
      --write-out '%{http_code}' \
      "$@" \
      "$url"
  )" || fail "$label request failed: $url"

  case "$status" in
    2??)
      info "$label succeeded ($status)"
      ;;
    *)
      printf 'ERROR: %s failed with HTTP %s: %s\n' "$label" "$status" "$url" >&2
      if [[ -s "$output_file" ]]; then
        printf '%s\n' '--- response body ---' >&2
        head -c 2000 "$output_file" >&2 || true
        printf '\n' >&2
      fi
      exit 1
      ;;
  esac
}

require_env

if ! command -v curl >/dev/null 2>&1; then
  fail "curl is required."
fi

select_json_runtime

backend_url="$(trim_trailing_slash "$BACKEND_URL")"
client_url="$(trim_trailing_slash "$CLIENT_URL")"
work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

login_payload="$work_dir/login.json"
login_response="$work_dir/login-response.json"
backend_health_response="$work_dir/backend-health.txt"
client_health_response="$work_dir/client-health.txt"
tenant_response="$work_dir/tenant-response.json"

write_login_payload "$login_payload"

info "Checking backend health: ${backend_url}/health"
curl_expect_success "backend health" GET "${backend_url}/health" "$backend_health_response"

info "Checking standalone client health: ${client_url}/healthz"
curl_expect_success "client health" GET "${client_url}/healthz" "$client_health_response"

info "Logging in as ${SMOKE_EMAIL}"
curl_expect_success \
  "API login" \
  POST \
  "${backend_url}/api/v1/account/login" \
  "$login_response" \
  --header 'Content-Type: application/json' \
  --data-binary "@${login_payload}"

jwt="$(read_json_field "$login_response" "jwt")" || fail "Login response did not contain a non-empty jwt field."

info "Calling authenticated tenant API for gym ${SMOKE_GYM_CODE}"
curl_expect_success \
  "authenticated tenant API" \
  GET \
  "${backend_url}/api/v1/${SMOKE_GYM_CODE}/maintenance-tasks" \
  "$tenant_response" \
  --header "Authorization: Bearer ${jwt}" \
  --header 'Accept: application/json'

info "Deployment smoke verification completed successfully."
