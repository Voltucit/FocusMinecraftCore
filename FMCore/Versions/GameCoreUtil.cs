using FMCore.Downloader;

namespace FMCore.Versions;

/// <summary>A loader recognized from existing metadata; recognition does not install or verify the loader.</summary>
public sealed record GameLoaderInfo(string Name, string Version);

/// <summary>Local inventory with resolved version metadata and Java requirements. File existence does not prove a complete installation.</summary>
public sealed record GameCoreInfo(string Id, string? Type, string? MinecraftVersion, string? MainClass,
    JavaRequirement? JavaRequirement, IReadOnlyList<GameLoaderInfo> Loaders, string? ClientJarPath,
    bool HasClientJar, ResolvedVersion? ResolvedVersion, VersionErrorCode? ErrorCode);

/// <summary>Local game inventory. These APIs never download metadata or require an account.</summary>
public static class GameCoreUtil
{
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
        var path = DownloadPath.Resolve(manager.RootDirectory, $"versions/{version.ClientJarId}/{version.ClientJarId}.jar");
        var loaders = new List<GameLoaderInfo>();
        foreach (var coordinate in version.Metadata.Libraries.Select(l => l.Coordinate))
        {
            var name = (coordinate.Group, coordinate.Artifact) switch
            {
                ("net.fabricmc", "fabric-loader") => "Fabric",
                ("org.quiltmc", "quilt-loader") => "Quilt",
                ("net.neoforged", "neoforge" or "forge") => "NeoForge",
                ("net.minecraftforge", "forge" or "fmlloader") => "Forge",
                _ => null
            };
            if (name != null && !loaders.Any(l => l.Name == name)) loaders.Add(new(name, coordinate.Version));
        }
        return new(id, version.Metadata.Type, version.InheritanceChain.Count > 1 ? version.InheritanceChain[0] : version.ClientJarId, version.Metadata.MainClass, version.Metadata.Java,
            loaders.AsReadOnly(), path, File.Exists(path) && new FileInfo(path).Length > 0, version, null);
    }
}
