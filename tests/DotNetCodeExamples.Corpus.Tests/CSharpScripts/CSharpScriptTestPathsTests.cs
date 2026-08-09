namespace DotNetCodeExamples.Corpus.Tests.CSharpScripts;

[TestClass]
[TestCategory("Unit")]
public sealed class CSharpScriptTestPathsTests
{
    [TestMethod]
    public void RootsResolveFromTheActiveTestOutput()
    {
        var repositoryRoot = CSharpScriptTestPaths.RepositoryRoot;
        var expectedShowcaseRoot = Path.Combine(
            repositoryRoot,
            "examples",
            "language-features",
            "CSharp",
            "csx",
            "roslyn-5.6.0");

        Assert.IsTrue(
            Directory.Exists(Path.Combine(repositoryRoot, ".git"))
            || File.Exists(Path.Combine(repositoryRoot, ".git")));
        Assert.AreEqual(expectedShowcaseRoot, CSharpScriptTestPaths.ShowcaseRoot);
    }

    [TestMethod]
    public void DescriptorReturnsAContainedExistingScenarioPath()
    {
        var expected = Path.Combine(
            CSharpScriptTestPaths.ShowcaseRoot,
            "examples",
            "expression-result",
            "scenario.json");

        Assert.AreEqual(expected, CSharpScriptTestPaths.Descriptor("expression-result"));
        Assert.ThrowsExactly<InvalidOperationException>(() => CSharpScriptTestPaths.Descriptor(".."));
    }
}
