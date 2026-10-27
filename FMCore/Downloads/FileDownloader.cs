using System.Net.Http;
using System.Security.Cryptography;
using FMCore.Installer;

namespace FMCore.Downloads;

public static partial class FileDownloader
{
    // 复用连接；每次操作通过自己的取消令牌控制超时。
    private static readonly HttpClient Client = new()
    {
        Timeout = System.Threading.Timeout.InfiniteTimeSpan
    };

    private static async Task<DownloadResult> DownloadOnceAsync(
        DownloadRequest request,
        string url,
        Action<DownloadProgress>? onProgress,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();



        string filePath = request.FilePath;
        string directory = Path.GetDirectoryName(filePath)!;
        string expectedSha1 = request.Sha1;

        string temporaryPath = Path.Combine(
            directory,
            $".fmcore-{Guid.NewGuid():N}.part");

        CancellationToken token = cancellationToken;

        bool temporaryCreated = false;
        Exception? failure = null;

        try
        {
            if (File.Exists(filePath))
            {
                string? existingSha1 =
                    await TryVerifyExistingAsync(
                        request,
                        token).ConfigureAwait(false);

                if (existingSha1 is not null)
                {
                    onProgress?.Invoke(new DownloadProgress(
                        filePath,
                        request.Size,
                        request.Size)
                    {
                        AlreadyPresent = true
                    });

                    token.ThrowIfCancellationRequested();

                    return new DownloadResult(
                        filePath,
                        request.Size,
                        existingSha1,
                        AlreadyPresent: true);
                }
            }

            onProgress?.Invoke(new DownloadProgress(
                filePath, 0, request.Size));

            using HttpResponseMessage response = await Client.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead,
                    token).ConfigureAwait(false);

            EnsureSuccessfulResponse(response);

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

            DownloadResult result = await CommitAsync(
                request,
                temporaryPath,
                received,
                actualSha1,
                token).ConfigureAwait(false);

            temporaryCreated = false;

            return result;
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
