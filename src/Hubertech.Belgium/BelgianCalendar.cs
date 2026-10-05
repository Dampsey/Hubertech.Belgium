namespace Hubertech.Belgium;

/// <summary>
/// Belgian holidays.
/// </summary>
/// <remarks>
/// <para>
/// By default, only the ten legal holidays are taken into account.
/// Moveable holidays are computed from Easter Sunday, itself computed with the anonymous
/// Gregorian algorithm (Meeus, Jones, Butcher); there is no table of dates.
/// </para>
/// <para>
/// The rules in force today are applied to every year: the calendar does not model the history
/// of Belgian holidays, for example the community holidays created in 1990 and 1991.
/// </para>
/// <para>
/// A holiday that falls on a Saturday or a Sunday stays on that day. The day off that labour law
/// grants in replacement is set by each employer, so it is not a calendar rule and is not
/// modelled; neither are the replacement days of the federal public services between 27 and 31
/// December.
/// </para>
/// </remarks>
/// <seealso href="https://www.ejustice.just.fgov.be/eli/arrete/1974/04/18/1974041801/justel">Royal Decree of 18 April 1974 on public holidays (consolidated text)</seealso>
/// <seealso href="https://www.ejustice.just.fgov.be/cgi_loi/change_lg_2.pl?language=fr&amp;nm=1998002123&amp;la=F">Royal Decree of 19 November 1998 on the leave of federal staff (consolidated text)</seealso>
public static class BelgianCalendar
{
    private const HolidaySet KnownSets =
        HolidaySet.Legal
        | HolidaySet.FlemishCommunity
        | HolidaySet.FrenchCommunity
        | HolidaySet.GermanSpeakingCommunity
        | HolidaySet.FederalPublicService;

    private static readonly HolidayRule[] Rules =
    [
        HolidayRule.Fixed(HolidayKind.NewYearsDay, HolidaySet.Legal, 1, 1),
        HolidayRule.AfterEaster(HolidayKind.EasterMonday, HolidaySet.Legal, 1),
        HolidayRule.Fixed(HolidayKind.LabourDay, HolidaySet.Legal, 5, 1),
        HolidayRule.AfterEaster(HolidayKind.AscensionDay, HolidaySet.Legal, 39),
        HolidayRule.AfterEaster(HolidayKind.WhitMonday, HolidaySet.Legal, 50),
        HolidayRule.Fixed(HolidayKind.NationalDay, HolidaySet.Legal, 7, 21),
        HolidayRule.Fixed(HolidayKind.AssumptionDay, HolidaySet.Legal, 8, 15),
        HolidayRule.Fixed(HolidayKind.AllSaintsDay, HolidaySet.Legal, 11, 1),
        HolidayRule.Fixed(HolidayKind.ArmisticeDay, HolidaySet.Legal, 11, 11),
        HolidayRule.Fixed(HolidayKind.ChristmasDay, HolidaySet.Legal, 12, 25),
        HolidayRule.Fixed(HolidayKind.FlemishCommunityDay, HolidaySet.FlemishCommunity, 7, 11),
        HolidayRule.Fixed(HolidayKind.FrenchCommunityDay, HolidaySet.FrenchCommunity, 9, 27),
        HolidayRule.Fixed(HolidayKind.GermanSpeakingCommunityDay, HolidaySet.GermanSpeakingCommunity, 11, 15),
        HolidayRule.Fixed(HolidayKind.AllSoulsDay, HolidaySet.FederalPublicService, 11, 2),
        HolidayRule.Fixed(HolidayKind.KingsFeast, HolidaySet.FederalPublicService, 11, 15),
        HolidayRule.Fixed(HolidayKind.BoxingDay, HolidaySet.FederalPublicService, 12, 26),
    ];

    /// <summary>
    /// Gets the holidays of a year.
    /// </summary>
    /// <param name="year">The year, from 1 to 9999.</param>
    /// <param name="set">The holidays to include. Only the legal holidays by default.</param>
    /// <returns>
    /// The holidays, ordered by date. Two holidays can fall on the same date: with
    /// <see cref="HolidaySet.GermanSpeakingCommunity"/> and <see cref="HolidaySet.FederalPublicService"/>,
    /// 15 November appears twice, once for each.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="year"/> is outside 1 to 9999, or <paramref name="set"/> contains an unknown value.</exception>
    public static IReadOnlyList<BelgianHoliday> GetHolidays(int year, HolidaySet set = HolidaySet.Legal)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, DateOnly.MinValue.Year);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, DateOnly.MaxValue.Year);
        ValidateSet(set);

        DateOnly easter = EasterSunday(year);
        var holidays = new List<BelgianHoliday>(Rules.Length);
        foreach (HolidayRule rule in Rules)
        {
            if ((rule.Set & set) != 0)
            {
                holidays.Add(new BelgianHoliday(rule.DateIn(year, easter), rule.Kind));
            }
        }

        holidays.Sort(static (left, right) =>
            left.Date != right.Date ? left.Date.CompareTo(right.Date) : left.Kind.CompareTo(right.Kind));

        return holidays.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a date is a holiday.
    /// </summary>
    /// <param name="date">The date.</param>
    /// <param name="set">The holidays to take into account. Only the legal holidays by default.</param>
    /// <returns><see langword="true"/> if <paramref name="date"/> is a holiday of <paramref name="set"/>, even on a weekend; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="set"/> contains an unknown value.</exception>
    public static bool IsHoliday(DateOnly date, HolidaySet set = HolidaySet.Legal)
    {
        ValidateSet(set);

        return IsHolidayCore(date, set);
    }

    /// <summary>
    /// Computes Easter Sunday in the Gregorian calendar with the anonymous algorithm published
    /// by Meeus, Jones and Butcher.
    /// </summary>
    internal static DateOnly EasterSunday(int year)
    {
        int a = year % 19;
        int b = year / 100;
        int c = year % 100;
        int d = b / 4;
        int e = b % 4;
        int f = (b + 8) / 25;
        int g = (b - f + 1) / 3;
        int h = ((19 * a) + b - d - g + 15) % 30;
        int i = c / 4;
        int k = c % 4;
        int l = (32 + (2 * e) + (2 * i) - h - k) % 7;
        int m = (a + (11 * h) + (22 * l)) / 451;
        int month = (h + l - (7 * m) + 114) / 31;
        int day = ((h + l - (7 * m) + 114) % 31) + 1;

        return new DateOnly(year, month, day);
    }

    private static bool IsHolidayCore(DateOnly date, HolidaySet set)
    {
        DateOnly easter = EasterSunday(date.Year);
        foreach (HolidayRule rule in Rules)
        {
            if ((rule.Set & set) != 0 && rule.DateIn(date.Year, easter) == date)
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateSet(HolidaySet set)
    {
        if ((set & ~KnownSets) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(set), set, "The holiday set contains an unknown value.");
        }
    }

    // A holiday is either on a fixed day of the year, or a number of days after Easter Sunday.
    private readonly struct HolidayRule
    {
        private readonly int _month;
        private readonly int _day;
        private readonly int _daysAfterEaster;

        private HolidayRule(HolidayKind kind, HolidaySet set, int month, int day, int daysAfterEaster)
        {
            Kind = kind;
            Set = set;
            _month = month;
            _day = day;
            _daysAfterEaster = daysAfterEaster;
        }

        public HolidayKind Kind { get; }

        public HolidaySet Set { get; }

        public static HolidayRule Fixed(HolidayKind kind, HolidaySet set, int month, int day) =>
            new(kind, set, month, day, 0);

        public static HolidayRule AfterEaster(HolidayKind kind, HolidaySet set, int days) =>
            new(kind, set, 0, 0, days);

        public DateOnly DateIn(int year, DateOnly easter) =>
            _month == 0 ? easter.AddDays(_daysAfterEaster) : new DateOnly(year, _month, _day);
    }
}
