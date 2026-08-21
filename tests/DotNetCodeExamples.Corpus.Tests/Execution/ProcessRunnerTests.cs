using System.Diagnostics;

namespace DotNetCodeExamples.Corpus.Tests.Execution;

[TestClass]
[TestCategory("Unit")]
public sealed class ProcessRunnerTests
{
    private static readonly string[] ListSdksArguments = ["--list-sdks"];

    [TestMethod]
    public async Task RunAsyncRetainsExecutableAndArgumentsInResult()
    {
        var runner = new ProcessRunner();

        var result = await runner.RunAsync("dotnet", ListSdksArguments);

        Assert.AreEqual("dotnet", result.Executable);
        CollectionAssert.AreEqual(ListSdksArguments, result.Arguments.ToArray());
        Assert.AreEqual(Environment.CurrentDirectory, result.WorkingDirectory);
        Assert.AreEqual(0, result.ExitCode);
    }

    [TestMethod]
    public async Task RunAsyncCanRemoveAnInheritedEnvironmentVariable()
    {
        var variableName = $"DOTNET_KNOWLEDGE_TEST_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(variableName, "inherited");
        var runner = new ProcessRunner();
        var (executable, arguments) = RemovedVariableCheckCommand(variableName);

        try
        {
            var result = await runner.RunAsync(
                executable,
                arguments,
                environmentVariables: new Dictionary<string, string?> { [variableName] = null });

            Assert.AreEqual(0, result.ExitCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }

    [TestMethod]
    public async Task RunAsyncTimesOutWhenAChildRetainsRedirectedOutputAfterItsParentExits()
    {
        var runner = new ProcessRunner(TimeSpan.FromMilliseconds(100));
        var (executable, arguments) = DetachedLongRunningCommand();

        await Assert.ThrowsExactlyAsync<TimeoutException>(() =>
            runner.RunAsync(executable, arguments));
    }

    [TestMethod]
    [Timeout(3000)]
    public async Task RunAsyncKillsTheProcessWhenCancellationIsRequested()
    {
        var runner = new ProcessRunner();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var (executable, arguments) = LongRunningCommand();

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
            runner.RunAsync(executable, arguments, cancellationToken: cancellation.Token));
    }

    [TestMethod]
    [Timeout(3000)]
    public async Task RunAsyncCancellationTerminatesDescendantThatRetainsRedirectedOutput()
    {
        var marker = Path.Combine(Path.GetTempPath(), $"dotnet-code-examples-{Guid.NewGuid():N}.txt");
        var runner = new ProcessRunner();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var (executable, arguments) = DetachedNestedCommand(marker);

        try
        {
            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() =>
                runner.RunAsync(executable, arguments, cancellationToken: cancellation.Token));

            await Task.Delay(TimeSpan.FromMilliseconds(1500));

            Assert.IsFalse(File.Exists(marker), "The descendant completed after cancellation instead of being terminated.");
        }
        finally
        {
            File.Delete(marker);
        }
    }

    [TestMethod]
    [Timeout(3000)]
    public async Task RunAsyncKillsStartedProcessWhenJobAssignmentFails()
    {
        var processId = 0;
        var runner = new ProcessRunner(
            TimeSpan.FromSeconds(1),
            process =>
            {
                processId = process.Id;
                throw new InvalidOperationException("Job assignment failed.");
            });
        var (executable, arguments) = LongRunningCommand();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            runner.RunAsync(executable, arguments));

        await Task.Delay(TimeSpan.FromMilliseconds(100));
        Assert.IsFalse(IsRunning(processId));
    }

    private static (string Executable, string[] Arguments) LongRunningCommand() =>
        OperatingSystem.IsWindows()
            ? ("cmd", ["/c", "ping -n 10 127.0.0.1"])
            : ("sleep", ["10"]);

    private static (string Executable, string[] Arguments) DetachedLongRunningCommand() =>
        OperatingSystem.IsWindows()
            ? ("cmd", ["/c", "start /b ping -n 10 127.0.0.1"])
            : ("/bin/sh", ["-c", "sleep 10 & exit 0"]);

    private static (string Executable, string[] Arguments) DetachedNestedCommand(string marker) =>
        OperatingSystem.IsWindows()
            ? ("cmd", ["/c", $"start \"\" /b cmd /c \"ping -n 2 127.0.0.1 > nul & echo completed > \"{marker}\"\""])
            : ("/bin/sh", ["-c", $"(sleep 1; echo completed > \"{marker}\") & exit 0"]);

    private static (string Executable, string[] Arguments) RemovedVariableCheckCommand(string variableName) =>
        OperatingSystem.IsWindows()
            ? ("cmd", ["/c", $"if defined {variableName} (exit /b 1) else (exit /b 0)"])
            : ("/bin/sh", ["-c", "if [ -z \"${" + variableName + ":-}\" ]; then exit 0; else exit 1; fi"]);

    private static bool IsRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
