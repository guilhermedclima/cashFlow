# Changelog

All notable changes to this project are documented here.

Format based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Planned
- Event Sourcing on the `Transaction` aggregate (see ARCHITECTURE.md §14)
- Schema registry for event versioning
- Migration from Docker Compose to Kubernetes manifests + HPA
- OIDC authentication via Keycloak (today only dev-only JWT HMAC)

---

## [0.2.0] - 2026-05-21

### Changed

- **Code is now in English.** Bounded contexts were renamed (`Lancamentos` -> `Transactions`, `Consolidado` -> `Reporting`); identifiers, tables, routes, and configs are now in English.

### Main renames

| Before | Now |
|---|---|
| `Lancamento` | `Transaction` |
| `SaldoDiario` | `DailyBalance` |
| `Dinheiro` | `Money` |
| `TipoLancamento.Credito` / `Debito` | `TransactionType.Credit` / `Debit` |
| `ComercianteId`, `Valor`, `Descricao` | `MerchantId`, `Amount`, `Description` |
| `LancamentoRegistrado(Event)` | `TransactionRegistered(Event)` |
| `POST /api/v1/lancamentos` | `POST /api/v1/transactions` |
| `GET /api/v1/consolidado/{id}/{data}` | `GET /api/v1/daily-balance/{id}/{date}` |
| Table `lancamentos` | `transactions` |
| Table `saldos_diarios` | `daily_balances` |
| Table `lancamentos_processados` | `processed_transactions` |
| DB `lancamentos_db` / `consolidado_db` | `transactions_db` / `reporting_db` |

---

## [0.1.0] - 2026-05-19

First release. Initial implementation of the Software Architect challenge (legacy Portuguese-named version).

### Added

**Architecture**
- Two independent microservices (event-driven, light CQRS).
- 100% asynchronous communication via RabbitMQ (satisfies NFR-01).
- Outbox Pattern for at-least-once publishing.
- Consumer idempotency via a dedup table.
- Cache-aside in Redis with a 60s TTL and active invalidation.
- Graceful degradation: balance query responds even when the cache is down.

**Domain**
- `Transaction` aggregate following Clean Architecture and lightweight DDD.
- `Money` Value Object with validations and banker's rounding.
- `TransactionRegistered` Domain Event.
- Invariants: positive amount, non-future date, description <= 500 chars.

**Infrastructure**
- PostgreSQL 16 (database per service) + Redis 7.
- MassTransit + RabbitMQ 3.13.
- EF Core 8 with snake_case naming.
- Polly for exponential retry + circuit breaker.

**Observability**
- Structured Serilog (JSON) with a Seq sink.
- OpenTelemetry traces + metrics (OTLP -> Jaeger / Prometheus).
- Custom metrics: `outbox_published_total`, `outbox_failed_total`, `balance_cache_hits_total`, `balance_cache_misses_total`.
- Correlation ID propagated through the `X-Correlation-Id` header.
- Health checks `/health/live` and `/health/ready` covering DB, broker, and cache.

**Tests**
- Domain unit tests (`Transaction`, `Money`, `DailyBalance`).
- Handler unit tests using NSubstitute.
- Integration tests using Testcontainers (real Postgres).
- NBomber load test validating 50 req/s for NFR-02.

**Documentation**
- `docs/ARCHITECTURE.md` with 5 C4 + sequence diagrams in Mermaid.
- 9 ADRs in Michael Nygard format under `docs/adr/`.
- `requests.http` (REST Client) with test scenarios.
- README with full execution instructions.

**DevOps**
- Docker Compose with the full stack (8 containers).
- 4 multi-stage Dockerfiles (Api and Worker x 2 services).
- GitHub Actions CI: build, unit tests, integration via Testcontainers, Dockerfile build smoke.
- Dependabot configured for weekly grouped updates.
- Makefile with 17 shortcuts.
- Scripts: `smoke-test.sh`, `generate-migrations.sh`, `demo.sh`, `export-diagrams.sh`.

**Git hygiene**
- `.editorconfig`, `.gitattributes`, `.dockerignore`.
- `LICENSE` (MIT), `CHANGELOG.md`, `PULL_REQUEST_TEMPLATE.md`.
- `global.json` pinning .NET 8.
- `Directory.Build.props` with nullable enabled and warnings-as-errors.

[Unreleased]: https://github.com/guilhermedclima/cashFlow/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/guilhermedclima/cashFlow/releases/tag/v0.2.0
[0.1.0]: https://github.com/guilhermedclima/cashFlow/releases/tag/v0.1.0
