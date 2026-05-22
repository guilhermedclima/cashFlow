using CashFlow.Reporting.Domain.Entities;

namespace CashFlow.Reporting.Application.Abstractions;

public interface IDailyBalanceRepository
{
    Task<DailyBalance?> GetAsync(Guid merchantId, DateOnly date, CancellationToken ct);

    /// <summary>
    /// Atomic UPSERT of the daily balance with idempotency check.
    /// Returns true if the event was applied; false if it was rejected as a duplicate.
    /// </summary>
    Task<bool> ApplyTransactionIdempotentAsync(
        Guid transactionId,
        Guid merchantId,
        DateOnly date,
        decimal amount,
        string type,
        CancellationToken ct);
}
