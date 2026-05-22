using CashFlow.Reporting.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CashFlow.Reporting.Infrastructure.Persistence.Configurations;

internal sealed class DailyBalanceConfiguration : IEntityTypeConfiguration<DailyBalance>
{
    public void Configure(EntityTypeBuilder<DailyBalance> b)
    {
        b.ToTable("daily_balances");
        b.HasKey(x => new { x.MerchantId, x.Date });

        b.Property(x => x.MerchantId).HasColumnName("merchant_id");
        b.Property(x => x.Date).HasColumnName("date");
        b.Property(x => x.TotalCredits).HasColumnName("total_credits").HasColumnType("numeric(18,2)");
        b.Property(x => x.TotalDebits).HasColumnName("total_debits").HasColumnType("numeric(18,2)");
        b.Property(x => x.TransactionCount).HasColumnName("transaction_count");
        b.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        b.Ignore(x => x.Balance); // computed at read time
    }
}
