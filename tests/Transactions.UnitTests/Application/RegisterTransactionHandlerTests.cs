using FluentAssertions;
using CashFlow.Transactions.Application.Abstractions;
using CashFlow.Transactions.Application.Commands.RegisterTransaction;
using CashFlow.Transactions.Domain.Common;
using CashFlow.Transactions.Domain.Entities;
using CashFlow.Transactions.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace CashFlow.Transactions.UnitTests.Application;

[Trait("Category", "Unit")]
public sealed class RegisterTransactionHandlerTests
{
    [Fact]
    public async Task Handler_ShouldPersistTransactionAndEnqueueOutboxEvent()
    {
        var repo = Substitute.For<ITransactionRepository>();
        var outbox = Substitute.For<IOutboxWriter>();
        var uow = Substitute.For<IUnitOfWork>();
        var handler = new RegisterTransactionHandler(repo, outbox, uow, NullLogger<RegisterTransactionHandler>.Instance);

        var cmd = new RegisterTransactionCommand(
            Guid.NewGuid(), 100m, TransactionType.Credit, DateTime.UtcNow, "test");

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.TransactionId.Should().NotBeEmpty();

        await repo.Received(1).AddAsync(Arg.Any<Transaction>(), Arg.Any<CancellationToken>());
        await outbox.Received(1).EnqueueAsync(Arg.Any<IDomainEvent>(), Arg.Any<CancellationToken>());
        await uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
