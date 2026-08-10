# Cross-Platform Corpus SDK Installer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the repository-private corpus SDK installer support Windows and Linux without using
a distro package manager or adding a tracked shell script.

**Architecture:** Keep `install-corpus-test-sdks.cs` as one self-contained entry point so it can
bootstrap the 10.0.3xx SDK under a 10.0.1xx file-based-app host, which does not support `#:include`.
Launch the real script in `--check` mode as a network-free integration test before changing
production behavior.

**Tech Stack:** .NET 10 file-based C#, MSTest 4.3.2, Microsoft's `dotnet-install.ps1` and
`dotnet-install.sh`.

## Global Constraints

- Keep the tool as one file-based C# program invoked with `dotnet`.
- Use only Microsoft-published `dotnet-install` scripts and SDK payloads.
- Install into `.artifacts/dotnet` by default and retain `--install-dir`, `--check`, and `--dry-run`
  behavior.
- Do not modify the machine-wide .NET installation or the caller's `PATH`.
- Keep downloaded installer files and disposable verification roots under the repository root's
  ignored `.scratch/` directory.
- Preserve all required SDKs side by side while retaining the newest private `dotnet` host.
- Keep Windows behavior supported and provide platform-appropriate commands and documentation.
- Reject operating systems other than Windows and Linux explicitly.
- Do not add `.sh`, `.ps1`, `.bat`, or `.py` files to the repository.

---

### Task 1: Real Linux check-mode contract

**Files:**
- Create: `tests/DotNetCodeExamples.Corpus.Tests/Toolchains/InstallCorpusTestSdksScriptTests.cs`

**Interfaces:**
- Produces: a process-level regression test for the existing installer command line.
- Consumes: the current `dotnet` host and a unique nonexistent installation path under `.scratch/`.

- [ ] **Step 1: Write a failing integration test against the real script**

Launch the current `dotnet` host with:

```text
scripts/install-corpus-test-sdks.cs -- --check --install-dir <repo>/.scratch/<unique path>
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

### Task 2: Cross-platform installer behavior

**Files:**
- Modify: `scripts/install-corpus-test-sdks.cs`
- Modify: `tests/DotNetCodeExamples.Corpus.Tests/Toolchains/InstallCorpusTestSdksScriptTests.cs`

**Interfaces:**
- Consumes: the real-script integration test from Task 1.
- Produces: existing command-line interface with working Windows and Linux `--check` and install
  paths.
- Produces: a Linux Bash-installer smoke path that resolves payloads without downloading them.

- [ ] **Step 1: Implement platform selection and installer invocation**

Replace the Windows-only guard and hard-coded host/installer values with a self-contained platform
contract. Download the selected installer extension and order missing versions with `Version.Parse`
descending. Preserve non-versioned files only when the inventory already contains an SDK newer than
the one being installed, so a newer host is retained without preventing replacement of an older
host. Retain captured output, cleanup, exact post-install inventory verification, and usage behavior.

- [ ] **Step 2: Run the integration test and verify GREEN**

Run:

```bash
dotnet test tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj \
  --nologo --filter FullyQualifiedName~InstallCorpusTestSdksScriptTests
```

Expected: the focused test passes with zero build warnings and errors.

- [ ] **Step 3: Exercise Linux check mode directly**

Run:

```bash
dotnet scripts/install-corpus-test-sdks.cs -- \
  --check --install-dir "$PWD/.scratch/corpus-sdk-linux-check"
```

Expected: exit `1`, normal private-directory and three-missing-SDK reporting, no download, and no
creation of `.scratch/corpus-sdk-linux-check`.

- [ ] **Step 4: Exercise the Linux installer without downloading payloads**

Run the installer tests on Linux. The dry-run test supplies a temporary private host reporting SDK
5.0.408, downloads Microsoft's Bash installer, and requires successful newest-first requests for
10.0.302 and 7.0.410. It must leave neither SDK payloads nor an installer file behind.

- [ ] **Step 5: Commit the real script behavior**

```bash
git add scripts/install-corpus-test-sdks.cs \
  tests/DotNetCodeExamples.Corpus.Tests/Toolchains/InstallCorpusTestSdksScriptTests.cs
git commit -m "feat: support Linux corpus SDK installation"
```

### Task 3: Cross-platform operator documentation and verification

**Files:**
- Modify: `scripts/install-corpus-test-sdks.md`
- Modify: `.github/workflows/corpus-tests.yml`
- Modify: `docs/design/ci.md`

**Interfaces:**
- Consumes: the unchanged CLI and platform-specific host names from Task 2.
- Produces: PowerShell and Bash instructions that use only the repository-private installation.

- [ ] **Step 1: Update the operator documentation**

Keep the exact SDK list and explain that the tool downloads Microsoft's platform installer. Add
PowerShell and Bash blocks for installation, `--check`, custom installation roots, and invoking the
private host. State that no distro package manager or AUR package is used, native dependencies remain
the operator's responsibility, and the full corpus matrix remains Windows-only.

Add a pinned Ubuntu job that runs only `InstallCorpusTestSdksScriptTests`; it must not install the
private SDK payloads or run the Windows-only corpus matrix.

- [ ] **Step 2: Run repository verification**

Run:

```bash
dotnet test tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj --nologo
dotnet scripts/install-corpus-test-sdks.cs -- --check --install-dir "$PWD/.scratch/corpus-sdk-linux-check"
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

### Task 4: Copy-paste completion command

**Files:**
- Modify: `scripts/install-corpus-test-sdks.cs:144-147,317-325`
- Modify: `scripts/install-corpus-test-sdks.md:72-85`
- Test: `tests/DotNetCodeExamples.Corpus.Tests/Toolchains/InstallCorpusTestSdksScriptTests.cs`

**Interfaces:**
- Consumes: `CorpusSdkInstallerPlatform.FormatTestCommand(string privateHost, string testProject)`
  and the existing successful no-install completion path.
- Produces: one PowerShell or Bash command that sets `DOTNET_HOST_PATH` and invokes that same private
  host to run the corpus test project.

- [ ] **Step 1: Write the failing process-level regression test**

Add `CompletionPrintsCopyPasteTestCommandForCurrentPlatform`. On Linux, create an executable fake
private host under a uniquely named `.scratch/corpus-sdk-command-'quote'-<guid>` directory whose
`--list-sdks` output contains literal entries for `5.0.408`, `7.0.410`, and `10.0.302`. Run the real
installer script without `--check` or `--dry-run` and assert this exact output line, with the actual
path substituted and POSIX single-quote escaping applied by the literal expectation:

```text
Private test host: DOTNET_HOST_PATH='<escaped-host>' '<escaped-host>' test tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj --nologo
```

On Windows, use the already-installed default `.artifacts/dotnet/dotnet.exe` when its inventory
contains all three required SDKs; otherwise mark the test inconclusive. Assert this exact output:

```text
Private test host: $env:DOTNET_HOST_PATH = '<escaped-host>'; & $env:DOTNET_HOST_PATH test tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj --nologo
```

The production mutation this test catches is removing the environment assignment, invoking a host
other than the assigned path, or emitting shell quoting that cannot represent the selected path.

- [ ] **Step 2: Run the focused test and verify RED**

Run:

```bash
private_host="$PWD/.scratch/corpus-sdk-linux-e2e/dotnet"
DOTNET_HOST_PATH="$private_host" "$private_host" test \
  tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj \
  --nologo --filter FullyQualifiedName~CompletionPrintsCopyPasteTestCommandForCurrentPlatform
```

Expected on Linux: FAIL because the current output invokes the quoted host but omits the
`DOTNET_HOST_PATH=...` assignment.

- [ ] **Step 3: Implement the two shell command formats**

Change `FormatTestCommand` so PowerShell produces:

```csharp
return IsWindows
    ? $"$env:DOTNET_HOST_PATH = {quotedHost}; & $env:DOTNET_HOST_PATH test {testProject} --nologo"
    : $"DOTNET_HOST_PATH={quotedHost} {quotedHost} test {testProject} --nologo";
```

Retain the existing PowerShell single-quote escaping and POSIX single-quote escaping when computing
`quotedHost`.

- [ ] **Step 4: Update operator documentation**

Make the PowerShell and Bash examples in `scripts/install-corpus-test-sdks.md` exactly match the
single commands printed by successful completion. Keep them one-command copy-paste examples and do
not mutate `PATH`.

- [ ] **Step 5: Run focused verification and verify GREEN**

Run:

```bash
private_host="$PWD/.scratch/corpus-sdk-linux-e2e/dotnet"
DOTNET_HOST_PATH="$private_host" "$private_host" test \
  tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj \
  --nologo --filter FullyQualifiedName~InstallCorpusTestSdksScriptTests
git diff --check
```

Expected: all installer-script tests pass with zero build warnings and errors; `git diff --check`
prints nothing and exits `0`.

- [ ] **Step 6: Commit**

```bash
git add scripts/install-corpus-test-sdks.cs scripts/install-corpus-test-sdks.md \
  tests/DotNetCodeExamples.Corpus.Tests/Toolchains/InstallCorpusTestSdksScriptTests.cs \
  docs/superpowers/plans/2026-08-10-cross-platform-corpus-sdk-installer.md
git commit -m "fix: print copy-paste private test commands"
```
