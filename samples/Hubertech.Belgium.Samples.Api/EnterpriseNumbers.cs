using Microsoft.AspNetCore.Http.HttpResults;

namespace Hubertech.Belgium.Samples.Api;

/// <summary>
/// The forms of an enterprise number.
/// </summary>
internal sealed record EnterpriseNumberForms(EnterpriseNumber Number, string Formatted, string Vat);

internal static class EnterpriseNumbers
{
    /// <summary>
    /// Binds an enterprise number straight from the route, through <see cref="IParsable{TSelf}"/>.
    /// It takes no code, but an invalid number gets a plain 400 without its reason: to report the
    /// reason, take a string and call the <c>TryParse</c> overload that returns the error, as
    /// <c>POST /invoices</c> does.
    /// </summary>
    public static void MapEnterpriseNumbers(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapGet("/enterprise-numbers/{number}", Get);

    private static Ok<EnterpriseNumberForms> Get(EnterpriseNumber number) =>
        TypedResults.Ok(new EnterpriseNumberForms(number, number.ToString(), number.ToString("V")));
}
