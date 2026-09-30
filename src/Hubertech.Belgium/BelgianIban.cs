using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Hubertech.Belgium;

/// <summary>
/// A Belgian IBAN: <c>BE</c>, two IBAN check digits, then the twelve digits of the Belgian
/// account number, for example <c>BE68 5390 0754 7034</c>.
/// </summary>
/// <remarks>
/// <para>
/// Parsing is tolerant on form and strict on content. White space, dots and hyphens are ignored,
/// and the <c>BE</c> country code is accepted in any case. An IBAN from another country is
/// rejected. Two checks must then pass: the IBAN check digits (ISO 7064 MOD 97-10, as for every
/// IBAN), and the check digits of the Belgian account number (the remainder of the division of
/// its first ten digits by 97, or 97 when that remainder is 0).
/// </para>
/// <para>
/// The bank code is not checked against the list of the National Bank of Belgium: an IBAN that
/// passes both checks is accepted even if no bank uses its code.
/// </para>
/// <para>
/// <c>default(BelgianIban)</c> is not a valid IBAN: <see cref="IsEmpty"/> is
/// <see langword="true"/> and it formats as an empty string. Parsing never returns it.
/// </para>
/// <para>
/// IBANs do not depend on culture. The members of <see cref="IParsable{TSelf}"/>,
/// <see cref="ISpanParsable{TSelf}"/> and <see cref="ISpanFormattable"/> that take an
/// <see cref="IFormatProvider"/> are implemented explicitly and ignore it.
/// </para>
/// </remarks>
/// <seealso href="https://www.swift.com/standards/data-standards/iban-international-bank-account-number">SWIFT: IBAN registry</seealso>
/// <seealso href="https://www.nbb.be/en/payment-systems/payment-standards/bank-identification-codes">National Bank of Belgium: bank identification codes</seealso>
public readonly struct BelgianIban : IEquatable<BelgianIban>, ISpanFormattable, ISpanParsable<BelgianIban>
{
    private const int DigitCount = 14;
    private const int AccountDigitCount = 12;
    private const int ElectronicLength = 16;
    private const int PaperLength = 19;
    private const ulong AccountNumberLimit = 1_000_000_000_000;

    // Errors about a legacy account number talk about an account number, not about an IBAN.
    private const string LegacyAccountNumberSubject = "BelgianAccountNumber";

    // The twelve digits of the Belgian account number; the IBAN check digits derive from them.
    // Zero is the empty value: account number 000-0000000-00 fails the national check.
    private readonly ulong _account;

    private BelgianIban(ulong account) => _account = account;

    /// <summary>
    /// Gets a value indicating whether this instance is the default value, which is not a valid IBAN.
    /// </summary>
    public bool IsEmpty => _account == 0;

    /// <summary>
    /// Gets the bank identification code: the first three digits of the account number, for
    /// example <c>539</c>.
    /// </summary>
    /// <remarks>An empty string when <see cref="IsEmpty"/> is <see langword="true"/>.</remarks>
    public string BankCode => IsEmpty
        ? string.Empty
        : string.Create(3, _account, static (destination, account) => Digits.Write(destination, account / 1_000_000_000));

    /// <summary>
    /// Gets the Belgian account number in its national notation, for example <c>539-0075470-34</c>.
    /// </summary>
    /// <remarks>An empty string when <see cref="IsEmpty"/> is <see langword="true"/>.</remarks>
    public string AccountNumber => IsEmpty
        ? string.Empty
        : string.Create(14, _account, static (destination, account) =>
        {
            Digits.Write(destination[..3], account / 1_000_000_000);
            destination[3] = '-';
            Digits.Write(destination[4..11], account / 100 % 10_000_000);
            destination[11] = '-';
            Digits.Write(destination[12..], account % 100);
        });

    /// <summary>
    /// Converts a string to a Belgian IBAN.
    /// </summary>
    /// <param name="s">The string to parse, for example <c>BE68 5390 0754 7034</c>.</param>
    /// <returns>The IBAN.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="s"/> is <see langword="null"/>.</exception>
    /// <exception cref="BelgianFormatException"><paramref name="s"/> is not a valid Belgian IBAN.</exception>
    public static BelgianIban Parse(string s)
    {
        ArgumentNullException.ThrowIfNull(s);

        return Parse(s.AsSpan());
    }

    /// <summary>
    /// Converts a span of characters to a Belgian IBAN.
    /// </summary>
    /// <param name="s">The characters to parse, for example <c>BE68 5390 0754 7034</c>.</param>
    /// <returns>The IBAN.</returns>
    /// <exception cref="BelgianFormatException"><paramref name="s"/> is not a valid Belgian IBAN.</exception>
    public static BelgianIban Parse(ReadOnlySpan<char> s) =>
        TryParse(s, out var result, out var error) ? result : throw new BelgianFormatException(error);

    /// <summary>
    /// Tries to convert a string to a Belgian IBAN.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">The IBAN, or <see langword="default"/> when parsing fails.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid Belgian IBAN; otherwise <see langword="false"/>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out BelgianIban result) =>
        TryParse(s.AsSpan(), out result, out _);

    /// <summary>
    /// Tries to convert a span of characters to a Belgian IBAN.
    /// </summary>
    /// <param name="s">The characters to parse.</param>
    /// <param name="result">The IBAN, or <see langword="default"/> when parsing fails.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid Belgian IBAN; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, out BelgianIban result) =>
        TryParse(s, out result, out _);

    /// <summary>
    /// Tries to convert a string to a Belgian IBAN, and describes why it failed.
    /// </summary>
    /// <param name="s">The string to parse.</param>
    /// <param name="result">The IBAN, or <see langword="default"/> when parsing fails.</param>
    /// <param name="error">The reason of the failure, or <see langword="default"/> when parsing succeeds.</param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid Belgian IBAN; otherwise <see langword="false"/>.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, out BelgianIban result, out BelgianValidationError error) =>
        TryParse(s.AsSpan(), out result, out error);

    /// <summary>
    /// Tries to convert a span of characters to a Belgian IBAN, and describes why it failed.
    /// </summary>
    /// <param name="s">The characters to parse.</param>
    /// <param name="result">The IBAN, or <see langword="default"/> when parsing fails.</param>
    /// <param name="error">
    /// The reason of the failure, or <see langword="default"/> when parsing succeeds. For
    /// <see cref="BelgianErrorCode.InvalidChecksum"/>, <see cref="BelgianValidationError.Expected"/>
    /// gives the IBAN check digits when they are wrong, otherwise the last two digits of the
    /// Belgian account number.
    /// </param>
    /// <returns><see langword="true"/> if <paramref name="s"/> is a valid Belgian IBAN; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, out BelgianIban result, out BelgianValidationError error)
    {
        result = default;

        int index = SkipSeparators(s, 0);
        if (index == s.Length)
        {
            error = BelgianValidationError.Empty(nameof(BelgianIban));
            return false;
        }

        // A Belgian IBAN always starts with BE; digits first means the country code is missing.
        if (CountryCode.StartsAt(s, index))
        {
            if (!CountryCode.IsBelgium(s, index))
            {
                error = BelgianValidationError.InvalidCountryCode(nameof(BelgianIban));
                return false;
            }

            index += 2;
        }
        else if (char.IsAsciiDigit(s[index]))
        {
            error = BelgianValidationError.InvalidCountryCode(nameof(BelgianIban));
            return false;
        }
        else
        {
            error = BelgianValidationError.InvalidCharacter(nameof(BelgianIban), index, s[index]);
            return false;
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
                error = BelgianValidationError.InvalidCharacter(nameof(BelgianIban), index, c);
                return false;
            }
        }

        if (digitCount != DigitCount)
        {
            error = BelgianValidationError.InvalidLength(nameof(BelgianIban));
            return false;
        }

        ulong account = value % AccountNumberLimit;
        uint expectedIbanCheckDigits = IbanCheckDigits(account);
        if (value / AccountNumberLimit != expectedIbanCheckDigits)
        {
            error = BelgianValidationError.InvalidChecksum(nameof(BelgianIban), (int)expectedIbanCheckDigits);
            return false;
        }

        uint expectedAccountCheckDigits = Mod97.CheckDigits(account / 100);
        if (account % 100 != expectedAccountCheckDigits)
        {
            error = BelgianValidationError.InvalidChecksum(nameof(BelgianIban), (int)expectedAccountCheckDigits);
            return false;
        }

        result = new BelgianIban(account);
        error = default;
        return true;
    }

    /// <summary>
    /// Validates a string as a Belgian IBAN.
    /// </summary>
    /// <param name="s">The string to validate.</param>
    /// <returns>The reason why <paramref name="s"/> is not a valid Belgian IBAN, or <see langword="null"/> if it is valid.</returns>
    public static BelgianValidationError? Validate(string? s) => Validate(s.AsSpan());

    /// <summary>
    /// Validates a span of characters as a Belgian IBAN.
    /// </summary>
    /// <param name="s">The characters to validate.</param>
    /// <returns>The reason why <paramref name="s"/> is not a valid Belgian IBAN, or <see langword="null"/> if it is valid.</returns>
    public static BelgianValidationError? Validate(ReadOnlySpan<char> s) =>
        TryParse(s, out _, out var error) ? null : error;

    /// <summary>
    /// Converts a Belgian account number in its national notation to the IBAN of that account.
    /// </summary>
    /// <param name="accountNumber">The twelve-digit account number, for example <c>539-0075470-34</c>.</param>
    /// <returns>The IBAN, for example <c>BE68 5390 0754 7034</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="accountNumber"/> is <see langword="null"/>.</exception>
    /// <exception cref="BelgianFormatException"><paramref name="accountNumber"/> is not a valid Belgian account number.</exception>
    public static BelgianIban FromLegacyAccountNumber(string accountNumber)
    {
        ArgumentNullException.ThrowIfNull(accountNumber);

        return FromLegacyAccountNumber(accountNumber.AsSpan());
    }

    /// <summary>
    /// Converts a Belgian account number in its national notation to the IBAN of that account.
    /// </summary>
    /// <param name="accountNumber">The twelve-digit account number, for example <c>539-0075470-34</c>.</param>
    /// <returns>The IBAN, for example <c>BE68 5390 0754 7034</c>.</returns>
    /// <exception cref="BelgianFormatException"><paramref name="accountNumber"/> is not a valid Belgian account number.</exception>
    public static BelgianIban FromLegacyAccountNumber(ReadOnlySpan<char> accountNumber) =>
        TryFromLegacyAccountNumber(accountNumber, out var result, out var error) ? result : throw new BelgianFormatException(error);

    /// <summary>
    /// Tries to convert a Belgian account number in its national notation to the IBAN of that
    /// account, and describes why it failed.
    /// </summary>
    /// <param name="accountNumber">The account number to convert, for example <c>539-0075470-34</c>.</param>
    /// <param name="result">The IBAN, or <see langword="default"/> when the conversion fails.</param>
    /// <param name="error">The reason of the failure, whose message is about the account number, or <see langword="default"/> when the conversion succeeds.</param>
    /// <returns><see langword="true"/> if <paramref name="accountNumber"/> is a valid Belgian account number; otherwise <see langword="false"/>.</returns>
    public static bool TryFromLegacyAccountNumber([NotNullWhen(true)] string? accountNumber, out BelgianIban result, out BelgianValidationError error) =>
        TryFromLegacyAccountNumber(accountNumber.AsSpan(), out result, out error);

    /// <summary>
    /// Tries to convert a Belgian account number in its national notation to the IBAN of that
    /// account, and describes why it failed.
    /// </summary>
    /// <param name="accountNumber">The account number to convert, for example <c>539-0075470-34</c>.</param>
    /// <param name="result">The IBAN, or <see langword="default"/> when the conversion fails.</param>
    /// <param name="error">The reason of the failure, whose message is about the account number, or <see langword="default"/> when the conversion succeeds.</param>
    /// <returns><see langword="true"/> if <paramref name="accountNumber"/> is a valid Belgian account number; otherwise <see langword="false"/>.</returns>
    public static bool TryFromLegacyAccountNumber(ReadOnlySpan<char> accountNumber, out BelgianIban result, out BelgianValidationError error)
    {
        result = default;

        ulong account = 0;
        int digitCount = 0;
        for (int index = 0; index < accountNumber.Length; index++)
        {
            char c = accountNumber[index];
            if (char.IsAsciiDigit(c))
            {
                if (digitCount < AccountDigitCount)
                {
                    account = (account * 10) + (uint)(c - '0');
                }

                digitCount++;
            }
            else if (!IsSeparator(c))
            {
                error = BelgianValidationError.InvalidCharacter(nameof(BelgianIban), index, c).About(LegacyAccountNumberSubject);
                return false;
            }
        }

        if (digitCount == 0)
        {
            error = BelgianValidationError.Empty(nameof(BelgianIban)).About(LegacyAccountNumberSubject);
            return false;
        }

        if (digitCount != AccountDigitCount)
        {
            error = BelgianValidationError.InvalidLength(nameof(BelgianIban)).About(LegacyAccountNumberSubject);
            return false;
        }

        uint expectedCheckDigits = Mod97.CheckDigits(account / 100);
        if (account % 100 != expectedCheckDigits)
        {
            error = BelgianValidationError.InvalidChecksum(nameof(BelgianIban), (int)expectedCheckDigits).About(LegacyAccountNumberSubject);
            return false;
        }

        result = new BelgianIban(account);
        error = default;
        return true;
    }

    /// <summary>
    /// Formats the IBAN in the default, paper format: <c>BE68 5390 0754 7034</c>.
    /// </summary>
    /// <returns>The formatted IBAN, or an empty string when <see cref="IsEmpty"/> is <see langword="true"/>.</returns>
    public override string ToString() => ToString(null);

    /// <summary>
    /// Formats the IBAN.
    /// </summary>
    /// <param name="format">
    /// <c>P</c> or <see langword="null"/> for the paper format <c>BE68 5390 0754 7034</c>,
    /// <c>E</c> for the electronic format <c>BE68539007547034</c>.
    /// </param>
    /// <returns>The formatted IBAN, or an empty string when <see cref="IsEmpty"/> is <see langword="true"/>.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is not supported.</exception>
    public string ToString(string? format)
    {
        Span<char> buffer = stackalloc char[PaperLength];
        bool formatted = TryFormat(buffer, out int charsWritten, format);
        Debug.Assert(formatted, "The buffer fits every format.");

        return new string(buffer[..charsWritten]);
    }

    /// <summary>
    /// Tries to format the IBAN into a span of characters.
    /// </summary>
    /// <param name="destination">The span in which to write the IBAN.</param>
    /// <param name="charsWritten">The number of characters written.</param>
    /// <param name="format">
    /// <c>P</c> or empty for the paper format <c>BE68 5390 0754 7034</c>,
    /// <c>E</c> for the electronic format <c>BE68539007547034</c>.
    /// </param>
    /// <returns><see langword="true"/> if the IBAN fits in <paramref name="destination"/>; otherwise <see langword="false"/>.</returns>
    /// <exception cref="FormatException"><paramref name="format"/> is not supported.</exception>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default)
    {
        char kind = GetFormatKind(format);
        int length = kind == 'E' ? ElectronicLength : PaperLength;

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
        Digits.Write(digits, (IbanCheckDigits(_account) * AccountNumberLimit) + _account);

        "BE".CopyTo(destination);
        if (kind == 'E')
        {
            digits.CopyTo(destination[2..]);
        }
        else
        {
            digits[..2].CopyTo(destination[2..]);
            destination[4] = ' ';
            digits[2..6].CopyTo(destination[5..]);
            destination[9] = ' ';
            digits[6..10].CopyTo(destination[10..]);
            destination[14] = ' ';
            digits[10..].CopyTo(destination[15..]);
        }

        charsWritten = length;
        return true;
    }

    /// <inheritdoc/>
    public bool Equals(BelgianIban other) => _account == other._account;

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is BelgianIban other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => _account.GetHashCode();

    /// <summary>
    /// Determines whether two IBANs are equal.
    /// </summary>
    /// <param name="left">The first IBAN.</param>
    /// <param name="right">The second IBAN.</param>
    /// <returns><see langword="true"/> if both IBANs designate the same account; otherwise <see langword="false"/>.</returns>
    public static bool operator ==(BelgianIban left, BelgianIban right) => left.Equals(right);

    /// <summary>
    /// Determines whether two IBANs are different.
    /// </summary>
    /// <param name="left">The first IBAN.</param>
    /// <param name="right">The second IBAN.</param>
    /// <returns><see langword="true"/> if the IBANs designate different accounts; otherwise <see langword="false"/>.</returns>
    public static bool operator !=(BelgianIban left, BelgianIban right) => !left.Equals(right);

    /// <inheritdoc/>
    static BelgianIban IParsable<BelgianIban>.Parse(string s, IFormatProvider? provider) => Parse(s);

    /// <inheritdoc/>
    static bool IParsable<BelgianIban>.TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out BelgianIban result) =>
        TryParse(s, out result);

    /// <inheritdoc/>
    static BelgianIban ISpanParsable<BelgianIban>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

    /// <inheritdoc/>
    static bool ISpanParsable<BelgianIban>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out BelgianIban result) =>
        TryParse(s, out result);

    /// <inheritdoc/>
    string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString(format);

    /// <inheritdoc/>
    bool ISpanFormattable.TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        TryFormat(destination, out charsWritten, format);

    // ISO 7064 MOD 97-10, as for every IBAN: the account number followed by the country code as
    // numbers (B = 11, E = 14) and 00 must leave a remainder r, and the check digits are 98 - r.
    // Twelve digits followed by six fit in an unsigned 64-bit integer.
    private static uint IbanCheckDigits(ulong account) =>
        98 - (uint)(((account * 1_000_000) + 111_400) % 97);

    private static bool IsSeparator(char c) => char.IsWhiteSpace(c) || c is '.' or '-';

    private static int SkipSeparators(ReadOnlySpan<char> s, int index)
    {
        while (index < s.Length && IsSeparator(s[index]))
        {
            index++;
        }

        return index;
    }

    private static char GetFormatKind(ReadOnlySpan<char> format)
    {
        char kind = format.Length switch
        {
            0 => 'P',
            1 => char.ToUpperInvariant(format[0]),
            _ => default,
        };

        return kind is 'P' or 'E'
            ? kind
            : throw new FormatException($"The format '{format}' is not supported. Supported formats are P and E.");
    }
}
