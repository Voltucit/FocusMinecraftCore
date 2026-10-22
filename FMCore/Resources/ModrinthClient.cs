using System.Net;
using System.Text.Json;

namespace FMCore.Resources;

/// <summary>Public Modrinth V2 queries. No token is required; uses an identifying User-Agent and bounded responses.</summary>
public sealed class ModrinthClient
{
    private static readonly HttpClient SharedHttp = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly HttpClient http;
    private readonly string userAgent;
    private readonly TimeSpan timeout;
    private static readonly Uri Api = new("https://api.modrinth.com/v2/");
    internal HttpClient Http => http;

    /// <summary>Creates a client with shared networking. Customize User-Agent to identify your launcher.</summary>
    public ModrinthClient(string userAgent = "Voltucit/FMCore (https://github.com/Voltucit/FocusMinecraftCore)")
        : this(SharedHttp, userAgent) { }

    /// <summary>Uses a caller-owned client (proxy/mock support). timeout covers headers and the entire bounded body.</summary>
    public ModrinthClient(HttpClient httpClient, string userAgent = "Voltucit/FMCore (https://github.com/Voltucit/FocusMinecraftCore)",
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        this.timeout = timeout ?? TimeSpan.FromSeconds(45);
        if (string.IsNullOrWhiteSpace(userAgent) || userAgent.Length > 512 || userAgent.Any(char.IsControl) ||
            this.timeout <= TimeSpan.Zero || this.timeout > TimeSpan.FromMinutes(10)) throw ModrinthMetadata.InvalidRequest();
        using var probe = new HttpRequestMessage();
        try { probe.Headers.UserAgent.ParseAdd(userAgent); }
        catch (FormatException) { throw ModrinthMetadata.InvalidRequest(); }
        (http, this.userAgent) = (httpClient, userAgent);
    }

    /// <summary>Searches projects with AND-combined Minecraft, loader and type facets. Cancellation does not return partial results.</summary>
    public async Task<ModrinthSearchResult> SearchAsync(string query = "", ModrinthSearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new();
        if (query == null || query.Length > 512 || query.Any(char.IsControl) || options.Limit is < 1 or > 100 || options.Offset < 0 ||
            options.ProjectType is not ("mod" or "modpack" or "resourcepack" or "shader") ||
            options.Sort is not ("relevance" or "downloads" or "follows" or "newest" or "updated")) throw ModrinthMetadata.InvalidRequest();
        List<string[]> facets = [["project_type:" + options.ProjectType]];
        if (options.MinecraftVersion is { } game) facets.Add(["versions:" + ModrinthMetadata.Token(game)]);
        if (options.Loader is { } loader) facets.Add(["categories:" + ModrinthMetadata.Token(loader)]);
        var path = $"search?query={Escape(query)}&facets={Escape(JsonSerializer.Serialize(facets))}&index={options.Sort}&limit={options.Limit}&offset={options.Offset}";
        using var doc = await GetAsync(path, cancellationToken).ConfigureAwait(false);
        return ModrinthMetadata.Search(doc.RootElement);
    }

    /// <summary>Reads a project by its canonical ID or slug.</summary>
    public async Task<ModrinthProject> GetProjectAsync(string projectId, CancellationToken cancellationToken = default)
    {
        using var doc = await GetAsync("project/" + ModrinthMetadata.Token(projectId), cancellationToken).ConfigureAwait(false);
        var result = ModrinthMetadata.Project(doc.RootElement);
        if (result.Id != projectId && result.Slug != projectId) throw ModrinthMetadata.Invalid();
        return result;
    }

    /// <summary>Lists versions, newest publication first. A target environment filters compatibility and automatic release channels.</summary>
    public async Task<IReadOnlyList<ModrinthVersion>> GetVersionsAsync(string projectId, ModEnvironment? environment = null,
        CancellationToken cancellationToken = default)
    {
        var path = "project/" + ModrinthMetadata.Token(projectId) + "/version?include_changelog=false";
        if (environment != null)
        {
            environment = ModrinthMetadata.Environment(environment);
            path += "&loaders=" + Escape(JsonSerializer.Serialize(new[] { environment.Loader })) +
                "&game_versions=" + Escape(JsonSerializer.Serialize(new[] { environment.MinecraftVersion }));
        }
        using var doc = await GetAsync(path, cancellationToken).ConfigureAwait(false);
        var versions = ModrinthMetadata.Versions(doc.RootElement);
        return Array.AsReadOnly(versions.Where(v => environment == null || ModrinthMetadata.Compatible(v, environment) &&
            (environment.IncludePrereleases || v.VersionType == "release")).OrderByDescending(v => v.PublishedAt).ThenBy(v => v.Id, StringComparer.Ordinal).ToArray());
    }

    /// <summary>Reads one version, preserving files, hashes and dependency metadata.</summary>
    public async Task<ModrinthVersion> GetVersionAsync(string versionId, CancellationToken cancellationToken = default)
    {
        using var doc = await GetAsync("version/" + ModrinthMetadata.Token(versionId), cancellationToken).ConfigureAwait(false);
        var version = ModrinthMetadata.Version(doc.RootElement);
        if (version.Id != versionId) throw ModrinthMetadata.Invalid();
        return version;
    }

    /// <summary>Identifies an existing file by SHA-512. Returns null only for HTTP 404; never adopts or modifies the file.</summary>
    public async Task<ModrinthVersion?> GetVersionByHashAsync(string sha512, CancellationToken cancellationToken = default)
    {
        if (!ModrinthMetadata.Hash(sha512, 128)) throw ModrinthMetadata.InvalidRequest();
        try
        {
            using var doc = await GetAsync("version_file/" + sha512.ToLowerInvariant() + "?algorithm=sha512", cancellationToken).ConfigureAwait(false);
            var version = ModrinthMetadata.Version(doc.RootElement);
            if (!version.Files.Any(f => f.Sha512.Equals(sha512, StringComparison.OrdinalIgnoreCase))) throw ModrinthMetadata.Invalid();
            return version;
        }
        catch (ResourceException e) when (e.Code == ResourceErrorCode.NotFound) { return null; }
    }

    private async Task<JsonDocument> GetAsync(string path, CancellationToken ct)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(timeout);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(Api, path));
            request.Headers.UserAgent.ParseAdd(userAgent);
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var code = response.StatusCode switch { HttpStatusCode.NotFound => ResourceErrorCode.NotFound,
                    HttpStatusCode.TooManyRequests => ResourceErrorCode.RateLimited, _ => ResourceErrorCode.HttpError };
                TimeSpan? wait = response.Headers.RetryAfter?.Delta;
                if (wait == null && response.Headers.RetryAfter?.Date is { } date) wait = date - DateTimeOffset.UtcNow;
                if (wait == null && response.Headers.TryGetValues("X-Ratelimit-Reset", out var values) &&
                    int.TryParse(values.FirstOrDefault(), out var seconds) && seconds >= 0) wait = TimeSpan.FromSeconds(seconds);
                throw new ResourceException(code, "Modrinth 请求失败；请检查状态码或稍后重试。")
                { StatusCode = (int)response.StatusCode, RetryAfter = wait < TimeSpan.Zero ? TimeSpan.Zero : wait };
            }
            const int limit = 16 * 1024 * 1024;
            if (response.Content.Headers.ContentLength > limit) throw ModrinthMetadata.Invalid();
            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            var bytes = new byte[81920];
            int count;
            while ((count = await stream.ReadAsync(bytes, deadline.Token).ConfigureAwait(false)) != 0)
            {
                if (buffer.Length + count > limit) throw ModrinthMetadata.Invalid();
                buffer.Write(bytes, 0, count);
            }
            return JsonDocument.Parse(buffer.GetBuffer().AsMemory(0, (int)buffer.Length), new() { MaxDepth = 32 });
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { throw new ResourceException(ResourceErrorCode.Timeout, "Modrinth 请求超时。"); }
        catch (HttpRequestException) { throw new ResourceException(ResourceErrorCode.NetworkError, "无法连接 Modrinth。"); }
        catch (IOException) { throw new ResourceException(ResourceErrorCode.NetworkError, "读取 Modrinth 响应失败。"); }
        catch (JsonException) { throw ModrinthMetadata.Invalid(); }
    }

    private static string Escape(string value) => Uri.EscapeDataString(value);
}
