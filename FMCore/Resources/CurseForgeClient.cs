using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace FMCore.Resources;

/// <summary>CurseForge V1 queries with a caller-owned API key. Keys are sent only to the fixed API origin and are never persisted.</summary>
[DebuggerDisplay("CurseForge API client")]
public sealed class CurseForgeClient : IDisposable
{
    private static readonly HttpClient Shared = new(new SocketsHttpHandler { AllowAutoRedirect = false }) { Timeout = Timeout.InfiniteTimeSpan };
    private readonly HttpClient http;
    private readonly string apiKey;
    private readonly bool owned;
    private readonly TimeSpan timeout;
    private bool disposed;

    /// <summary>Uses shared networking with redirects disabled. The caller must obtain their own lawful API key.</summary>
    public CurseForgeClient(string apiKey, TimeSpan? timeout = null) : this(apiKey, Shared, false, timeout) { }
    /// <summary>Advanced proxy/mock support. The caller owns the handler; SDK disables redirects on standard HTTP handlers.</summary>
    public CurseForgeClient(string apiKey, HttpMessageHandler handler, TimeSpan? timeout = null)
        : this(apiKey, CreateHttp(handler), true, timeout) { }
    private CurseForgeClient(string key, HttpClient http, bool owned, TimeSpan? timeout)
    {
        this.timeout = timeout ?? TimeSpan.FromSeconds(45);
        if (string.IsNullOrWhiteSpace(key) || key.Length > 4096 || key.Any(c => c is < '!' or > '~') ||
            this.timeout <= TimeSpan.Zero || this.timeout > TimeSpan.FromMinutes(10)) throw ModrinthMetadata.InvalidRequest();
        (apiKey, this.http, this.owned) = (key, http, owned);
    }

    /// <summary>Searches Minecraft resources. Defaults to the Mods class; file compatibility is checked again at installation.</summary>
    public async Task<CurseForgeSearchResult> SearchAsync(string query = "", CurseForgeSearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new(); CurseForgeMetadata.Pagination(options.Index, options.PageSize);
        if (query == null || query.Length > 512 || query.Any(char.IsControl) || options.ClassId <= 0 || options.CategoryId is <= 0 ||
            options.SortField is < 1 or > 12 || options.SortOrder is not ("asc" or "desc")) throw ModrinthMetadata.InvalidRequest();
        var path = $"mods/search?gameId=432&classId={options.ClassId}&searchFilter={Uri.EscapeDataString(query)}&sortField={options.SortField}&sortOrder={options.SortOrder}&index={options.Index}&pageSize={options.PageSize}";
        if (options.CategoryId is { } category) path += "&categoryId=" + category;
        if (options.MinecraftVersion is { } version) path += "&gameVersion=" + Uri.EscapeDataString(ModrinthMetadata.Token(version));
        if (options.Loader is { } loader) path += "&modLoaderType=" + CurseForgeMetadata.Loader(loader);
        using var doc = await GetAsync(path, cancellationToken).ConfigureAwait(false);
        var page = CurseForgeMetadata.Page(doc.RootElement);
        if (page.Index != options.Index || page.Size != options.PageSize) throw CurseForgeMetadata.Invalid();
        var mods = page.Items.Select(CurseForgeMetadata.Project).ToArray();
        if (mods.Any(m => m.GameId != 432)) throw CurseForgeMetadata.Invalid();
        return new(Array.AsReadOnly(mods), page.Index, page.Size, page.Total);
    }
    /// <summary>Reads a project including its nullable distribution policy.</summary>
    public async Task<CurseForgeProject> GetProjectAsync(int modId, CancellationToken cancellationToken = default)
    {
        using var doc = await GetAsync("mods/" + CurseForgeMetadata.Id(modId), cancellationToken).ConfigureAwait(false);
        var project = CurseForgeMetadata.Project(Data(doc));
        if (project.Id != modId || project.GameId != 432) throw CurseForgeMetadata.Invalid();
        return project;
    }
    /// <summary>Reads one file. A missing download URL is returned as metadata, never reconstructed or fetched from an alternate endpoint.</summary>
    public async Task<CurseForgeFile> GetFileAsync(int modId, int fileId, CancellationToken cancellationToken = default)
    {
        using var doc = await GetAsync($"mods/{CurseForgeMetadata.Id(modId)}/files/{CurseForgeMetadata.Id(fileId)}", cancellationToken).ConfigureAwait(false);
        var file = CurseForgeMetadata.File(Data(doc));
        if (file.Id != fileId || file.ModId != modId || file.GameId != 432) throw CurseForgeMetadata.Invalid();
        return file;
    }
    /// <summary>Reads one page, preserving pagination totals. With environment, filters Minecraft/loader/channel locally as well as on the server.</summary>
    public async Task<CurseForgeFilePage> GetFilesAsync(int modId, ModEnvironment? environment = null, int index = 0, int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        CurseForgeMetadata.Pagination(index, pageSize);
        var path = $"mods/{CurseForgeMetadata.Id(modId)}/files?index={index}&pageSize={pageSize}";
        if (environment != null)
        {
            environment = ModrinthMetadata.Environment(environment);
            path += "&gameVersion=" + Uri.EscapeDataString(environment.MinecraftVersion) + "&modLoaderType=" + CurseForgeMetadata.Loader(environment.Loader);
        }
        using var doc = await GetAsync(path, cancellationToken).ConfigureAwait(false);
        var page = CurseForgeMetadata.Page(doc.RootElement);
        if (page.Index != index || page.Size != pageSize) throw CurseForgeMetadata.Invalid();
        var files = page.Items.Select(CurseForgeMetadata.File).ToArray();
        if (files.Any(f => f.ModId != modId || f.GameId != 432) || files.Select(f => f.Id).Distinct().Count() != files.Length) throw CurseForgeMetadata.Invalid();
        return new(Array.AsReadOnly(files.Where(f => environment == null || CurseForgeMetadata.Compatible(f, environment) &&
            (environment.IncludePrereleases || f.ReleaseType == 1)).ToArray()), page.Index, page.Size, page.Total, page.Items.Length);
    }

    private async Task<JsonDocument> GetAsync(string path, CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct); deadline.CancelAfter(timeout);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.curseforge.com/v1/" + path);
            request.Headers.Add("x-api-key", apiKey);
            request.Headers.UserAgent.ParseAdd("Voltucit/FMCore (https://github.com/Voltucit/FocusMinecraftCore)");
            using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var code = response.StatusCode switch
                { HttpStatusCode.NotFound => ResourceErrorCode.NotFound, HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ResourceErrorCode.AuthenticationFailed,
                    HttpStatusCode.TooManyRequests => ResourceErrorCode.RateLimited, _ => ResourceErrorCode.HttpError };
                var wait = response.Headers.RetryAfter?.Delta;
                if (wait == null && response.Headers.RetryAfter?.Date is { } date) wait = date - DateTimeOffset.UtcNow;
                throw new ResourceException(code, "CurseForge 请求失败，请检查状态码、调用权限或稍后重试。")
                { StatusCode = (int)response.StatusCode, RetryAfter = wait < TimeSpan.Zero ? TimeSpan.Zero : wait };
            }
            if (response.RequestMessage?.RequestUri is { } final && (final.Scheme != "https" || final.Host != "api.curseforge.com" || !final.IsDefaultPort))
                throw new ResourceException(ResourceErrorCode.HttpError, "CurseForge API 请求发生了不允许的重定向。");
            const int limit = 16 * 1024 * 1024;
            if (response.Content.Headers.ContentLength > limit) throw CurseForgeMetadata.Invalid();
            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token).ConfigureAwait(false);
            using var buffer = new MemoryStream(); var bytes = new byte[81920]; int count;
            while ((count = await stream.ReadAsync(bytes, deadline.Token).ConfigureAwait(false)) != 0)
            { if (buffer.Length + count > limit) throw CurseForgeMetadata.Invalid(); buffer.Write(bytes, 0, count); }
            var document = JsonDocument.Parse(buffer.GetBuffer().AsMemory(0, (int)buffer.Length), new() { MaxDepth = 32 });
            try { RejectReflectedCredential(document.RootElement); return document; }
            catch { document.Dispose(); throw; }
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new ResourceException(ResourceErrorCode.Timeout, "CurseForge 请求超时。"); }
        catch (HttpRequestException) { throw new ResourceException(ResourceErrorCode.NetworkError, "无法连接 CurseForge。"); }
        catch (IOException) { throw new ResourceException(ResourceErrorCode.NetworkError, "读取 CurseForge 响应失败。"); }
        catch (JsonException) { throw CurseForgeMetadata.Invalid(); }
    }
    private static JsonElement Data(JsonDocument doc)
    { if (doc.RootElement.ValueKind != JsonValueKind.Object || !doc.RootElement.TryGetProperty("data", out var data)) throw CurseForgeMetadata.Invalid(); return data; }
    // A malformed/proxied service must not reflect the API credential into a filename, receipt, public model or CDN URL.
    private void RejectReflectedCredential(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String) Check(element.GetString()!);
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var child in element.EnumerateArray()) RejectReflectedCredential(child);
        else if (element.ValueKind == JsonValueKind.Object)
            foreach (var property in element.EnumerateObject()) { Check(property.Name); RejectReflectedCredential(property.Value); }
        void Check(string value)
        {
            if (value.Contains(apiKey, StringComparison.Ordinal) || Uri.UnescapeDataString(value).Contains(apiKey, StringComparison.Ordinal))
                throw CurseForgeMetadata.Invalid();
        }
    }
    private static HttpClient CreateHttp(HttpMessageHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        try
        {
            DisableRedirects(handler);
            return new(handler, false) { Timeout = Timeout.InfiniteTimeSpan };
        }
        catch (InvalidOperationException) { throw ModrinthMetadata.InvalidRequest(); }
    }
    private static void DisableRedirects(HttpMessageHandler handler)
    {
        if (handler is HttpClientHandler standard) standard.AllowAutoRedirect = false;
        else if (handler is SocketsHttpHandler sockets) sockets.AllowAutoRedirect = false;
        else if (handler is DelegatingHandler { InnerHandler: { } inner }) DisableRedirects(inner);
    }
    /// <summary>Releases the SDK wrapper for an injected handler; the caller retains ownership of the handler.</summary>
    public void Dispose() { disposed = true; if (owned) http.Dispose(); }
    /// <summary>Never exposes the API key.</summary>
    public override string ToString() => "CurseForge API client";
}
