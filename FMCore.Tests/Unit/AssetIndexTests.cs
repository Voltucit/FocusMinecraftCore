using FMCore.Assets;
using FMCore.Downloader;
using FMCore.Tests.Fixtures;

namespace FMCore.Tests.Unit;

public sealed class AssetIndexTests
{
    [Theory]
    [InlineData("1.16.5-assets.json", 2615, false)]
    [InlineData("1.7.10-assets.json", 686, false)]
    [InlineData("1.6.4-assets.json", 1120, true)]
    [InlineData("1.5.2-assets.json", 749, false)]
    public void ParsesRealOfficialIndexFormats(string fixture, int count, bool virtualLayout)
    {
        var index = AssetIndex.Parse(InstallationTestContext.Fixture(fixture));
        Assert.Equal(count, index.Objects.Count);
        Assert.Equal(virtualLayout, index.Virtual);
        Assert.Equal(fixture == "1.5.2-assets.json", index.MapToResources);
        Assert.All(index.Objects, o => Assert.Matches(@"\Aassets/objects/[a-f0-9]{2}/[a-f0-9]{40}\z", o.RelativePath));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"objects\":[]}")]
    [InlineData("{\"objects\":{},\"virtual\":\"yes\"}")]
    [InlineData("{\"objects\":{\"a\":{\"hash\":\"wrong\",\"size\":1}}}")]
    [InlineData("{\"objects\":{\"a\":{\"hash\":\"0000000000000000000000000000000000000000\",\"size\":-1}}}")]
    [InlineData("{\"objects\":{\"a\":{\"hash\":\"0000000000000000000000000000000000000000\",\"size\":1},\"a\":{\"hash\":\"0000000000000000000000000000000000000000\",\"size\":1}}}")]
    [InlineData("{\"objects\":{\"a\":{\"hash\":\"0000000000000000000000000000000000000000\",\"size\":1},\"b\":{\"hash\":\"0000000000000000000000000000000000000000\",\"size\":2}}}")]
    public void RejectsMalformedOrConflictingIndex(string json) =>
        Assert.Equal(AssetErrorCode.InvalidIndex, Assert.Throws<AssetException>(() => AssetIndex.Parse(json)).Code);

    [Fact]
    public void DeduplicatesIdenticalObjectsByHashForDownload()
    {
        using var context = new InstallationTestContext();
        var hash = new string('a', 40);
        var index = new AssetIndex([new("first", hash, 1), new("alias", hash, 1)], true, true);
        var request = Assert.Single(new AssetManager(context.Downloads).CreateDownloads(index));
        Assert.Equal(hash, request.ExpectedHash);
        Assert.Equal(DownloadHashAlgorithm.Sha1, request.HashAlgorithm);
        Assert.Equal($"https://resources.download.minecraft.net/aa/{hash}", request.Source.AbsoluteUri);
    }
}
