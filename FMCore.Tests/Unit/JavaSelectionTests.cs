using FMCore.Java;
using FMCore.Versions;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.JavaTestContext;

namespace FMCore.Tests.Unit;

public sealed class JavaSelectionTests
{
    private static VersionMetadata Requires(int major) => VersionParser.Parse($$$"""{"id":"test","javaVersion":{"majorVersion":{{{major}}}}}""");
    private static JavaSelectionOptions X64 => new() { TargetArchitecture = JavaArchitecture.X64 };

    [Fact]
    public void ChoosesRequiredMajorAndNewestPatchRatherThanNewestMajor()
    {
        var selected = new JavaManager(EmptyOptions).Select([Runtime("25"), Runtime("17.0.9"), Runtime("21"), Runtime("17.0.12")], Requires(17), X64);
        Assert.Equal("17.0.12", selected.Version.Text);
    }

    [Theory]
    [InlineData("8", JavaArchitecture.X64, JavaErrorCode.MajorVersionMismatch)]
    [InlineData("25", JavaArchitecture.X64, JavaErrorCode.MajorVersionMismatch)]
    [InlineData("21", JavaArchitecture.X86, JavaErrorCode.ArchitectureMismatch)]
    [InlineData("21", JavaArchitecture.Unknown, JavaErrorCode.ArchitectureMismatch)]
    [InlineData("21-ea", JavaArchitecture.X64, JavaErrorCode.EarlyAccessNotAllowed)]
    public void ReportsSpecificCompatibilityFailure(string version, JavaArchitecture arch, JavaErrorCode code)
    {
        var result = JavaCompatibility.Check(Runtime(version, arch), Requires(21), X64);
        Assert.False(result.IsCompatible);
        Assert.Equal(code, result.ErrorCode);
        Assert.Equal(21, result.RequiredMajorVersion);
    }

    [Fact]
    public void ArchitectureAndEarlyAccessOptInAreExplicit()
    {
        var options = new JavaSelectionOptions { TargetArchitecture = JavaArchitecture.Arm64, AllowEarlyAccess = true };
        Assert.True(JavaCompatibility.Check(Runtime("25-ea+12", JavaArchitecture.Arm64), Requires(25), options).IsCompatible);
        Assert.Equal(JavaArchitecture.Arm64, new JavaManager(EmptyOptions).Select(
            [Runtime("25", JavaArchitecture.X64), Runtime("25", JavaArchitecture.Arm64)], Requires(25), options).Architecture);
    }

    [Fact]
    public void MissingRequirementNeedsExplicitFallbackAndNeverOverridesMetadata()
    {
        var document = VersionParser.Parse(VersionTestContext.Fixture("1.12.2.json")).ToJson();
        document.Remove("javaVersion");
        var legacy = VersionParser.Parse(document.ToJsonString());
        Assert.Equal(JavaErrorCode.RequirementMissing, JavaCompatibility.Check(Runtime("8"), legacy, X64).ErrorCode);
        var manager = new JavaManager(EmptyOptions);
        Assert.Equal(JavaErrorCode.RequirementMissing, Assert.Throws<JavaException>(() => manager.Select([Runtime("8")], legacy, X64)).Code);
        var options = X64 with { FallbackMajorVersion = 8 };
        Assert.Equal(8, manager.Select([Runtime("8"), Runtime("25")], legacy, options).Version.Major);
        Assert.Equal(21, manager.Select([Runtime("8"), Runtime("21")], Requires(21), options).Version.Major);
    }

    [Fact]
    public void OfficialLegacyMetadataAlreadyProvidesItsJava8Requirement()
    {
        var version = VersionParser.Parse(VersionTestContext.Fixture("1.12.2.json"));
        Assert.Equal(8, new JavaManager(EmptyOptions).Select([Runtime("25"), Runtime("8")], version, X64).Version.Major);
    }

    [Fact]
    public void NoMatchDoesNotFallBackToWrongMajorOrArchitecture()
    {
        var error = Assert.Throws<JavaException>(() => new JavaManager(EmptyOptions).Select(
            [Runtime("25"), Runtime("21", JavaArchitecture.X86)], Requires(21), X64));
        Assert.Equal(JavaErrorCode.NoCompatibleRuntime, error.Code);
    }

    [Fact]
    public async Task CustomJavaIsAuthoritativeAndIsNotSilentlyReplaced()
    {
        var calls = new List<string>();
        var manager = new JavaManager(EmptyOptions, (path, _) => { calls.Add(path); return Task.FromResult(Runtime("17")); });
        var error = await Assert.ThrowsAsync<JavaException>(() => manager.SelectAsync(Requires(21), "custom", X64, cancellationToken: Ct));
        Assert.Equal(JavaErrorCode.MajorVersionMismatch, error.Code);
        Assert.Equal(["custom"], calls);
    }

    [Fact]
    public async Task SelectionReusesEffectiveInheritedJavaRequirement()
    {
        using var context = new VersionTestContext();
        await context.SaveAsync(VersionTestContext.Fixture("1.21.1.json"));
        await context.SaveAsync(VersionTestContext.Fixture("fabric-1.21.1.json"));
        var resolved = await context.Manager.ResolveAsync("fabric-loader-0.16.10-1.21.1", VersionCachePolicy.CacheOnly, Ct);
        Assert.Equal(21, new JavaManager(EmptyOptions).Select([Runtime("17"), Runtime("21"), Runtime("25")], resolved.Metadata, X64).Version.Major);
        Assert.Equal(0, context.Handler.Calls);
    }
}
