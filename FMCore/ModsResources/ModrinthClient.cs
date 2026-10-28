using System.Net;
using System.Net.Http;
using System.Text.Json;
using FMCore.Downloads;

namespace FMCore.ModsResources;

public sealed class ModrinthClient
{
    private const string ApiRoot = "https://api.modrinth.com/v2/";
    private readonly string _userAgent;

    private static readonly HttpClient Client = new()
    {
        Timeout = TimeSpan.FromSeconds(30),
        MaxResponseContentBufferSize = 8 * 1024 * 1024
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        RespectNullableAnnotations = true
    };

    public DownloadSource Source { get; init; } = DownloadSource.Official;

    public ModrinthClient(
        string userAgent = "FocusMinecraftCore/2.0.1 (+https://github.com/Voltucit/FocusMinecraftCore)")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userAgent);
        using HttpRequestMessage probe = new();
        probe.Headers.UserAgent.ParseAdd(userAgent);
        _userAgent = userAgent;
    }

    public static IReadOnlyList<ModrinthResourceTypeInfo> GetResourceTypes()
        => Array.AsReadOnly(Enum.GetValues<ModrinthResourceType>()
            .Select(type => new ModrinthResourceTypeInfo(
                type, ModrinthLocalization.ApiType(type),
                ModrinthLocalization.GetResourceTypeName(type))).ToArray());

    public async Task<IReadOnlyList<ModrinthCategory>> GetCategoriesAsync(
        ModrinthResourceType resourceType,
        CancellationToken cancellationToken = default)
    {
        string type = resourceType == ModrinthResourceType.DataPack
            ? "mod" : ModrinthLocalization.ApiType(resourceType);

        var categories = await GetAsync<ModrinthCategory[]>(
            "tag/category", cancellationToken).ConfigureAwait(false);

        return Array.AsReadOnly(categories
            .Where(category => category.ProjectType == type)
            .OrderBy(category => category.Header, StringComparer.Ordinal)
            .ThenBy(category => category.Name, StringComparer.Ordinal).ToArray());
    }

    public async Task<ModrinthSearchResult> SearchAsync(
        ModrinthSearchOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Query);
        ArgumentNullException.ThrowIfNull(options.Categories);
        ArgumentOutOfRangeException.ThrowIfNegative(options.Offset);

        if (options.Limit is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(options.Limit), "每页数量为 1 到 100。");

        string type = ModrinthLocalization.ApiType(options.ResourceType);
        string sort = options.Sort switch
        {
            ModrinthSearchSort.Relevance => "relevance",
            ModrinthSearchSort.Downloads => "downloads",
            ModrinthSearchSort.Follows => "follows",
            ModrinthSearchSort.Newest => "newest",
            ModrinthSearchSort.Updated => "updated",
            _ => throw new ArgumentOutOfRangeException(nameof(options.Sort))
        };

        List<string[]> facets = [new[] { "project_type:" + type }];

        if (options.GameVersion is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(options.GameVersion);
            facets.Add(["versions:" + options.GameVersion]);
        }

        if (options.Loader is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(options.Loader);
            facets.Add(["categories:" + options.Loader]);
        }

        foreach (string category in options.Categories)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(category);
            facets.Add(["categories:" + category]);
        }

        string path = "search?query=" + Uri.EscapeDataString(options.Query)
            + "&facets=" + Uri.EscapeDataString(JsonSerializer.Serialize(facets))
            + "&index=" + sort
            + "&offset=" + options.Offset
            + "&limit=" + options.Limit;

        return await GetAsync<ModrinthSearchResult>(
            path, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ModrinthProject> GetProjectAsync(
        string projectIdOrSlug,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectIdOrSlug);

        var project = await GetAsync<ModrinthProject>(
            "project/" + Uri.EscapeDataString(projectIdOrSlug),
            cancellationToken).ConfigureAwait(false);

        var members = await GetTeamMembersAsync(
            project.Team, cancellationToken).ConfigureAwait(false);

        return project with { TeamMembers = members };
    }

    public async Task<IReadOnlyList<ModrinthTeamMember>> GetTeamMembersAsync(
        string teamId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamId);
        return Array.AsReadOnly(await GetAsync<ModrinthTeamMember[]>(
            "team/" + Uri.EscapeDataString(teamId) + "/members",
            cancellationToken).ConfigureAwait(false));
    }

    public async Task<IReadOnlyList<ModrinthVersion>> GetVersionsAsync(
        string projectIdOrSlug,
        string? gameVersion = null,
        string? loader = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectIdOrSlug);
        string path = "project/" + Uri.EscapeDataString(projectIdOrSlug)
            + "/version?include_changelog=false";

        if (gameVersion is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(gameVersion);
            path += "&game_versions="
                + Uri.EscapeDataString(JsonSerializer.Serialize(new[] { gameVersion }));
        }

        if (loader is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(loader);
            path += "&loaders="
                + Uri.EscapeDataString(JsonSerializer.Serialize(new[] { loader }));
        }

        var versions = await GetAsync<ModrinthVersion[]>(
            path, cancellationToken).ConfigureAwait(false);

        return Array.AsReadOnly(versions
            .Where(version => gameVersion is null
                || version.GameVersions.Contains(gameVersion, StringComparer.Ordinal))
            .Where(version => loader is null
                || version.Loaders.Contains(loader, StringComparer.Ordinal))
            .OrderByDescending(version => version.DatePublished).ToArray());
    }

    public Task<ModrinthVersion> GetVersionAsync(
        string versionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versionId);
        return GetAsync<ModrinthVersion>(
            "version/" + Uri.EscapeDataString(versionId), cancellationToken);
    }

    public Task<DownloadResult> DownloadAsync(
        ModrinthFile file,
        string destinationFile,
        Action<DownloadProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(file.Hashes);
        ArgumentNullException.ThrowIfNull(Source);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(file.Hashes.Sha1))
            throw new NotSupportedException("此文件缺少 SHA-1，不能交给当前校验下载器。");

        DownloadRequest request = FileDownloader.NormalizeRequest(new DownloadRequest
        {
            Url = file.Url,
            FilePath = destinationFile,
            Size = file.Size,
            Sha1 = file.Hashes.Sha1
        });

        return FileDownloader.DownloadAsync(
            DownloadSourcePolicy.Apply(request, Source, replaceInvalidFiles: false),
            onProgress, cancellationToken);
    }

    private async Task<T> GetAsync<T>(
        string relativePath,
        CancellationToken cancellationToken) where T : class
    {
        ArgumentNullException.ThrowIfNull(Source);
        cancellationToken.ThrowIfCancellationRequested();
        string url = Source.GetUrl(ApiRoot + relativePath);

        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(_userAgent);

        try
        {
            using HttpResponseMessage response = await Client.SendAsync(
                request, HttpCompletionOption.ResponseContentRead,
                cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            if (response.StatusCode != HttpStatusCode.OK
                || response.Content.Headers.ContentRange is not null)
                throw new InvalidDataException("Modrinth 元数据必须返回完整的 HTTP 200。");

            string text = await response.Content.ReadAsStringAsync(
                cancellationToken).ConfigureAwait(false);

            return JsonSerializer.Deserialize<T>(text, JsonOptions)
                ?? throw new InvalidDataException("Modrinth 元数据不能为空。");
        }
        catch (OperationCanceledException error)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("读取 Modrinth 元数据超过 30 秒。", error);
        }
    }
}

