using System.Text.Json.Nodes;
using FMCore.Installer;
using FMCore.Java;
using FMCore.Versions;

namespace FMCore.Launch;

internal static class ModernLibraryResolver
{
    internal static IReadOnlyList<JsonObject> ReadLibrariesForArchitecture(
        MinecraftVersion version,
        string minecraftRoot,
        string? targetArchitecture,
        IReadOnlyDictionary<string,bool>?features=null)
    {


        IReadOnlyList<MinecraftVersion> chain =
            LibraryResolver.GetValidatedChain(
                version, minecraftRoot);

        List<JsonObject> result = [];

        HashSet<string> selected =
            new(StringComparer.Ordinal);

        // 子版本仍然优先于父版本。
        foreach (MinecraftVersion current in chain)
        {
            Dictionary<string, JsonObject> level =
                new(StringComparer.Ordinal);

            List<string> order = [];

            foreach (JsonObject library
                     in LibraryResolver.ReadLibraryEntries(current))
            {
                if (!LaunchRules.IsAllowedForArchitecture(
                        library,
                        targetArchitecture,
                        features))
                {
                    continue;
                }

                string name = GetName(library);

                if (!MatchesNativeArchitecture(
                        name,
                        targetArchitecture))
                {
                    continue;
                }

                string identity =
                    LibraryResolver.GetLibraryIdentity(name);

                if (selected.Contains(identity))
                    continue;

                if (level.TryGetValue(
                        identity, out JsonObject? previous))
                {
                    // 同一个版本号可能分开补充 artifact 和 natives。
                    // 不同版本号则使用后面的覆盖声明。
                    level[identity] = GetName(previous) == name
                        ? MergeObjects(previous, library)
                        : library;
                }
                else
                {
                    level.Add(identity, library);
                    order.Add(identity);
                }
            }

            foreach (string identity in order)
            {
                selected.Add(identity);
                result.Add(level[identity]);
            }
        }

        return result;
    }

    internal static string GetClassPath(
        MinecraftVersion version,
        string minecraftRoot,
        JavaInfo java,
        IReadOnlyDictionary<string, bool>? features = null)
    {
        List<string> paths = [];
        HashSet<string> seen = new(PathSafety.Comparer);

        foreach (JsonObject library
                 in ReadLibraries(version, minecraftRoot, java,features))
        {
            string? path =
                LibraryResolver.GetLibraryPath(
                    library, minecraftRoot);

            if (path is null)
                continue;

            if (path.Contains(';'))
                throw new NotSupportedException(
                    $"classpath 文件路径不能包含分号：{path}");

            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "缺少启动依赖，请先补全游戏文件。",
                    path);

            if (seen.Add(path))
                paths.Add(path);
        }

        string clientJar =
            LibraryResolver.GetClientJarPath(
                version, minecraftRoot);

        if (seen.Add(clientJar))
            paths.Add(clientJar);

        return string.Join(";", paths);
    }

    private static bool MatchesNativeArchitecture(
        string name,
        string? targetArchitecture)
    {
        string coordinate = name.Split('@')[0];
        string[] parts = coordinate.Split(':');

        if (parts.Length != 4
            || !parts[3].StartsWith(
                "natives-", StringComparison.Ordinal))
            return true;

        string classifier = parts[3];

        if (!classifier.StartsWith(
                "natives-windows", StringComparison.Ordinal))
            return false;

        if (classifier is not (
                "natives-windows"
                or "natives-windows-x86"
                or "natives-windows-arm64"))
            throw new NotSupportedException(
                $"暂未支持原生库分类：{classifier}");

        string expected = targetArchitecture switch
        {
            "x64" => "natives-windows",
            "x86" => "natives-windows-x86",
            "arm64" => "natives-windows-arm64",
            _ => throw new NotSupportedException(
                "无法根据未知 Java 架构选择原生库。")
        };

        return classifier == expected;
    }

    private static JsonObject MergeObjects(
        JsonObject first,
        JsonObject second)
    {
        JsonObject merged = first.DeepClone().AsObject();

        foreach (var pair in second)
        {
            if (merged[pair.Key] is JsonObject previous
                && pair.Value is JsonObject next)
            {
                merged[pair.Key] = MergeObjects(previous, next);
            }
            else
            {
                merged[pair.Key] = pair.Value?.DeepClone();
            }
        }

        return merged;
    }

    private static string GetName(JsonObject library)
        => MinecraftVersionReader.ReadRequiredString(
            library, "name", "依赖缺少有效的 name。");

    internal static IReadOnlyList<JsonObject> ReadLibraries(
        MinecraftVersion version,
        string minecraftRoot,
        JavaInfo java,
        IReadOnlyDictionary<string, bool>? features = null)
    {
        ArgumentNullException.ThrowIfNull(java);

        return ReadLibrariesForArchitecture(
            version,
            minecraftRoot,
            java.Architecture,
            features);
    }
}
