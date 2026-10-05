using CsCheck;

namespace Hubertech.Belgium.Tests;

public sealed class BelgianCalendarPropertyTests
{
    // Dates far enough from the bounds of DateOnly for a move of a few hundred business days.
    private static readonly Gen<DateOnly> Date = Gen.Int[new DateOnly(1900, 1, 1).DayNumber, new DateOnly(2200, 12, 31).DayNumber]
        .Select(DateOnly.FromDayNumber);

    private static readonly Gen<HolidaySet> Set = Gen.Int[0, 31].Select(flags => (HolidaySet)flags);

    [Fact]
    public void Easter_sunday_is_a_sunday_between_22_march_and_25_april()
    {
        Gen.Int[1, 9999].Sample(year =>
        {
            DateOnly easter = BelgianCalendar.EasterSunday(year);

            Assert.Equal(DayOfWeek.Sunday, easter.DayOfWeek);
            Assert.InRange(easter, new DateOnly(year, 3, 22), new DateOnly(year, 4, 25));
        });
    }

    [Fact]
    public void A_date_is_a_holiday_exactly_when_it_is_listed_among_the_holidays_of_its_year()
    {
        Gen.Select(Date, Set).Sample((date, set) =>
        {
            bool listed = BelgianCalendar.GetHolidays(date.Year, set).Any(holiday => holiday.Date == date);

            Assert.Equal(listed, BelgianCalendar.IsHoliday(date, set));
        });
    }
}
