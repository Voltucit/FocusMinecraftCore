using System.Net;
using System.Net.Http;
using FMCore.Downloads;

namespace FMCore.Modsloader;

public sealed partial class OptiFineOnlineInstaller
{
    private const int MaximumPackageBytes = 64 * 1024 * 1024;

    private static readonly HttpClient PackageClient = new()
    {
        Timeout = System.Threading.Timeout.InfiniteTimeSpan
    };

    private async Task EnsurePackageAsync(
        OptiFineInstallVersion version,
        string path,
        Action<OptiFineInstallProgress>? onProgress,
        CancellationToken cancellationToken)
    {
        void Message(string text) => onProgress?.Invoke(new OptiFineInstallProgress(
            OptiFineInstallStage.DownloadingInstaller, text));

        if (File.Exists(path))
        {
            await CheckPackageAsync(path, version, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            Message("已有 OptiFine 安装包版本检查通过，复用。");
            return;
        }

        using CancellationTokenSource deadline = new(TimeSpan.FromMinutes(5));
        using CancellationTokenSource linked =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, deadline.Token);
        CancellationToken token = linked.Token;

        string originalUrl = version.DownloadUrl;
        string selectedUrl = Source.GetUrl(originalUrl);
        bool mayFallback = selectedUrl != originalUrl;
        bool callbackFailed = false;
        long transferred = 0;
        FileDownloader.TransferMeter meter = new();

        void Report(long received, long? length, int attempt)
        {
            var metrics = meter.Measure(transferred, length is long size ? size - received : 0);
            string text = length is null
                ? $"下载 OptiFine 安装包：{received / 1024d / 1024:F2} MiB，大小未知。"
                : "下载 OptiFine 安装包……";

            try
            {
                onProgress?.Invoke(new OptiFineInstallProgress(
                    OptiFineInstallStage.DownloadingInstaller,
                    text,
                    InstallerDownload: length is long total
                        ? new DownloadProgress(path, received, total)
                        {
                            Attempt = attempt,
                            TransferredBytes = transferred,
                            BytesPerSecond = metrics.Speed,
                            RemainingTime = metrics.Remaining
                        }
                        : null));
            }
            catch
            {
                callbackFailed = true;
                throw;
            }
        }

        try
        {
            for (int attempt = 1; ; attempt++)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    await DownloadPackageOnceAsync(
                        selectedUrl, path,
                        bytes => transferred = checked(transferred + bytes),
                        (received, length) => Report(received, length, attempt),
                        version, token).ConfigureAwait(false);
                    return;
                }
                catch (Exception error) when (
                    !callbackFailed && !token.IsCancellationRequested && attempt < 3
                    && ((mayFallback && error is (HttpRequestException or HttpIOException))
                        || FileDownloader.IsTransientDownloadError(error)))
                {
                    if (mayFallback && error is (HttpRequestException or HttpIOException))
                    {
                        selectedUrl = originalUrl;
                        mayFallback = false;
                        continue;
                    }

                    TimeSpan delay = FileDownloader.GetRetryDelay(
                        TimeSpan.FromSeconds(1), attempt, error);

                    if (delay > TimeSpan.FromMinutes(5))
                        throw;

                    await Task.Delay(delay, token).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException error)
            when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("OptiFine 安装包获取超过 5 分钟。", error);
        }
    }

    private static async Task DownloadPackageOnceAsync(
        string url,
        string path,
        Action<int> recordTransfer,
        Action<long, long?> onProgress,
        OptiFineInstallVersion version,
        CancellationToken cancellationToken)
    {
        string directory = Path.GetDirectoryName(path)!;
        string temporary = Path.Combine(directory, ".optifine-download-" + Guid.NewGuid().ToString("N") + ".part");
        bool created = false;

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.AcceptEncoding.ParseAdd("identity");

            using HttpResponseMessage response = await PackageClient.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            FileDownloader.EnsureSuccessfulResponse(response);

            long? length = response.Content.Headers.ContentLength;

            if (response.StatusCode != HttpStatusCode.OK
                || response.Content.Headers.ContentRange is not null
                || length is <= 0 or > MaximumPackageBytes
                || response.Content.Headers.ContentEncoding.Any(
                    value => !value.Equals("identity", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("安装包响应无效、被编码或超过 64 MiB。");

            onProgress(0, length);
            Directory.CreateDirectory(directory);
            long received = 0;

            await using (Stream input = await response.Content.ReadAsStreamAsync(
                cancellationToken).ConfigureAwait(false))
            await using (FileStream output = new(
                temporary, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 64 * 1024, useAsync: true))
            {
                created = true;
                byte[] buffer = new byte[64 * 1024];

                while (true)
                {
                    int count = await input.ReadAsync(
                        buffer.AsMemory(), cancellationToken).ConfigureAwait(false);

                    if (count == 0)
                        break;

                    if (count > MaximumPackageBytes - received
                        || (length is long size && count > size - received))
                        throw new InvalidDataException("安装包正文超过声明长度或 64 MiB。");

                    await output.WriteAsync(
                        buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);

                    received += count;
                    recordTransfer(count);
                    onProgress(received, length);
                }

                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            if (length is long declared && received != declared)
                throw new InvalidDataException("安装包正文长度与响应不一致。");

            await CheckPackageAsync(
                temporary, version, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, path, overwrite: false);
        }
        catch (Exception error)
        {
            if (created)
            {
                try
                {
                    File.Delete(temporary);
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(
                        "安装包获取失败，临时文件清理也失败。", error, cleanup);
                }
            }

            throw;
        }
    }

    private static async Task CheckPackageAsync(
        string path,
        OptiFineInstallVersion expected,
        CancellationToken cancellationToken)
    {
        if (new FileInfo(path).Length > MaximumPackageBytes)
            throw new InvalidDataException("已有安装包超过 64 MiB，文件已保留。");

        var actual = await OptiFineInstaller.ReadInstallerVersionAsync(
            path, cancellationToken).ConfigureAwait(false);

        if (actual.GameVersion != expected.GameVersion || actual.Edition != expected.Edition)
            throw new InvalidDataException("安装包内版本与所选版本不匹配，已有文件保留。");
    }
}

