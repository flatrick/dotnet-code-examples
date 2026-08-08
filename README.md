# dotnet-code-examples

A hand-authored corpus of worked C# and VB.NET language-feature examples, one example per feature
per language version, across several TFM and project-format combinations — modern SDK-style
`net10.0`, SDK-style `net48`, legacy non-SDK `net48`, and dedicated `/unsafe` projects, every
project building at 0 errors and 0 warnings.

Browse it in [`examples/language-features/`](examples/language-features/);
[`MANIFEST.md`](examples/language-features/MANIFEST.md) is the index and says exactly which
features are absent from which project and why.

## For readers

The corpus is plain source files. C# is organized `<project>/<version>/<feature>/`, one source tree
per project. VB.NET keeps one shared source tree per family — `<family>/src/<version>/<feature>/` —
and a project per pinned language version selects rows from it; the pin directories themselves
(`latest/library/`, `11/my/`, …) hold only the project file. Nothing needs to be built or installed
to read it. Each example is commented with what the feature does, and — where it matters — what it
does *not* do; several comments record hazards found by running the code rather than by compiling
it.

## For agents

Start at [`AGENTS.md`](AGENTS.md).

## Verifying the corpus

The complete test suite requires exact SDK versions 5.0.408, 7.0.410, and 10.0.302, plus runtime
bands 5.0, 7.0, and 10.0. Its preflight fails if any requirement is absent. Use
`dotnet scripts/install-corpus-test-sdks.cs` to install or check the reusable private toolchains,
then run the full suite through the private host per
[`scripts/install-corpus-test-sdks.md`](scripts/install-corpus-test-sdks.md).

The unit-only test subset needs Windows and SDK 10 only; its process tests invoke `cmd`:

```powershell
dotnet test tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj --filter "TestCategory=Unit"
```

## Status

The corpus is complete.

## License

MIT — see [`LICENSE`](LICENSE).

Everything in this repository is authored here. [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md)
sets out why the example corpus is original work rather than derived from Microsoft's documentation.

## Who this is for

Anyone who wants worked, version-pinned language-feature examples, or a realistic multi-project,
multi-TFM target for testing Roslyn analyzers and other .NET tooling. This repository has no
dependency on any MCP server or tooling.
