# 3. Localization strategy

- Status: Accepted
- Date: 2026-09-29

## Context

Error messages must be displayable as is, in English, French and Dutch. The library must remain trimmable, compatible with Native AOT, and free of dependencies beyond the base class library. Applications usually set the culture per request (ASP.NET Core request localization sets `CultureInfo.CurrentUICulture`).

## Options

1. **`.resx` files, `ResourceManager` and satellite assemblies.** The standard .NET mechanism: culture fallback built in, format known by translators and tools.
2. **Messages hard-coded in C#,** selected with a `switch` on the language. No satellite assemblies, but no culture fallback, and translating means editing code.
3. **`IStringLocalizer`** (Microsoft.Extensions.Localization). Adds a dependency and assumes dependency injection; a library should not need a container to produce a message.
4. **No messages, codes only,** leaving translation to the application. Fails the goal of messages usable as is.

## Decision

Option 1.

- `Resources/ErrorMessages.resx` holds English, declared as the neutral language (`<NeutralLanguage>en</NeutralLanguage>`), so English is served from the main assembly without probing. `ErrorMessages.fr.resx` and `ErrorMessages.nl.resx` produce the satellite assemblies.
- The `ResourceManager` is created from a type, `new ResourceManager(typeof(ErrorMessages))`. MSBuild names the embedded resource after the class declared next to the `.resx` file, so there is no resource name to keep in sync by hand.
- One key per type and error code, `{TypeName}_{Code}`, whose value is a complete sentence. Sentences are never assembled from fragments: French needs "le numéro", "la communication" and "l'IBAN", and Dutch word order differs from English.
- Placeholders only where data varies: the character and position of an `InvalidCharacter` error.
- French messages follow French typography: a non-breaking space before a colon and inside guillemets.
- The culture is `CultureInfo.CurrentUICulture` when the message is read, or the culture passed to `GetMessage`.
- Unit tests guard the catalog: every key exists in every language, every value is actually translated, and placeholders are consistent.

## Consequences

- The package ships the `fr` and `nl` satellite assemblies.
- A Native AOT publication of a consuming application produces no trimming or AOT warning; the satellite resources are compiled into the executable, and French and Dutch messages are served. This was verified with a throwaway console application.
- With `InvariantGlobalization=true`, common in container images, only the invariant culture can be created by default. With `PredefinedCulturesOnly=false`, other cultures can be created but lose their parent chain: `fr-BE` then falls back to English, and only `fr` and `nl` return translated messages. This is a .NET behavior to document for consumers, not something the library can work around.
- German, the third official language of Belgium, is not supported in v0.1. Adding it means one `.resx` file and one line in the tests; it is on the roadmap.
- Translations must be reviewed by native speakers before the first release.
