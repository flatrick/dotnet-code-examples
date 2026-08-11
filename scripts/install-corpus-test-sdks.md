# Private corpus test SDKs

The corpus test matrix requires these exact SDK versions in a repository-private root:

- `5.0.408`
- `7.0.410`
- `10.0.302`

Install or verify them with:

```powershell
dotnet scripts/install-corpus-test-sdks.cs
```

```bash
dotnet scripts/install-corpus-test-sdks.cs
```

The command installs only versions that are absent from `.artifacts/dotnet`. It never changes the
machine-wide .NET installation or `PATH`. The private root supplies the exact compiler and runtime
bands that the matrix needs while allowing the machine's normal .NET setup to remain untouched.
On Windows the tool downloads Microsoft's `dotnet-install.ps1`; on Linux it downloads Microsoft's
`dotnet-install.sh` and runs it with Bash. The Linux path does not use AUR, another distro package
manager, or a tracked shell script. The downloaded installer is transient: it is written under the
repository's ignored `.scratch/` directory and deleted after the install attempt.

The Microsoft installer does not install a Linux distribution's native dependencies. Those remain
the operator's responsibility, particularly when running the end-of-life .NET 5 and .NET 7 SDKs on
a current distribution. Installing and checking the private SDK root is cross-platform; the full
corpus matrix remains Windows-only because its legacy projects need Visual Studio's `MSBuild.exe`
and its net48 runtime cases need .NET Framework.

`TargetFramework=net5.0` selects the target runtime and reference APIs; it does not select the
SDK 5 compiler. The `dotnet` host selects the SDK, so tests that exercise compiler boundaries must
run through the private host containing the exact SDK versions above.

Check a root without downloading or modifying anything:

```powershell
dotnet scripts/install-corpus-test-sdks.cs -- --check
```

```bash
dotnet scripts/install-corpus-test-sdks.cs -- --check
```

Exercise platform detection, the Microsoft installer download, SDK ordering, and installer
arguments without downloading or installing SDK payloads:

```powershell
dotnet scripts/install-corpus-test-sdks.cs -- --dry-run
```

```bash
dotnet scripts/install-corpus-test-sdks.cs -- --dry-run
```

Dry run still uses the network to download Microsoft's small platform installer into `.scratch/`.
The installer resolves the requested payload URLs but does not download them, and the C# tool
deletes the installer afterward.

Use another absolute or relative private directory when needed:

```powershell
dotnet scripts/install-corpus-test-sdks.cs -- --install-dir C:\build\corpus-dotnet
```

```bash
dotnet scripts/install-corpus-test-sdks.cs -- --install-dir "$PWD/.artifacts/corpus-dotnet"
```

To invoke the test project through the private host, set `DOTNET_HOST_PATH` and use that host:

```powershell
$env:DOTNET_HOST_PATH = '.artifacts\dotnet\dotnet.exe'; & $env:DOTNET_HOST_PATH test tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj --nologo
```

On Linux:

```bash
DOTNET_HOST_PATH='.artifacts/dotnet/dotnet' '.artifacts/dotnet/dotnet' test tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj --nologo
```

Remove the private installation by deleting its directory, for example
`.artifacts/dotnet`; the next normal install command recreates it. SDK updates are deliberate,
reviewed changes: update both the exact versions in `install-corpus-test-sdks.cs` and the matrix
expectations together.
