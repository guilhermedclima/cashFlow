using CashFlow.Transactions.Domain.Common;

namespace CashFlow.Transactions.Application.Abstractions;

/// <summary>
/// Port for enqueueing outbox messages within the same DB transaction as the
/// aggregate. Implemented by the Infrastructure layer.
/// </summary>
public interface IOutboxWriter
{
    Task EnqueueAsync(IDomainEvent @event, CancellationToken ct);
}
