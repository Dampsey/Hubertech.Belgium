# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `SocialSecurityIdentificationNumber`: the Belgian social security identification number (NISS/INSZ), which is either a national register number or a BIS number. Accepts separators, checks the month of birth and the mod 97 check digits of both centuries, tells the register (`Kind`) and the date of birth when it is complete (`BirthDate`). `ToString()` masks every digit but the check digits, `**.**.**-***.28`; the formats `D` and `N` give the full number.
- `BelgianErrorCode.InvalidBirthDate`, for a month of birth that no register uses.
- JSON support for `SocialSecurityIdentificationNumber`, which writes the full eleven digits although `ToString()` masks them, and the validation attribute `[BelgianSocialSecurityIdentificationNumber]`.
- `BelgianIban.Bic`: the BIC that the list of bank identification codes of the National Bank of Belgium assigns to the bank code, as published, or `null` when the list gives none. The package embeds the list of 1 September 2026.

## [0.1.0] - 2026-10-05

First release.

### Added

- `EnterpriseNumber`: the Belgian enterprise number (BCE/KBO). Accepts separators, the BE prefix and legacy nine-digit numbers, checks the first digit and the mod 97 check digits, and formats as `0202.239.951`, `0202239951` or `BE0202239951`.
- `StructuredCommunication`: the Belgian structured payment reference (OGM/VCS). Accepts `+++` or `***` markers and separators, checks the mod 97 check digits (97 when the remainder is 0), builds a reference from a number with `FromNumber`, and formats as `+++123/4567/89002+++`, `***123/4567/89002***` or `123456789002`.
- `BelgianIban`: the Belgian IBAN. Accepts separators and the BE prefix in any case, rejects other countries, checks both the ISO 7064 IBAN check digits and the Belgian account number check digits, exposes `BankCode` and `AccountNumber`, and formats as `BE68 5390 0754 7034` or `BE68539007547034`.
- `BelgianIban.FromLegacyAccountNumber`: converts a Belgian account number in its national notation, `539-0075470-34`, to its IBAN; `TryFromLegacyAccountNumber` reports errors that talk about the account number.
- `BelgianCalendar.GetHolidays` and `IsHoliday`: the ten Belgian legal holidays, with Easter computed rather than tabulated, and the optional days off of the communities and of the federal public services (`HolidaySet`), with names in English, French and Dutch.
- `BelgianCalendar.IsBusinessDay`, `AddBusinessDays` and `CountBusinessDays`: business day arithmetic that skips weekends and the requested holidays; counting excludes the start and includes the end, so that it is the inverse of `AddBusinessDays`.
- JSON converters for `EnterpriseNumber`, `StructuredCommunication` and `BelgianIban`, applied without registration and compatible with the System.Text.Json source generator. Values are written without presentation characters (`0202239951`, `123456789002`, `BE68539007547034`), read in any form the parsers accept, and can be dictionary keys. An invalid value throws a `JsonException` with the localized message, carrying the validation error.
- Validation attributes `[BelgianEnterpriseNumber]`, `[BelgianStructuredCommunication]` and `[BelgianIban]`, for strings and for the identifier types, reporting the localized message of the validation error. `null` and the empty string are left to `[Required]`; an empty identifier, such as a non-nullable member left unset, is reported.
- `BelgianErrorCode`: stable error codes, safe to store or to send to clients.
- `BelgianValidationError`: why a value is invalid, with a message in English, French or Dutch that can be shown to end users as is.
- `BelgianFormatException`: a `FormatException` carrying a `BelgianValidationError`.

[Unreleased]: https://github.com/Dampsey/Hubertech.Belgium/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/Dampsey/Hubertech.Belgium/releases/tag/v0.1.0
