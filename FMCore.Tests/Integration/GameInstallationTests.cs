using System.Collections.Concurrent;
using System.Text;
using FMCore.Assets;
using FMCore.Downloader;
using FMCore.Installer;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.InstallationTestContext;

namespace FMCore.Tests.Integration;

public sealed class GameInstallationTests
{
    [Fact]
    public async Task InstallsEveryComponentAndRepeatsWithoutNetwork()
    {
        using var context = new InstallationTestContext();
        await context.SeedAsync();
        var events = new ConcurrentQueue<InstallationProgress>();
        var first = await context.Installer.InstallAsync("fixture", Options, new InlineProgress<InstallationProgress>(events.Enqueue), Ct);
        Assert.Equal(7, first.DownloadedFiles);
        Assert.Equal(0, first.SkippedFiles);
        Assert.Equal("client bytes", await File.ReadAllTextAsync(first.ClientJarPath, Ct));
        Assert.Equal("native bytes", await File.ReadAllTextAsync(Path.Combine(first.NativeDirectory, "fixture.dll"), Ct));
        Assert.False(Directory.Exists(Path.Combine(first.NativeDirectory, "META-INF")));
        Assert.Equal("log config", await File.ReadAllTextAsync(first.LoggingPath!, Ct));
        Assert.Equal(2, first.ClasspathEntries.Count);
        Assert.All(first.ClasspathEntries, path => Assert.True(File.Exists(path)));
        Assert.False(context.Requests.ContainsKey("/linux.jar"));
        Assert.Equal(2, context.Requests.Keys.Count(p => p.Length == 44));
        Assert.Equal(Enum.GetValues<InstallationStage>().Where(s => s != InstallationStage.Verification), events.Select(e => e.Stage).Distinct());
        Assert.Contains(events, e => e.Download?.State == DownloadState.Downloading);
        Assert.Equal(InstallationStage.Completed, events.Last().Stage);
        context.Override = (_, _) => throw new InvalidOperationException("A repeated verified installation must not use HTTP.");
        var second = await context.Installer.InstallAsync("fixture", Options, cancellationToken: Ct);
        Assert.Equal(0, second.DownloadedFiles);
        Assert.Equal(7, second.SkippedFiles);
        Assert.Equal(0, second.DownloadedBytes);
        context.AssertNoTemporaryFiles();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task BuildsLegacyVirtualAndGameResourcesLayouts(bool virtualAssets, bool resources)
    {
        using var context = new InstallationTestContext();
        await context.SeedAsync(virtualAssets, resources);
        var options = Options with { GameDirectory = "instances/old-game" };
        var result = await context.Installer.InstallAsync("fixture", options, cancellationToken: Ct);
        if (virtualAssets)
        {
            Assert.Equal(context.At("assets/virtual/fixture-assets"), result.Assets.GameAssetsDirectory);
            Assert.Equal("fixture asset", await File.ReadAllTextAsync(context.At("assets/virtual/fixture-assets/sounds/first.ogg"), Ct));
        }
        if (resources)
        {
            Assert.Equal(context.At("instances/old-game/resources"), result.Assets.ResourcesDirectory);
            if (!virtualAssets) Assert.Equal(result.Assets.ResourcesDirectory, result.Assets.GameAssetsDirectory);
            Assert.Equal("fixture asset", await File.ReadAllTextAsync(context.At("instances/old-game/resources/sounds/alias.ogg"), Ct));
        }
        Assert.False(Directory.Exists(context.At("resources")));
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task ReinstallsOnlyDamagedDownloadsAndRepairsDerivedFiles()
    {
        using var context = new InstallationTestContext();
        await context.SeedAsync(true, true);
        var first = await context.Installer.InstallAsync("fixture", Options, cancellationToken: Ct);
        var before = context.Requests.Values.Sum();
        // Same length, different bytes: size alone must not make a hashed file reusable.
        await File.WriteAllTextAsync(first.ClientJarPath, "xxxxxx xxxxx", Ct);
        await File.WriteAllTextAsync(context.At("assets/virtual/fixture-assets/sounds/first.ogg"), "bad", Ct);
        await File.WriteAllTextAsync(Path.Combine(first.NativeDirectory, "stale.dll"), "old", Ct);
        var second = await context.Installer.InstallAsync("fixture", Options, cancellationToken: Ct);
        Assert.Equal(1, second.DownloadedFiles);
        Assert.Equal(6, second.SkippedFiles);
        Assert.Equal(before + 1, context.Requests.Values.Sum());
        Assert.Equal("client bytes", await File.ReadAllTextAsync(first.ClientJarPath, Ct));
        Assert.Equal("fixture asset", await File.ReadAllTextAsync(context.At("assets/virtual/fixture-assets/sounds/first.ogg"), Ct));
        Assert.False(File.Exists(Path.Combine(second.NativeDirectory, "stale.dll")));
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task CancellationPreservesCompletedFilesAndRetrySkipsThem()
    {
        using var context = new InstallationTestContext();
        await context.SeedAsync();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var events = new ConcurrentQueue<InstallationProgress>();
        var progress = new InlineProgress<InstallationProgress>(p =>
        {
            events.Enqueue(p);
            if (p.Stage == InstallationStage.Libraries && p.CompletedFiles == p.TotalFiles && p.TotalFiles > 0) cancel.Cancel();
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Installer.InstallAsync("fixture", Options, progress, cancel.Token));
        Assert.DoesNotContain(events, e => e.Stage == InstallationStage.Completed);
        Assert.True(File.Exists(context.At("versions/fixture/fixture.jar")));
        var result = await context.Installer.InstallAsync("fixture", Options, cancellationToken: Ct);
        Assert.Equal(3, result.SkippedFiles);
        Assert.Equal(4, result.DownloadedFiles);
        Assert.Equal(1, context.Requests["/client.jar"]);
        Assert.Equal(1, context.Requests["/both.jar"]);
        Assert.Equal(1, context.Requests["/natives.jar"]);
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task BrokenAssetTransferCanBeRetriedWithoutFetchingCompletedComponents()
    {
        using var context = new InstallationTestContext();
        await context.SeedAsync();
        context.Override = (request, _) => Task.FromResult(request.RequestUri!.Host == "resources.download.minecraft.net"
            ? new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes("corrupt")) }
            : context.Response(request.RequestUri.AbsolutePath));
        await Assert.ThrowsAsync<DownloadException>(() => context.Installer.InstallAsync("fixture", Options, cancellationToken: Ct));
        context.Override = null;
        var result = await context.Installer.InstallAsync("fixture", Options, cancellationToken: Ct);
        Assert.True(result.SkippedFiles >= 4);
        Assert.Equal(1, context.Requests["/client.jar"]);
        Assert.Equal(1, context.Requests["/index.json"]);
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task SharedRootLeaseRejectsOverlappingInstallations()
    {
        using var context = new InstallationTestContext();
        await context.SeedAsync();
        Directory.CreateDirectory(context.At(".cache"));
        using var lease = new FileStream(context.At(".cache/install.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var error = await Assert.ThrowsAsync<InstallationException>(() => context.Installer.InstallAsync("fixture", Options, cancellationToken: Ct));
        Assert.Equal(InstallationErrorCode.InstallationBusy, error.Code);
        Assert.Empty(context.Requests);
    }

    [Fact]
    public async Task CorruptSourceObjectCannotReplacePreviouslyGoodLegacyCopy()
    {
        using var context = new InstallationTestContext();
        await context.SeedAsync(true);
        await context.Installer.InstallAsync("fixture", Options, cancellationToken: Ct);
        var assets = new AssetManager(context.Downloads);
        var info = (await context.Versions.GetVersionAsync("fixture", cancellationToken: Ct)).AssetIndex!;
        var index = (await assets.GetIndexAsync(info, cancellationToken: Ct)).Index;
        var asset = index.Objects.First(o => o.Size != 0);
        await File.WriteAllTextAsync(context.At(asset.RelativePath), "broken", Ct);
        var target = context.At("assets/virtual/fixture-assets/" + asset.Name);
        await File.WriteAllTextAsync(target, "keep this previous file", Ct);
        var error = await Assert.ThrowsAsync<AssetException>(() => assets.MaterializeAsync(index, info.Id, cancellationToken: Ct));
        Assert.Equal(AssetErrorCode.HashMismatch, error.Code);
        Assert.Equal("keep this previous file", await File.ReadAllTextAsync(target, Ct));
        context.AssertNoTemporaryFiles();
    }
}
