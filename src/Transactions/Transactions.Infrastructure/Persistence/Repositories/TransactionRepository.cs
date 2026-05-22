using CashFlow.Transactions.Application.Abstractions;
using CashFlow.Transactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Transactions.Infrastructure.Persistence.Repositories;

internal sealed class TransactionRepository : ITransactionRepository
{
    private readonly TransactionsDbContext _db;

    public TransactionRepository(TransactionsDbContext db) => _db = db;

    public Task AddAsync(Transaction transaction, CancellationToken ct)
        => _db.Transactions.AddAsync(transaction, ct).AsTask();

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct)
        => _db.Transactions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
}
