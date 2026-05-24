# Build da API/Workers no PC (VM Oracle 1 GB trava no `docker build`)

Na Micro, compilar **dentro** do Docker (`sdk:8.0` + `dotnet publish`) costuma travar ou levar >1h.

## Caminho recomendado: publicar no Windows + imagem leve na VM

### 1) No PC (PowerShell) — com [.NET 8 SDK](https://dotnet.microsoft.com/download) e Docker Desktop

```powershell
cd "C:\Users\Aninha\OneDrive\Área de Trabalho\EduFlow"

dotnet publish src/EduFlow.Api/EduFlow.Api.csproj -c Release -o artifacts/publish/api
dotnet publish src/EduFlow.Workers/EduFlow.Workers.csproj -c Release -o artifacts/publish/workers

$env:DOCKER_BUILDKIT=0
docker build -f docker/Dockerfile.api.runtime -t eduflow-pilot-api:latest .
docker build -f docker/Dockerfile.workers.runtime -t eduflow-pilot-workers:latest .

docker save eduflow-pilot-api:latest -o eduflow-api.tar
docker save eduflow-pilot-workers:latest -o eduflow-workers.tar
```

### 2) Enviar para a VM

```powershell
scp -i "C:\Users\Aninha\Downloads\ssh-key-2026-05-22.key" eduflow-api.tar eduflow-workers.tar ubuntu@163.176.225.154:~/
```

(IP atual da VM se mudou.)

### 3) Na VM — carregar imagens

```bash
docker load -i ~/eduflow-api.tar
docker load -i ~/eduflow-workers.tar
docker images | grep eduflow-pilot
```

### 4) Na VM — EF + subir stack (sem `build`)

```bash
cd ~/EduFlow
export EDUFLOW_ENV_FILE=deploy/.env.production

docker compose -f docker-compose.pilot.yml --env-file deploy/.env.production run --rm \
  -e EDUFLOW_INIT_SCHEMA=true -e ASPNETCORE_ENVIRONMENT=Development api --no-build

# SQL 08/09 se ainda não rodou após EF (ver deploy/AZURE-SQL-ORACLE.md)

docker compose -f docker-compose.pilot.yml --env-file deploy/.env.production \
  up -d rabbitmq api workers caddy --no-build
```

---

## Alternativa: só `dotnet publish` na VM (sem SDK no Docker)

Com swap 2G e pacote .NET 8:

```bash
wget https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
chmod +x /tmp/dotnet-install.sh
/tmp/dotnet-install.sh --channel 8.0
export PATH="$HOME/.dotnet:$PATH"

cd ~/EduFlow
dotnet publish src/EduFlow.Api/EduFlow.Api.csproj -c Release -o artifacts/publish/api
dotnet publish src/EduFlow.Workers/EduFlow.Workers.csproj -c Release -o artifacts/publish/workers

docker pull mcr.microsoft.com/dotnet/aspnet:8.0
docker pull mcr.microsoft.com/dotnet/runtime:8.0
DOCKER_BUILDKIT=0 docker build -f docker/Dockerfile.api.runtime -t eduflow-pilot-api:latest .
DOCKER_BUILDKIT=0 docker build -f docker/Dockerfile.workers.runtime -t eduflow-pilot-workers:latest .
```

Depois `up` com `--no-build` como acima.

---

## Se o build antigo “travou”

```bash
docker ps -a
docker buildx ls 2>/dev/null
# matar build preso:
docker ps -q --filter "status=running" 
# Ctrl+C no terminal do build; depois:
docker builder prune -f
free -h
df -h /
```

Camadas `0B / 3.33kB` por horas = rede/registry ou disco; não espere indefinidamente.
