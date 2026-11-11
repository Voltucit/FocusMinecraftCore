using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace FMCore.Downloads;

public static partial class FileDownloader
{
    private sealed record RangePlan(string? EntityTag);

    private static bool RejectsRange(HttpStatusCode status) => status is HttpStatusCode.OK or HttpStatusCode.BadRequest
        or HttpStatusCode.Forbidden or HttpStatusCode.MethodNotAllowed or HttpStatusCode.RequestedRangeNotSatisfiable
        or HttpStatusCode.NotImplemented;

    private static async Task<RangePlan?> ProbeRangeAsync(DownloadRequest request, string url, CancellationToken token)
    {
        if (request.MaximumSegments == 1 || request.Size < request.SegmentThreshold || request.Size < 8 * 1024 * 1024)
            return null;
        using var message = new HttpRequestMessage(HttpMethod.Get, url);
        message.Headers.Range = new RangeHeaderValue(0, 0);
        using var lease = await OpenResponseAsync(request, message, token).ConfigureAwait(false);
        var response = lease.Response;
        if (RejectsRange(response.StatusCode)) return null;
        EnsureSuccessfulResponse(response);
        var range = response.Content.Headers.ContentRange;
        if (response.StatusCode != HttpStatusCode.PartialContent || range?.From != 0 || range.To != 0
            || range.Length != request.Size || !range.Unit.Equals("bytes", StringComparison.OrdinalIgnoreCase)
            || response.Content.Headers.ContentLength is { } length && length != 1)
            return null;
        return new(response.Headers.ETag is { IsWeak: false } tag ? tag.ToString() : null);
    }

    private static async Task<string?> TryDownloadSegmentsAsync(DownloadRequest request, string url, string temporary,
        Action<DownloadProgress>? progress, Action created, CancellationToken token)
    {
        var plan = await ProbeRangeAsync(request, url, token).ConfigureAwait(false);
        if (plan == null) return null;
        int count = (int)Math.Min(request.MaximumSegments, request.Size / (4 * 1024 * 1024));
        long received = 0;
        var progressGate = new object();
        Directory.CreateDirectory(Path.GetDirectoryName(temporary)!);
        await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.Read,
            64 * 1024, FileOptions.Asynchronous | FileOptions.RandomAccess))
        {
            created();
            output.SetLength(request.Size);
            await Parallel.ForEachAsync(Enumerable.Range(0, count), new ParallelOptions
            { MaxDegreeOfParallelism = count, CancellationToken = token }, async (index, segmentToken) =>
            {
                long Start(int part) => request.Size / count * part + Math.Min(part, request.Size % count);
                long start = Start(index);
                long end = Start(index + 1) - 1;
                long position = start;
                int retries = 0;
                var buffer = new byte[64 * 1024];
                while (position <= end)
                {
                    segmentToken.ThrowIfCancellationRequested();
                    try
                    {
                        using var message = new HttpRequestMessage(HttpMethod.Get, url);
                        message.Headers.Range = new RangeHeaderValue(position, end);
                        if (plan.EntityTag != null) message.Headers.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue(plan.EntityTag));
                        using var lease = await OpenResponseAsync(request, message, segmentToken).ConfigureAwait(false);
                        var response = lease.Response;
                        if (RejectsRange(response.StatusCode)) throw new RangeUnavailableException();
                        EnsureSuccessfulResponse(response);
                        var range = response.Content.Headers.ContentRange;
                        if (response.StatusCode != HttpStatusCode.PartialContent || range?.From != position || range.To != end
                            || range.Length != request.Size || !range.Unit.Equals("bytes", StringComparison.OrdinalIgnoreCase)
                            || response.Content.Headers.ContentLength is { } length && length != end - position + 1)
                            throw new RangeUnavailableException();
                        await using var source = await response.Content.ReadAsStreamAsync(segmentToken).ConfigureAwait(false);
                        while (position <= end)
                        {
                            int wanted = (int)Math.Min(buffer.Length, end - position + 1);
                            int read = await ReadNetworkAsync(source, buffer.AsMemory(0, wanted), request, segmentToken).ConfigureAwait(false);
                            if (read == 0) throw new HttpIOException(HttpRequestError.ResponseEnded, "分段下载连接提前结束。");
                            await RandomAccess.WriteAsync(output.SafeFileHandle, buffer.AsMemory(0, read), position, segmentToken).ConfigureAwait(false);
                            position += read;
                            lock (progressGate)
                            {
                                received += read;
                                progress?.Invoke(new DownloadProgress(request.FilePath, received, request.Size));
                            }
                        }
                    }
                    catch (Exception error) when (!segmentToken.IsCancellationRequested && error is not DownloadStalledException
                        && IsTransientDownloadError(error) && retries < request.MaxRetries)
                    {
                        // 重试本分段尚未收到的区间，不重新下载其他已完成分段。
                        await Task.Delay(GetRetryDelay(request, ++retries, error), segmentToken).ConfigureAwait(false);
                    }
                }
            }).ConfigureAwait(false);
            await output.FlushAsync(token).ConfigureAwait(false);
        }
        try { return await VerifyExistingAsync(temporary, request.Size, request.Sha1, token).ConfigureAwait(false); }
        catch (InvalidDataException) { throw new SourceContentException("分段下载内容的大小或 SHA-1 校验失败。"); }
    }
}
