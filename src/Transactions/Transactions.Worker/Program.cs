using CashFlow.Transactions.Infrastructure;
using CashFlow.Transactions.Worker;
using CashFlow.Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddCashFlowObservability("Transactions.Worker");
builder.Services.AddTransactionsInfrastructure(builder.Configuration);
builder.Services.AddHostedService<OutboxPublisherService>();

var app = builder.Build();
app.UseOpenTelemetryPrometheusScrapingEndpoint();
app.MapGet("/health/live", () => Results.Ok());
app.MapGet("/", () => "Transactions.Worker (OutboxPublisher) running");
app.Run();
