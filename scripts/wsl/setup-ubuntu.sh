#!/usr/bin/env bash
# Configura Ubuntu (WSL) para EduFlow — Docker Engine SEM Docker Desktop
set -euo pipefail

echo "=== EduFlow — setup Ubuntu/WSL ==="

if grep -qi microsoft /proc/version 2>/dev/null; then
  echo "WSL detectado: $(grep -oP 'WSL[0-9]+' /proc/version 2>/dev/null || echo 'verifique com: wsl -l -v')"
else
  echo "Aviso: não parece ser WSL."
fi

echo ""
echo "1) Atualizando pacotes..."
sudo apt-get update -y
sudo apt-get install -y ca-certificates curl gnupg lsb-release

if ! command -v docker >/dev/null 2>&1 || ! docker info >/dev/null 2>&1; then
  echo ""
  echo "2) Instalando Docker Engine (docker.io)..."
  sudo apt-get install -y docker.io docker-compose-v2
  sudo usermod -aG docker "$USER" || true
  sudo service docker start || sudo systemctl start docker || true
else
  echo "Docker já instalado."
fi

if ! docker info >/dev/null 2>&1; then
  echo ""
  echo ">>> Docker não respondeu. Possíveis causas:"
  echo "    - Ubuntu ainda em WSL 1 → no PowerShell (Admin): wsl --set-version Ubuntu 2"
  echo "    - Kernel WSL 2 ausente → wsl --update"
  echo "    - Reinicie o Ubuntu após: wsl --shutdown"
  exit 1
fi

echo ""
echo "3) Docker OK: $(docker --version)"

PROJECT_WIN="/mnt/c/Users/Aninha/OneDrive/Área de Trabalho/EduFlow"
if [ -d "$PROJECT_WIN" ]; then
  PROJECT="$PROJECT_WIN"
else
  echo "Ajuste o caminho do projeto no script se necessário."
  PROJECT="$(cd "$(dirname "$0")/../.." && pwd)"
fi

echo "Projeto: $PROJECT"
cd "$PROJECT"

echo ""
echo "4) Subindo SQL Server + RabbitMQ..."
docker compose up -d sqlserver rabbitmq

echo ""
echo "5) Aguardando SQL (até ~60s)..."
for i in $(seq 1 30); do
  if docker exec eduflow-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'EduFlow@123' -C -Q "SELECT 1" >/dev/null 2>&1; then
    echo "SQL Server pronto."
    break
  fi
  sleep 2
  if [ "$i" -eq 30 ]; then
    echo "SQL ainda não respondeu — tente de novo em 1 minuto."
    exit 1
  fi
done

echo ""
echo "6) Aplicando scripts SQL (se sqlcmd existir no container)..."
for f in sql/01-staging.sql sql/02-warehouse.sql sql/04-staging-snapshots.sql sql/05-dw-indexes.sql sql/06-erp-sync-schedule.sql sql/07-erp-search-parameters.sql; do
  if [ -f "$f" ]; then
    echo "  -> $f"
    docker exec -i eduflow-sql /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'EduFlow@123' -C -i /dev/stdin < "$f" || true
  fi
done

echo ""
echo "=== Setup concluído ==="
echo "RabbitMQ UI: http://localhost:15672  (eduflow / eduflow_secret)"
echo ""
echo "No WINDOWS (PowerShell), em 3 terminais:"
echo "  dotnet run --project src/EduFlow.Api"
echo "  dotnet run --project src/EduFlow.Workers"
echo "  cd src/frontend && npm run dev"
echo ""
echo "Front: http://localhost:3000  |  API: http://localhost:8080"
echo "Guia: RODAR-WSL.md"
