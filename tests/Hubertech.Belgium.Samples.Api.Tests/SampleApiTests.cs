using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Hubertech.Belgium.Samples.Api.Tests;

public sealed class SampleApiTests(SampleApiTests.Factory factory) : IClassFixture<SampleApiTests.Factory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task A_valid_invoice_comes_back_without_presentation_characters()
    {
        using var response = await PostInvoiceAsync(
            """{"supplier":"BE 0202.239.951","account":"BE68 5390 0754 7034","reference":"+++123/4567/89002+++"}""");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            """{"supplier":"0202239951","account":"BE68539007547034","reference":"123456789002"}""",
            await response.Content.ReadAsStringAsync(Token));
    }

    [Theory]
    [InlineData("fr-BE,fr;q=0.9", "fr")]
    [InlineData("nl", "nl")]
    [InlineData("de-BE", "en")]
    [InlineData(null, "en")]
    public async Task An_invalid_invoice_is_a_problem_listing_every_invalid_field_in_the_language_of_the_request(string? acceptLanguage, string language)
    {
        var culture = CultureInfo.GetCultureInfo(language);

        using var response = await PostInvoiceAsync("""{"supplier":"0202.239.952","account":"NL91 ABNA 0417 1643 00"}""", acceptLanguage);
        var problem = await ReadProblemAsync(response);

        Assert.Equal("The invoice is not valid.", (string?)problem["title"]);
        Assert.Equal(
            [
                ("#/supplier", "InvalidChecksum", EnterpriseNumber.Validate("0202.239.952")!.Value.GetMessage(culture)),
                ("#/account", "InvalidCountryCode", BelgianIban.Validate("NL91 ABNA 0417 1643 00")!.Value.GetMessage(culture)),
                ("#/reference", "Empty", StructuredCommunication.Validate(string.Empty)!.Value.GetMessage(culture)),
            ],
            problem["errors"]!.AsArray().Select(error => ((string?)error!["pointer"], (string?)error["code"], (string?)error["detail"])));
    }

    [Fact]
    public async Task An_enterprise_number_is_bound_from_the_route()
    {
        using var response = await _client.GetAsync(new Uri("/enterprise-numbers/BE0202239951", UriKind.Relative), Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            """{"number":"0202239951","formatted":"0202.239.951","vat":"BE0202239951"}""",
            await response.Content.ReadAsStringAsync(Token));
    }

    [Fact]
    public async Task An_invalid_enterprise_number_in_the_route_is_a_plain_bad_request()
    {
        using var response = await _client.GetAsync(new Uri("/enterprise-numbers/0202.239.952", UriKind.Relative), Token);
        var problem = await ReadProblemAsync(response);

        Assert.Equal("Bad Request", (string?)problem["title"]);
        Assert.Null(problem["errors"]);
    }

    [Fact]
    public async Task An_invalid_enterprise_number_in_the_route_is_a_bad_request_in_development_too()
    {
        using var development = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = development.CreateClient();

        using var response = await client.GetAsync(new Uri("/enterprise-numbers/0202.239.952", UriKind.Relative), Token);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("2026-04-02", 2, "2026-04-07")]
    [InlineData("2026-01-05", -10, "2025-12-18")]
    public async Task Business_days_are_added_to_a_date(string from, int days, string expected)
    {
        using var response = await _client.GetAsync(BusinessDays(from, days), Token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            $$"""{"from":"{{from}}","days":{{days}},"date":"{{expected}}"}""",
            await response.Content.ReadAsStringAsync(Token));
    }

    [Fact]
    public async Task Moving_beyond_the_range_of_dates_is_a_problem()
    {
        using var response = await _client.GetAsync(BusinessDays("9999-12-30", 5), Token);
        var problem = await ReadProblemAsync(response);

        Assert.Equal("The date is out of range.", (string?)problem["title"]);
    }

    [Fact]
    public async Task An_invalid_date_is_a_plain_bad_request()
    {
        using var response = await _client.GetAsync(BusinessDays("2026-02-30", 1), Token);
        var problem = await ReadProblemAsync(response);

        Assert.Equal("Bad Request", (string?)problem["title"]);
    }

    private static Uri BusinessDays(string from, int days) =>
        new(string.Create(CultureInfo.InvariantCulture, $"/business-days?from={from}&days={days}"), UriKind.Relative);

    private static async Task<JsonNode> ReadProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        return JsonNode.Parse(await response.Content.ReadAsStringAsync(Token))!;
    }

    private async Task<HttpResponseMessage> PostInvoiceAsync(string json, string? acceptLanguage = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/invoices", UriKind.Relative))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        if (acceptLanguage is not null)
        {
            request.Headers.AcceptLanguage.ParseAdd(acceptLanguage);
        }

        return await _client.SendAsync(request, Token);
    }

    /// <summary>
    /// Hosts the sample in memory, in the production environment that users see.
    /// </summary>
    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Production");
    }
}
