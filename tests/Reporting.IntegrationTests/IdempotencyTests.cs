using FluentAssertions;
using CashFlow.Reporting.Infrastructure.Persistence;
using CashFlow.Reporting.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace CashFlow.Reporting.IntegrationTests;

/// <summary>
/// Reproduces redelivery: the same transaction_id arrives twice.
/// Expected: apply once; the second call returns false without changing the balance.
/// </summary>
[Trait("Category", "Integration")]
public sealed class IdempotencyTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("reporting_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public Task InitializeAsync() => _pg.StartAsync();
    public Task DisposeAsync() => _pg.DisposeAsync().AsTask();

    [Fact]
    public async Task ApplyTransactionIdempotentAsync_ShouldDropDuplicate()
    {
        var opts = new DbContextOptionsBuilder<ReportingDbContext>()
            .UseNpgsql(_pg.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var db = new ReportingDbContext(opts);
        await db.Database.EnsureCreatedAsync();

        var repo = new DailyBalanceRepository(db);
        var transactionId = Guid.NewGuid();
        var merchant = Guid.NewGuid();
        var date = new DateOnly(2026, 5, 19);

        var first = await repo.ApplyTransactionIdempotentAsync(
            transactionId, merchant, date, 100m, "Credit", CancellationToken.None);
        first.Should().BeTrue();

        var second = await repo.ApplyTransactionIdempotentAsync(
            transactionId, merchant, date, 100m, "Credit", CancellationToken.None);
        second.Should().BeFalse(); // duplicate

        var balance = await repo.GetAsync(merchant, date, CancellationToken.None);
        balance!.TotalCredits.Should().Be(100m); // not doubled
        balance.TransactionCount.Should().Be(1);
    }
}
