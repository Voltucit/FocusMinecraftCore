using System.Text.Json.Nodes;
using FMCore.Versions;
using FMCore.Tests.Fixtures;
using FMCore.Tests.Security;
using static FMCore.Tests.Fixtures.VersionTestContext;

namespace FMCore.Tests.Integration;

public sealed class LocalVersionTests
{
    [Fact]
    public async Task MissingVersionsDirectoryReturnsEmptyAndDoesNotCreateFiles()
    {
        using var context = new VersionTestContext();
        Assert.Empty(await context.Manager.GetLocalVersionsAsync(Ct));
        Assert.Empty(Directory.EnumerateFileSystemEntries(context.Root));
        Assert.Equal(0, context.Handler.Calls);
    }

    [Fact]
    public async Task DistinguishesMetadataFromAnExistingClientFile()
    {
        using var context = new VersionTestContext();
        await context.SaveAsync(Fixture("inherit-base.json"));
        var metadataOnly = Assert.Single(await context.Manager.GetLocalVersionsAsync(Ct));
        Assert.Equal("test-base", metadataOnly.Id);
        Assert.Equal("release", metadataOnly.Type);
        Assert.False(metadataOnly.HasClientJar);
        Assert.Null(metadataOnly.ErrorCode);
        await WriteJarAsync(context, "test-base");
        var downloaded = Assert.Single(await context.Manager.GetLocalVersionsAsync(Ct));
        Assert.True(downloaded.HasClientJar);
        Assert.Equal(context.Manager.GetMetadataPath("test-base"), downloaded.MetadataPath);
        Assert.Equal(JarPath(context, "test-base"), downloaded.ClientJarPath);
        Assert.Equal(0, context.Handler.Calls);
    }

    [Fact]
    public async Task ChildVersionUsesItsParentsClientFile()
    {
        using var context = new VersionTestContext();
        await context.SaveAsync(Fixture("inherit-base.json"));
        await context.SaveAsync(Fixture("inherit-child.json"));
        await WriteJarAsync(context, "test-base");
        var versions = await context.Manager.GetLocalVersionsAsync(Ct);
        Assert.Equal(["test-base", "test-child"], versions.Select(v => v.Id));
        var child = versions.Single(v => v.Id == "test-child");
        Assert.True(child.HasClientJar);
        Assert.Equal("test-base", child.ClientJarId);
        Assert.Equal("release", child.Type);
        Assert.Equal(JarPath(context, "test-base"), child.ClientJarPath);
        Assert.Equal(0, context.Handler.Calls);
    }

    [Fact]
    public async Task ChildWithOwnDownloadDoesNotMistakeParentJarForItsOwn()
    {
        using var context = new VersionTestContext();
        await context.SaveAsync(Fixture("inherit-base.json"));
        var child = JsonNode.Parse(Fixture("inherit-child.json"))!.AsObject();
        child["downloads"] = JsonNode.Parse("""{"client":{"url":"https://metadata.example.test/own.jar"}}""");
        await context.SaveAsync(child.ToJsonString());
        await WriteJarAsync(context, "test-base");
        var result = (await context.Manager.GetLocalVersionsAsync(Ct)).Single(v => v.Id == "test-child");
        Assert.Equal("test-child", result.ClientJarId);
        Assert.False(result.HasClientJar);
    }

    [Fact]
    public async Task SupportsExplicitJarReferenceOutsideTheInheritanceChain()
    {
        using var context = new VersionTestContext();
        await context.SaveAsync(Fixture("inherit-base.json"));
        await context.SaveAsync("""{"id":"alternate","downloads":{"client":{"url":"https://metadata.example.test/alternate.jar"}}}""");
        await context.SaveAsync("""{"id":"child","inheritsFrom":"test-base","jar":"alternate"}""");
        await WriteJarAsync(context, "alternate");
        var result = (await context.Manager.GetLocalVersionsAsync(Ct)).Single(v => v.Id == "child");
        Assert.Equal("alternate", result.ClientJarId);
        Assert.True(result.HasClientJar);
    }

    [Theory]
    [InlineData("not-json", VersionErrorCode.InvalidMetadata)]
    [InlineData("{\"id\":\"wrong\"}", VersionErrorCode.InvalidMetadata)]
    [InlineData("{\"id\":\"broken\",\"inheritsFrom\":\"missing\"}", VersionErrorCode.NotCached)]
    [InlineData("{\"id\":\"broken\",\"inheritsFrom\":\"broken\"}", VersionErrorCode.InheritanceCycle)]
    [InlineData("{\"id\":\"broken\",\"inheritsFrom\":\"../escape\"}", VersionErrorCode.UnsafePath)]
    public async Task ReportsBrokenEntriesWithoutHidingValidVersions(string json, VersionErrorCode expected)
    {
        using var context = new VersionTestContext();
        await context.SaveAsync(Fixture("inherit-base.json"));
        await WriteJarAsync(context, "test-base");
        var path = context.Manager.GetMetadataPath("broken");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, json, Ct);
        var versions = await context.Manager.GetLocalVersionsAsync(Ct);
        var broken = versions.Single(v => v.Id == "broken");
        Assert.Equal(expected, broken.ErrorCode);
        Assert.False(broken.HasClientJar);
        Assert.True(versions.Single(v => v.Id == "test-base").HasClientJar);
        Assert.Equal(0, context.Handler.Calls);
    }

    [Fact]
    public async Task IgnoresDirectoriesWithoutMatchingMetadataAndDoesNotRecurse()
    {
        using var context = new VersionTestContext();
        var nested = Path.Combine(context.Root, "versions", "container", "nested");
        Directory.CreateDirectory(nested);
        await File.WriteAllTextAsync(Path.Combine(nested, "nested.json"), "{\"id\":\"nested\"}", Ct);
        Assert.Empty(await context.Manager.GetLocalVersionsAsync(Ct));
        Assert.Equal(0, context.Handler.Calls);
    }

    [Fact]
    public async Task EmptyJarFileDoesNotCountAsDownloaded()
    {
        using var context = new VersionTestContext();
        await context.SaveAsync(Fixture("inherit-base.json"));
        await File.WriteAllBytesAsync(JarPath(context, "test-base"), [], Ct);
        Assert.False(Assert.Single(await context.Manager.GetLocalVersionsAsync(Ct)).HasClientJar);
    }

    [Fact]
    public async Task LocalInventoryDoesNotDependOnOrModifyTheOfficialCatalogCache()
    {
        using var context = new VersionTestContext();
        await context.SaveAsync(Fixture("inherit-base.json"));
        var catalog = Path.Combine(context.Root, ".cache", "metadata", "version_manifest_v2.json");
        Directory.CreateDirectory(Path.GetDirectoryName(catalog)!);
        await File.WriteAllTextAsync(catalog, "invalid-catalog", Ct);
        var profilePath = context.Manager.GetMetadataPath("test-base");
        var original = await File.ReadAllBytesAsync(profilePath, Ct);
        Assert.Null(Assert.Single(await context.Manager.GetLocalVersionsAsync(Ct)).ErrorCode);
        Assert.Equal(original, await File.ReadAllBytesAsync(profilePath, Ct));
        Assert.Equal("invalid-catalog", await File.ReadAllTextAsync(catalog, Ct));
        Assert.Equal(0, context.Handler.Calls);
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task SupportsCancellationWithoutNetworkAccess()
    {
        using var context = new VersionTestContext();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        await cancel.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Manager.GetLocalVersionsAsync(cancel.Token));
        Assert.Equal(0, context.Handler.Calls);
    }

    [Fact]
    public async Task ReportsLinkedVersionDirectoryAndContinuesScanning()
    {
        using var context = new VersionTestContext();
        using var outside = new DownloadTestContext();
        await context.SaveAsync(Fixture("inherit-base.json"));
        var link = Path.Combine(context.Root, "versions", "linked");
        DownloadSecurityTests.CreateDirectoryLinkOrSkip(link, outside.Root);
        try
        {
            var versions = await context.Manager.GetLocalVersionsAsync(Ct);
            Assert.Equal(VersionErrorCode.UnsafePath, versions.Single(v => v.Id == "linked").ErrorCode);
            Assert.Null(versions.Single(v => v.Id == "test-base").ErrorCode);
            Assert.Empty(Directory.EnumerateFileSystemEntries(outside.Root));
        }
        finally { Directory.Delete(link); }
    }

    [Fact]
    public async Task DoesNotFollowLinkedClientJarPath()
    {
        using var context = new VersionTestContext();
        using var outside = new DownloadTestContext();
        await context.SaveAsync(Fixture("inherit-base.json"));
        var link = JarPath(context, "test-base");
        DownloadSecurityTests.CreateDirectoryLinkOrSkip(link, outside.Root);
        try
        {
            var result = Assert.Single(await context.Manager.GetLocalVersionsAsync(Ct));
            Assert.Equal(VersionErrorCode.UnsafePath, result.ErrorCode);
            Assert.False(result.HasClientJar);
            Assert.Null(result.ClientJarPath);
        }
        finally { Directory.Delete(link); }
    }

    [Fact]
    public async Task RejectsLinkedVersionsRoot()
    {
        using var context = new VersionTestContext();
        using var outside = new DownloadTestContext();
        var link = Path.Combine(context.Root, "versions");
        DownloadSecurityTests.CreateDirectoryLinkOrSkip(link, outside.Root);
        try
        {
            var error = await Assert.ThrowsAsync<VersionException>(() => context.Manager.GetLocalVersionsAsync(Ct));
            Assert.Equal(VersionErrorCode.UnsafePath, error.Code);
        }
        finally { Directory.Delete(link); }
    }

    // These bytes prove file presence only, deliberately not JAR/game integrity.
    private static Task WriteJarAsync(VersionTestContext context, string id) => File.WriteAllBytesAsync(JarPath(context, id), [1, 2, 3], Ct);
    private static string JarPath(VersionTestContext context, string id) => Path.Combine(context.Root, "versions", id, id + ".jar");
}
