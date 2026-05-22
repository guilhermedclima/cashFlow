using CashFlow.Transactions.Application.Abstractions;
using CashFlow.Transactions.Domain.Entities;
using CashFlow.Transactions.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Transactions.Infrastructure.Persistence;

public sealed class TransactionsDbContext : DbContext, IUnitOfWork
{
    public TransactionsDbContext(DbContextOptions<TransactionsDbContext> options) : base(options) { }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TransactionsDbContext).Assembly);
    }
}
