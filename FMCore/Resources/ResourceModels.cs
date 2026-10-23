using System.Diagnostics;
using FMCore.Downloader;

namespace FMCore.Resources;

/// <summary>Target Minecraft/loader pair. Automatic selection uses releases unless IncludePrereleases is enabled.</summary>
public sealed record ModEnvironment(string MinecraftVersion, string Loader)
{
    /// <summary>Allow beta/alpha versions during automatic selection. An explicitly requested version may use any channel.</summary>
    public bool IncludePrereleases { get; init; }
}

/// <summary>Modrinth search filters; search results still need per-version compatibility checks before installation.</summary>
public sealed record ModrinthSearchOptions
{
    /// <summary>Optional exact Minecraft version.</summary>
    public string? MinecraftVersion { get; init; }
    /// <summary>Optional loader category, for example fabric, forge or neoforge.</summary>
    public string? Loader { get; init; }
    /// <summary>mod (default), modpack, resourcepack or shader. Only mod installation is implemented.</summary>
    public string ProjectType { get; init; } = "mod";
    /// <summary>relevance, downloads, follows, newest or updated.</summary>
    public string Sort { get; init; } = "relevance";
    /// <summary>Number of hits, 1–100.</summary>
    public int Limit { get; init; } = 20;
    /// <summary>Pagination offset.</summary>
    public int Offset { get; init; }
}

/// <summary>Public project information, without authentication or raw response data.</summary>
public sealed record ModrinthProject(string Id, string Slug, string Title, string Description, string ProjectType)
{
    /// <summary>官方 icon_url；缺失或地址不安全时为 null。SDK 不请求图片。</summary>
    public Uri? IconUrl { get; init; }
    /// <summary>详情中的图片图库；搜索响应未提供图库时为空。</summary>
    public IReadOnlyList<ResourceImage> Gallery { get; init; } = [];
}
/// <summary>平台提供的展示图片。图片显示、下载及界面缓存由宿主负责，不附加认证信息。</summary>
public sealed record ResourceImage(Uri Url)
{
    /// <summary>平台提供的缩略图，可为 null。</summary>
    public Uri? ThumbnailUrl { get; init; }
    /// <summary>图片标题，可为 null。</summary>
    public string? Title { get; init; }
    /// <summary>图片说明，可为 null。</summary>
    public string? Description { get; init; }
    /// <summary>平台图片 ID；仅部分平台提供。</summary>
    public int? Id { get; init; }
    /// <summary>Modrinth 推荐展示标记。</summary>
    public bool Featured { get; init; }
    /// <summary>Modrinth 图片创建时间。</summary>
    public DateTimeOffset? CreatedAt { get; init; }
    /// <summary>Modrinth 图片排序值。</summary>
    public int? Ordering { get; init; }
    /// <summary>避免诊断输出包含图片地址的查询字符串。</summary>
    public override string ToString() => "Resource image";
}
/// <summary>One search page.</summary>
public sealed record ModrinthSearchResult(IReadOnlyList<ModrinthProject> Hits, int Offset, int Limit, int TotalHits);
/// <summary>Dependency semantics published by Modrinth.</summary>
public enum ModDependencyType
{
    /// <summary>Must be installed.</summary>
    Required,
    /// <summary>Not automatically installed.</summary>
    Optional,
    /// <summary>Cannot coexist with the referenced project/version.</summary>
    Incompatible,
    /// <summary>Already bundled by the publisher; not downloaded separately.</summary>
    Embedded
}
/// <summary>A dependency may identify a project, a pinned version, or an external file that cannot be resolved automatically.</summary>
public sealed record ModDependency(string? ProjectId, string? VersionId, string? FileName, ModDependencyType Type);
/// <summary>Publisher-provided downloadable artifact. Url can be signed and is omitted from diagnostic output.</summary>
[DebuggerDisplay("Modrinth artifact")]
public sealed record ModrinthFile(string FileName, Uri Url, string Sha512, string? Sha1, long Size, bool Primary, string? FileType)
{
    /// <summary>Does not expose download addresses.</summary>
    public override string ToString() => "Modrinth artifact";
}
/// <summary>Version metadata retained for dependency resolution, compatibility and updates.</summary>
public sealed record ModrinthVersion(string Id, string ProjectId, string Name, string VersionNumber, string VersionType,
    DateTimeOffset PublishedAt, IReadOnlyList<string> GameVersions, IReadOnlyList<string> Loaders,
    IReadOnlyList<ModrinthFile> Files, IReadOnlyList<ModDependency> Dependencies);
/// <summary>Persisted ownership receipt. Only these exact files/hashes may be replaced; no download URL or credential is stored.</summary>
public sealed record InstalledMod(string ProjectId, string VersionId, string VersionNumber, DateTimeOffset PublishedAt,
    string FileName, string Sha512, long Size, DateTimeOffset InstalledAt, IReadOnlyList<ModDependency> Dependencies)
{
    /// <summary>Origin platform identifier: modrinth (default) or curseforge. Installers assign it; editing a returned snapshot does not change ownership.</summary>
    public string Source { get; init; } = "modrinth";
}
/// <summary>A newer compatible version; checking does not change any files.</summary>
public sealed record ModUpdate(InstalledMod Installed, ModrinthVersion Available);
/// <summary>Successfully committed dependency closure; Reused indicates that no files or receipt changed.</summary>
public sealed record ModInstallResult(IReadOnlyList<InstalledMod> Installed, bool Reused);
/// <summary>Resource operation phases. File-level progress is provided during Downloading.</summary>
public enum ResourceStage
{
    /// <summary>Resolving metadata, required dependencies and conflicts.</summary>
    Resolving,
    /// <summary>Downloading into a private staging area.</summary>
    Downloading,
    /// <summary>Committing verified files and their ownership receipt.</summary>
    Committing,
    /// <summary>Operation completed.</summary>
    Completed
}
/// <summary>Stage and optional downloader progress. Callbacks may run on a worker thread.</summary>
public sealed record ResourceProgress(ResourceStage Stage, DownloadProgress? Download = null);
/// <summary>Structured resource failure category.</summary>
public enum ResourceErrorCode
{
    /// <summary>Invalid query, environment or identifier.</summary>
    InvalidRequest,
    /// <summary>Project/version not found.</summary>
    NotFound,
    /// <summary>HTTP failure without disclosing the response body or address.</summary>
    HttpError,
    /// <summary>Rate limited. RetryAfter may be available.</summary>
    RateLimited,
    /// <summary>Request exceeded its deadline.</summary>
    Timeout,
    /// <summary>Transport failed.</summary>
    NetworkError,
    /// <summary>Invalid or oversized platform response.</summary>
    InvalidMetadata,
    /// <summary>Unsupported resource kind, loader or ambiguous artifact format.</summary>
    Unsupported,
    /// <summary>Version does not support the target Minecraft/loader pair.</summary>
    Incompatible,
    /// <summary>Unresolvable dependency, cycle, depth/node limit or contradictory pinned versions.</summary>
    DependencyConflict,
    /// <summary>Conflicting project, filename or user-modified/unmanaged file.</summary>
    Conflict,
    /// <summary>Unsafe path, filename, symbolic link or reparse point.</summary>
    UnsafePath,
    /// <summary>The instance is running/preparing or another operation holds its lock.</summary>
    Busy,
    /// <summary>Ownership receipt is corrupt, unsupported or for a different environment.</summary>
    StorageCorrupted,
    /// <summary>Interrupted transaction needs recovery; original backups are retained.</summary>
    RecoveryRequired,
    /// <summary>Verified downloader failed; see DownloadError.</summary>
    DownloadFailed,
    /// <summary>Local file operation failed.</summary>
    IoError,
    /// <summary>Platform policy, unavailable URL or protected content prevents automatic downloading.</summary>
    DownloadRestricted,
    /// <summary>The caller-provided platform API key is invalid or lacks access.</summary>
    AuthenticationFailed,
    /// <summary>A pack-managed Mod or unconfirmed pack transaction requires explicit ModpackInstaller authorization.</summary>
    ModpackLocked
}
/// <summary>Sanitized resource error. Cancellation remains OperationCanceledException.</summary>
public sealed class ResourceException(ResourceErrorCode code, string message) : Exception(message)
{
    /// <summary>Machine-readable category.</summary>
    public ResourceErrorCode Code { get; } = code;
    /// <summary>HTTP status when relevant.</summary>
    public int? StatusCode { get; init; }
    /// <summary>Suggested wait after an HTTP 429 response; the SDK does not silently retry API calls.</summary>
    public TimeSpan? RetryAfter { get; init; }
    /// <summary>Downloader failure category, without an unsanitized inner exception.</summary>
    public DownloadErrorCode? DownloadError { get; init; }
}
