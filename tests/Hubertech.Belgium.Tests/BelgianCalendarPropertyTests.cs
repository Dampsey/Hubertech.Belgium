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

    [Fact]
    public void Adding_business_days_lands_on_a_business_day_that_counting_finds_again()
    {
        Gen.Select(Date, Gen.Int[-300, 300], Set).Sample((start, days, set) =>
        {
            DateOnly end = BelgianCalendar.AddBusinessDays(start, days, set);

            Assert.Equal(days, BelgianCalendar.CountBusinessDays(start, end, set));
            Assert.True(days == 0 || BelgianCalendar.IsBusinessDay(end, set));
        });
    }

    [Fact]
    public void Counting_backward_differs_from_counting_forward_only_by_the_bounds()
    {
        // Forward, the later date counts and the earlier does not; backward, the reverse.
        // So the two counts cancel out, except for the business days among the bounds.
        Gen.Select(Date, Date, Set).Sample((first, second, set) =>
        {
            DateOnly earlier = first < second ? first : second;
            DateOnly later = first < second ? second : first;

            Assert.Equal(
                BusinessDay(later, set) - BusinessDay(earlier, set),
                BelgianCalendar.CountBusinessDays(first, second, set) + BelgianCalendar.CountBusinessDays(second, first, set));
        });
    }

    private static int BusinessDay(DateOnly date, HolidaySet set) => BelgianCalendar.IsBusinessDay(date, set) ? 1 : 0;
}
