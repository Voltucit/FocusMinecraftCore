using System.Security.Cryptography;
using System.Text;
using FMCore.Downloader;

namespace FMCore.Versions;

/// <summary>One manager per metadata root. The caller owns the shared downloader and HttpClient.</summary>
public sealed partial class VersionManager
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
    public string GetMetadataPath(string id) => FindProfilePath(VersionParser.Id(id)) ?? PathFor(RelativeProfilePath(id));

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
                metadataPath = FindProfilePath(id);
                if (metadataPath == null) continue;
                type = (await ReadLocal(id)).Type;
                var resolved = await ResolveCoreAsync(id, ReadLocal, cancellationToken);
                type = resolved.Metadata.Type;
                jarId = resolved.ClientJarId;
                jarPath = PathFor(resolved.ClientJarRelativePath ?? $"versions/{jarId}/{jarId}.jar");
                var hasJar = File.Exists(jarPath) && new FileInfo(jarPath).Length > 0;
                results.Add(new(id, type, metadataPath, jarId, jarPath, hasJar, null)
                    { MetadataId = resolved.Metadata.Id, VersionDirectory = PathFor("versions/" + id) });
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
            if (depth >= MaximumInheritanceDepth) throw new VersionException(VersionErrorCode.InheritanceTooDeep, "版本继承超过 32 层。");
            if (completed.TryGetValue(current, out var result)) return result;
            var child = await load(current);
            var directory = child.LocalProfile?.DirectoryName ?? current;
            if (!active.Add(directory)) throw new VersionException(VersionErrorCode.InheritanceCycle, "版本继承或客户端 JAR 引用存在循环。");
            try
            {
                var parent = child.InheritsFrom == null ? null : await Resolve(child.InheritsFrom, depth + 1);
                var metadata = parent == null ? child : VersionInheritance.Merge(parent.Metadata, child);
                var jarId = child.Downloads.ContainsKey("client") ? directory : parent?.ClientJarId ?? directory;
                var client = child.Downloads.GetValueOrDefault("client") ?? parent?.ClientDownload;
                var jarRelative = child.Downloads.ContainsKey("client") || parent == null ? LocalJar(directory, child.Id) : parent.ClientJarRelativePath;
                if (child.Jar != null)
                {
                    if (child.Jar == directory)
                    { jarId = directory; client = child.Downloads.GetValueOrDefault("client"); jarRelative = LocalJar(directory, child.Id); }
                    else if (File.Exists(JarPath(child.Jar, child.Jar)))
                    {
                        jarId = child.Jar;
                        jarRelative = $"versions/{jarId}/{jarId}.jar";
                        client = parent?.ClientJarId == jarId ? parent.ClientDownload : null;
                    }
                    else if (child.Jar == child.Id || File.Exists(JarPath(directory, child.Jar)))
                    {
                        // Renamed folders often keep jar equal to the original JSON ID.
                        // This is a file reference, not an inheritance edge back to the same profile.
                        jarId = directory;
                        jarRelative = child.Jar == child.Id ? LocalJar(directory, child.Id)
                            : $"versions/{directory}/{child.Jar}.jar";
                        client = child.Downloads.GetValueOrDefault("client");
                    }
                    else
                    {
                        var jarSource = parent?.ClientJarId == child.Jar ? parent : await Resolve(child.Jar, depth + 1);
                        jarId = jarSource.ClientJarId;
                        client = jarSource.ClientDownload;
                        jarRelative = jarSource.ClientJarRelativePath;
                    }
                }
                var chain = (parent?.InheritanceChain ?? []).Append(child.Id).ToArray();
                var custom = child.LocalProfile?.IsCustom == true || parent?.IsCustom == true;
                result = new(metadata, jarId, client, Array.AsReadOnly(chain))
                {
                    DirectoryName = directory, MetadataPath = child.LocalProfile?.MetadataPath,
                    ClientJarRelativePath = jarRelative, IsCustom = custom,
                    Warnings = custom ? ["本地自定义启动配置会执行 Java、主类及其参数；请仅启动可信来源。缺失发布者哈希的本地文件只能确认存在，不能证明来源或完整性。"] : []
                };
                completed.Add(current, result);
                return result;
            }
            finally { active.Remove(directory); }
        }
        return await Resolve(id, 0);
    }

    private async Task<VersionMetadata> ReadLocalAsync(string id, CancellationToken ct)
    {
        var (path, parsed) = await FindLocalAsync(id, ct).ConfigureAwait(false);
        return AttachLocation(parsed, new(Path.GetFileName(Path.GetDirectoryName(path))!, path, true));
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
        var catalogPath = PathFor(ManifestPath);
        VersionManifest? catalog = null;
        if (File.Exists(catalogPath))
        {
            try { catalog = await ManifestAsync(VersionCachePolicy.CacheOnly, ct).ConfigureAwait(false); }
            catch (VersionException e) when (e.Code == VersionErrorCode.InvalidMetadata && policy == VersionCachePolicy.CacheOnly) { }
        }
        var entry = catalog?.Versions.FirstOrDefault(v => v.Id == id);
        string? existingHash = null;
        string? existingPath = null;
        VersionException? invalid = null;
        try
        {
            var (path, parsed) = await FindLocalAsync(id, ct).ConfigureAwait(false);
            existingPath = path;
            var directory = Path.GetFileName(Path.GetDirectoryName(path))!;
            var ownership = await OwnershipAsync(directory, ct).ConfigureAwait(false);
            await using (var input = File.OpenRead(path))
                existingHash = Convert.ToHexStringLower(await SHA1.HashDataAsync(input, ct).ConfigureAwait(false));
            var catalogMatch = entry != null && existingHash.Equals(entry.Sha1, StringComparison.OrdinalIgnoreCase);
            var owned = ownership != null && existingHash.Equals(ownership.Sha1, StringComparison.OrdinalIgnoreCase);
            var sameDocument = ownership != null && SemanticHash(parsed) == ownership.SemanticHash;
            // Only SDK-fetched, unchanged official documents are automatically refreshed/repaired.
            // Existing external/custom profiles stay intact even when their ID collides with vanilla.
            var custom = parsed.Id != directory || !catalogMatch && !owned && !sameDocument;
            var metadata = AttachLocation(parsed, new(directory, path, custom));
            if (custom) return metadata;
            if (entry != null && !catalogMatch || ownership != null && !owned && !catalogMatch)
                invalid = new VersionException(VersionErrorCode.InvalidMetadata, "官方版本缓存与记录的 SHA-1 不符。");
            if (invalid == null && policy != VersionCachePolicy.Refresh) return metadata;
        }
        catch (VersionException e) when (e.Code == VersionErrorCode.NotCached) { }
        if (policy == VersionCachePolicy.CacheOnly)
            throw invalid ?? new VersionException(VersionErrorCode.NotCached, "指定版本尚未缓存。");
        catalog ??= await ManifestAsync(VersionCachePolicy.PreferCache, ct).ConfigureAwait(false);
        entry ??= catalog.Versions.FirstOrDefault(v => v.Id == id);
        if (entry == null) throw invalid ?? new VersionException(VersionErrorCode.NotFound, "官方列表与本地目录中均未找到指定版本。");
        var destination = existingPath ?? PathFor(RelativeProfilePath(id));
        var fetched = await FetchAsync(entry.Url, entry.Sha1, null, destination, text =>
        {
            var metadata = VersionParser.Parse(text);
            if (metadata.Id != id) throw new VersionException(VersionErrorCode.InvalidMetadata, "远程版本 ID 与请求不一致。");
            return metadata;
        }, ct, protectDestination: true, existingHash).ConfigureAwait(false);
        await SaveOwnershipAsync(id, entry.Sha1, fetched, ct).ConfigureAwait(false);
        return AttachLocation(fetched, new(id, destination, false));
    }

    private async Task<T> FetchAsync<T>(Uri source, string? sha1, long? size, string destination, Func<string, T> validate, CancellationToken ct, bool protectDestination = false, string? expectedDestinationHash = null)
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
            if (protectDestination && (File.Exists(destination)
                ? expectedDestinationHash == null || !await MatchesAsync(destination, expectedDestinationHash, ct).ConfigureAwait(false)
                : expectedDestinationHash != null))
                throw new VersionException(VersionErrorCode.ConflictingProfiles, "下载期间本地版本已改变，未覆盖现有 JSON。");
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
