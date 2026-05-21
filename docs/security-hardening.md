# EduFlow — Endurecimento de segurança (Fase 3)

Relatório das medidas aplicadas. Ambiente **Development** continua com demo, Swagger e bootstrap.

## Achado → Correção

| Achado | Correção |
|--------|----------|
| JWT fraco em produção | `Program.cs` já valida `Jwt:Secret` (≥32 chars, sem `DEV_ONLY`) — mantido e documentado |
| Bootstrap público em produção | `BootstrapController` retorna 404 fora de Development |
| Registro público de tenant | `POST /api/auth/register` — 404 fora de Development |
| Ops acessível a qualquer usuário logado | `OpsController` exige role **admin** |
| Sync/ERP sem RBAC | Mantido `[Authorize(Roles = "admin")]` em sync e ERP |
| Token Sponte em texto claro no SQL | `ISecretProtector` + Data Protection (`dp1:` no campo Password) |
| CORS aberto em produção | Falha na subida se `Cors:Origins` vazio em Production |
| Sem rate limit em login | Política `auth`: 15 req/min por IP |
| Sync abusável | Política `sync`: 30 req/min por tenant |
| Logs com parâmetros Sponte | Log redige `sParametrosBusca` |
| Detalhe de exceção no dashboard | `detail` só em Development |
| Headers de segurança ausentes | `SecurityHeadersMiddleware` (HSTS em produção) |
| Isolamento tenant | Controllers usam `User.GetRequiredTenantId()`; DW/Staging filtram `TenantId` |

## Configuração em produção

```bash
Jwt__Secret=<segredo-aleatorio-32+-caracteres>
Cors__Origins__0=https://app.suaescola.com.br
ConnectionStrings__Staging=...
ConnectionStrings__Warehouse=...
```

Painel ops no front: manter `NEXT_PUBLIC_SHOW_OPS_PANEL=false` para gestores.

## Credenciais ERP existentes

Senhas já salvas em texto claro continuam funcionando (`Unprotect` legado). Ao **salvar** de novo em Configurações, passam a ser criptografadas.

| Sponte demo em produção | `SponteSoapConnector` respeita `ErpConnectors:AllowDemoFallback` |
| API atrás do Caddy | `ForwardedHeaders` + validação CORS HTTPS / sem placeholder |
| `EDUFLOW_INIT_SCHEMA` em Production | Bloqueado na subida |
| Piloto Vercel | `NEXT_PUBLIC_ENABLE_DEMO_LOGIN=false`; erros API sanitizados em `client.ts` |

Auditoria piloto (camadas 1–10): relatório local `docs/SECURITY-AUDIT-PILOT.md` (não versionado no GitHub).

## Pendências (fases futuras)

- CSP estrita (requer ajuste do Next.js)
- JWT em cookie `httpOnly` (mitigar XSS no token)
- Rotação de chave Data Protection em farm App Service (blob/redis)
- Auditoria completa de logs Serilog (filtro global de PII)
- Azure Key Vault para segredos (Fase 5)
