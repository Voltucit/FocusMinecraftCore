using System.Text.Json.Serialization;

namespace FMCore.ModsResources;

public enum ModrinthResourceType { Mod, ResourcePack, DataPack, Shader, Modpack }
public enum ModrinthSearchSort { Relevance, Downloads, Follows, Newest, Updated }

public sealed record ModrinthSearchOptions
{
    public string Query { get; init; } = "";
    public ModrinthResourceType ResourceType { get; init; } = ModrinthResourceType.Mod;
    public string? GameVersion { get; init; }
    public string? Loader { get; init; }
    public IReadOnlyList<string> Categories { get; init; } = [];
    public ModrinthSearchSort Sort { get; init; } = ModrinthSearchSort.Relevance;
    public int Offset { get; init; }
    public int Limit { get; init; } = 20;
}

public sealed record ModrinthResourceTypeInfo(
    ModrinthResourceType Type, string Id, string DisplayName);
public sealed record ModrinthLabel(string Id, string DisplayName);

public sealed record ModrinthCategory
{
    public required string Name { get; init; }
    public required string ProjectType { get; init; }
    public required string Header { get; init; }
    public string Icon { get; init; } = "";

    [JsonIgnore] public string Id => Name;
    [JsonIgnore] public string DisplayName => ModrinthLocalization.GetCategoryName(Name);
    [JsonIgnore] public string GroupName => ModrinthLocalization.GetGroupName(Header);
}

public sealed record ModrinthSearchResult
{
    public required IReadOnlyList<ModrinthSearchHit> Hits { get; init; }
    public int Offset { get; init; }
    public int Limit { get; init; }
    public long TotalHits { get; init; }
}

public sealed record ModrinthSearchHit
{
    public required string ProjectId { get; init; }
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public string ProjectType { get; init; } = "";
    public IReadOnlyList<string> AllProjectTypes { get; init; } = [];
    public string Description { get; init; } = "";
    public string? Author { get; init; }
    public string? AuthorId { get; init; }
    public string? Organization { get; init; }
    public string? OrganizationId { get; init; }
    public long Downloads { get; init; }
    public long Follows { get; init; }
    public required DateTimeOffset DateCreated { get; init; }
    public required DateTimeOffset DateModified { get; init; }
    public string? IconUrl { get; init; }
    public string? FeaturedGallery { get; init; }
    public IReadOnlyList<string> Gallery { get; init; } = [];
    public IReadOnlyList<string> Categories { get; init; } = [];
    public IReadOnlyList<string> DisplayCategories { get; init; } = [];
    public IReadOnlyList<string> Versions { get; init; } = [];
    public string? LatestVersion { get; init; }
    public string? License { get; init; }
    public string? ClientSide { get; init; }
    public string? ServerSide { get; init; }

    [JsonIgnore] public long Followers => Follows;
    [JsonIgnore] public DateTimeOffset PublishedAt => DateCreated;
    [JsonIgnore] public DateTimeOffset UpdatedAt => DateModified;
    [JsonIgnore] public IReadOnlyList<ModrinthLabel> Tags =>
        ModrinthLocalization.Labels(Categories.Concat(DisplayCategories));
    [JsonIgnore] public string? PreviewImageUrl =>
        new[] { FeaturedGallery }.Concat(Gallery).Append(IconUrl)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
}

public sealed record ModrinthProject
{
    public required string Id { get; init; }
    public required string Slug { get; init; }
    public required string Title { get; init; }
    public string ProjectType { get; init; } = "";
    public string Description { get; init; } = "";
    public string Body { get; init; } = "";
    public required string Team { get; init; }
    public required DateTimeOffset Published { get; init; }
    public required DateTimeOffset Updated { get; init; }
    public long Downloads { get; init; }
    public long Followers { get; init; }
    public string? IconUrl { get; init; }
    public IReadOnlyList<ModrinthGalleryImage> Gallery { get; init; } = [];
    public IReadOnlyList<string> Categories { get; init; } = [];
    public IReadOnlyList<string> AdditionalCategories { get; init; } = [];
    public IReadOnlyList<string> GameVersions { get; init; } = [];
    public IReadOnlyList<string> Loaders { get; init; } = [];
    public IReadOnlyList<string> Versions { get; init; } = [];
    public ModrinthLicense? License { get; init; }
    public string? SourceUrl { get; init; }
    public string? IssuesUrl { get; init; }
    public string? WikiUrl { get; init; }
    public string? DiscordUrl { get; init; }
    public string? ClientSide { get; init; }
    public string? ServerSide { get; init; }

    [JsonIgnore] public IReadOnlyList<ModrinthTeamMember> TeamMembers { get; init; } = [];
    [JsonIgnore] public IReadOnlyList<ModrinthUser> Authors =>
        TeamMembers.Where(member => member.Accepted).Select(member => member.User).ToArray();
    [JsonIgnore] public DateTimeOffset PublishedAt => Published;
    [JsonIgnore] public DateTimeOffset UpdatedAt => Updated;
    [JsonIgnore] public IReadOnlyList<ModrinthLabel> Tags =>
        ModrinthLocalization.Labels(Categories.Concat(AdditionalCategories));
}

public sealed record ModrinthGalleryImage
{
    public required string Url { get; init; }
    public string? RawUrl { get; init; }
    public bool Featured { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTimeOffset Created { get; init; }
    public int Ordering { get; init; }
    [JsonIgnore] public string OriginalImageUrl => RawUrl ?? Url;
}

public sealed record ModrinthLicense
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Url { get; init; }
}

public sealed record ModrinthUser
{
    public required string Id { get; init; }
    public required string Username { get; init; }
    public string? Name { get; init; }
    public string? AvatarUrl { get; init; }
    [JsonIgnore] public string DisplayName => Name ?? Username;
}

public sealed record ModrinthTeamMember
{
    public required ModrinthUser User { get; init; }
    public string Role { get; init; } = "";
    public bool Accepted { get; init; } = true;
}

public sealed record ModrinthVersion
{
    public required string Id { get; init; }
    public required string ProjectId { get; init; }
    public required string Name { get; init; }
    public required string VersionNumber { get; init; }
    public string VersionType { get; init; } = "";
    public required DateTimeOffset DatePublished { get; init; }
    public long Downloads { get; init; }
    public string? Changelog { get; init; }
    public IReadOnlyList<string> GameVersions { get; init; } = [];
    public IReadOnlyList<string> Loaders { get; init; } = [];
    public required IReadOnlyList<ModrinthFile> Files { get; init; }
    public IReadOnlyList<ModrinthDependency> Dependencies { get; init; } = [];

    [JsonIgnore] public DateTimeOffset PublishedAt => DatePublished;
    [JsonIgnore] public string VersionTypeName =>
        ModrinthLocalization.GetVersionTypeName(VersionType);
    [JsonIgnore] public ModrinthFile? PrimaryFile =>
        Files.FirstOrDefault(file => file.Primary) ?? Files.FirstOrDefault();
}

public sealed record ModrinthFile
{
    public required string Url { get; init; }
    public required string Filename { get; init; }
    public required long Size { get; init; }
    public required ModrinthFileHashes Hashes { get; init; }
    public bool Primary { get; init; }
    public string? FileType { get; init; }
}

public sealed record ModrinthFileHashes
{
    public string? Sha1 { get; init; }
    public string? Sha512 { get; init; }
}

public sealed record ModrinthDependency
{
    public string? ProjectId { get; init; }
    public string? VersionId { get; init; }
    public string? FileName { get; init; }
    public string DependencyType { get; init; } = "";
}

