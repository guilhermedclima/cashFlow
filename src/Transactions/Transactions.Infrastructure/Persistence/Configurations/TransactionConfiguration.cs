using CashFlow.Transactions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Transactions.Infrastructure.Persistence.Configurations;

internal sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> b)
    {
        b.ToTable("transactions");

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.MerchantId).HasColumnName("merchant_id").IsRequired();

        b.OwnsOne(x => x.Amount, vo =>
        {
            vo.Property(v => v.Amount).HasColumnName("amount").HasColumnType("numeric(18,2)").IsRequired();
            vo.Property(v => v.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        });

        b.Property(x => x.Type).HasColumnName("type").HasConversion<int>().IsRequired();
        b.Property(x => x.OccurredOnUtc).HasColumnName("occurred_on_utc").IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);

        // Domain events are transient — not persisted.
        b.Ignore(x => x.DomainEvents);

        b.HasIndex(x => new { x.MerchantId, x.OccurredOnUtc })
            .HasDatabaseName("ix_transactions_merchant_date");
    }
}
