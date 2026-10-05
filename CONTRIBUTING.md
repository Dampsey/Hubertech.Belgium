# Contributing

Thank you for your interest. Issues and pull requests are welcome, in English, French or Dutch.

## Building and testing

The only prerequisite is the .NET 10 SDK (`global.json` accepts any 10.0 feature band from 10.0.100).

```shell
dotnet build -c Release    # every warning is an error
dotnet test -c Release
```

The sample API and its requests:

```shell
dotnet run --project samples/Hubertech.Belgium.Samples.Api
# then send the requests of samples/Hubertech.Belgium.Samples.Api/Hubertech.Belgium.Samples.Api.http
```

The benchmarks, which take a few minutes:

```shell
dotnet run -c Release --project benchmarks/Hubertech.Belgium.Benchmarks -- --filter '*'
```

## Rules of the repository

- **No business rule without a source.** A Belgian rule comes with an official source, cited in the XML documentation or in the README. When a rule is uncertain, open an issue rather than guessing; the README says which rules could not be confirmed in an official text.
- **Tests come with the change**, in the same commit or just before it. Check digit rules also get property-based tests (CsCheck), and the parsing and formatting paths must not allocate: `AllocationTests` measures it.
- **The public API is tracked.** Every public member has XML documentation and is listed in `src/Hubertech.Belgium/PublicAPI.Unshipped.txt`; the build fails otherwise.
- **Messages exist in English, French and Dutch.** A new message goes into the three `.resx` files; `ErrorMessagesTests` fails when a translation is missing.
- **Commits are small and follow [Conventional Commits](https://www.conventionalcommits.org/)**: `feat:`, `fix:`, `test:`, `refactor:`, `docs:`, `build:`, `ci:`, `chore:`. One intention per commit.
- **Structuring decisions are recorded** in [`docs/adr`](docs/adr). A record is never rewritten: a reversed decision gets a new record, a precision gets a dated amendment.
- **No emoji**, in code, commits or documentation.

## Translations

The Dutch messages and holiday names have not been reviewed by a native speaker yet: a review is very welcome. German, the third official language of Belgium, is on the roadmap.
