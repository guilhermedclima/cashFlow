# ADR-0008: Idempotency in the Reporting consumer

- **Status:** Accepted
- **Date:** 2026-05-19

## Context

RabbitMQ (and any robust broker) delivers messages **at-least-once**. The Outbox Pattern (ADR-0004) reinforces this — in case of a failure during publication, the message can be published more than once. Consequently, the `TransactionRegisteredConsumer` in `Reporting` may receive the same `TransactionRegisteredEvent` multiple times.

If we apply the balance delta without protection, the balance doubles/triples. Unacceptable.

## Decision

Guarantee **idempotency** in the consumer via explicit dedup:

1. `processed_transactions` table in the `Reporting` schema:
   ```
   transaction_id (PK, UUID)
   processed_at_utc (timestamptz)
   ```
2. Consumer logic:
   ```
   BEGIN TX
     -- Try to mark as processed. PK conflict = duplicate.
     INSERT INTO processed_transactions (transaction_id, processed_at_utc)
     VALUES ($1, NOW())
     ON CONFLICT (transaction_id) DO NOTHING
     RETURNING 1;
     -- If nothing returned, it's a duplicate -> silent ACK and exit
     UPSERT into daily_balances (total_credits/total_debits += amount)
   COMMIT
   ACK on the queue
   ```
3. Everything in a local transaction — guarantees atomicity between dedup and balance update.

## Alternatives

### Trust the broker (exactly-once delivery)
- RabbitMQ does not guarantee this. Kafka has "exactly-once semantics" only within the same cluster and with specific configuration.
- In any case, consumer idempotency is defense in depth.

### Idempotency based on message hash
- Works but is fragile (any change to the payload changes the hash).
- The stable `TransactionId` is more natural and cheaper.

### Optimistic versioning on the balance aggregate
- Works to avoid concurrent lost updates, but does not detect replay.
- Combinable with dedup, does not replace it.

## Consequences

Positive:
- Correct balance even with replay, redeliveries, or publisher bugs.
- Defense in depth.

Negative:
- `processed_transactions` table grows indefinitely -> archival job (keep last 90 days).
- Implicit per-row (UUID) lock — does not cause relevant contention.
