namespace FMCore.Downloader;

/// <summary>Result of checking a local file against its declared download metadata.</summary>
public enum FileVerificationState { Valid, Unverified, Missing, SizeMismatch, HashMismatch }

/// <summary>Unverified means the file exists but no size or hash was supplied; it is not a cryptographic integrity guarantee.</summary>
public sealed record FileVerificationResult(string RelativePath, FileVerificationState State)
{
    /// <summary>Whether the local file meets all metadata checks available to the SDK.</summary>
    public bool IsUsable => State is FileVerificationState.Valid or FileVerificationState.Unverified;
}

public sealed partial class DownloadManager
{
    /// <summary>Checks one local file without network access or modifications, using the same checks as download reuse.</summary>
    public async Task<FileVerificationResult> VerifyAsync(DownloadRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var (_, path) = Prepare(request);
        try
        {
            DownloadPath.CheckLinks(path);
            var state = !File.Exists(path) ? FileVerificationState.Missing
                : request.ExpectedSize is long size && new FileInfo(path).Length != size ? FileVerificationState.SizeMismatch
                : request.ExpectedHash == null && request.ExpectedSize == null ? FileVerificationState.Unverified
                : await IsExistingValidAsync(request, path, cancellationToken).ConfigureAwait(false) ? FileVerificationState.Valid
                : FileVerificationState.HashMismatch;
            return new(request.RelativePath, state);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { throw Failure(DownloadErrorCode.IoError, "本地文件校验失败。", request); }
    }
}
