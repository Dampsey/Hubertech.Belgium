using System.Collections;
using System.Globalization;
using System.Resources;
using Hubertech.Belgium.Resources;

namespace Hubertech.Belgium.Tests;

/// <summary>
/// Guards the message catalog: a key missing from a translation would silently fall back
/// to English, and a malformed key or placeholder would only fail at run time.
/// </summary>
public sealed class ErrorMessagesTests
{
    // What a message can talk about: a parsed type, or the legacy account number that
    // BelgianIban.FromLegacyAccountNumber converts.
    private static readonly string[] Subjects =
        [nameof(EnterpriseNumber), nameof(StructuredCommunication), nameof(BelgianIban), "BelgianAccountNumber", nameof(SocialSecurityIdentificationNumber)];

    public static TheoryData<string> Translations => ["fr", "nl"];

    [Fact]
    public void Every_key_names_a_subject_and_an_error_code()
    {
        foreach (string key in ReadMessages(CultureInfo.InvariantCulture).Keys)
        {
            string[] parts = key.Split('_');

            Assert.Equal(2, parts.Length);
            Assert.Contains(parts[0], Subjects);
            Assert.True(
                Enum.TryParse(parts[1], out BelgianErrorCode code) && code != BelgianErrorCode.None,
                $"'{key}' does not end with an error code.");
        }
    }

    [Theory]
    [MemberData(nameof(Translations))]
    public void Every_message_is_translated(string language)
    {
        var english = ReadMessages(CultureInfo.InvariantCulture);
        var translated = ReadMessages(CultureInfo.GetCultureInfo(language));

        Assert.Equal(english.Keys.Order(StringComparer.Ordinal), translated.Keys.Order(StringComparer.Ordinal));
        Assert.All(english, message => Assert.NotEqual(message.Value, translated[message.Key]));
    }

    [Theory]
    [InlineData("")]
    [InlineData("fr")]
    [InlineData("nl")]
    public void Only_invalid_character_messages_have_placeholders(string language)
    {
        foreach (var (key, message) in ReadMessages(CultureInfo.GetCultureInfo(language)))
        {
            if (key.EndsWith($"_{BelgianErrorCode.InvalidCharacter}", StringComparison.Ordinal))
            {
                Assert.Contains("{0}", message, StringComparison.Ordinal);
                Assert.Contains("{1}", message, StringComparison.Ordinal);
            }
            else
            {
                Assert.DoesNotContain("{", message, StringComparison.Ordinal);
            }
        }
    }

    private static Dictionary<string, string> ReadMessages(CultureInfo culture)
    {
        ResourceSet resources = ErrorMessages.ResourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)
            ?? throw new InvalidOperationException($"No resources for culture '{culture.Name}'.");

        return resources.Cast<DictionaryEntry>().ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value!);
    }
}
