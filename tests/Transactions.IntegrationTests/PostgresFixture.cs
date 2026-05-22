using Testcontainers.PostgreSql;

namespace CashFlow.Transactions.IntegrationTests;

/// <summary>
/// Spins up a real PostgreSQL container for integration tests.
/// Shared across tests via xUnit ICollectionFixture.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("transactions_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public Task InitializeAsync() => Container.StartAsync();
    public Task DisposeAsync() => Container.DisposeAsync().AsTask();
}

[CollectionDefinition("Postgres")]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture> { }
