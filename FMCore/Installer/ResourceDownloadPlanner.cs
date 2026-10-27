using System.Text.Json.Nodes;
using FMCore.Downloads;
using FMCore.Launch;
using FMCore.Versions;

namespace FMCore.Installer;

internal static partial class ResourceDownloadPlanner
{
    internal static Task<IReadOnlyList<DownloadRequest>>
        ReadMetadataAsync(
            string versionId,
            string minecraftRoot,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(minecraftRoot);

        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(
            () => ReadMetadata(
                versionId,
                minecraftRoot,
                cancellationToken),
            cancellationToken);
    }

    private static IReadOnlyList<DownloadRequest> ReadMetadata(
        string versionId,
        string minecraftRoot,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string root =
            PathSafety.RequireDirectoryPath(minecraftRoot);

        MinecraftVersion version =
            new MinecraftVersionReader().GetVersion(
                versionId,
                root);

        string assetsRoot = PathSafety.RequireDirectoryPath(
            Path.Combine(root, "assets"));

        List<DownloadRequest> requests = [];

        cancellationToken.ThrowIfCancellationRequested();

        JsonObject assetIndex =
            LaunchResourceResolver.ReadInheritedObject(
                version,
                root,
                "assetIndex")
            ?? throw new NotSupportedException(
                "当前需要 assetIndex 声明，" +
                "暂不支持只有 assets 字段的旧格式。");

        string indexId = ReadSafeId(
            assetIndex,
            "资源索引");

        requests.Add(CreateRequest(
            assetIndex,
            assetsRoot,
            $"indexes/{indexId}.json"));

        cancellationToken.ThrowIfCancellationRequested();

        JsonObject? logging =
            LaunchResourceResolver.ReadInheritedObject(
                version,
                root,
                "logging");

        if (logging is not null
            && logging.TryGetPropertyValue(
                "client",
                out JsonNode? clientNode))
        {
            if (clientNode is not JsonObject client)
            {
                throw new InvalidDataException(
                    "logging.client 必须是 JSON 对象。");
            }

            if (client["file"] is not JsonObject file)
            {
                throw new InvalidDataException(
                    "logging.client.file 必须是 JSON 对象。");
            }

            string loggingId = ReadSafeId(
                file,
                "日志配置");

            requests.Add(CreateRequest(
                file,
                assetsRoot,
                $"log_configs/{loggingId}"));
        }

        HashSet<string> targets =
            new(PathSafety.Comparer);

        foreach (DownloadRequest request in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!targets.Add(request.FilePath))
            {
                throw new InvalidDataException(
                    $"资源下载声明存在重复目标：{request.FilePath}");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        return Array.AsReadOnly(requests.ToArray());
    }

    private static string ReadSafeId(
        JsonObject declaration,
        string description)
    {
        string id = MinecraftVersionReader.ReadRequiredString(
            declaration,
            "id",
            $"{description}缺少有效的 id。");

        PathSafety.ValidateWindowsRelativePath(id);

        if (id.Contains('/') || id.Contains('\\'))
        {
            throw new InvalidDataException(
                $"{description}的 id 必须是单个名称。");
        }

        return id;
    }

    private static DownloadRequest CreateRequest(
        JsonObject declaration,
        string assetsRoot,
        string relativePath)
    {
        string url = MinecraftVersionReader.ReadRequiredString(
            declaration,
            "url",
            $"下载声明 {relativePath} 缺少有效的 url。");

        string sha1 = MinecraftVersionReader.ReadRequiredString(
            declaration,
            "sha1",
            $"下载声明 {relativePath} 缺少有效的 sha1。");

        if (declaration["size"] is not JsonValue sizeValue
            || !sizeValue.TryGetValue<long>(out long size)
            || size < 0)
        {
            throw new InvalidDataException(
                $"下载声明 {relativePath} 的 size " +
                "必须是非负整数。");
        }

        PathSafety.ValidateWindowsRelativePath(relativePath);

        string filePath = PathSafety.Resolve(
            Path.Combine(
                assetsRoot,
                relativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar)));

        if (!PathSafety.IsWithin(filePath, assetsRoot))
        {
            throw new InvalidDataException(
                "资源下载目标越出了 assets 目录。");
        }

        return FileDownloader.NormalizeRequest(
            new DownloadRequest
            {
                Url = url,
                FilePath = filePath,
                Size = size,
                Sha1 = sha1
            });
    }
}
