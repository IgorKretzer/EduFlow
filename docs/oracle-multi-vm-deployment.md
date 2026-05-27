# Oracle Multi-VM Deployment

Arquitetura recomendada para reduzir carga da VM de 1 GB atual.

## VMs

### API VM

Mantém o IP público atual enquanto fizer sentido.

Sobe:

- `docker-compose.api.yml`
- API ASP.NET
- Caddy/HTTPS

Não sobe:

- RabbitMQ
- Workers
- SQL Server local

Comando:

```bash
export EDUFLOW_ENV_FILE=deploy/.env.production
docker compose -f docker-compose.api.yml --env-file deploy/.env.production up -d api caddy
```

### RabbitMQ VM

Sobe somente a mensageria.

Comando:

```bash
docker compose -f docker-compose.rabbitmq.yml --env-file deploy/.env.production up -d rabbitmq
```

Regras de rede recomendadas:

- Abrir `5672` somente para IP privado da API VM e Workers VM.
- Manter `15672` fechado publicamente; usar SSH tunnel se precisar da UI.

### Workers VM

Sobe os workers de ingestão, transformação, financeiro, analytics e scheduler.

Comando:

```bash
export EDUFLOW_ENV_FILE=deploy/.env.production
docker compose -f docker-compose.workers.yml --env-file deploy/.env.production up -d workers
```

## Variáveis principais

Nas VMs API e Workers:

```env
RABBITMQ_HOST=IP_PRIVADO_DA_VM_RABBITMQ
RABBITMQ_PORT=5672
RABBITMQ_USER=eduflow
RABBITMQ_PASSWORD=senha-forte
```

Na VM Workers, é possível ligar/desligar partes do processamento:

```env
WORKERS_ENABLE_STUDENTS=true
WORKERS_ENABLE_FINANCIAL=true
WORKERS_ENABLE_CONTRACTS=true
WORKERS_ENABLE_ANALYTICS=true
WORKERS_ENABLE_SCHEDULER=true
```

Para separar analytics em outra VM no futuro:

Workers principais:

```env
WORKERS_ENABLE_STUDENTS=true
WORKERS_ENABLE_FINANCIAL=true
WORKERS_ENABLE_CONTRACTS=true
WORKERS_ENABLE_ANALYTICS=false
WORKERS_ENABLE_SCHEDULER=true
```

Workers analytics:

```env
WORKERS_ENABLE_STUDENTS=false
WORKERS_ENABLE_FINANCIAL=false
WORKERS_ENABLE_CONTRACTS=false
WORKERS_ENABLE_ANALYTICS=true
WORKERS_ENABLE_SCHEDULER=false
```

## GitHub Actions Secrets

Obrigatórios:

- `ORACLE_SSH_KEY`
- `ORACLE_API_HOST`

Recomendados:

- `ORACLE_WORKERS_HOST`
- `ORACLE_RABBITMQ_HOST`
- `ORACLE_PORT`
- `ORACLE_USER`
- `ORACLE_APP_DIR`
- `API_HEALTH_URL`

Valores padrão usados pela pipeline:

- `ORACLE_PORT`: `22`
- `ORACLE_USER`: `ubuntu`
- `ORACLE_APP_DIR`: `/home/ubuntu/EduFlow`
- `API_HEALTH_URL`: `https://edu-flow-api.duckdns.org/health/live`

## Ordem de implantação

1. Criar RabbitMQ VM.
2. Configurar `deploy/.env.production` na RabbitMQ VM.
3. Subir `docker-compose.rabbitmq.yml`.
4. Criar Workers VM.
5. Configurar `deploy/.env.production` com `RABBITMQ_HOST`.
6. Subir `docker-compose.workers.yml`.
7. Ajustar API VM com `RABBITMQ_HOST`.
8. Subir `docker-compose.api.yml`.

Enquanto a migração não acontecer, `docker-compose.pilot.yml` continua disponível para o modelo atual em uma única VM.
