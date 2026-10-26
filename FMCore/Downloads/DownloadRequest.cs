namespace FMCore.Downloads;

public sealed class DownloadRequest
{
    public required string Url { get; init; }

    public required string FilePath { get; init; }

    public required long Size { get; init; }

    public required string Sha1 { get; init; }

    public TimeSpan Timeout { get; init; } =
        TimeSpan.FromMinutes(5);
}

public sealed record DownloadProgress(
    string FilePath,
    long DownloadedBytes,
    long TotalBytes)
{
    public double Percentage =>
        TotalBytes == 0
            ? 100
            : DownloadedBytes * 100d / TotalBytes;
}

public sealed record DownloadResult(
    string FilePath,
    long Size,
    string Sha1,
    bool AlreadyPresent);
