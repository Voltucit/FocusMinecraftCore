using System.Text.Json.Nodes;
using FMCore.Installer;
using FMCore.Versions;

namespace FMCore.Launch;

internal sealed record AssetPaths(
    string AssetsRoot,
    string IndexId,
    string IndexFile,
    string ObjectsDirectory);

internal sealed record LoggingConfig(
    string FilePath,
    string Argument);

internal static class LaunchResourceResolver
{
    internal static AssetPaths ResolveAssets(
        MinecraftVersion version,
        string minecraftRoot)
    {
        JsonObject declaration = ReadInheritedObject(
            version, minecraftRoot, "assetIndex")
            ?? throw new NotSupportedException(
                "当前需要 assetIndex 声明，暂不支持只有 assets 字段的旧格式。");

        string id = MinecraftVersionReader.ReadRequiredString(
            declaration, "id", "assetIndex 缺少有效的 id。");

        ValidateFileName(id);

        string assetsRoot = GetAssetsRoot(minecraftRoot);

        string indexFile = RequireFile(
            ResolveAssetPath(assetsRoot, $"indexes/{id}.json"));

        JsonObject index =
            JsonNode.Parse(File.ReadAllText(indexFile)) as JsonObject
            ?? throw new InvalidDataException(
                "资源索引的最外层必须是 JSON 对象。");

        if (index["objects"] is not JsonObject)
            throw new InvalidDataException(
                "资源索引的 objects 必须是 JSON 对象。");

        if (ReadFlag(index, "virtual")
            || ReadFlag(index, "map_to_resources"))
            throw new NotSupportedException(
                "暂未支持 virtual 或 map_to_resources 资源映射。");

        string objectsDirectory =
            ResolveAssetPath(assetsRoot, "objects");

        if (File.Exists(objectsDirectory))
            throw new IOException(
                $"资源对象目录被文件占用：{objectsDirectory}");

        if (!Directory.Exists(objectsDirectory))
            throw new DirectoryNotFoundException(
                $"缺少资源对象目录：{objectsDirectory}");

        return new AssetPaths(
            assetsRoot, id, indexFile, objectsDirectory);
    }

    internal static LoggingConfig? ResolveLogging(
        MinecraftVersion version,
        string minecraftRoot)
    {
        JsonObject? logging = ReadInheritedObject(
            version, minecraftRoot, "logging");

        if (logging is null || !logging.ContainsKey("client"))
            return null;

        if (logging["client"] is not JsonObject client)
            throw new InvalidDataException(
                "logging.client 必须是 JSON 对象。");

        string type = MinecraftVersionReader.ReadRequiredString(
            client, "type", "日志声明缺少有效的 type。");

        string argument = MinecraftVersionReader.ReadRequiredString(
            client, "argument", "日志声明缺少有效的 argument。");

        // 先支持真实样本中使用的标准格式。
        if (type != "log4j2-xml"
            || argument != "-Dlog4j.configurationFile=${path}")
            throw new NotSupportedException(
                "暂未支持这种日志类型或参数模板。");

        if (client["file"] is not JsonObject file)
            throw new InvalidDataException(
                "logging.client.file 必须是 JSON 对象。");

        string id = MinecraftVersionReader.ReadRequiredString(
            file, "id", "日志配置文件缺少有效的 id。");

        ValidateFileName(id);

        string assetsRoot = GetAssetsRoot(minecraftRoot);

        string filePath = RequireFile(
            ResolveAssetPath(assetsRoot, $"log_configs/{id}"));

        return new LoggingConfig(
            filePath,
            argument.Replace(
                "${path}", filePath, StringComparison.Ordinal));
    }

    // 从当前版本向父版本查找，使用最近一层的完整字段。
    internal static JsonObject? ReadInheritedObject(
        MinecraftVersion version,
        string minecraftRoot,
        string property)
    {
        IReadOnlyList<MinecraftVersion> chain =
            LibraryResolver.GetValidatedChain(version, minecraftRoot);

        foreach (MinecraftVersion current in chain)
        {
            JsonObject data =
                MinecraftVersionReader.ReadVersionJson(current.Directory);

            if (!data.TryGetPropertyValue(property, out JsonNode? node))
                continue;

            if (node is not JsonObject result)
                throw new InvalidDataException(
                    $"版本 {current.Id} 的 {property} 必须是 JSON 对象。");

            return result;
        }

        return null;
    }

    private static string GetAssetsRoot(string minecraftRoot)
    {
        string root = PathSafety.RequireDirectoryPath(
            Path.Combine(minecraftRoot, "assets"));

        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException(
                $"资源根目录不存在：{root}");

        return root;
    }

    private static string ResolveAssetPath(
        string assetsRoot,
        string relative)
    {
        PathSafety.ValidateWindowsRelativePath(relative);

        string target = PathSafety.Resolve(
            Path.Combine(
                assetsRoot,
                relative.Replace('/', Path.DirectorySeparatorChar)));

        if (!PathSafety.IsWithin(target, assetsRoot))
            throw new InvalidDataException(
                "资源或日志配置路径越出了 assets 目录。");

        return target;
    }

    private static string RequireFile(string path)
    {
        if (Directory.Exists(path))
            throw new IOException(
                $"文件路径被目录占用：{path}");

        if (!File.Exists(path))
            throw new FileNotFoundException(
                "缺少资源索引或日志配置文件，请先补全游戏文件。",
                path);

        return path;
    }

    private static void ValidateFileName(string name)
    {
        PathSafety.ValidateWindowsRelativePath(name);

        if (name.Contains('/') || name.Contains('\\'))
            throw new InvalidDataException(
                "资源索引 ID 和日志文件 ID 必须是单个名称。");
    }

    private static bool ReadFlag(JsonObject data, string key)
    {
        if (!data.TryGetPropertyValue(key, out JsonNode? node))
            return false;

        if (node is JsonValue value
            && value.TryGetValue<bool>(out bool result))
            return result;

        throw new InvalidDataException(
            $"资源索引的 {key} 必须是布尔值。");
    }
}
