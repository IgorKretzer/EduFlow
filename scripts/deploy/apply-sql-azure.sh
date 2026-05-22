#!/usr/bin/env bash
# Aplica scripts SQL no Azure SQL (sem container sqlserver na VM).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

ENV_FILE="${EDUFLOW_ENV_FILE:-deploy/.env.production}"
if [ ! -f "$ENV_FILE" ]; then
  echo "Crie $ENV_FILE a partir de deploy/.env.production.example"
  exit 1
fi

# shellcheck disable=SC1091
set -a
source "$ENV_FILE"
set +a

FQDN="${AZURE_SQL_FQDN:-}"
USER="${AZURE_SQL_USER:-}"
PASSWORD="${AZURE_SQL_PASSWORD:-}"

if [ -z "$FQDN" ] || [ -z "$USER" ] || [ -z "$PASSWORD" ]; then
  echo "Defina AZURE_SQL_FQDN, AZURE_SQL_USER e AZURE_SQL_PASSWORD em $ENV_FILE"
  echo "(servidor lógico: nome.database.windows.net)"
  exit 1
fi

SQLCMD_IMAGE="${EDUFLOW_SQLCMD_IMAGE:-mcr.microsoft.com/mssql-tools18}"
SQLCMD=(docker run --rm -i "$SQLCMD_IMAGE" /opt/mssql-tools18/bin/sqlcmd
  -S "tcp:${FQDN},1433" -U "$USER" -P "$PASSWORD" -b -N -l 30)

run_sql() {
  local db="$1"
  local file="$2"
  echo "  -> $(basename "$file") (db=${db})"
  "${SQLCMD[@]}" -d "$db" -i /dev/stdin < "$file"
}

echo "=== EduFlow — aplicar SQL (Azure) ==="
echo "Servidor: $FQDN"

echo "Testando conexão (master)..."
if ! "${SQLCMD[@]}" -d master -Q "SELECT 1" >/dev/null 2>&1; then
  echo "Falha ao conectar. Verifique firewall do Azure SQL (IP público da VM Oracle) e credenciais."
  exit 1
fi

if [ -f sql/00-pilot-databases.sql ]; then
  run_sql master sql/00-pilot-databases.sql || echo "    (aviso: 00 — bancos podem já existir no portal)"
fi

for f in sql/01-pilot-dw-schema.sql sql/02-eduflow-dw-views.sql; do
  if [ -f "$f" ]; then
    run_sql EduFlow_DW "$f" || echo "    (aviso: $f falhou ou já aplicado)"
  fi
done

for f in sql/08-canonical-unit-code.sql sql/09-finance-payables-categories.sql; do
  if [ -f "$f" ]; then
    run_sql EduFlow_Staging "$f" || echo "    (aviso: $f falhou ou já aplicado)"
  fi
done

echo "=== SQL Azure concluído ==="
