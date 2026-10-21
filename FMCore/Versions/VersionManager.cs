using System.Security.Cryptography;
using System.Text;
using FMCore.Downloader;

namespace FMCore.Versions;

/// <summary>One manager per metadata root. The caller owns the shared downloader and HttpClient.</summary>
public sealed class VersionManager
{
    /// <summary>Official Minecraft Java version catalog endpoint.</summary>
    public static Uri OfficialManifestUri { get; } = new("https://piston-meta.mojang.com/mc/game/version_manifest_v2.json");
    private const string ManifestPath = ".cache/metadata/version_manifest_v2.json";
    private const int MaximumJsonBytes = 16 * 1024 * 1024;
    private const int MaximumInheritanceDepth = 32;
    private readonly DownloadManager downloads;
    private readonly Uri manifestUri;
    private readonly SemaphoreSlim gate = new(1, 1);

    /// <summary>Shared metadata/download root.</summary>
    public string RootDirectory => downloads.RootDirectory;

    /// <summary>Uses the existing downloader's root, hash checks and networking. An alternate manifest endpoint is optional.</summary>
    public VersionManager(DownloadManager downloads, Uri? manifestUri = null)
    {
        ArgumentNullException.ThrowIfNull(downloads);
        this.downloads = downloads;
        this.manifestUri = manifestUri ?? OfficialManifestUri;
    }

    /// <summary>Gets the release/snapshot catalog according to the cache policy; CacheOnly never uses the network.</summary>
    public Task<VersionManifest> GetManifestAsync(VersionCachePolicy cachePolicy = VersionCachePolicy.PreferCache,
        CancellationToken cancellationToken = default) => RunAsync(() => ManifestAsync(cachePolicy, cancellationToken), cancellationToken);

    /// <summary>Reads or fetches one version profile. Use ResolveAsync to apply inheritance.</summary>
    public Task<VersionMetadata> GetVersionAsync(string id, VersionCachePolicy cachePolicy = VersionCachePolicy.PreferCache,
        CancellationToken cancellationToken = default) => RunAsync(async () =>
        {
            VersionParser.Id(id);
            if (cachePolicy == VersionCachePolicy.Refresh) await ManifestAsync(cachePolicy, cancellationToken);
            return await LoadAsync(id, cachePolicy, cancellationToken);
        }, cancellationToken);

    /// <summary>Returns the validated local JSON path for a version ID without writing a file.</summary>
    public string GetMetadataPath(string id) => PathFor(RelativeProfilePath(VersionParser.Id(id)));

    /// <summary>Enumerates versions under the configured root without network access or cache changes.</summary>
    public Task<IReadOnlyList<LocalVersionInfo>> GetLocalVersionsAsync(CancellationToken cancellationToken = default) => RunAsync<IReadOnlyList<LocalVersionInfo>>(async () =>
    {
        var directory = PathFor("versions");
        if (File.Exists(directory)) throw new VersionException(VersionErrorCode.IoError, "versions 路径不是目录。");
        if (!Directory.Exists(directory)) return Array.Empty<LocalVersionInfo>();
        var results = new List<LocalVersionInfo>();
        var metadataCache = new Dictionary<string, VersionMetadata>(StringComparer.OrdinalIgnoreCase);
        async Task<VersionMetadata> ReadLocal(string id)
        {
            if (!metadataCache.TryGetValue(id, out var metadata))
                metadataCache.Add(id, metadata = await ReadLocalAsync(id, cancellationToken));
            return metadata;
        }
        foreach (var folder in Directory.EnumerateDirectories(directory).OrderBy(p => Path.GetFileName(p), StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = Path.GetFileName(folder);
            string? metadataPath = null;
            string? type = null;
            string? jarId = null;
            string? jarPath = null;
            try
            {
                metadataPath = GetMetadataPath(id);
                if (!File.Exists(metadataPath)) continue;
                type = (await ReadLocal(id)).Type;
                var resolved = await ResolveCoreAsync(id, ReadLocal, cancellationToken);
                type = resolved.Metadata.Type;
                jarId = resolved.ClientJarId;
                jarPath = PathFor($"versions/{jarId}/{jarId}.jar");
                var hasJar = File.Exists(jarPath) && new FileInfo(jarPath).Length > 0;
                results.Add(new(id, type, metadataPath, jarId, jarPath, hasJar, null));
            }
            catch (VersionException error) { results.Add(new(id, type, metadataPath, jarId, null, false, error.Code)); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { results.Add(new(id, type, metadataPath, jarId, null, false, VersionErrorCode.IoError)); }
        }
        return results.AsReadOnly();
    }, cancellationToken);

    /// <summary>Stores local/loader metadata; this does not install the loader or trust its game files.</summary>
    public Task SaveLocalAsync(VersionMetadata metadata, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        return RunAsync(async () =>
        {
            await StoreAsync(GetMetadataPath(metadata.Id), Encoding.UTF8.GetBytes(metadata.ToJson().ToJsonString()), cancellationToken);
            return true;
        }, cancellationToken);
    }

    /// <summary>Resolves parent profiles and client JAR references, merging metadata and detecting cycles or unsafe paths.</summary>
    public Task<ResolvedVersion> ResolveAsync(string id, VersionCachePolicy cachePolicy = VersionCachePolicy.PreferCache,
        CancellationToken cancellationToken = default) => RunAsync(async () =>
        {
            VersionParser.Id(id);
            ValidatePolicy(cachePolicy);
            // Refresh the catalog once per resolution, not once for each ancestor.
            if (cachePolicy == VersionCachePolicy.Refresh) await ManifestAsync(cachePolicy, cancellationToken);
            return await ResolveCoreAsync(id, current => LoadAsync(current, cachePolicy, cancellationToken), cancellationToken);
        }, cancellationToken);

    private async Task<ResolvedVersion> ResolveCoreAsync(string id, Func<string, Task<VersionMetadata>> load, CancellationToken cancellationToken)
    {
        var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var completed = new Dictionary<string, ResolvedVersion>(StringComparer.OrdinalIgnoreCase);
        async Task<ResolvedVersion> Resolve(string current, int depth)
        {
            cancellationToken.ThrowIfCancellationRequested();
            VersionParser.Id(current);
            if (active.Contains(current)) throw new VersionException(VersionErrorCode.InheritanceCycle, "版本继承或客户端 JAR 引用存在循环。");
            if (depth >= MaximumInheritanceDepth) throw new VersionException(VersionErrorCode.InheritanceTooDeep, "版本继承超过 32 层。");
            if (completed.TryGetValue(current, out var result)) return result;
            active.Add(current);
            try
            {
                var child = await load(current);
                var parent = child.InheritsFrom == null ? null : await Resolve(child.InheritsFrom, depth + 1);
                var metadata = parent == null ? child : VersionInheritance.Merge(parent.Metadata, child);
                var jarId = child.Downloads.ContainsKey("client") ? child.Id : parent?.ClientJarId ?? child.Id;
                var client = child.Downloads.GetValueOrDefault("client") ?? parent?.ClientDownload;
                if (child.Jar != null)
                {
                    if (child.Jar == current) { jarId = current; client = child.Downloads.GetValueOrDefault("client"); }
                    else
                    {
                        var jarSource = parent?.ClientJarId == child.Jar ? parent : await Resolve(child.Jar, depth + 1);
                        jarId = jarSource.ClientJarId;
                        client = jarSource.ClientDownload;
                    }
                }
                var chain = (parent?.InheritanceChain ?? []).Append(child.Id).ToArray();
                result = new(metadata, jarId, client, Array.AsReadOnly(chain));
                completed.Add(current, result);
                return result;
            }
            finally { active.Remove(current); }
        }
        return await Resolve(id, 0);
    }

    private async Task<VersionMetadata> ReadLocalAsync(string id, CancellationToken ct)
    {
        var path = GetMetadataPath(id);
        if (!File.Exists(path)) throw new VersionException(VersionErrorCode.NotCached, "指定版本尚未缓存。");
        var metadata = VersionParser.Parse(await ReadAsync(path, ct));
        if (metadata.Id != id) throw new VersionException(VersionErrorCode.InvalidMetadata, "版本 ID 与缓存文件名不一致。");
        return metadata;
    }

    private async Task<VersionManifest> ManifestAsync(VersionCachePolicy policy, CancellationToken ct)
    {
        ValidatePolicy(policy);
        var path = PathFor(ManifestPath);
        if (policy != VersionCachePolicy.Refresh && File.Exists(path))
        {
            try { return VersionParser.ParseManifest(await ReadAsync(path, ct)); }
            catch (VersionException e) when (e.Code == VersionErrorCode.InvalidMetadata && policy != VersionCachePolicy.CacheOnly) { }
        }
        if (policy == VersionCachePolicy.CacheOnly) throw new VersionException(VersionErrorCode.NotCached, "版本列表未缓存。");
        return await FetchAsync(manifestUri, null, null, path, VersionParser.ParseManifest, ct);
    }

    private async Task<VersionMetadata> LoadAsync(string id, VersionCachePolicy policy, CancellationToken ct)
    {
        ValidatePolicy(policy);
        var path = GetMetadataPath(id);
        var catalogPath = PathFor(ManifestPath);
        VersionManifest? catalog = File.Exists(catalogPath) ? await ManifestAsync(VersionCachePolicy.PreferCache == policy
            ? VersionCachePolicy.PreferCache : VersionCachePolicy.CacheOnly, ct) : null;
        var entry = catalog?.Versions.FirstOrDefault(v => v.Id == id);
        VersionMetadata? local = null;
        VersionException? invalid = null;
        if (File.Exists(path))
        {
            try
            {
                local = await ReadLocalAsync(id, ct);
                if (entry != null && !await MatchesAsync(path, entry.Sha1, ct))
                    throw new VersionException(VersionErrorCode.InvalidMetadata, "版本缓存与官方 SHA-1 不符。");
                if (policy != VersionCachePolicy.Refresh) return local;
            }
            catch (VersionException e) when (e.Code == VersionErrorCode.InvalidMetadata)
            { local = null; invalid = e; }
        }
        if (policy == VersionCachePolicy.CacheOnly)
            throw invalid ?? new VersionException(VersionErrorCode.NotCached, "指定版本尚未缓存。");
        catalog ??= await ManifestAsync(VersionCachePolicy.PreferCache, ct);
        entry ??= catalog.Versions.FirstOrDefault(v => v.Id == id);
        if (entry == null)
            return local ?? throw invalid ?? new VersionException(VersionErrorCode.NotFound, "官方列表与本地缓存中均未找到指定版本。");
        return await FetchAsync(entry.Url, entry.Sha1, null, path, s =>
        {
            var metadata = VersionParser.Parse(s);
            if (metadata.Id != id) throw new VersionException(VersionErrorCode.InvalidMetadata, "远程版本 ID 与请求不一致。");
            return metadata;
        }, ct);
    }

    private async Task<T> FetchAsync<T>(Uri source, string? sha1, long? size, string destination, Func<string, T> validate, CancellationToken ct)
    {
        var relative = $".cache/metadata/incoming/{Guid.NewGuid():N}.json";
        var temporary = PathFor(relative);
        var downloaded = false;
        try
        {
            await downloads.DownloadAsync(new(source, relative) { ExpectedHash = sha1, ExpectedSize = size }, cancellationToken: ct);
            downloaded = true;
            var text = await ReadAsync(temporary, ct);
            var value = validate(text);
            ct.ThrowIfCancellationRequested();
            DownloadPath.CheckLinks(destination);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            DownloadPath.CheckLinks(destination);
            File.Move(temporary, destination, true);
            return value;
        }
        finally { if (downloaded) File.Delete(temporary); }
    }

    private static async Task<string> ReadAsync(string path, CancellationToken ct)
    {
        DownloadPath.CheckLinks(path);
        if (new FileInfo(path).Length > MaximumJsonBytes) throw new VersionException(VersionErrorCode.InvalidMetadata, "版本 JSON 超过 16 MiB 限制。");
        return await File.ReadAllTextAsync(path, ct);
    }

    private static async Task<bool> MatchesAsync(string path, string expected, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        return string.Equals(Convert.ToHexString(await SHA1.HashDataAsync(stream, ct)), expected, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task StoreAsync(string path, byte[] content, CancellationToken ct)
    {
        if (content.Length > MaximumJsonBytes) throw new VersionException(VersionErrorCode.InvalidMetadata, "版本 JSON 超过 16 MiB 限制。");
        DownloadPath.CheckLinks(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        DownloadPath.CheckLinks(path);
        var temporary = Path.Combine(Path.GetDirectoryName(path)!, $".metadata-{Guid.NewGuid():N}.tmp");
        var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        try
        {
            await using (output) { await output.WriteAsync(content, ct); await output.FlushAsync(ct); }
            ct.ThrowIfCancellationRequested();
            DownloadPath.CheckLinks(path);
            File.Move(temporary, path, true);
        }
        finally { File.Delete(temporary); }
    }

    private string PathFor(string relative)
    {
        try { return DownloadPath.Resolve(downloads.RootDirectory, relative); }
        catch (DownloadException) { throw new VersionException(VersionErrorCode.UnsafePath, "版本缓存路径不安全。"); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { throw new VersionException(VersionErrorCode.IoError, "无法访问版本缓存路径。"); }
    }
    private static string RelativeProfilePath(string id) => $"versions/{id}/{id}.json";
    private static void ValidatePolicy(VersionCachePolicy policy)
    {
        if (!Enum.IsDefined(policy)) throw new VersionException(VersionErrorCode.InvalidMetadata, "缓存策略无效。");
    }
    private async Task<T> RunAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try { return await action(); }
        catch (DownloadException e) when (e.Code == DownloadErrorCode.UnsafePath)
        { throw new VersionException(VersionErrorCode.UnsafePath, "版本缓存路径不安全。"); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { throw new VersionException(VersionErrorCode.IoError, "版本缓存文件操作失败。"); }
        finally { gate.Release(); }
    }
}
