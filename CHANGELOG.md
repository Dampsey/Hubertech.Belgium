# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- `BelgianErrorCode`: stable error codes, safe to store or to send to clients.
- `BelgianValidationError`: why a value is invalid, with a message in English, French or Dutch that can be shown to end users as is.
- `BelgianFormatException`: a `FormatException` carrying a `BelgianValidationError`.
