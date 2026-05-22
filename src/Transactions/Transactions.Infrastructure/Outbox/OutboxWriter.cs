using System.Text.Json;
using CashFlow.Transactions.Application.Abstractions;
using CashFlow.Transactions.Domain.Common;
using CashFlow.Transactions.Infrastructure.Persistence;

namespace CashFlow.Transactions.Infrastructure.Outbox;

internal sealed class OutboxWriter : IOutboxWriter
{
    private readonly TransactionsDbContext _db;

    public OutboxWriter(TransactionsDbContext db) => _db = db;

    public Task EnqueueAsync(IDomainEvent @event, CancellationToken ct)
    {
        var msg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = @event.GetType().AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(@event, @event.GetType()),
            OccurredAtUtc = @event.OccurredAtUtc,
            PublishedAtUtc = null,
            Attempts = 0
        };
        return _db.Outbox.AddAsync(msg, ct).AsTask();
    }
}
