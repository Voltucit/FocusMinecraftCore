using System.Text.Json.Nodes;
using FMCore.Versions;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.VersionTestContext;

namespace FMCore.Tests.Security;

public sealed class VersionSecurityTests
{
    [Theory]
    [InlineData("id", "../outside")]
    [InlineData("id", "C:\\outside")]
    [InlineData("id", "CON")]
    [InlineData("inheritsFrom", "../outside")]
    [InlineData("jar", "a/b")]
    [InlineData("assets", "../outside")]
    public void RejectsUnsafeMetadataIdentifiers(string field, string value)
    {
        var json = new JsonObject { ["id"] = "safe", [field] = value };
        var error = Assert.Throws<VersionException>(() => VersionParser.Parse(json.ToJsonString()));
        Assert.Equal(VersionErrorCode.UnsafePath, error.Code);
    }

    [Theory]
    [InlineData("g:a:../escape")]
    [InlineData("..:a:1")]
    [InlineData("g:CON:1")]
    [InlineData("g:a:1@../zip")]
    public void RejectsUnsafeMavenCoordinates(string name) => Assert.Throws<VersionException>(() => MavenCoordinate.Parse(name));

    [Fact]
    public void RejectsUnsafeArtifactPathsAndOmitsRawMetadataFromErrors()
    {
        const string secret = "private-token";
        var json = """{"id":"test","libraries":[{"name":"g:a:1","downloads":{"artifact":{"url":"https://example.test/file?token=private-token","path":"../../outside"}}}]}""";
        var error = Assert.Throws<VersionException>(() => VersionParser.Parse(json));
        Assert.Equal(VersionErrorCode.UnsafePath, error.Code);
        Assert.DoesNotContain(secret, error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RefusesLinkedMetadataCacheDirectory()
    {
        using var context = new VersionTestContext();
        using var outside = new DownloadTestContext();
        var link = Path.Combine(context.Root, ".cache");
        DownloadSecurityTests.CreateDirectoryLinkOrSkip(link, outside.Root);
        try
        {
            var error = await Assert.ThrowsAsync<VersionException>(() => context.Manager.GetManifestAsync(VersionCachePolicy.CacheOnly, Ct));
            Assert.Equal(VersionErrorCode.UnsafePath, error.Code);
            Assert.Empty(Directory.EnumerateFileSystemEntries(outside.Root));
        }
        finally { Directory.Delete(link); }
    }

    [Fact]
    public async Task InvalidResolutionPathIsRejectedBeforeRefreshingTheCatalog()
    {
        using var context = new VersionTestContext();
        var error = await Assert.ThrowsAsync<VersionException>(() => context.Manager.ResolveAsync("../escape", VersionCachePolicy.Refresh, Ct));
        Assert.Equal(VersionErrorCode.UnsafePath, error.Code);
        Assert.Equal(0, context.Handler.Calls);
    }
}
