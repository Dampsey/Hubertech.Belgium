# 2. Error model

- Status: Accepted
- Date: 2026-09-29

## Context

The library exists to validate user input. A developer must be able to:

- validate a value in one line, without exceptions;
- know *why* it failed, through a code that is stable enough to be stored or sent to a client;
- show the end user a message that can be displayed as is, in their language, and says what to correct;
- plug the result into whatever result type the application already uses (ErrorOr, FluentResults, OneOf, a custom one).

Parsing must not allocate on the success path.

## Options

1. **Exceptions only** (`Parse` and `catch`). Expensive and awkward for form validation, where invalid input is expected.
2. **Standard `TryParse(..., out T)` only.** Cheap, but says nothing about the reason.
3. **A `Result<T>` type of our own.** Forces yet another result type on applications that already have one.
4. **`TryParse(..., out T, out BelgianValidationError)`, `Validate`, and `Parse` throwing an exception that carries the same error.** The pattern is known from the BCL and composes with any result type.

## Decision

Option 4. Each parsable type exposes the same members:

| Member | Throws | Purpose |
| --- | --- | --- |
| `Parse(string, IFormatProvider?)`, `Parse(ReadOnlySpan<char>, IFormatProvider?)` | `BelgianFormatException` | `IParsable<T>`, `ISpanParsable<T>` |
| `TryParse(string?, IFormatProvider?, out T)`, `TryParse(ReadOnlySpan<char>, IFormatProvider?, out T)` | never | `IParsable<T>`, `ISpanParsable<T>`; used by ASP.NET Core binding |
| `TryParse(string?, out T, out BelgianValidationError)`, `TryParse(ReadOnlySpan<char>, out T, out BelgianValidationError)` | never | validation with details |
| `Validate(string?)` | never | returns the error, or `null` when the value is valid |

The span overload of the detailed `TryParse` was added to the initial design for consistency: every entry point accepts both strings and spans.

### `BelgianValidationError` is a `readonly record struct`

- Creating an error does not allocate, which matters when validating large batches (an import with many invalid rows).
- It fits the `out` parameter: on success, the method returns `default`, which means "no error".
- Equality and `ToString` come for free, and `ToString` lists every detail for logs.
- Its constructor is private. Errors only come from parsers, so their details are always consistent. A test double is obtained by parsing a known invalid input.

The cost is that `default` must mean something: it is the "no error" value, with the code `None`.

### `BelgianErrorCode`

- `None = 0`, so that `default(BelgianValidationError)` does not read as `Empty`.
- Explicit values, never renumbered nor reused. They are pinned by a unit test and by the public API files.
- Only codes that the v0.1 parsers actually produce. `InvalidPrefix` and `OutOfRange`, listed in the initial design, are left out: an enum value is a permanent promise, and adding one later is not a breaking change whereas removing one is. `OutOfRange` would only have served `StructuredCommunication.FromNumber`, whose argument comes from code, not from a user; an out-of-range value there is a programming error, reported with `ArgumentOutOfRangeException`.
- The type is named `BelgianErrorCode` rather than `ErrorCode`, which would collide with the many `ErrorCode` types of consuming applications.

### Message

- `Message` is built when it is read, in `CultureInfo.CurrentUICulture`. `GetMessage(CultureInfo)` takes an explicit culture, for a background job, a test, or an HTTP request whose culture is known.
- Each message is a complete sentence per type and code (see [ADR 0003](0003-localization-strategy.md)).

### `Position`

The zero-based index of the offending character in the input as given, separators included. The message shows it one-based, as users count. Invisible characters (control characters, non-breaking spaces, lone surrogates) are shown as a code point, such as `U+00A0`.

### `Expected` is exposed, but not suggested to end users

For `InvalidChecksum`, `Expected` gives the check digits that would match the other digits. The initial design wanted the message to say "the last two digits should be 51". This was rejected.

A mod 97 checksum detects every single-digit error, but cannot locate it. Assuming a typing error is equally likely on each digit of an enterprise number, 8 times out of 10 it is in the first eight digits. The "expected" check digits are then wrong advice: a user who follows it turns a mistyped number into a valid number that belongs to another enterprise, which the checksum existed to prevent. The same holds for the IBAN (12 digits out of 14) and the structured communication (10 out of 12).

The message therefore asks the user to check for a typing error, and `Expected` remains available for diagnostics and support.

### `TypeName` is a `string`, not a `Type`

It says which parser failed. A string can be logged and serialized as is, whereas System.Text.Json refuses to serialize `System.Type`.

### `BelgianFormatException`

A sealed subclass of `FormatException`, so existing `catch (FormatException)` blocks keep working. It carries the error and uses its message. It has no parameterless constructor and rejects an error with the code `None`: an exception without an error would break the contract.

## Consequences

- Parsing and validation do not allocate until the message is read.
- Two reads of `Message` may return different languages if the UI culture changed in between. This is intended: the message follows the reader.
- Consumers that switch on `BelgianErrorCode` must handle values added in future versions.
- Applications that want to show the expected check digits can do so explicitly through `Expected`, knowing the risk.
