using MediatR;

namespace CashFlow.Reporting.Application.Queries.GetDailyBalance;

public sealed record GetDailyBalanceQuery(Guid MerchantId, DateOnly Date)
    : IRequest<GetDailyBalanceResult?>;

public sealed record GetDailyBalanceResult(
    Guid MerchantId,
    DateOnly Date,
    decimal TotalCredits,
    decimal TotalDebits,
    decimal Balance,
    int TransactionCount,
    DateTime UpdatedAtUtc);
