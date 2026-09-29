# 4. Identifiers are value types

- Status: Accepted
- Date: 2026-09-29

## Context

Enterprise numbers, structured communications and IBANs are small immutable values, compared by value, and often parsed in bulk: imports, API requests, database reads. Parsing a valid value must not allocate. Whatever the representation, a type must have a clear answer to "what is an uninitialized value?".

## Options

1. **A sealed class** (or record class). `null` naturally represents "no value", and there is no invalid default state. But every parsed value is a heap allocation, and every use needs a null check.
2. **A `record struct`.** Equality comes for free, but the generated `ToString` must be replaced anyway, and positional records would expose the internal representation through their constructor, `Deconstruct` and `with` expressions.
3. **A `readonly struct` wrapping a compact representation**, with equality and formatting written explicitly.

## Decision

Option 3.

- **Representation.** The digits are stored as an integer: a `uint` for the ten digits of an enterprise number. Equality and hashing are a single comparison, the struct is four bytes, and formatting is done on demand. The input form is not kept: `0202.239.951` and `BE 202239951` are the same value.
- **Instances only come from parsing.** There is no public constructor apart from the implicit parameterless one, so a value obtained from the API has always passed validation.
- **`default` is the empty value, and it is not valid.** `IsEmpty` returns `true`, `ToString` and `TryFormat` produce an empty string, and parsing never returns it. `ToString` does not throw, because it is called by debuggers, loggers and string interpolation, which must never fail. An optional identifier is expressed with `Nullable<T>`, for example `EnterpriseNumber?`, not with `default`.
- **No ordering.** Identifiers do not implement `IComparable<T>`: sorting enterprise numbers numerically has no business meaning. It can be added later without breaking anything.

## Consequences

- Parsing and formatting do not allocate. A unit test measures it with `GC.GetAllocatedBytesForCurrentThread`, for valid and invalid input alike.
- `new EnterpriseNumber()` and uninitialized fields compile and produce the empty value. This is documented, and detectable with `IsEmpty`. How an empty value is serialized is decided with the JSON converter.
- Two values are equal if and only if they denote the same identifier, whatever the separators, case or legacy form used to write them.
