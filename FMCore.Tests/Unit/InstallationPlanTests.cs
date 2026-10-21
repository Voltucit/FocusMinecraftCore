using System.Text.Json.Nodes;
using FMCore.Installer;
using FMCore.Versions;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.InstallationTestContext;

namespace FMCore.Tests.Unit;

public sealed class InstallationPlanTests
{
    private static ResolvedVersion Resolve(VersionMetadata metadata) => new(metadata, metadata.Id, metadata.Downloads.GetValueOrDefault("client"), [metadata.Id]);

    [Theory]
    [InlineData("AMD64")]
    [InlineData("X64")]
    [InlineData("ARM64")]
    public async Task NormalizesArchitectureAliasesBeforeResolvingBitnessClassifier(string arch)
    {
        using var context = new InstallationTestContext();
        await context.SeedAsync();
        var version = await context.Versions.ResolveAsync("fixture", VersionCachePolicy.CacheOnly, Ct);
        var plan = context.Installer.CreatePlan(version, Options with { Context = new("windows", arch, "10") });
        Assert.EndsWith("natives-windows-64.jar", Assert.Single(plan.NativeArchives).RelativePath);
        Assert.True(plan.Context.Is64Bit);
    }

    [Fact]
    public void KeepsTextToSpeechClasspathAndNativesAndDeduplicatesOfficialLibraries()
    {
        using var context = new InstallationTestContext();
        var version = VersionParser.Parse(Fixture("1.16.5.json"));
        var plan = context.Installer.CreatePlan(Resolve(version), Options);
        Assert.Single(plan.Libraries, r => r.RelativePath.EndsWith("text2speech-1.11.3.jar", StringComparison.Ordinal));
        Assert.Contains(plan.ClasspathEntries, p => p.EndsWith("text2speech-1.11.3.jar", StringComparison.Ordinal));
        Assert.Contains(plan.NativeArchives, a => a.RelativePath.EndsWith("text2speech-1.11.3-natives-windows.jar", StringComparison.Ordinal));
        Assert.DoesNotContain(plan.NativeArchives, a => a.RelativePath.Contains("natives-linux", StringComparison.Ordinal));
        Assert.Equal(plan.Libraries.Count, plan.Libraries.Select(r => r.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal("versions/1.16.5/1.16.5.jar", plan.ClasspathEntries[^1]);
    }

    [Fact]
    public void ModernNativeArtifactsStayOnClasspathRatherThanBeingMisclassifiedAsLegacyArchives()
    {
        using var context = new InstallationTestContext();
        var version = VersionParser.Parse(VersionTestContext.Fixture("1.21.1.json"));
        var plan = context.Installer.CreatePlan(Resolve(version), Options);
        Assert.Contains(plan.ClasspathEntries, p => p.Contains("lwjgl-3.3.3-natives-windows.jar", StringComparison.Ordinal));
        Assert.DoesNotContain(plan.NativeArchives, a => a.RelativePath.Contains("lwjgl-3.3.3-natives-windows.jar", StringComparison.Ordinal));
        Assert.DoesNotContain(plan.ClasspathEntries, p => p.Contains("natives-linux", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UsesInheritedClientSourceAndDerivesMissingClassifierPathFromMavenCoordinate()
    {
        using var context = new InstallationTestContext();
        await context.SeedAsync();
        await context.Versions.SaveLocalAsync(VersionParser.Parse("""{"id":"child","inheritsFrom":"fixture"}"""), Ct);
        var resolved = await context.Versions.ResolveAsync("child", VersionCachePolicy.CacheOnly, Ct);
        var plan = context.Installer.CreatePlan(resolved, Options);
        Assert.Equal("versions/fixture/fixture.jar", plan.Client.RelativePath);
        Assert.Equal("versions/child/natives/windows-x64", plan.NativeDirectory);
        Assert.Equal("libraries/example/both/1/both-1-natives-windows-64.jar", Assert.Single(plan.NativeArchives).RelativePath);
    }

    [Fact]
    public async Task EvaluatesOperatingSystemArchitectureAndFeatureRules()
    {
        using var context = new InstallationTestContext();
        var document = await context.SeedAsync();
        var library = document["libraries"]![1]!;
        library["rules"] = JsonNode.Parse("""[{"action":"allow","os":{"name":"windows","arch":"x86"},"features":{"is_demo_user":true}}]""");
        var version = Resolve(VersionParser.Parse(document.ToJsonString()));
        var x64 = context.Installer.CreatePlan(version, Options);
        Assert.DoesNotContain(x64.ClasspathEntries, p => p.Contains("linux-only", StringComparison.Ordinal));
        // This context needs a matching classifier; remove the unrelated native-only requirement first.
        document["libraries"]![0]!.AsObject().Remove("natives");
        version = Resolve(VersionParser.Parse(document.ToJsonString()));
        var x86 = context.Installer.CreatePlan(version, Options with { Context = new("windows", "x86", "10", new Dictionary<string, bool> { ["is_demo_user"] = true }) });
        Assert.Contains(x86.ClasspathEntries, p => p.Contains("linux-only", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("client", InstallationErrorCode.MissingClient)]
    [InlineData("assetIndex", InstallationErrorCode.MissingAssetIndex)]
    [InlineData("classifier", InstallationErrorCode.MissingNativeClassifier)]
    public async Task MissingRequiredFilesFailBeforeAnyGameDownload(string missing, InstallationErrorCode code)
    {
        using var context = new InstallationTestContext();
        var document = await context.SeedAsync();
        if (missing == "client") document["downloads"]!.AsObject().Remove("client");
        else if (missing == "assetIndex") document.Remove("assetIndex");
        else document["libraries"]![0]!["downloads"]!["classifiers"]!.AsObject().Clear();
        await context.SaveAsync(document);
        var error = await Assert.ThrowsAsync<InstallationException>(() => context.Installer.InstallAsync("fixture", Options, cancellationToken: Ct));
        Assert.Equal(code, error.Code);
        Assert.Empty(context.Requests);
    }

    [Fact]
    public async Task ConflictingSharedArtifactIsNotSilentlyOverwritten()
    {
        using var context = new InstallationTestContext();
        var document = await context.SeedAsync();
        var copy = document["libraries"]![0]!.DeepClone();
        copy["downloads"]!["artifact"]!["sha1"] = new string('f', 40);
        document["libraries"]!.AsArray().Add(copy);
        Assert.Equal(InstallationErrorCode.ConflictingFiles, Assert.Throws<InstallationException>(() =>
            context.Installer.CreatePlan(Resolve(VersionParser.Parse(document.ToJsonString())), Options)).Code);
    }

    [Fact]
    public async Task SharedArchiveDoesNotLoseFilesNeededByAnotherApplicableDeclaration()
    {
        using var context = new InstallationTestContext();
        var document = await context.SeedAsync();
        document["libraries"]![0]!["extract"]!["exclude"]!.AsArray().Add("needed.dll");
        var second = document["libraries"]![0]!.DeepClone();
        second["extract"]!["exclude"] = new JsonArray("META-INF/");
        document["libraries"]!.AsArray().Add(second);
        var plan = context.Installer.CreatePlan(Resolve(VersionParser.Parse(document.ToJsonString())), Options);
        var native = Assert.Single(plan.NativeArchives);
        Assert.Equal(["META-INF/"], native.Excludes);
        Assert.NotNull(native.Sha1);
    }
}
