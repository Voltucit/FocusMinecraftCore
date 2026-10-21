namespace FMCore.Downloader;

/// <summary>Attempts is zero when a previously verified file was skipped.</summary>
public sealed record DownloadResult(string FullPath, long Size, int Attempts, bool WasSkipped);
