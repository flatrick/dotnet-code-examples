# Continuous integration

One workflow, `.github/workflows/corpus-tests.yml`, running one job, `corpus-tests`, covering the
corpus build matrix and the project-namespace guard.

**Nothing here currently executes.** Actions is disabled for this repository, so no trigger below
fires and verification is whatever is run locally. Minutes cost money on a private repository, and
while this is a personal project that cost buys nothing a local run does not. The configuration is
kept current anyway, so enabling Actions is a settings change rather than a project.

## What runs, and when

```yaml
on:
  pull_request:
    branches: [main]
  push:
    branches: [main]
  workflow_dispatch:
```

Pushes to a feature branch do not trigger a run. Verification happens when a pull request targets
`main`, and `push` on `main` verifies the post-merge state.

## Version pinning

Every external reference is an exact coordinate. No floating major tags, no `-latest` runner label.

```yaml
runs-on: windows-2025-vs2026

- uses: actions/checkout@v7.0.1
- uses: actions/setup-dotnet@v6.0.0
- uses: actions/upload-artifact@v7.0.1
```

`windows-2025-vs2026` names both the operating system and the Visual Studio version. That second
half is what this repository actually depends on: the legacy net48 corpus builds through Visual
Studio's `MSBuild.exe`, and `scripts/verify-feature-floors.cs` locates compilers through `vswhere`
and through the in-box `%WINDIR%\Microsoft.NET\Framework64` compilers. `CorpusProjectBuildTests`
depends on it too and reports those eleven projects inconclusive without it.

`.github/dependabot.yml` supplies weekly patch adoption for the pinned actions, scoped to
`github-actions`. NuGet is deliberately excluded: corpus package versions are pinned to historical
releases as part of the corpus contract, so that ecosystem would only produce pull requests that
must be rejected.

## The merge gate is not active

Nothing gates anything: Actions is disabled, so no run is produced at all, and even with it
enabled, nothing blocks a merge on a result or blocks a direct push to `main`. Required status
checks need a ruleset or branch protection, and both refuse while a repository is private on the
GitHub Free plan. Activate the gate the same way as any repository once it goes public: confirm the
`corpus-tests` check has reported at least once, then create a ruleset requiring it.
