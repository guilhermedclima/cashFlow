# ADR-0006: PostgreSQL as primary database + Redis as cache

- **Status:** Accepted
- **Date:** 2026-05-19

## Context

We need:
- A reliable transactional database for `Transactions` (local transaction with outbox).
- A database for the `Reporting` materialized model.
- A read cache to support 50 req/s without hammering the database.

## Decision

- **PostgreSQL 16** as the primary database in both services (database per service: `transactions_db` and `reporting_db`).
- **Redis 7** as cache in the `Reporting` service.

Cache strategy:
- **Cache-aside** with 60s TTL for the queried `DailyBalance`.
- **Active invalidation** by the Consumer when updating the balance (DEL `balance:{merchant}:{date}`).
- In case of Redis failure: fallback to direct read from the database (graceful degradation).

## Alternatives

### SQL Server
- Natural option in the .NET world.
- Heavier licensing for production; LocalDB only on Windows.
- Postgres is open-source, multi-platform, excellent support in EF Core.

### NoSQL (MongoDB, DynamoDB)
- Attractive for the `Reporting` denormalized model.
- Cost of operating two distinct engines doesn't pay off for the challenge.
- Postgres can be a sufficient "document store" via JSONB if needed.

### Distributed cache via Memcached
- Simpler.
- Redis has more features that matter in production (fine-grained TTL, pub/sub, optional persistence).

### No cache, reading directly from the database
- Possible for 50 req/s with good indexes.
- Cache reduces p99 latency and decouples peaks from the database. Extra margin for NFR-02.

## Consequences

Positive:
- Open-source stack, no lock-in.
- Same technology in both databases simplifies operations.
- Cache absorbs read peaks, reduces DB pressure.

Negative:
- Need for correct invalidation -> mitigated by active invalidation in the consumer + short TTL as safety net.
- Possibility of serving stale balance for up to 60s (TTL) -> acceptable given the domain.
