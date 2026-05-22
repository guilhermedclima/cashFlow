using CashFlow.Reporting.Api.Endpoints;
using CashFlow.Reporting.Application;
using CashFlow.Reporting.Infrastructure;
using CashFlow.Shared.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddCashFlowObservability("Reporting.Api");
builder.Services.AddReportingApplication();
builder.Services.AddReportingInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!, name: "postgres", tags: new[] { "ready" })
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis", tags: new[] { "ready" });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Use(async (ctx, next) =>
{
    var corr = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    ctx.Response.Headers["X-Correlation-Id"] = corr;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", corr))
        await next();
});

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new() { Predicate = r => r.Tags.Contains("ready") });
app.MapDailyBalanceEndpoints();

app.Run();

public partial class Program { }
