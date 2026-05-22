namespace CashFlow.Shared.Contracts.Events;

/// <summary>
/// Integration event published by the Transactions service when a new
/// transaction is successfully registered. Consumed by the Reporting service.
///
/// IMPORTANT: this contract is the intentional shared kernel between the two
/// bounded contexts. Changes here are breaking — version the event
/// (e.g. TransactionRegisteredEventV2) instead of mutating the existing record.
/// </summary>
public sealed record TransactionRegisteredEvent(
    Guid EventId,
    Guid TransactionId,
    Guid MerchantId,
    decimal Amount,
    string Type,          // "Credit" | "Debit"
    DateTime OccurredOnUtc,
    string? Description,
    DateTime OccurredAtUtc);
