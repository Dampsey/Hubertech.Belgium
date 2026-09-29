using System.Globalization;
using CsCheck;

namespace Hubertech.Belgium.Tests;

public sealed class BelgianValidationErrorTests
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr");
    private static readonly CultureInfo Dutch = CultureInfo.GetCultureInfo("nl");

    [Fact]
    public void Default_value_means_no_error()
    {
        BelgianValidationError error = default;

        Assert.Equal(BelgianErrorCode.None, error.Code);
        Assert.Equal(string.Empty, error.TypeName);
        Assert.Null(error.Position);
        Assert.Null(error.Expected);
        Assert.Equal(string.Empty, error.Message);
    }

    [Fact]
    public void Type_name_identifies_the_failing_parser()
    {
        var error = BelgianValidationError.Empty("EnterpriseNumber");

        Assert.Equal(BelgianErrorCode.Empty, error.Code);
        Assert.Equal("EnterpriseNumber", error.TypeName);
    }

    [Fact]
    public void Invalid_character_error_exposes_a_zero_based_position_and_shows_a_one_based_one()
    {
        var error = BelgianValidationError.InvalidCharacter("EnterpriseNumber", position: 4, character: 'O');

        Assert.Equal(4, error.Position);
        Assert.Equal(
            "The enterprise number contains an invalid character, 'O', at position 5.",
            error.GetMessage(English));
        Assert.Equal(
            "Le numéro d'entreprise contient un caractère non autorisé, « O », en position 5.",
            error.GetMessage(French));
        Assert.Equal(
            "Het ondernemingsnummer bevat een ongeldig teken, 'O', op positie 5.",
            error.GetMessage(Dutch));
    }

    [Theory]
    [InlineData('\t', "U+0009")]
    [InlineData(' ', "U+00A0")]
    [InlineData('\ud83d', "U+D83D")]
    public void Invisible_characters_are_shown_as_code_points(char character, string expected)
    {
        var error = BelgianValidationError.InvalidCharacter("StructuredCommunication", position: 0, character);

        Assert.Contains($"'{expected}'", error.GetMessage(English), StringComparison.Ordinal);
    }

    [Fact]
    public void Invalid_character_message_is_well_formed_for_any_character_and_position()
    {
        Gen.Select(Gen.Char, Gen.Int[0, 10_000]).Sample((character, position) =>
        {
            var error = BelgianValidationError.InvalidCharacter("BelgianIban", position, character);

            foreach (var culture in new[] { English, French, Dutch })
            {
                string message = error.GetMessage(culture);

                Assert.Contains((position + 1).ToString(CultureInfo.InvariantCulture), message, StringComparison.Ordinal);
                Assert.DoesNotContain("{0}", message, StringComparison.Ordinal);
                Assert.DoesNotContain("{1}", message, StringComparison.Ordinal);
            }
        });
    }

    [Fact]
    public void Checksum_error_exposes_the_expected_check_digits_with_a_leading_zero()
    {
        var error = BelgianValidationError.InvalidChecksum("EnterpriseNumber", expectedCheckDigits: 5);

        Assert.Equal(BelgianErrorCode.InvalidChecksum, error.Code);
        Assert.Equal("05", error.Expected);
    }

    [Fact]
    public void Checksum_message_does_not_suggest_the_expected_check_digits()
    {
        var error = BelgianValidationError.InvalidChecksum("EnterpriseNumber", expectedCheckDigits: 51);

        foreach (var culture in new[] { English, French, Dutch })
        {
            Assert.DoesNotContain("51", error.GetMessage(culture), StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("fr-BE", "Le numéro d'entreprise doit comporter 10 chiffres.")]
    [InlineData("nl-BE", "Het ondernemingsnummer moet 10 cijfers bevatten.")]
    [InlineData("en-GB", "The enterprise number must contain 10 digits.")]
    [InlineData("de-BE", "The enterprise number must contain 10 digits.")]
    [InlineData("", "The enterprise number must contain 10 digits.")]
    public void Regional_and_unsupported_cultures_fall_back_to_a_supported_language(string cultureName, string expected)
    {
        var error = BelgianValidationError.InvalidLength("EnterpriseNumber");

        Assert.Equal(expected, error.GetMessage(CultureInfo.GetCultureInfo(cultureName)));
    }

    [Fact]
    public void Message_follows_the_current_UI_culture_when_it_is_read()
    {
        var error = BelgianValidationError.InvalidLength("StructuredCommunication");
        var originalCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = French;
            Assert.Equal("La communication structurée doit comporter 12 chiffres.", error.Message);

            CultureInfo.CurrentUICulture = Dutch;
            Assert.Equal("De gestructureerde mededeling moet 12 cijfers bevatten.", error.Message);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void Getting_a_message_requires_a_culture()
    {
        var error = BelgianValidationError.Empty("BelgianIban");

        Assert.Throws<ArgumentNullException>(() => error.GetMessage(null!));
    }

    [Fact]
    public void Errors_with_the_same_details_are_equal()
    {
        Assert.Equal(
            BelgianValidationError.InvalidChecksum("BelgianIban", 68),
            BelgianValidationError.InvalidChecksum("BelgianIban", 68));
        Assert.NotEqual(
            BelgianValidationError.InvalidChecksum("BelgianIban", 68),
            BelgianValidationError.InvalidChecksum("BelgianIban", 69));
        Assert.NotEqual(
            BelgianValidationError.InvalidChecksum("BelgianIban", 68),
            BelgianValidationError.InvalidChecksum("EnterpriseNumber", 68));
    }
}
