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

write_refresh_payload() {
  local output_file="$1"
  local jwt="$2"
  local refresh_token="$3"

  case "$JSON_RUNTIME_KIND" in
    python)
      "$JSON_RUNTIME_CMD" - "$output_file" "$jwt" "$refresh_token" <<'PY'
import json
import sys

with open(sys.argv[1], "w", encoding="utf-8") as file:
    json.dump(
        {
            "jwt": sys.argv[2],
            "refreshToken": sys.argv[3],
        },
        file,
        separators=(",", ":"),
    )
PY
      ;;
    node)
      "$JSON_RUNTIME_CMD" -e 'const fs = require("fs"); fs.writeFileSync(process.argv[1], JSON.stringify({ jwt: process.argv[2], refreshToken: process.argv[3] }));' "$output_file" "$jwt" "$refresh_token"
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

headers_allow_origin() {
  local input_file="$1"
  local expected_origin="$2"

  case "$JSON_RUNTIME_KIND" in
    python)
      "$JSON_RUNTIME_CMD" - "$input_file" "$expected_origin" <<'PY'
import sys

with open(sys.argv[1], "rb") as file:
    text = file.read().decode("iso-8859-1", errors="replace")

expected = sys.argv[2].casefold()
for line in text.splitlines():
    if ":" not in line:
        continue

    name, value = line.split(":", 1)
    if name.strip().casefold() == "access-control-allow-origin" and value.strip().casefold() == expected:
        sys.exit(0)

sys.exit(1)
PY
      ;;
    node)
      "$JSON_RUNTIME_CMD" -e 'const fs = require("fs"); const text = fs.readFileSync(process.argv[1], "latin1"); const expected = process.argv[2].toLowerCase(); const ok = text.split(/\r?\n/).some((line) => { const separator = line.indexOf(":"); if (separator < 0) return false; const name = line.slice(0, separator).trim().toLowerCase(); const value = line.slice(separator + 1).trim().toLowerCase(); return name === "access-control-allow-origin" && value === expected; }); process.exit(ok ? 0 : 1);' "$input_file" "$expected_origin"
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
cors_origin="$(trim_trailing_slash "${SMOKE_CORS_ORIGIN:-$client_url}")"
work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

login_payload="$work_dir/login.json"
login_response="$work_dir/login-response.json"
refresh_payload="$work_dir/refresh.json"
refresh_response="$work_dir/refresh-response.json"
backend_health_response="$work_dir/backend-health.txt"
swagger_response="$work_dir/swagger.json"
client_health_response="$work_dir/client-health.txt"
tenant_response="$work_dir/tenant-response.json"
cors_response="$work_dir/cors-response.txt"
cors_headers="$work_dir/cors-headers.txt"

write_login_payload "$login_payload"

info "Checking backend health: ${backend_url}/health"
curl_expect_success "backend health" GET "${backend_url}/health" "$backend_health_response"

info "Checking Swagger/OpenAPI metadata: ${backend_url}/swagger/v1/swagger.json"
curl_expect_success "Swagger metadata" GET "${backend_url}/swagger/v1/swagger.json" "$swagger_response"

info "Checking standalone client health: ${client_url}/healthz"
curl_expect_success "client health" GET "${client_url}/healthz" "$client_health_response"

info "Checking CORS preflight from standalone client origin ${cors_origin}"
cors_status="$(
  curl \
    --silent \
    --show-error \
    --location \
    --max-time 20 \
    --request OPTIONS \
    --dump-header "$cors_headers" \
    --output "$cors_response" \
    --write-out '%{http_code}' \
    --header "Origin: ${cors_origin}" \
    --header "Access-Control-Request-Method: POST" \
    --header "Access-Control-Request-Headers: authorization,content-type,accept-language" \
    "${backend_url}/api/v1/account/login"
)" || fail "CORS preflight request failed."

case "$cors_status" in
  2??)
    ;;
  *)
    fail "CORS preflight failed with HTTP ${cors_status}."
    ;;
esac

if headers_allow_origin "$cors_headers" "$cors_origin"; then
  info "CORS preflight allowed ${cors_origin}"
else
  fail "CORS preflight did not allow ${cors_origin}."
fi

info "Logging in as ${SMOKE_EMAIL}"
curl_expect_success \
  "API login" \
  POST \
  "${backend_url}/api/v1/account/login" \
  "$login_response" \
  --header 'Content-Type: application/json' \
  --data-binary "@${login_payload}"

jwt="$(read_json_field "$login_response" "jwt")" || fail "Login response did not contain a non-empty jwt field."
refresh_token="$(read_json_field "$login_response" "refreshToken")" || fail "Login response did not contain a non-empty refreshToken field."

write_refresh_payload "$refresh_payload" "$jwt" "$refresh_token"

info "Renewing refresh token"
curl_expect_success \
  "refresh token renewal" \
  POST \
  "${backend_url}/api/v1/account/renew-refresh-token" \
  "$refresh_response" \
  --header 'Content-Type: application/json' \
  --data-binary "@${refresh_payload}"

renewed_jwt="$(read_json_field "$refresh_response" "jwt")" || fail "Refresh response did not contain a non-empty jwt field."

info "Calling authenticated tenant API for gym ${SMOKE_GYM_CODE}"
curl_expect_success \
  "authenticated tenant API" \
  GET \
  "${backend_url}/api/v1/${SMOKE_GYM_CODE}/maintenance-tasks" \
  "$tenant_response" \
  --header "Authorization: Bearer ${renewed_jwt}" \
  --header 'Accept: application/json'

info "Deployment smoke verification completed successfully."
