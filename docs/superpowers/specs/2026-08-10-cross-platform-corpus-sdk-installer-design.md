# Cross-Platform Corpus SDK Installer Design

## Goal

Allow `scripts/install-corpus-test-sdks.cs` to install and verify the exact repository-private SDK
set on Windows and Linux without using a system package manager, changing `PATH`, or adding a
tracked shell script. This does not make the Windows-only corpus matrix runnable on Linux.

## Constraints

- Keep the tool as one file-based C# program invoked with `dotnet`.
- Use only Microsoft-published `dotnet-install` scripts and SDK payloads.
- Install into `.artifacts/dotnet` by default and retain `--install-dir`, `--check`, and `--dry-run`
  behavior.
- Do not modify the machine-wide .NET installation or the caller's `PATH`.
- Keep downloaded installer files under the repository root's ignored `.scratch/` directory.
- Preserve all required SDKs side by side while retaining the newest private `dotnet` host.
- Keep Windows behavior supported and provide platform-appropriate commands and documentation.
- Reject unsupported operating systems explicitly.

## Approaches Considered

### Select Microsoft's installer by platform

The C# program selects `dotnet-install.ps1` plus `powershell.exe` on Windows and
`dotnet-install.sh` plus `bash` on Linux. It also selects `dotnet.exe` or `dotnet` as the private
host. This delegates architecture, glibc-versus-musl detection, download selection, and archive
extraction to Microsoft's maintained installers while retaining one repository-native entry point.
The platform contract remains in that entry point because this bootstrap tool must run under a .NET
10.0.1xx host, whose file-based-app implementation does not recognize `#:include`.

This is the selected approach because it is the smallest design that satisfies the repository's
tooling convention and avoids distro package managers.

### Download and extract SDK archives directly

The C# program could consume release metadata, select an OS, architecture, and libc-specific
archive, validate it, and extract it. This avoids requiring Bash but duplicates installer logic,
expands the security-sensitive download surface, and creates more platform cases for the repository
to maintain.

### Maintain separate Windows and Linux tools

A PowerShell-oriented C# program and a Linux shell tool would be straightforward independently, but
a tracked shell script violates the repository convention and two entry points could drift.

## Behavior

Argument validation remains platform-independent. Windows and Linux then resolve these values:

| Value | Windows | Linux |
|---|---|---|
| Private host | `dotnet.exe` | `dotnet` |
| Installer | `dotnet-install.ps1` | `dotnet-install.sh` |
| Runner | `powershell.exe` | `bash` |
| Installer switches | PowerShell spelling | POSIX spelling |

`--check` only inspects the selected private host with `--list-sdks`; it never downloads or writes
to the installation directory. A missing host is an empty installation rather than an exceptional
condition. `--dry-run` downloads the platform installer into `.scratch/` and executes its dry-run
mode for missing SDKs without downloading payloads or modifying the installation root.

Normal installation downloads the platform installer to a uniquely named file under the repository
root's ignored `.scratch/` directory. Missing SDKs are installed from newest to oldest. An
installation uses the installer's
skip-non-versioned-files option when the inventory already contains a newer SDK; this preserves a
newer host without preventing a newly installed SDK from replacing an older host. The temporary
installer is deleted in a `finally` block.

After installation, the program asks the private host for its SDK inventory again and fails if any
required exact version is absent. Its success output prints one self-contained command appropriate
to the current shell family. The command sets `DOTNET_HOST_PATH` to the private host and invokes
that same host, so it can be copied and pasted without any preceding shell setup. PowerShell uses a
semicolon-separated environment assignment and invocation; Bash uses an environment assignment
scoped to the invocation. Paths are quoted and escaped for the selected shell. Network, installer,
or process failures retain captured standard output and error.

Other operating systems fail before filesystem or network mutation with an explicit supported-OS
message.

## Testing

An integration test launches the real file-based program in `--check` mode against an empty
temporary directory. Its assertions require normal inventory reporting without downloading or
creating the installation root. The same test protects the Windows and Linux check paths.

A Linux-only integration test supplies a temporary private host reporting SDK 5.0.408, then runs
the real program in `--dry-run` mode. Microsoft's Bash installer must accept both missing SDK
requests in newest-first order, including the host-preservation switch on the older request. The
test asserts that no SDK payload or installer file remains. CI runs only these installer tests on
Ubuntu; it does not attempt the Windows-only corpus matrix.

Focused verification runs that test and exercises the program directly on Linux. Full verification
builds and runs the repository test project to the extent supported by the host; Windows-only
toolchain tests remain governed by their existing matrix contract. Documentation examples cover
both PowerShell and Bash without recommending AUR or another distro package manager. Tests assert
the literal PowerShell and Bash command formats independently, including `DOTNET_HOST_PATH`, shell
quoting, and invocation of the private host.

## Non-Goals

- Installing native dependencies required by a particular Linux distribution.
- Supporting macOS, FreeBSD, or other operating systems.
- Running Visual Studio MSBuild, .NET Framework builds, or net48 runtime cases on Linux.
- Replacing the repository-private SDK set with system packages.
