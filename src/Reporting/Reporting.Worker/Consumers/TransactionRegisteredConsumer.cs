using CashFlow.Reporting.Application.Abstractions;
using CashFlow.Shared.Contracts.Events;
using MassTransit;

namespace CashFlow.Reporting.Worker.Consumers;

/// <summary>
/// Consumer of the TransactionRegisteredEvent integration event.
///
/// Idempotency:
///   The repository applies the transaction inside a DB transaction that
///   INSERTs into processed_transactions (PK = transaction_id) BEFORE the
///   UPSERT of the balance. A duplicate delivery hits the PK conflict and
///   is silently acked.
///
/// Cache:
///   After success, invalidate the cache for that day. Next read repopulates.
///
/// Failures:
///   Exceptions bubble up to MassTransit, which redelivers N times and
///   eventually routes the message to the DLQ.
/// </summary>
public sealed class TransactionRegisteredConsumer : IConsumer<TransactionRegisteredEvent>
{
    private readonly IDailyBalanceRepository _repo;
    private readonly IBalanceCache _cache;
    private readonly ILogger<TransactionRegisteredConsumer> _logger;

    public TransactionRegisteredConsumer(
        IDailyBalanceRepository repo,
        IBalanceCache cache,
        ILogger<TransactionRegisteredConsumer> logger)
    {
        _repo = repo;
        _cache = cache;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TransactionRegisteredEvent> context)
    {
        var ev = context.Message;
        var date = DateOnly.FromDateTime(ev.OccurredOnUtc);

        var applied = await _repo.ApplyTransactionIdempotentAsync(
            ev.TransactionId, ev.MerchantId, date, ev.Amount, ev.Type, context.CancellationToken);

        if (!applied)
        {
            _logger.LogInformation("Event {EventId} for transaction {TransactionId} already processed — skipping", ev.EventId, ev.TransactionId);
            return;
        }

        try
        {
            await _cache.InvalidateAsync(ev.MerchantId, date, context.CancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to invalidate cache after update — TTL will expire shortly");
        }

        _logger.LogInformation(
            "Balance updated: merchant={MerchantId} date={Date} delta={Type} {Amount}",
            ev.MerchantId, date, ev.Type, ev.Amount);
    }
}
