namespace CashFlow.Transactions.Application.Abstractions;

/// <summary>
/// Marks the transactional boundary of a use case. SaveChanges persists
/// the aggregate together with any outbox entries in the same local transaction.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}
