using Hubertech.Belgium.Samples.Api;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, SampleJsonContext.Default));

// Errors are problem details (RFC 9457), including the plain 400 of a value that cannot be bound.
builder.Services.AddProblemDetails();

// Validation messages follow Accept-Language: English by default, French or Dutch on request.
builder.Services.AddRequestLocalization(options =>
{
    string[] cultures = ["en", "fr", "nl"];
    options.SetDefaultCulture(cultures[0])
        .AddSupportedCultures(cultures)
        .AddSupportedUICultures(cultures);
});

var app = builder.Build();

// In development, a value that cannot be bound throws BadHttpRequestException: keep its 400.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError,
});
app.UseStatusCodePages();
app.UseRequestLocalization();

app.MapInvoices();
app.MapEnterpriseNumbers();
app.MapBusinessDays();

app.Run();
