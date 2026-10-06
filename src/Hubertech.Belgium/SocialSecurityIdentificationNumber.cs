using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hubertech.Belgium;

/// <summary>
/// A Belgian social security identification number (NISS in French, INSZ in Dutch): the
/// national register number of a person registered in the National Register, or the BIS number
/// of a person who is not. Eleven digits, written <c>85.07.30-033.28</c>.
/// </summary>
/// <remarks>
/// <para>
/// The first six digits encode the date of birth, <c>YYMMDD</c>, the month being increased by 20
/// or 40 in a BIS number; the next three are a serial number; the last two are check digits,
/// equal to 97 minus the remainder of the division of the first nine digits by 97. For a person
/// born from 2000, the nine digits are preceded by a 2 before the division. Both computations
/// never give the same check digits, so the matching one tells the century of birth.
/// </para>
/// <para>
/// Accepting both computations lets a few typos through: about 0.4% of the single-digit typos
/// and 1% of the swaps of two adjacent digits turn a valid number into another one that the
/// other computation accepts. A number whose check digits only match for a birth after the
/// current year is therefore rejected, which halves these cases; as a consequence, validation
/// depends on the current date.
/// </para>
/// <para>
/// Parsing is tolerant on form and strict on content. White space, dots and hyphens are ignored.
/// The number must then have eleven digits, a month of birth from 00 to 12, from 20 to 32 or
/// from 40 to 52, and matching check digits. The day of birth is not checked, since the register
/// writes zeros, or other values, in the date of birth when it is unknown or when the serial
/// numbers of a day run out.
/// </para>
/// <para>
/// This is personal data, whose processing is regulated: in particular, the use of the national
/// register number is restricted by article 8 of the law of 8 August 1983 organising a National
/// Register of natural persons. To keep it out of logs, <see cref="ToString()"/> masks every digit
/// but the check digits, <c>**.**.**-***.28</c>: the full number needs an explicit format. The
/// parity of the serial number encodes the sex registered when the number was assigned; it is
/// not exposed.
/// </para>
/// <para>
/// <c>default(SocialSecurityIdentificationNumber)</c> is not a valid number: <see cref="IsEmpty"/>
/// is <see langword="true"/> and it formats as an empty string. Parsing never returns it.
/// </para>
/// <para>
/// Social security identification numbers do not depend on culture. The members of
/// <see cref="IParsable{TSelf}"/>, <see cref="ISpanParsable{TSelf}"/> and
/// <see cref="ISpanFormattable"/> that take an <see cref="IFormatProvider"/> are implemented
/// explicitly and ignore it.
/// </para>
/// </remarks>
/// <seealso href="https://www.ksz-bcss.fgov.be/fr/page/arrete-royal-du-8-fevier-1991">Royal Decree of 8 February 1991 on the composition of the BIS number</seealso>
/// <seealso href="https://www.ibz.rrn.fgov.be/sites/default/files/documents/fr/registre-national/instructions/liste-TI/TI000_Numero-identification.pdf">National Register: instruction TI000 on the identification number</seealso>
public readonly struct SocialSecurityIdentificationNumber
    : IEquatable<SocialSecurityIdentificationNumber>, ISpanFormattable, ISpanParsable<SocialSecurityIdentificationNumber>, IBelgianIdentifier<SocialSecurityIdentificationNumber>
{
    private const int DigitCount = 11;
    private const int FormattedLength = 15;

    // Precedes the nine digits of a person born from 2000 when computing the check digits.
    private const ulong BornFrom2000Prefix = 2_000_000_000;

    // The eleven digits read as a number. Zero is the empty value: 00000000000 never passes the check.
    private readonly ulong _value;

    private SocialSecurityIdentificationNumber(ulong value) => _value = value;

    /// <summary>
    /// Gets a value indicating whether this instance is the default value, which is not a valid
    /// social security identification number.
    /// </summary>
    public bool IsEmpty => _value == 0;

    /// <summary>
    /// Gets the register that assigned the number, as told by its month of birth.
    /// </summary>
    /// <remarks><see cref="SocialSecurityIdentificationNumberKind.None"/> when <see cref="IsEmpty"/> is <see langword="true"/>.</remarks>
    public SocialSecurityIdentificationNumberKind Kind => IsEmpty
        ? SocialSecurityIdentificationNumberKind.None
        : EncodedMonth(_value) < 20 ? SocialSecurityIdentificationNumberKind.NationalRegister : SocialSecurityIdentificationNumberKind.Bis;

    /// <summary>
    /// Gets the date of birth encoded in the number, when it is complete.
    /// </summary>
    /// <remarks>
    /// The date is the one known when the number was assigned. It is <see langword="null"/> when
    /// its month or its day is zero, which happens when they were unknown or when the serial
    /// numbers of the day ran out, and when it is not a date of the calendar. The number never
    /// changes afterwards, so a person's actual date of birth must be read from the register,
    /// not from the number.
    /// </remarks>
    public DateOnly? BirthDate
    {
        get
        {
            if (IsEmpty)
            {
                return null;
            }

            int year = YearDigits(_value) + (MatchesBirthFrom2000(_value) ? 2000 : 1900);
            int month = EncodedMonth(_value) % 20;
            int day = (int)(_value / 100_000 % 100);

            return month == 0 || day == 0 || day > DateTime.DaysInMonth(year, month)
                ? null
                : new DateOnly(year, month, day);
        }
    }

    /// <summary>
    /// Converts a string to a social security identification number.
    /// </summary>
    /// <param name="s">The string to parse, for example <c>85.07.30-033.28</c> or <c>85073003328</c>.</param>
    /// <returns>The social security identification number.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
    /// <exception cref="BelgianFormatException"><paramref name="s"/> is not a valid social security identification number.</exception>
    public static SocialSecurityIdentificationNumber Parse(string s)
    {
        ArgumentNullException.ThrowIfNull(s);

        return Parse(s.AsSpan());
    }

    /// <summary>
    /// Converts a span of characters to a social security identification number.
    /// </summary>
    /// <param name="s">The characters to parse, for example <c>85.07.30-033.28</c> or <c>85073003328</c>.</param>
    /// <returns>The social security identification number.</returns>
    /// <exception cref="BelgianFormatException"><paramref name="s"/> is not a valid social security identification number.</exception>
    public static SocialSecurityIdentificationNumber Parse(ReadOnlySpan<char> s) =>
        TryParse(s, out var result, out var error) ? result : throw new BelgianFormatException(error);

    /// <summary>
    /// Tries to convert a string to a social security identification number.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">The social security identification number, or <see langword="default"/> when parsing fails.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid social security identification number; otherwise <see langword="false"/>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out SocialSecurityIdentificationNumber result) =>
        TryParse(s.AsSpan(), out result, out _);

    /// <summary>
    /// Tries to convert a span of characters to a social security identification number.
    /// </summary>
    /// <param name="s">The characters to parse.</param>
    /// <param name="result">The social security identification number, or <see langword="default"/> when parsing fails.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid social security identification number; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, out SocialSecurityIdentificationNumber result) =>
        TryParse(s, out result, out _);

    /// <summary>
    /// Tries to convert a string to a social security identification number, and describes why it failed.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">The social security identification number, or <see langword="default"/> when parsing fails.</param>
    /// <param name="error">The reason of the failure, or <see langword="default"/> when parsing succeeds.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid social security identification number; otherwise <see langword="false"/>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out SocialSecurityIdentificationNumber result, out BelgianValidationError error) =>
        TryParse(s.AsSpan(), out result, out error);

    /// <summary>
    /// Tries to convert a span of characters to a social security identification number, and describes why it failed.
    /// </summary>
    /// <param name="s">The characters to parse.</param>
    /// <param name="result">The social security identification number, or <see langword="default"/> when parsing fails.</param>
    /// <param name="error">The reason of the failure, or <see langword="default"/> when parsing succeeds.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid social security identification number; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, out SocialSecurityIdentificationNumber result, out BelgianValidationError error)
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
                error = BelgianValidationError.InvalidCharacter(nameof(SocialSecurityIdentificationNumber), index, c);
                return false;
            }
        }

        if (digitCount == 0)
        {
            error = BelgianValidationError.Empty(nameof(SocialSecurityIdentificationNumber));
            return false;
        }

        if (digitCount != DigitCount)
        {
            error = BelgianValidationError.InvalidLength(nameof(SocialSecurityIdentificationNumber));
            return false;
        }

        // 00 to 12 in a national register number; increased by 20 or 40 in a BIS number.
        int month = EncodedMonth(value);
        if (month % 20 > 12 || month > 52)
        {
            error = BelgianValidationError.InvalidBirthDate(nameof(SocialSecurityIdentificationNumber));
            return false;
        }

        // The computation that matches tells the century of birth. A birth from 2000 cannot be
        // after the current year: ruling it out catches about half of the typos that the second
        // computation would let through.
        bool valid = MatchesBirthBefore2000(value)
            || (MatchesBirthFrom2000(value) && 2000 + YearDigits(value) <= DateTime.UtcNow.Year);
        if (!valid)
        {
            error = BelgianValidationError.InvalidChecksum(nameof(SocialSecurityIdentificationNumber));
            return false;
        }

        result = new SocialSecurityIdentificationNumber(value);
        error = default;
        return true;
    }

    /// <summary>
    /// Validates a string as a social security identification number.
    /// </summary>
    /// <param name="s">The string to validate.</param>
    /// <returns>The reason why <paramref name="s"/> is not a valid social security identification number, or <see langword="null"/> if it is valid.</returns>
    public static BelgianValidationError? Validate(string? s) => Validate(s.AsSpan());

    /// <summary>
    /// Validates a span of characters as a social security identification number.
    /// </summary>
    /// <param name="s">The characters to validate.</param>
    /// <returns>The reason why <paramref name="s"/> is not a valid social security identification number, or <see langword="null"/> if it is valid.</returns>
    public static BelgianValidationError? Validate(ReadOnlySpan<char> s) =>
        TryParse(s, out _, out var error) ? null : error;

    /// <summary>
    /// Formats the number masked, <c>**.**.**-***.28</c>: only the check digits are shown.
    /// </summary>
    /// <returns>The masked number, or an empty string when <see cref="IsEmpty"/> is <see langword="true"/>.</returns>
    public override string ToString() => ToString(null);

    /// <summary>
    /// Formats the number.
    /// </summary>
    /// <param name="format">
    /// <c>M</c> or <see langword="null"/> for the masked number, <c>**.**.**-***.28</c>; <c>D</c>
    /// for the full number, <c>85.07.30-033.28</c>; <c>N</c> for the eleven digits, <c>85073003328</c>.
    /// </param>
    /// <returns>The formatted number, or an empty string when <see cref="IsEmpty"/> is <see langword="true"/>.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is not supported.</exception>
    public string ToString(string? format)
    {
        Span<char> buffer = stackalloc char[FormattedLength];
        bool formatted = TryFormat(buffer, out int charsWritten, format);
        Debug.Assert(formatted, "The buffer fits every format.");

        return new string(buffer[..charsWritten]);
    }

    /// <summary>
    /// Tries to format the number into a span of characters.
    /// </summary>
    /// <param name="destination">The span in which to write the number.</param>
    /// <param name="charsWritten">The number of characters written.</param>
    /// <param name="format">
    /// <c>M</c> or empty for the masked number, <c>**.**.**-***.28</c>; <c>D</c> for the full
    /// number, <c>85.07.30-033.28</c>; <c>N</c> for the eleven digits, <c>85073003328</c>.
    /// </param>
    /// <returns><see langword="true"/> if the number fits in <paramref name="destination"/>; otherwise <see langword="false"/>.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is not supported.</exception>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default)
    {
        char kind = GetFormatKind(format);
        int length = kind == 'N' ? DigitCount : FormattedLength;

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
        Digits.Write(digits, _value);
        if (kind == 'M')
        {
            digits[..9].Fill('*');
        }

        if (kind == 'N')
        {
            digits.CopyTo(destination);
        }
        else
        {
            // YY.MM.DD-SSS.CC
            digits[..2].CopyTo(destination);
            destination[2] = '.';
            digits[2..4].CopyTo(destination[3..]);
            destination[5] = '.';
            digits[4..6].CopyTo(destination[6..]);
            destination[8] = '-';
            digits[6..9].CopyTo(destination[9..]);
            destination[12] = '.';
            digits[9..].CopyTo(destination[13..]);
        }

        charsWritten = length;
        return true;
    }

    /// <inheritdoc/>
    public bool Equals(SocialSecurityIdentificationNumber other) => _value == other._value;

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is SocialSecurityIdentificationNumber other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _value.GetHashCode();

    /// <summary>
    /// Determines whether two social security identification numbers are equal.
    /// </summary>
    /// <param name="left">The first number.</param>
    /// <param name="right">The second number.</param>
    /// <returns><see langword="true"/> if both numbers have the same digits; otherwise <see langword="false"/>.</returns>
    public static bool operator ==(SocialSecurityIdentificationNumber left, SocialSecurityIdentificationNumber right) => left.Equals(right);

    /// <summary>
    /// Determines whether two social security identification numbers are different.
    /// </summary>
    /// <param name="left">The first number.</param>
    /// <param name="right">The second number.</param>
    /// <returns><see langword="true"/> if the numbers have different digits; otherwise <see langword="false"/>.</returns>
    public static bool operator !=(SocialSecurityIdentificationNumber left, SocialSecurityIdentificationNumber right) => !left.Equals(right);

    /// <inheritdoc/>
    static SocialSecurityIdentificationNumber IParsable<SocialSecurityIdentificationNumber>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <inheritdoc/>
    static bool IParsable<SocialSecurityIdentificationNumber>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out SocialSecurityIdentificationNumber result) =>
        TryParse(s, out result);

    /// <inheritdoc/>
    static SocialSecurityIdentificationNumber ISpanParsable<SocialSecurityIdentificationNumber>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

    /// <inheritdoc/>
    static bool ISpanParsable<SocialSecurityIdentificationNumber>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out SocialSecurityIdentificationNumber result) =>
        TryParse(s, out result);

    /// <inheritdoc/>
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString(format);

    /// <inheritdoc/>
    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        TryFormat(destination, out charsWritten, format);

    private static bool IsSeparator(char c) => char.IsWhiteSpace(c) || c is '.' or '-';

    private static int YearDigits(ulong value) => (int)(value / 1_000_000_000);

    // The month digits as written, including the 20 or 40 added in a BIS number.
    private static int EncodedMonth(ulong value) => (int)(value / 10_000_000 % 100);

    private static uint CheckDigits(ulong nineDigits) => 97 - (uint)(nineDigits % 97);

    private static bool MatchesBirthBefore2000(ulong value) => value % 100 == CheckDigits(value / 100);

    private static bool MatchesBirthFrom2000(ulong value) => value % 100 == CheckDigits(BornFrom2000Prefix + (value / 100));

    private static char GetFormatKind(ReadOnlySpan<char> format)
    {
        char kind = format.Length switch
        {
            0 => 'M',
            1 => char.ToUpperInvariant(format[0]),
            _ => default,
        };

        return kind is 'M' or 'D' or 'N'
            ? kind
            : throw new FormatException($"The format '{format}' is not supported. Supported formats are M, D and N.");
    }
}
