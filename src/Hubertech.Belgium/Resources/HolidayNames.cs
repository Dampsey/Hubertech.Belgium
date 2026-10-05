using System.Diagnostics;
using System.Globalization;
using System.Resources;

namespace Hubertech.Belgium.Resources;

/// <summary>
/// Localized names of the holidays, stored in <c>HolidayNames.resx</c> (English, neutral) and
/// its French and Dutch satellites, under the name of the <see cref="HolidayKind"/> member.
/// </summary>
internal static class HolidayNames
{
    internal static ResourceManager ResourceManager { get; } = new(typeof(HolidayNames));

    internal static string Get(HolidayKind kind, CultureInfo culture)
    {
        string key = kind.ToString();

        return ResourceManager.GetString(key, culture)
            ?? throw new UnreachableException($"No name is defined for '{key}'.");
    }
}
