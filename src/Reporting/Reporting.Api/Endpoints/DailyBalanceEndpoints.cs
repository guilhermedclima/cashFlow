using CashFlow.Reporting.Application.Queries.GetDailyBalance;
using MediatR;

namespace CashFlow.Reporting.Api.Endpoints;

public static class DailyBalanceEndpoints
{
    public static IEndpointRouteBuilder MapDailyBalanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/daily-balance").WithTags("DailyBalance");

        group.MapGet("/{merchantId:guid}/{date}",
            async (Guid merchantId, string date, ISender sender, CancellationToken ct) =>
            {
                if (!DateOnly.TryParse(date, out var parsedDate))
                    return Results.BadRequest(new { error = "Invalid date. Use yyyy-MM-dd format." });

                var result = await sender.Send(new GetDailyBalanceQuery(merchantId, parsedDate), ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            })
            .WithSummary("Returns the consolidated daily balance for a merchant.")
            .Produces<GetDailyBalanceResult>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
