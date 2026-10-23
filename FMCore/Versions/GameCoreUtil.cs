using FMCore.Downloader;

namespace FMCore.Versions;

/// <summary>A loader recognized from existing metadata; recognition does not install or verify the loader.</summary>
public sealed record GameLoaderInfo(string Name, string Version);

/// <summary>Local inventory with resolved version metadata and Java requirements. File existence does not prove a complete installation.</summary>
public sealed record GameCoreInfo(string Id, string? Type, string? MinecraftVersion, string? MainClass,
    JavaRequirement? JavaRequirement, IReadOnlyList<GameLoaderInfo> Loaders, string? ClientJarPath,
    bool HasClientJar, ResolvedVersion? ResolvedVersion, VersionErrorCode? ErrorCode)
{
    /// <summary>原始 JSON id；查询/启动时使用 Id（实际目录名）。</summary>
    public string? MetadataId => ResolvedVersion?.Metadata.Id;
    /// <summary>实际版本目录名，区别于 JSON id 和宿主指定的工作目录。</summary>
    public string DirectoryName => ResolvedVersion?.DirectoryName ?? Id;
    /// <summary>调用方指定的 Minecraft 根目录。</summary>
    public string? RootDirectory { get; init; }
    /// <summary>实际版本目录的绝对路径。</summary>
    public string? VersionDirectory { get; init; }
    /// <summary>实际启动 JSON 的绝对路径。</summary>
    public string? MetadataPath => ResolvedVersion?.MetadataPath;
    /// <summary>是否包含本地自定义/未证实官方来源的配置。不是启动能力分类。</summary>
    public bool IsCustom => ResolvedVersion?.IsCustom == true;
    /// <summary>信任与校验范围提示。启动任意第三方代码前应由宿主向用户展示。</summary>
    public IReadOnlyList<string> Warnings => ResolvedVersion?.Warnings ?? [];
}

/// <summary>官方版本目录与本地游戏信息便捷入口；本地查询不联网，目录查询遵循缓存策略，无需账号。</summary>
public static class GameCoreUtil
{
    /// <summary>查询官方可用版本目录，含正式版、快照及最新版本标识；不安装游戏。</summary>
    /// <param name="root">元数据缓存所在游戏根目录，默认 .minecraft。</param>
    /// <param name="cachePolicy">缓存策略；CacheOnly 不联网，Refresh 强制刷新。</param>
    /// <param name="cancellationToken">取消查询。</param>
    /// <returns>可直接读取 Versions 与 LatestRelease 的强类型目录。</returns>
    public static Task<VersionManifest> GetAvailableVersionsAsync(string root = ".minecraft",
        VersionCachePolicy cachePolicy = VersionCachePolicy.PreferCache, CancellationToken cancellationToken = default) =>
        new VersionManager(new DownloadManager(root)).GetManifestAsync(cachePolicy, cancellationToken);

    /// <summary>Enumerates local versions, retaining invalid entries with their error codes.</summary>
    public static async Task<IReadOnlyList<GameCoreInfo>> GetGameCoresAsync(string root = ".minecraft", CancellationToken cancellationToken = default)
    {
        var manager = new VersionManager(new DownloadManager(root));
        var results = new List<GameCoreInfo>();
        foreach (var local in await manager.GetLocalVersionsAsync(cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { results.Add(await ReadAsync(manager, local.Id, cancellationToken).ConfigureAwait(false)); }
            catch (VersionException error)
            { results.Add(new(local.Id, local.Type, null, null, null, [], local.ClientJarPath, false, null, error.Code)); }
        }
        return results.AsReadOnly();
    }

    /// <summary>Reads one local version and its inheritance chain; throws VersionException for missing or invalid metadata.</summary>
    public static Task<GameCoreInfo> GetGameCoreAsync(string versionId, string root = ".minecraft", CancellationToken cancellationToken = default) =>
        ReadAsync(new VersionManager(new DownloadManager(root)), versionId, cancellationToken);

    /// <summary>Blocking convenience API; prefer GetGameCoresAsync in UI applications.</summary>
    public static IReadOnlyList<GameCoreInfo> GetGameCores(string root = ".minecraft") =>
        Task.Run(() => GetGameCoresAsync(root)).GetAwaiter().GetResult();

    /// <summary>Blocking convenience API; prefer GetGameCoreAsync in UI applications.</summary>
    public static GameCoreInfo GetGameCore(string versionId, string root = ".minecraft") =>
        Task.Run(() => GetGameCoreAsync(versionId, root)).GetAwaiter().GetResult();

    private static async Task<GameCoreInfo> ReadAsync(VersionManager manager, string id, CancellationToken ct)
    {
        var version = await manager.ResolveAsync(id, VersionCachePolicy.CacheOnly, ct).ConfigureAwait(false);
        var path = DownloadPath.Resolve(manager.RootDirectory, version.ClientJarRelativePath ?? $"versions/{version.ClientJarId}/{version.ClientJarId}.jar");
        var loaders = new List<GameLoaderInfo>();
        foreach (var coordinate in version.Metadata.Libraries.Select(l => l.Coordinate))
        {
            var name = (coordinate.Group, coordinate.Artifact) switch
            {
                ("net.fabricmc", "fabric-loader") => "Fabric",
                ("org.quiltmc", "quilt-loader") => "Quilt",
                ("net.neoforged", "neoforge" or "forge") => "NeoForge",
                ("net.neoforged.fancymodloader", "loader") => "NeoForge",
                ("optifine", "OptiFine") => "OptiFine",
                ("net.minecraftforge", "forge" or "fmlloader") => "Forge",
                ("com.mumfrey", "liteloader") => "LiteLoader",
                _ => null
            };
            if (name != null && !loaders.Any(l => l.Name == name)) loaders.Add(new(name, coordinate.Version));
        }
        var argumentText = string.Join(' ', version.Metadata.Arguments.Game.SelectMany(a => a.Values));
        var main = version.Metadata.MainClass ?? "";
        void Recognize(string name)
        { if (!loaders.Any(l => l.Name == name)) loaders.Add(new(name, "")); }
        if (main.StartsWith("org.quiltmc.loader.", StringComparison.Ordinal)) Recognize("Quilt");
        if (main.StartsWith("net.fabricmc.loader.", StringComparison.Ordinal)) Recognize("Fabric");
        if (argumentText.Contains("com.mumfrey.liteloader.", StringComparison.Ordinal)) Recognize("LiteLoader");
        if (argumentText.Contains("optifine.", StringComparison.Ordinal)) Recognize("OptiFine");
        if (argumentText.Contains("cpw.mods.fml.", StringComparison.Ordinal) || argumentText.Contains("net.minecraftforge.fml.", StringComparison.Ordinal)) Recognize("Forge");
        if (loaders.Count == 0 && (main.Length > 0 && main is not ("net.minecraft.client.main.Main" or "net.minecraft.client.Minecraft" or "com.mojang.rubydung.RubyDung") || argumentText.Contains("--tweakClass", StringComparison.Ordinal)))
            loaders.Add(new("Custom", ""));
        var receipt = version.Metadata.ToJson()["fmcoreInstaller"];
        if (receipt?["name"]?.GetValue<string>() is string installedName && installedName is "Forge" or "NeoForge" or "OptiFine" &&
            receipt["version"]?.GetValue<string>() is string installedVersion)
        {
            loaders.RemoveAll(l => l.Name == installedName); loaders.Add(new(installedName, installedVersion));
        }
        return new(version.DirectoryName, version.Metadata.Type, version.MinecraftVersion, version.Metadata.MainClass, version.Metadata.Java,
            loaders.AsReadOnly(), path, File.Exists(path) && new FileInfo(path).Length > 0, version, null)
            { RootDirectory = manager.RootDirectory, VersionDirectory = DownloadPath.Resolve(manager.RootDirectory, "versions/" + version.DirectoryName) };
    }
}
