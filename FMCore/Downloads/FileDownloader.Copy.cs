using FMCore.Installer;

namespace FMCore.Downloads;

public static partial class FileDownloader
{
    // 旧版资源从已下载的对象复制；复用下载器的校验、备份和原子替换。
    internal static async Task<DownloadResult> CopyVerifiedAsync(
        string sourcePath, DownloadRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        request = NormalizeRequest(request);
        sourcePath = PathSafety.Resolve(sourcePath);

        if (File.Exists(request.FilePath))
        {
            string? existing = await TryVerifyExistingAsync(request, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
                return new DownloadResult(request.FilePath, request.Size, existing, true);
        }

        string directory = Path.GetDirectoryName(request.FilePath)!;
        string temporary = Path.Combine(directory, $".fmcore-{Guid.NewGuid():N}.part");
        bool created = false;
        try
        {
            await using (FileStream source = new(sourcePath, FileMode.Open,
                FileAccess.Read, FileShare.Read, 64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                if (source.Length != request.Size)
                    throw new InvalidDataException($"资源对象大小不匹配：{sourcePath}");
                cancellationToken.ThrowIfCancellationRequested();
                Directory.CreateDirectory(directory);
                await using FileStream output = new(temporary, FileMode.CreateNew,
                    FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous);
                created = true;
                await source.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            string sha1 = await VerifyExistingAsync(temporary, request.Size,
                request.Sha1, cancellationToken).ConfigureAwait(false);
            DownloadResult result = await CommitAsync(request, temporary,
                request.Size, sha1, cancellationToken).ConfigureAwait(false);
            created = false;
            return result;
        }
        finally
        {
            if (created)
                File.Delete(temporary);
        }
    }
}
