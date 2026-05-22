using CashFlow.Reporting.Application.Abstractions;
using CashFlow.Reporting.Infrastructure.Cache;
using CashFlow.Reporting.Infrastructure.Persistence;
using CashFlow.Reporting.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace CashFlow.Reporting.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingInfrastructure(
        this IServiceCollection services,
        IConfiguration cfg)
    {
        services.AddDbContext<ReportingDbContext>(opt =>
            opt.UseNpgsql(cfg.GetConnectionString("Postgres"))
               .UseSnakeCaseNamingConvention());

        services.AddScoped<IDailyBalanceRepository, DailyBalanceRepository>();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(cfg.GetConnectionString("Redis") ?? "localhost:6379"));
        services.AddSingleton<IBalanceCache, RedisBalanceCache>();

        return services;
    }
}
