using System.Net;
namespace FMCore.Downloads;

public static partial class FileDownloader
{
    public static async Task<DownloadResult> DownloadAsync(
        DownloadRequest request, Action<DownloadProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        request = NormalizeRequest(request);
        using var deadline = new CancellationTokenSource(request.Timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        var token = linked.Token;
        var sources = SourceCandidates(request);
        int sourceIndex = 0, attempt = 1, retries = 0;
        string currentUrl = sources[0];
        string currentHost = new Uri(currentUrl).Host;
        bool callbackFailed = false;
        var meter = new TransferMeter();
        long transferred = 0, lastReported = 0;

        void ReportProgress(DownloadProgress progress)
        {
            try
            {
                if (!progress.AlreadyPresent)
                {
                    // Range 退回普通 GET 时，文件进度可归零，实际传输量不能倒扣。
                    transferred = checked(transferred + Math.Max(0, progress.DownloadedBytes - lastReported));
                    lastReported = progress.DownloadedBytes;
                }
                var metrics = meter.Measure(transferred, progress.TotalBytes - progress.DownloadedBytes);
                onProgress?.Invoke(progress with
                {
                    Attempt = attempt, TransferredBytes = transferred,
                    BytesPerSecond = metrics.Speed, RemainingTime = metrics.Remaining, SourceHost = progress.AlreadyPresent ? "" : currentHost
                });
            }
            catch { callbackFailed = true; throw; }
        }

        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                lastReported = 0;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    var result = await DownloadOnceAsync(request, currentUrl, ReportProgress, token).ConfigureAwait(false);
                    if (!result.AlreadyPresent) ObserveSource(currentUrl, request.Size, watch.Elapsed);
                    return result with { Attempts = attempt, SourceHost = result.AlreadyPresent ? "" : currentHost };
                }
                catch (Exception error) when (!callbackFailed && !token.IsCancellationRequested
                    && error is HttpRequestException or HttpIOException or DownloadStalledException or SourceContentException)
                {
                    ObserveSource(currentUrl, request.Size, watch.Elapsed, error);
                    if (sourceIndex + 1 < sources.Length)
                    {
                        currentUrl = sources[++sourceIndex];
                        currentHost = new Uri(currentUrl).Host;
                        retries = 0;
                        attempt++;
                        continue;
                    }
                    if (retries >= request.MaxRetries || !IsTransientDownloadError(error)) throw;
                    var delay = GetRetryDelay(request, ++retries, error);
                    if (delay > request.Timeout) throw;
                    await Task.Delay(delay, token).ConfigureAwait(false);
                    attempt++;
                }
            }
        }
        catch (OperationCanceledException error) when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        { throw new TimeoutException("文件下载、校验或重试等待超时。", error); }
        catch (SourceContentException error)
        { throw new InvalidDataException(error.Message, error); }
    }

    internal static bool IsTransientDownloadError(
        Exception error)
    {
        return error switch
        {
            DownloadStalledException => true,

            RetryableHttpException => true,

            HttpRequestException requestError =>
                requestError.StatusCode is null
                && requestError.HttpRequestError is
                    HttpRequestError.NameResolutionError
                    or HttpRequestError.ConnectionError
                    or HttpRequestError.ResponseEnded,

            HttpIOException streamError =>
                streamError.HttpRequestError
                    == HttpRequestError.ResponseEnded,

            _ => false
        };
    }

    private static bool IsTransientStatus(
        HttpStatusCode? statusCode)
    {
        return statusCode is
            HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    internal static void EnsureSuccessfulResponse(
        HttpResponseMessage response)
    {
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException error)
            when (IsTransientStatus(error.StatusCode))
        {
            var header = response.Headers.RetryAfter;

            TimeSpan? retryAfter = header?.Delta;

            if (retryAfter is null
                && header?.Date is DateTimeOffset date)
            {
                retryAfter = date - DateTimeOffset.UtcNow;
            }

            if (retryAfter is TimeSpan delay
                && delay < TimeSpan.Zero)
            {
                retryAfter = TimeSpan.Zero;
            }

            throw new RetryableHttpException(
                error,
                retryAfter);
        }
    }

    private static TimeSpan GetRetryDelay(
        DownloadRequest request,
        int attempt,
        Exception error)
        => GetRetryDelay(request.RetryDelay, attempt, error);

    internal static TimeSpan GetRetryDelay(
        TimeSpan baseDelay,
        int attempt,
        Exception error)
    {
        double milliseconds = Math.Min(
            30_000,
            baseDelay.TotalMilliseconds
                * Math.Pow(2, attempt - 1));

        TimeSpan delay =
            TimeSpan.FromMilliseconds(milliseconds);

        if (error is RetryableHttpException
            {
                RetryAfter: TimeSpan serverDelay
            }
            && serverDelay > delay)
        {
            delay = serverDelay;
        }

        return delay;
    }

    private sealed class RetryableHttpException
        : HttpRequestException
    {
        public TimeSpan? RetryAfter { get; }

        public RetryableHttpException(
            HttpRequestException error,
            TimeSpan? retryAfter)
            : base(
                error.Message,
                error,
                error.StatusCode)
        {
            RetryAfter = retryAfter;
        }
    }
}

