# ADR-0003: RabbitMQ as messaging broker

- **Status:** Accepted
- **Date:** 2026-05-19

## Context

Defined in [ADR-0002](0002-mensageria-assincrona.md) that communication will be asynchronous via broker. We need to choose which one.

Criteria:
- Pub/sub support (multiple consumers possible in the future).
- Operable locally via Docker.
- Sufficient for the challenge volume (50 req/s ~= 4.3M messages/day in the worst case).
- DLQ support.
- Maturity and .NET ecosystem.

## Decision

**RabbitMQ** with:
- `topic` type exchange (`lancamentos.events`).
- Routing key `lancamento.registrado`.
- Durable queue `consolidado.lancamento-registrado`.
- DLQ via `x-dead-letter-exchange` for messages that fail after N retries.
- .NET client via **MassTransit** (abstraction over `RabbitMQ.Client`).

## Alternatives

### Apache Kafka
- Excellent for high throughput and historical replay.
- Overkill for 50 req/s.
- High operational complexity (Zookeeper/KRaft, partitions, consumer groups).
- Steeper learning curve.
- Excellent choice if volume grows 100x — documented as roadmap.

### Cloud-managed (Azure Service Bus, AWS SQS+SNS, Google Pub/Sub)
- Very simple operation.
- Cloud lock-in that doesn't fit the challenge (must run locally).

### Redis Streams
- Lighter.
- Consumer groups model less mature than RabbitMQ.
- Operationally confusing alongside Redis used as cache.

## Consequences

Positive:
- Trivial local operation (1 container).
- Excellent Management UI for debugging in dev.
- MassTransit abstracts the client -> swapping broker in the future is a configuration change.

Negative:
- Maximum throughput lower than Kafka (not a problem in scope).
- Ordering guaranteed only per queue — multi-partition requires other tactics (not a problem in scope).
