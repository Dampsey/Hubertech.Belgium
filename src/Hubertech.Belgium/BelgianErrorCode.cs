namespace Hubertech.Belgium;

/// <summary>
/// Identifies why a value could not be parsed.
/// </summary>
/// <remarks>
/// Values are stable: they are never renumbered nor reused, so they can be stored,
/// logged or sent to clients. New values may be added in future versions; code that
/// switches on this enum should handle unknown values.
/// </remarks>
public enum BelgianErrorCode
{
    /// <summary>
    /// No error. This is the code of <c>default(BelgianValidationError)</c>, returned
    /// alongside a successful parse.
    /// </summary>
    None = 0,

    /// <summary>
    /// The input is <see langword="null"/>, empty, or contains only separators and white space.
    /// </summary>
    Empty = 1,

    /// <summary>
    /// The input contains a character that is neither a digit nor an accepted separator.
    /// <see cref="BelgianValidationError.Position"/> gives its index in the input.
    /// </summary>
    InvalidCharacter = 2,

    /// <summary>
    /// The input does not contain the expected number of digits.
    /// </summary>
    InvalidLength = 3,

    /// <summary>
    /// The input starts with a country code other than <c>BE</c>.
    /// </summary>
    InvalidCountryCode = 4,

    /// <summary>
    /// The first digit is not allowed, for example an enterprise number that does not
    /// start with 0 or 1.
    /// </summary>
    InvalidFirstDigit = 5,

    /// <summary>
    /// The check digits do not match the other digits, which usually reveals a typing error.
    /// <see cref="BelgianValidationError.Expected"/> gives the check digits that would match.
    /// </summary>
    InvalidChecksum = 6,
}
