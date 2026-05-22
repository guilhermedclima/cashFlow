# ADR-0009: Clean Architecture (Ports & Adapters) per service

- **Status:** Accepted
- **Date:** 2026-05-19

## Context

We need an internal organization for each service that:
- Keeps the domain free of technical details (database, broker, HTTP).
- Makes unit testing easy without heavy mocking.
- Allows swapping infrastructure technologies without rewriting the domain.
- Is familiar to the .NET community.

## Decision

Adopt **Clean Architecture** (a Hexagonal/Ports & Adapters variation) with 4 layers per service:

```
Api (presentation)
  | depends on
Application (use cases, ports)
  | depends on
Domain (entities, value objects, rules)
  ^ implemented by
Infrastructure (adapters: EF, RabbitMQ, Redis)
```

Rules:
- **Dependencies point inward.** Domain does not know Application; Application does not know Infrastructure.
- **Ports** live in `Application/Abstractions` as interfaces (`ITransactionRepository`, `IUnitOfWork`, `IEventPublisher`).
- **Adapters** live in `Infrastructure` implementing those interfaces.
- **MediatR** organizes use cases as Commands and Queries (lightweight CQRS inside the service).

## Alternatives

### Classic layered architecture (UI -> BL -> DAL)
- Familiar but usually leaks ORM details into the domain.
- Hard to test BL without instantiating DAL.

### Vertical Slice Architecture
- Each feature self-contained with Endpoint -> Handler -> Persistence in per-feature folders.
- Attractive for large services with many loosely related features.
- In the challenge scope, Clean Architecture better communicates the "why" of the layers to the reviewer.

### DDD with large aggregates
- We will keep DDD where it makes sense (Transaction is a small aggregate with rich VOs), without falling into "domain modeling theater".

## Consequences

Positive:
- Domain testable without infrastructure.
- Infrastructure swappable (e.g. swap EF for Dapper, swap RabbitMQ for Kafka via MassTransit).
- Clear separation of responsibilities communicates intent.

Negative:
- More files and projects than strictly necessary for a small system.
- Justified by the goal of demonstrating architectural capability in the challenge.
