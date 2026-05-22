using System.Diagnostics.Metrics;
using CashFlow.Reporting.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CashFlow.Reporting.Application.Queries.GetDailyBalance;

/// <summary>
/// Read handler. Cache-aside: try cache → fallback to DB → repopulate cache.
/// Cache failures do not propagate (graceful degradation — NFR-03).
/// </summary>
public sealed class GetDailyBalanceHandler : IRequestHandler<GetDailyBalanceQuery, GetDailyBalanceResult?>
{
    private static readonly Meter _meter = new("CashFlow.Reporting");
    private static readonly Counter<long> _cacheHits = _meter.CreateCounter<long>("balance_cache_hits_total");
    private static readonly Counter<long> _cacheMisses = _meter.CreateCounter<long>("balance_cache_misses_total");

    private readonly IDailyBalanceRepository _repo;
    private readonly IBalanceCache _cache;
    private readonly ILogger<GetDailyBalanceHandler> _logger;
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public GetDailyBalanceHandler(IDailyBalanceRepository repo, IBalanceCache cache, ILogger<GetDailyBalanceHandler> logger)
    {
        _repo = repo;
        _cache = cache;
        _logger = logger;
    }

    public async Task<GetDailyBalanceResult?> Handle(GetDailyBalanceQuery q, CancellationToken ct)
    {
        try
        {
            var cached = await _cache.GetAsync(q.MerchantId, q.Date, ct);
            if (cached is not null)
            {
                _cacheHits.Add(1);
                return Map(cached);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache lookup failed — falling back to DB");
        }

        _cacheMisses.Add(1);

        var balance = await _repo.GetAsync(q.MerchantId, q.Date, ct);
        if (balance is null) return null;

        try
        {
            await _cache.SetAsync(balance, Ttl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache population failed — continuing");
        }

        return Map(balance);
    }

    private static GetDailyBalanceResult Map(CashFlow.Reporting.Domain.Entities.DailyBalance b) =>
        new(b.MerchantId, b.Date, b.TotalCredits, b.TotalDebits, b.Balance, b.TransactionCount, b.UpdatedAtUtc);
}
