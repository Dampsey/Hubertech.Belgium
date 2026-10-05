namespace Hubertech.Belgium.Tests;

// Holiday kinds and sets can be stored by consumers: a value must never change.
// Adding a member means adding a line here, with the next free value.
public sealed class StableValuesTests
{
    [Theory]
    [InlineData(HolidayKind.None, 0)]
    [InlineData(HolidayKind.NewYearsDay, 1)]
    [InlineData(HolidayKind.EasterMonday, 2)]
    [InlineData(HolidayKind.LabourDay, 3)]
    [InlineData(HolidayKind.AscensionDay, 4)]
    [InlineData(HolidayKind.WhitMonday, 5)]
    [InlineData(HolidayKind.NationalDay, 6)]
    [InlineData(HolidayKind.AssumptionDay, 7)]
    [InlineData(HolidayKind.AllSaintsDay, 8)]
    [InlineData(HolidayKind.ArmisticeDay, 9)]
    [InlineData(HolidayKind.ChristmasDay, 10)]
    [InlineData(HolidayKind.FlemishCommunityDay, 11)]
    [InlineData(HolidayKind.FrenchCommunityDay, 12)]
    [InlineData(HolidayKind.GermanSpeakingCommunityDay, 13)]
    [InlineData(HolidayKind.AllSoulsDay, 14)]
    [InlineData(HolidayKind.KingsFeast, 15)]
    [InlineData(HolidayKind.BoxingDay, 16)]
    public void Holiday_kind_value_never_changes(HolidayKind kind, int value)
    {
        Assert.Equal(value, (int)kind);
    }

    [Theory]
    [InlineData(HolidaySet.None, 0)]
    [InlineData(HolidaySet.Legal, 1)]
    [InlineData(HolidaySet.FlemishCommunity, 2)]
    [InlineData(HolidaySet.FrenchCommunity, 4)]
    [InlineData(HolidaySet.GermanSpeakingCommunity, 8)]
    [InlineData(HolidaySet.FederalPublicService, 16)]
    public void Holiday_set_value_never_changes(HolidaySet set, int value)
    {
        Assert.Equal(value, (int)set);
    }

    [Fact]
    public void Every_value_is_pinned_by_a_test()
    {
        Assert.Equal(17, Enum.GetValues<HolidayKind>().Length);
        Assert.Equal(6, Enum.GetValues<HolidaySet>().Length);
    }
}
