using FMCore.Downloader;
using FMCore.Installer;

namespace FMCore.Loaders;

/// <summary>A Minecraft version listed by Fabric Meta; Stable distinguishes releases from snapshots.</summary>
public sealed record FabricGameVersion(string Version, bool Stable);

/// <summary>A loader compatible with the queried Minecraft version, in the official service's order.</summary>
public sealed record FabricLoaderVersion(string Version, bool Stable, string IntermediaryVersion);

/// <summary>Optional Fabric profile ID and the existing platform/cache/instance installation options.</summary>
public sealed record FabricInstallOptions
{
    /// <summary>Defaults to fabric-loader-{loaderVersion}-{minecraftVersion}; existing different profiles are never overwritten.</summary>
    public string? VersionId { get; init; }
    /// <summary>Reuse vanilla installation settings. CachePolicy applies to both Fabric and Minecraft metadata.</summary>
    public InstallationOptions Installation { get; init; } = new();
}

/// <summary>Fabric preparation stages; Installing carries the existing detailed installation progress.</summary>
public enum FabricInstallationStage { Metadata, Checksums, Installing, Completed }

/// <summary>Preparation counts, metadata/checksum downloads and the reused game installation progress.</summary>
public sealed record FabricInstallationProgress(FabricInstallationStage Stage, int CompletedFiles, int TotalFiles,
    InstallationProgress? Installation = null, DownloadProgress? Download = null);

/// <summary>A completed client installation. Pass VersionId to GameCoreConfig or InstanceManager.</summary>
public sealed record FabricInstallationResult(string MinecraftVersion, string LoaderVersion, string VersionId,
    InstallationResult Installation);

/// <summary>Loader-specific failures; download, version and vanilla installation failures retain their existing error types.</summary>
public enum FabricErrorCode
{
    InvalidMetadata, UnsupportedFormat, UnsupportedCombination, UnsupportedJavaRequirement,
    ProfileConflict, NotCached, InstallationBusy, UnsafePath, IoError
}

/// <summary>Sanitized Fabric failure; excludes remote bodies, URLs and inner exception messages.</summary>
public sealed class FabricException : Exception
{
    /// <summary>Machine-readable failure reason.</summary>
    public FabricErrorCode Code { get; }
    internal FabricException(FabricErrorCode code, string message) : base(message) => Code = code;
}
