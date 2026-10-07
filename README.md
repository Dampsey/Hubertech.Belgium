# Hubertech.Belgium

[![NuGet](https://img.shields.io/nuget/v/Hubertech.Belgium)](https://www.nuget.org/packages/Hubertech.Belgium)
[![CI](https://github.com/Dampsey/Hubertech.Belgium/actions/workflows/ci.yml/badge.svg)](https://github.com/Dampsey/Hubertech.Belgium/actions/workflows/ci.yml)

Belgian administrative identifiers and rules for .NET, in one dependency-free package: the enterprise number (BCE/KBO), the structured communication (OGM/VCS), the Belgian IBAN, the social security identification number (NISS/INSZ), public holidays and business days. Every validation error comes with a stable code and a message in English, French or Dutch that can be shown to end users as is.

> **Status:** v0.1. While the major version is 0, a minor version may change the API; every change is listed in the [changelog](https://github.com/Dampsey/Hubertech.Belgium/blob/main/CHANGELOG.md).

- **One API for every identifier**: `Parse`, `TryParse`, `Validate`, `IParsable<T>`, `ISpanFormattable`.
- **Errors made for end users**: stable codes, localized messages that say what to correct.
- **Modern .NET**: value types, no allocation when parsing or formatting, trimming and Native AOT compatible, no dependency beyond the base class library.
- **Ready for ASP.NET Core**: System.Text.Json converters and validation attributes, with nothing to register.

This package checks the **syntax and the check digits** of identifiers. It never calls a remote service: a valid enterprise number is not necessarily assigned to an active enterprise, nor registered for VAT, and a valid IBAN is not necessarily an open account.

## Installation

```shell
dotnet add package Hubertech.Belgium
```

The package targets .NET 10.

## Enterprise number

`EnterpriseNumber` is the ten-digit number of the Crossroads Bank for Enterprises (BCE in French, KBO in Dutch), which, prefixed with `BE`, is also the VAT number of the enterprises registered for VAT.

```csharp
using Hubertech.Belgium;

var number = EnterpriseNumber.Parse("BE 0202.239.951");

number.ToString();     // "0202.239.951"
number.ToString("N");  // "0202239951"
number.ToString("V");  // "BE0202239951", the VAT form

EnterpriseNumber.Parse("202239951") == number;  // true: the legacy nine-digit form
```

Spaces, dots, hyphens and the `BE` prefix, in any case, are accepted. The number must then have ten digits (nine for a legacy number, completed with a leading zero), start with 0 or 1, and end with two check digits equal to 97 minus the remainder of the division of the first eight digits by 97.

## Structured communication

`StructuredCommunication` is the twelve-digit payment reference written `+++123/4567/89002+++`, whose last two digits check the first ten.

```csharp
var reference = StructuredCommunication.FromNumber(2_026_000_123);  // from an invoice number, for example

reference.ToString();     // "+++202/6000/12320+++"
reference.ToString("*");  // "***202/6000/12320***"
reference.ToString("N");  // "202600012320"
reference.BaseNumber;     // 2026000123

StructuredCommunication.Parse("123/4567/89002");  // markers are optional
```

The check digits are the remainder of the division of the first ten digits by 97, or 97 when that remainder is 0. `FromNumber` accepts numbers from 0 to `StructuredCommunication.MaxBaseNumber`, 9 999 999 999.

## Belgian IBAN

`BelgianIban` accepts Belgian IBANs only. An IBAN of another country is rejected with a clear message rather than a checksum error.

```csharp
var iban = BelgianIban.Parse("be68 5390 0754 7034");

iban.ToString();     // "BE68 5390 0754 7034", the paper format
iban.ToString("E");  // "BE68539007547034", the electronic format
iban.BankCode;       // "539"
iban.AccountNumber;  // "539-0075470-34"

BelgianIban.FromLegacyAccountNumber("539-0075470-34") == iban;  // true
```

Two checks must pass: the IBAN check digits (ISO 7064 MOD 97-10, as for every IBAN) and the check digits of the Belgian account number (the remainder of the division of its first ten digits by 97, or 97 when that remainder is 0). The bank code is not checked against the list of the National Bank of Belgium.

## Social security identification number

`SocialSecurityIdentificationNumber` is the NISS (INSZ in Dutch): the national register number of a person registered in the National Register, or the BIS number of a person who is not but deals with the Belgian social security.

```csharp
var number = SocialSecurityIdentificationNumber.Parse("85.07.30-033.28");

number.ToString();     // "**.**.**-***.28": masked, safe to log
number.ToString("D");  // "85.07.30-033.28"
number.ToString("N");  // "85073003328"
number.Kind;           // SocialSecurityIdentificationNumberKind.NationalRegister
number.BirthDate;      // 1985-07-30, or null when the encoded date is incomplete
```

Spaces, dots and hyphens are accepted. The number must then have eleven digits, a month of birth from 00 to 12, or increased by 20 or 40 for a BIS number, and two check digits equal to 97 minus the first nine digits modulo 97; for a person born from 2000, these nine digits are preceded by a 2. Check digits that only match a birth after the current year are rejected, so validation depends on the current date.

This number is personal data, and its use is regulated: article 8 of the law of 8 August 1983 organising a National Register restricts the use of the national register number. This package checks the syntax only; it does not tell whether an application may process the number. To keep the number out of logs, `ToString()` masks it, and the full number needs an explicit format. JSON, however, holds the full number. The sex encoded by the serial number is not exposed.

Since the check digits depend on the century of birth, a few typos go undetected: about 0.2% of the single-digit typos and 0.6% of the swaps of adjacent digits turn a number into a valid one of the other century, `06.02.27-549.42` into `00.02.27-549.42` for instance. For the same reason, `BelgianValidationError.Expected` is `null` for this type.

## Holidays and business days

`BelgianCalendar` knows the ten legal holidays, computes Easter rather than reading a table, and counts business days: Monday to Friday, except the holidays asked for.

```csharp
foreach (var holiday in BelgianCalendar.GetHolidays(2026))
{
    Console.WriteLine($"{holiday.Date:yyyy-MM-dd} {holiday.Name}");  // 2026-04-06 Easter Monday, ...
}

BelgianCalendar.IsBusinessDay(new DateOnly(2026, 4, 6));                                  // false: Easter Monday
BelgianCalendar.AddBusinessDays(new DateOnly(2026, 4, 2), 2);                             // 2026-04-07
BelgianCalendar.CountBusinessDays(new DateOnly(2025, 12, 31), new DateOnly(2026, 12, 31)); // 253
```

Holiday names follow the current UI culture, in English, French or Dutch; `GetName(CultureInfo)` asks for a given language. The days off of some employers only are opt-in, through a combination of `HolidaySet` flags:

| `HolidaySet` | Days |
| --- | --- |
| `Legal` (default) | 1 January, Easter Monday, 1 May, Ascension Day, Whit Monday, 21 July, 15 August, 1 November, 11 November, 25 December |
| `FlemishCommunity` | 11 July |
| `FrenchCommunity` | 27 September |
| `GermanSpeakingCommunity` | 15 November |
| `FederalPublicService` | 2 November, 15 November, 26 December |

```csharp
var federal = HolidaySet.Legal | HolidaySet.FederalPublicService;

BelgianCalendar.AddBusinessDays(new DateOnly(2026, 10, 30), 1, federal);  // 2026-11-03: 2 November is off
```

`AddBusinessDays` never counts the starting date. `CountBusinessDays` counts the business days after the start, up to and including the end, so that it is the exact inverse of `AddBusinessDays`, backward too.

Not modelled: a holiday falling on a weekend stays there, since its replacement day is set by each employer under labour law; the afternoon of 22 July off for federal staff; regional days off and school holidays. The rules in force today apply to every year.

## Error handling

Every identifier type offers the same members:

| Member | Throws | Use it to |
| --- | --- | --- |
| `Parse(string)` | `BelgianFormatException` | read a value known to be valid |
| `TryParse(string?, out T)` | never | check a value, without the reason |
| `TryParse(string?, out T, out BelgianValidationError)` | never | check a value and explain what is wrong |
| `Validate(string?)` | never | get the error, or `null` for a valid value |

Each one also exists for `ReadOnlySpan<char>`. There is no result type to adopt: the error composes with whatever the application already uses.

```csharp
if (!EnterpriseNumber.TryParse(input, out var number, out var error))
{
    // error.Code    == BelgianErrorCode.InvalidChecksum
    // error.Message == "The enterprise number is invalid: its check digits do not match. Please check it for typing errors."
    return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["number"] = [error.Message] });
}
```

`BelgianValidationError` gives:

- `Code`, a `BelgianErrorCode` whose values never change, safe to store or to send to a client: `Empty`, `InvalidCharacter`, `InvalidLength`, `InvalidCountryCode`, `InvalidFirstDigit`, `InvalidChecksum`, `InvalidBirthDate`.
- `Message`, in the current UI culture, and `GetMessage(CultureInfo)`, in English, French or Dutch. In French, the message above reads "Le numéro d'entreprise est invalide : ses chiffres de contrôle ne correspondent pas. Vérifiez qu'il ne contient pas de faute de frappe."
- `Position`, the index of an invalid character, which the message shows counted from 1.
- `Expected`, the check digits that would match the other digits. It is meant for logs and support, and deliberately left out of the message: a mismatch does not tell which digit is wrong, and suggesting new check digits to a user would turn a mistyped number into a valid but wrong one.
- `TypeName`, the type whose parser failed.

Creating an error does not allocate; the message is built only when it is read. `Parse` throws a `BelgianFormatException`, a `FormatException` whose `Error` property carries the same error.

`default(EnterpriseNumber)`, and likewise for the other identifiers, is not a valid value: `IsEmpty` is `true`, it formats as an empty string, and parsing never returns it.

## ASP.NET Core and JSON

**System.Text.Json.** The identifiers carry their converter: there is nothing to register, with or without the source generator. JSON holds them without presentation characters, as `"0202239951"`, `"123456789002"` and `"BE68539007547034"`, and reading accepts any form that `Parse` accepts. An invalid value throws a `JsonException` with the localized message and a `BelgianFormatException` as inner exception. An optional value is a nullable member: `null` is not a valid `EnterpriseNumber`, and the empty `default` value cannot be written. Identifiers can also be dictionary keys.

**Validation attributes.** `[BelgianEnterpriseNumber]`, `[BelgianStructuredCommunication]` and `[BelgianIban]` validate strings, or the identifier types, and report the localized message of the error. As with `[RegularExpression]`, `null` and the empty string are left to `[Required]`.

```csharp
public sealed class SupplierForm
{
    [Required, BelgianEnterpriseNumber]
    public string? EnterpriseNumber { get; set; }

    [BelgianIban]
    public string? Account { get; set; }
}
```

**Binding.** The identifiers implement `IParsable<T>`, so ASP.NET Core binds them from a route or a query string. An invalid value then gets a plain 400, without its reason: to tell the user what is wrong, take a string and call the `TryParse` overload that returns the error.

### Sample API

[`samples/Hubertech.Belgium.Samples.Api`](https://github.com/Dampsey/Hubertech.Belgium/tree/main/samples/Hubertech.Belgium.Samples.Api) shows the error handling end to end, in a minimal API published with Native AOT. `POST /invoices` takes the identifiers as typed by a user and lists every invalid field in a problem details response (RFC 9457), with a JSON pointer, the stable code and the message in the language of the `Accept-Language` header:

```http
POST /invoices
Accept-Language: fr-BE

{ "supplier": "0202.239.952", "account": "NL91 ABNA 0417 1643 00" }
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "The invoice is not valid.",
  "status": 400,
  "errors": [
    { "pointer": "#/supplier", "code": "InvalidChecksum", "detail": "Le numéro d'entreprise est invalide : ses chiffres de contrôle ne correspondent pas. Vérifiez qu'il ne contient pas de faute de frappe." },
    { "pointer": "#/account", "code": "InvalidCountryCode", "detail": "Seuls les IBAN belges sont acceptés (commençant par BE)." },
    { "pointer": "#/reference", "code": "Empty", "detail": "La communication structurée est vide." }
  ]
}
```

It also binds an `EnterpriseNumber` from the route, and moves dates by business days. Its [`.http` file](https://github.com/Dampsey/Hubertech.Belgium/blob/main/samples/Hubertech.Belgium.Samples.Api/Hubertech.Belgium.Samples.Api.http) sends these requests by hand.

## Performance

Parsing and formatting into a span never allocate, for valid and invalid input alike: a unit test measures it on every build. Indicative figures, measured with BenchmarkDotNet on a shared cloud virtual machine (Intel Xeon 2.1 GHz, .NET 10.0.12, short runs), so orders of magnitude rather than precise numbers:

| Operation | Time | Allocated |
| --- | ---: | ---: |
| Parse an enterprise number, `BE 0202.239.951` | 20 ns | 0 B |
| Parse an invalid enterprise number, error included | 20 ns | 0 B |
| Parse a structured communication | 40 ns | 0 B |
| Parse an IBAN | 35 ns | 0 B |
| Format an IBAN into a span | 30 ns | 0 B |
| `BelgianIban.ToString()` | 65 ns | 64 B, the string |
| Read a JSON payment holding the three identifiers | 650 ns | 168 B |
| The same payment holding plain strings, unchecked | 420 ns | 280 B |
| `BelgianCalendar.IsBusinessDay` | 35 ns | 0 B |
| `BelgianCalendar.CountBusinessDays` over a year | 20 µs | 0 B |

Reading typed identifiers from JSON costs about half as much time again as reading unchecked strings, and allocates less, since no string is created for them. Business day arithmetic is linear in the number of calendar days, about 50 ns per day: negligible for payment terms and deadlines, 2 ms for a century.

The benchmarks are in [`benchmarks`](https://github.com/Dampsey/Hubertech.Belgium/tree/main/benchmarks/Hubertech.Belgium.Benchmarks); [CONTRIBUTING.md](https://github.com/Dampsey/Hubertech.Belgium/blob/main/CONTRIBUTING.md) tells how to run them.

## Trimming and Native AOT

The library uses no reflection: it is marked trimmable and AOT compatible, and the analyzers check it on every build. In a Native AOT application, the French and Dutch messages are compiled into the executable.

The messages need culture data. In globalization-invariant mode, which the `webapiaot` template of ASP.NET Core turns on with `<InvariantGlobalization>true</InvariantGlobalization>`, no culture but the invariant one can be created; even with `PredefinedCulturesOnly` turned off, a regional culture such as `fr-BE` then loses its parent and gets English messages. Keep culture data to serve French and Dutch, as the sample does.

## Sources

| Rule | Source |
| --- | --- |
| Structure of the enterprise number: ten digits, the last two being check digits | [Royal Decree of 24 June 2003](https://www.ejustice.just.fgov.be/cgi_loi/change_lg.pl?language=nl&la=N&cn=2003062432&table_name=wet), as amended |
| Enterprise numbers starting with 1 | [FPS Economy](https://news.economie.fgov.be/228779-les-numeros-d-entreprise-passent-au-1/) |
| Check digits of the enterprise number: 97 minus the first eight digits modulo 97 | No official text found: see below |
| Check digits of the structured communication | [Febelfin banking standards](https://febelfin.be/en/publications/2023/febelfin-banking-standards-for-online-banking) |
| Format of the Belgian IBAN | [SWIFT IBAN registry](https://www.swift.com/standards/data-standards/iban-international-bank-account-number) |
| Bank codes | [National Bank of Belgium](https://www.nbb.be/en/payment-systems/payment-standards/bank-identification-codes) |
| Structure of the national register number and its check digits, including for a birth from 2000 | Royal Decree of 3 April 1984; [National Register, instruction TI000](https://www.ibz.rrn.fgov.be/sites/default/files/documents/fr/registre-national/instructions/liste-TI/TI000_Numero-identification.pdf) |
| Month of birth of the BIS number, increased by 20 or 40 | [Royal Decree of 8 February 1991](https://www.ksz-bcss.fgov.be/fr/page/arrete-royal-du-8-fevier-1991), article 2 |
| Restricted use of the national register number | Law of 8 August 1983 organising a National Register, article 8 |
| Legal holidays | [Royal Decree of 18 April 1974](https://www.ejustice.just.fgov.be/eli/arrete/1974/04/18/1974041801/justel) |
| Days off of the federal public services | [Royal Decree of 19 November 1998](https://www.ejustice.just.fgov.be/cgi_loi/change_lg_2.pl?language=fr&nm=1998002123&la=F), article 14 |
| Days of the Flemish and French communities | Decrees of 7 November 1990 and 3 July 1991 |
| Day of the German-speaking Community | No decree reference found |
| Easter Sunday | Anonymous Gregorian algorithm (Meeus, Jones, Butcher), checked against python-dateutil from 2020 to 2035 |

The Royal Decree of 24 June 2003 sets where the check digits of the enterprise number are, but no official text describing how they are computed was found. The rule applied here is the one that published enterprise numbers satisfy and that other validation libraries, such as python-stdnum, implement. Links to official texts for the rules marked as unsourced, and corrections, are welcome through an issue.

## Roadmap

Candidates for the next versions, none of them committed yet:

- German messages and holiday names, the third official language of Belgium.
- The establishment unit number, once its check digit rule is confirmed in an official text.
- The BIC of a Belgian IBAN, from the bank codes of the National Bank of Belgium.
- Postal codes.
- Separate packages for Entity Framework Core value converters and FluentValidation rules.

## Contributing

See [CONTRIBUTING.md](https://github.com/Dampsey/Hubertech.Belgium/blob/main/CONTRIBUTING.md). Design decisions are recorded in [`docs/adr`](https://github.com/Dampsey/Hubertech.Belgium/tree/main/docs/adr).

## License

[MIT](https://github.com/Dampsey/Hubertech.Belgium/blob/main/LICENSE), by [Dampsey](https://github.com/Dampsey).
