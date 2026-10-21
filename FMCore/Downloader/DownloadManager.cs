using System.Buffers;
using System.Net;
using System.Security.Cryptography;

namespace FMCore.Downloader;

/// <summary>Downloads verified files under a fixed root. The caller owns and reuses HttpClient.</summary>
public sealed partial class DownloadManager
{
    private static readonly HttpClient SharedHttp = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly HttpClient http;
    private readonly string root;
    private readonly DownloadOptions options;
    private readonly SemaphoreSlim slots;
    private readonly HashSet<string> activeTargets = new(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
    private readonly object targetLock = new();

    /// <summary>The absolute root shared by downloads and metadata caches.</summary>
    public string RootDirectory => root;

    /// <summary>Creates a downloader using the SDK's shared HTTP client. Destinations remain relative to this root.</summary>
    public DownloadManager(string rootDirectory, DownloadOptions? options = null) : this(SharedHttp, rootDirectory, options) { }

    /// <summary>Uses a caller-owned HTTP client, for example for proxy configuration or integration tests.</summary>
    public DownloadManager(HttpClient httpClient, string rootDirectory, DownloadOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        if (string.IsNullOrWhiteSpace(rootDirectory)) throw Invalid("下载根目录不能为空。");
        this.options = options ?? new();
        if (this.options.MaxConcurrency is < 1 or > 64 || this.options.MaxRetries is < 0 or > 10 ||
            this.options.AttemptTimeout <= TimeSpan.Zero || this.options.AttemptTimeout > TimeSpan.FromDays(1) ||
            this.options.RetryDelay < TimeSpan.Zero || this.options.MaxRetryDelay < this.options.RetryDelay ||
            this.options.MaxRetryDelay > TimeSpan.FromMinutes(5)) throw Invalid("下载选项超出允许范围。");
        try
        {
            root = Path.GetFullPath(rootDirectory);
            DownloadPath.CheckLinks(root);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        { throw Invalid("下载根目录无效。"); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { throw new DownloadException(DownloadErrorCode.IoError, "无法访问下载根目录。"); }
        http = httpClient;
        slots = new(this.options.MaxConcurrency);
    }

    /// <summary>Downloads one file, reusing it when its declared hash/size is valid. Supports progress, cancellation and retries.</summary>
    public Task<DownloadResult> DownloadAsync(DownloadRequest request, IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return DownloadPreparedAsync(Prepare(request), progress, cancellationToken);
    }

    /// <summary>Results follow input order. On failure, completed files remain valid; the batch is not a transaction.</summary>
    public async Task<IReadOnlyList<DownloadResult>> DownloadManyAsync(IEnumerable<DownloadRequest> requests,
        IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requests);
        cancellationToken.ThrowIfCancellationRequested();
        // Validate the whole batch before writing anything, including duplicate normalized destinations.
        var prepared = requests.Select(Prepare).ToArray();
        var comparison = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        if (prepared.Select(p => p.Path).Distinct(comparison).Count() != prepared.Length)
            throw Invalid("批次中包含重复目标路径。");
        var results = new DownloadResult[prepared.Length];
        await Parallel.ForEachAsync(Enumerable.Range(0, prepared.Length),
            new ParallelOptions { MaxDegreeOfParallelism = options.MaxConcurrency, CancellationToken = cancellationToken },
            async (index, ct) => results[index] = await DownloadPreparedAsync(prepared[index], progress, ct));
        return results;
    }

    private (DownloadRequest Request, string Path) Prepare(DownloadRequest request)
    {
        if (request == null || request.Source == null || !ValidSource(request.Source) || request.ExpectedSize is < 0)
            throw Invalid("下载地址或预期大小无效；远程地址必须使用 HTTPS，HTTP 仅限本机回环地址。");
        var length = request.HashAlgorithm switch
        {
            DownloadHashAlgorithm.Sha1 => 40,
            DownloadHashAlgorithm.Sha256 => 64,
            DownloadHashAlgorithm.Sha512 => 128,
            _ => throw Invalid("不支持的哈希算法。")
        };
        if (request.ExpectedHash is string hash && (hash.Length != length || !hash.All(char.IsAsciiHexDigit)))
            throw Invalid("预期哈希的长度或字符无效。");
        try { return (request, DownloadPath.Resolve(root, request.RelativePath)); }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        { throw Invalid("目标路径格式无效。"); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { throw Failure(DownloadErrorCode.IoError, "无法访问目标路径。", request); }
    }

    private async Task<DownloadResult> DownloadPreparedAsync((DownloadRequest Request, string Path) prepared,
        IProgress<DownloadProgress>? progress, CancellationToken ct)
    {
        var (request, path) = prepared;
        lock (targetLock)
            if (!activeTargets.Add(path)) throw Failure(DownloadErrorCode.DestinationBusy, "同一下载器已在处理该目标文件。", request);
        var acquired = false;
        try
        {
            await slots.WaitAsync(ct);
            acquired = true;
            DownloadPath.CheckLinks(path);
            if (await IsExistingValidAsync(request, path, ct))
            {
                var size = new FileInfo(path).Length;
                progress?.Report(new(request.RelativePath, size, size, 0, DownloadState.Skipped));
                return new(path, size, 0, true);
            }
            for (var attempt = 1; ; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try { return await AttemptAsync(request, path, attempt, progress, ct); }
                catch (DownloadException error) when (error.Retryable && attempt <= options.MaxRetries)
                {
                    progress?.Report(new(request.RelativePath, 0, request.ExpectedSize, attempt, DownloadState.Retrying));
                    var backoff = options.RetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
                    var delay = TimeSpan.FromMilliseconds(Math.Min(options.MaxRetryDelay.TotalMilliseconds,
                        Math.Max(backoff, error.RetryAfter?.TotalMilliseconds ?? 0)));
                    await Task.Delay(delay, ct);
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { throw Failure(DownloadErrorCode.IoError, "本地文件操作失败。", request); }
        finally
        {
            if (acquired) slots.Release();
            lock (targetLock) activeTargets.Remove(path);
        }
    }

    private static async Task<bool> IsExistingValidAsync(DownloadRequest request, string path, CancellationToken ct)
    {
        // File existence alone cannot prove that an earlier download is complete.
        if ((request.ExpectedHash == null && request.ExpectedSize == null) || !File.Exists(path)) return false;
        if (request.ExpectedSize is long size && new FileInfo(path).Length != size) return false;
        if (request.ExpectedHash == null) return true;
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var hash = IncrementalHash.CreateHash(Algorithm(request.HashAlgorithm));
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            int count;
            while ((count = await stream.ReadAsync(buffer.AsMemory(), ct)) > 0) hash.AppendData(buffer, 0, count);
            return Matches(hash, request.ExpectedHash);
        }
        finally { ArrayPool<byte>.Shared.Return(buffer); }
    }

    private async Task<DownloadResult> AttemptAsync(DownloadRequest request, string path, int attempt,
        IProgress<DownloadProgress>? progress, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(options.AttemptTimeout);
        try { return await TransferAsync(request, path, attempt, progress, timeout.Token); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw Failure(DownloadErrorCode.Timeout, "下载尝试超时。", request, attempt, retryable: true); }
        catch (HttpRequestException)
        { throw Failure(DownloadErrorCode.NetworkError, "网络连接或传输失败。", request, attempt, retryable: true); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        { throw Failure(DownloadErrorCode.IoError, "本地文件操作失败。", request, attempt); }
    }

    private async Task<DownloadResult> TransferAsync(DownloadRequest request, string path, int attempt,
        IProgress<DownloadProgress>? progress, CancellationToken ct)
    {
        progress?.Report(new(request.RelativePath, 0, request.ExpectedSize, attempt, DownloadState.Started));
        using var message = new HttpRequestMessage(HttpMethod.Get, request.Source);
        using var response = await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, ct);
        if (response.RequestMessage?.RequestUri is Uri finalUri && !ValidSource(finalUri))
            throw Failure(DownloadErrorCode.InvalidRequest, "重定向后的地址不符合下载策略。", request, attempt);
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var status = response.StatusCode;
            var retryAfter = response.Headers.RetryAfter;
            var wait = retryAfter?.Delta ?? (retryAfter?.Date - DateTimeOffset.UtcNow);
            throw new DownloadException(DownloadErrorCode.HttpError, $"下载服务返回 HTTP {(int)status}。", request.RelativePath,
                attempt, status, status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)status >= 500, wait);
        }
        var contentLength = response.Content.Headers.ContentLength;
        if (request.ExpectedSize is long expected && contentLength is long declared && expected != declared)
            throw Failure(DownloadErrorCode.SizeMismatch, "响应声明的大小与预期不符。", request, attempt, retryable: true);
        var total = request.ExpectedSize ?? contentLength;
        DownloadPath.CheckLinks(path);
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        DownloadPath.CheckLinks(path);
        var temporary = Path.Combine(directory, $".fmcore-{Guid.NewGuid():N}.part");
        // Only clean up a temporary file after CreateNew has established ownership.
        var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        try
        {
            long bytes;
            await using (output)
                bytes = await WriteResponseAsync(response, request, output, total, attempt, progress, ct);
            ct.ThrowIfCancellationRequested();
            DownloadPath.CheckLinks(path);
            // Same-directory rename: never truncate/delete the existing target before validation succeeds.
            File.Move(temporary, path, overwrite: true);
            progress?.Report(new(request.RelativePath, bytes, total, attempt, DownloadState.Completed));
            return new(path, bytes, attempt, false);
        }
        finally { File.Delete(temporary); }
    }

    private static async Task<long> WriteResponseAsync(HttpResponseMessage response, DownloadRequest request,
        Stream output, long? total, int attempt, IProgress<DownloadProgress>? progress, CancellationToken ct)
    {
        Stream inputStream;
        try { inputStream = await response.Content.ReadAsStreamAsync(ct); }
        catch (IOException) { throw Failure(DownloadErrorCode.NetworkError, "无法读取响应内容。", request, attempt, retryable: true); }
        await using var input = inputStream;
        using var hash = request.ExpectedHash == null ? null : IncrementalHash.CreateHash(Algorithm(request.HashAlgorithm));
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        long bytes = 0;
        try
        {
            while (true)
            {
                int count;
                try { count = await input.ReadAsync(buffer.AsMemory(), ct); }
                catch (IOException) { throw Failure(DownloadErrorCode.NetworkError, "响应内容传输中断。", request, attempt, retryable: true); }
                if (count == 0) break;
                bytes += count;
                if (total is long expected && bytes > expected)
                    throw Failure(DownloadErrorCode.SizeMismatch, "响应内容超过预期大小。", request, attempt, retryable: true);
                await output.WriteAsync(buffer.AsMemory(0, count), ct);
                hash?.AppendData(buffer, 0, count);
                progress?.Report(new(request.RelativePath, bytes, total, attempt, DownloadState.Downloading));
            }
            if (total is long length && bytes != length)
                throw Failure(DownloadErrorCode.SizeMismatch, "响应内容不完整。", request, attempt, retryable: true);
            if (hash != null && !Matches(hash, request.ExpectedHash!))
                throw Failure(DownloadErrorCode.HashMismatch, "下载内容的哈希校验失败。", request, attempt, retryable: true);
            await output.FlushAsync(ct);
            return bytes;
        }
        finally { ArrayPool<byte>.Shared.Return(buffer); }
    }

    private static bool ValidSource(Uri source) => source.IsAbsoluteUri && source.UserInfo.Length == 0 &&
        (source.Scheme == Uri.UriSchemeHttps || source.Scheme == Uri.UriSchemeHttp && source.IsLoopback);

    private static HashAlgorithmName Algorithm(DownloadHashAlgorithm algorithm) => algorithm switch
    {
        DownloadHashAlgorithm.Sha1 => HashAlgorithmName.SHA1,
        DownloadHashAlgorithm.Sha256 => HashAlgorithmName.SHA256,
        DownloadHashAlgorithm.Sha512 => HashAlgorithmName.SHA512,
        _ => throw Invalid("不支持的哈希算法。")
    };

    private static bool Matches(IncrementalHash hash, string expected) =>
        string.Equals(Convert.ToHexString(hash.GetHashAndReset()), expected, StringComparison.OrdinalIgnoreCase);

    private static DownloadException Invalid(string message) => new(DownloadErrorCode.InvalidRequest, message);
    private static DownloadException Failure(DownloadErrorCode code, string message, DownloadRequest request,
        int attempt = 0, bool retryable = false) => new(code, message, request.RelativePath, attempt, retryable: retryable);
}
