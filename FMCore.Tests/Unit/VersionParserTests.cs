using System.Text.Json.Nodes;
using FMCore.Versions;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.VersionTestContext;

namespace FMCore.Tests.Unit;

public sealed class VersionParserTests
{
    [Theory]
    [InlineData("1.21.1.json", "1.21.1", false)]
    [InlineData("1.12.2.json", "1.12.2", true)]
    [InlineData("24w14a.json", "24w14a", false)]
    [InlineData("fabric-1.21.1.json", "fabric-loader-0.16.10-1.21.1", false)]
    public void ParsesOfficialAndFabricProfiles(string file, string id, bool legacy)
    {
        var version = VersionParser.Parse(Fixture(file));
        Assert.Equal(id, version.Id);
        Assert.NotEmpty(version.Libraries);
        Assert.NotNull(version.MainClass);
        Assert.Equal(legacy, version.LegacyGameArguments != null);
    }

    [Fact]
    public void ParsesOfficialManifestAndBothVersionTypes()
    {
        var manifest = VersionParser.ParseManifest(Fixture("official-manifest.json"));
        Assert.Contains(manifest.Versions, v => v.Id == manifest.LatestRelease && v.Type == "release");
        Assert.Contains(manifest.Versions, v => v.Id == manifest.LatestSnapshot);
        Assert.Contains(manifest.Versions, v => v.Id == "24w14a" && v.Type == "snapshot");
        Assert.All(manifest.Versions, v => Assert.Equal(40, v.Sha1.Length));
    }

    [Fact]
    public void PreservesFutureFieldsAndReturnsIndependentJsonCopies()
    {
        var json = JsonNode.Parse(Fixture("1.21.1.json"))!.AsObject();
        json["futureMetadata"] = new JsonObject { ["list"] = new JsonArray(1, 2, 3) };
        var parsed = VersionParser.Parse(json.ToJsonString());
        var copy = parsed.ToJson();
        Assert.True(JsonNode.DeepEquals(json, copy));
        copy["id"] = "changed";
        copy["futureMetadata"]!["list"] = null;
        Assert.Equal("1.21.1", parsed.Id);
        Assert.Equal(3, parsed.ToJson()["futureMetadata"]!["list"]!.AsArray().Count);
    }

    [Fact]
    public void ExposesClientAssetsLoggingJavaAndNativeMetadata()
    {
        var modern = VersionParser.Parse(Fixture("1.21.1.json"));
        Assert.Equal(21, modern.Java!.MajorVersion);
        Assert.NotNull(modern.Downloads["client"].Sha1);
        Assert.True(modern.AssetIndex!.TotalSize > 0);
        Assert.Contains("${path}", modern.Logging!.Argument, StringComparison.Ordinal);
        var old = VersionParser.Parse(Fixture("1.12.2.json"));
        var native = old.Libraries.First(l => l.Natives.ContainsKey("windows"));
        Assert.True(native.Classifiers.ContainsKey(native.GetNativeClassifier(new("windows", "amd64", "10.0"))!));
        Assert.Contains("META-INF/", native.ExtractExcludes);
        Assert.Contains("--username", old.Arguments.GetGameArguments(new("windows", "amd64", "10.0")));
    }

    [Theory]
    [InlineData("org.example:mod:1.0", "org/example/mod/1.0/mod-1.0.jar")]
    [InlineData("org.example:mod:1.0:natives-windows", "org/example/mod/1.0/mod-1.0-natives-windows.jar")]
    [InlineData("org.example:mod:1.0+build.2@zip", "org/example/mod/1.0+build.2/mod-1.0+build.2.zip")]
    public void ComputesMavenPaths(string name, string path) => Assert.Equal(path, MavenCoordinate.Parse(name).RelativePath);

    [Theory]
    [InlineData("{bad json}")]
    [InlineData("{\"id\":\"test\",\"libraries\":{}}")]
    [InlineData("{\"id\":\"test\",\"arguments\":{\"game\":[true]}}")]
    [InlineData("{\"id\":\"test\",\"javaVersion\":{\"majorVersion\":-1}}")]
    [InlineData("{\"id\":\"test\",\"downloads\":{\"client\":{\"url\":\"file:///etc/passwd\"}}}")]
    [InlineData("{\"id\":\"test\",\"libraries\":[{\"name\":\"g:a:1\",\"rules\":[{\"action\":\"unknown\"}]}]}")]
    [InlineData("{\"id\":\"test\",\"libraries\":[{\"name\":\"g:a:1\",\"rules\":[{\"action\":\"allow\",\"os\":{\"version\":\"[\"}}]}]}")]
    public void RejectsMalformedMetadataWithStructuredErrors(string json)
    {
        var error = Assert.Throws<VersionException>(() => VersionParser.Parse(json));
        Assert.Equal(VersionErrorCode.InvalidMetadata, error.Code);
    }

    [Fact]
    public void TokenizesQuotedLegacyArgumentsWithoutExpandingVariables()
    {
        var document = new JsonObject { ["id"] = "legacy", ["minecraftArguments"] = "--name \"hello world\" --empty '' --uuid ${auth_uuid}" };
        var parsed = VersionParser.Parse(document.ToJsonString());
        Assert.Equal(["--name", "hello world", "--empty", "", "--uuid", "${auth_uuid}"], parsed.Arguments.GetGameArguments(RuleContext.Current));
    }
}
