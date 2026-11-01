using System.Text.Json.Nodes;

namespace FMCore.Installer;

// 规划、检查和启动共用目录规则；这里只解析路径，不创建文件。
internal sealed record AssetLayout(
    string AssetsRoot,
    string ObjectsDirectory,
    string GameAssets,
    IReadOnlyList<string> MappingDirectories)
{
    internal static AssetLayout Read(
        JsonObject index, string minecraftRoot, string indexId,
        string gameDirectory)
    {
        PathSafety.ValidateWindowsRelativePath(indexId);
        if (indexId.Contains('/') || indexId.Contains('\\'))
            throw new InvalidDataException("资源索引 ID 必须是单个名称。");

        string assets = ResolveWithin(minecraftRoot, "assets");
        string objects = ResolveWithin(assets, "objects");
        bool virtualAssets = ReadFlag(index, "virtual");
        bool resources = ReadFlag(index, "map_to_resources");
        List<string> directories = [];
        string gameAssets = assets;

        if (resources)
        {
            gameAssets = ResolveWithin(gameDirectory, "resources");
            directories.Add(gameAssets);
        }
        if (virtualAssets)
        {
            gameAssets = ResolveWithin(assets, $"virtual/{indexId}");
            directories.Add(gameAssets);
        }

        return new AssetLayout(assets, objects, gameAssets,
            directories.AsReadOnly());
    }

    internal static string ResolveWithin(string root, string relativePath)
    {
        PathSafety.ValidateWindowsRelativePath(relativePath);
        root = PathSafety.RequireDirectoryPath(root);
        string target = PathSafety.Resolve(Path.Combine(root,
            relativePath.Replace('\\', '/').Replace('/', Path.DirectorySeparatorChar)));
        if (!PathSafety.IsWithin(target, root)
            || PathSafety.Comparer.Equals(target, root))
            throw new InvalidDataException("资源映射路径越出了目标目录。");
        return target;
    }

    private static bool ReadFlag(JsonObject index, string name)
    {
        if (!index.TryGetPropertyValue(name, out JsonNode? node))
            return false;
        if (node is JsonValue value && value.TryGetValue<bool>(out bool result))
            return result;
        throw new InvalidDataException($"资源索引的 {name} 必须是布尔值。");
    }
}
