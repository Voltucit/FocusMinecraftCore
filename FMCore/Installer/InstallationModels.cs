using FMCore.Assets;
using FMCore.Downloader;
using FMCore.Versions;

namespace FMCore.Installer;

public enum InstallationStage { Metadata, Client, Libraries, AssetIndex, AssetObjects, AssetLayout, Natives, Logging, Completed, Verification, NativeVerification }
public enum InstallationErrorCode { InvalidMetadata, MissingClient, MissingAssetIndex, MissingNativeClassifier, UnsupportedPlatform, ConflictingFiles, InvalidArchive, ArchiveLimitExceeded, UnsafePath, IoError, InstallationBusy }
public sealed class InstallationException(InstallationErrorCode code, string message) : Exception(message)
{
    public InstallationErrorCode Code { get; } = code;
}

public sealed record InstallationOptions
{
    public RuleContext Context { get; init; } = RuleContext.Current;
    /// <summary>Relative to the shared download root. Empty uses the root itself.</summary>
    public string GameDirectory { get; init; } = "";
    public VersionCachePolicy CachePolicy { get; init; } = VersionCachePolicy.PreferCache;
    public NativeExtractionOptions NativeExtraction { get; init; } = new();
    internal string? NativeDirectory { get; init; }
    /// <summary>Check extracted native files against the same validated archives used for extraction. Enabled by default.</summary>
    public bool VerifyNativeFiles { get; init; } = true;
    internal bool VerifyAssetLayouts { get; init; } = true;
}

public sealed record InstallationProgress(InstallationStage Stage, int CompletedFiles, int TotalFiles, DownloadProgress? Download = null);
public sealed record NativeArchive(string RelativePath, string? Sha1, long? Size, IReadOnlyList<string> Excludes);
public sealed record NativeExtractionProgress(int CompletedArchives, int TotalArchives);
public sealed record NativeExtractionOptions
{
    public int MaxEntries { get; init; } = 10_000;
    public long MaxEntryBytes { get; init; } = 256L * 1024 * 1024;
    public long MaxTotalBytes { get; init; } = 1024L * 1024 * 1024;
}

/// <summary>Relative paths share the downloader root. Classpath includes ordinary artifacts (including modern native JARs), then the client.</summary>
public sealed record InstallationPlan(ResolvedVersion Version, RuleContext Context, string GameDirectory, string NativeDirectory,
    DownloadRequest Client, IReadOnlyList<DownloadRequest> Libraries, IReadOnlyList<string> ClasspathEntries,
    IReadOnlyList<NativeArchive> NativeArchives, AssetIndexInfo AssetIndex, DownloadRequest? Logging);

public sealed record InstallationResult(InstallationPlan Plan, string ClientJarPath, IReadOnlyList<string> ClasspathEntries,
    string NativeDirectory, AssetLayoutResult Assets, string? LoggingPath, int DownloadedFiles, int SkippedFiles, long DownloadedBytes);
