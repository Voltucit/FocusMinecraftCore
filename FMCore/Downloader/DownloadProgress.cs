namespace FMCore.Downloader;

/// <summary>Per-file progress. BytesReceived resets to zero at the start of each attempt.</summary>
public sealed record DownloadProgress(
    string RelativePath,
    long BytesReceived,
    long? TotalBytes,
    int Attempt,
    DownloadState State);

public enum DownloadState
{
    Started,
    Downloading,
    Retrying,
    Completed,
    Skipped
}
