namespace FMCore.Downloads;

public sealed class DownloadRequest
{
    public required string Url { get; init; }

    internal string? FallbackUrl { get; init; }

    public required string FilePath { get; init; }

    public required long Size { get; init; }

    public required string Sha1 { get; init; }

    public bool ReplaceInvalidFiles { get; init; }

    public TimeSpan Timeout { get; init; } =
        TimeSpan.FromMinutes(5);

    public int MaxRetries { get; init; } = 2;

    public TimeSpan RetryDelay { get; init; } =
        TimeSpan.FromSeconds(1);
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

    public int Attempt { get; init; } = 1;

    public bool AlreadyPresent { get; init; }

    public long TransferredBytes { get; init; }

    public double BytesPerSecond { get; init; }

    public TimeSpan? RemainingTime { get; init; }
}

public sealed record DownloadResult(
    string FilePath,
    long Size,
    string Sha1,
    bool AlreadyPresent)
{
    public int Attempts { get; init; } = 1;

    public string? BackupPath { get; init; }
}

public sealed record DownloadBatchProgress(
    int TotalFiles,
    int CompletedFiles,
    int ActiveFiles,
    int PeakActiveFiles,
    long ProcessedBytes,
    long TotalBytes)
{
    public double Percentage =>
        TotalBytes == 0
            ? CompletedFiles == TotalFiles ? 100 : 0
            : ProcessedBytes * 100d / TotalBytes;

    public long TransferredBytes { get; init; }

    public double BytesPerSecond { get; init; }

    public TimeSpan? RemainingTime { get; init; }
}
