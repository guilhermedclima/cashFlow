using CashFlow.Reporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CashFlow.Reporting.Infrastructure.Persistence;

public sealed class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options) : base(options) { }

    public DbSet<DailyBalance> DailyBalances => Set<DailyBalance>();
    public DbSet<ProcessedTransaction> ProcessedTransactions => Set<ProcessedTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReportingDbContext).Assembly);
    }
}
