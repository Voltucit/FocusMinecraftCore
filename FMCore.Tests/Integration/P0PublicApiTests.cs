using FMCore.Authentication;
using FMCore.Downloader;
using FMCore.Java;
using FMCore.Tests.Fixtures;
using FMCore.Versions;
using static FMCore.Tests.Fixtures.VersionTestContext;

namespace FMCore.Tests.Integration;

public sealed class P0PublicApiTests
{
    [Fact]
    public async Task LocalInventoryExposesResolvedJavaAndLoaderWithoutNetwork()
    {
        using var context = new VersionTestContext();
        await context.SaveAsync(Fixture("1.21.1.json"));
        await context.SaveAsync(Fixture("fabric-1.21.1.json"));
        var versions = await GameCoreUtil.GetGameCoresAsync(context.Root, Ct);
        var fabric = versions.Single(v => v.Loaders.Any(l => l.Name == "Fabric"));
        Assert.Equal("1.21.1", fabric.MinecraftVersion);
        Assert.Equal(21, fabric.JavaRequirement!.MajorVersion);
        Assert.Equal("0.16.10", Assert.Single(fabric.Loaders).Version);
        Assert.NotNull(fabric.ResolvedVersion);
        Assert.False(fabric.HasClientJar);
        Assert.Equal(2, GameCoreUtil.GetGameCores(context.Root).Count);
        Assert.Equal(fabric.Id, GameCoreUtil.GetGameCore(fabric.Id, context.Root).Id);
        Assert.Equal(fabric.Id, (await GameCoreUtil.GetGameCoreAsync(fabric.Id, context.Root, Ct)).Id);
        Assert.Equal(0, context.Handler.Calls);
    }

    [Fact]
    public async Task InvalidLocalProfileRemainsVisibleWithAnError()
    {
        using var context = new DownloadTestContext();
        var path = context.At("versions/broken/broken.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "{}", Ct);
        var version = Assert.Single(await GameCoreUtil.GetGameCoresAsync(context.Root, Ct));
        Assert.Equal(VersionErrorCode.InvalidMetadata, version.ErrorCode);
        Assert.Null(version.ResolvedVersion);
    }

    [Theory]
    [InlineData("net.fabricmc:fabric-loader:0.16.10", "Fabric")]
    [InlineData("org.quiltmc:quilt-loader:0.27.0", "Quilt")]
    [InlineData("net.minecraftforge:forge:1.20.1-47.2.0", "Forge")]
    [InlineData("net.neoforged:neoforge:21.1.0", "NeoForge")]
    public async Task LoaderInventoryUsesExistingMetadataRatherThanInstallingAnything(string library, string expected)
    {
        using var context = new VersionTestContext();
        var json = new System.Text.Json.Nodes.JsonObject
        { ["id"] = "loader", ["libraries"] = new System.Text.Json.Nodes.JsonArray { new System.Text.Json.Nodes.JsonObject { ["name"] = library } } };
        await context.SaveAsync(json.ToJsonString());
        Assert.Equal(expected, Assert.Single((await GameCoreUtil.GetGameCoreAsync("loader", context.Root, Ct)).Loaders).Name);
        Assert.Equal(0, context.Handler.Calls);
    }

    [Fact]
    public async Task JavaHelpersOfferAsyncAndBlockingDiscoveryAndStructuredManualPathErrors()
    {
        Assert.Empty(await JavaUtil.GetJavasAsync(JavaTestContext.EmptyOptions, cancellationToken: Ct));
        Assert.Empty(JavaUtil.GetJavas(JavaTestContext.EmptyOptions));
        Assert.Equal(JavaErrorCode.InvalidPath, (await Assert.ThrowsAsync<JavaException>(() => JavaUtil.GetJavaInfoAsync("relative/java", Ct))).Code);
        Assert.Equal(JavaErrorCode.InvalidPath, Assert.Throws<JavaException>(() => JavaUtil.GetJavaInfo("relative/java")).Code);
    }

    [Theory]
    [InlineData(DownloadHashAlgorithm.Sha1)]
    [InlineData(DownloadHashAlgorithm.Sha256)]
    [InlineData(DownloadHashAlgorithm.Sha512)]
    public async Task DownloadVerificationUsesTheRequestedHashAndReportsDistinctFailureStates(DownloadHashAlgorithm algorithm)
    {
        using var directory = new DownloadTestContext();
        var downloads = new DownloadManager(directory.Root);
        var request = DownloadTestContext.Request("file.bin", "data", algorithm);
        Assert.Equal(FileVerificationState.Missing, (await downloads.VerifyAsync(request, Ct)).State);
        await File.WriteAllTextAsync(directory.At("file.bin"), "xxxx", Ct);
        Assert.Equal(FileVerificationState.HashMismatch, (await downloads.VerifyAsync(request, Ct)).State);
        await File.WriteAllTextAsync(directory.At("file.bin"), "longer", Ct);
        Assert.Equal(FileVerificationState.SizeMismatch, (await downloads.VerifyAsync(request, Ct)).State);
        await File.WriteAllTextAsync(directory.At("file.bin"), "data", Ct);
        Assert.Equal(FileVerificationState.Valid, (await downloads.VerifyAsync(request, Ct)).State);
        Assert.Equal(FileVerificationState.Unverified, (await downloads.VerifyAsync(DownloadTestContext.Request("file.bin"), Ct)).State);
    }

    [Fact]
    public async Task OfflineConvenienceLoginReturnsTheExistingT05Identity()
    {
        using var directory = new DownloadTestContext();
        using var accounts = new AccountManager(directory.Root);
        var identity = await accounts.LoginOfflineAsync("Steve", Ct);
        Assert.Equal(AccountKind.Offline, identity.Account.Kind);
        Assert.Equal("5627dd98e6be3c21b8a8e92344183641", identity.Uuid);
        Assert.Equal("0", identity.AccessToken);
        Assert.Single((await accounts.GetAccountsAsync(Ct)).Accounts);
    }
}
