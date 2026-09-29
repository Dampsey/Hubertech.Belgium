using System.Diagnostics;
using System.Globalization;
using Hubertech.Belgium.Resources;

namespace Hubertech.Belgium;

/// <summary>
/// Describes why a value could not be parsed, with a localized message that can be shown
/// to end users as is.
/// </summary>
/// <remarks>
/// <para>
/// <c>default(BelgianValidationError)</c> means "no error": its <see cref="Code"/> is
/// <see cref="BelgianErrorCode.None"/> and its <see cref="Message"/> is empty. This is the
/// value returned alongside a successful parse.
/// </para>
/// <para>
/// Creating an error does not allocate. The message is built each time <see cref="Message"/>
/// or <see cref="GetMessage(CultureInfo)"/> is read, so it follows the culture current at
/// that moment, not the one current when the error occurred.
/// </para>
/// </remarks>
public readonly record struct BelgianValidationError
{
    private readonly string? _typeName;
    private readonly char _character;
    private readonly byte? _expectedCheckDigits;

    private BelgianValidationError(
        string typeName,
        BelgianErrorCode code,
        int? position = null,
        char character = default,
        byte? expectedCheckDigits = null)
    {
        _typeName = typeName;
        Code = code;
        Position = position;
        _character = character;
        _expectedCheckDigits = expectedCheckDigits;
    }

    /// <summary>
    /// Gets the reason of the failure.
    /// </summary>
    public BelgianErrorCode Code { get; }

    /// <summary>
    /// Gets the name of the type whose parser failed, for example <c>EnterpriseNumber</c>,
    /// or an empty string when there is no error.
    /// </summary>
    public string TypeName => _typeName ?? string.Empty;

    /// <summary>
    /// Gets the zero-based index of the offending character in the input, when
    /// <see cref="Code"/> is <see cref="BelgianErrorCode.InvalidCharacter"/>; otherwise
    /// <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// The index refers to the input as given, separators included. The message shows it
    /// one-based, as users count.
    /// </remarks>
    public int? Position { get; }

    /// <summary>
    /// Gets the two check digits that would match the other digits, when <see cref="Code"/> is
    /// <see cref="BelgianErrorCode.InvalidChecksum"/>; otherwise <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// A checksum mismatch does not tell which digit is wrong, and a typing error is more
    /// likely among the other digits than in the check digits themselves. This value is meant
    /// for diagnostics and support. It is deliberately left out of <see cref="Message"/>:
    /// suggesting it to end users would lead them to turn a mistyped number into a valid but
    /// wrong one.
    /// </remarks>
    public string? Expected => _expectedCheckDigits?.ToString("00", CultureInfo.InvariantCulture);

    /// <summary>
    /// Gets the message describing the error in <see cref="CultureInfo.CurrentUICulture"/>,
    /// or an empty string when there is no error.
    /// </summary>
    /// <remarks>
    /// Messages exist in English, French and Dutch. Regional cultures fall back to their
    /// language (<c>fr-BE</c> to French), and other languages fall back to English.
    /// </remarks>
    public string Message => GetMessage(CultureInfo.CurrentUICulture);

    /// <summary>
    /// Gets the message describing the error in the specified culture, or an empty string when
    /// there is no error.
    /// </summary>
    /// <param name="culture">The culture of the message, for example the culture of an HTTP request.</param>
    /// <returns>The localized message.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="culture"/> is <see langword="null"/>.</exception>
    public string GetMessage(CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        if (Code == BelgianErrorCode.None)
        {
            return string.Empty;
        }

        string template = ErrorMessages.GetTemplate(TypeName, Code, culture);

        return Code == BelgianErrorCode.InvalidCharacter
            ? string.Format(culture, template, DisplayCharacter(_character), Position + 1)
            : template;
    }

    internal static BelgianValidationError Empty(string typeName) =>
        new(typeName, BelgianErrorCode.Empty);

    internal static BelgianValidationError InvalidCharacter(string typeName, int position, char character) =>
        new(typeName, BelgianErrorCode.InvalidCharacter, position, character);

    internal static BelgianValidationError InvalidLength(string typeName) =>
        new(typeName, BelgianErrorCode.InvalidLength);

    internal static BelgianValidationError InvalidCountryCode(string typeName) =>
        new(typeName, BelgianErrorCode.InvalidCountryCode);

    internal static BelgianValidationError InvalidFirstDigit(string typeName) =>
        new(typeName, BelgianErrorCode.InvalidFirstDigit);

    internal static BelgianValidationError InvalidChecksum(string typeName, int expectedCheckDigits)
    {
        Debug.Assert(expectedCheckDigits is >= 0 and <= 99, "Check digits are two decimal digits.");

        return new(typeName, BelgianErrorCode.InvalidChecksum, expectedCheckDigits: (byte)expectedCheckDigits);
    }

    // Invisible characters (control characters, non-breaking spaces, lone surrogates) would
    // make the message look like it quotes nothing, so they are shown as a code point.
    private static string DisplayCharacter(char character) =>
        char.IsControl(character) || char.IsWhiteSpace(character) || char.IsSurrogate(character)
            ? string.Create(CultureInfo.InvariantCulture, $"U+{(int)character:X4}")
            : character.ToString();
}
