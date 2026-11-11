namespace FMCore.Downloads;

public sealed class DownloadRequest
{
    public required string Url { get; init; }

    internal string? FallbackUrl { get; init; }

    /// <summary>同一文件的备用地址；所有地址必须对应同一个大小和 SHA-1。</summary>
    public IReadOnlyList<string> FallbackUrls { get; init; } = [];

    /// <summary>根据本进程已观察到的源表现排序；关闭后严格使用提供的顺序。</summary>
    public bool UseSourcePerformance { get; init; } = true;

    public required string FilePath { get; init; }

    public required long Size { get; init; }

    public required string Sha1 { get; init; }

    public bool ReplaceInvalidFiles { get; init; }

    public TimeSpan Timeout { get; init; } =
        TimeSpan.FromMinutes(30);

    public TimeSpan HeadersTimeout { get; init; } = TimeSpan.FromSeconds(15);

    public TimeSpan IdleTimeout { get; init; } = TimeSpan.FromSeconds(20);

    /// <summary>大文件的最大分段并发；1 表示不分段。</summary>
    public int MaximumSegments { get; init; } = 4;

    public long SegmentThreshold { get; init; } = 16 * 1024 * 1024;

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

    public string SourceHost { get; init; } = "";
}

public sealed record DownloadResult(
    string FilePath,
    long Size,
    string Sha1,
    bool AlreadyPresent)
{
    public int Attempts { get; init; } = 1;

    public string? BackupPath { get; init; }

    public string SourceHost { get; init; } = "";
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
