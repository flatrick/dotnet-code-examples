using DotNetCodeExamples.CorpusTooling;

namespace DotNetCodeExamples.Corpus.Tests.Toolchains;

[TestClass]
[TestCategory("Unit")]
public sealed class CorpusSdkInstallerPlatformTests
{
    private const string TestProject =
        "tests/DotNetCodeExamples.Corpus.Tests/DotNetCodeExamples.Corpus.Tests.csproj";

    [TestMethod]
    public void WindowsUsesThePowerShellInstallerContract()
    {
        var platform = CorpusSdkInstallerPlatform.ForOperatingSystem(isWindows: true, isLinux: false);

        Assert.AreEqual("dotnet.exe", platform.HostFileName);
        Assert.AreEqual("https://dot.net/v1/dotnet-install.ps1", platform.InstallerUri);
        Assert.AreEqual(".ps1", platform.InstallerExtension);
        Assert.AreEqual("powershell.exe", platform.RunnerFileName);
        CollectionAssert.AreEqual(
            new[]
            {
                "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", @"C:\temp\dotnet-install.ps1",
                "-Version", "7.0.410", "-InstallDir", @"C:\private sdk", "-NoPath"
            },
            platform.BuildInstallerArguments(
                @"C:\temp\dotnet-install.ps1", "7.0.410", @"C:\private sdk", preserveExistingHost: false));
    }

    [TestMethod]
    public void WindowsPreservesAnExistingHostWhenAddingAnSdk()
    {
        var platform = CorpusSdkInstallerPlatform.ForOperatingSystem(isWindows: true, isLinux: false);

        var arguments = platform.BuildInstallerArguments(
            @"C:\temp\dotnet-install.ps1", "5.0.408", @"C:\private sdk", preserveExistingHost: true);

        Assert.AreEqual("-SkipNonVersionedFiles", arguments[^1]);
    }

    [TestMethod]
    public void LinuxUsesTheBashInstallerContract()
    {
        var platform = CorpusSdkInstallerPlatform.ForOperatingSystem(isWindows: false, isLinux: true);

        Assert.AreEqual("dotnet", platform.HostFileName);
        Assert.AreEqual("https://dot.net/v1/dotnet-install.sh", platform.InstallerUri);
        Assert.AreEqual(".sh", platform.InstallerExtension);
        Assert.AreEqual("bash", platform.RunnerFileName);
        CollectionAssert.AreEqual(
            new[]
            {
                "/tmp/dotnet-install.sh", "--version", "7.0.410", "--install-dir", "/tmp/private sdk", "--no-path"
            },
            platform.BuildInstallerArguments(
                "/tmp/dotnet-install.sh", "7.0.410", "/tmp/private sdk", preserveExistingHost: false));
    }

    [TestMethod]
    public void LinuxPreservesAnExistingHostWhenAddingAnSdk()
    {
        var platform = CorpusSdkInstallerPlatform.ForOperatingSystem(isWindows: false, isLinux: true);

        var arguments = platform.BuildInstallerArguments(
            "/tmp/dotnet-install.sh", "5.0.408", "/tmp/private sdk", preserveExistingHost: true);

        Assert.AreEqual("--skip-non-versioned-files", arguments[^1]);
    }

    [TestMethod]
    public void UnsupportedOperatingSystemIsRejected()
    {
        var exception = Assert.ThrowsExactly<PlatformNotSupportedException>(
            () => CorpusSdkInstallerPlatform.ForOperatingSystem(isWindows: false, isLinux: false));

        StringAssert.Contains(exception.Message, "Windows and Linux");
    }

    [TestMethod]
    public void WindowsTestCommandUsesPowerShellQuoting()
    {
        var platform = CorpusSdkInstallerPlatform.ForOperatingSystem(isWindows: true, isLinux: false);

        var command = platform.FormatTestCommand(@"C:\sdk's\dotnet.exe", TestProject);

        Assert.AreEqual($"& 'C:\\sdk''s\\dotnet.exe' test {TestProject} --nologo", command);
    }

    [TestMethod]
    public void LinuxTestCommandUsesPosixShellQuoting()
    {
        var platform = CorpusSdkInstallerPlatform.ForOperatingSystem(isWindows: false, isLinux: true);

        var command = platform.FormatTestCommand("/tmp/sdk's/dotnet", TestProject);

        Assert.AreEqual($"'/tmp/sdk'\"'\"'s/dotnet' test {TestProject} --nologo", command);
    }
}
