using FluentAssertions;
using CashFlow.Reporting.Domain.Entities;

namespace CashFlow.Reporting.UnitTests;

[Trait("Category", "Unit")]
public sealed class DailyBalanceTests
{
    [Fact]
    public void Apply_Credit_ShouldIncreaseTotalCredits()
    {
        var b = new DailyBalance { MerchantId = Guid.NewGuid(), Date = new DateOnly(2026, 5, 19) };
        b.Apply(100m, "Credit");
        b.TotalCredits.Should().Be(100m);
        b.TotalDebits.Should().Be(0m);
        b.Balance.Should().Be(100m);
        b.TransactionCount.Should().Be(1);
    }

    [Fact]
    public void Apply_Debit_ShouldIncreaseTotalDebits()
    {
        var b = new DailyBalance { MerchantId = Guid.NewGuid(), Date = new DateOnly(2026, 5, 19) };
        b.Apply(50m, "Debit");
        b.TotalDebits.Should().Be(50m);
        b.Balance.Should().Be(-50m);
    }

    [Fact]
    public void Apply_InvalidType_ShouldThrow()
    {
        var b = new DailyBalance();
        var act = () => b.Apply(10m, "Payment");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Apply_MultipleTransactions_ShouldAccumulate()
    {
        var b = new DailyBalance { MerchantId = Guid.NewGuid(), Date = new DateOnly(2026, 5, 19) };
        b.Apply(100m, "Credit");
        b.Apply(30m, "Debit");
        b.Apply(50m, "Credit");
        b.TotalCredits.Should().Be(150m);
        b.TotalDebits.Should().Be(30m);
        b.Balance.Should().Be(120m);
        b.TransactionCount.Should().Be(3);
    }
}
