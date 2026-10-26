using System.Text.Json.Nodes;
using FMCore.Installer;

namespace FMCore.Versions;

/// <summary>只读查询本地 Minecraft 版本，不创建或迁移游戏目录。</summary>
public sealed class MinecraftVersionReader
{
    // 保留最初 C# 学习步骤中的入口，增加可选根目录。
    public string[] GetVersionDirectories(string? minecraftRoot = null)
    {
        List<string> directories = [];
        foreach (string root in FindMinecraftRoots(minecraftRoot))
        {
            string versionsPath = Path.Combine(root, "versions");
            if (File.Exists(versionsPath))
                throw new IOException($"versions 路径被文件占用：{versionsPath}");
            if (!System.IO.Directory.Exists(versionsPath))
                continue;
            foreach (string directory in System.IO.Directory.EnumerateDirectories(versionsPath).Order(PathSafety.Comparer))
            {
                string name = Path.GetFileName(directory);
                if (File.Exists(Path.Combine(directory, name + ".json")))
                    directories.Add(directory);
            }
        }
        return directories.ToArray();
    }

    public IReadOnlyList<MinecraftVersion> GetVersions(string? minecraftRoot = null)
        => GetVersionDirectories(minecraftRoot)
            .Select(directory => ParseVersion(directory, ReadVersionJson(directory))).ToArray();

    public async Task<IReadOnlyList<MinecraftVersion>> GetVersionsAsync(
        string? minecraftRoot = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        List<MinecraftVersion> versions = [];
        foreach (string directory in GetVersionDirectories(minecraftRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string jsonPath = GetJsonPath(directory);
            string text = await File.ReadAllTextAsync(jsonPath, cancellationToken).ConfigureAwait(false);
            versions.Add(ParseVersion(directory, ParseJson(text, jsonPath)));
        }
        return versions;
    }

    public MinecraftVersion GetVersion(string versionId, string? minecraftRoot = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versionId);
        return FindUnique(versionId, GetVersions(minecraftRoot));
    }

    public async Task<MinecraftVersion> GetVersionAsync(string versionId, string? minecraftRoot = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versionId);
        IReadOnlyList<MinecraftVersion> versions = await GetVersionsAsync(minecraftRoot, cancellationToken)
            .ConfigureAwait(false);
        return FindUnique(versionId, versions);
    }

    public IReadOnlyList<MinecraftVersion> GetInheritanceChain(MinecraftVersion version,
        IReadOnlyList<MinecraftVersion> versions)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(versions);
        List<MinecraftVersion> chain = [];
        HashSet<string> visitedIds = new(StringComparer.Ordinal);
        MinecraftVersion? current = version;
        while (current is not null)
        {
            if (!visitedIds.Add(current.Id))
                throw new InvalidDataException($"版本继承存在循环：再次遇到 {current.Id}");
            chain.Add(current);
            current = GetParentVersion(current, versions);
        }
        return chain;
    }

    public string ResolveMainClass(MinecraftVersion version, IReadOnlyList<MinecraftVersion> versions)
    {
        // 先验证整条链，不能因子版本已有入口而掩盖循环或缺失父版本。
        IReadOnlyList<MinecraftVersion> chain = GetInheritanceChain(version, versions);
        return chain.Select(item => item.MainClass).FirstOrDefault(item => item is not null)
            ?? throw new InvalidDataException($"版本 {version.Id} 及其父版本均未声明启动入口 mainClass");
    }

    private static MinecraftVersion FindUnique(string id, IReadOnlyList<MinecraftVersion> versions)
    {
        MinecraftVersion[] matches = versions.Where(item => item.Id == id).ToArray();
        return matches.Length switch
        {
            0 => throw new KeyNotFoundException($"未找到版本：{id}"),
            1 => matches[0],
            _ => throw new InvalidDataException($"版本 ID 存在重名冲突：{id}，请指定 Minecraft 根目录。")
        };
    }

    private static MinecraftVersion? GetParentVersion(MinecraftVersion version,
        IReadOnlyList<MinecraftVersion> versions)
    {
        if (version.InheritsFrom is null)
            return null;
        string root = GetMinecraftRoot(version.Directory);
        MinecraftVersion[] matches = versions.Where(candidate => candidate.Id == version.InheritsFrom
            && PathSafety.Comparer.Equals(GetMinecraftRoot(candidate.Directory), root)).ToArray();
        return matches.Length switch
        {
            0 => throw new KeyNotFoundException($"版本 {version.Id} 缺少父版本：{version.InheritsFrom}"),
            1 => matches[0],
            _ => throw new InvalidDataException($"父版本 ID 存在重名冲突：{version.InheritsFrom}")
        };
    }

    private static string GetMinecraftRoot(string versionDirectory)
    {
        string full = Path.GetFullPath(versionDirectory);
        string? versions = Path.GetDirectoryName(full);
        string? root = versions is null ? null : Path.GetDirectoryName(versions);
        return root is null ? throw new InvalidDataException($"无法确定版本所属根目录：{versionDirectory}")
            : PathSafety.Resolve(root);
    }

    internal static IReadOnlyList<string> FindMinecraftRoots(string? minecraftRoot)
    {
        if (minecraftRoot is not null)
            return [PathSafety.RequireDirectoryPath(minecraftRoot)];
        List<string> candidates = [Path.Combine(Environment.CurrentDirectory, ".minecraft")];
        string? appdata = Environment.GetEnvironmentVariable("APPDATA");
        if (!string.IsNullOrWhiteSpace(appdata))
            candidates.Add(Path.Combine(appdata, ".minecraft"));
        return candidates.Where(System.IO.Directory.Exists).Select(PathSafety.Resolve)
            .Distinct(PathSafety.Comparer).ToArray();
    }

    internal static JsonObject ReadVersionJson(string directory)
    {
        string path = GetJsonPath(directory);
        return ParseJson(File.ReadAllText(path), path);
    }

    private static string GetJsonPath(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        string full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));
        return Path.Combine(full, Path.GetFileName(full) + ".json");
    }

    private static JsonObject ParseJson(string text, string path)
    {
        JsonObject data = JsonNode.Parse(text) as JsonObject
            ?? throw new InvalidDataException($"版本 JSON 的最外层必须是对象：{path}");
        _ = ReadRequiredString(data, "id", $"版本 JSON 缺少有效的 id：{path}");
        return data;
    }

    private static MinecraftVersion ParseVersion(string directory, JsonObject data)
        => new(ReadRequiredString(data, "id", $"版本 JSON 缺少有效的 id：{directory}"),
            ReadOptionalString(data, "type", allowEmpty: true), directory,
            ReadOptionalString(data, "inheritsFrom"), ReadOptionalString(data, "mainClass"));

    internal static string ReadRequiredString(JsonObject data, string key, string message)
        => data[key] is JsonValue value && value.TryGetValue(out string? result)
            && !string.IsNullOrWhiteSpace(result) ? result : throw new InvalidDataException(message);

    private static string? ReadOptionalString(JsonObject data, string key, bool allowEmpty = false)
    {
        if (data[key] is null)
            return null;
        if (data[key] is JsonValue value && value.TryGetValue(out string? result)
            && result is not null && (allowEmpty || !string.IsNullOrWhiteSpace(result)))
            return result;
        throw new InvalidDataException($"版本字段 {key} 必须是{(allowEmpty ? "" : "非空")}字符串。");
    }
}
