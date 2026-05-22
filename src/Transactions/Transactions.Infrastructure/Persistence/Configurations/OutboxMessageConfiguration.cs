using CashFlow.Transactions.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Transactions.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("outbox_messages");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.Type).HasColumnName("type").IsRequired();
        b.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb").IsRequired();
        b.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc").IsRequired();
        b.Property(x => x.PublishedAtUtc).HasColumnName("published_at_utc");
        b.Property(x => x.Attempts).HasColumnName("attempts").HasDefaultValue(0);
        b.Property(x => x.LastError).HasColumnName("last_error");

        // Partial index — the publisher only reads unpublished messages.
        b.HasIndex(x => x.OccurredAtUtc)
            .HasDatabaseName("ix_outbox_pending")
            .HasFilter(@"published_at_utc IS NULL");
    }
}
