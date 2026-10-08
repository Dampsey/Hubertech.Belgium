# 8. Embedded reference data

- Status: Accepted
- Date: 2026-10-08

## Context

The first three digits of a Belgian account number are a bank identification code. The National Bank of Belgium allocates these codes and publishes their list, with the BIC of the institution that holds each code, on its [bank identification codes page](https://www.nbb.be/en/payments-and-securities/bank-identification-codes), as PDF and as Excel. The page states that the list is updated each time a change occurs, and that for a specific use the institution concerned or SWIFT should be consulted.

Until now, every rule of the library was an algorithm: check digits, the date of Easter. The BIC of a bank code is data, which ages: banks merge, close and start, and the National Bank publishes a new list when they do.

The grouped list of 1 September 2026, `grouped_list_current.xlsx`, has one row per range of codes, with the columns `From`, `To`, `Biccode` and the name of the institution in Dutch, French, German and English. Its rows cover every code from 000 to 999 exactly once. 183 ranges carry a BIC, 100 distinct ones. The others carry a placeholder: `VRIJ` (free) for 72 ranges, and `N/A`, `NAV` or `nav`, `NYA` or `-` for 17 ranges, which are either codes marked unavailable or codes of an institution that the list gives without a BIC. 16 BICs have eleven characters, 4 of them ending with `XXX`.

Other libraries ship this data too; python-stdnum, for one, under the LGPL. Taking it from them would mean depending on their licence and on their update cycle, for data that the National Bank publishes itself.

## Options

1. **Download the list at run time.** Always current, but a validation library would perform network calls, need a cache and a fallback, and depend on the availability of a web site.
2. **Let the application provide the list.** Flexible, but every application would have to find, parse and refresh the file: the library would no longer answer the question.
3. **Embed the file and parse it at run time.** Reading an Excel file needs a zip archive and XML parsing at startup, for data that does not change between two releases of the package.
4. **Generate a C# table from the official file, and embed it.** No I/O, no parsing at run time, compatible with trimming and Native AOT; the data is as old as the package.

## Decisions

- **A generated table, option 4.** `tools/UpdateBankCodes.cs` reads the official grouped list and writes `src/Hubertech.Belgium/BankCodes.Generated.cs`: the ranges that have a BIC, in ascending order. The header of the generated file gives the date of the list and its address. The tool is a .NET 10 file-based app, run with `dotnet run`, outside the solution so that the build does not depend on it; CI builds it so that it does not break unnoticed.
- **Only the official file.** The table comes from the file of the National Bank and from nothing else. The Excel file itself is not committed: the National Bank publishes it, and the generated file records which version was used.
- **The tool fails rather than guesses.** It stops, writing nothing, when the header is not the expected one, when a code is missing or listed twice, or when a cell is neither a BIC nor one of the placeholders listed above. A new placeholder, or a change of layout, is then looked at by a person instead of turning silently into a missing or a wrong BIC.
- **BICs as published.** A BIC of eleven characters is kept as is, including the four ending with `XXX`, which designates the main office: removing it would be a correction that the list does not make. The documentation tells consumers to compare the first eight characters when only the bank matters.
- **`Bic` is information, not validation.** An IBAN whose code has no BIC, or is free, stays valid: validation must not depend on data that ages, and a code may be allocated after the release of the package. `null` covers a free code, an unavailable one and an institution without a BIC: the placeholders do not separate the last two, and the difference does not matter to an application that looks for a BIC.
- **No allocation.** The BICs are string literals, found by binary search among the ranges.
- **A new list is a new version of the package.** A release that only updates the list is a patch version. The README gives the date of the embedded list, and the changelog the date of each new one.

## Consequences

- The package grows by a few kilobytes, and the library has its first data that ages. Applications that need the current BIC, for instance to send payments, should check it with the bank or SWIFT, as the National Bank recommends.
- Updating the list is a manual step for the maintainer, described in `CONTRIBUTING.md`. A scheduled workflow could run the tool and open a pull request when the list changes; it is not in place yet.
- The tests cite codes of the list of 1 September 2026. When a new list changes one of these codes, the tests fail and must be updated with the diff of the generated file, which makes every change of a cited code visible in review.
