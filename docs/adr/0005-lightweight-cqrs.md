# ADR-0005: Lightweight CQRS (no Event Sourcing)

- **Status:** Accepted
- **Date:** 2026-05-19

## Context

The write and read models are different:
- **Write:** individual `Transaction` (debit/credit) with domain rules (`Amount` positive, `OccurredOnUtc` not in the future, etc.).
- **Read:** `DailyBalance` aggregated per day (credits - debits) — report model.

Applying the same model to both creates friction: reads would have to aggregate per request (expensive at volume) or maintain a denormalized field on writes (coupling).

## Decision

Adopt **lightweight CQRS**:

- Rich write model in the **Transactions** context with `Transaction` aggregate and `Money` value object.
- Denormalized/materialized read model in the **Reporting** context, updated via event consumption (`DailyBalance`).
- Each context has its own database and schema.
- No pure Event Sourcing (we will not persist the event stream as the source of truth for the aggregate).

## Alternatives

### Shared single model
- Coupling between contexts.
- Aggregated reads costly at runtime.
- Rejected.

### CQRS + pure Event Sourcing
- Aggregate rebuilt from event stream.
- Complete temporal audit, replay possible.
- High adoption cost: event schema versioning, snapshotting, projections.
- YAGNI for the challenge. Current requirements do not ask for replay nor complete temporal audit.
- Documented as roadmap.

## Consequences

Positive:
- Clear separation of responsibilities.
- `Reporting` has predictable performance (aggregated query already materialized).
- Allows the models to evolve independently.

Negative:
- Eventual consistency between write and read — assumed and tolerated by the domain.
- More code (two models), but the extra complexity is justified by the real difference between the two uses.
