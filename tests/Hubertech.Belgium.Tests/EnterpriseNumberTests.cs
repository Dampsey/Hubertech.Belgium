using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Hubertech.Belgium.Tests;

public sealed class EnterpriseNumberTests
{
    private static readonly EnterpriseNumber Sample = EnterpriseNumber.Parse("0202.239.951");

    [Theory]
    [InlineData("0202.239.951")]
    [InlineData("0202239951")]
    [InlineData("BE0202239951")]
    [InlineData("be 0202 239 951")]
    [InlineData("Be0202.239.951")]
    [InlineData("BE 0202-239-951")]
    [InlineData("  0202 . 239 . 951  ")]
    [InlineData("0202 239 951")]
    [InlineData("202239951")]
    [InlineData("BE 202.239.951")]
    public void Accepts_the_usual_ways_of_writing_a_number(string input)
    {
        Assert.Equal("0202.239.951", EnterpriseNumber.Parse(input).ToString());
    }

    [Theory]
    [InlineData("0403.170.701")]
    [InlineData("0417.497.106")]
    [InlineData("1234.567.894")]
    public void Accepts_numbers_starting_with_0_or_1(string input)
    {
        Assert.True(EnterpriseNumber.TryParse(input, out var number));
        Assert.Equal(input, number.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" . - ")]
    public void Rejects_empty_input(string? input)
    {
        AssertError(input, BelgianErrorCode.Empty);
    }

    [Theory]
    [InlineData("0202.239.95X", 11, 'X')]
    [InlineData("O202239951", 0, 'O')]
    [InlineData("TVA BE0202239951", 0, 'T')]
    [InlineData("0202/239/951", 4, '/')]
    [InlineData("0202239951BE", 10, 'B')]
    public void Rejects_an_invalid_character_and_gives_its_position(string input, int position, char character)
    {
        var error = AssertError(input, BelgianErrorCode.InvalidCharacter);

        Assert.Equal(position, error.Position);
        Assert.Contains($"'{character}'", error.GetMessage(CultureInfo.GetCultureInfo("en")), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("FR0202239951")]
    [InlineData("nl 0202.239.951")]
    public void Rejects_a_foreign_country_code(string input)
    {
        AssertError(input, BelgianErrorCode.InvalidCountryCode);
    }

    [Theory]
    [InlineData("BE")]
    [InlineData("0202.239.9")]
    [InlineData("0202.239.9511")]
    public void Rejects_a_number_without_nine_or_ten_digits(string input)
    {
        AssertError(input, BelgianErrorCode.InvalidLength);
    }

    [Fact]
    public void Reads_a_ten_digit_number_missing_a_digit_as_a_legacy_nine_digit_number()
    {
        // Nine digits are a legacy number: the check digits, not the length, reveal the omission.
        AssertError("0202.239.95", BelgianErrorCode.InvalidChecksum);
    }

    [Theory]
    [InlineData("2202.239.951")]
    [InlineData("9999.999.999")]
    public void Rejects_a_number_not_starting_with_0_or_1(string input)
    {
        AssertError(input, BelgianErrorCode.InvalidFirstDigit);
    }

    [Fact]
    public void Rejects_wrong_check_digits_and_exposes_the_expected_ones()
    {
        var error = AssertError("0202.239.952", BelgianErrorCode.InvalidChecksum);

        Assert.Equal("51", error.Expected);
        Assert.Equal(
            "Le numéro d'entreprise est invalide : ses chiffres de contrôle ne correspondent pas. Vérifiez qu'il ne contient pas de faute de frappe.",
            error.GetMessage(CultureInfo.GetCultureInfo("fr-BE")));
    }

    [Theory]
    [InlineData("", BelgianErrorCode.Empty)]
    [InlineData("0202.239.95X", BelgianErrorCode.InvalidCharacter)]
    [InlineData("0202", BelgianErrorCode.InvalidLength)]
    [InlineData("FR0202239951", BelgianErrorCode.InvalidCountryCode)]
    [InlineData("2202.239.951", BelgianErrorCode.InvalidFirstDigit)]
    [InlineData("0202.239.952", BelgianErrorCode.InvalidChecksum)]
    public void Every_error_has_a_message_in_every_language(string input, BelgianErrorCode code)
    {
        var error = AssertError(input, code);

        foreach (string language in new[] { "en", "fr", "nl" })
        {
            Assert.NotEmpty(error.GetMessage(CultureInfo.GetCultureInfo(language)));
        }
    }

    [Theory]
    [InlineData(null, "0202.239.951")]
    [InlineData("", "0202.239.951")]
    [InlineData("D", "0202.239.951")]
    [InlineData("d", "0202.239.951")]
    [InlineData("N", "0202239951")]
    [InlineData("n", "0202239951")]
    [InlineData("V", "BE0202239951")]
    [InlineData("v", "BE0202239951")]
    public void Formats_in_the_requested_format(string? format, string expected)
    {
        Assert.Equal(expected, Sample.ToString(format));
    }

    [Theory]
    [InlineData("G")]
    [InlineData("X")]
    [InlineData("DD")]
    public void Rejects_an_unknown_format(string format)
    {
        Assert.Throws<FormatException>(() => Sample.ToString(format));
    }

    [Fact]
    public void Formats_through_the_standard_interfaces()
    {
        Assert.Equal("BE0202239951", ((IFormattable)Sample).ToString("V", CultureInfo.InvariantCulture));
        Assert.Equal("BE0202239951", string.Create(CultureInfo.InvariantCulture, $"{Sample:V}"));
    }

    [Fact]
    public void TryFormat_reports_a_destination_that_is_too_small()
    {
        Span<char> destination = stackalloc char[11];

        Assert.False(Sample.TryFormat(destination, out int charsWritten, "D"));
        Assert.Equal(0, charsWritten);

        Assert.True(Sample.TryFormat(destination, out charsWritten, "N"));
        Assert.Equal("0202239951", destination[..charsWritten].ToString());
    }

    [Fact]
    public void Default_value_is_empty_and_formats_as_an_empty_string()
    {
        EnterpriseNumber number = default;

        Assert.True(number.IsEmpty);
        Assert.Equal(string.Empty, number.ToString());
        Assert.Equal(string.Empty, number.ToString("V"));
        Assert.False(Sample.IsEmpty);
        Assert.NotEqual(default, Sample);
    }

    [Fact]
    public void Numbers_with_the_same_digits_are_equal_whatever_the_input_form()
    {
        var other = EnterpriseNumber.Parse("BE 202239951");

        Assert.Equal(Sample, other);
        Assert.True(Sample == other);
        Assert.False(Sample != other);
        Assert.Equal(Sample.GetHashCode(), other.GetHashCode());
        Assert.NotEqual(Sample, EnterpriseNumber.Parse("0403.170.701"));
        Assert.False(Sample.Equals("0202.239.951"));
    }

    [Fact]
    public void Parse_throws_a_format_exception_carrying_the_error()
    {
        var exception = Assert.Throws<BelgianFormatException>(() => EnterpriseNumber.Parse("0202.239.952"));

        Assert.Equal(BelgianErrorCode.InvalidChecksum, exception.Error.Code);
        Assert.Equal(nameof(EnterpriseNumber), exception.Error.TypeName);
    }

    [Fact]
    public void Parse_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => EnterpriseNumber.Parse(null!));
    }

    [Fact]
    public void Parse_accepts_spans()
    {
        Assert.Equal(Sample, EnterpriseNumber.Parse("BE0202239951".AsSpan()));
    }

    [Fact]
    public void Validate_returns_null_for_a_valid_number()
    {
        Assert.Null(EnterpriseNumber.Validate("0202.239.951"));
        Assert.Null(EnterpriseNumber.Validate("0202.239.951".AsSpan()));
    }

    [Fact]
    public void Implements_the_generic_parsing_contracts()
    {
        Assert.Equal(Sample, ParseAs<EnterpriseNumber>("0202.239.951"));
        Assert.True(TryParseAs<EnterpriseNumber>("BE0202239951", out var number));
        Assert.Equal(Sample, number);
        Assert.False(TryParseAs<EnterpriseNumber>("0202.239.952", out _));
        Assert.Throws<BelgianFormatException>(() => ParseAs<EnterpriseNumber>("0202.239.952"));
    }

    private static T ParseAs<T>(string s)
        where T : IParsable<T> => T.Parse(s, CultureInfo.InvariantCulture);

    private static bool TryParseAs<T>(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out T result)
        where T : ISpanParsable<T> => T.TryParse(s, CultureInfo.InvariantCulture, out result);

    private static BelgianValidationError AssertError(string? input, BelgianErrorCode code)
    {
        Assert.False(EnterpriseNumber.TryParse(input, out var number, out var error));
        Assert.True(number.IsEmpty);
        Assert.Equal(code, error.Code);
        Assert.Equal(nameof(EnterpriseNumber), error.TypeName);
        Assert.Equal(error, EnterpriseNumber.Validate(input));
        Assert.Equal(error, EnterpriseNumber.Validate(input.AsSpan()));

        return error;
    }
}
