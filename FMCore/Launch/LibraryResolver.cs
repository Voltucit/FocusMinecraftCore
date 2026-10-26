using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FMCore.Installer;
using FMCore.Versions;
using FMCore.Java;

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
        return ResolveLibraryPath(rawPath, minecraftRoot);
    }

    internal static string? ReadNativeClassifier(
        JsonObject library,
        string osName)
    {
        ArgumentNullException.ThrowIfNull(library);

        if (osName is not ("windows" or "linux" or "osx"))
            throw new ArgumentException(
                "osName 必须是 windows、linux 或 osx。",
                nameof(osName));

        if (!library.ContainsKey("natives"))
            return null;

        if (library["natives"] is not JsonObject natives)
            throw new InvalidDataException(
                "依赖库的 natives 必须是 JSON 对象。");

        if (!natives.ContainsKey(osName))
            return null;

        return MinecraftVersionReader.ReadRequiredString(
            natives,
            osName,
            "原生库分类名称必须是非空字符串。");
    }

    internal static string ExpandNativeClassifier(
        string classifier,
        JavaInfo java)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(classifier);
        ArgumentNullException.ThrowIfNull(java);

        if (!classifier.Contains("${arch}"))
            return classifier;

        string bits;

        if (java.Architecture == "x86")
            bits = "32";
        else if (java.Architecture == "x64")
            bits = "64";
        else
            throw new NotSupportedException(
                "旧式 ${arch} 模板目前只支持 x86 或 x64 Java，请检查所选 Java 的架构。");

        return classifier.Replace("${arch}", bits);
    }

    private static string ResolveLibraryPath(
        string rawPath,
        string minecraftRoot)
    {
        PathSafety.ValidateWindowsRelativePath(rawPath);

        string basePath = PathSafety.RequireDirectoryPath(
            Path.Combine(minecraftRoot, "libraries"));

        string target = PathSafety.Resolve(Path.Combine(
            basePath,
            rawPath.Replace('\\', Path.DirectorySeparatorChar)
                .Replace('/', Path.DirectorySeparatorChar)));

        if (!PathSafety.IsWithin(target, basePath))
            throw new InvalidDataException(
                "依赖文件路径越出了公共 libraries 目录。");

        if (Directory.Exists(target))
            throw new IOException(
                $"依赖文件路径被目录占用：{target}");

        return target;
    }

    internal static string? GetWindowsNativeLibraryPath(
        JsonObject library,
        string minecraftRoot,
        JavaInfo java)
    {
        ArgumentNullException.ThrowIfNull(library);
        ArgumentNullException.ThrowIfNull(java);
        ArgumentException.ThrowIfNullOrWhiteSpace(minecraftRoot);

        if (!IsLibraryAllowed(library, "windows"))
            return null;

        string? classifier = ReadNativeClassifier(library, "windows");

        if (classifier is null)
            return null;

        if (java.Architecture is not ("x86" or "x64"))
            throw new NotSupportedException(
                "旧式 Windows 原生库定位目前只支持 x86 或 x64 Java。");

        classifier = ExpandNativeClassifier(classifier, java);

        if (library["downloads"] is not JsonObject downloads)
            throw new InvalidDataException(
                "原生库缺少有效的 downloads 对象。");

        if (downloads["classifiers"] is not JsonObject classifiers)
            throw new InvalidDataException(
                "原生库缺少有效的 classifiers 对象。");

        if (classifiers[classifier] is not JsonObject nativeFile)
            throw new InvalidDataException(
                $"缺少原生库分类下载信息：{classifier}");

        string rawPath = MinecraftVersionReader.ReadRequiredString(
            nativeFile,
            "path",
            "原生库下载信息缺少有效的 path。");

        return ResolveLibraryPath(rawPath, minecraftRoot);
    }


    internal static IReadOnlyList<string> GetWindowsNativeLibraryPaths(
        MinecraftVersion version,
        string minecraftRoot,
        JavaInfo java)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(java);
        ArgumentException.ThrowIfNullOrWhiteSpace(minecraftRoot);

        List<string> paths = [];
        HashSet<string> seen = new(PathSafety.Comparer);

        foreach (JsonObject library in GetWindowsLibraryEntries(version, minecraftRoot))
        {
            string? path =
                GetWindowsNativeLibraryPath(library, minecraftRoot, java);

            if (path is null)
                continue;

            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "缺少原生库文件，请先补全游戏文件。",
                    path);

            if (seen.Add(path))
                paths.Add(path);
        }

        return paths;
    }

    // 取得完整继承链，并确认版本属于调用方指定的 Minecraft 根目录。
internal static IReadOnlyList<MinecraftVersion> GetValidatedChain(
    MinecraftVersion version,
    string minecraftRoot)
{
    ArgumentNullException.ThrowIfNull(version);
    ArgumentException.ThrowIfNullOrWhiteSpace(minecraftRoot);

    _ = RequireVersionDirectory(version, minecraftRoot);

    MinecraftVersionReader reader = new();

    IReadOnlyList<MinecraftVersion> versions =
        reader.GetVersions(minecraftRoot);

    // 现有方法已经检查缺失父版本、重名父版本和循环继承。
    return reader.GetInheritanceChain(version, versions);
}

// 版本目录必须直接位于这个 Minecraft 根目录的 versions 下。
private static string RequireVersionDirectory(
    MinecraftVersion version,
    string minecraftRoot)
{
    string versionsDirectory = PathSafety.RequireDirectoryPath(
        Path.Combine(minecraftRoot, "versions"));

    string directory =
        PathSafety.RequireDirectoryPath(version.Directory);

    string? parent = Path.GetDirectoryName(directory);

    if (parent is null
        || !PathSafety.Comparer.Equals(parent, versionsDirectory))
        throw new InvalidDataException(
            $"版本目录不属于指定根目录的 versions：{version.Directory}");

    if (!Directory.Exists(directory))
        throw new DirectoryNotFoundException(
            $"版本目录不存在：{directory}");

    return directory;
}

// 依赖身份不包含版本号，但保留分类和扩展名。
    internal static string GetLibraryIdentity(string name)
{
    // 复用已有坐标校验，避免再写一套规则。
    _ = MavenToPath(name);

    int at = name.IndexOf('@');

    string coordinate = at < 0 ? name : name[..at];
    string extension = at < 0 ? "jar" : name[(at + 1)..];

    string[] parts = coordinate.Split(':');
    string classifier = parts.Length == 4 ? parts[3] : "";

    return $"{parts[0]}:{parts[1]}:{classifier}@{extension}";
}

// 当前版本优先，逐层补充没有被覆盖的父版本依赖。
internal static IReadOnlyList<JsonObject> GetWindowsLibraryEntries(
    MinecraftVersion version,
    string minecraftRoot)
{
    IReadOnlyList<MinecraftVersion> chain =
        GetValidatedChain(version, minecraftRoot);

    List<JsonObject> result = [];

    Dictionary<string, (int Level, JsonObject Library)> selected =
        new(StringComparer.Ordinal);

    for (int level = 0; level < chain.Count; level++)
    {
        foreach (JsonObject library in ReadLibraryEntries(chain[level]))
        {
            if (!IsLibraryAllowed(library, "windows"))
                continue;

            string name = ReadName(library);
            string identity = GetLibraryIdentity(name);

            if (selected.TryGetValue(identity, out var previous))
            {
                // 更靠近当前版本的声明已经选中，父版本不能覆盖它。
                if (previous.Level < level)
                    continue;

                // 同一层只允许完全相同的重复声明。
                if (!JsonNode.DeepEquals(previous.Library, library))
                    throw new InvalidDataException(
                        $"版本 {chain[level].Id} 存在冲突的依赖声明：{name}");

                continue;
            }

            selected.Add(identity, (level, library));
            result.Add(library);
        }
    }

    return result;
}

// 根据版本元数据确定客户端 JAR 的来源。
internal static string GetClientJarPath(
    MinecraftVersion version,
    string minecraftRoot)
{
    IReadOnlyList<MinecraftVersion> chain =
        GetValidatedChain(version, minecraftRoot);

    MinecraftVersionReader reader = new();

    foreach (MinecraftVersion current in chain)
    {
        JsonObject data =
            MinecraftVersionReader.ReadVersionJson(current.Directory);

        // 显式 jar 字段表示使用这个版本 ID 对应的客户端文件。
        // 通过版本查询定位目录，不直接把 ID 拼成文件路径。
        if (data.ContainsKey("jar"))
        {
            string jarVersionId =
                MinecraftVersionReader.ReadRequiredString(
                    data,
                    "jar",
                    "版本 jar 字段必须是非空字符串。");

            MinecraftVersion owner =
                reader.GetVersion(jarVersionId, minecraftRoot);

            return RequireClientJar(owner, minecraftRoot);
        }

        if (data.TryGetPropertyValue(
                "downloads", out JsonNode? downloadsNode))
        {
            if (downloadsNode is not JsonObject downloads)
                throw new InvalidDataException(
                    $"版本 {current.Id} 的 downloads 必须是对象。");

            if (downloads.ContainsKey("client"))
            {
                if (downloads["client"] is not JsonObject)
                    throw new InvalidDataException(
                        $"版本 {current.Id} 的 downloads.client 必须是对象。");

                return RequireClientJar(current, minecraftRoot);
            }
        }

        // 没有继续继承的基础版本，使用自己的客户端 JAR。
        // 这也允许没有 downloads 元数据的旧式基础版本。
        if (current.InheritsFrom is null)
            return RequireClientJar(current, minecraftRoot);
    }

    throw new InvalidDataException(
        $"无法确定版本 {version.Id} 的客户端 JAR。");
}

private static string RequireClientJar(
    MinecraftVersion owner,
    string minecraftRoot)
{
    string directory =
        RequireVersionDirectory(owner, minecraftRoot);

    // 项目已经允许版本 ID 与目录名不同。
    // 文件名按实际版本目录名确定。
    string folderName = Path.GetFileName(directory);
    string fileName = folderName + ".jar";

    PathSafety.ValidateWindowsRelativePath(fileName);

    string target = PathSafety.Resolve(
        Path.Combine(directory, fileName));

    if (!PathSafety.IsWithin(target, directory))
        throw new InvalidDataException(
            "客户端 JAR 路径越出了所属版本目录。");

    return RequireClassPathFile(target);
}

// 所有加入 classpath 的文件，都经过同一处存在性检查。
private static string RequireClassPathFile(string path)
{
    // Windows 文件名允许分号，但 classpath 会把它当成分隔符。
    if (path.Contains(';'))
        throw new NotSupportedException(
            $"classpath 文件路径不能包含分号：{path}");

    if (Directory.Exists(path))
        throw new IOException(
            $"classpath 文件路径被目录占用：{path}");

    if (!File.Exists(path))
        throw new FileNotFoundException(
            "缺少启动所需的文件，请先补全游戏文件。",
            path);

    return path;
}

internal static string GetWindowsClassPath(
    MinecraftVersion version,
    string minecraftRoot)
{
    if (!OperatingSystem.IsWindows())
        throw new PlatformNotSupportedException(
            "当前 classpath 构造只支持 Windows。");

    List<string> paths = [];
    HashSet<string> seen = new(PathSafety.Comparer);

    foreach (JsonObject library
             in GetWindowsLibraryEntries(version, minecraftRoot))
    {
        string? path = GetLibraryPath(library, minecraftRoot);

        // 只有原生库分类文件、没有普通 artifact 的声明不加入这里。
        if (path is null)
            continue;

        path = RequireClassPathFile(path);

        if (seen.Add(path))
            paths.Add(path);
    }

    string clientJar = GetClientJarPath(version, minecraftRoot);

    if (seen.Add(clientJar))
        paths.Add(clientJar);

    return string.Join(";", paths);
}

    private static string ReadName(JsonObject library)
        => MinecraftVersionReader.ReadRequiredString(library, "name", "依赖库缺少有效 name。");
}
