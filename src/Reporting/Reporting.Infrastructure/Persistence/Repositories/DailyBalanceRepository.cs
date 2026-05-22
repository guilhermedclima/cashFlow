using CashFlow.Reporting.Application.Abstractions;
using CashFlow.Reporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Reporting.Infrastructure.Persistence.Repositories;

public sealed class DailyBalanceRepository : IDailyBalanceRepository
{
    private readonly ReportingDbContext _db;

    public DailyBalanceRepository(ReportingDbContext db) => _db = db;

    public Task<DailyBalance?> GetAsync(Guid merchantId, DateOnly date, CancellationToken ct)
        => _db.DailyBalances
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.MerchantId == merchantId && x.Date == date, ct);

    public async Task<bool> ApplyTransactionIdempotentAsync(
        Guid transactionId,
        Guid merchantId,
        DateOnly date,
        decimal amount,
        string type,
        CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // Dedup: if the id is already processed, this is a duplicate delivery
        var alreadyProcessed = await _db.ProcessedTransactions
            .AnyAsync(x => x.TransactionId == transactionId, ct);

        if (alreadyProcessed)
        {
            await tx.RollbackAsync(ct);
            return false;
        }

        _db.ProcessedTransactions.Add(new ProcessedTransaction
        {
            TransactionId = transactionId,
            ProcessedAtUtc = DateTime.UtcNow
        });

        var balance = await _db.DailyBalances
            .FirstOrDefaultAsync(x => x.MerchantId == merchantId && x.Date == date, ct);

        if (balance is null)
        {
            balance = new DailyBalance { MerchantId = merchantId, Date = date };
            _db.DailyBalances.Add(balance);
        }

        balance.Apply(amount, type);

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }
}
