using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;

namespace FMCore.Downloads;

public static partial class FileDownloader
{
    // 文件并发和分段共享连接预算，不把 16 个文件扩大成无限制的连接数。
    private static readonly SemaphoreSlim NetworkSlots = new(32, 32);
    private static readonly ConcurrentDictionary<string, SourceObservation> SourceObservations = new(StringComparer.OrdinalIgnoreCase);
    private sealed record SourceObservation(double Speed, DateTimeOffset? FailedAt);
    private sealed class DownloadStalledException(string message, Exception? inner = null) : TimeoutException(message, inner);
    private sealed class SourceContentException(string message) : IOException(message);
    private sealed class RangeUnavailableException : IOException { }

    private sealed class ResponseLease(HttpResponseMessage response) : IDisposable
    {
        public HttpResponseMessage Response { get; } = response;
        public void Dispose() { Response.Dispose(); NetworkSlots.Release(); }
    }

    private static async Task<ResponseLease> OpenResponseAsync(DownloadRequest request, HttpRequestMessage message, CancellationToken token)
    {
        await NetworkSlots.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (_userAgent is { } agent) message.Headers.TryAddWithoutValidation("User-Agent", agent);
            message.Headers.TryAddWithoutValidation("Accept-Encoding", "identity");
            using var headers = CancellationTokenSource.CreateLinkedTokenSource(token);
            headers.CancelAfter(request.HeadersTimeout);
            try
            {
                return new ResponseLease(await Client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, headers.Token).ConfigureAwait(false));
            }
            catch (OperationCanceledException error) when (!token.IsCancellationRequested)
            { throw new DownloadStalledException("服务器连接或返回响应头超时。", error); }
        }
        catch { NetworkSlots.Release(); throw; }
    }

    private static async ValueTask<int> ReadNetworkAsync(Stream source, Memory<byte> buffer, DownloadRequest request, CancellationToken token)
    {
        using var idle = CancellationTokenSource.CreateLinkedTokenSource(token);
        idle.CancelAfter(request.IdleTimeout);
        try { return await source.ReadAsync(buffer, idle.Token).ConfigureAwait(false); }
        catch (OperationCanceledException error) when (!token.IsCancellationRequested && idle.IsCancellationRequested)
        { throw new DownloadStalledException("下载连接持续未收到数据。", error); }
    }

    private static string[] SourceCandidates(DownloadRequest request)
    {
        var addresses = new[] { request.Url }.Concat(request.FallbackUrl is { } fallback ? [fallback] : [])
            .Concat(request.FallbackUrls).Distinct(StringComparer.Ordinal).ToArray();
        if (!request.UseSourcePerformance) return addresses;
        // 未测过的来源保持调用方顺序；近期网络故障的来源排后，成功来源按实际吞吐排序。
        return addresses.Select((address, index) => (Address: address, Index: index,
                Observation: SourceObservations.GetValueOrDefault(new Uri(address).Host)))
            .OrderBy(item => item.Observation?.FailedAt is { } failed && DateTimeOffset.UtcNow - failed < TimeSpan.FromMinutes(2) ? 1 : 0)
            .ThenByDescending(item => item.Observation?.Speed ?? 0)
            .ThenBy(item => item.Index).Select(item => item.Address).ToArray();
    }

    private static void ObserveSource(string address, long size, TimeSpan elapsed, Exception? error = null)
    {
        string host = new Uri(address).Host;
        if (error == null)
        {
            double speed = size / Math.Max(elapsed.TotalSeconds, 0.001);
            SourceObservations.AddOrUpdate(host, new SourceObservation(speed, null), (_, old) => new(old.Speed > 0 ? old.Speed * 0.5 + speed * 0.5 : speed, null));
        }
        else if (error is DownloadStalledException || IsTransientDownloadError(error))
            SourceObservations.AddOrUpdate(host, new SourceObservation(0, DateTimeOffset.UtcNow), (_, old) => old with { FailedAt = DateTimeOffset.UtcNow });
    }
}
