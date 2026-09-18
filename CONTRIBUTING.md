# Contributing to Aviant

Thanks for helping. Bug reports, fixes, tests and documentation are all welcome.

## Before you start

- **Bugs:** open an issue with a minimal reproduction: the request or aggregate involved, what you expected, and what happened.
- **Features or API changes:** open an issue first to agree the shape. Aviant is used as a library, so public API changes need care.
- **Security issues:** do not open a public issue; see [SECURITY.md](SECURITY.md).

## Building

You need the .NET SDK pinned in `global.json` (10.0.x).

```bash
dotnet build Aviant.sln
dotnet test Aviant.sln
dotnet pack Aviant.sln -c Release -o artifacts   # optional: inspect the packages
```

The build fails on packages with known moderate, high or critical advisories (NU1902–NU1904). If a dependency picks one up, pin the patched version in `Directory.Packages.props` with a comment naming the advisory.

## Pull requests

- Keep each PR to one concern. Refactors go separately from behaviour changes.
- Behaviour changes and bug fixes come with tests (`tests/<Module>/Unit`, xunit v3 + AwesomeAssertions). Write the test first and watch it fail.
- Add an entry under **Unreleased** in [CHANGELOG.md](CHANGELOG.md).
- Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/) (`fix(kernel): …`, `feat(persistence): …`). Mark breaking changes with `!` and a `BREAKING CHANGE:` footer.
- CI must be green: build, tests and pack.

## Dependencies and licensing

Aviant is MIT and must stay usable in closed-source commercial software. Only add dependencies under permissive licences (MIT, Apache-2.0, BSD). MediatR stays below 13 and AutoMapper is not used. See the README section *Dependencies and Licensing*.

## Releasing (maintainers)

Tag the commit (`git tag v1.2.0 && git push origin v1.2.0`). The Release workflow builds, tests, packs, pushes to NuGet (when the `NUGET_API_KEY` secret is set) and creates the GitHub release. Move the **Unreleased** changelog entries under the new version first.
