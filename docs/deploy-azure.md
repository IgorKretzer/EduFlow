# Deploy EduFlow no Azure

Guia para publicar API (.NET 8), Workers, frontend (Next.js) e bancos SQL Server em ambiente de produção, alinhado ao hardening de segurança do projeto.

## Visão geral

| Componente | Serviço Azure sugerido |
|------------|------------------------|
| API `EduFlow.Api` | App Service (Linux ou Windows) |
| Workers | App Service separado ou Container Apps |
| Frontend `src/frontend` | Azure Static Web Apps ou App Service (Node) |
| Staging + DW | Azure SQL (dois bancos ou um servidor, dois databases) |
| Filas | Azure Service Bus **ou** RabbitMQ gerenciado (VM/Container) |
| Segredos | Azure Key Vault + referências no App Service |

## Pré-requisitos

- Assinatura Azure com permissão para criar recursos
- Domínio do cliente (opcional) para HTTPS e CORS
- Scripts SQL do repositório aplicados em `EduFlow_Staging` e `EduFlow_DW`

## 1. Azure SQL

1. Crie um **SQL Server** lógico e dois databases: `EduFlow_Staging` e `EduFlow_DW`.
2. Firewall: permita apenas IPs do App Service / rede privada (VNet integration recomendada).
3. Connection strings (formato ADO.NET):

```text
Server=tcp:<servidor>.database.windows.net,1433;Database=EduFlow_Staging;User ID=<user>;Password=<senha>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

Repita para `EduFlow_DW`.

## 2. Key Vault e segredos

Armazene no Key Vault (nunca no repositório):

| Segredo | Uso |
|---------|-----|
| `ConnectionStrings--Staging` | EF Staging |
| `ConnectionStrings--Warehouse` | DW |
| `Jwt--Secret` | Assinatura JWT (mín. 32 caracteres aleatórios) |
| `RabbitMq--Password` | Mensageria |
| Token Sponte / API REST por tenant | Já persistido criptografado (`dp1:`) no banco após configuração na UI |

No App Service: **Configuration → Application settings** → referências Key Vault (`@Microsoft.KeyVault(...)`).

## 3. API — App Service

1. Publique `src/EduFlow.Api` (zip deploy, GitHub Actions ou `dotnet publish`).
2. Runtime: **.NET 8**.
3. Variáveis de ambiente (`ASPNETCORE_ENVIRONMENT=Production`):

```json
{
  "ConnectionStrings": {
    "Staging": "@KeyVault-Staging",
    "Warehouse": "@KeyVault-Warehouse"
  },
  "Jwt": {
    "Issuer": "EduFlow",
    "Audience": "EduFlow.Clients",
    "Secret": "@KeyVault-JwtSecret",
    "ExpirationMinutes": 480
  },
  "Cors": {
    "Origins": ["https://<seu-frontend>.azurestaticapps.net"]
  },
  "ErpConnectors": {
    "AllowDemoFallback": false
  },
  "RabbitMq": {
    "Host": "<host>",
    "Port": 5672,
    "Username": "eduflow",
    "Password": "@KeyVault-RabbitPassword",
    "Exchange": "eduflow.events"
  },
  "SyncScheduler": {
    "Enabled": true,
    "PollIntervalSeconds": 60,
    "DefaultIntervalMinutes": 15
  }
}
```

4. **HTTPS only** no App Service; o middleware HSTS já está ativo em Production.
5. **CORS**: liste apenas a URL do frontend publicado (`Cors:Origins`). Sem `*` em produção.
6. **Registro público**: desabilitado fora de Development (`AuthController`).
7. Data Protection: em farm com várias instâncias, configure persistência compartilhada (Blob + Key Vault) para tokens Sponte criptografados continuarem válidos após recycle.

## 4. Workers

1. Publique `src/EduFlow.Workers` em segundo App Service ou Container App.
2. Mesmas connection strings, RabbitMQ e `ErpConnectors:AllowDemoFallback=false`.
3. Health: configure probe em `/health/live` (porta configurada, ex. 5055).
4. `Ops:WorkersHealthUrl` na API deve apontar para a URL pública/interna do health dos workers.

## 5. Frontend — Static Web Apps

1. Build: `cd src/frontend && npm ci && npm run build`.
2. Defina no build/deploy:

```bash
NEXT_PUBLIC_API_URL=https://<api>.azurewebsites.net
NEXT_PUBLIC_SHOW_OPS_PANEL=false
```

3. Custom domain + HTTPS automático do SWA.
4. O browser chama apenas a API; **nunca** o ERP do cliente.

## 6. Checklist de produção

- [ ] `Jwt__Secret` forte e único por ambiente
- [ ] `ErpConnectors__AllowDemoFallback=false`
- [ ] CORS restrito ao domínio do front
- [ ] Sem `DEV_ONLY` em connection strings
- [ ] SQL com TLS (`Encrypt=True`)
- [ ] Key Vault + managed identity no App Service
- [ ] Rate limiting ativo (login/sync) — já no `Program.cs`
- [ ] Backup automático Azure SQL
- [ ] Monitoramento: Application Insights ligado à API e Workers

## 7. Conectores ERP em produção

- **Sponte (`providerKey: sponte`)**: endpoint ASMX, código cliente, token — inalterado.
- **OpenAPI (`providerKey: openapi`)**: URL base + paths em `SearchParameters*` (ex. `/students?limit=200`). Token no campo senha (Bearer ou header customizado).

Troca de fornecedor é por tenant na tela **Configurações**; o `SyncOrchestrator` resolve o connector pelo `ProviderKey` sem alteração de código.

## 8. CI/CD (esboço)

1. Build & test .NET solution.
2. `dotnet publish` API e Workers.
3. `npm run build` frontend com `NEXT_PUBLIC_API_URL` do slot de produção.
4. Deploy slots (staging → swap) para zero-downtime na API.

## 9. Rollback

- App Service: **Deployment Center → Deployment history** ou swap de slot.
- SQL: restore point-in-time no Azure SQL.
- Front SWA: redeploy do artefato anterior no GitHub Actions / pipeline.

## Referências no repositório

- `docs/security-hardening.md` — headers, rate limit, proteção de token
- `src/EduFlow.Infrastructure/Connectors/` — Sponte SOAP + OpenAPI REST
