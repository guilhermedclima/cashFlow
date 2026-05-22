using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace CashFlow.Shared.Observability;

/// <summary>
/// Extensions that configure structured logging (Serilog), distributed tracing
/// and metrics (OpenTelemetry) for all CashFlow services.
/// Usage: builder.AddCashFlowObservability("Transactions.Api");
/// </summary>
public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddCashFlowObservability(
        this WebApplicationBuilder builder,
        string serviceName)
    {
        var otlpEndpoint = builder.Configuration["Otel:OtlpEndpoint"] ?? "http://localhost:4317";
        var seqUrl = builder.Configuration["Serilog:Seq:ServerUrl"] ?? "http://localhost:5341";

        builder.Host.UseSerilog((ctx, lc) => lc
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter())
            .WriteTo.Seq(seqUrl));

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(serviceName))
            .WithTracing(t => t
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource("MassTransit")
                .AddSource("Npgsql")
                .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)))
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("CashFlow.*")
                .AddPrometheusExporter());

        return builder;
    }
}
