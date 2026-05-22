using CashFlow.Transactions.Application.Abstractions;
using CashFlow.Transactions.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CashFlow.Transactions.Application.Commands.RegisterTransaction;

/// <summary>
/// Use case: register a transaction. Persists the aggregate together with the
/// outbox entry within the SAME local DB transaction (transactional outbox —
/// avoids 2PC against the broker).
/// </summary>
public sealed class RegisterTransactionHandler : IRequestHandler<RegisterTransactionCommand, RegisterTransactionResult>
{
    private readonly ITransactionRepository _repo;
    private readonly IOutboxWriter _outbox;
    private readonly IUnitOfWork _uow;
    private readonly ILogger<RegisterTransactionHandler> _logger;

    public RegisterTransactionHandler(
        ITransactionRepository repo,
        IOutboxWriter outbox,
        IUnitOfWork uow,
        ILogger<RegisterTransactionHandler> logger)
    {
        _repo = repo;
        _outbox = outbox;
        _uow = uow;
        _logger = logger;
    }

    public async Task<RegisterTransactionResult> Handle(
        RegisterTransactionCommand cmd,
        CancellationToken ct)
    {
        var transaction = Transaction.Register(
            cmd.MerchantId,
            cmd.Amount,
            cmd.Type,
            cmd.OccurredOnUtc,
            cmd.Description);

        await _repo.AddAsync(transaction, ct);

        foreach (var ev in transaction.DomainEvents)
            await _outbox.EnqueueAsync(ev, ct);

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Transaction {TransactionId} registered for merchant {MerchantId} ({Type} {Amount})",
            transaction.Id, transaction.MerchantId, transaction.Type, transaction.Amount);

        return new RegisterTransactionResult(transaction.Id);
    }
}
