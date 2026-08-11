#!/usr/bin/env dotnet
#:property PublishAot=false

// Installs the exact SDK bands used by the corpus test matrix into a repository-private directory.
// The private host is intentionally never added to PATH and does not alter any machine installation.

using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;

#nullable enable

var requiredVersions = new[] { "5.0.408", "7.0.410", "10.0.302" };
var installDir = "";
var checkOnly = false;
var dryRun = false;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--check":
            checkOnly = true;
            break;
        case "--dry-run":
            dryRun = true;
            break;
        case "--install-dir":
            if (installDir.Length != 0 || ++i >= args.Length || args[i].Length == 0
                || args[i].StartsWith("--", StringComparison.Ordinal))
                return Usage("--install-dir requires one path and can be specified only once.");

            installDir = args[i];
            break;
        default:
            return Usage($"Unknown option: {args[i]}");
    }
}

if (checkOnly && dryRun)
    return Usage("--check and --dry-run cannot be specified together.");

CorpusSdkInstallerPlatform platform;
try
{
    platform = CorpusSdkInstallerPlatform.ForOperatingSystem(
        OperatingSystem.IsWindows(),
        OperatingSystem.IsLinux());
}
catch (PlatformNotSupportedException exception)
{
    Console.Error.WriteLine(exception.Message);
    return 1;
}

var repoRoot = FindRepoRoot(Environment.CurrentDirectory)
    ?? throw new InvalidOperationException("Could not locate the repository root (no .git entry found).");
var resolvedInstallDir = Path.GetFullPath(installDir.Length == 0
    ? Path.Combine(repoRoot, ".artifacts", "dotnet")
    : installDir);
var privateHost = Path.Combine(resolvedInstallDir, platform.HostFileName);

Console.WriteLine($"Private SDK directory: {resolvedInstallDir}");

var presentVersions = ListInstalledSdkVersions(privateHost);
var missingVersions = requiredVersions.Where(version => !presentVersions.Contains(version)).ToArray();

foreach (var version in requiredVersions)
    Console.WriteLine(presentVersions.Contains(version) ? $"Found SDK: {version}" : $"Missing SDK: {version}");

if (checkOnly)
{
    if (missingVersions.Length == 0)
    {
        Console.WriteLine("All required private SDKs are present.");
        return 0;
    }

    Console.Error.WriteLine($"Missing {missingVersions.Length} required private SDK version(s).");
    return 1;
}

if (missingVersions.Length != 0)
{
    if (!dryRun)
        Directory.CreateDirectory(resolvedInstallDir);
    var scratchDirectory = Path.Combine(repoRoot, ".scratch");
    Directory.CreateDirectory(scratchDirectory);
    var installerPath = Path.Combine(
        scratchDirectory,
        $"dotnet-install-{Guid.NewGuid():N}{platform.InstallerExtension}");

    try
    {
        Console.WriteLine("Downloading the Microsoft dotnet-install script to the repository scratch directory.");
        using var client = new HttpClient();
        await File.WriteAllBytesAsync(installerPath,
            await client.GetByteArrayAsync(platform.InstallerUri));

        foreach (var version in missingVersions.OrderByDescending(Version.Parse))
        {
            Console.WriteLine($"Installing SDK {version} into the private directory.");
            var installerArguments = platform.BuildInstallerArguments(
                installerPath,
                version,
                resolvedInstallDir,
                CorpusSdkInstallerPlatform.ShouldPreserveExistingHost(version, presentVersions),
                dryRun);

            if (dryRun)
            {
                var switches = installerArguments.Where(
                    argument => argument.StartsWith("-", StringComparison.Ordinal));
                Console.WriteLine($"Dry-run switches for SDK {version}: {string.Join(" ", switches)}");
            }

            var result = await RunProcess(
                platform.RunnerFileName,
                installerArguments);
            ReportProcessOutput($"Installer output for SDK {version}", result);
            RequireSuccess($"Installer for SDK {version}", result);
            presentVersions.Add(version);
        }
    }
    finally
    {
        if (File.Exists(installerPath))
            File.Delete(installerPath);
    }
}

if (dryRun)
{
    Console.WriteLine("Dry run complete; no SDK payloads were installed.");
    return 0;
}

presentVersions = ListInstalledSdkVersions(privateHost);
missingVersions = requiredVersions.Where(version => !presentVersions.Contains(version)).ToArray();
if (missingVersions.Length != 0)
    throw new InvalidOperationException("The private installation completed without all required SDKs: "
        + string.Join(", ", missingVersions) + ".");

Console.WriteLine("All required private SDKs are present.");
Console.WriteLine("Private test host: " + platform.FormatTestCommand(
    privateHost,
    "tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj"));
return 0;

static int Usage(string error)
{
    Console.Error.WriteLine(error);
    Console.Error.WriteLine(
        "Usage: dotnet scripts/install-corpus-test-sdks.cs -- [--check | --dry-run] [--install-dir <path>]");
    return 2;
}

static HashSet<string> ListInstalledSdkVersions(string privateHost)
{
    if (!File.Exists(privateHost))
        return new HashSet<string>(StringComparer.Ordinal);

    var result = RunProcess(privateHost, "--list-sdks").GetAwaiter().GetResult();
    ReportProcessOutput("Private dotnet --list-sdks output", result);
    RequireSuccess("Private dotnet --list-sdks", result);

    var versions = new HashSet<string>(StringComparer.Ordinal);
    foreach (var line in result.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
    {
        var match = Regex.Match(line, @"^(?<version>\d+\.\d+\.\d+)\s+\[");
        if (match.Success)
            versions.Add(match.Groups["version"].Value);
    }

    return versions;
}

static async Task<ProcessResult> RunProcess(string fileName, params string[] arguments)
{
    var startInfo = new ProcessStartInfo(fileName)
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    foreach (var argument in arguments)
        startInfo.ArgumentList.Add(argument);

    using var process = Process.Start(startInfo)
        ?? throw new InvalidOperationException($"Could not start '{fileName}'.");
    var standardOutput = process.StandardOutput.ReadToEndAsync();
    var standardError = process.StandardError.ReadToEndAsync();
    await process.WaitForExitAsync();
    await Task.WhenAll(standardOutput, standardError);
    return new ProcessResult(process.ExitCode, standardOutput.Result, standardError.Result);
}

static void ReportProcessOutput(string heading, ProcessResult result)
{
    if (result.StandardOutput.Length != 0)
        Console.WriteLine($"{heading} (stdout):{Environment.NewLine}{result.StandardOutput.TrimEnd()}");
    if (result.StandardError.Length != 0)
        Console.Error.WriteLine($"{heading} (stderr):{Environment.NewLine}{result.StandardError.TrimEnd()}");
}

static void RequireSuccess(string operation, ProcessResult result)
{
    if (result.ExitCode == 0)
        return;

    throw new InvalidOperationException($"{operation} failed with exit code {result.ExitCode}. "
        + "Its captured stdout and stderr were reported above.");
}

static string? FindRepoRoot(string start)
{
    var directory = new DirectoryInfo(start);
    while (directory is not null)
    {
        var gitEntry = Path.Combine(directory.FullName, ".git");
        if (Directory.Exists(gitEntry) || File.Exists(gitEntry))
            return directory.FullName;
        directory = directory.Parent;
    }

    return null;
}

internal readonly record struct ProcessResult(int ExitCode, string StandardOutput, string StandardError);

// This bootstrap script must stay self-contained: a .NET 10.0.1xx host can run a file-based C#
// program but does not recognize #:include, and installing the required 10.0.3xx host is this
// program's job.
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

    internal static bool ShouldPreserveExistingHost(
        string version,
        IEnumerable<string> presentVersions)
    {
        var installingVersion = Version.Parse(version);
        return presentVersions.Any(presentVersion => Version.Parse(presentVersion) > installingVersion);
    }

    internal string[] BuildInstallerArguments(
        string installerPath,
        string version,
        string installDir,
        bool preserveExistingHost,
        bool dryRun)
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
        if (dryRun)
            arguments.Add(IsWindows ? "-DryRun" : "--dry-run");

        return arguments.ToArray();
    }

    internal string FormatTestCommand(string privateHost, string testProject)
    {
        var quotedHost = IsWindows
            ? $"'{privateHost.Replace("'", "''", StringComparison.Ordinal)}'"
            : $"'{privateHost.Replace("'", "'\"'\"'", StringComparison.Ordinal)}'";
        return IsWindows
            ? $"$env:DOTNET_HOST_PATH = {quotedHost}; & $env:DOTNET_HOST_PATH test {testProject} --nologo"
            : $"DOTNET_HOST_PATH={quotedHost} {quotedHost} test {testProject} --nologo";
    }
}
