namespace CashFlow.Transactions.Infrastructure.Outbox;

/// <summary>
/// Row in the outbox_messages table. Persisted in the same transaction as the
/// aggregate. The OutboxPublisher (BackgroundService in the Worker host) polls
/// pending rows and publishes them to the broker.
/// </summary>
public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = null!;        // Assembly-qualified .NET type
    public string Payload { get; set; } = null!;     // JSON-serialized event
    public DateTime OccurredAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
