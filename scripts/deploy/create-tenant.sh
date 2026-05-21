#!/usr/bin/env bash
# Cria escola + administrador (requer Pilot__AllowRegistration=true na API até concluir).
# Uso: ./scripts/deploy/create-tenant.sh "Escola X" slug admin@x.com 'SenhaForte1'
set -euo pipefail

if [ $# -lt 4 ]; then
  echo "Uso: $0 \"Nome Escola\" slug admin@email.com 'Senha'"
  exit 1
fi

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"

ENV_FILE="${EDUFLOW_ENV_FILE:-deploy/.env.production}"
# shellcheck disable=SC1091
set -a
source "$ENV_FILE"
set +a

if [ "${Pilot__AllowRegistration:-false}" != "true" ]; then
  echo "Ative Pilot__AllowRegistration=true em $ENV_FILE, reinicie a API, execute este script e volte para false."
  exit 1
fi

API_BASE="${PILOT_API_URL:-}"
if [ -z "$API_BASE" ]; then
  if [ -n "${API_DOMAIN:-}" ]; then
    API_BASE="https://${API_DOMAIN}"
  else
    echo "Defina PILOT_API_URL ou API_DOMAIN"
    exit 1
  fi
fi

export NAME="$1" SLUG="$2" EMAIL="$3" PASS="$4"
PAYLOAD="$(python3 -c 'import json,os; print(json.dumps({"name":os.environ["NAME"],"slug":os.environ["SLUG"],"adminEmail":os.environ["EMAIL"],"adminPassword":os.environ["PASS"]}))')"

curl -fsS -X POST "${API_BASE}/api/auth/register" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD"
echo ""
echo "OK. Defina Pilot__AllowRegistration=false e reinicie a API."
