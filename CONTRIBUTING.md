# Contributing to EarthWorks

[Русская версия](CONTRIBUTING_RU.md)

Pull Requests are welcome for focused bug fixes, Valheim compatibility updates, tests, documentation, localization, and agreed roadmap work. EarthWorks is source-available rather than open source; read [LICENSE.md](LICENSE.md) before reusing or distributing code.

## Where help matters most

- **Players and testers:** reproduce one item from [TESTING.md](TESTING.md), attach the exact input sequence, screenshot, `Player.log`, world location, and whether a Heightmap seam was crossed.
- **Server owners:** test save/reload, restart, reconnect, ward permissions, version mismatch, and two-client project persistence without using the laboratory shortcuts.
- **Translators:** improve English or Russian wording, or propose a new complete language catalog with placeholder parity and no untranslated `$earthworks_*` tokens.
- **UI/UX contributors:** document where the current Route workflow is unclear before proposing a redesign; a short annotated capture is more useful than a speculative replacement UI.
- **Mod authors:** report Harmony targets, terrain limits, camera ownership, piece-table changes, or saved-terrain behavior that may overlap EarthWorks. Include mod/version, client/server requirements, and the smallest reproducible conflict.
- **C# contributors:** focused tests, Valheim API compatibility fixes, and agreed roadmap items are welcome. Do not copy GPL or other incompatible source into EarthWorks.

Use a GitHub Issue for findings or proposals and a focused Pull Request for an agreed fix. The best report states what was expected, what happened, exact versions, whether the result survived reload, and what remains unverified.

## Before editing

1. Read [the architecture map](docs/ARCHITECTURE.md), [product contract](PROJECT_CONTRACT.md), and [test protocol](TESTING.md).
2. For a large feature, open an Issue first so behavior and scope can be agreed.
3. Keep a Pull Request focused on one behavior or one mechanical refactor.

## Local setup

EarthWorks cannot redistribute Valheim, BepInEx, or Jotunn assemblies. Copy `Directory.Build.props.user.example` to `Directory.Build.props.user` and set your local paths, or set `EARTHWORKS_PROFILE_ROOT` and `VALHEIM_MANAGED_DIR`.

Required local versions for the current branch are listed at the top of `README.md`.

```powershell
dotnet build .\EarthWorks.sln -c Release
dotnet run --project .\tests\EarthWorks.GeometryTests\EarthWorks.GeometryTests.csproj -c Release
dotnet run --project .\tests\EarthWorks.Tests\EarthWorks.Tests.csproj -c Release
.\scripts\Audit-Localization.ps1
.\scripts\Audit-ValheimApi.ps1
```

Do not start Valheim or deploy to a profile as part of an automated test. Runtime verification is a separate, explicit step.

## Naming and compatibility

- Use `Road...` for the current route-project domain and `EarthWorks...` only for plugin-wide services.
- Name files after their primary type or responsibility.
- Keep Unity/Jotunn code out of `EarthWorks.Geometry`.
- Never renumber persisted enums. Add values at the end and update persistence tests.
- Increment `RoadProjectRecord` format only when the binary layout changes, while retaining readers for supported older versions.
- Add user-facing text to both JSON files under `Translations/EarthWorks`; never place Russian strings in C#.
- Comments should explain invariants, compatibility reasons, or non-obvious limits—not restate the code.

## Pull Request evidence

Explain the root cause, affected call path, compatibility impact, and exact checks run. A build proves compilation only. Mark in-game, multiplayer, save/reload, and visual behavior as unverified until tested with `TESTING.md`.

By submitting a Pull Request, you agree to the contribution terms in [LICENSE.md](LICENSE.md). A GitHub fork may be used to prepare a contribution, but the license does not permit using EarthWorks code in another project without written permission from Ostrix.
