#!/usr/bin/env bash
# Backup dos bancos EduFlow_Staging e EduFlow_DW (produção beta).
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

ENV_FILE="${EDUFLOW_ENV_FILE:-deploy/.env.production}"
if [ ! -f "$ENV_FILE" ]; then
  ENV_FILE="deploy/.env.pilot"
fi

# shellcheck disable=SC1091
set -a
source "$ENV_FILE"
set +a

CONTAINER="${EDUFLOW_SQL_CONTAINER:-eduflow-pilot-sql}"
SA_PASSWORD="${MSSQL_SA_PASSWORD:?MSSQL_SA_PASSWORD não definido}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUT_DIR="${EDUFLOW_BACKUP_DIR:-$ROOT/backups}"
mkdir -p "$OUT_DIR"

backup_db() {
  local db="$1"
  local file="$OUT_DIR/${db}_${STAMP}.bak"
  echo "Backup $db -> $file"
  docker exec "$CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$SA_PASSWORD" -C -Q \
    "BACKUP DATABASE [${db}] TO DISK = N'/var/opt/mssql/data/${db}_${STAMP}.bak' WITH FORMAT, INIT;"
  docker cp "${CONTAINER}:/var/opt/mssql/data/${db}_${STAMP}.bak" "$file"
  docker exec "$CONTAINER" rm -f "/var/opt/mssql/data/${db}_${STAMP}.bak"
}

backup_db "EduFlow_Staging"
backup_db "EduFlow_DW"

echo "Backups concluídos em $OUT_DIR"
