using System.Collections.Concurrent;
using System.Net;
using FMCore.Downloader;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.DownloadTestContext;

namespace FMCore.Tests.Unit;

public sealed class DownloadFailureTests
{
    [Theory]
    [InlineData(3, 4, "bad")]
    [InlineData(4, 4, "bad")]
    [InlineData(2, 2, "bad")]
    public async Task RejectsHeaderMismatchShortBodyAndOversizedBody(long expected, long declared, string body)
    {
        using var context = new DownloadTestContext();
        await File.WriteAllTextAsync(context.At("file"), "original", cancellationToken: TestContext.Current.CancellationToken);
        using var http = new HttpClient(new DelegateHandler((_, _) =>
        {
            var response = Response(body);
            response.Content.Headers.ContentLength = declared;
            return Task.FromResult(response);
        }));
        var manager = new DownloadManager(http, context.Root, FastOptions);
        var error = await Assert.ThrowsAsync<DownloadException>(() => manager.DownloadAsync(Request("file") with { ExpectedSize = expected }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(DownloadErrorCode.SizeMismatch, error.Code);
        Assert.Equal("original", await File.ReadAllTextAsync(context.At("file"), cancellationToken: TestContext.Current.CancellationToken));
        context.AssertNoParts();
    }

    [Fact]
    public async Task LocalIoFailureDoesNotRetryOrDamageTargetDirectory()
    {
        using var context = new DownloadTestContext();
        Directory.CreateDirectory(context.At("target"));
        await File.WriteAllTextAsync(context.At("target/user.txt"), "keep", cancellationToken: TestContext.Current.CancellationToken);
        using var handler = new DelegateHandler((_, _) => Task.FromResult(Response("new")));
        using var http = new HttpClient(handler);
        var manager = new DownloadManager(http, context.Root, FastOptions with { MaxRetries = 3 });
        var error = await Assert.ThrowsAsync<DownloadException>(() => manager.DownloadAsync(Request("target", "new"), cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(DownloadErrorCode.IoError, error.Code);
        Assert.Equal(1, handler.Calls);
        Assert.Equal("keep", await File.ReadAllTextAsync(context.At("target/user.txt"), cancellationToken: TestContext.Current.CancellationToken));
        context.AssertNoParts();
    }

    [Fact]
    public async Task CancellationOfQueuedRequestReleasesDestinationReservation()
    {
        using var context = new DownloadTestContext();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new DelegateHandler(async (request, ct) =>
        {
            if (request.RequestUri!.AbsolutePath == "/first") { entered.TrySetResult(); await release.Task.WaitAsync(ct); }
            return Response("ok");
        });
        using var http = new HttpClient(handler);
        var manager = new DownloadManager(http, context.Root, FastOptions with { MaxConcurrency = 1 });
        var first = manager.DownloadAsync(Request("first", "ok") with { Source = new("https://downloads.example.test/first") }, cancellationToken: TestContext.Current.CancellationToken);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
            using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            var queued = manager.DownloadAsync(Request("queued", "ok"), cancellationToken: cancel.Token);
            await cancel.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => queued);
            Assert.Equal(1, handler.Calls);
        }
        finally { release.TrySetResult(); }
        await first;
        var result = await manager.DownloadAsync(Request("queued", "ok"), cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result.WasSkipped);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task RetryingProgressResetsBytesAndCompletesAfterCommit()
    {
        using var context = new DownloadTestContext();
        var calls = 0;
        using var http = new HttpClient(new DelegateHandler((_, _) => Task.FromResult(Response(Interlocked.Increment(ref calls) == 1 ? "bad" : "new"))));
        var events = new ConcurrentQueue<DownloadProgress>();
        var manager = new DownloadManager(http, context.Root, FastOptions with { MaxRetries = 1 });
        await manager.DownloadAsync(Request("target", "new"), new InlineProgress<DownloadProgress>(p =>
        {
            if (p.State == DownloadState.Completed) Assert.Equal("new", File.ReadAllText(context.At("target")));
            events.Enqueue(p);
        }), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Contains(events, p => p.State == DownloadState.Retrying && p.Attempt == 1);
        Assert.Contains(events, p => p.State == DownloadState.Started && p.Attempt == 2 && p.BytesReceived == 0);
        Assert.Equal(2, events.Last().Attempt);
    }

    [Fact]
    public async Task BadExistingHashIsReplacedEvenIfTheSizeMatches()
    {
        using var context = new DownloadTestContext();
        await File.WriteAllTextAsync(context.At("target"), "bad", cancellationToken: TestContext.Current.CancellationToken);
        using var http = new HttpClient(new DelegateHandler((_, _) => Task.FromResult(Response("new"))));
        var result = await new DownloadManager(http, context.Root, FastOptions).DownloadAsync(Request("target", "new"), cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result.WasSkipped);
        Assert.Equal("new", await File.ReadAllTextAsync(result.FullPath, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CallerStillOwnsTheInjectedHttpClient()
    {
        using var context = new DownloadTestContext();
        using var http = new HttpClient(new DelegateHandler((_, _) => Task.FromResult(Response("ok"))));
        await new DownloadManager(http, context.Root, FastOptions).DownloadAsync(Request("target", "ok"), cancellationToken: TestContext.Current.CancellationToken);
        using var response = await http.GetAsync(Source, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(1, -1)]
    [InlineData(65, 0)]
    [InlineData(1, 11)]
    public void RejectsInvalidConcurrencyAndRetryOptions(int concurrency, int retries)
    {
        using var context = new DownloadTestContext();
        using var http = new HttpClient();
        var error = Assert.Throws<DownloadException>(() => new DownloadManager(http, context.Root, new() { MaxConcurrency = concurrency, MaxRetries = retries }));
        Assert.Equal(DownloadErrorCode.InvalidRequest, error.Code);
    }

    [Fact]
    public async Task AlreadyCancelledTokenDoesNotMakeRequests()
    {
        using var context = new DownloadTestContext();
        using var handler = new DelegateHandler((_, _) => throw new InvalidOperationException("No request expected."));
        using var http = new HttpClient(handler);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        await cancel.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new DownloadManager(http, context.Root).DownloadAsync(Request(), cancellationToken: cancel.Token));
        Assert.Equal(0, handler.Calls);
    }
}
