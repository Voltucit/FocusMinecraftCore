using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using FMCore.Downloader;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.DownloadTestContext;

namespace FMCore.Tests.Integration;

/// <summary>Real HTTP transport on loopback only; no public network services are contacted.</summary>
public sealed class DownloadHttpTests
{
    [Fact]
    public async Task DownloadsChunkedResponseWithoutContentLength()
    {
        using var context = new DownloadTestContext();
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        var server = ServeAsync(listener, ["HTTP/1.1 200 OK\r\nTransfer-Encoding: chunked\r\nConnection: close\r\n\r\n3\r\nabc\r\n3\r\ndef\r\n0\r\n\r\n"], timeout.Token);
        using var http = new HttpClient(new SocketsHttpHandler { UseProxy = false });
        var manager = new DownloadManager(http, context.Root, FastOptions);
        var events = new ConcurrentQueue<DownloadProgress>();
        var request = Request(text: "abcdef") with { Source = Address(listener), ExpectedSize = null };
        var result = await manager.DownloadAsync(request, new InlineProgress<DownloadProgress>(events.Enqueue), timeout.Token);
        await server;
        Assert.Equal("abcdef", await File.ReadAllTextAsync(result.FullPath, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(6, result.Size);
        Assert.All(events, progress => Assert.Null(progress.TotalBytes));
        context.AssertNoParts();
    }

    [Fact]
    public async Task RetriesAnActuallyDroppedHttpConnection()
    {
        using var context = new DownloadTestContext();
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        var server = ServeAsync(listener,
            ["HTTP/1.1 200 OK\r\nContent-Length: 6\r\nConnection: close\r\n\r\nabc",
             "HTTP/1.1 200 OK\r\nContent-Length: 6\r\nConnection: close\r\n\r\nabcdef"], timeout.Token);
        using var http = new HttpClient(new SocketsHttpHandler { UseProxy = false });
        var manager = new DownloadManager(http, context.Root, FastOptions with { MaxRetries = 1 });
        var result = await manager.DownloadAsync(Request(text: "abcdef") with { Source = Address(listener) }, cancellationToken: timeout.Token);
        await server;
        Assert.Equal(2, result.Attempts);
        Assert.Equal("abcdef", await File.ReadAllTextAsync(result.FullPath, cancellationToken: TestContext.Current.CancellationToken));
        context.AssertNoParts();
    }

    private static Uri Address(TcpListener listener) => new($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/file");

    private static async Task ServeAsync(TcpListener listener, string[] responses, CancellationToken ct)
    {
        foreach (var response in responses)
        {
            using var client = await listener.AcceptTcpClientAsync(ct);
            await using var stream = client.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            while (await reader.ReadLineAsync(ct) is { Length: > 0 }) { }
            await stream.WriteAsync(Encoding.ASCII.GetBytes(response), ct);
            await stream.FlushAsync(ct);
        }
    }
}
