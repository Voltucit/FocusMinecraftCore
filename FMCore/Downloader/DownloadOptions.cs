namespace FMCore.Downloader;

public sealed record DownloadOptions
{
    /// <summary>Shared by all calls to the same manager, including concurrent batches.</summary>
    public int MaxConcurrency { get; init; } = 8;
    /// <summary>Additional attempts after the initial request.</summary>
    public int MaxRetries { get; init; } = 3;
    /// <summary>Timeout for each complete attempt, including streaming the response body.</summary>
    public TimeSpan AttemptTimeout { get; init; } = TimeSpan.FromMinutes(2);
    public TimeSpan RetryDelay { get; init; } = TimeSpan.FromMilliseconds(250);
    public TimeSpan MaxRetryDelay { get; init; } = TimeSpan.FromSeconds(30);
}
