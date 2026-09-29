using System.Diagnostics;
using System.Globalization;
using System.Resources;

namespace Hubertech.Belgium.Resources;

/// <summary>
/// Localized message templates of <see cref="BelgianValidationError"/>, stored in
/// <c>ErrorMessages.resx</c> (English, neutral) and its French and Dutch satellites.
/// </summary>
/// <remarks>
/// Each template is a full sentence identified by <c>{TypeName}_{Code}</c>. Sentences are never
/// assembled from fragments, because grammatical gender and word order differ between languages.
/// </remarks>
internal static class ErrorMessages
{
    internal static ResourceManager ResourceManager { get; } = new(typeof(ErrorMessages));

    internal static string GetTemplate(string typeName, BelgianErrorCode code, CultureInfo culture)
    {
        string key = $"{typeName}_{code}";

        return ResourceManager.GetString(key, culture)
            ?? throw new UnreachableException($"No message is defined for '{key}'.");
    }
}
