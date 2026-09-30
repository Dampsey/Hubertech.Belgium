using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Hubertech.Belgium.Tests;

public sealed class BelgianIbanTests
{
    private static readonly BelgianIban Sample = BelgianIban.Parse("BE68539007547034");

    [Theory]
    [InlineData("BE68539007547034")]
    [InlineData("BE68 5390 0754 7034")]
    [InlineData("be68 5390 0754 7034")]
    [InlineData("  BE68-5390-0754-7034  ")]
    [InlineData("BE68 539-0075470-34")]
    [InlineData("BE68 5390 0754 7034")]
    public void Accepts_the_usual_ways_of_writing_an_iban(string input)
    {
        Assert.Equal("BE68 5390 0754 7034", BelgianIban.Parse(input).ToString());
    }

    [Theory]
    [InlineData("BE32 123-4567890-02")]
    [InlineData("BE31435411161155")]
    [InlineData("BE 48 3200 7018 4927")]
    [InlineData("BE83138811735115")]
    public void Accepts_valid_ibans(string input)
    {
        Assert.True(BelgianIban.TryParse(input, out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" - . ")]
    public void Rejects_empty_input(string? input)
    {
        AssertError(input, BelgianErrorCode.Empty);
    }

    [Theory]
    [InlineData("GR1601101050000010547023795")]
    [InlineData("FR76 3000 6000 0112 3456 7890 189")]
    [InlineData("539007547034")]
    [InlineData("68 5390 0754 7034")]
    public void Rejects_an_iban_that_is_not_belgian(string input)
    {
        var error = AssertError(input, BelgianErrorCode.InvalidCountryCode);

        Assert.Equal("Seuls les IBAN belges sont acceptés (commençant par BE).", error.GetMessage(CultureInfo.GetCultureInfo("fr-BE")));
    }

    [Theory]
    [InlineData("BE68 5390 0754 703X", 18, 'X')]
    [InlineData("IBAN BE68 5390 0754 7034", 0, 'I')]
    [InlineData("BE68/5390/0754/7034", 4, '/')]
    [InlineData("#BE68539007547034", 0, '#')]
    public void Rejects_an_invalid_character_and_gives_its_position(string input, int position, char character)
    {
        var error = AssertError(input, BelgianErrorCode.InvalidCharacter);

        Assert.Equal(position, error.Position);
        Assert.Contains($"'{character}'", error.GetMessage(CultureInfo.GetCultureInfo("en")), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("BE")]
    [InlineData("BE68 5390 0754 703")]
    [InlineData("BE68 5390 0754 7034 1")]
    public void Rejects_an_iban_without_fourteen_digits_after_the_country_code(string input)
    {
        AssertError(input, BelgianErrorCode.InvalidLength);
    }

    [Fact]
    public void Rejects_wrong_iban_check_digits_and_exposes_the_expected_ones()
    {
        var error = AssertError("BE68539007547035", BelgianErrorCode.InvalidChecksum);

        Assert.Equal("41", error.Expected);
    }

    [Fact]
    public void Rejects_a_wrong_belgian_account_number_even_when_the_iban_check_digits_match()
    {
        // BE41091811735141 passes the ISO 7064 check, but 0918117351 mod 97 is 32, not 41.
        var error = AssertError("BE41091811735141", BelgianErrorCode.InvalidChecksum);

        Assert.Equal("32", error.Expected);
    }

    [Theory]
    [InlineData("", BelgianErrorCode.Empty)]
    [InlineData("BE68 5390 0754 703X", BelgianErrorCode.InvalidCharacter)]
    [InlineData("BE68", BelgianErrorCode.InvalidLength)]
    [InlineData("NL91ABNA0417164300", BelgianErrorCode.InvalidCountryCode)]
    [InlineData("BE68539007547035", BelgianErrorCode.InvalidChecksum)]
    public void Every_error_has_a_message_in_every_language(string input, BelgianErrorCode code)
    {
        var error = AssertError(input, code);

        foreach (string language in new[] { "en", "fr", "nl" })
        {
            Assert.NotEmpty(error.GetMessage(CultureInfo.GetCultureInfo(language)));
        }
    }

    [Theory]
    [InlineData(null, "BE68 5390 0754 7034")]
    [InlineData("", "BE68 5390 0754 7034")]
    [InlineData("P", "BE68 5390 0754 7034")]
    [InlineData("p", "BE68 5390 0754 7034")]
    [InlineData("E", "BE68539007547034")]
    [InlineData("e", "BE68539007547034")]
    public void Formats_in_the_requested_format(string? format, string expected)
    {
        Assert.Equal(expected, Sample.ToString(format));
    }

    [Theory]
    [InlineData("D")]
    [InlineData("N")]
    [InlineData("PE")]
    public void Rejects_an_unknown_format(string format)
    {
        Assert.Throws<FormatException>(() => Sample.ToString(format));
    }

    [Fact]
    public void Exposes_the_bank_code_and_the_belgian_account_number()
    {
        Assert.Equal("539", Sample.BankCode);
        Assert.Equal("539-0075470-34", Sample.AccountNumber);
        Assert.Equal("001", BelgianIban.Parse("BE48 0011 2345 6727").BankCode);
    }

    [Fact]
    public void Formats_through_the_standard_interfaces()
    {
        Assert.Equal("BE68539007547034", ((IFormattable)Sample).ToString("E", CultureInfo.InvariantCulture));
        Assert.Equal("BE68 5390 0754 7034", string.Create(CultureInfo.InvariantCulture, $"{Sample}"));
    }

    [Fact]
    public void TryFormat_reports_a_destination_that_is_too_small()
    {
        Span<char> destination = stackalloc char[18];

        Assert.False(Sample.TryFormat(destination, out int charsWritten, "P"));
        Assert.Equal(0, charsWritten);

        Assert.True(Sample.TryFormat(destination, out charsWritten, "E"));
        Assert.Equal("BE68539007547034", destination[..charsWritten].ToString());
    }

    [Fact]
    public void Default_value_is_empty_and_formats_as_an_empty_string()
    {
        BelgianIban iban = default;

        Assert.True(iban.IsEmpty);
        Assert.Equal(string.Empty, iban.ToString());
        Assert.Equal(string.Empty, iban.ToString("E"));
        Assert.Equal(string.Empty, iban.BankCode);
        Assert.Equal(string.Empty, iban.AccountNumber);
        Assert.False(Sample.IsEmpty);
    }

    [Fact]
    public void Ibans_with_the_same_digits_are_equal_whatever_the_input_form()
    {
        var other = BelgianIban.Parse("be68 539-0075470-34");

        Assert.Equal(Sample, other);
        Assert.True(Sample == other);
        Assert.False(Sample != other);
        Assert.Equal(Sample.GetHashCode(), other.GetHashCode());
        Assert.NotEqual(Sample, BelgianIban.Parse("BE31435411161155"));
        Assert.False(Sample.Equals("BE68539007547034"));
    }

    [Fact]
    public void Parse_throws_a_format_exception_carrying_the_error()
    {
        var exception = Assert.Throws<BelgianFormatException>(() => BelgianIban.Parse("NL91ABNA0417164300"));

        Assert.Equal(BelgianErrorCode.InvalidCountryCode, exception.Error.Code);
        Assert.Equal(nameof(BelgianIban), exception.Error.TypeName);
    }

    [Fact]
    public void Parse_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => BelgianIban.Parse(null!));
    }

    [Fact]
    public void Validate_returns_null_for_a_valid_iban()
    {
        Assert.Null(BelgianIban.Validate("BE68 5390 0754 7034"));
        Assert.Null(BelgianIban.Validate("BE68539007547034".AsSpan()));
    }

    [Fact]
    public void Implements_the_generic_parsing_contracts()
    {
        Assert.Equal(Sample, ParseAs<BelgianIban>("BE68 5390 0754 7034"));
        Assert.True(TryParseAs<BelgianIban>("BE68539007547034", out var iban));
        Assert.Equal(Sample, iban);
        Assert.False(TryParseAs<BelgianIban>("BE68539007547035", out _));
        Assert.Throws<BelgianFormatException>(() => ParseAs<BelgianIban>("BE68539007547035"));
    }

    private static T ParseAs<T>(string s)
        where T : IParsable<T> => T.Parse(s, CultureInfo.InvariantCulture);

    private static bool TryParseAs<T>(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out T result)
        where T : ISpanParsable<T> => T.TryParse(s, CultureInfo.InvariantCulture, out result);

    private static BelgianValidationError AssertError(string? input, BelgianErrorCode code)
    {
        Assert.False(BelgianIban.TryParse(input, out var iban, out var error));
        Assert.True(iban.IsEmpty);
        Assert.Equal(code, error.Code);
        Assert.Equal(nameof(BelgianIban), error.TypeName);
        Assert.Equal(error, BelgianIban.Validate(input));
        Assert.Equal(error, BelgianIban.Validate(input.AsSpan()));

        return error;
    }
}
