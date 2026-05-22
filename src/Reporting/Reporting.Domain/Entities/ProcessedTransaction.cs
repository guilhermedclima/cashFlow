namespace CashFlow.Reporting.Domain.Entities;

/// <summary>
/// Idempotency marker — ensures that the same TransactionRegisteredEvent is
/// never applied to the daily balance more than once.
/// </summary>
public sealed class ProcessedTransaction
{
    public Guid TransactionId { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
}
