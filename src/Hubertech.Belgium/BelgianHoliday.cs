using System.Globalization;
using Hubertech.Belgium.Resources;

namespace Hubertech.Belgium;

/// <summary>
/// A holiday on a given date, as returned by <see cref="BelgianCalendar.GetHolidays(int, HolidaySet)"/>.
/// </summary>
/// <remarks>
/// <c>default(BelgianHoliday)</c> is not a holiday: its <see cref="Kind"/> is
/// <see cref="HolidayKind.None"/> and its <see cref="Name"/> is empty.
/// </remarks>
public readonly record struct BelgianHoliday
{
    internal BelgianHoliday(DateOnly date, HolidayKind kind)
    {
        Date = date;
        Kind = kind;
    }

    /// <summary>
    /// Gets the date of the holiday.
    /// </summary>
    public DateOnly Date { get; }

    /// <summary>
    /// Gets the holiday, for example <see cref="HolidayKind.EasterMonday"/>.
    /// </summary>
    public HolidayKind Kind { get; }

    /// <summary>
    /// Gets the name of the holiday in <see cref="CultureInfo.CurrentUICulture"/>, for example
    /// <c>Lundi de Pâques</c> in French.
    /// </summary>
    /// <remarks>
    /// Names exist in English, French and Dutch. Regional cultures fall back to their language
    /// and other languages fall back to English.
    /// </remarks>
    public string Name => GetName(CultureInfo.CurrentUICulture);

    /// <summary>
    /// Gets the name of the holiday in the specified culture.
    /// </summary>
    /// <param name="culture">The culture of the name.</param>
    /// <returns>The localized name, or an empty string when <see cref="Kind"/> is <see cref="HolidayKind.None"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
    public string GetName(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        return Kind == HolidayKind.None ? string.Empty : HolidayNames.Get(Kind, culture);
    }
}
