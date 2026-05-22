# Cash Flow

[![CI](https://github.com/guilhermedclima/cashFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/guilhermedclima/cashFlow/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Daily cash flow control system (debit/credit transactions + daily consolidation), implemented as event-driven microservices in .NET 8.

Full architectural documentation is in [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md); individual decisions are in [`docs/adr/`](docs/adr/).

---

## Overview

Two independent bounded contexts:

- **Transactions** — records credits and debits. Publishes `TransactionRegisteredEvent` via the Outbox Pattern.
- **Reporting** — consumes the event, materializes the daily balance (`DailyBalance`), and exposes a GET query by date, with Redis cache.

Communication between services is **100% asynchronous** via RabbitMQ, so Reporting unavailability does not affect Transactions (NFR-01).

```
[Merchant] -> [Transactions.Api] -> [Postgres] -> [Outbox Worker] -> [RabbitMQ]
                                                                     |
                                                                     v
[Merchant] <- [Reporting.Api]   <- [Redis/Postgres] <- [Consumer]  <-
```

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

---

## How to Run

### Option 1 - Full stack via Docker Compose (recommended)

```bash
git clone https://github.com/guilhermedclima/cashFlow.git
cd cashFlow

docker compose up -d --build

# Wait ~30s for health checks
docker compose ps
```

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
docker compose -f docker-compose.infra.yml up -d

dotnet run --project src/Transactions/Transactions.Api
dotnet run --project src/Transactions/Transactions.Worker
dotnet run --project src/Reporting/Reporting.Api
dotnet run --project src/Reporting/Reporting.Worker
```

The Postgres schema is bootstrapped automatically by `infra/postgres/init-*.sql` on the first container start.

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
```

Response:
```json
{
  "merchantId": "11111111-1111-1111-1111-111111111111",
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

Covered:
- Domain (`Transaction`, `Money`, `DailyBalance`, invariants)
- Handlers (`RegisterTransactionHandler`, `GetDailyBalanceHandler`)
- Outbox publisher (publishing, retry, idempotency)
- Consumer (idempotency, dedup)
- API (HTTP tests via `WebApplicationFactory` + Testcontainers)

End-to-end smoke test (with the full stack up):

```bash
./scripts/smoke-test.sh
```

---

## Load Test

Validation of NFR-02 (50 req/s on Reporting with up to 5% errors):

```bash
dotnet run --project tests/CashFlow.LoadTests -c Release
```

The NBomber report is generated in `tests/CashFlow.LoadTests/reports/`.

---

## Demo

Generates realistic synthetic traffic (mixed credits and debits across multiple merchants) and shows the `DailyBalance` evolving:

```bash
./scripts/demo.sh                                       # ~120 transactions in 60s, 3 merchants
./scripts/demo.sh --duration 30 --rate 10 --merchants 5 --watch
```

Useful for populating the system before exploring [Grafana](http://localhost:3000), [Jaeger](http://localhost:16686), and [Seq](http://localhost:5341).

---

## Observability

- **Structured logs** (JSON) with `CorrelationId` propagated between services via the `X-Correlation-Id` header. Viewable in [Seq](http://localhost:5341).
- **Distributed tracing** with OpenTelemetry -> Jaeger. A single span covers `POST /transactions` -> DB -> Outbox -> RabbitMQ -> Consumer -> Reporting DB.
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
|-- .github/
|   |-- workflows/ci.yml          GitHub Actions: build, unit tests, docker
|   |-- dependabot.yml            Weekly package updates
|   `-- PULL_REQUEST_TEMPLATE.md
|-- docs/
|   |-- ARCHITECTURE.md           Full architectural document
|   |-- adr/                      Architecture Decision Records (9 ADRs)
|   `-- diagrams/                 Mermaid sources (.mmd) of the C4 diagrams
|-- infra/
|   |-- grafana/                  Pre-provisioned dashboards
|   |-- prometheus/               Config + scrape targets
|   `-- postgres/                 Bootstrap SQL scripts
|-- scripts/
|   |-- smoke-test.sh             End-to-end smoke test via curl
|   |-- demo.sh                   Synthetic traffic generator
|   |-- generate-migrations.sh    Helper for EF Core migrations
|   `-- export-diagrams.sh        Exports Mermaid diagrams to PNG/SVG
|-- src/
|   |-- Transactions/             Transactions bounded context (5 projects)
|   |-- Reporting/                Reporting bounded context (5 projects)
|   `-- Shared/                   Event contracts + observability
|-- tests/
|   |-- Transactions.UnitTests
|   |-- Transactions.IntegrationTests
|   |-- Reporting.UnitTests
|   |-- Reporting.IntegrationTests
|   `-- CashFlow.LoadTests        NBomber load test for NFR-02
|-- docker-compose.yml            Full stack (12 containers)
|-- docker-compose.infra.yml      Infra only (no apps)
|-- Makefile                      Common command shortcuts
|-- requests.http                 REST Client collection
`-- CashFlow.sln                  17 projects
```

---

## Next Steps

Items consciously out of scope, detailed in [`docs/ARCHITECTURE.md` section 14](docs/ARCHITECTURE.md#14-future-evolutions):

- Event Sourcing on the `Transaction` aggregate
- Sharding by `MerchantId`
- API Gateway with BFF (YARP)
- OIDC + IdP (Keycloak)
- Kubernetes + HPA
- Schema Registry for event contracts

---

## License

MIT.
