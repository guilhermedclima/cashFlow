using FluentValidation;
using CashFlow.Transactions.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CashFlow.Transactions.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddTransactionsApplication(this IServiceCollection services)
    {
        var asm = Assembly.GetExecutingAssembly();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(asm);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(asm, includeInternalTypes: true);
        return services;
    }
}
