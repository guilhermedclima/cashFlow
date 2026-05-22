using CashFlow.Reporting.Infrastructure;
using CashFlow.Reporting.Worker.Consumers;
using CashFlow.Shared.Observability;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.AddCashFlowObservability("Reporting.Worker");
builder.Services.AddReportingInfrastructure(builder.Configuration);

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TransactionRegisteredConsumer>();

    x.UsingRabbitMq((ctx, bus) =>
    {
        bus.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", h =>
        {
            h.Username(builder.Configuration["RabbitMq:User"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });

        bus.ReceiveEndpoint("reporting.transaction-registered", e =>
        {
            // Resilience: exponential retry + redelivery; DLQ via _error queue
            e.UseMessageRetry(r => r.Exponential(5,
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));

            e.ConfigureConsumer<TransactionRegisteredConsumer>(ctx);
        });
    });
});

var app = builder.Build();
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.MapGet("/health/live", () => Results.Ok());
app.MapGet("/", () => "Reporting.Worker (Consumer) running");
app.Run();
