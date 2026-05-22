using CashFlow.Reporting.Domain.Entities;

namespace CashFlow.Reporting.Application.Abstractions;

public interface IBalanceCache
{
    Task<DailyBalance?> GetAsync(Guid merchantId, DateOnly date, CancellationToken ct);
    Task SetAsync(DailyBalance balance, TimeSpan ttl, CancellationToken ct);
    Task InvalidateAsync(Guid merchantId, DateOnly date, CancellationToken ct);
}
