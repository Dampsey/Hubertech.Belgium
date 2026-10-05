using Microsoft.AspNetCore.Http.HttpResults;

namespace Hubertech.Belgium.Samples.Api;

/// <summary>
/// The body of <c>POST /invoices</c>, as a user typed it. The members are strings so that every
/// invalid field is reported, with its reason, rather than the first one the deserializer meets.
/// </summary>
internal sealed record InvoiceRequest(string? Supplier, string? Account, string? Reference);

/// <summary>
/// An invoice whose identifiers are valid. They are written in JSON without presentation characters.
/// </summary>
internal sealed record Invoice(EnterpriseNumber Supplier, BelgianIban Account, StructuredCommunication Reference);

/// <summary>
/// One invalid field, as an item of the <c>errors</c> member of a problem details object: the shape
/// suggested by RFC 9457, section 3, with the stable code of the error added.
/// </summary>
/// <param name="Pointer">The JSON Pointer to the field in the request body, for example <c>#/supplier</c>.</param>
/// <param name="Code">The stable error code, for example <c>InvalidChecksum</c>.</param>
/// <param name="Detail">The message in the language of the request.</param>
internal sealed record FieldError(string Pointer, string Code, string Detail)
{
    public static FieldError For(string member, BelgianValidationError error) =>
        new($"#/{member}", error.Code.ToString(), error.Message);
}

internal static class Invoices
{
    public static void MapInvoices(this IEndpointRouteBuilder endpoints) =>
        endpoints.MapPost("/invoices", Create);

    private static Results<Ok<Invoice>, ProblemHttpResult> Create(InvoiceRequest request)
    {
        var errors = new List<FieldError>();

        if (!EnterpriseNumber.TryParse(request.Supplier, out var supplier, out var error))
        {
            errors.Add(FieldError.For("supplier", error));
        }

        if (!BelgianIban.TryParse(request.Account, out var account, out error))
        {
            errors.Add(FieldError.For("account", error));
        }

        if (!StructuredCommunication.TryParse(request.Reference, out var reference, out error))
        {
            errors.Add(FieldError.For("reference", error));
        }

        if (errors.Count > 0)
        {
            return TypedResults.Problem(
                title: "The invoice is not valid.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["errors"] = errors });
        }

        // Nothing is stored: the sample answers with the invoice as it understood it.
        return TypedResults.Ok(new Invoice(supplier, account, reference));
    }
}
