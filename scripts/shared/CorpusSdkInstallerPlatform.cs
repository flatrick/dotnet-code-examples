#nullable enable

namespace DotNetCodeExamples.CorpusTooling;

// Platform-specific values for scripts/install-corpus-test-sdks.cs. The script names this file
// with `#:include shared/CorpusSdkInstallerPlatform.cs` and the corpus test project compiles the
// same file through a linked Compile item so its Windows and Linux process contracts cannot drift.
internal sealed class CorpusSdkInstallerPlatform
{
    private static readonly CorpusSdkInstallerPlatform Windows = new(
        isWindows: true,
        hostFileName: "dotnet.exe",
        installerUri: "https://dot.net/v1/dotnet-install.ps1",
        installerExtension: ".ps1",
        runnerFileName: "powershell.exe");

    private static readonly CorpusSdkInstallerPlatform Linux = new(
        isWindows: false,
        hostFileName: "dotnet",
        installerUri: "https://dot.net/v1/dotnet-install.sh",
        installerExtension: ".sh",
        runnerFileName: "bash");

    private CorpusSdkInstallerPlatform(
        bool isWindows,
        string hostFileName,
        string installerUri,
        string installerExtension,
        string runnerFileName)
    {
        IsWindows = isWindows;
        HostFileName = hostFileName;
        InstallerUri = installerUri;
        InstallerExtension = installerExtension;
        RunnerFileName = runnerFileName;
    }

    private bool IsWindows { get; }

    internal string HostFileName { get; }

    internal string InstallerUri { get; }

    internal string InstallerExtension { get; }

    internal string RunnerFileName { get; }

    internal static CorpusSdkInstallerPlatform ForOperatingSystem(bool isWindows, bool isLinux)
    {
        if (isWindows)
            return Windows;
        if (isLinux)
            return Linux;

        throw new PlatformNotSupportedException(
            "install-corpus-test-sdks.cs supports Windows and Linux only; the full corpus matrix requires Windows.");
    }

    internal string[] BuildInstallerArguments(
        string installerPath,
        string version,
        string installDir,
        bool preserveExistingHost)
    {
        var arguments = IsWindows
            ? new List<string>
            {
                "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", installerPath,
                "-Version", version, "-InstallDir", installDir, "-NoPath"
            }
            : new List<string>
            {
                installerPath, "--version", version, "--install-dir", installDir, "--no-path"
            };

        if (preserveExistingHost)
            arguments.Add(IsWindows ? "-SkipNonVersionedFiles" : "--skip-non-versioned-files");

        return arguments.ToArray();
    }

    internal string FormatTestCommand(string privateHost, string testProject)
    {
        var quotedHost = IsWindows
            ? $"'{privateHost.Replace("'", "''", StringComparison.Ordinal)}'"
            : $"'{privateHost.Replace("'", "'\"'\"'", StringComparison.Ordinal)}'";
        var prefix = IsWindows ? "& " : string.Empty;
        return $"{prefix}{quotedHost} test {testProject} --nologo";
    }
}
