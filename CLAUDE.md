# CLAUDE.md

This file provides guidance to Claude Code when working in this repository.

Read [`AGENTS.md`](AGENTS.md) first; it carries the corpus rules and the reasoning behind them.
This file covers commands.

## Commands

```bash
dotnet scripts/verify-feature-floors.cs                          # classify every CSharp_v* group folder
dotnet scripts/verify-feature-floors.cs -- --project CSharp_v7.0 # one project
dotnet scripts/verify-feature-floors.cs -- --language vb         # the VB ladder, both families
dotnet scripts/verify-feature-floors.cs -- --language vb --project dotnet/Net10/15.5/library
dotnet scripts/verify-feature-floors.cs -- --json                # machine-readable
dotnet scripts/verify-feature-floors.cs -- --offline             # skip the NuGet download
dotnet scripts/verify-project-namespaces.cs                      # namespace vs. RootNamespace drift
dotnet scripts/verify-project-namespaces.cs -- --json            # machine-readable
dotnet scripts/csharp-placement-sweep.cs -- --out .scratch/sweep.json   # ~3 min; needs VS MSBuild
dotnet scripts/render-floor-report.cs -- --sweep .scratch/sweep.json    # readable report
dotnet scripts/install-corpus-test-sdks.cs                       # install/verify the private SDK host
```

`dotnet <file>.cs` silently claims some flags for itself, which is why every script takes its
arguments after `--`.

### Building corpus projects

SDK-style projects (`CSharp/dotnet/10/latest/*`, the VB projects, and three of the fourteen net48 C#
projects — `CSharp_v1.0-Unsafe`, `CSharp_v8.0`, `CSharp_v8.0-Unsafe`) build with `dotnet build`. The
other eleven net48 C# projects are legacy non-SDK XML and need Visual Studio's `MSBuild.exe` on
Windows:

```bash
vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe"
MSBuild.exe examples/language-features/CSharp/dotNetFramework/v4.8/CSharp_v7.0/CSharp70.csproj -t:Restore;Build
```

The corpus test suite is `tests/DotNetCodeExamples.Corpus.Tests/`. Its exact SDK versions are installed
or verified with `dotnet scripts/install-corpus-test-sdks.cs`; run the suite through the private
host documented in [`scripts/install-corpus-test-sdks.md`](scripts/install-corpus-test-sdks.md).

A plain `dotnet test` against `Corpus.slnx` is wrong — the machine host does not carry SDK 5.0.408
or 7.0.410, so the compiler-boundary cases skip and the toolchain preflight fails.

## Conventions

1. **Tooling is single-file C#, never a shell script.** No `.sh`, `.ps1`, `.bat`, or `.py`.
   `scripts/Directory.Build.props` resets the production analyzer settings for these.
   [`scripts/shared/`](scripts/shared/) is the one exception, for code a test must also compile —
   today just `CompileItems.cs`, the VB `Compile` glob resolver `verify-feature-floors.cs`
   `#:include`s and `DotNetCodeExamples.Corpus.Tests.csproj` links.
2. **State current truth only.** `git log` is the changelog.
3. **American English.**
4. **LF line endings, UTF-8**, enforced by `.gitattributes`.
5. **Never add third-party content.** See [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).
