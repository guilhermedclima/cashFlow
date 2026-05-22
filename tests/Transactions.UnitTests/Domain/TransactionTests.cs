using FluentAssertions;
using CashFlow.Transactions.Domain.Entities;
using CashFlow.Transactions.Domain.Enums;
using CashFlow.Transactions.Domain.Events;
using CashFlow.Transactions.Domain.Exceptions;

namespace CashFlow.Transactions.UnitTests.Domain;

[Trait("Category", "Unit")]
public sealed class TransactionTests
{
    private static readonly Guid MerchantId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Register_ShouldCreateTransaction_WhenInputIsValid()
    {
        var t = Transaction.Register(MerchantId, 150.75m, TransactionType.Credit, DateTime.UtcNow, "POS sale");

        t.Id.Should().NotBeEmpty();
        t.MerchantId.Should().Be(MerchantId);
        t.Amount.Amount.Should().Be(150.75m);
        t.Amount.Currency.Should().Be("BRL");
        t.Type.Should().Be(TransactionType.Credit);
        t.DomainEvents.Should().HaveCount(1);
        t.DomainEvents.First().Should().BeOfType<TransactionRegistered>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Register_ShouldThrow_WhenAmountNotPositive(decimal amount)
    {
        var act = () => Transaction.Register(MerchantId, amount, TransactionType.Debit, DateTime.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*positive*");
    }

    [Fact]
    public void Register_ShouldThrow_WhenMerchantIdIsEmpty()
    {
        var act = () => Transaction.Register(Guid.Empty, 100m, TransactionType.Credit, DateTime.UtcNow);
        act.Should().Throw<DomainException>().WithMessage("*MerchantId*");
    }

    [Fact]
    public void Register_ShouldThrow_WhenDateIsInTheFuture()
    {
        var act = () => Transaction.Register(MerchantId, 100m, TransactionType.Credit, DateTime.UtcNow.AddDays(1));
        act.Should().Throw<DomainException>().WithMessage("*future*");
    }

    [Fact]
    public void Register_ShouldThrow_WhenDescriptionExceeds500Chars()
    {
        var description = new string('x', 501);
        var act = () => Transaction.Register(MerchantId, 100m, TransactionType.Credit, DateTime.UtcNow, description);
        act.Should().Throw<DomainException>().WithMessage("*500*");
    }

    [Fact]
    public void DomainEvent_ShouldContainTransactionData()
    {
        var t = Transaction.Register(MerchantId, 99.99m, TransactionType.Debit, DateTime.UtcNow);
        var ev = (TransactionRegistered)t.DomainEvents.Single();

        ev.TransactionId.Should().Be(t.Id);
        ev.MerchantId.Should().Be(MerchantId);
        ev.Amount.Should().Be(99.99m);
        ev.Type.Should().Be(TransactionType.Debit);
    }
}
