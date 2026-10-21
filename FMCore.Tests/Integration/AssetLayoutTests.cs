using FMCore.Assets;
using FMCore.Tests.Fixtures;
using FMCore.Versions;
using static FMCore.Tests.Fixtures.InstallationTestContext;

namespace FMCore.Tests.Integration;

public sealed class AssetLayoutTests
{
    [Fact]
    public async Task EmptyLegacyIndexCreatesDirectoriesButCanceledCallDoesNot()
    {
        using var context = new InstallationTestContext();
        var assets = new AssetManager(context.Downloads);
        var index = new AssetIndex([], true, true);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        cancel.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => assets.MaterializeAsync(index, "legacy", "instance", cancellationToken: cancel.Token));
        Assert.False(Directory.Exists(context.At("assets")));
        var result = await assets.MaterializeAsync(index, "legacy", "instance", cancellationToken: Ct);
        Assert.True(Directory.Exists(result.GameAssetsDirectory));
        Assert.True(Directory.Exists(result.ResourcesDirectory));
    }

    [Fact]
    public async Task OversizedIndexIsRejectedBeforeDownload()
    {
        using var context = new InstallationTestContext();
        var info = new AssetIndexInfo("too-large", new(new Uri("https://fixture.example.test/large"), null, 17 * 1024 * 1024, null), null);
        Assert.Equal(AssetErrorCode.InvalidIndex, (await Assert.ThrowsAsync<AssetException>(() =>
            new AssetManager(context.Downloads).GetIndexAsync(info, cancellationToken: Ct))).Code);
        Assert.Empty(context.Requests);
    }

    [Fact]
    public async Task MissingObjectCannotBeMaterialized()
    {
        using var context = new InstallationTestContext();
        var index = new AssetIndex([new("sound.ogg", new string('a', 40), 10)], true, false);
        Assert.Equal(AssetErrorCode.MissingObject, (await Assert.ThrowsAsync<AssetException>(() =>
            new AssetManager(context.Downloads).MaterializeAsync(index, "legacy", cancellationToken: Ct))).Code);
        context.AssertNoTemporaryFiles();
    }
}
