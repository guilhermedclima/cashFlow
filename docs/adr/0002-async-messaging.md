# ADR-0002: Asynchronous communication via broker between Transactions and Reporting

- **Status:** Accepted
- **Date:** 2026-05-19

## Context

The two services need to exchange information: when a `Transaction` is registered, `Reporting` needs to reflect the new balance. There are three possible integration models:

1. Synchronous (REST from `Transactions` -> `Reporting`).
2. Asynchronous via broker (publish/subscribe).
3. Polling by `Reporting` against the `Transactions` database.

NFR-01 (`Transactions` cannot go down if `Reporting` goes down) basically decides the case.

## Decision

**100% asynchronous communication via pub/sub broker**.

- The `Transactions` service publishes the integration event `TransactionRegisteredEvent` to a RabbitMQ exchange.
- The `Reporting` service consumes the event, materializes the `DailyBalance`, and invalidates the cache.
- `Transactions` **never** calls `Reporting`, under any circumstances.

## Alternatives

### Synchronous REST
- `Transactions` couples its availability to `Reporting` -> violates NFR-01.
- Latency added to the critical path of registration.
- Rejected.

### Database polling
- `Reporting` would periodically read the `transactions` table.
- Couples databases (`Reporting` would need to access the other context's DB).
- High latency or high DB consumption depending on frequency.
- Hard to evolve.
- Rejected.

### CDC (Change Data Capture) with Debezium
- Functionally equivalent to Outbox + broker, without needing outbox code.
- More infrastructure (Kafka, Debezium connector) for the challenge scope.
- Documented in ARCHITECTURE.md section 14 as future evolution.

## Consequences

Positive:
- `Transactions` operates with `Reporting` entirely offline (events pile up in the broker).
- Allows multiple consumers in the future (BI, audit, notifications) without changing the publisher.

Negative:
- Eventual consistency.
- Need for idempotency in the consumer (see ADR-0008).
- Need for delivery guarantees in the publisher (see ADR-0004).
