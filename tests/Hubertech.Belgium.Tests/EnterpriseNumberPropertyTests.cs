using System.Globalization;
using CsCheck;

namespace Hubertech.Belgium.Tests;

public sealed class EnterpriseNumberPropertyTests
{
    // Any valid number: an eight-digit base starting with 0 or 1, followed by its check digits.
    private static readonly Gen<string> ValidNumber = Gen.UInt[0, 19_999_999].Select(baseNumber =>
        string.Create(CultureInfo.InvariantCulture, $"{baseNumber:D8}{97 - (baseNumber % 97):D2}"));

    private static readonly string[] Formats = ["D", "N", "V"];

    // Text close to an enterprise number, to reach the parser's edge cases more often than random text does.
    private static readonly Gen<string> NumberLikeText = Gen.String[Gen.Char["0123456789 .-BEbeFR"], 0, 20];

    [Fact]
    public void Parsing_never_throws_and_always_explains_a_failure()
    {
        Gen.OneOf(Gen.String, NumberLikeText, ValidNumber).Sample(input =>
        {
            bool parsed = EnterpriseNumber.TryParse(input, out var number, out var error);

            Assert.Equal(parsed, !number.IsEmpty);
            Assert.Equal(parsed, error.Code == BelgianErrorCode.None);
            Assert.Equal(parsed, EnterpriseNumber.TryParse(input.AsSpan(), out _));
            Assert.Equal(parsed, error.Message.Length == 0);
        });
    }

    [Fact]
    public void Every_valid_number_round_trips_through_every_format()
    {
        ValidNumber.Sample(digits =>
        {
            var number = EnterpriseNumber.Parse(digits);

            Assert.Equal(digits, number.ToString("N"));
            foreach (string format in Formats)
            {
                Assert.Equal(number, EnterpriseNumber.Parse(number.ToString(format)));
            }
        });
    }

    [Fact]
    public void Every_single_digit_typo_is_detected()
    {
        Gen.Select(ValidNumber, Gen.Int[0, 9], Gen.Int[1, 9]).Sample((digits, position, shift) =>
        {
            char[] typo = digits.ToCharArray();
            typo[position] = (char)('0' + ((typo[position] - '0' + shift) % 10));

            Assert.False(EnterpriseNumber.TryParse(new string(typo), out _));
        });
    }

    [Fact]
    public void Every_swap_of_two_different_adjacent_digits_is_detected()
    {
        Gen.Select(ValidNumber, Gen.Int[0, 8])
            .Where(sample => sample.Item1[sample.Item2] != sample.Item1[sample.Item2 + 1])
            .Sample((digits, position) =>
            {
                char[] swapped = digits.ToCharArray();
                (swapped[position], swapped[position + 1]) = (swapped[position + 1], swapped[position]);

                Assert.False(EnterpriseNumber.TryParse(new string(swapped), out _));
            });
    }
}
