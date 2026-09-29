using CsCheck;

namespace Hubertech.Belgium.Tests;

public sealed class StructuredCommunicationPropertyTests
{
    private static readonly Gen<ulong> BaseNumber = Gen.ULong[0, StructuredCommunication.MaxBaseNumber];

    // Any valid reference, as twelve digits.
    private static readonly Gen<string> ValidDigits = BaseNumber.Select(number => StructuredCommunication.FromNumber(number).ToString("N"));

    private static readonly string[] Formats = ["+", "*", "N"];

    // Text close to a structured communication, to reach the parser's edge cases more often than random text does.
    private static readonly Gen<string> ReferenceLikeText = Gen.String[Gen.Char["0123456789 +*/.-"], 0, 24];

    [Fact]
    public void Parsing_never_throws_and_always_explains_a_failure()
    {
        Gen.OneOf(Gen.String, ReferenceLikeText, ValidDigits).Sample(input =>
        {
            bool parsed = StructuredCommunication.TryParse(input, out var reference, out var error);

            Assert.Equal(parsed, !reference.IsEmpty);
            Assert.Equal(parsed, error.Code == BelgianErrorCode.None);
            Assert.Equal(parsed, StructuredCommunication.TryParse(input.AsSpan(), out _));
            Assert.Equal(parsed, error.Message.Length == 0);
        });
    }

    [Fact]
    public void Every_number_round_trips_through_every_format()
    {
        BaseNumber.Sample(number =>
        {
            var reference = StructuredCommunication.FromNumber(number);

            Assert.Equal(number, reference.BaseNumber);
            foreach (string format in Formats)
            {
                Assert.Equal(reference, StructuredCommunication.Parse(reference.ToString(format)));
            }
        });
    }

    [Fact]
    public void Every_single_digit_typo_is_detected()
    {
        Gen.Select(ValidDigits, Gen.Int[0, 11], Gen.Int[1, 9]).Sample((digits, position, shift) =>
        {
            char[] typo = digits.ToCharArray();
            typo[position] = (char)('0' + ((typo[position] - '0' + shift) % 10));

            Assert.False(StructuredCommunication.TryParse(new string(typo), out _));
        });
    }

    [Fact]
    public void Every_swap_of_two_different_adjacent_digits_is_detected()
    {
        Gen.Select(ValidDigits, Gen.Int[0, 10])
            .Where(sample => sample.Item1[sample.Item2] != sample.Item1[sample.Item2 + 1])
            .Sample((digits, position) =>
            {
                char[] swapped = digits.ToCharArray();
                (swapped[position], swapped[position + 1]) = (swapped[position + 1], swapped[position]);

                Assert.False(StructuredCommunication.TryParse(new string(swapped), out _));
            });
    }
}
