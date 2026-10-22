using FMCore.Downloader;
using FMCore.Installer;
using FMCore.Java;

namespace FMCore.Loaders;

/// <summary>A Forge or NeoForge Maven release for one Minecraft version.</summary>
public sealed record MavenLoaderVersion(string MinecraftVersion, string Version, string MavenVersion, bool IsPrerelease);

/// <summary>Shared optional installation settings for installer-based loaders.</summary>
public sealed record LoaderInstallOptions
{
    /// <summary>Optional safe profile ID. Different existing profiles are never overwritten.</summary>
    public string? VersionId { get; init; }
    /// <summary>Existing platform, instance, cache and extraction settings.</summary>
    public InstallationOptions Installation { get; init; } = new();
    /// <summary>Optional installer Java executable/home; actually probed and checked before execution.</summary>
    public string? JavaPath { get; init; }
    /// <summary>Bounded Java discovery settings for installer-based loaders.</summary>
    public JavaDetectionOptions JavaDetection { get; init; } = new();
    /// <summary>Maximum installer process duration, default ten minutes; cancellation also terminates the process tree.</summary>
    public TimeSpan InstallerTimeout { get; init; } = TimeSpan.FromMinutes(10);
}
/// <summary>Loader preparation stages. Installation includes existing file and byte progress.</summary>
public enum LoaderInstallationStage { Metadata, InstallerDownload, Dependencies, Processors, Verification, Completed }
/// <summary>Current phase and optional existing install/download progress; Message never contains raw installer output.</summary>
public sealed record LoaderInstallationProgress(LoaderInstallationStage Stage, int CompletedFiles = 0, int TotalFiles = 0,
    InstallationProgress? Installation = null, DownloadProgress? Download = null, string? Message = null);
/// <summary>Completed loader installation; pass VersionId to the existing launch or instance API.</summary>
public sealed record LoaderInstallationResult(string Loader, string MinecraftVersion, string LoaderVersion, string VersionId,
    InstallationResult Installation, int? InstallerExitCode = null);
/// <summary>Structured loader-specific failures; existing download/Java/version/install failures retain their own types.</summary>
public enum LoaderErrorCode
{
    InvalidMetadata, UnsupportedFormat, UnsupportedCombination, ProfileConflict, NotCached, InstallationBusy,
    UnsafePath, IoError, InvalidArchive, ProcessFailed, ProcessTimeout, MissingOutput, HashMismatch, DownloadUnavailable
}
/// <summary>Sanitized failure with no raw service/installer output or credential-bearing inner exception.</summary>
public sealed class LoaderException : Exception
{
    /// <summary>Machine-readable reason.</summary>
    public LoaderErrorCode Code { get; }
    /// <summary>Actual installer exit code when available, including zero for missing/invalid output after an apparent success.</summary>
    public int? ExitCode { get; }
    internal LoaderException(LoaderErrorCode code, string message, int? exitCode = null) : base(message)
    { Code = code; ExitCode = exitCode; }
}
