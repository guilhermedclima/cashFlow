using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace CashFlow.Reporting.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddReportingApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        return services;
    }
}
