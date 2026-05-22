# EduFlow — Azure SQL + app na Oracle (1 GB)

Banco no **Azure SQL Database**; na VM Oracle só **RabbitMQ + API + Workers + Caddy**.

## 1. Criar Azure SQL (Portal) — passo a passo

### 1.1 Iniciar o assistente

1. [portal.azure.com](https://portal.azure.com) → **Criar um recurso**
2. Busque **SQL Database** → **Criar**
3. Na aba **Básico**, em **Servidor do SQL**, escolha **Criar novo** (não use servidor existente na primeira vez)

### 1.2 Criar servidor do banco de dados SQL

| Campo | O que colocar | Por quê |
|-------|----------------|---------|
| **Nome do servidor** | `eduflow-prod` | Fica `eduflow-prod.database.windows.net` — use esse FQDN no `.env` |
| **Localização** | **Brazil South** | Mesma região reduz latência para a VM Oracle no Brasil |
| **Método de autenticação** | **Usar autenticação SQL** | A API na Oracle conecta com **usuário + senha** (connection string). Entra-only exigiria identidade gerenciada (mais complexo) |
| **Nome de logon do administrador do servidor** | `eduflowadmin` | É o `AZURE_SQL_USER` / `User ID=` nas connection strings |
| **Senha** | Senha forte (16+ chars, maiúsc, minúsc, número, símbolo) | Mesma em `AZURE_SQL_PASSWORD` e nas duas `ConnectionStrings__*` |
| **Confirmar senha** | Repetir | — |

**Não marque** “Usar somente Microsoft Entra” neste piloto, a menos que você configure depois identidade gerenciada no App Service (não é o caso da VM Oracle).

**Opcional:** “Usar autenticação SQL **e** Microsoft Entra” — pode marcar se quiser entrar no portal com sua conta Microsoft; a API continua usando o login SQL `eduflowadmin`.

**Administrador do Microsoft Entra:** pode deixar em branco / padrão se escolheu só SQL.

Clique em **OK** para voltar ao formulário do banco.

### 1.3 Banco de dados (mesma tela Básico)

| Campo | O que colocar |
|-------|----------------|
| **Assinatura** | A sua (ex. Azure for Students / Pay-As-You-Go) |
| **Grupo de recursos** | Novo: `eduflow-rg` (ou um nome que você já use) |
| **Nome do banco de dados** | `EduFlow_Staging` (primeiro) — depois você cria o DW ou deixa o script criar |
| **Deseja usar o pool elástico de SQL?** | **Não** |
| **Ambiente de computação** | **Provisionado** (mais previsível) ou **Serverless** (paga uso; bom para piloto barato) |
| **Tipo de computação** | Piloto barato: **Basic** (DTU) ou Serverless **Gen5, 1 vCore** com pausa automática |
| **Backup** | Padrão (geo-redundância opcional; pode desligar em dev para economizar) |

**Avançar** → aba **Rede** (crítico).

### 1.4 Rede (firewall)

| Campo | O que colocar |
|-------|----------------|
| **Método de conectividade** | **Ponto de extremidade público** |
| **Permitir que os serviços do Azure acessem este servidor** | **Não** (a VM Oracle não é “serviço Azure” neste sentido) |
| **Adicionar endereço IPv4 atual** | **Sim** (seu PC — para testar no SSMS/Azure Data Studio) |
| **Regras de firewall** | **Adicionar regra:** Nome `oracle-vm`, IP inicial e final = IP público da VM (`137.131.173.66` ou o IP atual em Oracle → Instância → IP público) |

**Avançar** → **Segurança** → pode deixar padrão → **Avançar** → **Tags** (opcional) → **Revisar + criar**.

Aguarde a implantação (alguns minutos).

### 1.5 Segundo banco `EduFlow_DW`

O script `apply-sql-azure.sh` tenta criar os dois bancos. Se preferir pelo portal:

1. Abra o **servidor SQL** `eduflow-prod` (não só um database)
2. **Bancos de dados SQL** → **+ Criar**
3. Nome: `EduFlow_DW`, mesma região/tier parecido com Staging

Se só existir `EduFlow_Staging`, rode o script na VM — ele executa `00-pilot-databases.sql` no `master`.

### 1.6 Conferir firewall depois de criar

**SQL Server** `eduflow-prod` → **Segurança** → **Rede** → confirme:

- Regra com IP da VM Oracle
- Regra com seu IP (temporário)

Se a VM Oracle mudar de IP, atualize a regra.

## 2. `deploy/.env.production` na VM

```bash
EDUFLOW_SQL_TARGET=azure

AZURE_SQL_FQDN=eduflow-prod.database.windows.net
AZURE_SQL_USER=eduflowadmin
AZURE_SQL_PASSWORD=<senha do admin SQL>

ConnectionStrings__Staging='Server=tcp:eduflow-prod.database.windows.net,1433;Database=EduFlow_Staging;User ID=eduflowadmin;Password=<mesma senha>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
ConnectionStrings__Warehouse='Server=tcp:eduflow-prod.database.windows.net,1433;Database=EduFlow_DW;User ID=eduflowadmin;Password=<mesma senha>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

Aspas simples `'...'` são obrigatórias por causa do espaço em `User ID` (senão `source` do script dá `User: command not found`).

# Rabbit, JWT, CORS, domínio — como antes
RABBITMQ_USER=eduflow
RABBITMQ_PASSWORD=...
Jwt__Secret=...
Cors__Origins__0=https://edu-flow-taupe.vercel.app
API_DOMAIN=edu-flow.duckdns.org
ACME_EMAIL=...
Pilot__AllowRegistration=false
ErpConnectors__AllowDemoFallback=false
```

`MSSQL_SA_PASSWORD` **não é necessário** com Azure SQL.

## 3. Parar SQL local na VM (libera RAM)

```bash
cd ~/EduFlow
docker compose -f docker-compose.pilot.yml --env-file deploy/.env.production stop sqlserver 2>/dev/null || true
docker rm -f eduflow-pilot-sql 2>/dev/null || true
```

## 4. Subir produção

```bash
export EDUFLOW_ENV_FILE=deploy/.env.production
./scripts/deploy/init-production.sh
```

Ou manualmente:

```bash
docker compose -f docker-compose.pilot.yml --env-file deploy/.env.production up -d rabbitmq
bash scripts/deploy/apply-sql-azure.sh
docker compose -f docker-compose.pilot.yml --env-file deploy/.env.production build api
docker compose -f docker-compose.pilot.yml --env-file deploy/.env.production run --rm \
  -e EDUFLOW_INIT_SCHEMA=true -e ASPNETCORE_ENVIRONMENT=Development api
docker compose -f docker-compose.pilot.yml --env-file deploy/.env.production up -d api workers caddy
```

## 5. Verificar

```bash
curl -s https://edu-flow.duckdns.org/health/live
curl -s https://edu-flow.duckdns.org/api/version
```

## 6. Primeira escola

Temporariamente `Pilot__AllowRegistration=true`, reinicie API, rode `create-tenant.sh`, volte para `false`.

## Backup

Com Azure SQL use **retention automática** no portal ou export BACPAC. O script `backup-sql.sh` é só para SQL em container local.

## Vercel

`NEXT_PUBLIC_API_URL=https://edu-flow.duckdns.org` (sem mudança).
