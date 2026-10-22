using System.Diagnostics;

namespace FMCore.Resources;

/// <summary>Search filters for Minecraft (game ID 432). Defaults to the Mods class and popularity sorting.</summary>
public sealed record CurseForgeSearchOptions
{
    /// <summary>Exact Minecraft version, when specified.</summary>
    public string? MinecraftVersion { get; init; }
    /// <summary>fabric, forge or neoforge; null searches all loaders.</summary>
    public string? Loader { get; init; }
    /// <summary>CurseForge class ID; 6 = Mods. Other classes may be queried but not installed by the Mod installer.</summary>
    public int ClassId { get; init; } = 6;
    /// <summary>Optional category ID.</summary>
    public int? CategoryId { get; init; }
    /// <summary>Official sort field, 1–12; 2 = popularity.</summary>
    public int SortField { get; init; } = 2;
    /// <summary>asc or desc (default).</summary>
    public string SortOrder { get; init; } = "desc";
    /// <summary>Zero-based pagination offset.</summary>
    public int Index { get; init; }
    /// <summary>Results per page, 1–50. Index + PageSize cannot exceed 10,000.</summary>
    public int PageSize { get; init; } = 20;
}
/// <summary>Project metadata including platform distribution policy. Null AllowModDistribution means unspecified, not an explicit permission override.</summary>
public sealed record CurseForgeProject(int Id, int GameId, string Name, string Slug, string Summary, int? ClassId,
    bool? AllowModDistribution, bool IsAvailable);
/// <summary>One project-search page.</summary>
public sealed record CurseForgeSearchResult(IReadOnlyList<CurseForgeProject> Mods, int Index, int PageSize, int TotalCount);
/// <summary>Original CurseForge dependency relation values.</summary>
public enum CurseForgeRelationType
{
    /// <summary>Already embedded; not downloaded separately.</summary>
    EmbeddedLibrary = 1,
    /// <summary>Optional runtime dependency; not installed automatically.</summary>
    OptionalDependency = 2,
    /// <summary>Required runtime dependency.</summary>
    RequiredDependency = 3,
    /// <summary>Related tool; not installed as a runtime Mod.</summary>
    Tool = 4,
    /// <summary>Cannot coexist with this project.</summary>
    Incompatible = 5,
    /// <summary>Included content; not installed separately.</summary>
    Include = 6
}
/// <summary>CurseForge identifies dependencies by project, without a pinned file version.</summary>
public sealed record CurseForgeDependency(int ModId, CurseForgeRelationType RelationType);
/// <summary>File metadata. Missing DownloadUrl is retained for queries and explicitly restricts automatic installation.</summary>
[DebuggerDisplay("CurseForge file")]
public sealed record CurseForgeFile(int Id, int ModId, int GameId, string DisplayName, string FileName, int ReleaseType,
    DateTimeOffset PublishedAt, long Size, string? Sha1, string? Md5, Uri? DownloadUrl, bool IsAvailable,
    bool IsServerPack, bool IsEarlyAccessContent, bool ExposeAsAlternative, IReadOnlyList<string> GameVersions,
    IReadOnlyList<CurseForgeDependency> Dependencies)
{
    /// <summary>Does not reveal download addresses.</summary>
    public override string ToString() => "CurseForge file";
}
/// <summary>One file page, locally filtered when an environment is supplied. Pagination describes the original API page.</summary>
public sealed record CurseForgeFilePage(IReadOnlyList<CurseForgeFile> Files, int Index, int PageSize, int TotalCount, int ResultCount);
/// <summary>A newer compatible file; dependency/conflict checks are repeated during UpdateAsync.</summary>
public sealed record CurseForgeUpdate(InstalledMod Installed, CurseForgeFile Available);
