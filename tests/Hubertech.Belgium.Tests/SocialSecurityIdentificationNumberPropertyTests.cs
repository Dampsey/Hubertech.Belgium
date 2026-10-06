using System.Globalization;
using CsCheck;

namespace Hubertech.Belgium.Tests;

public sealed class SocialSecurityIdentificationNumberPropertyTests
{
    private const ulong BornFrom2000Prefix = 2_000_000_000;

    // 00 to 12 in a national register number, increased by 20 or 40 in a BIS number.
    private static readonly int[] Months = [.. Enumerable.Range(0, 13), .. Enumerable.Range(20, 13), .. Enumerable.Range(40, 13)];

    private static readonly string[] Formats = ["D", "N"];

    // Any valid number, computed independently of the library: a birth before 2000, or from 2000
    // up to the current year, with any day digits since the register does not always write a date.
    private static readonly Gen<string> ValidNumber = Gen.Select(Gen.Bool, Gen.Int[0, 99], Gen.OneOfConst(Months), Gen.Int[0, 99], Gen.Int[0, 999])
        .Select((bornFrom2000, year, month, day, serial) =>
        {
            int yearDigits = bornFrom2000 ? year % (DateTime.UtcNow.Year - 1999) : year;
            ulong nineDigits = ulong.Parse(string.Create(CultureInfo.InvariantCulture, $"{yearDigits:D2}{month:D2}{day:D2}{serial:D3}"), CultureInfo.InvariantCulture);
            ulong checkDigits = 97 - (((bornFrom2000 ? BornFrom2000Prefix : 0) + nineDigits) % 97);

            return string.Create(CultureInfo.InvariantCulture, $"{nineDigits:D9}{checkDigits:D2}");
        });

    // Text close to a number, to reach the parser's edge cases more often than random text does.
    private static readonly Gen<string> NumberLikeText = Gen.String[Gen.Char["0123456789 .-"], 0, 20];

    [Fact]
    public void Parsing_never_throws_and_always_explains_a_failure()
    {
        Gen.OneOf(Gen.String, NumberLikeText, ValidNumber).Sample(input =>
        {
            bool parsed = SocialSecurityIdentificationNumber.TryParse(input, out var number, out var error);

            Assert.Equal(parsed, !number.IsEmpty);
            Assert.Equal(parsed, error.Code == BelgianErrorCode.None);
            Assert.Equal(parsed, SocialSecurityIdentificationNumber.TryParse(input.AsSpan(), out _));
            Assert.Equal(parsed, error.Message.Length == 0);
        });
    }

    [Fact]
    public void Every_number_round_trips_through_every_full_format()
    {
        ValidNumber.Sample(digits =>
        {
            var number = SocialSecurityIdentificationNumber.Parse(digits);

            Assert.Equal(digits, number.ToString("N"));
            foreach (string format in Formats)
            {
                Assert.Equal(number, SocialSecurityIdentificationNumber.Parse(number.ToString(format)));
            }
        });
    }

    [Fact]
    public void The_masked_form_shows_the_check_digits_only()
    {
        ValidNumber.Sample(digits =>
            Assert.Equal($"**.**.**-***.{digits[9..]}", SocialSecurityIdentificationNumber.Parse(digits).ToString()));
    }

    [Fact]
    public void The_month_tells_the_register_and_the_check_digits_the_century()
    {
        ValidNumber.Sample(digits =>
        {
            var number = SocialSecurityIdentificationNumber.Parse(digits);
            int month = int.Parse(digits[2..4], CultureInfo.InvariantCulture);
            int day = int.Parse(digits[4..6], CultureInfo.InvariantCulture);
            int year = int.Parse(digits[..2], CultureInfo.InvariantCulture) + (IsBornFrom2000(digits) ? 2000 : 1900);

            Assert.Equal(month < 20 ? SocialSecurityIdentificationNumberKind.NationalRegister : SocialSecurityIdentificationNumberKind.Bis, number.Kind);
            Assert.Equal(
                month % 20 == 0 || day == 0 || day > DateTime.DaysInMonth(year, month % 20) ? null : new DateOnly(year, month % 20, day),
                number.BirthDate);
        });
    }

    [Fact]
    public void A_single_digit_typo_goes_undetected_only_by_changing_the_century()
    {
        Gen.Select(ValidNumber, Gen.Int[0, 10], Gen.Int[1, 9]).Sample((digits, position, shift) =>
        {
            char[] typo = digits.ToCharArray();
            typo[position] = (char)('0' + ((typo[position] - '0' + shift) % 10));

            Assert.True(
                !SocialSecurityIdentificationNumber.TryParse(new string(typo), out _) || IsBornFrom2000(digits) != IsBornFrom2000(new string(typo)),
                $"{new string(typo)} is accepted with the same computation as {digits}.");
        });
    }

    [Fact]
    public void A_swap_of_adjacent_digits_goes_undetected_only_by_changing_the_century()
    {
        Gen.Select(ValidNumber, Gen.Int[0, 9])
            .Where(sample => sample.Item1[sample.Item2] != sample.Item1[sample.Item2 + 1])
            .Sample((digits, position) =>
            {
                char[] swapped = digits.ToCharArray();
                (swapped[position], swapped[position + 1]) = (swapped[position + 1], swapped[position]);

                Assert.True(
                    !SocialSecurityIdentificationNumber.TryParse(new string(swapped), out _) || IsBornFrom2000(digits) != IsBornFrom2000(new string(swapped)),
                    $"{new string(swapped)} is accepted with the same computation as {digits}.");
            });
    }

    private static bool IsBornFrom2000(string digits)
    {
        ulong nineDigits = ulong.Parse(digits[..9], CultureInfo.InvariantCulture);
        ulong checkDigits = ulong.Parse(digits[9..], CultureInfo.InvariantCulture);

        return checkDigits == 97 - ((BornFrom2000Prefix + nineDigits) % 97);
    }
}
