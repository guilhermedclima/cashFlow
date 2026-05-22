using CashFlow.Transactions.Application.Abstractions;
using CashFlow.Transactions.Application.Commands.RegisterTransaction;
using CashFlow.Transactions.Domain.Enums;
using MediatR;

namespace CashFlow.Transactions.Api.Endpoints;

public static class TransactionsEndpoints
{
    public static IEndpointRouteBuilder MapTransactionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/transactions").WithTags("Transactions");

        group.MapPost("/", async (RegisterTransactionRequest body, ISender sender, CancellationToken ct) =>
        {
            var cmd = new RegisterTransactionCommand(
                body.MerchantId,
                body.Amount,
                Enum.Parse<TransactionType>(body.Type, ignoreCase: true),
                body.OccurredOnUtc ?? DateTime.UtcNow,
                body.Description);

            var result = await sender.Send(cmd, ct);
            return Results.Created($"/api/v1/transactions/{result.TransactionId}", new { id = result.TransactionId });
        })
        .WithSummary("Registers a transaction (credit or debit).")
        .Produces(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        group.MapGet("/{id:guid}", async (Guid id, ITransactionRepository repo, CancellationToken ct) =>
        {
            var t = await repo.GetByIdAsync(id, ct);
            return t is null
                ? Results.NotFound()
                : Results.Ok(new
                {
                    id = t.Id,
                    merchantId = t.MerchantId,
                    amount = t.Amount.Amount,
                    currency = t.Amount.Currency,
                    type = t.Type.ToString(),
                    occurredOnUtc = t.OccurredOnUtc,
                    description = t.Description
                });
        });

        return app;
    }

    public sealed record RegisterTransactionRequest(
        Guid MerchantId,
        decimal Amount,
        string Type,
        DateTime? OccurredOnUtc,
        string? Description);
}
