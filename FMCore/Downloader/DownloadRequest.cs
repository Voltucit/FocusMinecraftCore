namespace FMCore.Downloader;

/// <summary>A download destination is always relative to the manager's root directory.</summary>
public sealed record DownloadRequest(Uri Source, string RelativePath)
{
    public string? ExpectedHash { get; init; }
    public DownloadHashAlgorithm HashAlgorithm { get; init; } = DownloadHashAlgorithm.Sha1;
    public long? ExpectedSize { get; init; }
    /// <summary>Verify/reuse an already generated local file; requires ExpectedHash and never performs a network request.</summary>
    public bool LocalOnly { get; init; }
    // Existing third-party files may lack publisher hashes. Only internal metadata planning can opt in;
    // public LocalOnly requests still require a hash. Unverified is reported honestly by VerifyAsync.
    internal bool AllowUnverifiedLocal { get; init; }
    internal bool PreserveExisting { get; init; }
    internal IReadOnlyList<string> AlternateHashes { get; init; } = [];

    // Signed URLs may contain secrets, so never include Source in diagnostic output.
    public override string ToString() => $"Download: {RelativePath}";
}

public enum DownloadHashAlgorithm
{
    Sha1,
    Sha256,
    Sha512
}
