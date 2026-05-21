# EduFlow

Plataforma de inteligência educacional para gestores escolares — painéis, insights e integração ERP (Sponte).

## Stack

- **Frontend:** Next.js 14 — `src/frontend`
- **API:** .NET 8 — `src/EduFlow.Api`
- **Dados:** SQL Server, RabbitMQ, workers

## Desenvolvimento local (Windows)

Ver [RODAR-TUDO.md](RODAR-TUDO.md).

```powershell
dotnet run --project src/EduFlow.Api
dotnet run --project src/EduFlow.Workers
cd src/frontend; npm run dev
```

## Deploy (Vercel + VM)

- Frontend na Vercel: root directory `src/frontend`
- Backend: `docker-compose.pilot.yml` + `deploy/.env.production.example`
- Scripts: `scripts/deploy/`

Variáveis de ambiente e segredos: copiar os arquivos `deploy/.env.*.example` (nunca commitar `.env.production`).

## Documentação técnica

- [docs/security-hardening.md](docs/security-hardening.md)
- [docs/erp-connectors.md](docs/erp-connectors.md)
