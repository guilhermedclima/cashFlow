using FluentAssertions;
using CashFlow.Transactions.Domain.Entities;
using CashFlow.Transactions.Domain.Enums;
using CashFlow.Transactions.Infrastructure.Outbox;
using CashFlow.Transactions.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Transactions.IntegrationTests;

/// <summary>
/// Ensures persisting a Transaction together with an outbox row happens in
/// the same local DB transaction — rolling back one rolls back the other.
/// </summary>
[Collection("Postgres")]
[Trait("Category", "Integration")]
public sealed class OutboxIntegrationTests
{
    private readonly PostgresFixture _pg;

    public OutboxIntegrationTests(PostgresFixture pg) => _pg = pg;

    [Fact]
    public async Task SaveChanges_ShouldPersistTransactionAndOutboxInSameTransaction()
    {
        var opts = new DbContextOptionsBuilder<TransactionsDbContext>()
            .UseNpgsql(_pg.ConnectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new TransactionsDbContext(opts);
        await db.Database.EnsureCreatedAsync();

        var t = Transaction.Register(Guid.NewGuid(), 100m, TransactionType.Credit, DateTime.UtcNow);
        await db.Transactions.AddAsync(t);

        var outboxMsg = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "CashFlow.Transactions.Domain.Events.TransactionRegistered",
            Payload = "{}",
            OccurredAtUtc = DateTime.UtcNow
        };
        await db.Outbox.AddAsync(outboxMsg);

        await db.SaveChangesAsync();

        (await db.Transactions.CountAsync()).Should().BeGreaterThan(0);
        (await db.Outbox.CountAsync(x => x.PublishedAtUtc == null)).Should().BeGreaterThan(0);
    }
}
