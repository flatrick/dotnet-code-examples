# Cross-Platform Corpus SDK Installer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the repository-private corpus SDK installer support Windows and Linux without using
a distro package manager or adding a tracked shell script.

**Architecture:** Keep `install-corpus-test-sdks.cs` as the single entry point and move only its
platform-specific values and argument construction into one `scripts/shared` C# type that the test
project also compiles. Exercise that type with unit tests and launch the real script in `--check`
mode as a network-free integration test before changing production behavior.

**Tech Stack:** .NET 10 file-based C#, MSTest 4.3.2, Microsoft's `dotnet-install.ps1` and
`dotnet-install.sh`.

## Global Constraints

- Keep the tool as one file-based C# program invoked with `dotnet`.
- Use only Microsoft-published `dotnet-install` scripts and SDK payloads.
- Install into `.artifacts/dotnet` by default and retain `--install-dir` and `--check` behavior.
- Do not modify the machine-wide .NET installation or the caller's `PATH`.
- Preserve all required SDKs side by side while retaining the newest private `dotnet` host.
- Keep Windows behavior supported and provide platform-appropriate commands and documentation.
- Reject operating systems other than Windows and Linux explicitly.
- Do not add `.sh`, `.ps1`, `.bat`, or `.py` files to the repository.

---

### Task 1: Platform-specific installer contract

**Files:**
- Create: `scripts/shared/CorpusSdkInstallerPlatform.cs`
- Create: `tests/DotNetCodeExamples.Corpus.Tests/Toolchains/CorpusSdkInstallerPlatformTests.cs`
- Modify: `tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj`

**Interfaces:**
- Produces: `CorpusSdkInstallerPlatform.ForOperatingSystem(bool isWindows, bool isLinux)`.
- Produces: `HostFileName`, `InstallerUri`, `InstallerExtension`, and `RunnerFileName` values.
- Produces: `BuildInstallerArguments(string installerPath, string version, string installDir,
  bool preserveExistingHost)` and `FormatTestCommand(string privateHost)`.
- Consumes: no repository-specific implementation beyond `System` collections and strings.

- [ ] **Step 1: Link the wished-for shared implementation into the test project**

Add this item next to the existing linked `CompileItems.cs` item:

```xml
<Compile Include="..\..\scripts\shared\CorpusSdkInstallerPlatform.cs"
         Link="Shared\CorpusSdkInstallerPlatform.cs" />
```

- [ ] **Step 2: Write failing platform contract tests**

Create table-driven MSTest coverage that hand-checks these Windows values:

```text
host: dotnet.exe
URI: https://dot.net/v1/dotnet-install.ps1
extension: .ps1
runner: powershell.exe
arguments: -NoProfile -ExecutionPolicy Bypass -File <file> -Version <version>
           -InstallDir <dir> -NoPath [-SkipNonVersionedFiles]
```

and these Linux values:

```text
host: dotnet
URI: https://dot.net/v1/dotnet-install.sh
extension: .sh
runner: bash
arguments: <file> --version <version> --install-dir <dir> --no-path
           [--skip-non-versioned-files]
```

Also assert that neither platform selected throws `PlatformNotSupportedException`, and that a
private-host path containing a single quote is shell-quoted correctly by `FormatTestCommand`.

- [ ] **Step 3: Run the focused tests and verify RED**

Run:

```bash
dotnet test tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj \
  --nologo --filter FullyQualifiedName~CorpusSdkInstallerPlatformTests
```

Expected: compilation fails because `CorpusSdkInstallerPlatform` does not exist.

- [ ] **Step 4: Implement the minimal shared platform contract**

Implement an immutable `internal sealed class CorpusSdkInstallerPlatform` with two static instances
selected by `ForOperatingSystem`. Return literal argument arrays in platform spelling and append the
skip-non-versioned-files switch only when `preserveExistingHost` is true. Quote PowerShell paths by
doubling single quotes and POSIX shell paths with the standard `'<text>'` plus `'"'"'"'`
escape for embedded single quotes.

- [ ] **Step 5: Run the focused tests and verify GREEN**

Run the command from Step 3. Expected: all `CorpusSdkInstallerPlatformTests` pass with zero build
warnings and errors.

- [ ] **Step 6: Commit the platform contract**

```bash
git add scripts/shared/CorpusSdkInstallerPlatform.cs \
  tests/DotNetCodeExamples.Corpus.Tests/Toolchains/CorpusSdkInstallerPlatformTests.cs \
  tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj
git commit -m "test: define cross-platform corpus SDK installer contract"
```

### Task 2: Real Linux check-mode behavior

**Files:**
- Create: `tests/DotNetCodeExamples.Corpus.Tests/Toolchains/InstallCorpusTestSdksScriptTests.cs`
- Modify: `scripts/install-corpus-test-sdks.cs`

**Interfaces:**
- Consumes: `CorpusSdkInstallerPlatform.ForOperatingSystem` and its properties/methods from Task 1.
- Produces: existing command-line interface with working Windows and Linux `--check` and install
  paths.

- [ ] **Step 1: Write a failing integration test against the real script**

Launch the current `dotnet` host with:

```text
scripts/install-corpus-test-sdks.cs -- --check --install-dir <unique nonexistent temp path>
```

On Windows and Linux assert exit code `1`, output naming the private SDK directory and each missing
version, error output `Missing 3 required private SDK version(s).`, absence of a Windows-only error,
and absence of the installation directory after the command. Mark other operating systems
inconclusive.

- [ ] **Step 2: Run the integration test and verify RED on Linux**

Run:

```bash
dotnet test tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj \
  --nologo --filter FullyQualifiedName~InstallCorpusTestSdksScriptTests
```

Expected on Linux: failure because the real script exits through its Windows-only guard before
inventory reporting.

- [ ] **Step 3: Implement platform selection and installer invocation**

Add `#:include shared/CorpusSdkInstallerPlatform.cs`. Replace the Windows-only guard and hard-coded
host/installer values with the platform contract. Download the selected installer extension, order
missing versions with `Version.Parse` descending, and invoke `BuildInstallerArguments`, passing
`File.Exists(privateHost)` so every older side-by-side install preserves the newest host. Retain
captured output, cleanup, exact post-install inventory verification, and usage behavior.

- [ ] **Step 4: Run the integration and platform tests and verify GREEN**

Run:

```bash
dotnet test tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj \
  --nologo --filter 'FullyQualifiedName~InstallCorpusTestSdksScriptTests|FullyQualifiedName~CorpusSdkInstallerPlatformTests'
```

Expected: all focused tests pass with zero build warnings and errors.

- [ ] **Step 5: Exercise Linux check mode directly**

Run:

```bash
dotnet scripts/install-corpus-test-sdks.cs -- \
  --check --install-dir /tmp/corpus-sdk-linux-check
```

Expected: exit `1`, normal private-directory and three-missing-SDK reporting, no download, and no
creation of `/tmp/corpus-sdk-linux-check`.

- [ ] **Step 6: Commit the real script behavior**

```bash
git add scripts/install-corpus-test-sdks.cs \
  tests/DotNetCodeExamples.Corpus.Tests/Toolchains/InstallCorpusTestSdksScriptTests.cs
git commit -m "feat: support Linux corpus SDK installation"
```

### Task 3: Cross-platform operator documentation and verification

**Files:**
- Modify: `scripts/install-corpus-test-sdks.md`

**Interfaces:**
- Consumes: the unchanged CLI and platform-specific host names from Task 2.
- Produces: PowerShell and Bash instructions that use only the repository-private installation.

- [ ] **Step 1: Update the operator documentation**

Keep the exact SDK list and explain that the tool downloads Microsoft's platform installer. Add
PowerShell and Bash blocks for installation, `--check`, custom installation roots, and invoking the
private host. State that no distro package manager or AUR package is used, native dependencies remain
the operator's responsibility, and the full corpus matrix remains Windows-only.

- [ ] **Step 2: Run repository verification**

Run:

```bash
dotnet test tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj --nologo
dotnet scripts/install-corpus-test-sdks.cs -- --check --install-dir /tmp/corpus-sdk-linux-check
git diff --check
git status --short
```

The direct check is expected to exit `1` because the temporary root is empty; its output must be the
normal missing-inventory result. Classify Windows-only corpus test failures separately from failures
introduced by this change.

- [ ] **Step 3: Review the final diff**

Confirm the diff contains no tracked shell script, package-manager instruction, PATH mutation,
third-party content, unrelated refactor, or accidental generated artifact.

- [ ] **Step 4: Commit documentation**

```bash
git add scripts/install-corpus-test-sdks.md
git commit -m "docs: document private SDK installation on Linux"
```
