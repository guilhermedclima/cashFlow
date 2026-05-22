using System.Text.Json;
using CashFlow.Reporting.Application.Abstractions;
using CashFlow.Reporting.Domain.Entities;
using StackExchange.Redis;

namespace CashFlow.Reporting.Infrastructure.Cache;

internal sealed class RedisBalanceCache : IBalanceCache
{
    private readonly IConnectionMultiplexer _redis;

    public RedisBalanceCache(IConnectionMultiplexer redis) => _redis = redis;

    private static string Key(Guid m, DateOnly d) => $"balance:{m:N}:{d:yyyy-MM-dd}";

    public async Task<DailyBalance?> GetAsync(Guid merchantId, DateOnly date, CancellationToken ct)
    {
        var val = await _redis.GetDatabase().StringGetAsync(Key(merchantId, date));
        return val.IsNullOrEmpty ? null : JsonSerializer.Deserialize<DailyBalance>(val!);
    }

    public Task SetAsync(DailyBalance balance, TimeSpan ttl, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(balance);
        return _redis.GetDatabase().StringSetAsync(Key(balance.MerchantId, balance.Date), payload, ttl);
    }

    public Task InvalidateAsync(Guid merchantId, DateOnly date, CancellationToken ct)
        => _redis.GetDatabase().KeyDeleteAsync(Key(merchantId, date));
}
