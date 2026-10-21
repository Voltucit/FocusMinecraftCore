using System.Text.Json.Nodes;
using FMCore.Downloader;
using FMCore.Versions;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.VersionTestContext;

namespace FMCore.Tests.Integration;

public sealed class VersionCacheTests
{
    [Fact]
    public async Task FetchesOfficialListAndVersionOnceThenSupportsOfflineRead()
    {
        var profile = Fixture("1.21.1.json");
        var manifest = Manifest(("1.21.1", profile));
        using var context = new VersionTestContext((request, _) => Task.FromResult(Response(request.RequestUri!.Host == "piston-meta.mojang.com" ? manifest : profile)));
        var first = await context.Manager.GetVersionAsync("1.21.1", cancellationToken: Ct);
        var second = await context.Manager.GetVersionAsync("1.21.1", VersionCachePolicy.CacheOnly, Ct);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(2, context.Handler.Calls);
        Assert.True(File.Exists(context.Manager.GetMetadataPath("1.21.1")));
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task ConcurrentManifestCallsShareValidatedCache()
    {
        var manifest = Manifest(("test", "{\"id\":\"test\"}"));
        using var context = new VersionTestContext((_, _) => Task.FromResult(Response(manifest)));
        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => context.Manager.GetManifestAsync(cancellationToken: Ct)));
        Assert.All(results, m => Assert.Equal("test", m.LatestRelease));
        Assert.Equal(1, context.Handler.Calls);
    }

    [Fact]
    public async Task RefreshFetchesNewManifestAndProfileInsteadOfSkippingExistingFiles()
    {
        var oldProfile = "{\"id\":\"test\",\"mainClass\":\"old.Main\"}";
        var newProfile = "{\"id\":\"test\",\"mainClass\":\"new.Main\"}";
        var refresh = false;
        using var context = new VersionTestContext((request, _) => Task.FromResult(Response(request.RequestUri!.Host == "piston-meta.mojang.com"
            ? Manifest(("test", refresh ? newProfile : oldProfile)) : refresh ? newProfile : oldProfile)));
        Assert.Equal("old.Main", (await context.Manager.GetVersionAsync("test", cancellationToken: Ct)).MainClass);
        refresh = true;
        Assert.Equal("new.Main", (await context.Manager.GetVersionAsync("test", VersionCachePolicy.Refresh, Ct)).MainClass);
        Assert.Equal(4, context.Handler.Calls);
    }

    [Fact]
    public async Task FailedManifestValidationPreservesPreviousGoodCache()
    {
        var calls = 0;
        var manifest = Manifest(("test", "{\"id\":\"test\"}"));
        using var context = new VersionTestContext((_, _) => Task.FromResult(Response(Interlocked.Increment(ref calls) == 1 ? manifest : "not-json")));
        await context.Manager.GetManifestAsync(cancellationToken: Ct);
        var error = await Assert.ThrowsAsync<VersionException>(() => context.Manager.GetManifestAsync(VersionCachePolicy.Refresh, Ct));
        Assert.Equal(VersionErrorCode.InvalidMetadata, error.Code);
        Assert.Equal("test", (await context.Manager.GetManifestAsync(VersionCachePolicy.CacheOnly, Ct)).LatestRelease);
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task CorruptProfileIsRejectedOfflineAndRepairedOnline()
    {
        const string profile = "{\"id\":\"test\"}";
        using var context = new VersionTestContext((request, _) => Task.FromResult(Response(request.RequestUri!.Host == "piston-meta.mojang.com" ? Manifest(("test", profile)) : profile)));
        await context.Manager.GetVersionAsync("test", cancellationToken: Ct);
        await File.AppendAllTextAsync(context.Manager.GetMetadataPath("test"), " ", Ct);
        var error = await Assert.ThrowsAsync<VersionException>(() => context.Manager.GetVersionAsync("test", VersionCachePolicy.CacheOnly, Ct));
        Assert.Equal(VersionErrorCode.InvalidMetadata, error.Code);
        Assert.Equal("test", (await context.Manager.GetVersionAsync("test", cancellationToken: Ct)).Id);
        Assert.Equal(3, context.Handler.Calls);
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task RejectsProfileWithWrongOfficialHash()
    {
        const string expected = "{\"id\":\"test\"}";
        using var context = new VersionTestContext((request, _) => Task.FromResult(Response(request.RequestUri!.Host == "piston-meta.mojang.com" ? Manifest(("test", expected)) : "{\"id\":\"wrong\"}")));
        var error = await Assert.ThrowsAsync<DownloadException>(() => context.Manager.GetVersionAsync("test", cancellationToken: Ct));
        Assert.Equal(DownloadErrorCode.HashMismatch, error.Code);
        Assert.False(File.Exists(context.Manager.GetMetadataPath("test")));
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task ValidHashDoesNotPermitMismatchedProfileIdentity()
    {
        const string wrongId = "{\"id\":\"other\"}";
        using var context = new VersionTestContext((request, _) => Task.FromResult(Response(request.RequestUri!.Host == "piston-meta.mojang.com" ? Manifest(("test", wrongId)) : wrongId)));
        var error = await Assert.ThrowsAsync<VersionException>(() => context.Manager.GetVersionAsync("test", cancellationToken: Ct));
        Assert.Equal(VersionErrorCode.InvalidMetadata, error.Code);
        Assert.False(File.Exists(context.Manager.GetMetadataPath("test")));
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task CacheOnlyNeverUsesNetworkAndMissingVersionHasStructuredError()
    {
        using var context = new VersionTestContext();
        var error = await Assert.ThrowsAsync<VersionException>(() => context.Manager.GetVersionAsync("missing", VersionCachePolicy.CacheOnly, Ct));
        Assert.Equal(VersionErrorCode.NotCached, error.Code);
        Assert.Equal(0, context.Handler.Calls);
        using var online = new VersionTestContext((_, _) => Task.FromResult(Response(Manifest(("test", "{\"id\":\"test\"}")))));
        var absent = await Assert.ThrowsAsync<VersionException>(() => online.Manager.GetVersionAsync("missing", cancellationToken: Ct));
        Assert.Equal(VersionErrorCode.NotFound, absent.Code);
    }

    [Fact]
    public async Task ImportedLocalProfilesRemainAvailableAcrossManagerInstances()
    {
        using var context = new VersionTestContext();
        await context.SaveAsync(Fixture("inherit-base.json"));
        using var http = new HttpClient(new DelegateHandler((_, _) => throw new InvalidOperationException("No network expected.")));
        var manager = new VersionManager(new DownloadManager(http, context.Root));
        Assert.Equal("test-base", (await manager.GetVersionAsync("test-base", VersionCachePolicy.CacheOnly, Ct)).Id);
    }

    [Fact]
    public async Task CancellationBeforeStartDoesNotAccessTheNetwork()
    {
        using var context = new VersionTestContext();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        await cancel.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Manager.GetManifestAsync(cancellationToken: cancel.Token));
        Assert.Equal(0, context.Handler.Calls);
    }

    [Fact]
    public async Task CancellationDuringRefreshPreservesValidManifestAndCleansIncomingFiles()
    {
        var stream = new BlockingStream();
        var calls = 0;
        using var context = new VersionTestContext((_, _) => Task.FromResult(Interlocked.Increment(ref calls) == 1
            ? Response(Manifest(("test", "{\"id\":\"test\"}")))
            : new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StreamContent(stream) }));
        await context.Manager.GetManifestAsync(cancellationToken: Ct);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        var refresh = context.Manager.GetManifestAsync(VersionCachePolicy.Refresh, cancel.Token);
        await stream.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), Ct);
        await cancel.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => refresh);
        Assert.Equal("test", (await context.Manager.GetManifestAsync(VersionCachePolicy.CacheOnly, Ct)).LatestRelease);
        context.AssertNoTemporaryFiles();
    }
}
