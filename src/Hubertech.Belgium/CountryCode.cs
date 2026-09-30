namespace Hubertech.Belgium;

/// <summary>
/// Detection of the two-letter country code that may precede an identifier.
/// </summary>
internal static class CountryCode
{
    /// <summary>
    /// Determines whether exactly two letters start at <paramref name="index"/>:
    /// <c>BE0202239951</c> starts with a country code, <c>TVA BE0202239951</c> does not.
    /// </summary>
    internal static bool StartsAt(ReadOnlySpan<char> s, int index) =>
        index + 1 < s.Length
        && char.IsAsciiLetter(s[index])
        && char.IsAsciiLetter(s[index + 1])
        && (index + 2 == s.Length || !char.IsAsciiLetter(s[index + 2]));

    /// <summary>
    /// Determines whether the two letters at <paramref name="index"/> are <c>BE</c>, in any case.
    /// </summary>
    internal static bool IsBelgium(ReadOnlySpan<char> s, int index) =>
        s.Slice(index, 2).Equals("BE", StringComparison.OrdinalIgnoreCase);
}
