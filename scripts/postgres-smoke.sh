#!/usr/bin/env bash
#
# postgres-smoke.sh — Live-Postgres smoke verification (roadmap feature 0.3).
#
# Proves the app talks to a real PostgreSQL instance end to end:
#   1. start a throwaway Postgres container
#   2. apply EF Core migrations against it (real schema, not InMemory)
#   3. boot the API pointed at that database
#   4. round-trip a Livro through the HTTP API (POST -> GET)
#   5. confirm the row is physically in Postgres (psql count)
#   6. tear everything down
#
# Requires: docker, the .NET 10 SDK, and the dotnet-ef local tool (.config/dotnet-tools.json).
# Usage: scripts/postgres-smoke.sh
set -euo pipefail

# --- config -----------------------------------------------------------------
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONTAINER="biblioteca-smoke-pg"
PG_PORT="5433"
PG_USER="biblioteca_user"
PG_PASS="biblioteca_smoke_pass"
PG_DB="biblioteca"
API_PORT="5099"
API_BASE="http://localhost:${API_PORT}"
CONN="Host=localhost;Port=${PG_PORT};Database=${PG_DB};Username=${PG_USER};Password=${PG_PASS}"

API_PID=""

log()  { printf '\033[1;34m▶ %s\033[0m\n' "$*"; }
ok()   { printf '\033[1;32m✓ %s\033[0m\n' "$*"; }
fail() { printf '\033[1;31m✗ %s\033[0m\n' "$*" >&2; exit 1; }

cleanup() {
  log "Cleaning up"
  [ -n "${API_PID}" ] && kill "${API_PID}" 2>/dev/null || true
  docker rm -f "${CONTAINER}" >/dev/null 2>&1 || true
}
trap cleanup EXIT

cd "${ROOT_DIR}"

# --- 1. Postgres ------------------------------------------------------------
log "Starting Postgres container (${CONTAINER}) on port ${PG_PORT}"
docker rm -f "${CONTAINER}" >/dev/null 2>&1 || true
docker run -d --name "${CONTAINER}" \
  -e POSTGRES_USER="${PG_USER}" \
  -e POSTGRES_PASSWORD="${PG_PASS}" \
  -e POSTGRES_DB="${PG_DB}" \
  -p "${PG_PORT}:5432" \
  postgres:16-alpine >/dev/null

log "Waiting for Postgres to accept connections"
for i in $(seq 1 30); do
  if docker exec "${CONTAINER}" pg_isready -U "${PG_USER}" -d "${PG_DB}" >/dev/null 2>&1; then
    ok "Postgres is ready"
    break
  fi
  [ "$i" -eq 30 ] && fail "Postgres did not become ready in time"
  sleep 1
done

# --- 2. Build once (reused by migrations and the API) -----------------------
log "Building the API (Release)"
dotnet build BibliotecaAPI/BibliotecaAPI.csproj -c Release --nologo -v q >/dev/null

# --- 3. Migrations ----------------------------------------------------------
log "Applying EF Core migrations"
if ! ConnectionStrings__DefaultConnection="${CONN}" \
      dotnet ef database update --project BibliotecaAPI --no-build --configuration Release \
      >/tmp/biblioteca-smoke-ef.log 2>&1; then
  cat /tmp/biblioteca-smoke-ef.log >&2
  fail "Migration failed"
fi
ok "Migrations applied"

# --- 4. Boot the API --------------------------------------------------------
log "Starting the API on ${API_BASE}"
ASPNETCORE_ENVIRONMENT=Production \
ASPNETCORE_URLS="${API_BASE}" \
ConnectionStrings__DefaultConnection="${CONN}" \
  dotnet BibliotecaAPI/bin/Release/net10.0/BibliotecaAPI.dll >/tmp/biblioteca-smoke-api.log 2>&1 &
API_PID=$!

log "Waiting for the API to respond"
for i in $(seq 1 30); do
  if curl -fsS "${API_BASE}/livros" >/dev/null 2>&1; then
    ok "API is up"
    break
  fi
  if ! kill -0 "${API_PID}" 2>/dev/null; then
    cat /tmp/biblioteca-smoke-api.log >&2
    fail "API process exited during startup"
  fi
  [ "$i" -eq 30 ] && { cat /tmp/biblioteca-smoke-api.log >&2; fail "API did not respond in time"; }
  sleep 1
done

# --- 5. Round-trip through HTTP --------------------------------------------
log "POST /livros"
CREATE_BODY='{"titulo":"O Cortiço","autor":"Aluísio Azevedo","categoria":"Romance","exemplaresDisponiveis":3}'
CREATED="$(curl -fsS -X POST "${API_BASE}/livros" -H 'Content-Type: application/json' -d "${CREATE_BODY}")"
NEW_ID="$(printf '%s' "${CREATED}" | python3 -c 'import sys,json;print(json.load(sys.stdin)["id"])')"
[ -n "${NEW_ID}" ] || fail "POST did not return an id: ${CREATED}"
ok "Created Livro id=${NEW_ID}"

log "GET /livros/${NEW_ID}"
FETCHED="$(curl -fsS "${API_BASE}/livros/${NEW_ID}")"
TITULO="$(printf '%s' "${FETCHED}" | python3 -c 'import sys,json;print(json.load(sys.stdin)["titulo"])')"
[ "${TITULO}" = "O Cortiço" ] || fail "Round-trip mismatch: got '${TITULO}'"
ok "Round-trip title matches: ${TITULO}"

# --- 6. Prove it is physically in Postgres ----------------------------------
log "Verifying the row exists directly in Postgres"
COUNT="$(docker exec "${CONTAINER}" psql -U "${PG_USER}" -d "${PG_DB}" -tAc \
  "SELECT COUNT(*) FROM \"Livros\" WHERE \"Id\" = ${NEW_ID};")"
[ "${COUNT}" = "1" ] || fail "Expected 1 row in Postgres, found '${COUNT}'"
ok "Row confirmed in Postgres (COUNT=${COUNT})"

printf '\n\033[1;32m=== POSTGRES SMOKE: PASS ===\033[0m\n'
