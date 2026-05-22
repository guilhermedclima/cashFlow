# ADR-0004: Outbox Pattern for reliable event publishing

- **Status:** Accepted
- **Date:** 2026-05-19

## Context

The `Transactions` service needs, when registering a `Transaction`, to publish a `TransactionRegisteredEvent` that `Reporting` will consume. The question is: how do we ensure the event is published **if and only if** the `Transaction` was persisted?

Publishing directly from the handler has two failure scenarios:

1. **DB commit OK -> broker unavailable:** the `Transaction` exists, the event is never published -> `Reporting` is forever out of date.
2. **Broker OK -> DB rollback:** event published for a `Transaction` that does not exist -> `Reporting` records a phantom balance.

Even distributed transactions (XA / 2PC) do not solve this well, and are heavy/slow.

## Decision

Implement the **Outbox Pattern**:

1. When the `RegisterTransactionHandler` runs, it inserts TWO records in the same local transaction:
   - The `Transaction` in the `transactions` table.
   - The serialized message in the `outbox_messages` table (with `published_at_utc = NULL`).
2. A `BackgroundService` (`OutboxPublisherService`) performs periodic polling (e.g. every 500ms):
   - SELECT messages with `published_at_utc IS NULL`.
   - Publishes to RabbitMQ via MassTransit.
   - UPDATE `published_at_utc = NOW()` after publication confirmation.
3. Messages that fail N times remain with `attempts >= MaxAttempts` and require manual inspection (future evolution: `outbox_dead_messages` table).

## Alternatives

### Publish directly from the handler
- Inconsistency window unacceptable.
- Rejected.

### Distributed transaction (2PC)
- Poor performance, distributed lock, complex MSDTC/XA.
- Most modern brokers don't support it properly.
- Rejected.

### CDC (Debezium / Postgres logical replication)
- Works well, avoids outbox code.
- More infrastructure for the scope.
- Documented as roadmap.

## Consequences

Positive:
- **At-least-once** delivery guarantee with a simple local transaction.
- Does not depend on broker guarantees.
- Natural audit trail (the `outbox_messages` table is the log of published events).

Negative (and mitigations):
- Additional latency (polling interval). Mitigation: short interval (500ms) and/or use Postgres `LISTEN/NOTIFY` for immediate wake-up.
- Consumer must be idempotent (ADR-0008).
- `outbox_messages` table grows -> periodic archival job for messages already published.
