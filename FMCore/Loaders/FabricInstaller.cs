using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FMCore.Downloader;
using FMCore.Installer;
using FMCore.Utilities;
using FMCore.Versions;

namespace FMCore.Loaders;

/// <summary>Queries Fabric Meta and installs client profiles with the existing vanilla pipeline. No Java installer is required.</summary>
public sealed class FabricInstaller
{
    /// <summary>Official Fabric Meta V2 endpoint.</summary>
    public static Uri OfficialMetaUri { get; } = new("https://meta.fabricmc.net/v2/");
    private readonly DownloadManager downloads;
    private readonly VersionManager versions;
    private readonly GameInstaller installer;
    private readonly Uri endpoint;
    private readonly string cache;
    private readonly SemaphoreSlim gate = new(1, 1);

    /// <summary>Absolute shared game/download root.</summary>
    public string RootDirectory => downloads.RootDirectory;

    /// <summary>Creates a Fabric installer with default networking and cache settings.</summary>
    public FabricInstaller(string rootDirectory = ".minecraft") : this(new DownloadManager(rootDirectory)) { }

    /// <summary>Uses caller-configured networking and optional Minecraft metadata manager and Fabric Meta V2 mirror.</summary>
    public FabricInstaller(DownloadManager downloads, VersionManager? versions = null, Uri? metaUri = null)
    {
        ArgumentNullException.ThrowIfNull(downloads);
        this.downloads = downloads;
        this.versions = versions ?? new(downloads);
        if (!GameFiles.PathComparer.Equals(this.versions.RootDirectory, downloads.RootDirectory))
            throw new ArgumentException("Fabric 与版本管理器必须使用同一根目录。", nameof(versions));
        var uri = metaUri ?? OfficialMetaUri;
        if (!uri.IsAbsoluteUri || uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0)
            throw new ArgumentException("Fabric Meta V2 地址必须是无凭据、查询串或片段的 HTTPS 目录。", nameof(metaUri));
        endpoint = new(uri.AbsoluteUri.TrimEnd('/') + "/");
        cache = ".cache/loaders/fabric/" + Key(endpoint.AbsoluteUri);
        installer = new(downloads, this.versions);
    }

    /// <summary>Gets supported releases and snapshots in official order. Refresh is the default; CacheOnly never queries the service.</summary>
    public Task<IReadOnlyList<FabricGameVersion>> GetGameVersionsAsync(VersionCachePolicy cachePolicy = VersionCachePolicy.Refresh,
        CancellationToken cancellationToken = default) => RunAsync(() => ReadAsync("games.json", "versions/game", cachePolicy,
            FabricMetadata.Games, null, cancellationToken), cancellationToken);

    /// <summary>Gets compatible loader versions, including unstable versions; callers can filter Stable. Does not assume every historical format is supported.</summary>
    public Task<IReadOnlyList<FabricLoaderVersion>> GetLoaderVersionsAsync(string minecraftVersion,
        VersionCachePolicy cachePolicy = VersionCachePolicy.Refresh, CancellationToken cancellationToken = default) => RunAsync(() =>
    {
        VersionParser.Id(minecraftVersion);
        return ReadAsync("loaders-" + Key(minecraftVersion) + ".json", "versions/loader/" + Uri.EscapeDataString(minecraftVersion),
            cachePolicy, FabricMetadata.Loaders, null, cancellationToken);
    }, cancellationToken);

    /// <summary>Installs Fabric and inherited vanilla files; a null loader version selects the first officially stable compatible loader.</summary>
    /// <remarks>Publishes the profile only after downloads, checksums, asset layout and natives succeed. Cancellation/failure retains reusable files but creates no new launchable profile.</remarks>
    public Task<FabricInstallationResult> InstallAsync(string minecraftVersion, string? loaderVersion = null,
        FabricInstallOptions? options = null, IProgress<FabricInstallationProgress>? progress = null,
        CancellationToken cancellationToken = default) => RunAsync(async () =>
    {
        VersionParser.Id(minecraftVersion);
        if (loaderVersion != null) VersionParser.Id(loaderVersion);
        options ??= new();
        ArgumentNullException.ThrowIfNull(options.Installation);
        var policy = options.Installation.CachePolicy;
        ValidatePolicy(policy);
        // A stable, root-wide lock also protects profile publication between different installer objects.
        var lockPath = GameFiles.Resolve(RootDirectory, ".cache/loaders/install.lock");
        GameFiles.CreateDirectory(Path.GetDirectoryName(lockPath)!);
        using var lease = AcquireLease(lockPath);
        progress?.Report(new(FabricInstallationStage.Metadata, 0, 3));
        var prefix = "versions/loader/" + Uri.EscapeDataString(minecraftVersion);
        if (loaderVersion == null)
        {
            var loaders = await ReadAsync("loaders-" + Key(minecraftVersion) + ".json", prefix, policy,
                FabricMetadata.Loaders, null, cancellationToken).ConfigureAwait(false);
            loaderVersion = loaders.FirstOrDefault(v => v.Stable)?.Version ?? throw new FabricException(
                FabricErrorCode.UnsupportedCombination, "该 Minecraft 版本没有可自动选择的稳定 Fabric Loader，请明确指定版本。");
        }
        var id = VersionParser.Id(options.VersionId ?? $"fabric-loader-{loaderVersion}-{minecraftVersion}");
        if (id.Equals(minecraftVersion, StringComparison.OrdinalIgnoreCase))
            throw new FabricException(FabricErrorCode.ProfileConflict, "Fabric 版本 ID 不能与原版版本 ID 相同。");
        var pair = minecraftVersion + ":" + loaderVersion;
        var route = prefix + "/" + Uri.EscapeDataString(loaderVersion);
        var detail = await ReadAsync("detail-" + Key(pair) + ".json", route, policy,
            s => FabricMetadata.Detail(s, minecraftVersion, loaderVersion), null, cancellationToken).ConfigureAwait(false);
        progress?.Report(new(FabricInstallationStage.Metadata, 1, 3));
        var profile = await ReadAsync("profile-" + Key(pair) + ".json", route + "/profile/json", policy,
            s => FabricMetadata.Profile(s, minecraftVersion, loaderVersion, detail.MainClass), null, cancellationToken).ConfigureAwait(false);
        var parent = await versions.ResolveAsync(minecraftVersion, policy, cancellationToken).ConfigureAwait(false);
        if (parent.Metadata.Java == null || detail.MinimumJava > parent.Metadata.Java.MajorVersion)
            throw new FabricException(FabricErrorCode.UnsupportedJavaRequirement,
                "Fabric 最低 Java 要求与原版元数据不兼容或原版缺少 Java 要求；不会擅自升级 Java。");
        profile["id"] = id;
        progress?.Report(new(FabricInstallationStage.Metadata, 3, 3));
        var libraries = (JsonArray)profile["libraries"]!;
        for (var index = 0; index < libraries.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var library = (JsonObject)libraries[index]!;
            var parsed = VersionParser.ParseObject(new JsonObject { ["id"] = id, ["libraries"] = new JsonArray(library.DeepClone()) }).Libraries[0];
            var artifact = parsed.Artifact!;
            if (artifact.Sha1 == null)
            {
                var hash = await ReadAsync("sha1-" + Key(artifact.Url.AbsoluteUri) + ".txt", artifact.Url.AbsoluteUri + ".sha1", policy,
                    FabricMetadata.Sha1, new InlineProgress<DownloadProgress>(p => progress?.Report(new(FabricInstallationStage.Checksums,
                        index, libraries.Count, Download: p))), cancellationToken).ConfigureAwait(false);
                if (library["downloads"] is JsonObject desc) ((JsonObject)desc["artifact"]!)["sha1"] = hash;
                else library["sha1"] = hash;
            }
            progress?.Report(new(FabricInstallationStage.Checksums, index + 1, libraries.Count));
        }
        var child = VersionParser.ParseObject(profile);
        var resolved = new ResolvedVersion(VersionInheritance.Merge(parent.Metadata, child), parent.ClientJarId,
            parent.ClientDownload, Array.AsReadOnly(parent.InheritanceChain.Append(id).ToArray()));
        var profilePath = versions.GetMetadataPath(id);
        var exists = File.Exists(profilePath);
        if (exists && !FabricMetadata.Equivalent(VersionParser.Parse(await GameFiles.ReadJsonAsync(profilePath, cancellationToken)
            .ConfigureAwait(false)).ToJson(), profile))
            throw new FabricException(FabricErrorCode.ProfileConflict, "目标版本已有不同配置；请选择新的 VersionId，现有文件未被覆盖。");
        var installed = await installer.InstallResolvedAsync(resolved, options.Installation,
            new InlineProgress<InstallationProgress>(p => progress?.Report(new(FabricInstallationStage.Installing,
                p.CompletedFiles, p.TotalFiles, p))), cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        // File.Move(overwrite:false) prevents races with other applications writing this version ID.
        if (!exists) await PublishAsync(profilePath, profile, cancellationToken).ConfigureAwait(false);
        else if (!FabricMetadata.Equivalent(VersionParser.Parse(await GameFiles.ReadJsonAsync(profilePath, cancellationToken)
            .ConfigureAwait(false)).ToJson(), profile))
            throw new FabricException(FabricErrorCode.ProfileConflict, "安装期间版本配置已发生变化。");
        progress?.Report(new(FabricInstallationStage.Completed, 1, 1));
        return new FabricInstallationResult(minecraftVersion, loaderVersion, id, installed);
    }, cancellationToken);

    private async Task<T> ReadAsync<T>(string name, string route, VersionCachePolicy policy, Func<string, T> parse,
        IProgress<DownloadProgress>? progress, CancellationToken ct)
    {
        ValidatePolicy(policy);
        var path = GameFiles.Resolve(RootDirectory, cache + "/" + name);
        if (policy != VersionCachePolicy.Refresh && File.Exists(path))
        {
            try { return parse(await GameFiles.ReadJsonAsync(path, ct).ConfigureAwait(false)); }
            catch (Exception e) when (policy == VersionCachePolicy.PreferCache && e is JsonException or InvalidDataException or InvalidOperationException or FormatException or FabricException or VersionException) { }
        }
        if (policy == VersionCachePolicy.CacheOnly)
            throw new FabricException(FabricErrorCode.NotCached, "所需 Fabric 元数据或 Maven SHA-1 尚未缓存。");
        var incoming = cache + "/incoming/" + Guid.NewGuid().ToString("N") + ".tmp";
        var temporary = GameFiles.Resolve(RootDirectory, incoming);
        try
        {
            await downloads.DownloadAsync(new(new Uri(endpoint, route), incoming), progress, ct).ConfigureAwait(false);
            var text = await GameFiles.ReadJsonAsync(temporary, ct).ConfigureAwait(false);
            var value = parse(text);
            await GameFiles.WriteAtomicAsync(path, async (stream, token) =>
                await stream.WriteAsync(Encoding.UTF8.GetBytes(text), token).ConfigureAwait(false), ct).ConfigureAwait(false);
            return value;
        }
        finally { if (File.Exists(temporary)) { DownloadPath.CheckLinks(temporary); File.Delete(temporary); } }
    }

    private static async Task PublishAsync(string path, JsonObject profile, CancellationToken ct)
    {
        GameFiles.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = Path.Combine(Path.GetDirectoryName(path)!, ".fmcore-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await File.WriteAllTextAsync(temporary, profile.ToJsonString(), new UTF8Encoding(false), ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            DownloadPath.CheckLinks(path);
            try { File.Move(temporary, path, false); }
            catch (IOException) when (File.Exists(path))
            { throw new FabricException(FabricErrorCode.ProfileConflict, "安装期间目标版本已被其他调用方创建。"); }
        }
        finally { if (File.Exists(temporary)) { DownloadPath.CheckLinks(temporary); File.Delete(temporary); } }
    }

    private async Task<T> RunAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try { return await action().ConfigureAwait(false); }
        catch (VersionException e) when (e.Code == VersionErrorCode.UnsafePath)
        { throw new FabricException(FabricErrorCode.UnsafePath, "Fabric 版本或依赖路径不安全。"); }
        catch (DownloadException e) when (e.Code == DownloadErrorCode.UnsafePath)
        { throw new FabricException(FabricErrorCode.UnsafePath, "Fabric 文件路径越界或包含链接。"); }
        catch (Exception e) when (e is JsonException or InvalidDataException or FormatException or InvalidOperationException ||
            e is VersionException { Code: VersionErrorCode.InvalidMetadata })
        { throw FabricMetadata.Invalid(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { throw new FabricException(FabricErrorCode.IoError, "Fabric 文件操作失败。"); }
        finally { gate.Release(); }
    }

    private static FileStream AcquireLease(string path)
    {
        try { return new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException e) when ((e.HResult & 0xffff) is 32 or 33 or 11)
        { throw new FabricException(FabricErrorCode.InstallationBusy, "同一目录已有 Fabric 安装任务。"); }
    }
    private static void ValidatePolicy(VersionCachePolicy policy)
    { if (!Enum.IsDefined(policy)) throw new ArgumentOutOfRangeException(nameof(policy)); }
    private static string Key(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T> { public void Report(T value) => report(value); }
}
