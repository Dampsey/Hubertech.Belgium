using System.Globalization;

namespace Hubertech.Belgium.Tests;

public sealed class SocialSecurityIdentificationNumberTests
{
    private static readonly SocialSecurityIdentificationNumber Sample = SocialSecurityIdentificationNumber.Parse("85.07.30-033.28");

    [Theory]
    [InlineData("85.07.30-033.28")]
    [InlineData("85073003328")]
    [InlineData("85 07 30 033 28")]
    [InlineData("850730-033-28")]
    [InlineData(" 85.07.30 - 033.28 ")]
    public void Accepts_the_usual_ways_of_writing_a_number(string input)
    {
        Assert.Equal(Sample, SocialSecurityIdentificationNumber.Parse(input));
    }

    [Theory]
    // Examples of the documentation of python-stdnum.
    [InlineData("85.07.30-033.28", SocialSecurityIdentificationNumberKind.NationalRegister, "1985-07-30")]
    [InlineData("17.07.30-033.84", SocialSecurityIdentificationNumberKind.NationalRegister, "2017-07-30")]
    [InlineData("98.47.28-997.65", SocialSecurityIdentificationNumberKind.Bis, "1998-07-28")]
    [InlineData("01.49.07-001.85", SocialSecurityIdentificationNumberKind.Bis, "2001-09-07")]
    // Computed independently of the library.
    [InlineData("85.27.30-033.71", SocialSecurityIdentificationNumberKind.Bis, "1985-07-30")]
    [InlineData("00.02.29-002.44", SocialSecurityIdentificationNumberKind.NationalRegister, "2000-02-29")]
    [InlineData("10.12.31-001.71", SocialSecurityIdentificationNumberKind.NationalRegister, "2010-12-31")]
    public void Tells_the_register_and_the_date_of_birth(string input, SocialSecurityIdentificationNumberKind kind, string birthDate)
    {
        var number = SocialSecurityIdentificationNumber.Parse(input);

        Assert.Equal(kind, number.Kind);
        Assert.Equal(DateOnly.ParseExact(birthDate, "yyyy-MM-dd", CultureInfo.InvariantCulture), number.BirthDate);
    }

    [Theory]
    [InlineData("85.00.00-033.06", SocialSecurityIdentificationNumberKind.NationalRegister)]
    [InlineData("85.07.00-033.55", SocialSecurityIdentificationNumberKind.NationalRegister)]
    [InlineData("85.40.00-033.92", SocialSecurityIdentificationNumberKind.Bis)]
    [InlineData("85.02.30-033.90", SocialSecurityIdentificationNumberKind.NationalRegister)]
    [InlineData("00.02.29-002.15", SocialSecurityIdentificationNumberKind.NationalRegister)]
    public void Has_no_date_of_birth_when_the_encoded_one_is_incomplete_or_impossible(string input, SocialSecurityIdentificationNumberKind kind)
    {
        // Month or day zero: unknown, or the serial numbers of the day ran out. 30 February, and
        // 29 February 1900, are not dates: the check digits of the last one are those of 1900.
        var number = SocialSecurityIdentificationNumber.Parse(input);

        Assert.Equal(kind, number.Kind);
        Assert.Null(number.BirthDate);
    }

    [Fact]
    public void Masks_every_digit_but_the_check_digits_by_default()
    {
        Assert.Equal("**.**.**-***.28", Sample.ToString());
        Assert.Equal("**.**.**-***.28", Sample.ToString("M"));
        Assert.Equal("**.**.**-***.28", $"{Sample}");
        Assert.Equal("**.**.**-***.28", string.Format(CultureInfo.InvariantCulture, "{0}", Sample));
    }

    [Theory]
    [InlineData("D", "85.07.30-033.28")]
    [InlineData("d", "85.07.30-033.28")]
    [InlineData("N", "85073003328")]
    [InlineData("n", "85073003328")]
    [InlineData("m", "**.**.**-***.28")]
    public void Formats_the_full_number_on_request(string format, string expected)
    {
        Span<char> destination = stackalloc char[15];

        Assert.Equal(expected, Sample.ToString(format));
        Assert.True(Sample.TryFormat(destination, out int charsWritten, format));
        Assert.Equal(expected, destination[..charsWritten].ToString());
    }

    [Fact]
    public void Interpolation_with_a_format_gives_the_full_number()
    {
        Assert.Equal("85.07.30-033.28", $"{Sample:D}");
    }

    [Fact]
    public void Rejects_an_unknown_format()
    {
        Assert.Throws<FormatException>(() => Sample.ToString("V"));
    }

    [Fact]
    public void Reports_a_destination_too_small()
    {
        Span<char> destination = stackalloc char[14];

        Assert.False(Sample.TryFormat(destination, out int charsWritten, "D"));
        Assert.Equal(0, charsWritten);
    }

    [Fact]
    public void Default_value_is_empty()
    {
        SocialSecurityIdentificationNumber number = default;

        Assert.True(number.IsEmpty);
        Assert.Equal(SocialSecurityIdentificationNumberKind.None, number.Kind);
        Assert.Null(number.BirthDate);
        Assert.Equal(string.Empty, number.ToString());
        Assert.Equal(string.Empty, number.ToString("D"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(".. -")]
    public void Rejects_empty_input(string? input)
    {
        AssertError(input, BelgianErrorCode.Empty);
    }

    [Theory]
    [InlineData("85.07.30/033.28", 8, '/')]
    [InlineData("BE85073003328", 0, 'B')]
    public void Rejects_an_invalid_character_and_gives_its_position(string input, int position, char character)
    {
        var error = AssertError(input, BelgianErrorCode.InvalidCharacter);

        Assert.Equal(position, error.Position);
        Assert.Contains($"'{character}'", error.GetMessage(CultureInfo.GetCultureInfo("en")), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("85.07.30-033.2")]
    [InlineData("85.07.30-033.281")]
    public void Rejects_a_number_without_eleven_digits(string input)
    {
        AssertError(input, BelgianErrorCode.InvalidLength);
    }

    [Theory]
    // The check digits of these numbers match: only their month is wrong.
    [InlineData("85.13.30-033.70")]
    [InlineData("85.33.30-033.16")]
    [InlineData("85.53.30-033.59")]
    public void Rejects_a_month_of_birth_that_no_register_uses(string input)
    {
        var error = AssertError(input, BelgianErrorCode.InvalidBirthDate);

        Assert.Equal(
            "Le numéro de registre national ou BIS est invalide : ses troisième et quatrième chiffres ne forment pas un mois de naissance.",
            error.GetMessage(CultureInfo.GetCultureInfo("fr-BE")));
    }

    [Fact]
    public void Rejects_wrong_check_digits_without_guessing_the_expected_ones()
    {
        // The expected check digits depend on the century of birth, which the number does not carry.
        var error = AssertError("85.07.30-033.29", BelgianErrorCode.InvalidChecksum);

        Assert.Null(error.Expected);
    }

    [Fact]
    public void Rejects_check_digits_that_only_match_a_birth_after_the_current_year()
    {
        // These check digits are those of a birth from 2000, here in 2099.
        AssertError("99.01.01-001.47", BelgianErrorCode.InvalidChecksum);
    }

    [Fact]
    public void Lets_through_a_typo_that_turns_a_number_into_one_of_the_other_century()
    {
        // A known limit of the two computations: changing 06 into 00 turns a birth in 1906 into a
        // birth in 2000, and both check digits happen to match.
        Assert.Equal(new DateOnly(1906, 2, 27), SocialSecurityIdentificationNumber.Parse("06.02.27-549.42").BirthDate);
        Assert.Equal(new DateOnly(2000, 2, 27), SocialSecurityIdentificationNumber.Parse("00.02.27-549.42").BirthDate);
    }

    [Fact]
    public void Parse_throws_a_format_exception_carrying_the_error()
    {
        var exception = Assert.Throws<BelgianFormatException>(() => SocialSecurityIdentificationNumber.Parse("85.07.30-033.29"));

        Assert.Equal(BelgianErrorCode.InvalidChecksum, exception.Error.Code);
        Assert.Equal(nameof(SocialSecurityIdentificationNumber), exception.Error.TypeName);
    }

    [Fact]
    public void Parse_rejects_null()
    {
        Assert.Throws<ArgumentNullException>(() => SocialSecurityIdentificationNumber.Parse(null!));
    }

    [Fact]
    public void Validate_returns_null_for_a_valid_number()
    {
        Assert.Null(SocialSecurityIdentificationNumber.Validate("85.07.30-033.28"));
        Assert.Null(SocialSecurityIdentificationNumber.Validate("85.07.30-033.28".AsSpan()));
    }

    [Fact]
    public void Equal_numbers_are_equal_whatever_their_form()
    {
        var other = SocialSecurityIdentificationNumber.Parse("85073003328");

        Assert.True(Sample == other);
        Assert.False(Sample != other);
        Assert.True(Sample.Equals((object)other));
        Assert.Equal(Sample.GetHashCode(), other.GetHashCode());
        Assert.NotEqual(Sample, SocialSecurityIdentificationNumber.Parse("17.07.30-033.84"));
    }

    [Fact]
    public void Generic_parsing_ignores_the_format_provider()
    {
        Assert.Equal(Sample, ParseGeneric<SocialSecurityIdentificationNumber>("85.07.30-033.28"));
        Assert.Equal("85.07.30-033.28", ((IFormattable)Sample).ToString("D", CultureInfo.GetCultureInfo("fr-BE")));
    }

    private static T ParseGeneric<T>(string s)
        where T : ISpanParsable<T>
    {
        Assert.True(T.TryParse(s, CultureInfo.InvariantCulture, out var result));
        Assert.True(T.TryParse(s.AsSpan(), CultureInfo.InvariantCulture, out var fromSpan));
        Assert.Equal(result, fromSpan);
        Assert.Equal(result, T.Parse(s, CultureInfo.InvariantCulture));

        return T.Parse(s.AsSpan(), CultureInfo.InvariantCulture);
    }

    private static BelgianValidationError AssertError(string? input, BelgianErrorCode code)
    {
        Assert.False(SocialSecurityIdentificationNumber.TryParse(input, out var number, out var error));
        Assert.False(SocialSecurityIdentificationNumber.TryParse(input.AsSpan(), out _));
        Assert.True(number.IsEmpty);
        Assert.Equal(code, error.Code);
        Assert.Equal(nameof(SocialSecurityIdentificationNumber), error.TypeName);
        Assert.Equal(error, SocialSecurityIdentificationNumber.Validate(input));

        return error;
    }
}
