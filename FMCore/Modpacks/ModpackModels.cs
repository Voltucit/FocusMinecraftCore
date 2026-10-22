using FMCore.Downloader;
using FMCore.Installer;
using FMCore.Resources;

namespace FMCore.Modpacks;

/// <summary>Client modpack formats supported on Windows.</summary>
public enum ModpackFormat { Modrinth, CurseForge }
/// <summary>One exact file reference; an override has no download URL. Paths are relative to the instance.</summary>
public sealed record ModpackFile(string Path, long Size, string? Sha512, string? Sha1,
    IReadOnlyList<Uri> Downloads, bool Optional = false, int? ProjectId = null, int? FileId = null, bool IsOverride = false);
/// <summary>Archive metadata, exact game/loader requirements and selected client files. No files have been installed.</summary>
public sealed record ModpackInfo(ModpackFormat Format, string Name, string Version, string MinecraftVersion,
    string? Loader, string? LoaderVersion, IReadOnlyList<ModpackFile> Files)
{
    /// <summary>Safe relative paths whose platform labels or dependency version pins differ from the pack selection. Exact pack versions remain authoritative by default.</summary>
    public IReadOnlyList<string> CompatibilityWarnings { get; init; } = [];
}
/// <summary>Options for an isolated instance. Runtime installation is enabled by default.</summary>
public sealed record ModpackOptions
{
    /// <summary>Install the exact runtime using existing installers. False explicitly installs pack content only; it does not verify launch readiness.</summary>
    public bool InstallRuntime { get; init; } = true;
    /// <summary>Optional Java path for Forge/NeoForge processors.</summary>
    public string? JavaPath { get; init; }
    /// <summary>Include optional client files; defaults to false.</summary>
    public bool IncludeOptionalFiles { get; init; }
    /// <summary>Reject platform compatibility labels/dependency version pins that differ from the pack. False trusts the pack's exact selection and reports CompatibilityWarnings.</summary>
    public bool RequireDeclaredModCompatibility { get; init; }
    /// <summary>Keep locally modified/unowned configuration files instead of overwriting them.</summary>
    public bool PreserveModifiedConfigurations { get; init; } = true;
    /// <summary>Delete this update's old backups after explicit confirmation, never merely after download/process creation.</summary>
    public bool AutoCleanupAfterConfirmation { get; init; } = true;
    /// <summary>Optional non-secret origin identifier recorded with the pack, such as a project slug; URLs and credentials are forbidden.</summary>
    public string? SourceId { get; init; }
    /// <summary>Caller-owned Modrinth metadata connection. Defaults to the standard client.</summary>
    public ModrinthClient? Modrinth { get; init; }
    /// <summary>Caller-owned CurseForge client containing the caller's legal API key; required for CurseForge manifests.</summary>
    public CurseForgeClient? CurseForge { get; init; }
    /// <summary>Caller-owned credential-free file/runtime connection. API keys must never be configured on it.</summary>
    public HttpClient? DownloadClient { get; init; }
    /// <summary>Existing downloader concurrency/retry/timeouts.</summary>
    public DownloadOptions Downloads { get; init; } = new();
}
/// <summary>Planned file action. Preserved configuration files are no longer considered pack-owned.</summary>
public enum ModpackChangeKind { Add, Delete, Replace, Conflict, Preserve }
/// <summary>Safe relative file path and the reason for the planned action.</summary>
public sealed record ModpackChange(string Path, ModpackChangeKind Kind, string Reason);
/// <summary>Read-only plan bound to the observed pack revision; UpdateAsync recomputes it while holding the instance lock.</summary>
public sealed record ModpackUpdatePlan(Guid? CurrentRevision, ModpackInfo Target, IReadOnlyList<ModpackChange> Changes)
{
    /// <summary>False when a managed Mod was changed or an unknown destination would be overwritten.</summary>
    public bool CanApply => Changes.All(c => c.Kind != ModpackChangeKind.Conflict);
}
/// <summary>Pack operation phases.</summary>
public enum ModpackStage { Reading, Resolving, Runtime, Downloading, BackingUp, Committing, Completed }
/// <summary>Progress with reused downloader/runtime details; callbacks can run on background threads.</summary>
public sealed record ModpackProgress(ModpackStage Stage, int CompletedFiles = 0, int TotalFiles = 0,
    DownloadProgress? Download = null, InstallationProgress? Installation = null);
/// <summary>Installed state, pending explicit confirmation and exact current content. Individual Mod changes set IsModified.</summary>
public sealed record InstalledModpack(Guid Revision, ModpackInfo Pack, string? SourceId, DateTimeOffset InstalledAt,
    Guid? PendingUpdateId, bool RuntimeInstalled, bool IsModified);
/// <summary>A completed transaction. An update ID identifies only this instance's backup set.</summary>
public sealed record ModpackInstallResult(InstalledModpack Installed, Guid UpdateId, bool Reused);
/// <summary>Cleanup failures are reported without changing active game files. Retry CleanupAsync with the same update ID.</summary>
public sealed record ModpackCleanupResult(Guid UpdateId, int DeletedFiles, IReadOnlyList<string> FailedPaths);
/// <summary>Export references pinned downloads by default. Local configs are excluded unless explicitly selected.</summary>
public sealed record ModpackExportOptions
{
    /// <summary>Include unchanged pack-owned configuration/override files. No saves, account data or unknown user files are exported.</summary>
    public bool IncludeOverrides { get; init; }
    /// <summary>Explicit caller assurance that bundled binary override files may be redistributed.</summary>
    public bool AllowBinaryOverrides { get; init; }
}
/// <summary>Structured modpack failures. Cancellation remains OperationCanceledException.</summary>
public enum ModpackErrorCode
{
    InvalidRequest, InvalidArchive, ArchiveLimitExceeded, Unsupported, UnsafePath, Conflict, Busy,
    NotFound, DownloadFailed, DownloadRestricted, DependencyConflict, RecoveryRequired, StorageCorrupted,
    ConfirmationRequired, UpdateLocked, RuntimeFailed, IoError
}
/// <summary>Sanitized failure; no source URLs, API keys, remote response bodies or inner exceptions.</summary>
public sealed class ModpackException(ModpackErrorCode code, string message) : Exception(message)
{
    /// <summary>Machine-readable category.</summary>
    public ModpackErrorCode Code { get; } = code;
    /// <summary>Underlying resource category, when an existing resource operation fails.</summary>
    public ResourceErrorCode? ResourceError { get; init; }
    /// <summary>Underlying downloader category, when applicable.</summary>
    public DownloadErrorCode? DownloadError { get; init; }
}
