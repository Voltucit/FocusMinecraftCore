using System.Net.Http;
using System.Security.Cryptography;
using FMCore.Installer;

namespace FMCore.Downloads;

public static class FileDownloader
{
    // 复用连接；每次操作通过自己的取消令牌控制超时。
    private static readonly HttpClient Client = new()
    {
        Timeout = System.Threading.Timeout.InfiniteTimeSpan
    };

    public static async Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        Action<DownloadProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

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

        if (request.Sha1.Length != 40
            || !request.Sha1.All(Uri.IsHexDigit))
        {
            throw new ArgumentException(
                "SHA-1 必须是 40 位十六进制字符串。",
                nameof(request));
        }

        if (request.Timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(
                nameof(request),
                "下载超时必须大于 0。");

        string filePath = PathSafety.Resolve(request.FilePath);

        PathSafety.ValidateWindowsRelativePath(
            Path.GetFileName(filePath));

        if (Directory.Exists(filePath))
            throw new IOException(
                $"目标文件路径被目录占用：{filePath}");

        string directory = Path.GetDirectoryName(filePath)
            ?? throw new ArgumentException(
                "无法确定目标文件所在目录。",
                nameof(request));

        string expectedSha1 = request.Sha1.ToLowerInvariant();

        string temporaryPath = Path.Combine(
            directory,
            $".fmcore-{Guid.NewGuid():N}.part");

        using CancellationTokenSource deadline =
            new(request.Timeout);

        using CancellationTokenSource linked =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                deadline.Token);

        CancellationToken token = linked.Token;

        bool temporaryCreated = false;
        Exception? failure = null;

        try
        {
            if (File.Exists(filePath))
            {
                string existingSha1 = await VerifyExistingAsync(
                    filePath,
                    request.Size,
                    expectedSha1,
                    token).ConfigureAwait(false);

                onProgress?.Invoke(new DownloadProgress(
                    filePath,
                    request.Size,
                    request.Size));

                token.ThrowIfCancellationRequested();

                return new DownloadResult(
                    filePath,
                    request.Size,
                    existingSha1,
                    AlreadyPresent: true);
            }

            onProgress?.Invoke(new DownloadProgress(
                filePath, 0, request.Size));

            using HttpResponseMessage response =
                await Client.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    token).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength
                    is long contentLength
                && contentLength != request.Size)
            {
                throw new InvalidDataException(
                    $"服务器返回的文件大小不匹配。" +
                    $"预期 {request.Size}，实际 {contentLength}。");
            }

            await using Stream source =
                await response.Content.ReadAsStreamAsync(token)
                    .ConfigureAwait(false);

            token.ThrowIfCancellationRequested();
            Directory.CreateDirectory(directory);

            long received = 0;
            string actualSha1;

            await using (FileStream output = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                FileOptions.Asynchronous
                    | FileOptions.SequentialScan))
            {
                temporaryCreated = true;

                using IncrementalHash hasher =
                    IncrementalHash.CreateHash(
                        HashAlgorithmName.SHA1);

                byte[] buffer = new byte[64 * 1024];

                while (true)
                {
                    int read = await source.ReadAsync(
                        buffer.AsMemory(),
                        token).ConfigureAwait(false);

                    if (read == 0)
                        break;

                    if (read > request.Size - received)
                        throw new InvalidDataException(
                            "下载内容超过清单声明的大小。");

                    await output.WriteAsync(
                        buffer.AsMemory(0, read),
                        token).ConfigureAwait(false);

                    hasher.AppendData(buffer, 0, read);
                    received += read;

                    onProgress?.Invoke(new DownloadProgress(
                        filePath,
                        received,
                        request.Size));
                }

                if (received != request.Size)
                    throw new InvalidDataException(
                        $"下载文件大小不匹配。" +
                        $"预期 {request.Size}，实际 {received}。");

                actualSha1 = Convert.ToHexString(
                    hasher.GetHashAndReset()).ToLowerInvariant();

                if (actualSha1 != expectedSha1)
                    throw new InvalidDataException(
                        "下载文件的 SHA-1 校验失败。");

                await output.FlushAsync(token)
                    .ConfigureAwait(false);
            }

            token.ThrowIfCancellationRequested();

            // 临时文件与目标位于同一目录，且不覆盖已有目标。
            File.Move(
                temporaryPath,
                filePath,
                overwrite: false);

            temporaryCreated = false;

            return new DownloadResult(
                filePath,
                received,
                actualSha1,
                AlreadyPresent: false);
        }
        catch (OperationCanceledException error)
            when (deadline.IsCancellationRequested
                && !cancellationToken.IsCancellationRequested)
        {
            TimeoutException timeoutError = new(
                "文件下载或校验超时。",
                error);

            failure = timeoutError;
            throw timeoutError;
        }
        catch (Exception error)
        {
            failure = error;
            throw;
        }
        finally
        {
            if (temporaryCreated)
            {
                try
                {
                    // 仅删除本次成功创建的这一个临时文件。
                    File.Delete(temporaryPath);
                }
                catch (Exception cleanupError)
                {
                    if (failure is not null)
                    {
                        throw new AggregateException(
                            "下载失败，且临时文件清理失败。",
                            failure,
                            cleanupError);
                    }

                    throw new IOException(
                        "下载临时文件清理失败。",
                        cleanupError);
                }
            }
        }
    }

    private static async Task<string> VerifyExistingAsync(
        string filePath,
        long expectedSize,
        string expectedSha1,
        CancellationToken cancellationToken)
    {
        await using FileStream stream = new(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous
                | FileOptions.SequentialScan);

        if (stream.Length != expectedSize)
            throw new InvalidDataException(
                $"已有目标文件大小不匹配，保留原文件：{filePath}");

        byte[] hash = await SHA1.HashDataAsync(
            stream,
            cancellationToken).ConfigureAwait(false);

        string actualSha1 =
            Convert.ToHexString(hash).ToLowerInvariant();

        if (actualSha1 != expectedSha1)
            throw new InvalidDataException(
                $"已有目标文件 SHA-1 不匹配，保留原文件：{filePath}");

        return actualSha1;
    }
}
