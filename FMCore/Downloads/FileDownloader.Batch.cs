using System.Collections.Concurrent;
using FMCore.Installer;

namespace FMCore.Downloads;

public static partial class FileDownloader
{
    public static async Task<IReadOnlyList<DownloadResult>>
        DownloadManyAsync(
            IEnumerable<DownloadRequest> requests,
            int maxConcurrency = 4,
            Action<DownloadBatchProgress>? onProgress = null,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);

        if (maxConcurrency <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxConcurrency),
                "最大并发数必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 固定本批次的请求集合。
        DownloadRequest[] items = requests.ToArray();

        HashSet<string> destinations =
            new(PathSafety.Comparer);

        long totalBytes = 0;

        // 全部请求先检查参数和重复目标，再开始下载。
        for (int index = 0; index < items.Length; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            DownloadRequest request =
                NormalizeRequest(items[index]);

            if (!destinations.Add(request.FilePath))
            {
                throw new ArgumentException(
                    $"批次中存在重复目标：{request.FilePath}",
                    nameof(requests));
            }

            items[index] = request;

            totalBytes = checked(totalBytes + request.Size);
        }

        // 拒绝一个目标同时成为另一个目标的父目录。
        foreach (DownloadRequest request in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (
                string? parent =
                    Path.GetDirectoryName(request.FilePath);
                parent is not null;
                parent = Path.GetDirectoryName(parent))
            {
                if (destinations.Contains(parent))
                {
                    throw new ArgumentException(
                        $"目标文件与其他目标的父目录冲突：{parent}",
                        nameof(requests));
                }
            }
        }

        DownloadResult[] results = new DownloadResult[items.Length];
        long[] processedByFile = new long[items.Length];
        long[] transferredByFile = new long[items.Length];


        ConcurrentQueue<Exception> failures = new();

        object progressLock = new();

        int completedFiles = 0;
        int activeFiles = 0;
        int peakActiveFiles = 0;
        long processedBytes = 0;
        long transferredBytes = 0;

        TransferMeter meter = new();
        // 调用本方法时，需要持有 progressLock。
        void ReportProgress()
        {
            var metrics = meter.Measure(
                transferredBytes,
                totalBytes - processedBytes);

            onProgress?.Invoke(new DownloadBatchProgress(
                items.Length,
                completedFiles,
                activeFiles,
                peakActiveFiles,
                processedBytes,
                totalBytes)
            {
                TransferredBytes = transferredBytes,
                BytesPerSecond = metrics.Speed,
                RemainingTime = metrics.Remaining
            });
        }

        lock (progressLock)
        {
            ReportProgress();
        }

        ParallelOptions options = new()
        {
            MaxDegreeOfParallelism = maxConcurrency,
            CancellationToken = cancellationToken
        };

        try
        {
            await Parallel.ForEachAsync(
                Enumerable.Range(0, items.Length),
                options,
                async (index, token) =>
                {
                    bool entered = false;

                    try
                    {
                        lock (progressLock)
                        {
                            token.ThrowIfCancellationRequested();

                            activeFiles++;
                            entered = true;

                            peakActiveFiles = Math.Max(
                                peakActiveFiles,
                                activeFiles);

                            ReportProgress();
                        }

                        DownloadResult result =
                            await DownloadAsync(
                                items[index],
                                progress =>
                                {
                                    lock (progressLock)
                                    {
                                        long transferredDelta =
                                            progress.TransferredBytes
                                            - transferredByFile[index];

                                        transferredBytes = checked(
                                            transferredBytes + transferredDelta);

                                        transferredByFile[index] =
                                            progress.TransferredBytes;

                                        processedBytes +=
                                            progress.DownloadedBytes
                                            - processedByFile[index];

                                        processedByFile[index] =
                                            progress.DownloadedBytes;

                                        ReportProgress();
                                    }
                                },
                                token).ConfigureAwait(false);

                        lock (progressLock)
                        {
                            results[index] = result;

                            processedBytes +=
                                result.Size - processedByFile[index];

                            processedByFile[index] = result.Size;

                            completedFiles++;

                            ReportProgress();
                        }
                    }
                    catch (OperationCanceledException)
                        when (token.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception error)
                    {
                        failures.Enqueue(
                            new InvalidOperationException(
                                $"处理下载项失败：{items[index].FilePath}；" +
                                error.Message,
                                error));

                        throw;
                    }
                    finally
                    {
                        if (entered)
                        {
                            lock (progressLock)
                            {
                                activeFiles--;
                            }
                        }
                    }
                }).ConfigureAwait(false);
        }
        catch
        {
            // 此时已经等待所有启动的操作结束及清理。
            if (!failures.IsEmpty)
            {
                throw new AggregateException(
                    "批量下载失败；已经完成的文件保留。",
                    failures.ToArray());
            }

            throw;
        }

        cancellationToken.ThrowIfCancellationRequested();

        lock (progressLock)
        {
            ReportProgress();
        }

        cancellationToken.ThrowIfCancellationRequested();

        // 返回顺序与请求顺序一致。
        return Array.AsReadOnly(results);
    }

    internal static DownloadRequest NormalizeRequest(
        DownloadRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ArgumentException.ThrowIfNullOrWhiteSpace(request.Url);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Sha1);
        ArgumentOutOfRangeException.ThrowIfNegative(request.Size);

        if (!Uri.TryCreate(
                request.Url,
                UriKind.Absolute,
                out Uri? url)
            || url is null
            || url.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException(
                "下载地址必须是完整的 HTTP 或 HTTPS 地址。",
                nameof(request));
        }

        string? fallbackUrl = null;

        if (request.FallbackUrl is not null)
        {
            if (!Uri.TryCreate(
                    request.FallbackUrl,
                    UriKind.Absolute,
                    out Uri? fallback)
                || fallback is null
                || fallback.Scheme is not ("http" or "https"))
            {
                throw new ArgumentException(
                    "备用下载地址必须是完整的 HTTP 或 HTTPS 地址。",
                    nameof(request));
            }

            fallbackUrl = fallback.AbsoluteUri;
        }

        if (request.Sha1.Length != 40
            || !request.Sha1.All(Uri.IsHexDigit))
        {
            throw new ArgumentException(
                "SHA-1 必须是 40 位十六进制字符串。",
                nameof(request));
        }

        if (request.Timeout <= TimeSpan.Zero
            || request.Timeout.TotalMilliseconds
                > uint.MaxValue - 1d)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "下载超时必须大于 0，且不能超过计时器支持的范围。");
        }

        if (request.MaxRetries is < 0 or > 10)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "额外重试次数必须在 0 到 10 之间。");
        }

        if (request.RetryDelay < TimeSpan.Zero
            || request.RetryDelay > TimeSpan.FromSeconds(30))
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "重试基础等待时间必须在 0 到 30 秒之间。");
        }

        string filePath = PathSafety.Resolve(request.FilePath);

        PathSafety.ValidateWindowsRelativePath(
            Path.GetFileName(filePath));

        if (Directory.Exists(filePath))
        {
            throw new IOException(
                $"目标文件路径被目录占用：{filePath}");
        }

        if (Path.GetDirectoryName(filePath) is null)
        {
            throw new ArgumentException(
                "无法确定目标文件所在目录。",
                nameof(request));
        }

        return new DownloadRequest
        {
            Url = url.AbsoluteUri,
            FallbackUrl = fallbackUrl,
            FilePath = filePath,
            Size = request.Size,
            Sha1 = request.Sha1.ToLowerInvariant(),
            Timeout = request.Timeout,
            MaxRetries = request.MaxRetries,
            RetryDelay = request.RetryDelay,
            ReplaceInvalidFiles = request.ReplaceInvalidFiles
        };
    }
}
