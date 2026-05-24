#!/usr/bin/env bash
# Subida produção beta 1.0.0 (dados reais) — Vercel + Oracle.
set -euo pipefail

export EDUFLOW_ENV_FILE="${EDUFLOW_ENV_FILE:-deploy/.env.production}"

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

if [ ! -f "$EDUFLOW_ENV_FILE" ]; then
  echo "Crie $EDUFLOW_ENV_FILE a partir de deploy/.env.production.example"
  exit 1
fi

# shellcheck disable=SC1091
set -a
source "$EDUFLOW_ENV_FILE"
set +a

if [ "${Pilot__AllowRegistration:-false}" = "true" ]; then
  echo "AVISO: Pilot__AllowRegistration=true expõe registro público. Use create-tenant.sh e defina false."
fi

if [ "${ErpConnectors__AllowDemoFallback:-true}" != "false" ]; then
  echo "AVISO: Defina ErpConnectors__AllowDemoFallback=false para dados reais do Sponte."
fi

export EDUFLOW_ENV_FILE
COMPOSE="docker compose -f docker-compose.pilot.yml --env-file $EDUFLOW_ENV_FILE"

echo "=== EduFlow 1.0.0-beta — produção ==="

SQL_TARGET="${EDUFLOW_SQL_TARGET:-azure}"
if [ "$SQL_TARGET" = "azure" ]; then
  echo "Banco: Azure SQL (sem container sqlserver na VM)"
  docker compose -f docker-compose.pilot.yml --env-file "$EDUFLOW_ENV_FILE" stop sqlserver 2>/dev/null || true
  $COMPOSE up -d rabbitmq
  EDUFLOW_ENV_FILE="$EDUFLOW_ENV_FILE" bash scripts/deploy/apply-sql-azure.sh
elif [ "$SQL_TARGET" = "local-sql" ]; then
  if [ -z "${MSSQL_SA_PASSWORD:-}" ] || [ "${MSSQL_SA_PASSWORD}" = "local-sql-not-used" ]; then
    echo "Defina MSSQL_SA_PASSWORD em $EDUFLOW_ENV_FILE para EDUFLOW_SQL_TARGET=local-sql"
    exit 1
  fi
  echo "Banco: SQL Server em container (exige VM >= 2 GB RAM)"
  $COMPOSE --profile local-sql up -d sqlserver rabbitmq
  EDUFLOW_ENV_FILE="$EDUFLOW_ENV_FILE" bash scripts/deploy/apply-sql.sh
else
  echo "EDUFLOW_SQL_TARGET inválido: $SQL_TARGET (use azure ou local-sql)"
  exit 1
fi

echo "Schema staging (EF)..."
$COMPOSE build api
$COMPOSE run --rm -e EDUFLOW_INIT_SCHEMA=true -e ASPNETCORE_ENVIRONMENT=Development api

if [ "$SQL_TARGET" = "azure" ]; then
  EDUFLOW_ENV_FILE="$EDUFLOW_ENV_FILE" bash scripts/deploy/apply-sql-staging-post-ef.sh
fi

$COMPOSE up -d api workers caddy

API_BASE="${PILOT_API_URL:-}"
if [ -z "$API_BASE" ] && [ -n "${API_DOMAIN:-}" ]; then
  API_BASE="https://${API_DOMAIN}"
fi

if [ -n "$API_BASE" ]; then
  echo "Aguardando $API_BASE/health/live ..."
  for i in $(seq 1 40); do
    if curl -fsS "${API_BASE}/health/live" >/dev/null 2>&1; then
      curl -fsS "${API_BASE}/api/version" && echo ""
      break
    fi
    sleep 3
  done
fi

echo ""
echo "Próximo passo: ./scripts/deploy/create-tenant.sh (se ainda não há escola)"
echo "Documentação: deploy/AZURE-SQL-ORACLE.md (Azure SQL + Oracle)"
