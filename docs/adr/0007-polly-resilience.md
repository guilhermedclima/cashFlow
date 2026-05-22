# ADR-0007: Resilience with Polly (retry, circuit breaker, bulkhead, timeout)

- **Status:** Accepted
- **Date:** 2026-05-19

## Context

Every external call can fail: the DB may be failing over, Redis may be restarting, the broker may be slow. Without resilience policies, transient failures become hard failures.

In addition, NFR-03 allows up to 5% loss in consolidation. That authorizes tactics such as circuit breaker and graceful fallback.

## Decision

Apply **Polly** at all integration points with:

| Policy | Where | Configuration |
|---|---|---|
| Retry | DB, broker, cache calls | Exponential backoff with jitter, max 3 retries |
| Circuit Breaker | DB and cache calls in Reporting | Opens after 5 failures in 30s; half-open after 15s |
| Timeout | All external calls | 2s for cache, 5s for DB |
| Bulkhead | HTTP/DB connection pool | Isolates pools per integration |
| Fallback | Reporting when cache AND DB fail | Returns 503 with `Retry-After` |

## Alternatives

### No policies (let the exception bubble up)
- Unacceptable given NFR-02 and NFR-03.

### Manual retry implementation
- Reinvents the wheel; less tested, more bug-prone.

### `Microsoft.Extensions.Http.Resilience` (built-in .NET 8)
- Good modern alternative; in practice uses Polly under the hood.
- Decision to use Polly directly to have granular control outside HttpClient (DB, cache).

## Consequences

Positive:
- System handles transient failures without propagating the error to the client.
- Circuit breaker avoids "thundering herd" against a downed dependency.
- Polly metrics exposed for observability.

Negative:
- Configuration must be tuned per integration (one-size-fits-all doesn't work).
- Retry can amplify load if the problem is capacity rather than transient — mitigated by circuit breaker.
