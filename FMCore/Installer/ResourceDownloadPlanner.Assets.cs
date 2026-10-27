using System.Security.Cryptography;
using System.Text.Json.Nodes;
using FMCore.Downloads;
using FMCore.Versions;

namespace FMCore.Installer;

internal sealed record AssetDownloadPlan(
    string ObjectsDirectory,
    IReadOnlyList<DownloadRequest> Requests);

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
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(indexRequest);
        ArgumentException.ThrowIfNullOrWhiteSpace(minecraftRoot);

        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(
            () => ReadAssetObjectsCoreAsync(
                indexRequest,
                minecraftRoot,
                cancellationToken),
            cancellationToken);
    }

    private static async Task<AssetDownloadPlan>
        ReadAssetObjectsCoreAsync(
            DownloadRequest indexRequest,
            string minecraftRoot,
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

        if (ReadAssetFlag(index, "virtual")
            || ReadAssetFlag(index, "map_to_resources"))
        {
            throw new NotSupportedException(
                "暂未支持 virtual 或 map_to_resources 资源映射。");
        }

        if (index["objects"] is not JsonObject entries)
        {
            throw new InvalidDataException(
                "资源索引的 objects 必须是 JSON 对象。");
        }

        Dictionary<string, DownloadRequest> requests =
            new(StringComparer.Ordinal);

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

            if (requests.TryGetValue(
                    hash,
                    out DownloadRequest? previous))
            {
                if (previous.Size != size)
                {
                    throw new InvalidDataException(
                        $"同一资源哈希声明了不同大小：{hash}");
                }

                continue;
            }

            string filePath = PathSafety.Resolve(
                Path.Combine(
                    objectsDirectory,
                    hash[..2],
                    hash));

            if (!PathSafety.IsWithin(
                    filePath,
                    objectsDirectory))
            {
                throw new InvalidDataException(
                    "资源文件路径越出了 objects 目录。");
            }

            requests.Add(
                hash,
                FileDownloader.NormalizeRequest(
                    new DownloadRequest
                    {
                        Url =
                            "https://resources.download.minecraft.net/" +
                            $"{hash[..2]}/{hash}",

                        FilePath = filePath,
                        Size = size,
                        Sha1 = hash
                    }));
        }

        cancellationToken.ThrowIfCancellationRequested();

        return new AssetDownloadPlan(
            objectsDirectory,
            Array.AsReadOnly(requests.Values.ToArray()));
    }

    private static bool ReadAssetFlag(
        JsonObject index,
        string name)
    {
        if (!index.TryGetPropertyValue(name, out JsonNode? node))
            return false;

        if (node is JsonValue value
            && value.TryGetValue<bool>(out bool result))
        {
            return result;
        }

        throw new InvalidDataException(
            $"资源索引的 {name} 必须是布尔值。");
    }
}
