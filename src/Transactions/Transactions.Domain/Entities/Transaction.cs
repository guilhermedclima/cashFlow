using CashFlow.Transactions.Domain.Common;
using CashFlow.Transactions.Domain.Enums;
using CashFlow.Transactions.Domain.Events;
using CashFlow.Transactions.Domain.Exceptions;
using CashFlow.Transactions.Domain.ValueObjects;

namespace CashFlow.Transactions.Domain.Entities;

/// <summary>
/// Aggregate root for the Transactions bounded context.
/// Invariants:
///   - Amount is strictly positive (enforced by Money value object).
///   - OccurredOnUtc cannot be in the future.
///   - Once registered, the transaction is immutable (registration is atomic).
/// </summary>
public sealed class Transaction : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid MerchantId { get; private set; }
    public Money Amount { get; private set; } = default!;
    public TransactionType Type { get; private set; }
    public DateTime OccurredOnUtc { get; private set; }
    public string? Description { get; private set; }

    // For EF Core
    private Transaction() { }

    private Transaction(
        Guid id,
        Guid merchantId,
        Money amount,
        TransactionType type,
        DateTime occurredOnUtc,
        string? description)
    {
        Id = id;
        MerchantId = merchantId;
        Amount = amount;
        Type = type;
        OccurredOnUtc = occurredOnUtc;
        Description = description;
    }

    /// <summary>
    /// Factory enforcing all domain invariants and raising the appropriate
    /// domain event.
    /// </summary>
    public static Transaction Register(
        Guid merchantId,
        decimal amount,
        TransactionType type,
        DateTime occurredOnUtc,
        string? description = null)
    {
        if (merchantId == Guid.Empty)
            throw new DomainException("MerchantId is required.");

        if (occurredOnUtc > DateTime.UtcNow.AddMinutes(1))
            throw new DomainException("Transaction date cannot be in the future.");

        if (description is { Length: > 500 })
            throw new DomainException("Description cannot exceed 500 characters.");

        var transaction = new Transaction(
            id: Guid.NewGuid(),
            merchantId,
            amount: new Money(amount),
            type,
            occurredOnUtc,
            description);

        transaction.Raise(new TransactionRegistered(
            transaction.Id,
            transaction.MerchantId,
            transaction.Amount.Amount,
            transaction.Type,
            transaction.OccurredOnUtc,
            transaction.Description));

        return transaction;
    }
}
