using CashFlow.Transactions.Domain.Enums;
using MediatR;

namespace CashFlow.Transactions.Application.Commands.RegisterTransaction;

public sealed record RegisterTransactionCommand(
    Guid MerchantId,
    decimal Amount,
    TransactionType Type,
    DateTime OccurredOnUtc,
    string? Description) : IRequest<RegisterTransactionResult>;

public sealed record RegisterTransactionResult(Guid TransactionId);
