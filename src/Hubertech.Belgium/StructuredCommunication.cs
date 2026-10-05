using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Hubertech.Belgium.Serialization;

namespace Hubertech.Belgium;

/// <summary>
/// A Belgian structured communication (OGM in French, VCS in Dutch): the twelve-digit payment
/// reference written <c>+++123/4567/89002+++</c>, whose last two digits check the first ten.
/// </summary>
/// <remarks>
/// <para>
/// Parsing is tolerant on form and strict on content. White space, plus signs, asterisks,
/// slashes, dots and hyphens are ignored. The reference must then have twelve digits, and its
/// last two digits must equal the remainder of the division of the first ten digits by 97, or
/// 97 when that remainder is 0.
/// </para>
/// <para>
/// <see cref="FromNumber(ulong)"/> builds a reference from a number of up to ten digits, such as
/// an invoice number, and <see cref="BaseNumber"/> gives it back.
/// </para>
/// <para>
/// <c>default(StructuredCommunication)</c> is not a valid reference: <see cref="IsEmpty"/> is
/// <see langword="true"/> and it formats as an empty string. Parsing never returns it.
/// </para>
/// <para>
/// Structured communications do not depend on culture. The members of
/// <see cref="IParsable{TSelf}"/>, <see cref="ISpanParsable{TSelf}"/> and
/// <see cref="ISpanFormattable"/> that take an <see cref="IFormatProvider"/> are implemented
/// explicitly and ignore it.
/// </para>
/// <para>
/// In JSON, the value is a string, converted by <see cref="StructuredCommunicationJsonConverter"/>.
/// </para>
/// </remarks>
/// <seealso href="https://febelfin.be/en/publications/2023/febelfin-banking-standards-for-online-banking">Febelfin banking standards for online banking</seealso>
[JsonConverter(typeof(StructuredCommunicationJsonConverter))]
public readonly struct StructuredCommunication : IEquatable<StructuredCommunication>, ISpanFormattable, ISpanParsable<StructuredCommunication>, IBelgianIdentifier<StructuredCommunication>
{
    /// <summary>
    /// The largest number that <see cref="FromNumber(ulong)"/> accepts: ten digits.
    /// </summary>
    public const ulong MaxBaseNumber = 9_999_999_999;

    private const int DigitCount = 12;
    private const int MaxFormattedLength = 20;

    // The twelve digits read as a number. Zero is the empty value: check digits are never 00.
    private readonly ulong _value;

    private StructuredCommunication(ulong value) => _value = value;

    /// <summary>
    /// Gets a value indicating whether this instance is the default value, which is not a valid
    /// structured communication.
    /// </summary>
    public bool IsEmpty => _value == 0;

    /// <summary>
    /// Gets the number carried by the reference: its first ten digits, without the check digits.
    /// </summary>
    /// <remarks>Zero when <see cref="IsEmpty"/> is <see langword="true"/>.</remarks>
    public ulong BaseNumber => _value / 100;

    /// <summary>
    /// Builds the structured communication that carries a number, for example an invoice number.
    /// </summary>
    /// <param name="value">The number to carry, from 0 to <see cref="MaxBaseNumber"/>.</param>
    /// <returns>The structured communication, whose <see cref="BaseNumber"/> is <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> has more than ten digits.</exception>
    public static StructuredCommunication FromNumber(ulong value)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaxBaseNumber);

        return new StructuredCommunication((value * 100) + Mod97.CheckDigits(value));
    }

    /// <summary>
    /// Converts a string to a structured communication.
    /// </summary>
    /// <param name="s">The string to parse, for example <c>+++123/4567/89002+++</c> or <c>123456789002</c>.</param>
    /// <returns>The structured communication.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
    /// <exception cref="BelgianFormatException"><paramref name="s"/> is not a valid structured communication.</exception>
    public static StructuredCommunication Parse(string s)
    {
        ArgumentNullException.ThrowIfNull(s);

        return Parse(s.AsSpan());
    }

    /// <summary>
    /// Converts a span of characters to a structured communication.
    /// </summary>
    /// <param name="s">The characters to parse, for example <c>+++123/4567/89002+++</c> or <c>123456789002</c>.</param>
    /// <returns>The structured communication.</returns>
    /// <exception cref="BelgianFormatException"><paramref name="s"/> is not a valid structured communication.</exception>
    public static StructuredCommunication Parse(ReadOnlySpan<char> s) =>
        TryParse(s, out var result, out var error) ? result : throw new BelgianFormatException(error);

    /// <summary>
    /// Tries to convert a string to a structured communication.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">The structured communication, or <see langword="default"/> when parsing fails.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid structured communication; otherwise <see langword="false"/>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out StructuredCommunication result) =>
        TryParse(s.AsSpan(), out result, out _);

    /// <summary>
    /// Tries to convert a span of characters to a structured communication.
    /// </summary>
    /// <param name="s">The characters to parse.</param>
    /// <param name="result">The structured communication, or <see langword="default"/> when parsing fails.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid structured communication; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, out StructuredCommunication result) =>
        TryParse(s, out result, out _);

    /// <summary>
    /// Tries to convert a string to a structured communication, and describes why it failed.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">The structured communication, or <see langword="default"/> when parsing fails.</param>
    /// <param name="error">The reason of the failure, or <see langword="default"/> when parsing succeeds.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid structured communication; otherwise <see langword="false"/>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out StructuredCommunication result, out BelgianValidationError error) =>
        TryParse(s.AsSpan(), out result, out error);

    /// <summary>
    /// Tries to convert a span of characters to a structured communication, and describes why it failed.
    /// </summary>
    /// <param name="s">The characters to parse.</param>
    /// <param name="result">The structured communication, or <see langword="default"/> when parsing fails.</param>
    /// <param name="error">The reason of the failure, or <see langword="default"/> when parsing succeeds.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid structured communication; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, out StructuredCommunication result, out BelgianValidationError error)
    {
        result = default;

        ulong value = 0;
        int digitCount = 0;
        for (int index = 0; index < s.Length; index++)
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
                error = BelgianValidationError.InvalidCharacter(nameof(StructuredCommunication), index, c);
                return false;
            }
        }

        if (digitCount == 0)
        {
            error = BelgianValidationError.Empty(nameof(StructuredCommunication));
            return false;
        }

        if (digitCount != DigitCount)
        {
            error = BelgianValidationError.InvalidLength(nameof(StructuredCommunication));
            return false;
        }

        uint expectedCheckDigits = Mod97.CheckDigits(value / 100);
        if (value % 100 != expectedCheckDigits)
        {
            error = BelgianValidationError.InvalidChecksum(nameof(StructuredCommunication), (int)expectedCheckDigits);
            return false;
        }

        result = new StructuredCommunication(value);
        error = default;
        return true;
    }

    /// <summary>
    /// Validates a string as a structured communication.
    /// </summary>
    /// <param name="s">The string to validate.</param>
    /// <returns>The reason why <paramref name="s"/> is not a valid structured communication, or <see langword="null"/> if it is valid.</returns>
    public static BelgianValidationError? Validate(string? s) => Validate(s.AsSpan());

    /// <summary>
    /// Validates a span of characters as a structured communication.
    /// </summary>
    /// <param name="s">The characters to validate.</param>
    /// <returns>The reason why <paramref name="s"/> is not a valid structured communication, or <see langword="null"/> if it is valid.</returns>
    public static BelgianValidationError? Validate(ReadOnlySpan<char> s) =>
        TryParse(s, out _, out var error) ? null : error;

    /// <summary>
    /// Formats the structured communication in the default format, <c>+++123/4567/89002+++</c>.
    /// </summary>
    /// <returns>The formatted reference, or an empty string when <see cref="IsEmpty"/> is <see langword="true"/>.</returns>
    public override string ToString() => ToString(null);

    /// <summary>
    /// Formats the structured communication.
    /// </summary>
    /// <param name="format">
    /// <c>+</c> or <see langword="null"/> for <c>+++123/4567/89002+++</c>, <c>*</c> for
    /// <c>***123/4567/89002***</c>, <c>N</c> for the digits only, <c>123456789002</c>.
    /// </param>
    /// <returns>The formatted reference, or an empty string when <see cref="IsEmpty"/> is <see langword="true"/>.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is not supported.</exception>
    public string ToString(string? format)
    {
        Span<char> buffer = stackalloc char[MaxFormattedLength];
        bool formatted = TryFormat(buffer, out int charsWritten, format);
        Debug.Assert(formatted, "The buffer fits every format.");

        return new string(buffer[..charsWritten]);
    }

    /// <summary>
    /// Tries to format the structured communication into a span of characters.
    /// </summary>
    /// <param name="destination">The span in which to write the reference.</param>
    /// <param name="charsWritten">The number of characters written.</param>
    /// <param name="format">
    /// <c>+</c> or empty for <c>+++123/4567/89002+++</c>, <c>*</c> for
    /// <c>***123/4567/89002***</c>, <c>N</c> for the digits only, <c>123456789002</c>.
    /// </param>
    /// <returns><see langword="true"/> if the reference fits in <paramref name="destination"/>; otherwise <see langword="false"/>.</returns>
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

        if (kind == 'N')
        {
            Digits.Write(destination[..DigitCount], _value);
        }
        else
        {
            Span<char> digits = stackalloc char[DigitCount];
            Digits.Write(digits, _value);

            destination[..3].Fill(kind);
            digits[..3].CopyTo(destination[3..]);
            destination[6] = '/';
            digits[3..7].CopyTo(destination[7..]);
            destination[11] = '/';
            digits[7..].CopyTo(destination[12..]);
            destination[17..20].Fill(kind);
        }

        charsWritten = length;
        return true;
    }

    /// <inheritdoc/>
    public bool Equals(StructuredCommunication other) => _value == other._value;

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is StructuredCommunication other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    /// Determines whether two structured communications are equal.
    /// </summary>
    /// <param name="left">The first reference.</param>
    /// <param name="right">The second reference.</param>
    /// <returns><see langword="true"/> if both references have the same digits; otherwise <see langword="false"/>.</returns>
    public static bool operator ==(StructuredCommunication left, StructuredCommunication right) => left.Equals(right);

    /// <summary>
    /// Determines whether two structured communications are different.
    /// </summary>
    /// <param name="left">The first reference.</param>
    /// <param name="right">The second reference.</param>
    /// <returns><see langword="true"/> if the references have different digits; otherwise <see langword="false"/>.</returns>
    public static bool operator !=(StructuredCommunication left, StructuredCommunication right) => !left.Equals(right);

    /// <inheritdoc/>
    static StructuredCommunication IParsable<StructuredCommunication>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <inheritdoc/>
    static bool IParsable<StructuredCommunication>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out StructuredCommunication result) =>
        TryParse(s, out result);

    /// <inheritdoc/>
    static StructuredCommunication ISpanParsable<StructuredCommunication>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

    /// <inheritdoc/>
    static bool ISpanParsable<StructuredCommunication>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out StructuredCommunication result) =>
        TryParse(s, out result);

    /// <inheritdoc/>
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString(format);

    /// <inheritdoc/>
    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        TryFormat(destination, out charsWritten, format);

    private static bool IsSeparator(char c) => char.IsWhiteSpace(c) || c is '+' or '*' or '/' or '.' or '-';

    private static char GetFormatKind(ReadOnlySpan<char> format)
    {
        char kind = format.Length switch
        {
            0 => '+',
            1 => char.ToUpperInvariant(format[0]),
            _ => default,
        };

        return kind is '+' or '*' or 'N'
            ? kind
            : throw new FormatException($"The format '{format}' is not supported. Supported formats are +, * and N.");
    }
}
