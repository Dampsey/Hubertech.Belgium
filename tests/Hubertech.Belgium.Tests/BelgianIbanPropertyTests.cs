using System.Globalization;
using CsCheck;

namespace Hubertech.Belgium.Tests;

public sealed class BelgianIbanPropertyTests
{
    // Any valid Belgian IBAN: a ten-digit account base, its national check digits, then the
    // ISO 7064 check digits computed independently of the library.
    private static readonly Gen<string> ValidIban = Gen.ULong[1, 9_999_999_999].Select(accountBase =>
    {
        ulong remainder = accountBase % 97;
        ulong account = (accountBase * 100) + (remainder == 0 ? 97 : remainder);
        ulong ibanCheckDigits = 98 - (((account * 1_000_000) + 111_400) % 97);

        return string.Create(CultureInfo.InvariantCulture, $"BE{ibanCheckDigits:D2}{account:D12}");
    });

    private static readonly string[] Formats = ["P", "E"];

    // Text close to an IBAN, to reach the parser's edge cases more often than random text does.
    private static readonly Gen<string> IbanLikeText = Gen.String[Gen.Char["0123456789 .-BEbeNL"], 0, 24];

    [Fact]
    public void Parsing_never_throws_and_always_explains_a_failure()
    {
        Gen.OneOf(Gen.String, IbanLikeText, ValidIban).Sample(input =>
        {
            bool parsed = BelgianIban.TryParse(input, out var iban, out var error);

            Assert.Equal(parsed, !iban.IsEmpty);
            Assert.Equal(parsed, error.Code == BelgianErrorCode.None);
            Assert.Equal(parsed, BelgianIban.TryParse(input.AsSpan(), out _));
            Assert.Equal(parsed, error.Message.Length == 0);
        });
    }

    [Fact]
    public void Every_valid_iban_round_trips_through_every_format()
    {
        ValidIban.Sample(electronic =>
        {
            var iban = BelgianIban.Parse(electronic);

            Assert.Equal(electronic, iban.ToString("E"));
            foreach (string format in Formats)
            {
                Assert.Equal(iban, BelgianIban.Parse(iban.ToString(format)));
            }
        });
    }

    [Fact]
    public void Every_single_digit_typo_is_detected()
    {
        Gen.Select(ValidIban, Gen.Int[2, 15], Gen.Int[1, 9]).Sample((iban, position, shift) =>
        {
            char[] typo = iban.ToCharArray();
            typo[position] = (char)('0' + ((typo[position] - '0' + shift) % 10));

            Assert.False(BelgianIban.TryParse(new string(typo), out _));
        });
    }

    [Fact]
    public void Every_swap_of_two_different_adjacent_digits_is_detected()
    {
        Gen.Select(ValidIban, Gen.Int[2, 14])
            .Where(sample => sample.Item1[sample.Item2] != sample.Item1[sample.Item2 + 1])
            .Sample((iban, position) =>
            {
                char[] swapped = iban.ToCharArray();
                (swapped[position], swapped[position + 1]) = (swapped[position + 1], swapped[position]);

                Assert.False(BelgianIban.TryParse(new string(swapped), out _));
            });
    }
}
