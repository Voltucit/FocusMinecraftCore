using System.Diagnostics;
using FMCore.Java;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.JavaTestContext;

namespace FMCore.Tests.Security;

public sealed class JavaSecurityTests
{
    [Theory]
    [InlineData("java")]
    [InlineData("java -version")]
    [InlineData("\"C:\\Java\\bin\\java.exe\" -jar sensitive.jar")]
    [InlineData("../java")]
    [InlineData("\0")]
    public async Task CustomPathCannotBeACommandLine(string path)
    {
        var error = await Assert.ThrowsAsync<JavaException>(() => new JavaManager(EmptyOptions).ProbeAsync(path, Ct));
        Assert.Equal(JavaErrorCode.InvalidPath, error.Code);
        Assert.DoesNotContain("sensitive.jar", error.ToString());
    }

    [Fact]
    public void ProbesHomeAndBinWithSpacesAndMapsJavawToConsoleSibling()
    {
        using var directory = new DownloadTestContext();
        var executable = Candidate(directory, "JDK with spaces");
        Assert.Equal(executable, JavaProbe.ResolveExecutable(directory.At("JDK with spaces")));
        Assert.Equal(executable, JavaProbe.ResolveExecutable(Path.GetDirectoryName(executable)!));
        if (OperatingSystem.IsWindows()) Assert.Equal(executable, JavaProbe.ResolveExecutable(Path.Combine(Path.GetDirectoryName(executable)!, "javaw.exe")));
    }

    [Fact]
    public void RemovesInheritedJavaOptionsWithoutChangingParentEnvironment()
    {
        var start = new ProcessStartInfo("unused");
        foreach (var key in new[] { "JAVA_TOOL_OPTIONS", "JDK_JAVA_OPTIONS", "_JAVA_OPTIONS", "CLASSPATH" })
            start.Environment[key] = "synthetic-sensitive-value";
        start.Environment["FMCORE_TEST_KEEP"] = "retained";
        JavaProbe.ClearInheritedOptions(start);
        Assert.DoesNotContain(start.Environment, e => e.Value == "synthetic-sensitive-value");
        Assert.Equal("retained", start.Environment["FMCORE_TEST_KEEP"]);
    }

    [Fact]
    public async Task MissingExecutableHasExplicitNotFoundError()
    {
        using var directory = new DownloadTestContext();
        var error = await Assert.ThrowsAsync<JavaException>(() => new JavaManager(EmptyOptions).ProbeAsync(directory.At(ExecutableName), Ct));
        Assert.Equal(JavaErrorCode.NotFound, error.Code);
    }

    [Fact]
    public void JavaInstallationLinksAreAllowedWithoutRecursiveTraversal()
    {
        using var directory = new DownloadTestContext();
        Candidate(directory, "real-jdk");
        var link = directory.At("linked-jdk");
        DownloadSecurityTests.CreateDirectoryLinkOrSkip(link, directory.At("real-jdk"));
        try { Assert.Equal(Path.Combine(link, "bin", ExecutableName), JavaProbe.ResolveExecutable(link)); }
        finally { Directory.Delete(link); }
    }
}
