using CashFlow.Transactions.Domain.Exceptions;

namespace CashFlow.Transactions.Domain.ValueObjects;

/// <summary>
/// Value Object representing a monetary amount paired with a currency.
/// Invariants: amount must be strictly positive; currency must be a 3-letter
/// ISO-4217 code. Immutable — use Add for arithmetic.
/// </summary>
public sealed record Money
{
    public decimal Amount { get; private init; }
    public string Currency { get; private init; }

    private Money() { Currency = "BRL"; }

    public Money(decimal amount, string currency = "BRL")
    {
        if (amount <= 0)
            throw new DomainException("Monetary amount must be strictly positive. Sign is conveyed by the transaction type.");

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new DomainException("Currency must be a 3-letter ISO-4217 code.");

        Amount = Math.Round(amount, 2, MidpointRounding.ToEven);
        Currency = currency.ToUpperInvariant();
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException($"Cannot operate on different currencies: {Currency} vs {other.Currency}.");
    }

    public override string ToString() => $"{Currency} {Amount:0.00}";
}
