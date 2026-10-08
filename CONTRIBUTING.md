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

## Releasing

A release is made from `main`, by the maintainer:

1. Open a pull request that moves the content of `PublicAPI.Unshipped.txt` to `PublicAPI.Shipped.txt` and gives the `[Unreleased]` entries of `CHANGELOG.md` a section `## [x.y.z] - YYYY-MM-DD`, with its comparison link. Merge it once CI is green.
2. In the Actions tab, run the **Release** workflow on `main` with the version, for example `0.2.0`.
3. Approve the deployment of the `nuget` environment.

The workflow checks that the version is not released yet and has a section in the changelog, builds, tests and packs the commit, publishes the package to nuget.org, then creates the tag and the GitHub release with the section of the changelog. Pushing a tag `vx.y.z` on a commit of `main` runs the same workflow.

After the release, a pull request sets `PackageValidationBaselineVersion` in `src/Hubertech.Belgium/Hubertech.Belgium.csproj` to the released version.

## Updating the bank codes

`BelgianIban.Bic` reads `src/Hubertech.Belgium/BankCodes.Generated.cs`, generated from the [grouped list of bank identification codes](https://www.nbb.be/en/payments-and-securities/bank-identification-codes) of the National Bank of Belgium, which changes each time a code does. To take a new list:

```shell
dotnet run tools/UpdateBankCodes.cs                        # downloads the current list
dotnet run tools/UpdateBankCodes.cs -- path/to/list.xlsx   # or reads a copy downloaded beforehand
```

The tool writes nothing when the list does not have the expected shape: a code missing or listed twice, or a value that is neither a BIC nor a placeholder it knows. Never edit the generated file by hand. Review its diff, update the date of the list in the README, the XML documentation if needed and the changelog, and update the tests that cite a code whose BIC changed. A new list alone is released as a patch version.

## Translations

The Dutch messages and holiday names have not been reviewed by a native speaker yet: a review is very welcome. German, the third official language of Belgium, is on the roadmap.
