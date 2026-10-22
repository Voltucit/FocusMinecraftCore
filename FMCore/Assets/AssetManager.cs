using FMCore.Downloader;
using FMCore.Utilities;
using FMCore.Versions;

namespace FMCore.Assets;

public sealed record AssetIndexResult(AssetIndex Index, DownloadResult Download);
public sealed record AssetLayoutProgress(int CompletedFiles, int TotalFiles);
public sealed record AssetLayoutResult(string AssetsRootDirectory, string GameAssetsDirectory, string? ResourcesDirectory);

public sealed class AssetManager(DownloadManager downloads)
{
    public string RootDirectory => downloads.RootDirectory;

    public async Task<AssetIndexResult> GetIndexAsync(AssetIndexInfo info, IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(info);
        if (info.Download.Size > 16 * 1024 * 1024)
            throw new AssetException(AssetErrorCode.InvalidIndex, "资源索引超过 16 MiB 限制。");
        try
        {
            var result = await downloads.DownloadAsync(IndexRequest(info), progress, cancellationToken).ConfigureAwait(false);
            return new(AssetIndex.Parse(await GameFiles.ReadJsonAsync(result.FullPath, cancellationToken).ConfigureAwait(false)), result);
        }
        catch (InvalidDataException) { throw new AssetException(AssetErrorCode.InvalidIndex, "资源索引超过 16 MiB 限制。"); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { throw new AssetException(AssetErrorCode.IoError, "资源索引文件操作失败。"); }
    }

    public IReadOnlyList<DownloadRequest> CreateDownloads(AssetIndex index) => index.Objects.DistinctBy(o => o.Hash)
        .Select(o => new DownloadRequest(new Uri($"https://resources.download.minecraft.net/{o.Hash[..2]}/{o.Hash}"), o.RelativePath)
        { ExpectedHash = o.Hash, ExpectedSize = o.Size }).ToArray();

    public async Task<AssetLayoutResult> MaterializeAsync(AssetIndex index, string indexId, string gameDirectory = "",
        IProgress<AssetLayoutProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(index);
        cancellationToken.ThrowIfCancellationRequested();
        VersionParser.Id(indexId);
        if (gameDirectory.Length != 0) GameFiles.Resolve(RootDirectory, gameDirectory);
        var virtualRelative = $"assets/virtual/{indexId}";
        var resourcesRelative = gameDirectory.Length == 0 ? "resources" : gameDirectory.TrimEnd('/', '\\') + "/resources";
        var targets = LayoutTargets(index, indexId, gameDirectory).ToList();
        // Validate every layout destination before making any copies.
        foreach (var (_, relative) in targets) GameFiles.Resolve(RootDirectory, relative);
        try
        {
            var completed = 0;
            progress?.Report(new(0, targets.Count));
            foreach (var (asset, relative) in targets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var source = GameFiles.Resolve(RootDirectory, asset.RelativePath);
                var destination = GameFiles.Resolve(RootDirectory, relative);
                if (!await GameFiles.HasSha1Async(destination, asset.Hash, asset.Size, cancellationToken).ConfigureAwait(false))
                {
                    if (!File.Exists(source)) throw new AssetException(AssetErrorCode.MissingObject, "资源对象尚未下载。");
                    await GameFiles.WriteAtomicAsync(destination, async (output, ct) =>
                    {
                        await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
                        using var hash = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA1);
                        var buffer = new byte[81920];
                        long length = 0;
                        int count;
                        while ((count = await input.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                        {
                            length += count;
                            if (length > asset.Size) throw Corrupt();
                            hash.AppendData(buffer, 0, count);
                            await output.WriteAsync(buffer.AsMemory(0, count), ct).ConfigureAwait(false);
                        }
                        if (length != asset.Size || !Convert.ToHexString(hash.GetHashAndReset()).Equals(asset.Hash, StringComparison.OrdinalIgnoreCase)) throw Corrupt();
                    }, cancellationToken).ConfigureAwait(false);
                }
                progress?.Report(new(++completed, targets.Count));
            }
            // Empty legacy indexes must still have an existing directory for launch placeholders.
            cancellationToken.ThrowIfCancellationRequested();
            var assets = GameFiles.Resolve(RootDirectory, "assets");
            GameFiles.CreateDirectory(assets);
            if (index.Virtual) GameFiles.CreateDirectory(GameFiles.Resolve(RootDirectory, virtualRelative));
            if (index.MapToResources) GameFiles.CreateDirectory(GameFiles.Resolve(RootDirectory, resourcesRelative));
            return new(assets, index.Virtual ? GameFiles.Resolve(RootDirectory, virtualRelative)
                : index.MapToResources ? GameFiles.Resolve(RootDirectory, resourcesRelative) : assets,
                index.MapToResources ? GameFiles.Resolve(RootDirectory, resourcesRelative) : null);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { throw new AssetException(AssetErrorCode.IoError, "旧版资源布局文件操作失败。"); }
    }

    internal static DownloadRequest IndexRequest(AssetIndexInfo info) => new(info.Download.Url, $"assets/indexes/{VersionParser.Id(info.Id)}.json")
    { ExpectedHash = info.Download.Sha1, ExpectedSize = info.Download.Size };

    internal static IEnumerable<(AssetObject Asset, string Relative)> LayoutTargets(AssetIndex index, string indexId, string gameDirectory)
    {
        foreach (var asset in index.Objects)
        {
            if (index.Virtual) yield return (asset, $"assets/virtual/{indexId}/{asset.Name}");
            if (index.MapToResources) yield return (asset, (gameDirectory.Length == 0 ? "" : gameDirectory.TrimEnd('/', '\\') + "/") + "resources/" + asset.Name);
        }
    }

    private static AssetException Corrupt() => new(AssetErrorCode.HashMismatch, "资源对象大小或哈希无效，无法生成旧版布局。");
}
