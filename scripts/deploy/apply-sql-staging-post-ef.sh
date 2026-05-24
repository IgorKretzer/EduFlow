#!/usr/bin/env bash
# Scripts SQL no Staging que dependem das tabelas criadas pelo EF (após EDUFLOW_INIT_SCHEMA).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

ENV_FILE="${EDUFLOW_ENV_FILE:-deploy/.env.production}"
# shellcheck disable=SC1091
set -a
source "$ENV_FILE"
set +a

FQDN="${AZURE_SQL_FQDN:?AZURE_SQL_FQDN não definido}"
USER="${AZURE_SQL_USER:?AZURE_SQL_USER não definido}"
PASSWORD="${AZURE_SQL_PASSWORD:?AZURE_SQL_PASSWORD não definido}"

SQLCMD_IMAGE="${EDUFLOW_SQLCMD_IMAGE:-mcr.microsoft.com/mssql-tools}"
SQLCMD_BIN="${EDUFLOW_SQLCMD_BIN:-/opt/mssql-tools/bin/sqlcmd}"

run_sql() {
  local file="$1"
  echo "  -> $(basename "$file")"
  docker run --rm -i "$SQLCMD_IMAGE" "$SQLCMD_BIN" \
    -S "tcp:${FQDN},1433" -U "$USER" -P "$PASSWORD" -d EduFlow_Staging -b -N -C -l 30 \
    -i /dev/stdin < "$file" || echo "    (aviso: $file falhou ou já aplicado)"
}

echo "=== EduFlow — SQL staging pós-EF ==="
for f in sql/08-canonical-unit-code.sql sql/09-finance-payables-categories.sql; do
  [ -f "$f" ] && run_sql "$f"
done
echo "=== Concluído ==="
