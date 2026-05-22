using System.Diagnostics.Metrics;
using System.Text.Json;
using CashFlow.Transactions.Domain.Events;
using CashFlow.Transactions.Infrastructure.Outbox;
using CashFlow.Transactions.Infrastructure.Persistence;
using CashFlow.Shared.Contracts.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

namespace CashFlow.Transactions.Worker;

/// <summary>
/// BackgroundService implementing the publisher side of the Outbox pattern.
///
/// Loop:
///  1. SELECT N rows where published_at_utc IS NULL ORDER BY occurred_at_utc
///  2. For each: deserialize the domain event → map to integration contract →
///     publish via MassTransit → mark published_at_utc = NOW
///  3. On failure: increment attempts and record last_error. After N attempts
///     the row is considered "poison" and requires manual inspection
///     (could be moved to outbox_dead_messages — see roadmap).
/// </summary>
public sealed class OutboxPublisherService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<OutboxPublisherService> _logger;
    private readonly int _pollIntervalMs;
    private readonly int _batchSize;
    private readonly int _maxAttempts;
    private readonly AsyncRetryPolicy _retryPolicy;

    private static readonly Meter _meter = new("CashFlow.Transactions.Outbox");
    private static readonly Counter<long> _publishedCounter = _meter.CreateCounter<long>("outbox_published_total");
    private static readonly Counter<long> _failedCounter = _meter.CreateCounter<long>("outbox_failed_total");

    public OutboxPublisherService(
        IServiceProvider sp,
        IConfiguration cfg,
        ILogger<OutboxPublisherService> logger)
    {
        _sp = sp;
        _logger = logger;
        _pollIntervalMs = cfg.GetValue("Outbox:PollIntervalMs", 500);
        _batchSize = cfg.GetValue("Outbox:BatchSize", 50);
        _maxAttempts = cfg.GetValue("Outbox:MaxAttempts", 10);

        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * Math.Pow(2, attempt)),
                onRetry: (ex, ts, attempt, _) => _logger.LogWarning(ex, "Retry {Attempt} publishing outbox", attempt));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxPublisher started (poll={Ms}ms, batch={Batch})", _pollIntervalMs, _batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failure in OutboxPublisher cycle");
            }

            await Task.Delay(_pollIntervalMs, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TransactionsDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pending = await db.Outbox
            .Where(x => x.PublishedAtUtc == null && x.Attempts < _maxAttempts)
            .OrderBy(x => x.OccurredAtUtc)
            .Take(_batchSize)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        foreach (var msg in pending)
        {
            try
            {
                var contract = MapToContract(msg);
                await _retryPolicy.ExecuteAsync(token => publisher.Publish(contract, token), ct);

                msg.PublishedAtUtc = DateTime.UtcNow;
                _publishedCounter.Add(1);
            }
            catch (Exception ex)
            {
                msg.Attempts++;
                msg.LastError = ex.Message;
                _failedCounter.Add(1);
                _logger.LogError(ex, "Permanent failure publishing outbox {Id} (attempt {N})", msg.Id, msg.Attempts);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Maps the persisted domain event to the integration contract (shared kernel),
    /// isolating consumers from the producer's internal domain model.
    /// </summary>
    private static TransactionRegisteredEvent MapToContract(OutboxMessage msg)
    {
        var type = Type.GetType(msg.Type)
            ?? throw new InvalidOperationException($"Cannot resolve event type: {msg.Type}");

        if (type == typeof(TransactionRegistered))
        {
            var ev = JsonSerializer.Deserialize<TransactionRegistered>(msg.Payload)!;
            return new TransactionRegisteredEvent(
                EventId: ev.EventId,
                TransactionId: ev.TransactionId,
                MerchantId: ev.MerchantId,
                Amount: ev.Amount,
                Type: ev.Type.ToString(),
                OccurredOnUtc: ev.OccurredOnUtc,
                Description: ev.Description,
                OccurredAtUtc: ev.OccurredAtUtc);
        }

        throw new InvalidOperationException($"Missing mapping for event type {type.FullName}");
    }
}
