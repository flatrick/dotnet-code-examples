# Cross-Platform Corpus SDK Installer Design

## Goal

Allow `scripts/install-corpus-test-sdks.cs` to install and verify the exact repository-private SDK
set on Windows and Linux without using a system package manager, changing `PATH`, or adding a
tracked shell script. This does not make the Windows-only corpus matrix runnable on Linux.

## Constraints

- Keep the tool as one file-based C# program invoked with `dotnet`.
- Use only Microsoft-published `dotnet-install` scripts and SDK payloads.
- Install into `.artifacts/dotnet` by default and retain `--install-dir` and `--check` behavior.
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
condition.

Normal installation downloads the platform installer to a uniquely named file under the repository
root's ignored `.scratch/` directory. Missing SDKs are installed from newest to oldest. An
installation uses the installer's
skip-non-versioned-files option when the inventory already contains a newer SDK; this preserves a
newer host without preventing a newly installed SDK from replacing an older host. The temporary
installer is deleted in a `finally` block.

After installation, the program asks the private host for its SDK inventory again and fails if any
required exact version is absent. Its success output prints a command appropriate to the current
shell family. Network, installer, or process failures retain captured standard output and error.

Other operating systems fail before filesystem or network mutation with an explicit supported-OS
message.

## Testing

An integration test launches the real file-based program in `--check` mode against an empty
temporary directory. On Linux this test fails against the Windows-only guard and passes only when
the script reaches normal inventory reporting without downloading anything. The same test protects
the existing Windows check path.

Focused verification runs that test and exercises the program directly on Linux. Full verification
builds and runs the repository test project to the extent supported by the host; Windows-only
toolchain tests remain governed by their existing matrix contract. Documentation examples cover
both PowerShell and Bash without recommending AUR or another distro package manager.

## Non-Goals

- Installing native dependencies required by a particular Linux distribution.
- Supporting macOS, FreeBSD, or other operating systems.
- Running Visual Studio MSBuild, .NET Framework builds, or net48 runtime cases on Linux.
- Replacing the repository-private SDK set with system packages.
