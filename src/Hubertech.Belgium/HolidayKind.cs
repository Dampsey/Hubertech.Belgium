namespace Hubertech.Belgium;

/// <summary>
/// Identifies a Belgian holiday.
/// </summary>
/// <remarks>
/// Values are stable: they are never renumbered nor reused, so they can be stored. New holidays
/// may be added in future versions; code that switches on this enum should handle unknown values.
/// </remarks>
public enum HolidayKind
{
    /// <summary>
    /// No holiday. This is the kind of <c>default(BelgianHoliday)</c>.
    /// </summary>
    None = 0,

    /// <summary>1 January.</summary>
    NewYearsDay = 1,

    /// <summary>The day after Easter Sunday.</summary>
    EasterMonday = 2,

    /// <summary>1 May.</summary>
    LabourDay = 3,

    /// <summary>39 days after Easter Sunday, a Thursday.</summary>
    AscensionDay = 4,

    /// <summary>50 days after Easter Sunday, the Monday after Pentecost.</summary>
    WhitMonday = 5,

    /// <summary>21 July.</summary>
    NationalDay = 6,

    /// <summary>15 August.</summary>
    AssumptionDay = 7,

    /// <summary>1 November.</summary>
    AllSaintsDay = 8,

    /// <summary>11 November.</summary>
    ArmisticeDay = 9,

    /// <summary>25 December.</summary>
    ChristmasDay = 10,

    /// <summary>11 July, day of the Flemish Community.</summary>
    FlemishCommunityDay = 11,

    /// <summary>27 September, day of the French Community (Wallonia-Brussels Federation).</summary>
    FrenchCommunityDay = 12,

    /// <summary>15 November, day of the German-speaking Community.</summary>
    GermanSpeakingCommunityDay = 13,

    /// <summary>2 November, a day off for the federal public services.</summary>
    AllSoulsDay = 14,

    /// <summary>15 November, a day off for the federal public services.</summary>
    KingsFeast = 15,

    /// <summary>26 December, a day off for the federal public services.</summary>
    BoxingDay = 16,
}
