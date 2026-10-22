namespace FMCore.Downloader;

public sealed partial class DownloadManager
{
    /// <summary>下载 URL 到根目录内的相对路径；复用原下载器的路径校验、重试、原子替换和取消。需要哈希时使用 DownloadRequest。</summary>
    /// <param name="url">HTTPS URL；HTTP 只允许回环地址。地址不会写入异常。</param>
    /// <param name="relativePath">相对于 RootDirectory 的目标文件路径。</param>
    /// <param name="progress">可选字节进度与下载状态。</param>
    /// <param name="cancellationToken">取消等待或传输。</param>
    /// <returns>包含完整路径、字节数和复用状态的下载结果。</returns>
    public Task<DownloadResult> DownloadAsync(string url, string relativePath,
        IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var source))
            throw new DownloadException(DownloadErrorCode.InvalidRequest, "下载地址格式无效。");
        return DownloadAsync(new DownloadRequest(source, relativePath), progress, cancellationToken);
    }

    /// <summary>只指定 URL 和目标文件即可下载。以目标父目录作为安全根；高级校验使用 DownloadRequest 与实例入口。</summary>
    /// <param name="url">HTTPS URL；HTTP 只允许回环地址。</param>
    /// <param name="destinationPath">调用方明确指定的目标文件，可为绝对或相对路径。</param>
    /// <param name="options">可选超时、重试及并发配置。</param>
    /// <param name="progress">可选字节进度。</param>
    /// <param name="cancellationToken">取消下载。</param>
    /// <returns>与实例 DownloadAsync 相同的结果；没有哈希或大小时每次重新下载。</returns>
    public static Task<DownloadResult> DownloadFileAsync(string url, string destinationPath,
        DownloadOptions? options = null, IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(destinationPath))
            throw new DownloadException(DownloadErrorCode.InvalidRequest, "目标文件不能为空。");
        string path;
        try { path = Path.GetFullPath(destinationPath); }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        { throw new DownloadException(DownloadErrorCode.InvalidRequest, "目标文件路径无效。"); }
        var parent = Path.GetDirectoryName(path);
        if (parent == null || Path.GetFileName(path).Length == 0)
            throw new DownloadException(DownloadErrorCode.InvalidRequest, "目标必须为文件路径。");
        return new DownloadManager(parent, options).DownloadAsync(url, Path.GetFileName(path), progress, cancellationToken);
    }
}
