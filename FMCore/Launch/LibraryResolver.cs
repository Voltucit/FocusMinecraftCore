using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FMCore.Internal;
using FMCore.Versions;

namespace FMCore.Launch;

// 当前阶段的内部步骤；调用方以后由启动入口统一编排，不暴露半成品启动管线。
internal static class LibraryResolver
{
    internal static IReadOnlyList<JsonObject> ReadLibraryEntries(MinecraftVersion version)
    {
        JsonObject data = MinecraftVersionReader.ReadVersionJson(version.Directory);
        if (!data.TryGetPropertyValue("libraries", out JsonNode? node))
            return [];
        if (node is not JsonArray array)
            throw new InvalidDataException($"版本 {version.Id} 的 libraries 必须是列表。");
        List<JsonObject> entries = [];
        foreach (JsonNode? entry in array)
        {
            if (entry is not JsonObject library)
                throw new InvalidDataException("依赖声明必须是对象。");
            _ = ReadName(library);
            entries.Add(library);
        }
        return entries;
    }

    internal static bool IsLibraryAllowed(JsonObject library, string osName)
    {
        if (osName is not ("windows" or "linux" or "osx"))
            throw new ArgumentException("osName 必须是 windows、linux 或 osx。", nameof(osName));
        if (!library.TryGetPropertyValue("rules", out JsonNode? rulesNode))
            return true;
        if (rulesNode is not JsonArray rules)
            throw new InvalidDataException("依赖库的 rules 必须是列表。");
        if (rules.Count == 0)
            return true;
        bool allowed = false;
        foreach (JsonNode? node in rules)
        {
            if (node is not JsonObject rule)
                throw new InvalidDataException("依赖规则必须是对象。");
            if (rule.Any(pair => pair.Key is not ("action" or "os")))
                throw new NotSupportedException("暂未支持额外的依赖规则字段。");
            string action = MinecraftVersionReader.ReadRequiredString(rule, "action", "规则缺少 action。");
            if (action is not ("allow" or "disallow"))
                throw new InvalidDataException("规则 action 必须是 allow 或 disallow。");
            JsonObject os = new();
            if (rule.TryGetPropertyValue("os", out JsonNode? osNode))
                os = osNode as JsonObject ?? throw new InvalidDataException("规则 os 必须是对象。");
            if (os.Any(pair => pair.Key != "name"))
                throw new NotSupportedException("暂未支持 os.arch、os.version 等额外系统条件。");
            if (os.ContainsKey("name"))
            {
                string name = MinecraftVersionReader.ReadRequiredString(os, "name", "os.name 必须是非空字符串。");
                if (name != osName)
                    continue;
            }
            allowed = action == "allow";
        }
        return allowed;
    }

    internal static string MavenToPath(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        int at = name.IndexOf('@');
        string coordinate = at < 0 ? name : name[..at];
        string extension = at < 0 ? "jar" : name[(at + 1)..];
        string[] parts = coordinate.Split(':');
        if (parts.Length is not (3 or 4))
            throw new InvalidDataException("依赖坐标应为 分组:名称:版本[:分类][@扩展名]。");
        string[] groups = parts[0].Split('.');
        foreach (string component in groups.Concat(parts.Skip(1)).Append(extension))
        {
            if (!Regex.IsMatch(component, @"\A[A-Za-z0-9_][A-Za-z0-9_.+\-]*\z", RegexOptions.CultureInvariant))
                throw new InvalidDataException("依赖坐标包含空片段或不支持的路径字符。");
        }
        string filename = $"{parts[1]}-{parts[2]}" + (parts.Length == 4 ? "-" + parts[3] : "") + "." + extension;
        string relative = string.Join('/', groups.Concat([parts[1], parts[2], filename]));
        PathSafety.ValidateWindowsRelativePath(relative);
        return relative.Replace('/', Path.DirectorySeparatorChar);
    }

    internal static string? GetLibraryPath(JsonObject library, string minecraftRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(minecraftRoot);
        string rawPath;
        if (library["downloads"] is null)
        {
            if (library["natives"] is not null)
                throw new NotSupportedException("旧格式原生库路径将在原生库步骤中处理。");
            rawPath = MavenToPath(ReadName(library));
        }
        else
        {
            JsonObject downloads = library["downloads"] as JsonObject
                ?? throw new InvalidDataException("依赖库的 downloads 必须是对象。");
            if (downloads["artifact"] is null)
            {
                if (downloads["classifiers"] is JsonObject { Count: > 0 }
                    && library["natives"] is JsonObject { Count: > 0 })
                    return null;
                throw new InvalidDataException("缺少普通 artifact 或明确的原生库下载信息。");
            }
            JsonObject artifact = downloads["artifact"] as JsonObject
                ?? throw new InvalidDataException("依赖库的 artifact 必须是对象。");
            rawPath = artifact.ContainsKey("path")
                ? MinecraftVersionReader.ReadRequiredString(artifact, "path", "依赖文件路径必须是非空字符串。")
                : MavenToPath(ReadName(library));
        }
        PathSafety.ValidateWindowsRelativePath(rawPath);
        string basePath = PathSafety.RequireDirectoryPath(Path.Combine(minecraftRoot, "libraries"));
        string target = PathSafety.Resolve(Path.Combine(basePath, rawPath.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar)));
        if (!PathSafety.IsWithin(target, basePath))
            throw new InvalidDataException("依赖文件路径越出了公共 libraries 目录。");
        if (Directory.Exists(target))
            throw new IOException($"依赖文件路径被目录占用：{target}");
        return target;
    }

    private static string ReadName(JsonObject library)
        => MinecraftVersionReader.ReadRequiredString(library, "name", "依赖库缺少有效 name。");
}
