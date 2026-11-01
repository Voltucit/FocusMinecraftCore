using System.Security.Cryptography;
using System.Text.Json.Nodes;
using FMCore.Downloads;
using FMCore.Versions;

namespace FMCore.Installer;

internal sealed record AssetDownloadPlan(
    string ObjectsDirectory,
    IReadOnlyList<DownloadRequest> Requests,
    IReadOnlyList<AssetMapping> Mappings,
    IReadOnlyList<string> MappingDirectories);

internal sealed record AssetMapping(string SourcePath, DownloadRequest Request);

internal static partial class ResourceDownloadPlanner
{
    internal const int MaximumIndexBytes = 16 * 1024 * 1024;

    internal static void ValidateIndexSize(
        DownloadRequest indexRequest)
    {
        ArgumentNullException.ThrowIfNull(indexRequest);

        if (indexRequest.Size <= 0
            || indexRequest.Size > MaximumIndexBytes)
        {
            throw new InvalidDataException(
                "资源索引大小必须大于 0，且不能超过 16 MiB。");
        }
    }

    internal static Task<AssetDownloadPlan> ReadAssetObjectsAsync(
        DownloadRequest indexRequest,
        string minecraftRoot,
        CancellationToken cancellationToken = default,
        string? gameDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(indexRequest);
        ArgumentException.ThrowIfNullOrWhiteSpace(minecraftRoot);

        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(
            () => ReadAssetObjectsCoreAsync(
                indexRequest,
                minecraftRoot,
                gameDirectory ?? minecraftRoot,
                cancellationToken),
            cancellationToken);
    }

    private static async Task<AssetDownloadPlan>
        ReadAssetObjectsCoreAsync(
            DownloadRequest indexRequest,
            string minecraftRoot,
            string gameDirectory,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DownloadRequest request =
            FileDownloader.NormalizeRequest(indexRequest);

        ValidateIndexSize(request);

        string assetsRoot = PathSafety.RequireDirectoryPath(
            Path.Combine(minecraftRoot, "assets"));

        if (!PathSafety.IsWithin(request.FilePath, assetsRoot))
        {
            throw new InvalidDataException(
                "资源索引不属于指定的 assets 目录。");
        }

        string objectsDirectory = PathSafety.RequireDirectoryPath(
            Path.Combine(assetsRoot, "objects"));

        if (!PathSafety.IsWithin(objectsDirectory, assetsRoot))
        {
            throw new InvalidDataException(
                "资源对象目录越出了 assets 目录。");
        }

        await using FileStream stream = new(
            request.FilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            FileOptions.Asynchronous
                | FileOptions.SequentialScan);

        if (stream.Length != request.Size)
        {
            throw new InvalidDataException(
                "资源索引大小与版本声明不一致。");
        }

        byte[] bytes = new byte[(int)request.Size];

        await stream.ReadExactlyAsync(
            bytes.AsMemory(),
            cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        string actualSha1 = Convert.ToHexString(
            SHA1.HashData(bytes)).ToLowerInvariant();

        if (actualSha1 != request.Sha1)
        {
            throw new InvalidDataException(
                "资源索引 SHA-1 与版本声明不一致。");
        }

        // JSON 解析跳过可选的 UTF-8 BOM，
        // 上面的哈希校验仍然使用全部原始字节。
        int offset = bytes.Length >= 3
            && bytes[0] == 0xEF
            && bytes[1] == 0xBB
            && bytes[2] == 0xBF
                ? 3
                : 0;

        JsonObject index =
            JsonNode.Parse(bytes.AsSpan(offset)) as JsonObject
            ?? throw new InvalidDataException(
                "资源索引的最外层必须是 JSON 对象。");

        AssetLayout layout = AssetLayout.Read(index, minecraftRoot,
            Path.GetFileNameWithoutExtension(request.FilePath), gameDirectory);

        if (index["objects"] is not JsonObject entries)
        {
            throw new InvalidDataException(
                "资源索引的 objects 必须是 JSON 对象。");
        }

        Dictionary<string, DownloadRequest> requests =
            new(StringComparer.Ordinal);

        Dictionary<string, AssetMapping> mappings = new(PathSafety.Comparer);

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.Value is not JsonObject file)
            {
                throw new InvalidDataException(
                    $"资源 {entry.Key} 的声明必须是对象。");
            }

            string hash = MinecraftVersionReader.ReadRequiredString(
                file,
                "hash",
                $"资源 {entry.Key} 缺少有效的 hash。");

            if (hash.Length != 40 || !hash.All(Uri.IsHexDigit))
            {
                throw new InvalidDataException(
                    $"资源 {entry.Key} 的 hash 格式错误。");
            }

            hash = hash.ToLowerInvariant();

            if (file["size"] is not JsonValue sizeValue
                || !sizeValue.TryGetValue<long>(out long size)
                || size < 0)
            {
                throw new InvalidDataException(
                    $"资源 {entry.Key} 的 size 必须是非负整数。");
            }

            if (!requests.TryGetValue(hash, out DownloadRequest? source))
            {
                source = FileDownloader.NormalizeRequest(new DownloadRequest
                {
                    Url = "https://resources.download.minecraft.net/" + $"{hash[..2]}/{hash}",
                    FilePath = AssetLayout.ResolveWithin(objectsDirectory, $"{hash[..2]}/{hash}"),
                    Size = size,
                    Sha1 = hash
                });
                requests.Add(hash, source);
            }
            else if (source.Size != size)
            {
                throw new InvalidDataException($"同一资源哈希声明了不同大小：{hash}");
            }

            // 一个哈希可以有多个逻辑名称；对象去重不应丢失任何映射。
            foreach (string directory in layout.MappingDirectories)
            {
                string target = AssetLayout.ResolveWithin(directory, entry.Key);
                if (mappings.TryGetValue(target, out AssetMapping? previous))
                {
                    if (previous.Request.Sha1 != hash)
                        throw new InvalidDataException($"资源映射目标冲突：{entry.Key}");
                    continue;
                }
                mappings.Add(target, new AssetMapping(source.FilePath,
                    FileDownloader.NormalizeRequest(new DownloadRequest
                    {
                        Url = source.Url, FilePath = target, Size = size, Sha1 = hash
                    })));
            }
        }

        foreach (string target in mappings.Keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (string? parent = Path.GetDirectoryName(target);
                 parent is not null; parent = Path.GetDirectoryName(parent))
            {
                if (mappings.ContainsKey(parent))
                    throw new InvalidDataException($"资源映射文件与父目录冲突：{parent}");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        return new AssetDownloadPlan(
            objectsDirectory,
            Array.AsReadOnly(requests.Values.ToArray()),
            Array.AsReadOnly(mappings.Values.ToArray()),
            layout.MappingDirectories);
    }

}
