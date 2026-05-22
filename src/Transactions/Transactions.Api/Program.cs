using FluentValidation;
using CashFlow.Transactions.Api.Endpoints;
using CashFlow.Transactions.Application;
using CashFlow.Transactions.Domain.Exceptions;
using CashFlow.Transactions.Infrastructure;
using CashFlow.Shared.Observability;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.AddCashFlowObservability("Transactions.Api");
builder.Services.AddTransactionsApplication();
builder.Services.AddTransactionsInfrastructure(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!, name: "postgres", tags: new[] { "ready" })
    .AddRabbitMQ(
        rabbitConnectionString: $"amqp://{builder.Configuration["RabbitMq:User"]}:{builder.Configuration["RabbitMq:Password"]}@{builder.Configuration["RabbitMq:Host"]}:5672",
        name: "rabbitmq", tags: new[] { "ready" });

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

// Lean exception middleware: DomainException -> 422, ValidationException -> 400
app.Use(async (ctx, next) =>
{
    try
    {
        await next();
    }
    catch (ValidationException vex)
    {
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        await ctx.Response.WriteAsJsonAsync(new { errors = vex.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }) });
    }
    catch (DomainException dex)
    {
        ctx.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
        await ctx.Response.WriteAsJsonAsync(new { error = dex.Message });
    }
});

// Correlation ID propagation
app.Use(async (ctx, next) =>
{
    var corr = ctx.Request.Headers["X-Correlation-Id"].FirstOrDefault() ?? Guid.NewGuid().ToString();
    ctx.Response.Headers["X-Correlation-Id"] = corr;
    using (Serilog.Context.LogContext.PushProperty("CorrelationId", corr))
        await next();
});

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready", new() { Predicate = r => r.Tags.Contains("ready") });

app.MapTransactionsEndpoints();

app.Run();

// For WebApplicationFactory in integration tests
public partial class Program { }
