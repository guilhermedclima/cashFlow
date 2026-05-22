using CashFlow.Transactions.Application.Abstractions;
using CashFlow.Transactions.Infrastructure.Outbox;
using CashFlow.Transactions.Infrastructure.Persistence;
using CashFlow.Transactions.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CashFlow.Transactions.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionsInfrastructure(
        this IServiceCollection services,
        IConfiguration cfg)
    {
        services.AddDbContext<TransactionsDbContext>(opt =>
            opt.UseNpgsql(cfg.GetConnectionString("Postgres"))
               .UseSnakeCaseNamingConvention());

        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IOutboxWriter, OutboxWriter>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<TransactionsDbContext>());

        // MassTransit is configured in the Worker host (publisher loop).
        // The API never publishes directly — it always goes through the outbox.
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((ctx, bus) =>
            {
                bus.Host(cfg["RabbitMq:Host"] ?? "localhost", h =>
                {
                    h.Username(cfg["RabbitMq:User"] ?? "guest");
                    h.Password(cfg["RabbitMq:Password"] ?? "guest");
                });
            });
        });

        return services;
    }
}
