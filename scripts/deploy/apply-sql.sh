#!/usr/bin/env bash
# Aplica scripts SQL no container eduflow-pilot-sql.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

ENV_FILE="${EDUFLOW_ENV_FILE:-deploy/.env.production}"
if [ ! -f "$ENV_FILE" ] && [ -f deploy/.env.pilot ]; then
  ENV_FILE="deploy/.env.pilot"
fi
if [ ! -f "$ENV_FILE" ]; then
  echo "Crie deploy/.env.production ou deploy/.env.pilot"
  exit 1
fi

# shellcheck disable=SC1091
set -a
source "$ENV_FILE"
set +a

CONTAINER="${EDUFLOW_SQL_CONTAINER:-eduflow-pilot-sql}"
SA_PASSWORD="${MSSQL_SA_PASSWORD:?MSSQL_SA_PASSWORD não definido}"

run_sql() {
  local file="$1"
  echo "  -> $(basename "$file")"
  docker exec -i "$CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C -b -i /dev/stdin < "$file"
}

echo "=== EduFlow — aplicar SQL (piloto) ==="

for i in $(seq 1 60); do
  if docker exec "$CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" >/dev/null 2>&1; then
    break
  fi
  sleep 2
  if [ "$i" -eq 60 ]; then
    echo "SQL Server não respondeu."
    exit 1
  fi
done

for f in \
  sql/00-pilot-databases.sql \
  sql/01-pilot-dw-schema.sql \
  sql/02-eduflow-dw-views.sql \
  sql/08-canonical-unit-code.sql \
  sql/09-finance-payables-categories.sql
do
  if [ -f "$f" ]; then
    run_sql "$f" || echo "    (aviso: $f falhou ou já aplicado)"
  fi
done

echo "=== SQL concluído ==="
