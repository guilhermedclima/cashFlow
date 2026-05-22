using CashFlow.Reporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Reporting.Infrastructure.Persistence.Configurations;

internal sealed class ProcessedTransactionConfiguration : IEntityTypeConfiguration<ProcessedTransaction>
{
    public void Configure(EntityTypeBuilder<ProcessedTransaction> b)
    {
        b.ToTable("processed_transactions");
        b.HasKey(x => x.TransactionId);
        b.Property(x => x.TransactionId).HasColumnName("transaction_id");
        b.Property(x => x.ProcessedAtUtc).HasColumnName("processed_at_utc");
    }
}
