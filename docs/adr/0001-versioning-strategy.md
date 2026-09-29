# 1. Versioning strategy

- Status: Accepted
- Date: 2026-09-29

## Context

Hubertech.Belgium is published on NuGet.org. Consumers rely on Semantic Versioning to decide whether an update is safe, so the version must be:

- impossible to forget to bump, and never out of sync with the release tag;
- reproducible from the sources alone, on any machine;
- the same in the package, the assembly metadata and the Git history.

Separately from the version number itself, a breaking change to the public API must be visible in code review, not discovered by a consumer after the release.

## Options

1. **Hand-edited `<Version>` in the project file.** Simple, but the version and the release tag can drift, and every release needs a "bump version" commit.
2. **Nerdbank.GitVersioning.** A `version.json` file plus the Git height. Powerful, but introduces a configuration file and concepts (version height, public release refs) that a single-package, single-maintainer repository does not need.
3. **GitVersion.** Derives versions from branching strategies (GitFlow, GitHub Flow). Its configuration surface is designed for multi-branch release processes, not for a trunk-based repository.
4. **MinVer.** The version is the latest reachable Git tag. Commits after a tag get a pre-release version with the commit height. No configuration file.

## Decision

- **MinVer**, with the tag prefix `v` (`v0.1.0`). It is referenced with `PrivateAssets="all"`: it runs at build time and is not a dependency of the package.
- **Semantic Versioning 2.0.** While the major version is `0`, a minor release may contain breaking changes (SemVer, item 4); they are always listed in `CHANGELOG.md`.
- **Assembly versions use MinVer defaults**: `AssemblyVersion` is `{major}.0.0.0` (it only changes with the major version, which avoids needless binding breaks), `FileVersion` is `{major}.{minor}.{patch}.0`, and `InformationalVersion` carries the full version and the commit SHA.
- **The public API is tracked** with `Microsoft.CodeAnalysis.PublicApiAnalyzers`. Any change to the public surface is a diff in `PublicAPI.Unshipped.txt` and is reviewed like code. At release time, its content moves to `PublicAPI.Shipped.txt`.
- **Package validation is enabled.** Once v0.1.0 is published, `PackageValidationBaselineVersion` is set to the latest published version so that `dotnet pack` reports binary and source breaking changes against it.

## Consequences

- Releasing is pushing a tag: `git tag v0.1.0 && git push origin v0.1.0`. There is no version to edit in the sources.
- CI must clone the full history (`fetch-depth: 0`); with a shallow clone, MinVer cannot see the tags.
- Untagged builds, including local ones, produce pre-release versions (for example `0.1.1-alpha.0.3`), which cannot be mistaken for a release.
- A tag pushed on the wrong commit produces a wrong release. Tags are only created on `main`, after CI is green.
