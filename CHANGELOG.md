# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `EnterpriseNumber`: the Belgian enterprise number (BCE/KBO). Accepts separators, the BE prefix and legacy nine-digit numbers, checks the first digit and the mod 97 check digits, and formats as `0202.239.951`, `0202239951` or `BE0202239951`.
- `StructuredCommunication`: the Belgian structured payment reference (OGM/VCS). Accepts `+++` or `***` markers and separators, checks the mod 97 check digits (97 when the remainder is 0), builds a reference from a number with `FromNumber`, and formats as `+++123/4567/89002+++`, `***123/4567/89002***` or `123456789002`.
- `BelgianIban`: the Belgian IBAN. Accepts separators and the BE prefix in any case, rejects other countries, checks both the ISO 7064 IBAN check digits and the Belgian account number check digits, exposes `BankCode` and `AccountNumber`, and formats as `BE68 5390 0754 7034` or `BE68539007547034`.
- `BelgianErrorCode`: stable error codes, safe to store or to send to clients.
- `BelgianValidationError`: why a value is invalid, with a message in English, French or Dutch that can be shown to end users as is.
- `BelgianFormatException`: a `FormatException` carrying a `BelgianValidationError`.
