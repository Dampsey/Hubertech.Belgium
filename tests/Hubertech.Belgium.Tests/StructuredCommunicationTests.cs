using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Hubertech.Belgium.Tests;

public sealed class StructuredCommunicationTests
{
    private static readonly StructuredCommunication Sample = StructuredCommunication.Parse("+++123/4567/89002+++");

    [Theory]
    [InlineData("+++123/4567/89002+++")]
    [InlineData("***123/4567/89002***")]
    [InlineData("123/4567/89002")]
    [InlineData("123456789002")]
    [InlineData(" +++ 123 / 4567 / 89002 +++ ")]
    [InlineData("123.4567.89002")]
    [InlineData("123-4567-89002")]
    [InlineData("123 4567 89002")]
    public void Accepts_the_usual_ways_of_writing_a_reference(string input)
    {
        Assert.Equal("+++123/4567/89002+++", StructuredCommunication.Parse(input).ToString());
    }

    [Theory]
    [InlineData(0UL, "+++000/0000/00097+++")]
    [InlineData(1_234_567_890UL, "+++123/4567/89002+++")]
    [InlineData(2_026_000_123UL, "+++202/6000/12320+++")]
    [InlineData(108_068_171UL, "+++010/8068/17183+++")]
    [InlineData(StructuredCommunication.MaxBaseNumber, "+++999/9999/99948+++")]
    public void Builds_a_reference_from_a_number(ulong number, string expected)
    {
        var reference = StructuredCommunication.FromNumber(number);

        Assert.Equal(expected, reference.ToString());
        Assert.Equal(number, reference.BaseNumber);
        Assert.Equal(reference, StructuredCommunication.Parse(expected));
    }

    [Fact]
    public void Rejects_a_number_of_more_than_ten_digits()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => StructuredCommunication.FromNumber(StructuredCommunication.MaxBaseNumber + 1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("+++//+++")]
    public void Rejects_empty_input(string? input)
    {
        AssertError(input, BelgianErrorCode.Empty);
    }

    [Theory]
    [InlineData("+++123/4567/8900X+++", 16, 'X')]
    [InlineData("OGM 123/4567/89002", 0, 'O')]
    [InlineData("123,4567,89002", 3, ',')]
    public void Rejects_an_invalid_character_and_gives_its_position(string input, int position, char character)
    {
        var error = AssertError(input, BelgianErrorCode.InvalidCharacter);

        Assert.Equal(position, error.Position);
        Assert.Contains($"'{character}'", error.GetMessage(CultureInfo.GetCultureInfo("en")), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("+++123/4567/8900+++")]
    [InlineData("+++123/4567/890022+++")]
    [InlineData("12")]
    public void Rejects_a_reference_without_twelve_digits(string input)
    {
        AssertError(input, BelgianErrorCode.InvalidLength);
    }

    [Theory]
    [InlineData("+++123/4567/89003+++", "02")]
    [InlineData("010/8068/17180", "83")]
    [InlineData("+++000/0000/00000+++", "97")]
    public void Rejects_wrong_check_digits_and_exposes_the_expected_ones(string input, string expected)
    {
        var error = AssertError(input, BelgianErrorCode.InvalidChecksum);

        Assert.Equal(expected, error.Expected);
    }

    [Fact]
    public void Checksum_message_is_localized()
    {
        var error = AssertError("+++123/4567/89003+++", BelgianErrorCode.InvalidChecksum);

        Assert.Equal(
            "De gestructureerde mededeling is ongeldig: de controlecijfers kloppen niet. Controleer of ze geen tikfout bevat.",
            error.GetMessage(CultureInfo.GetCultureInfo("nl-BE")));
    }

    [Theory]
    [InlineData("", BelgianErrorCode.Empty)]
    [InlineData("+++123/4567/8900X+++", BelgianErrorCode.InvalidCharacter)]
    [InlineData("123", BelgianErrorCode.InvalidLength)]
    [InlineData("+++123/4567/89003+++", BelgianErrorCode.InvalidChecksum)]
    public void Every_error_has_a_message_in_every_language(string input, BelgianErrorCode code)
    {
        var error = AssertError(input, code);

        foreach (string language in new[] { "en", "fr", "nl" })
        {
            Assert.NotEmpty(error.GetMessage(CultureInfo.GetCultureInfo(language)));
        }
    }

    [Theory]
    [InlineData(null, "+++123/4567/89002+++")]
    [InlineData("", "+++123/4567/89002+++")]
    [InlineData("+", "+++123/4567/89002+++")]
    [InlineData("*", "***123/4567/89002***")]
    [InlineData("N", "123456789002")]
    [InlineData("n", "123456789002")]
    public void Formats_in_the_requested_format(string? format, string expected)
    {
        Assert.Equal(expected, Sample.ToString(format));
    }

    [Theory]
    [InlineData("D")]
    [InlineData("/")]
    [InlineData("++")]
    public void Rejects_an_unknown_format(string format)
    {
        Assert.Throws<FormatException>(() => Sample.ToString(format));
    }

    [Fact]
    public void Formats_through_the_standard_interfaces()
    {
        Assert.Equal("***123/4567/89002***", ((IFormattable)Sample).ToString("*", CultureInfo.InvariantCulture));
        Assert.Equal("123456789002", string.Create(CultureInfo.InvariantCulture, $"{Sample:N}"));
    }

    [Fact]
    public void TryFormat_reports_a_destination_that_is_too_small()
    {
        Span<char> destination = stackalloc char[19];

        Assert.False(Sample.TryFormat(destination, out int charsWritten, "+"));
        Assert.Equal(0, charsWritten);

        Assert.True(Sample.TryFormat(destination, out charsWritten, "N"));
        Assert.Equal("123456789002", destination[..charsWritten].ToString());
    }

    [Fact]
    public void Default_value_is_empty_and_formats_as_an_empty_string()
    {
        StructuredCommunication reference = default;

        Assert.True(reference.IsEmpty);
        Assert.Equal(0UL, reference.BaseNumber);
        Assert.Equal(string.Empty, reference.ToString());
        Assert.Equal(string.Empty, reference.ToString("N"));
        Assert.False(StructuredCommunication.FromNumber(0).IsEmpty);
    }

    [Fact]
    public void References_with_the_same_digits_are_equal_whatever_the_input_form()
    {
        var other = StructuredCommunication.Parse("***123 4567 89002***");

        Assert.Equal(Sample, other);
        Assert.True(Sample == other);
        Assert.False(Sample != other);
        Assert.Equal(Sample.GetHashCode(), other.GetHashCode());
        Assert.NotEqual(Sample, StructuredCommunication.FromNumber(0));
        Assert.False(Sample.Equals("+++123/4567/89002+++"));
    }

    [Fact]
    public void Parse_throws_a_format_exception_carrying_the_error()
    {
        var exception = Assert.Throws<BelgianFormatException>(() => StructuredCommunication.Parse("+++123/4567/89003+++"));

        Assert.Equal(BelgianErrorCode.InvalidChecksum, exception.Error.Code);
        Assert.Equal(nameof(StructuredCommunication), exception.Error.TypeName);
    }

    [Fact]
    public void Parse_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => StructuredCommunication.Parse(null!));
    }

    [Fact]
    public void Validate_returns_null_for_a_valid_reference()
    {
        Assert.Null(StructuredCommunication.Validate("+++123/4567/89002+++"));
        Assert.Null(StructuredCommunication.Validate("123456789002".AsSpan()));
    }

    [Fact]
    public void Implements_the_generic_parsing_contracts()
    {
        Assert.Equal(Sample, ParseAs<StructuredCommunication>("123456789002"));
        Assert.True(TryParseAs<StructuredCommunication>("***123/4567/89002***", out var reference));
        Assert.Equal(Sample, reference);
        Assert.False(TryParseAs<StructuredCommunication>("123456789003", out _));
        Assert.Throws<BelgianFormatException>(() => ParseAs<StructuredCommunication>("123456789003"));
    }

    private static T ParseAs<T>(string s)
        where T : IParsable<T> => T.Parse(s, CultureInfo.InvariantCulture);

    private static bool TryParseAs<T>(ReadOnlySpan<char> s, [MaybeNullWhen(false)] out T result)
        where T : ISpanParsable<T> => T.TryParse(s, CultureInfo.InvariantCulture, out result);

    private static BelgianValidationError AssertError(string? input, BelgianErrorCode code)
    {
        Assert.False(StructuredCommunication.TryParse(input, out var reference, out var error));
        Assert.True(reference.IsEmpty);
        Assert.Equal(code, error.Code);
        Assert.Equal(nameof(StructuredCommunication), error.TypeName);
        Assert.Equal(error, StructuredCommunication.Validate(input));
        Assert.Equal(error, StructuredCommunication.Validate(input.AsSpan()));

        return error;
    }
}
