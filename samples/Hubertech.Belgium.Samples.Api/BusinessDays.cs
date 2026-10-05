using Microsoft.AspNetCore.Http.HttpResults;

namespace Hubertech.Belgium.Samples.Api;

/// <summary>
/// A date moved by a number of business days.
/// </summary>
internal sealed record BusinessDayResult(DateOnly From, int Days, DateOnly Date);

internal static class BusinessDays
{
    public static void MapBusinessDays(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/business-days", Get);

    private static Results<Ok<BusinessDayResult>, ProblemHttpResult> Get(DateOnly from, int days)
    {
        try
        {
            return TypedResults.Ok(new BusinessDayResult(from, days, BelgianCalendar.AddBusinessDays(from, days)));
        }
        catch (ArgumentOutOfRangeException)
        {
            return TypedResults.Problem(
                title: "The date is out of range.",
                detail: "Moving by this number of business days goes beyond the year 9999 or before the year 1.",
                statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
