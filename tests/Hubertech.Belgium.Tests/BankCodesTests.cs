using System.Globalization;
using System.Text.RegularExpressions;

namespace Hubertech.Belgium.Tests;

public sealed partial class BankCodesTests
{
    [Fact]
    public void Ranges_are_sorted_disjoint_and_within_the_three_digit_codes()
    {
        int nextFree = 0;
        foreach (var range in BankCodes.Ranges)
        {
            Assert.True(range.First >= nextFree, $"{range} overlaps or follows the previous range out of order.");
            Assert.True(range.Last >= range.First, $"{range} is empty.");
            nextFree = range.Last + 1;
        }

        Assert.True(nextFree <= 1000, "The last range goes beyond code 999.");
    }

    [Fact]
    public void Every_bic_is_a_belgian_bic_of_eight_or_eleven_characters()
    {
        Assert.All(BankCodes.Ranges, range => Assert.Matches(BelgianBic(), range.Bic));
    }

    [Fact]
    public void Lookup_finds_the_range_of_every_code()
    {
        for (int code = 0; code < 1000; code++)
        {
            Assert.Equal(
                BankCodes.Ranges.SingleOrDefault(range => range.First <= code && code <= range.Last).Bic,
                BankCodes.FindBic(code));
        }
    }

    [Theory]
    // From the list of the National Bank of Belgium of 1 September 2026.
    [InlineData(0, "GEBABEBB")]
    [InlineData(49, "GEBABEBB")]
    [InlineData(50, "GKCCBEBB")]
    [InlineData(310, "BBRUBEBB")]
    [InlineData(735, "KREDBEBB")]
    [InlineData(978, "ARSPBE22")]
    [InlineData(980, "ARSPBE22")]
    [InlineData(116, "ATMNBE32XXX")]
    [InlineData(650, "REVOBEB2XXX")]
    [InlineData(102, null)]
    [InlineData(112, null)]
    [InlineData(539, null)]
    [InlineData(999, null)]
    public void Gives_the_bic_of_the_list_of_the_national_bank(int bankCode, string? bic)
    {
        var iban = WithBankCode(bankCode);

        Assert.Equal(bic, iban.Bic);
    }

    [Fact]
    public void Gives_the_bic_of_real_world_ibans()
    {
        Assert.Equal("GEBABEBB", BelgianIban.Parse("BE48 0011 2345 6727").Bic);
        Assert.Equal("BBRUBEBB", BelgianIban.Parse("BE48 3200 7018 4927").Bic);
        Assert.Equal("KREDBEBB", BelgianIban.Parse("BE31 4354 1116 1155").Bic);

        // The example of the IBAN registry of SWIFT uses a code that the list marks as unavailable.
        Assert.Null(BelgianIban.Parse("BE68 5390 0754 7034").Bic);
    }

    // A valid IBAN whose account number starts with the bank code, built independently of the
    // library: the national check digits are the remainder of the division by 97, or 97.
    private static BelgianIban WithBankCode(int bankCode)
    {
        long accountBase = bankCode * 10_000_000L;
        long remainder = accountBase % 97;

        return BelgianIban.FromLegacyAccountNumber(
            string.Create(CultureInfo.InvariantCulture, $"{accountBase:D10}{(remainder == 0 ? 97 : remainder):D2}"));
    }

    [GeneratedRegex("^[A-Z]{4}BE[A-Z0-9]{2}([A-Z0-9]{3})?$")]
    private static partial Regex BelgianBic();
}
