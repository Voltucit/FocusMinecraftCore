namespace FMCore.Downloader;

/// <summary>A download destination is always relative to the manager's root directory.</summary>
public sealed record DownloadRequest(Uri Source, string RelativePath)
{
    public string? ExpectedHash { get; init; }
    public DownloadHashAlgorithm HashAlgorithm { get; init; } = DownloadHashAlgorithm.Sha1;
    public long? ExpectedSize { get; init; }

    // Signed URLs may contain secrets, so never include Source in diagnostic output.
    public override string ToString() => $"Download: {RelativePath}";
}

public enum DownloadHashAlgorithm
{
    Sha1,
    Sha256,
    Sha512
}
