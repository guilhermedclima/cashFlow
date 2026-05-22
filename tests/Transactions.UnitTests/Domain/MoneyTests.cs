using FluentAssertions;
using CashFlow.Transactions.Domain.Exceptions;
using CashFlow.Transactions.Domain.ValueObjects;

namespace CashFlow.Transactions.UnitTests.Domain;

[Trait("Category", "Unit")]
public sealed class MoneyTests
{
    [Fact]
    public void Constructor_ShouldRoundToTwoDecimals()
    {
        var m = new Money(10.125m);
        m.Amount.Should().Be(10.12m); // banker's rounding
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenAmountIsZero()
    {
        var act = () => new Money(0m);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Add_ShouldThrow_ForDifferentCurrencies()
    {
        var brl = new Money(100m, "BRL");
        var usd = new Money(100m, "USD");
        var act = () => brl.Add(usd);
        act.Should().Throw<DomainException>().WithMessage("*different currencies*");
    }

    [Fact]
    public void Add_ShouldAccumulate_WhenSameCurrency()
    {
        var a = new Money(10.50m);
        var b = new Money(20.25m);
        a.Add(b).Amount.Should().Be(30.75m);
    }

    [Fact]
    public void Constructor_ShouldThrow_ForInvalidCurrency()
    {
        var act = () => new Money(100m, "REAL");
        act.Should().Throw<DomainException>().WithMessage("*ISO-4217*");
    }
}
