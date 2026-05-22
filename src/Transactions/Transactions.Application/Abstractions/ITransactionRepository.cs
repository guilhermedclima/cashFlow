using CashFlow.Transactions.Domain.Entities;

namespace CashFlow.Transactions.Application.Abstractions;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken ct);
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct);
}
