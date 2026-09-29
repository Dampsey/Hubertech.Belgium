using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hubertech.Belgium;

/// <summary>
/// A Belgian enterprise number, assigned by the Crossroads Bank for Enterprises (BCE in French,
/// KBO in Dutch): ten digits, starting with 0 or 1, the last two being check digits.
/// </summary>
/// <remarks>
/// <para>
/// Parsing is tolerant on form and strict on content. White space, dots and hyphens are ignored,
/// a <c>BE</c> prefix is accepted in any case, and a legacy nine-digit number is completed with a
/// leading zero. The number must then have ten digits, start with 0 or 1, and its last two digits
/// must equal 97 minus the remainder of the division of the first eight digits by 97.
/// </para>
/// <para>
/// A valid number is not necessarily assigned to an active enterprise, nor registered for VAT:
/// this type checks the syntax and the check digits only, and never calls a remote service.
/// </para>
/// <para>
/// <c>default(EnterpriseNumber)</c> is not a valid number: <see cref="IsEmpty"/> is
/// <see langword="true"/> and it formats as an empty string. Parsing never returns it.
/// </para>
/// <para>
/// Enterprise numbers do not depend on culture. The members of <see cref="IParsable{TSelf}"/>,
/// <see cref="ISpanParsable{TSelf}"/> and <see cref="ISpanFormattable"/> that take an
/// <see cref="IFormatProvider"/> are implemented explicitly and ignore it.
/// </para>
/// </remarks>
/// <seealso href="https://news.economie.fgov.be/228779-les-numeros-d-entreprise-passent-au-1/">FPS Economy: enterprise numbers starting with 1</seealso>
public readonly struct EnterpriseNumber : IEquatable<EnterpriseNumber>, ISpanFormattable, ISpanParsable<EnterpriseNumber>
{
    private const int DigitCount = 10;
    private const int LegacyDigitCount = 9;
    private const int MaxFormattedLength = 12;
    private const uint FirstValueStartingWithTwo = 2_000_000_000;

    // The ten digits read as a number. Zero is the empty value: 0000000000 never passes the check.
    private readonly uint _value;

    private EnterpriseNumber(uint value) => _value = value;

    /// <summary>
    /// Gets a value indicating whether this instance is the default value, which is not a valid
    /// enterprise number.
    /// </summary>
    public bool IsEmpty => _value == 0;

    /// <summary>
    /// Converts a string to an enterprise number.
    /// </summary>
    /// <param name="s">The string to parse, for example <c>0202.239.951</c> or <c>BE0202239951</c>.</param>
    /// <returns>The enterprise number.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
    /// <exception cref="BelgianFormatException"><paramref name="s"/> is not a valid enterprise number.</exception>
    public static EnterpriseNumber Parse(string s)
    {
        ArgumentNullException.ThrowIfNull(s);

        return Parse(s.AsSpan());
    }

    /// <summary>
    /// Converts a span of characters to an enterprise number.
    /// </summary>
    /// <param name="s">The characters to parse, for example <c>0202.239.951</c> or <c>BE0202239951</c>.</param>
    /// <returns>The enterprise number.</returns>
    /// <exception cref="BelgianFormatException"><paramref name="s"/> is not a valid enterprise number.</exception>
    public static EnterpriseNumber Parse(ReadOnlySpan<char> s) =>
        TryParse(s, out var result, out var error) ? result : throw new BelgianFormatException(error);

    /// <summary>
    /// Tries to convert a string to an enterprise number.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">The enterprise number, or <see langword="default"/> when parsing fails.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid enterprise number; otherwise <see langword="false"/>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out EnterpriseNumber result) =>
        TryParse(s.AsSpan(), out result, out _);

    /// <summary>
    /// Tries to convert a span of characters to an enterprise number.
    /// </summary>
    /// <param name="s">The characters to parse.</param>
    /// <param name="result">The enterprise number, or <see langword="default"/> when parsing fails.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid enterprise number; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, out EnterpriseNumber result) =>
        TryParse(s, out result, out _);

    /// <summary>
    /// Tries to convert a string to an enterprise number, and describes why it failed.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">The enterprise number, or <see langword="default"/> when parsing fails.</param>
    /// <param name="error">The reason of the failure, or <see langword="default"/> when parsing succeeds.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid enterprise number; otherwise <see langword="false"/>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out EnterpriseNumber result, out BelgianValidationError error) =>
        TryParse(s.AsSpan(), out result, out error);

    /// <summary>
    /// Tries to convert a span of characters to an enterprise number, and describes why it failed.
    /// </summary>
    /// <param name="s">The characters to parse.</param>
    /// <param name="result">The enterprise number, or <see langword="default"/> when parsing fails.</param>
    /// <param name="error">The reason of the failure, or <see langword="default"/> when parsing succeeds.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid enterprise number; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, out EnterpriseNumber result, out BelgianValidationError error)
    {
        result = default;

        int index = SkipSeparators(s, 0);
        if (index == s.Length)
        {
            error = BelgianValidationError.Empty(nameof(EnterpriseNumber));
            return false;
        }

        if (StartsWithCountryCode(s, index))
        {
            if (!s.Slice(index, 2).Equals("BE", StringComparison.OrdinalIgnoreCase))
            {
                error = BelgianValidationError.InvalidCountryCode(nameof(EnterpriseNumber));
                return false;
            }

            index += 2;
        }

        ulong value = 0;
        int digitCount = 0;
        for (; index < s.Length; index++)
        {
            char c = s[index];
            if (char.IsAsciiDigit(c))
            {
                if (digitCount < DigitCount)
                {
                    value = (value * 10) + (uint)(c - '0');
                }

                digitCount++;
            }
            else if (!IsSeparator(c))
            {
                error = BelgianValidationError.InvalidCharacter(nameof(EnterpriseNumber), index, c);
                return false;
            }
        }

        // A legacy nine-digit number has the same value as its ten-digit form, 0 prepended.
        if (digitCount is not (DigitCount or LegacyDigitCount))
        {
            error = BelgianValidationError.InvalidLength(nameof(EnterpriseNumber));
            return false;
        }

        if (value >= FirstValueStartingWithTwo)
        {
            error = BelgianValidationError.InvalidFirstDigit(nameof(EnterpriseNumber));
            return false;
        }

        uint number = (uint)value;
        uint expectedCheckDigits = 97 - (number / 100 % 97);
        if (number % 100 != expectedCheckDigits)
        {
            error = BelgianValidationError.InvalidChecksum(nameof(EnterpriseNumber), (int)expectedCheckDigits);
            return false;
        }

        result = new EnterpriseNumber(number);
        error = default;
        return true;
    }

    /// <summary>
    /// Validates a string as an enterprise number.
    /// </summary>
    /// <param name="s">The string to validate.</param>
    /// <returns>The reason why <paramref name="s"/> is not a valid enterprise number, or <see langword="null"/> if it is valid.</returns>
    public static BelgianValidationError? Validate(string? s) => Validate(s.AsSpan());

    /// <summary>
    /// Validates a span of characters as an enterprise number.
    /// </summary>
    /// <param name="s">The characters to validate.</param>
    /// <returns>The reason why <paramref name="s"/> is not a valid enterprise number, or <see langword="null"/> if it is valid.</returns>
    public static BelgianValidationError? Validate(ReadOnlySpan<char> s) =>
        TryParse(s, out _, out var error) ? null : error;

    /// <summary>
    /// Formats the enterprise number in the default format, <c>0202.239.951</c>.
    /// </summary>
    /// <returns>The formatted number, or an empty string when <see cref="IsEmpty"/> is <see langword="true"/>.</returns>
    public override string ToString() => ToString(null);

    /// <summary>
    /// Formats the enterprise number.
    /// </summary>
    /// <param name="format">
    /// <c>D</c> or <see langword="null"/> for <c>0202.239.951</c>, <c>N</c> for <c>0202239951</c>,
    /// <c>V</c> for the VAT form <c>BE0202239951</c>.
    /// </param>
    /// <returns>The formatted number, or an empty string when <see cref="IsEmpty"/> is <see langword="true"/>.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is not supported.</exception>
    public string ToString(string? format)
    {
        Span<char> buffer = stackalloc char[MaxFormattedLength];
        bool formatted = TryFormat(buffer, out int charsWritten, format);
        Debug.Assert(formatted, "The buffer fits every format.");

        return new string(buffer[..charsWritten]);
    }

    /// <summary>
    /// Tries to format the enterprise number into a span of characters.
    /// </summary>
    /// <param name="destination">The span in which to write the number.</param>
    /// <param name="charsWritten">The number of characters written.</param>
    /// <param name="format">
    /// <c>D</c> or empty for <c>0202.239.951</c>, <c>N</c> for <c>0202239951</c>,
    /// <c>V</c> for the VAT form <c>BE0202239951</c>.
    /// </param>
    /// <returns><see langword="true"/> if the number fits in <paramref name="destination"/>; otherwise <see langword="false"/>.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is not supported.</exception>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default)
    {
        char kind = GetFormatKind(format);
        int length = kind == 'N' ? DigitCount : MaxFormattedLength;

        if (IsEmpty)
        {
            charsWritten = 0;
            return true;
        }

        if (destination.Length < length)
        {
            charsWritten = 0;
            return false;
        }

        Span<char> digits = stackalloc char[DigitCount];
        WriteDigits(digits, _value);

        switch (kind)
        {
            case 'N':
                digits.CopyTo(destination);
                break;
            case 'V':
                "BE".CopyTo(destination);
                digits.CopyTo(destination[2..]);
                break;
            default:
                digits[..4].CopyTo(destination);
                destination[4] = '.';
                digits[4..7].CopyTo(destination[5..]);
                destination[8] = '.';
                digits[7..].CopyTo(destination[9..]);
                break;
        }

        charsWritten = length;
        return true;
    }

    /// <inheritdoc/>
    public bool Equals(EnterpriseNumber other) => _value == other._value;

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is EnterpriseNumber other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    /// Determines whether two enterprise numbers are equal.
    /// </summary>
    /// <param name="left">The first number.</param>
    /// <param name="right">The second number.</param>
    /// <returns><see langword="true"/> if both numbers have the same digits; otherwise <see langword="false"/>.</returns>
    public static bool operator ==(EnterpriseNumber left, EnterpriseNumber right) => left.Equals(right);

    /// <summary>
    /// Determines whether two enterprise numbers are different.
    /// </summary>
    /// <param name="left">The first number.</param>
    /// <param name="right">The second number.</param>
    /// <returns><see langword="true"/> if the numbers have different digits; otherwise <see langword="false"/>.</returns>
    public static bool operator !=(EnterpriseNumber left, EnterpriseNumber right) => !left.Equals(right);

    /// <inheritdoc/>
    static EnterpriseNumber IParsable<EnterpriseNumber>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <inheritdoc/>
    static bool IParsable<EnterpriseNumber>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out EnterpriseNumber result) =>
        TryParse(s, out result);

    /// <inheritdoc/>
    static EnterpriseNumber ISpanParsable<EnterpriseNumber>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

    /// <inheritdoc/>
    static bool ISpanParsable<EnterpriseNumber>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out EnterpriseNumber result) =>
        TryParse(s, out result);

    /// <inheritdoc/>
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString(format);

    /// <inheritdoc/>
    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        TryFormat(destination, out charsWritten, format);

    private static bool IsSeparator(char c) => char.IsWhiteSpace(c) || c is '.' or '-';

    private static int SkipSeparators(ReadOnlySpan<char> s, int index)
    {
        while (index < s.Length && IsSeparator(s[index]))
        {
            index++;
        }

        return index;
    }

    // A country code is exactly two letters: "BE0202239951" has one, "TVA BE0202239951" does not.
    private static bool StartsWithCountryCode(ReadOnlySpan<char> s, int index) =>
        index + 1 < s.Length
        && char.IsAsciiLetter(s[index])
        && char.IsAsciiLetter(s[index + 1])
        && (index + 2 == s.Length || !char.IsAsciiLetter(s[index + 2]));

    private static char GetFormatKind(ReadOnlySpan<char> format)
    {
        char kind = format.Length switch
        {
            0 => 'D',
            1 => char.ToUpperInvariant(format[0]),
            _ => default,
        };

        return kind is 'D' or 'N' or 'V'
            ? kind
            : throw new FormatException($"The format '{format}' is not supported. Supported formats are D, N and V.");
    }

    private static void WriteDigits(Span<char> destination, uint value)
    {
        for (int i = destination.Length - 1; i >= 0; i--)
        {
            destination[i] = (char)('0' + (value % 10));
            value /= 10;
        }
    }
}
