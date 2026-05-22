# Architecture Decision Records

Individual architectural decisions, in Michael Nygard format (Context -> Decision -> Alternatives -> Consequences).

| # | Title | Status |
|---|---|---|
| [0001](0001-microservices.md) | Adopt microservices architecture | Accepted |
| [0002](0002-async-messaging.md) | Asynchronous communication via broker | Accepted |
| [0003](0003-rabbitmq.md) | RabbitMQ as broker | Accepted |
| [0004](0004-outbox-pattern.md) | Outbox Pattern for reliable publishing | Accepted |
| [0005](0005-lightweight-cqrs.md) | Lightweight CQRS (no Event Sourcing) | Accepted |
| [0006](0006-postgres-redis.md) | PostgreSQL + Redis | Accepted |
| [0007](0007-polly-resilience.md) | Resilience with Polly | Accepted |
| [0008](0008-idempotency.md) | Idempotency in the consumer | Accepted |
| [0009](0009-clean-architecture.md) | Clean Architecture per service | Accepted |

## How to create a new ADR

1. Next available number (zero-padded to 4 digits).
2. Copy the structure from any existing ADR as a starting point.
3. Initial status: `Proposed`.
4. PR with discussion; mark as `Accepted` or `Rejected` after review.
5. Accepted ADRs are only changed via a new ADR that supersedes them (status `Superseded by: ADR-NNNN`).
