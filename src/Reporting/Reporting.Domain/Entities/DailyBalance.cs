namespace CashFlow.Reporting.Domain.Entities;

/// <summary>
/// Materialized read model. Not a full DDD aggregate — it is a projection
/// updated by consuming Transaction events. No business invariants beyond
/// arithmetic correctness.
/// </summary>
public sealed class DailyBalance
{
    public Guid MerchantId { get; set; }
    public DateOnly Date { get; set; }
    public decimal TotalCredits { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal Balance => TotalCredits - TotalDebits;
    public int TransactionCount { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public void Apply(decimal amount, string type)
    {
        if (type.Equals("Credit", StringComparison.OrdinalIgnoreCase))
            TotalCredits += amount;
        else if (type.Equals("Debit", StringComparison.OrdinalIgnoreCase))
            TotalDebits += amount;
        else
            throw new ArgumentException($"Invalid transaction type: {type}", nameof(type));

        TransactionCount++;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
