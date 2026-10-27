using System.Net;
namespace FMCore.Downloads;

public static partial class FileDownloader
{
    public static async Task<DownloadResult> DownloadAsync(
    DownloadRequest request,
    Action<DownloadProgress>? onProgress = null,
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();

    request = NormalizeRequest(request);

    using CancellationTokenSource deadline =
        new(request.Timeout);

    using CancellationTokenSource linked =
        CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            deadline.Token);

    CancellationToken token = linked.Token;

    int attempt = 1;
    bool callbackFailed = false;

    string currentUrl = request.Url;
    bool usingFallback = false;

    TransferMeter meter = new();

    long transferredBytes = 0;
    long lastReportedBytes = 0;

    bool CanFallback(Exception error)
    {
        return !usingFallback
            && request.FallbackUrl is not null
            && error is (
                HttpRequestException
                or HttpIOException);
    }

    void ReportProgress(DownloadProgress progress)
    {
        try
        {
            if (!progress.AlreadyPresent)
            {
                long delta =
                    progress.DownloadedBytes
                    - lastReportedBytes;

                transferredBytes = checked(
                    transferredBytes + delta);

                lastReportedBytes =
                    progress.DownloadedBytes;
            }

            var metrics = meter.Measure(
                transferredBytes,
                progress.TotalBytes
                - progress.DownloadedBytes);

            onProgress?.Invoke(progress with
            {
                Attempt = attempt,
                TransferredBytes = transferredBytes,
                BytesPerSecond = metrics.Speed,
                RemainingTime = metrics.Remaining
            });
        }
        catch
        {
            callbackFailed = true;
            throw;
        }
    }

    try
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            lastReportedBytes = 0;

            try
            {
                DownloadResult result =
                    await DownloadOnceAsync(
                        request,
                        currentUrl,
                        ReportProgress,
                        token).ConfigureAwait(false);

                return result with
                {
                    Attempts = attempt
                };
            }
            catch (Exception error)
                when (!callbackFailed
                    && !token.IsCancellationRequested
                    && attempt <= request.MaxRetries
                    && (CanFallback(error)
                        || IsTransientDownloadError(error)))
            {
                if (CanFallback(error))
                {
                    currentUrl = request.FallbackUrl!;
                    usingFallback = true;
                    attempt++;

                    continue;
                }

                TimeSpan delay = GetRetryDelay(
                    request,
                    attempt,
                    error);

                if (delay > request.Timeout)
                    throw;

                await Task.Delay(delay, token)
                    .ConfigureAwait(false);

                attempt++;
            }
        }
    }
    catch (OperationCanceledException error)
        when (deadline.IsCancellationRequested
            && !cancellationToken.IsCancellationRequested)
    {
        throw new TimeoutException(
            "文件下载、校验或重试等待超时。",
            error);
    }
}

    private static bool IsTransientDownloadError(
        Exception error)
    {
        return error switch
        {
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

    private static void EnsureSuccessfulResponse(
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
    {
        double milliseconds = Math.Min(
            30_000,
            request.RetryDelay.TotalMilliseconds
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
