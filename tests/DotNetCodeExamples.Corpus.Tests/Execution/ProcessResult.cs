namespace DotNetCodeExamples.Corpus.Tests.Execution;

internal sealed record ProcessResult(
    string Executable,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    int ExitCode,
    string StandardOutput,
    string StandardError);
