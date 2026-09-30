namespace Hubertech.Belgium;

/// <summary>
/// The Belgian mod 97 check shared by structured communications and Belgian account numbers.
/// </summary>
internal static class Mod97
{
    /// <summary>
    /// Gets the check digits of <paramref name="number"/>: the remainder of its division by 97,
    /// except that 0 becomes 97, so that the check digits are never <c>00</c>.
    /// </summary>
    internal static uint CheckDigits(ulong number)
    {
        uint remainder = (uint)(number % 97);

        return remainder == 0 ? 97 : remainder;
    }
}
