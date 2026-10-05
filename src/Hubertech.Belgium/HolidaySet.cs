namespace Hubertech.Belgium;

/// <summary>
/// The days off that <see cref="BelgianCalendar"/> takes into account. Sets combine with the
/// <c>|</c> operator, for example <c>HolidaySet.Legal | HolidaySet.FlemishCommunity</c>.
/// </summary>
/// <remarks>
/// <para>
/// Only <see cref="Legal"/> holidays are days off for every worker. The other sets are days off
/// for some employers only, mostly public services: include them when they apply.
/// </para>
/// <para>
/// Values are stable flags: they are never renumbered nor reused. New sets may be added in
/// future versions.
/// </para>
/// </remarks>
[Flags]
public enum HolidaySet
{
    /// <summary>
    /// No holiday: only Saturdays and Sundays are not business days.
    /// </summary>
    None = 0,

    /// <summary>
    /// The ten public holidays set by the Royal Decree of 18 April 1974: New Year's Day, Easter
    /// Monday, Labour Day, Ascension Day, Whit Monday, National Day, Assumption Day, All Saints'
    /// Day, Armistice Day and Christmas Day.
    /// </summary>
    Legal = 1,

    /// <summary>
    /// 11 July, day of the Flemish Community (decree of 7 November 1990).
    /// </summary>
    FlemishCommunity = 2,

    /// <summary>
    /// 27 September, day of the French Community, also called Wallonia-Brussels Federation
    /// (decree of 3 July 1991).
    /// </summary>
    FrenchCommunity = 4,

    /// <summary>
    /// 15 November, day of the German-speaking Community.
    /// </summary>
    GermanSpeakingCommunity = 8,

    /// <summary>
    /// 2 November, 15 November (King's Feast) and 26 December: days off for the staff of the
    /// federal public services on top of the legal holidays (Royal Decree of 19 November 1998,
    /// article 14). The afternoon of 22 July, also off, is not a full day and is not included.
    /// </summary>
    FederalPublicService = 16,
}
