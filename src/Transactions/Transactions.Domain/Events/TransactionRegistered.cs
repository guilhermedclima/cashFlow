using CashFlow.Transactions.Domain.Common;
using CashFlow.Transactions.Domain.Enums;

namespace CashFlow.Transactions.Domain.Events;

/// <summary>
/// Internal domain event raised when a Transaction is registered.
/// It is translated into the integration contract <c>TransactionRegisteredEvent</c>
/// when persisted to the outbox.
/// </summary>
public sealed record TransactionRegistered(
    Guid TransactionId,
    Guid MerchantId,
    decimal Amount,
    TransactionType Type,
    DateTime OccurredOnUtc,
    string? Description) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; } = DateTime.UtcNow;
}
