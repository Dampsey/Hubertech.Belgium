namespace Hubertech.Belgium.Tests;

public sealed class BelgianCalendarTests
{
    // Easter Sunday from 2020 to 2035, checked against python-dateutil's easter().
    [Theory]
    [InlineData(2020, 4, 12)]
    [InlineData(2021, 4, 4)]
    [InlineData(2022, 4, 17)]
    [InlineData(2023, 4, 9)]
    [InlineData(2024, 3, 31)]
    [InlineData(2025, 4, 20)]
    [InlineData(2026, 4, 5)]
    [InlineData(2027, 3, 28)]
    [InlineData(2028, 4, 16)]
    [InlineData(2029, 4, 1)]
    [InlineData(2030, 4, 21)]
    [InlineData(2031, 4, 13)]
    [InlineData(2032, 3, 28)]
    [InlineData(2033, 4, 17)]
    [InlineData(2034, 4, 9)]
    [InlineData(2035, 3, 25)]
    public void Computes_easter_sunday(int year, int month, int day)
    {
        Assert.Equal(new DateOnly(year, month, day), BelgianCalendar.EasterSunday(year));
    }

    [Fact]
    public void Gives_the_ten_legal_holidays_of_a_year_in_date_order()
    {
        var holidays = BelgianCalendar.GetHolidays(2026);

        Assert.Equal(
            [
                (new DateOnly(2026, 1, 1), HolidayKind.NewYearsDay),
                (new DateOnly(2026, 4, 6), HolidayKind.EasterMonday),
                (new DateOnly(2026, 5, 1), HolidayKind.LabourDay),
                (new DateOnly(2026, 5, 14), HolidayKind.AscensionDay),
                (new DateOnly(2026, 5, 25), HolidayKind.WhitMonday),
                (new DateOnly(2026, 7, 21), HolidayKind.NationalDay),
                (new DateOnly(2026, 8, 15), HolidayKind.AssumptionDay),
                (new DateOnly(2026, 11, 1), HolidayKind.AllSaintsDay),
                (new DateOnly(2026, 11, 11), HolidayKind.ArmisticeDay),
                (new DateOnly(2026, 12, 25), HolidayKind.ChristmasDay),
            ],
            holidays.Select(holiday => (holiday.Date, holiday.Kind)));
    }

    [Theory]
    [InlineData(HolidaySet.FlemishCommunity, HolidayKind.FlemishCommunityDay, 7, 11)]
    [InlineData(HolidaySet.FrenchCommunity, HolidayKind.FrenchCommunityDay, 9, 27)]
    [InlineData(HolidaySet.GermanSpeakingCommunity, HolidayKind.GermanSpeakingCommunityDay, 11, 15)]
    public void Gives_the_day_of_a_community_only_when_asked(HolidaySet set, HolidayKind kind, int month, int day)
    {
        var date = new DateOnly(2026, month, day);

        Assert.Equal([new BelgianHoliday(date, kind)], BelgianCalendar.GetHolidays(2026, set));
        Assert.False(BelgianCalendar.IsHoliday(date));
        Assert.True(BelgianCalendar.IsHoliday(date, set));
    }

    [Fact]
    public void Gives_the_days_off_of_the_federal_public_services_on_top_of_the_legal_holidays()
    {
        var holidays = BelgianCalendar.GetHolidays(2026, HolidaySet.Legal | HolidaySet.FederalPublicService);

        Assert.Equal(13, holidays.Count);
        Assert.Contains(new BelgianHoliday(new DateOnly(2026, 11, 2), HolidayKind.AllSoulsDay), holidays);
        Assert.Contains(new BelgianHoliday(new DateOnly(2026, 11, 15), HolidayKind.KingsFeast), holidays);
        Assert.Contains(new BelgianHoliday(new DateOnly(2026, 12, 26), HolidayKind.BoxingDay), holidays);
    }

    [Fact]
    public void Gives_both_holidays_of_15_november_when_both_sets_are_asked()
    {
        var holidays = BelgianCalendar.GetHolidays(2026, HolidaySet.GermanSpeakingCommunity | HolidaySet.FederalPublicService);

        Assert.Equal(
            [HolidayKind.AllSoulsDay, HolidayKind.GermanSpeakingCommunityDay, HolidayKind.KingsFeast, HolidayKind.BoxingDay],
            holidays.Select(holiday => holiday.Kind));
    }

    [Fact]
    public void Gives_no_holiday_for_an_empty_set()
    {
        Assert.Empty(BelgianCalendar.GetHolidays(2026, HolidaySet.None));
        Assert.False(BelgianCalendar.IsHoliday(new DateOnly(2026, 1, 1), HolidaySet.None));
    }

    [Theory]
    [InlineData(2026, 4, 6, true)]
    [InlineData(2026, 4, 3, false)]
    [InlineData(2026, 5, 14, true)]
    [InlineData(2026, 5, 25, true)]
    [InlineData(2026, 11, 1, true)]
    [InlineData(2026, 11, 2, false)]
    public void Recognizes_legal_holidays_even_on_weekends(int year, int month, int day, bool expected)
    {
        Assert.Equal(expected, BelgianCalendar.IsHoliday(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10_000)]
    public void Rejects_a_year_outside_the_range_of_dates(int year)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BelgianCalendar.GetHolidays(year));
    }

    [Fact]
    public void Rejects_an_unknown_holiday_set()
    {
        var unknown = (HolidaySet)64;
        var date = new DateOnly(2026, 1, 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => BelgianCalendar.GetHolidays(2026, unknown));
        Assert.Throws<ArgumentOutOfRangeException>(() => BelgianCalendar.IsHoliday(date, unknown));
    }

    [Fact]
    public void Holidays_are_computed_up_to_the_last_year_of_the_range_of_dates()
    {
        Assert.Equal(10, BelgianCalendar.GetHolidays(9999).Count);
        Assert.Equal(10, BelgianCalendar.GetHolidays(1).Count);
    }
}
