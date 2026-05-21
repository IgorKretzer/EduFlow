# Conectores ERP (multi-protocolo)

## Arquitetura

- Contrato único: `IErpConnector` → `RawErpRecord` → ingestão + `ICanonicalNormalizer` → eventos RabbitMQ.
- `SyncOrchestrator` escolhe o adapter pelo `ProviderKey` do tenant (`IEnumerable<IErpConnector>` registrado no DI).
- Sponte permanece em `SponteSoapConnector`; REST em `OpenApiRestConnector`.

## Fornecedores suportados

| ProviderKey | Protocolo | Configuração |
|-------------|-----------|--------------|
| `sponte` | SOAP | Endpoint ASMX, `Username` = nCodigoCliente, `Password` = sToken, `SearchParameters*` = `Situacao=2\|TOP=150` |
| `openapi` | REST GET | `EndpointUrl` = base, `SearchParameters*` = path relativo (`/students?limit=100`), `Password` = token |

### Autenticação REST

- `Username` vazio → `Authorization: Bearer {Password}`
- `Username` = `ApiKey` → header `X-Api-Key: {Password}`
- Outro `Username` → header customizado com valor `{Password}`

### JSON → canônico

Campos aceitos (exemplos): `id`, `enrollmentCode`, `status`, `debtAmount`, `dueDate`. Ver `OpenApiCanonicalNormalizer.cs`.

## Demo fallback

`ErpConnectors:AllowDemoFallback` em `appsettings`:

- `true` em Development — gera registros de exemplo se a API falhar ou vier vazia.
- `false` em Production (padrão em `appsettings.json`).

## Adicionar outro ERP

1. Implementar `IErpConnector` com novo `ProviderKey`.
2. Registrar `services.AddScoped<IErpConnector, MeuConnector>();`
3. Criar normalizador e estender `CompositeCanonicalNormalizer`.
4. Permitir o key em `ErpConfigService.NormalizeProvider`.
