using System.Diagnostics;

namespace DotNetCodeExamples.Corpus.Tests.Toolchains;

[TestClass]
[TestCategory("Integration")]
public sealed class InstallCorpusTestSdksScriptTests
{
    [TestMethod]
    [Timeout(60_000)]
    public async Task CheckModeInspectsAnEmptyPrivateDirectoryOnTheCurrentPlatform()
    {
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
        {
            Assert.Inconclusive("The corpus SDK installer supports Windows and Linux only.");
            return;
        }

        var repositoryRoot = RepositoryRoot();
        var installDirectory = Path.Combine(
            repositoryRoot,
            ".scratch",
            $"corpus-sdk-empty-{Guid.NewGuid():N}");

        try
        {
            var startInfo = new ProcessStartInfo(CurrentDotnetHost())
            {
                WorkingDirectory = repositoryRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("scripts/install-corpus-test-sdks.cs");
            startInfo.ArgumentList.Add("--");
            startInfo.ArgumentList.Add("--check");
            startInfo.ArgumentList.Add("--install-dir");
            startInfo.ArgumentList.Add(installDirectory);

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Could not start the dotnet host.");
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            await Task.WhenAll(standardOutput, standardError);

            Assert.AreEqual(1, process.ExitCode);
            StringAssert.Contains(standardOutput.Result, $"Private SDK directory: {installDirectory}");
            StringAssert.Contains(standardOutput.Result, "Missing SDK: 5.0.408");
            StringAssert.Contains(standardOutput.Result, "Missing SDK: 7.0.410");
            StringAssert.Contains(standardOutput.Result, "Missing SDK: 10.0.302");
            StringAssert.Contains(standardError.Result, "Missing 3 required private SDK version(s).");
            Assert.IsFalse(
                Directory.Exists(installDirectory),
                "Check mode must not create the private SDK directory.");
        }
        finally
        {
            if (Directory.Exists(installDirectory))
                Directory.Delete(installDirectory, recursive: true);
        }
    }

    [TestMethod]
    [Timeout(120_000)]
    public async Task DryRunExercisesTheLinuxInstallerWithoutInstallingSdkPayloads()
    {
        if (!OperatingSystem.IsLinux())
        {
            Assert.Inconclusive("The Bash installer smoke test runs on Linux only.");
            return;
        }

        var repositoryRoot = RepositoryRoot();
        var scratchDirectory = Path.Combine(repositoryRoot, ".scratch");
        var installDirectory = Path.Combine(
            scratchDirectory,
            $"corpus-sdk-dry-run-{Guid.NewGuid():N}");
        Directory.CreateDirectory(installDirectory);

        try
        {
            var privateHost = Path.Combine(installDirectory, "dotnet");
            await File.WriteAllTextAsync(
                privateHost,
                """
                #!/usr/bin/env bash
                if [ "$1" = "--list-sdks" ]; then
                    printf '5.0.408 [%s/sdk]\n' "$(dirname "$0")"
                    exit 0
                fi
                exit 64
                """);
            File.SetUnixFileMode(
                privateHost,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

            var startInfo = new ProcessStartInfo(CurrentDotnetHost())
            {
                WorkingDirectory = repositoryRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.ArgumentList.Add("scripts/install-corpus-test-sdks.cs");
            startInfo.ArgumentList.Add("--");
            startInfo.ArgumentList.Add("--dry-run");
            startInfo.ArgumentList.Add("--install-dir");
            startInfo.ArgumentList.Add(installDirectory);

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Could not start the dotnet host.");
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            await Task.WhenAll(standardOutput, standardError);

            Assert.AreEqual(0, process.ExitCode, standardError.Result);
            var sdk10 = standardOutput.Result.IndexOf("Installing SDK 10.0.302", StringComparison.Ordinal);
            var sdk7 = standardOutput.Result.IndexOf("Installing SDK 7.0.410", StringComparison.Ordinal);
            Assert.IsTrue(sdk10 >= 0 && sdk7 > sdk10, "Missing SDKs must be processed newest first.");
            StringAssert.Contains(
                standardOutput.Result,
                "Dry-run switches for SDK 10.0.302: --version --install-dir --no-path --dry-run");
            StringAssert.Contains(
                standardOutput.Result,
                "Dry-run switches for SDK 7.0.410: --version --install-dir --no-path "
                + "--skip-non-versioned-files --dry-run");
            StringAssert.Contains(standardOutput.Result, "Dry run complete; no SDK payloads were installed.");
            Assert.IsFalse(Directory.Exists(Path.Combine(installDirectory, "sdk")));
            Assert.HasCount(
                0,
                Directory.GetFiles(scratchDirectory, "dotnet-install-*", SearchOption.TopDirectoryOnly),
                "The downloaded installer must be removed after the dry run.");
        }
        finally
        {
            Directory.Delete(installDirectory, recursive: true);
        }
    }

    private static string CurrentDotnetHost()
    {
        var host = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");
        return string.IsNullOrWhiteSpace(host) ? "dotnet" : host;
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null
            && !Directory.Exists(Path.Combine(directory.FullName, ".git"))
            && !File.Exists(Path.Combine(directory.FullName, ".git")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}
