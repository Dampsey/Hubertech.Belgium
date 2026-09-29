namespace Hubertech.Belgium;

/// <summary>
/// Helpers shared by the identifiers, which store their digits as an integer.
/// </summary>
internal static class Digits
{
    /// <summary>
    /// Writes <paramref name="value"/> in <paramref name="destination"/>, right-aligned and
    /// padded with leading zeros to the length of <paramref name="destination"/>.
    /// </summary>
    internal static void Write(Span<char> destination, ulong value)
    {
        for (int i = destination.Length - 1; i >= 0; i--)
        {
            destination[i] = (char)('0' + (value % 10));
            value /= 10;
        }
    }
}
