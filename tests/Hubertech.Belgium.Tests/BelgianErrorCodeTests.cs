namespace Hubertech.Belgium.Tests;

public sealed class BelgianErrorCodeTests
{
    // Error codes are stored and sent to clients by consumers: a value must never change.
    // Adding a code means adding a line here, with the next free number.
    [Theory]
    [InlineData(BelgianErrorCode.None, 0)]
    [InlineData(BelgianErrorCode.Empty, 1)]
    [InlineData(BelgianErrorCode.InvalidCharacter, 2)]
    [InlineData(BelgianErrorCode.InvalidLength, 3)]
    [InlineData(BelgianErrorCode.InvalidCountryCode, 4)]
    [InlineData(BelgianErrorCode.InvalidFirstDigit, 5)]
    [InlineData(BelgianErrorCode.InvalidChecksum, 6)]
    [InlineData(BelgianErrorCode.InvalidBirthDate, 7)]
    public void Value_never_changes(BelgianErrorCode code, int value)
    {
        Assert.Equal(value, (int)code);
    }

    [Fact]
    public void Every_value_is_pinned_by_a_test()
    {
        Assert.Equal(8, Enum.GetValues<BelgianErrorCode>().Length);
    }
}
