using System.Collections.Concurrent;
using System.Net;
using FMCore.Downloader;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.DownloadTestContext;

namespace FMCore.Tests.Unit;

public sealed class DownloadManagerTests
{
    [Theory]
    [InlineData(DownloadHashAlgorithm.Sha1)]
    [InlineData(DownloadHashAlgorithm.Sha256)]
    [InlineData(DownloadHashAlgorithm.Sha512)]
    public async Task DownloadsAndVerifiesEachHashAlgorithm(DownloadHashAlgorithm algorithm)
    {
        using var context = new DownloadTestContext();
        using var http = new HttpClient(new DelegateHandler((_, _) => Task.FromResult(Response("verified"))));
        var manager = new DownloadManager(http, context.Root, FastOptions);
        var events = new ConcurrentQueue<DownloadProgress>();
        var result = await manager.DownloadAsync(Request(text: "verified", algorithm: algorithm), new InlineProgress<DownloadProgress>(events.Enqueue), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("verified", await File.ReadAllTextAsync(result.FullPath, cancellationToken: TestContext.Current.CancellationToken));
        Assert.False(result.WasSkipped);
        Assert.Equal(1, result.Attempts);
        Assert.Equal(DownloadState.Started, events.First().State);
        Assert.Equal(DownloadState.Completed, events.Last().State);
        Assert.Equal(8, events.Last().BytesReceived);
        Assert.Equal(8, events.Last().TotalBytes);
        context.AssertNoParts();
    }

    [Fact]
    public async Task SkipsExistingFileOnlyAfterVerification()
    {
        using var context = new DownloadTestContext();
        await File.WriteAllTextAsync(context.At("file.bin"), "verified", cancellationToken: TestContext.Current.CancellationToken);
        using var handler = new DelegateHandler((_, _) => throw new InvalidOperationException("Network must not be called."));
        using var http = new HttpClient(handler);
        var manager = new DownloadManager(http, context.Root, FastOptions);
        var result = await manager.DownloadAsync(Request("file.bin", "verified"), cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(result.WasSkipped);
        Assert.Equal(0, result.Attempts);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task ExistingUnverifiableFileIsDownloadedAgain()
    {
        using var context = new DownloadTestContext();
        await File.WriteAllTextAsync(context.At("file.bin"), "old", cancellationToken: TestContext.Current.CancellationToken);
        using var http = new HttpClient(new DelegateHandler((_, _) => Task.FromResult(Response("new"))));
        var result = await new DownloadManager(http, context.Root, FastOptions).DownloadAsync(Request("file.bin"), cancellationToken: TestContext.Current.CancellationToken);
        Assert.False(result.WasSkipped);
        Assert.Equal("new", await File.ReadAllTextAsync(result.FullPath, cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task HashFailurePreservesOldFileAndCleansTemporaryFile()
    {
        using var context = new DownloadTestContext();
        await File.WriteAllTextAsync(context.At("file.bin"), "old", cancellationToken: TestContext.Current.CancellationToken);
        using var http = new HttpClient(new DelegateHandler((_, _) => Task.FromResult(Response("bad"))));
        var manager = new DownloadManager(http, context.Root, FastOptions);
        var error = await Assert.ThrowsAsync<DownloadException>(() => manager.DownloadAsync(Request("file.bin", "new"), cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(DownloadErrorCode.HashMismatch, error.Code);
        Assert.Equal("old", await File.ReadAllTextAsync(context.At("file.bin"), cancellationToken: TestContext.Current.CancellationToken));
        context.AssertNoParts();
    }

    [Fact]
    public async Task RetryCanRecoverFromCorruptedContent()
    {
        using var context = new DownloadTestContext();
        var calls = 0;
        using var handler = new DelegateHandler((_, _) => Task.FromResult(Response(Interlocked.Increment(ref calls) == 1 ? "bad" : "new")));
        using var http = new HttpClient(handler);
        var manager = new DownloadManager(http, context.Root, FastOptions with { MaxRetries = 1 });
        var result = await manager.DownloadAsync(Request(text: "new"), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, result.Attempts);
        Assert.Equal(2, handler.Calls);
        Assert.Equal("new", await File.ReadAllTextAsync(result.FullPath, cancellationToken: TestContext.Current.CancellationToken));
        context.AssertNoParts();
    }

    [Theory]
    [InlineData(408)]
    [InlineData(429)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task RetriesTransientHttpErrors(int status)
    {
        using var context = new DownloadTestContext();
        var calls = 0;
        using var handler = new DelegateHandler((_, _) => Task.FromResult(Interlocked.Increment(ref calls) == 1 ? Response("error", (HttpStatusCode)status) : Response("ok")));
        using var http = new HttpClient(handler);
        var result = await new DownloadManager(http, context.Root, FastOptions with { MaxRetries = 1 }).DownloadAsync(Request(text: "ok"), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, result.Attempts);
        Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(206)]
    public async Task DoesNotRetryPermanentHttpErrorsOrUnexpectedPartialResponses(int status)
    {
        using var context = new DownloadTestContext();
        using var handler = new DelegateHandler((_, _) => Task.FromResult(Response("error", (HttpStatusCode)status)));
        using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<DownloadException>(() => new DownloadManager(http, context.Root, FastOptions with { MaxRetries = 3 }).DownloadAsync(Request(), cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(DownloadErrorCode.HttpError, error.Code);
        Assert.Equal((HttpStatusCode)status, error.StatusCode);
        Assert.Equal(1, handler.Calls);
        context.AssertNoParts();
    }

    [Fact]
    public async Task StopsAtConfiguredRetryLimit()
    {
        using var context = new DownloadTestContext();
        using var handler = new DelegateHandler((_, _) => throw new HttpRequestException("connection lost"));
        using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<DownloadException>(() => new DownloadManager(http, context.Root, FastOptions with { MaxRetries = 2 }).DownloadAsync(Request(), cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(DownloadErrorCode.NetworkError, error.Code);
        Assert.Equal(3, error.Attempt);
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public async Task WholeAttemptTimeoutIncludesReadingTheBody()
    {
        using var context = new DownloadTestContext();
        using var handler = new DelegateHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new BlockingStream()) }));
        using var http = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan };
        var manager = new DownloadManager(http, context.Root, FastOptions with { MaxRetries = 1, AttemptTimeout = TimeSpan.FromMilliseconds(150) });
        var error = await Assert.ThrowsAsync<DownloadException>(() => manager.DownloadAsync(Request(), cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(DownloadErrorCode.Timeout, error.Code);
        Assert.Equal(2, handler.Calls);
        context.AssertNoParts();
    }

    [Fact]
    public async Task CancellationDuringTransferPreservesOldFileAndDoesNotRetry()
    {
        using var context = new DownloadTestContext();
        await File.WriteAllTextAsync(context.At("file.bin"), "old", cancellationToken: TestContext.Current.CancellationToken);
        var stream = new BlockingStream();
        using var handler = new DelegateHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) }));
        using var http = new HttpClient(handler);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var manager = new DownloadManager(http, context.Root, FastOptions with { MaxRetries = 3 });
        var download = manager.DownloadAsync(Request("file.bin"), cancellationToken: cancel.Token);
        await stream.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
        await cancel.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => download);
        Assert.Equal(1, handler.Calls);
        Assert.Equal("old", await File.ReadAllTextAsync(context.At("file.bin"), cancellationToken: TestContext.Current.CancellationToken));
        context.AssertNoParts();
    }

    [Fact]
    public async Task CancellationInterruptsRetryAfterDelay()
    {
        using var context = new DownloadTestContext();
        using var handler = new DelegateHandler((_, _) =>
        {
            var response = Response("busy", HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new(TimeSpan.FromSeconds(30));
            return Task.FromResult(response);
        });
        using var http = new HttpClient(handler);
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var retrying = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var manager = new DownloadManager(http, context.Root, new() { MaxRetries = 2 });
        var download = manager.DownloadAsync(Request(), new InlineProgress<DownloadProgress>(p => { if (p.State == DownloadState.Retrying) retrying.TrySetResult(); }), cancel.Token);
        await retrying.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
        await cancel.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => download.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task RejectsDuplicateBatchDestinationsBeforeAnyNetworkRequest()
    {
        using var context = new DownloadTestContext();
        using var handler = new DelegateHandler((_, _) => throw new InvalidOperationException("No request expected."));
        using var http = new HttpClient(handler);
        var error = await Assert.ThrowsAsync<DownloadException>(() => new DownloadManager(http, context.Root).DownloadManyAsync([Request("a/b"), Request("a\\b")], cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(DownloadErrorCode.InvalidRequest, error.Code);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task ConcurrencyLimitAppliesAcrossSimultaneousBatchesAndSingleCalls()
    {
        using var context = new DownloadTestContext();
        var active = 0;
        var maximum = 0;
        var saturated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var handler = new DelegateHandler(async (_, ct) =>
        {
            var current = Interlocked.Increment(ref active);
            int observed;
            do { observed = Volatile.Read(ref maximum); } while (current > observed && Interlocked.CompareExchange(ref maximum, current, observed) != observed);
            if (current == 2) saturated.TrySetResult();
            try { await release.Task.WaitAsync(ct); return Response("ok"); }
            finally { Interlocked.Decrement(ref active); }
        });
        using var http = new HttpClient(handler);
        var manager = new DownloadManager(http, context.Root, FastOptions with { MaxConcurrency = 2 });
        var first = manager.DownloadManyAsync(Enumerable.Range(0, 4).Select(i => Request($"first-{i}", "ok")), cancellationToken: TestContext.Current.CancellationToken);
        var second = manager.DownloadManyAsync(Enumerable.Range(0, 4).Select(i => Request($"second-{i}", "ok")), cancellationToken: TestContext.Current.CancellationToken);
        var single = manager.DownloadAsync(Request("single", "ok"), cancellationToken: TestContext.Current.CancellationToken);
        try { await saturated.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken); Assert.Equal(2, Volatile.Read(ref maximum)); }
        finally { release.TrySetResult(); }
        await Task.WhenAll(first, second, single);
        Assert.Equal(2, maximum);
        Assert.Equal(9, handler.Calls);
        Assert.Equal("first-0", Path.GetFileName((await first)[0].FullPath));
        context.AssertNoParts();
    }

    [Fact]
    public async Task ConcurrentTargetConflictDoesNotAffectFirstDownload()
    {
        using var context = new DownloadTestContext();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = new HttpClient(new DelegateHandler(async (_, ct) => { entered.TrySetResult(); await release.Task.WaitAsync(ct); return Response("ok"); }));
        var manager = new DownloadManager(http, context.Root, FastOptions);
        var first = manager.DownloadAsync(Request("same", "ok"), cancellationToken: TestContext.Current.CancellationToken);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken: TestContext.Current.CancellationToken);
            var error = await Assert.ThrowsAsync<DownloadException>(() => manager.DownloadAsync(Request("same", "ok"), cancellationToken: TestContext.Current.CancellationToken));
            Assert.Equal(DownloadErrorCode.DestinationBusy, error.Code);
        }
        finally { release.TrySetResult(); }
        await first;
    }

    [Fact]
    public async Task AcceptsVerifiedEmptyFilesAndSizeOnlyVerification()
    {
        using var context = new DownloadTestContext();
        using var handler = new DelegateHandler((_, _) => Task.FromResult(Response("")));
        using var http = new HttpClient(handler);
        var manager = new DownloadManager(http, context.Root, FastOptions);
        await manager.DownloadAsync(Request("empty", ""), cancellationToken: TestContext.Current.CancellationToken);
        var skipped = await manager.DownloadAsync(Request("empty") with { ExpectedSize = 0 }, cancellationToken: TestContext.Current.CancellationToken);
        Assert.True(skipped.WasSkipped);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task ConcurrencySlotRemainsOccupiedWhileStreamingResponseBody()
    {
        using var context = new DownloadTestContext();
        var stream = new BlockingStream();
        var calls = 0;
        using var handler = new DelegateHandler((_, _) => Task.FromResult(Interlocked.Increment(ref calls) == 1
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(stream) }
            : Response("ok")));
        using var http = new HttpClient(handler);
        var manager = new DownloadManager(http, context.Root, FastOptions with { MaxConcurrency = 1 });
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var first = manager.DownloadAsync(Request("first"), cancellationToken: cancel.Token);
        await stream.Started.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var second = manager.DownloadAsync(Request("second", "ok"), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, handler.Calls);
        await cancel.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        await second;
        Assert.Equal(2, handler.Calls);
        context.AssertNoParts();
    }

    [Fact]
    public async Task SupportsSpacesAndNonAsciiFilenames()
    {
        using var context = new DownloadTestContext();
        using var http = new HttpClient(new DelegateHandler((_, _) => Task.FromResult(Response("ok"))));
        var result = await new DownloadManager(http, context.Root, FastOptions).DownloadAsync(
            Request("资源 文件夹/hello world+测试.bin", "ok"), cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("ok", await File.ReadAllTextAsync(result.FullPath, TestContext.Current.CancellationToken));
    }
}
