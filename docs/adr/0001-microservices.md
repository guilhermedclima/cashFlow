# ADR-0001: Adopt microservices architecture instead of a modular monolith

- **Status:** Accepted
- **Date:** 2026-05-19

## Context

The challenge imposes the following critical non-functional requirement (NFR-01):

> The transaction control service (`Transactions`) must not become unavailable if the daily consolidation system (`Reporting`) goes down.

This requires that the two services have fully independent lifecycles: process, deployment, scale, database, and failures. A modular monolith, even with good namespace separation, shares the process — if `Reporting` throws an unhandled exception, runs out of memory, or consumes all threads, `Transactions` goes down with it.

In addition, the load profiles are distinct:
- `Transactions`: write-heavy, low frequency.
- `Reporting`: read-heavy, peaks of 50 req/s (NFR-02).

## Decision

Adopt microservices architecture with:

1. Two separate services (`Transactions`, `Reporting`).
2. Independent databases (database per service).
3. Asynchronous communication via messaging (see ADR-0002).
4. Independent deployments.

## Alternatives Considered

### Modular monolith
- Lower operational complexity.
- Does not honestly satisfy NFR-01 — an unhandled failure in one module brings down the other.
- Coupled scaling.

### Modular monolith with feature flag
- Allows turning the module off.
- Does not isolate runtime failures (memory leak, deadlock).
- Comparable complexity without the benefit of real isolation.

## Consequences

Positive:
- Real failure isolation.
- Independent scaling.
- Teams can evolve the services at different paces.

Negative and mitigations:
- Operational complexity -> Docker Compose for dev, k8s on the roadmap.
- Eventual consistency -> acceptable: daily balance does not require strong consistency.
- Additional latency -> tolerable (seconds).
- Distributed debugging -> mitigated by tracing with OpenTelemetry.
