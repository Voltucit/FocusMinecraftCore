namespace FMCore.Downloads;

public static partial class FileDownloader
{
    private static async Task<string?> TryVerifyExistingAsync(
        DownloadRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await VerifyExistingAsync(
                request.FilePath,
                request.Size,
                request.Sha1,
                cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidDataException)
            when (request.ReplaceInvalidFiles)
        {
            // 只有内容不匹配才进入修复。
            // 权限、磁盘、取消等错误继续传播。
            return null;
        }
    }

    private static async Task<DownloadResult> CommitAsync(
        DownloadRequest request,
        string temporaryPath,
        long size,
        string actualSha1,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string filePath = request.FilePath;

        if (Directory.Exists(filePath))
        {
            throw new IOException(
                $"目标文件路径被目录占用：{filePath}");
        }

        if (request.ReplaceInvalidFiles && File.Exists(filePath))
        {
            // 下载期间，其他操作可能已经补好了目标。
            string? existingSha1 =
                await TryVerifyExistingAsync(
                    request,
                    cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (existingSha1 is not null)
            {
                File.Delete(temporaryPath);

                return new DownloadResult(
                    filePath,
                    request.Size,
                    existingSha1,
                    AlreadyPresent: true);
            }

            string directory = Path.GetDirectoryName(filePath)!;

            string backupPath = Path.Combine(
                directory,
                Path.GetFileName(filePath)
                + $".fmcore-backup-{Guid.NewGuid():N}.bak");

            if (File.Exists(backupPath)
                || Directory.Exists(backupPath))
            {
                throw new IOException(
                    $"备份位置已被占用：{backupPath}");
            }

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                File.Replace(
                    temporaryPath,
                    filePath,
                    backupPath,
                    ignoreMetadataErrors: false);
            }
            catch (Exception error)
                when (error is IOException
                    or UnauthorizedAccessException
                    or NotSupportedException)
            {
                throw new IOException(
                    $"替换文件失败：{filePath}。" +
                    $"备份候选位置：{backupPath}。" +
                    error.Message,
                    error);
            }

            return new DownloadResult(
                filePath,
                size,
                actualSha1,
                AlreadyPresent: false)
            {
                BackupPath = backupPath
            };
        }

        File.Move(
            temporaryPath,
            filePath,
            overwrite: false);

        return new DownloadResult(
            filePath,
            size,
            actualSha1,
            AlreadyPresent: false);
    }
}
