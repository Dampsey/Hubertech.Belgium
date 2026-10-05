using System.Collections;
using System.Globalization;
using System.Resources;
using Hubertech.Belgium.Resources;

namespace Hubertech.Belgium.Tests;

public sealed class BelgianHolidayTests
{
    public static TheoryData<string> Translations => ["fr", "nl"];

    [Theory]
    [InlineData("en", "Easter Monday")]
    [InlineData("fr-BE", "Lundi de Pâques")]
    [InlineData("nl-BE", "Paasmaandag")]
    [InlineData("de-BE", "Easter Monday")]
    public void Name_is_localized(string cultureName, string expected)
    {
        var easterMonday = BelgianCalendar.GetHolidays(2026)[1];

        Assert.Equal(expected, easterMonday.GetName(CultureInfo.GetCultureInfo(cultureName)));
    }

    [Fact]
    public void Name_follows_the_current_UI_culture()
    {
        var nationalDay = BelgianCalendar.GetHolidays(2026)[5];
        var originalCulture = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-BE");

            Assert.Equal("Fête nationale", nationalDay.Name);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void Default_value_is_not_a_holiday()
    {
        BelgianHoliday holiday = default;

        Assert.Equal(HolidayKind.None, holiday.Kind);
        Assert.Equal(string.Empty, holiday.Name);
    }

    [Fact]
    public void Getting_a_name_requires_a_culture()
    {
        var holiday = BelgianCalendar.GetHolidays(2026)[0];

        Assert.Throws<ArgumentNullException>(() => holiday.GetName(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("fr")]
    [InlineData("nl")]
    public void Every_holiday_has_a_name_in_every_language(string language)
    {
        var names = ReadNames(CultureInfo.GetCultureInfo(language));
        var kinds = Enum.GetValues<HolidayKind>().Where(kind => kind != HolidayKind.None).Select(kind => kind.ToString());

        Assert.Equal(kinds.Order(StringComparer.Ordinal), names.Keys.Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Translations))]
    public void Every_name_is_translated(string language)
    {
        var english = ReadNames(CultureInfo.InvariantCulture);
        var translated = ReadNames(CultureInfo.GetCultureInfo(language));

        Assert.All(english, name => Assert.NotEqual(name.Value, translated[name.Key]));
    }

    private static Dictionary<string, string> ReadNames(CultureInfo culture)
    {
        ResourceSet resources = HolidayNames.ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)
            ?? throw new InvalidOperationException($"No resources for culture '{culture.Name}'.");

        return resources.Cast<DictionaryEntry>().ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);
    }
}
