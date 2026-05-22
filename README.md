# Cash Flow

[![CI](https://github.com/guilhermedclima/cashFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/guilhermedclima/cashFlow/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Daily cash flow control system (debit/credit transactions + daily consolidation), implemented as event-driven microservices in .NET 8. **Documentation in English, code in English.**

> Solution to the Software Architect challenge. The full architectural documentation is in [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md). Individual architectural decisions are in [`docs/adr/`](docs/adr/).

---

## Table of Contents

- [Overview](#overview)
- [Glossary PT to EN](#glossary-pt-to-en)
- [Stack](#stack)
- [Prerequisites](#prerequisites)
- [How to Run](#how-to-run)
- [Endpoints](#endpoints)
- [Tests](#tests)
- [Load Test](#load-test)
- [End-to-end Demo](#end-to-end-demo)
- [Observability](#observability)
- [Repository Structure](#repository-structure)
- [Publish on GitHub](#publish-on-github)
- [Next Steps](#next-steps)

---

## Overview

Two independent microservices (*bounded contexts*):

- **Transactions** — records credits and debits. Publishes the `TransactionRegisteredEvent` event via the Outbox Pattern.
- **Reporting** — consumes the event, materializes the daily balance (`DailyBalance`), and exposes a GET query by date, with Redis cache.

Communication between services is **100% asynchronous** via RabbitMQ. This ensures that Reporting unavailability **does not affect** Transactions (NFR-01 of the challenge).

```
[Merchant] -> [Transactions.Api] -> [Postgres] -> [Outbox Worker] -> [RabbitMQ]
                                                                     |
                                                                     v
[Merchant] <- [Reporting.Api]   <- [Redis/Postgres] <- [Consumer]  <-
```

For details, read [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

---

## Glossary PT to EN

| Portuguese (domain) | English (code) |
|---|---|
| Lancamentos (BC) | `Transactions` |
| Consolidado (BC) | `Reporting` |
| Lancamento | `Transaction` |
| Saldo diario | `DailyBalance` |
| Lancamento processado (dedup) | `ProcessedTransaction` |
| Dinheiro / Valor monetario | `Money` |
| Tipo (Credito/Debito) | `TransactionType` (`Credit` / `Debit`) |
| Comerciante | `Merchant` |
| Valor | `Amount` |
| Moeda | `Currency` |
| Descricao | `Description` |
| Data/hora do lancamento | `OccurredOnUtc` |

---

## Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 |
| API | ASP.NET Core Minimal APIs |
| Mediator | MediatR |
| ORM | EF Core 8 + PostgreSQL (Npgsql) |
| Messaging | RabbitMQ + MassTransit |
| Cache | Redis |
| Resilience | Polly |
| Observability | Serilog + OpenTelemetry (OTLP -> Jaeger/Prometheus) |
| Tests | xUnit + FluentAssertions + NSubstitute + Testcontainers |
| Load | NBomber |
| Local infra | Docker Compose |

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://docs.docker.com/get-docker/) and Docker Compose
- (Optional) [dotnet-ef](https://docs.microsoft.com/ef/core/cli/dotnet) for migrations:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

---

## How to Run

### Option 1 - Everything via Docker Compose (recommended)

```bash
git clone <this-repo>
cd cash-flow

# Bring up EVERYTHING: infra + services + observability
docker compose up -d --build

# Wait ~30s for all health checks to turn green
docker compose ps
```

Available services:

| Service | URL |
|---|---|
| Transactions API | http://localhost:5001/swagger |
| Reporting API | http://localhost:5002/swagger |
| RabbitMQ Management | http://localhost:15672 (guest/guest) |
| Seq (logs) | http://localhost:5341 |
| Jaeger (traces) | http://localhost:16686 |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3000 (admin/admin) |

### Option 2 - Infra in Docker, apps on the host

```bash
# Bring up only infra
docker compose -f docker-compose.infra.yml up -d

# Schema is created automatically via SQL scripts.
# (Optional) To use EF Core Migrations instead of SQL bootstrap:
./scripts/generate-migrations.sh
dotnet ef database update --project src/Transactions/Transactions.Infrastructure --startup-project src/Transactions/Transactions.Api
dotnet ef database update --project src/Reporting/Reporting.Infrastructure --startup-project src/Reporting/Reporting.Api

# Run the 4 hosts in separate terminals
dotnet run --project src/Transactions/Transactions.Api
dotnet run --project src/Transactions/Transactions.Worker
dotnet run --project src/Reporting/Reporting.Api
dotnet run --project src/Reporting/Reporting.Worker
```

---

## Endpoints

### Transactions (`http://localhost:5001`)

```http
POST /api/v1/transactions
Content-Type: application/json

{
  "merchantId": "11111111-1111-1111-1111-111111111111",
  "amount": 150.75,
  "type": "Credit",
  "description": "POS sale #042"
}
```

```http
GET /api/v1/transactions/{id}
```

### Reporting (`http://localhost:5002`)

```http
GET /api/v1/daily-balance/{merchantId}/{date}

# Example:
GET /api/v1/daily-balance/11111111-1111-1111-1111-111111111111/2026-05-19
```

Response:
```json
{
  "merchantId": "11111111-...",
  "date": "2026-05-19",
  "totalCredits": 1500.00,
  "totalDebits": 320.50,
  "balance": 1179.50,
  "transactionCount": 14
}
```

### Health

- `GET /health/live` - liveness
- `GET /health/ready` - readiness (checks DB, broker, Redis)

---

## Tests

```bash
# Unit (fast, no infra)
dotnet test --filter "Category=Unit"

# Integration (spins up containers via Testcontainers - requires Docker)
dotnet test --filter "Category=Integration"

# Everything
dotnet test
```

Test coverage:
- Domain (`Transaction`, `Money`, `DailyBalance`, invariants)
- Handlers (`RegisterTransactionHandler`, `GetDailyBalanceHandler`)
- Outbox publisher (publishing, retry, idempotency)
- Consumer (idempotency, dedup)
- API (HTTP tests via `WebApplicationFactory` + Testcontainers)

---

## Load Test

Validation of NFR-02 (50 req/s on Reporting with up to 5% errors):

```bash
dotnet run --project tests/CashFlow.LoadTests -c Release
```

The NBomber report is generated in `tests/CashFlow.LoadTests/reports/`.

---

## End-to-end Demo

Generates realistic synthetic traffic (mixed credits+debits, multiple `Merchant`s) and shows the `DailyBalance` evolving:

```bash
make demo              # ~120 transactions in 60s, 3 merchants
make demo-watch        # with tail of balances every 4s
```

Arguments:

```bash
./scripts/demo.sh --duration 30 --rate 10 --merchants 5 --watch
```

Useful for populating the system before exploring [Grafana](http://localhost:3000), [Jaeger](http://localhost:16686), and [Seq](http://localhost:5341).

---

## Observability

- **Structured logs** (JSON) with `CorrelationId` propagated between services via the `X-Correlation-Id` header. Viewable in [Seq](http://localhost:5341).
- **Distributed tracing** with OpenTelemetry -> Jaeger ([http://localhost:16686](http://localhost:16686)). A single span covers `POST /transactions` -> DB -> Outbox -> RabbitMQ -> Consumer -> Reporting DB.
- **Metrics** exposed at `/metrics` (Prometheus). Custom metrics:
  - `outbox_published_total`
  - `outbox_failed_total`
  - `balance_cache_hits_total`
  - `balance_cache_misses_total`
- **Dashboards** in Grafana pre-provisioned at `infra/grafana/`.

---

## Repository Structure

```
cash-flow/
├── .github/
│   ├── workflows/ci.yml          GitHub Actions: build, tests, docker
│   ├── dependabot.yml            Weekly package updates
│   └── PULL_REQUEST_TEMPLATE.md
├── docs/
│   ├── ARCHITECTURE.md           Full architectural document (EN)
│   ├── adr/                      Architecture Decision Records (EN, 9 ADRs)
│   └── diagrams/                 Mermaid sources (.mmd) of the C4 diagrams
├── infra/
│   ├── grafana/                  Pre-provisioned dashboards
│   ├── prometheus/               Config + scrape targets
│   └── postgres/                 Bootstrap SQL scripts
├── scripts/
│   ├── generate-migrations.sh    Helper for EF Core migrations
│   ├── smoke-test.sh             End-to-end smoke test via curl
│   ├── demo.sh                   Synthetic traffic generator
│   └── export-diagrams.sh        Exports Mermaid diagrams to PNG/SVG
├── src/
│   ├── Transactions/             Transactions bounded context (5 projects)
│   ├── Reporting/                Reporting bounded context (5 projects)
│   └── Shared/                   Event contracts + utilities
├── tests/
│   ├── Transactions.UnitTests       Domain + handler tests
│   ├── Transactions.IntegrationTests   Real Outbox via Testcontainers
│   ├── Reporting.UnitTests
│   ├── Reporting.IntegrationTests      Idempotency via Testcontainers
│   └── CashFlow.LoadTests         NBomber to validate NFR-02 (50 req/s)
├── docker-compose.yml            Full stack (8 containers)
├── docker-compose.infra.yml      Infra only (no apps)
├── Directory.Build.props         Common settings (nullable, warnings)
├── global.json                   .NET 8 SDK pin
├── .editorconfig                 .NET code style
├── .gitattributes                Line endings normalization
├── .dockerignore
├── nuget.config
├── Makefile                      Command shortcuts
├── requests.http                 REST Client collection
├── CHANGELOG.md
├── LICENSE                       MIT
└── CashFlow.sln                18 projects
```

---

## Publish on GitHub

This solution is ready to become a public repository. Steps:

```bash
# 1. Initialize git
cd cash-flow
git init -b main
git add .
git commit -m "feat: initial CashFlow implementation (Software Architect challenge)"

# 2. Create the repo on GitHub (via CLI or web UI)
gh repo create cashFlow --public --source=. --remote=origin --push

# OR, without GitHub CLI:
# Create the repo manually at https://github.com/new and then:
git remote add origin git@github.com:guilhermedclima/cashFlow.git
git push -u origin main
```

After the first push, the [`.github/workflows/ci.yml`](.github/workflows/ci.yml) workflow will run automatically:

1. **build** - restore + build Release + unit tests (fast).
2. **integration** - tests with Testcontainers (real Postgres).
3. **docker-build** - validates that the 4 Dockerfiles build.

[Dependabot](.github/dependabot.yml) is already configured to open weekly PRs for NuGet package, GitHub Actions, and Docker base image updates.

### Suggested commit conventions

I recommend (but it is not required) [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add list endpoint to Transactions API
fix: race condition in outbox publisher
docs: add ADR-0010 about schema registry
refactor(transactions): extract validator to its own class
test(reporting): cover consumer with duplicate deliveries
chore: bump MassTransit to 8.2.4
```

### Shortcuts via Makefile

```bash
make up                 # docker compose up -d --build
make test-unit          # fast tests
make test-integration   # tests with Testcontainers
make loadtest           # NBomber against Reporting
make smoke              # end-to-end smoke test (requires stack running)
make demo               # traffic generator
make help               # lists all targets
```

---

## Next Steps

Items consciously out of scope for the challenge. Detailed in [`docs/ARCHITECTURE.md` section 14](docs/ARCHITECTURE.md#14-future-evolutions):

- Event Sourcing on the `Transaction` aggregate
- Sharding by `MerchantId`
- API Gateway with BFF (YARP)
- OIDC + IdP (Keycloak)
- Kubernetes + HPA
- Schema Registry for event contracts
- Automated chaos engineering
- Backup/DR policy

---

## License

MIT.
