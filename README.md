# Hubertech.Belgium

Belgian administrative identifiers and rules for .NET, in one dependency-free package.

> **Status:** under active development. Nothing is published on NuGet yet and the API may change until v0.1.0.

## Scope of v0.1

| Type | What it covers |
| --- | --- |
| `EnterpriseNumber` | Belgian enterprise number (BCE/KBO): parsing, check digits, formatting. |
| `StructuredCommunication` | Structured payment reference (`+++123/4567/89002+++`): parsing, generation, formatting. |
| `BelgianIban` | Belgian IBAN: ISO 7064 and national check digits, paper and electronic formats. |
| `BelgianCalendar` | Belgian public holidays and business day arithmetic. |

Design goals:

- **Consistent API across types**: `Parse`, `TryParse`, `Validate`, `IParsable<T>`, `ISpanFormattable`.
- **Errors made for end users**: stable error codes, localized messages (English, French, Dutch), and the expected check digits when a checksum fails.
- **Modern .NET**: `net10.0`, value types, no allocation on the success path, trimming and Native AOT compatible, no dependency beyond the base class library.

This package validates the **syntax and check digits** of identifiers only. It never calls a remote service: a syntactically valid enterprise number is not necessarily active, nor registered for VAT.

## License

[MIT](LICENSE)
