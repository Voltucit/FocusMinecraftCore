using System.Net;
using System.Security.Cryptography;
using System.Text;
using FMCore.Downloader;

namespace FMCore.Tests.Fixtures;

internal sealed class DownloadTestContext : IDisposable
{
    internal string Root { get; } = Path.Combine(Path.GetTempPath(), "FMCore.Tests", Guid.NewGuid().ToString("N"));
    internal static readonly Uri Source = new("https://downloads.example.test/file");
    internal static DownloadOptions FastOptions => new() { MaxRetries = 0, RetryDelay = TimeSpan.Zero, MaxRetryDelay = TimeSpan.Zero };

    internal DownloadTestContext() => Directory.CreateDirectory(Root);
    internal string At(string name) => Path.Combine(Root, name);
    internal void AssertNoParts() => Assert.Empty(Directory.EnumerateFiles(Root, "*.part", SearchOption.AllDirectories));

    internal static HttpResponseMessage Response(string text, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new ByteArrayContent(Encoding.UTF8.GetBytes(text)) };

    internal static DownloadRequest Request(string path = "nested/file.bin", string? text = null,
        DownloadHashAlgorithm algorithm = DownloadHashAlgorithm.Sha256) => new(Source, path)
        {
            ExpectedHash = text == null ? null : Hash(text, algorithm),
            HashAlgorithm = algorithm,
            ExpectedSize = text == null ? null : Encoding.UTF8.GetByteCount(text)
        };

    internal static string Hash(string text, DownloadHashAlgorithm algorithm) => Convert.ToHexString(algorithm switch
    {
        DownloadHashAlgorithm.Sha1 => SHA1.HashData(Encoding.UTF8.GetBytes(text)),
        DownloadHashAlgorithm.Sha256 => SHA256.HashData(Encoding.UTF8.GetBytes(text)),
        DownloadHashAlgorithm.Sha512 => SHA512.HashData(Encoding.UTF8.GetBytes(text)),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    });

    public void Dispose()
    {
        var expectedParent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "FMCore.Tests"));
        if (Path.GetDirectoryName(Path.GetFullPath(Root)) != expectedParent) throw new InvalidOperationException("Unexpected test directory.");
        if (Directory.Exists(Root)) Directory.Delete(Root, true);
    }
}

internal sealed class DelegateHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    private int calls;
    internal int Calls => Volatile.Read(ref calls);
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref calls);
        return respond(request, cancellationToken);
    }
}

internal sealed class InlineProgress<T>(Action<T> report) : IProgress<T>
{
    public void Report(T value) => report(value);
}

internal sealed class BlockingStream : Stream
{
    private bool first = true;
    internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (first) { first = false; buffer.Span[0] = 42; return 1; }
        Started.TrySetResult();
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        return 0;
    }
    public override void Flush() => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
