# 6. JSON and validation integrations

- Status: Accepted
- Date: 2026-10-05

## Context

The identifiers travel in JSON bodies and are typed by users in forms. Without help, an application writes a converter for System.Text.Json and a validation attribute for each type, and the clear errors of ADR 0002 stop at that boundary. Both System.Text.Json and System.ComponentModel.DataAnnotations are part of the shared framework, so supporting them adds no dependency to the package. The integrations must also work with the System.Text.Json source generator and under Native AOT, like the rest of the library.

## JSON

### Options

1. **Converters registered by the application**, in `JsonSerializerOptions.Converters` or in the source generation options. Explicit, but forgetting it fails at run time, and each application repeats it.
2. **A `[JsonConverter]` attribute on each type.** Nothing to register. The source generator honours it, provided the converter is public and has a public parameterless constructor: with an internal converter it reports SYSLIB1220 and generates no metadata for the type, as a trial showed. A converter registered in the options still takes precedence over the attribute.

### Decision

Option 2, with three public converters in `Hubertech.Belgium.Serialization`, a namespace applications never need to import.

- **Written without presentation characters**: `0202239951`, `123456789002`, and the IBAN in its electronic format, `BE68539007547034`. JSON is read by programs, which format values for display themselves, as they do with dates written in ISO 8601. The formatted forms of `ToString()` remain for people.
- **Read in any form the parsers accept**, since JSON often carries what a user typed. Reading copies the string to the stack and parses it there: valid values allocate nothing.
- **An invalid string** throws a `JsonException` whose message is the localized message of the validation error, with a `BelgianFormatException` carrying the error as inner exception. The serializer sets the `Path` of the exception, such as `$.supplier.enterpriseNumber`.
- **A token that is not a string**, `null` included, throws a `JsonException` without a message, so that the serializer writes its usual one with the path. An optional value is a nullable member, which the serializer handles before calling the converter.
- **The empty `default` value cannot be written.** Writing `""` would produce JSON that the converter itself refuses to read, and writing `null` would break the type of the member. An empty value in an object being serialized is a bug, reported where it happens. `JsonIgnoreCondition.WhenWritingDefault` leaves such members out for applications that want it.
- **Dictionary keys** are supported, since identifiers are natural keys.
- **One format only.** A converter taking the format as a parameter can be added later without breaking anything; until someone needs it, it would be API to maintain.

A Native AOT application that does not use JSON keeps the same size: the attribute does not pull System.Text.Json into it.

## Validation attributes

### Decision

`[BelgianEnterpriseNumber]`, `[BelgianStructuredCommunication]` and `[BelgianIban]` derive from `ValidationAttribute`, in the root namespace since applications write them on their models.

- **The message is the one of `BelgianValidationError`**, in the current UI culture, so a form shows "the check digits do not match" rather than "the field is invalid". Setting `ErrorMessage` or `ErrorMessageResourceName` replaces it, as with any validation attribute: the attributes never look up resources by reflection themselves.
- **`null` and the empty string are valid**, as with `RegularExpressionAttribute`: requiring a value is the business of `[Required]`. White space is not a missing value: it fails with the `Empty` error, so that any other string that passes validation parses.
- **The identifier types are accepted too.** A non-nullable identifier left unset, for example a JSON property that is missing, is empty, and `[Required]` cannot see it since a structure is never `null`; the attribute reports it.
- **Any other type throws an `InvalidOperationException`.** Applying the attribute to an `int` is a programming error, which should surface at the first test rather than as a validation message shown to users.
- **The member name is reported** in the `ValidationResult`, as the framework's own attributes do, so that ASP.NET Core and Blazor attach the message to the right field.

## Implementation

An internal interface, `IBelgianIdentifier<TSelf>`, gathers what the integrations need: `IsEmpty`, the rich `TryParse` as a static abstract member, and `TryFormat`. The converters and the attributes each have one generic implementation, called without boxing; the public classes are thin wrappers that fix the type and the format.

## Consequences

- Identifiers work in JSON bodies and in validated models without configuration.
- The public API grows by six classes, three converters and three attributes, which the public API files track.
- The JSON form differs from `ToString()`. This is documented on each converter.
