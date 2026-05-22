#!/usr/bin/env bash
# Primeira subida do piloto: SQL + schema EF staging + stack + registro opcional.
set -euo pipefail

export EDUFLOW_ENV_FILE="${EDUFLOW_ENV_FILE:-deploy/.env.pilot}"

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

if [ ! -f "$EDUFLOW_ENV_FILE" ]; then
  echo "Copie deploy/.env.pilot.example para $EDUFLOW_ENV_FILE e edite."
  exit 1
fi

# shellcheck disable=SC1091
set -a
source "$EDUFLOW_ENV_FILE"
set +a

echo "=== EduFlow — init piloto ==="

echo "1) Subindo SQL + RabbitMQ..."
COMPOSE="docker compose -f docker-compose.pilot.yml --env-file $EDUFLOW_ENV_FILE"

$COMPOSE --profile local-sql up -d sqlserver rabbitmq

EDUFLOW_ENV_FILE="$EDUFLOW_ENV_FILE" bash scripts/deploy/apply-sql.sh

echo "2) Criando schema staging (EF EnsureCreated)..."
$COMPOSE build api
$COMPOSE run --rm \
  -e EDUFLOW_INIT_SCHEMA=true \
  -e ASPNETCORE_ENVIRONMENT=Development \
  api

echo "3) Subindo API, workers e Caddy..."
$COMPOSE up -d api workers caddy

API_BASE="${PILOT_API_URL:-}"
if [ -z "$API_BASE" ]; then
  if [ -n "${API_DOMAIN:-}" ]; then
    API_BASE="https://${API_DOMAIN}"
  else
    API_BASE="http://127.0.0.1"
  fi
fi

echo "4) Aguardando API..."
for i in $(seq 1 40); do
  if curl -fsS "${API_BASE}/health/live" >/dev/null 2>&1; then
    break
  fi
  sleep 3
  if [ "$i" -eq 40 ]; then
    echo "API não respondeu em ${API_BASE}/health/live"
    echo "Se testar na VM local, exporte: PILOT_API_URL=http://127.0.0.1"
    exit 1
  fi
done

if [ "${Pilot__AllowRegistration:-false}" = "true" ]; then
  echo "5) Criando tenant de piloto (register)..."
  curl -fsS -X POST "${API_BASE}/api/auth/register" \
    -H "Content-Type: application/json" \
    -d '{
      "name": "Escola Piloto",
      "slug": "piloto",
      "adminEmail": "admin@piloto.eduflow",
      "adminPassword": "Piloto@123"
    }' && echo ""
  echo ">>> Login criado: admin@piloto.eduflow (troque a senha após o primeiro acesso)"
  echo ">>> Depois defina Pilot__AllowRegistration=false em deploy/.env.pilot e reinicie a API."
else
  echo "5) Registro desabilitado (Pilot__AllowRegistration não é true). Use login existente."
fi

echo ""
echo "=== Piloto pronto ==="
echo "API health: ${API_BASE}/health/live"
echo "Configure na Vercel: NEXT_PUBLIC_API_URL=${API_BASE}"
echo "Cors__Origins__0 deve ser a URL do frontend (ex.: https://xxx.vercel.app)"
