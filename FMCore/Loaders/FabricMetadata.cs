using System.Text.Json;
using System.Text.Json.Nodes;
using FMCore.Versions;

namespace FMCore.Loaders;

internal static class FabricMetadata
{
    internal sealed record LauncherDetail(string MainClass, int MinimumJava);
    internal static IReadOnlyList<FabricGameVersion> Games(string json)
    {
        var array = JsonNode.Parse(json) as JsonArray ?? throw Invalid();
        var result = array.Select(n => new FabricGameVersion(VersionParser.Id(Text(n, "version")), Bool(n, "stable"))).ToArray();
        if (result.Select(v => v.Version).Distinct(StringComparer.OrdinalIgnoreCase).Count() != result.Length) throw Invalid();
        return Array.AsReadOnly(result);
    }
    internal static IReadOnlyList<FabricLoaderVersion> Loaders(string json)
    {
        var array = JsonNode.Parse(json) as JsonArray ?? throw Invalid();
        var result = array.Select(n => new FabricLoaderVersion(VersionParser.Id(Text(n?["loader"], "version")),
            Bool(n?["loader"], "stable"), VersionParser.Id(Text(n?["intermediary"], "version")))).ToArray();
        if (result.Select(v => v.Version).Distinct(StringComparer.OrdinalIgnoreCase).Count() != result.Length) throw Invalid();
        return Array.AsReadOnly(result);
    }
    internal static LauncherDetail Detail(string json, string game, string loader)
    {
        var node = JsonNode.Parse(json) as JsonObject ?? throw Invalid();
        if (Text(node["loader"], "version") != loader || Text(node["intermediary"], "version") != game ||
            Text(node["loader"], "maven") != "net.fabricmc:fabric-loader:" + loader ||
            Text(node["intermediary"], "maven") != "net.fabricmc:intermediary:" + game)
            throw new FabricException(FabricErrorCode.UnsupportedCombination, "Fabric 响应不匹配请求的 Minecraft/Loader 组合。");
        var meta = node["launcherMeta"] as JsonObject ?? throw Unsupported();
        var format = meta["version"]?.GetValue<int>() ?? 0;
        if (format is not (1 or 2)) throw Unsupported();
        var main = Text(meta["mainClass"], "client");
        if (main is not ("net.fabricmc.loader.impl.launch.knot.KnotClient" or "net.fabricmc.loader.launch.knot.KnotClient")) throw Unsupported();
        var java = meta["min_java_version"]?.GetValue<int>() ?? (format == 1 ? 8 : 0);
        if (java is < 8 or > 999) throw Invalid();
        return new(main, java);
    }
    internal static JsonObject Profile(string json, string game, string loader, string main)
    {
        var parsed = VersionParser.Parse(json);
        var node = parsed.ToJson();
        if (parsed.InheritsFrom != game || parsed.Id != $"fabric-loader-{loader}-{game}" || parsed.MainClass != main)
            throw new FabricException(FabricErrorCode.UnsupportedCombination, "Fabric 客户端配置与请求的组合不一致。");
        if (parsed.Jar != null || parsed.Downloads.Count != 0 || parsed.Java != null || parsed.AssetIndex != null || parsed.Assets != null ||
            parsed.Logging != null || node["processors"] != null || node["install"] != null || parsed.LegacyGameArguments != null ||
            node["arguments"] is not JsonObject || node["arguments"]?["game"] is not JsonArray || node["libraries"] is not JsonArray)
            throw Unsupported();
        var loaderLibraries = parsed.Libraries.Where(l => l.Coordinate.Group == "net.fabricmc" && l.Coordinate.Artifact == "fabric-loader").ToArray();
        var mappings = parsed.Libraries.Where(l => l.Coordinate.Group == "net.fabricmc" && l.Coordinate.Artifact == "intermediary").ToArray();
        if (loaderLibraries.Length != 1 || loaderLibraries[0].Coordinate.Version != loader || mappings.Length != 1 || mappings[0].Coordinate.Version != game)
            throw Invalid();
        if (parsed.Libraries.Select(l => l.Coordinate.ConflictKey).Distinct(StringComparer.Ordinal).Count() != parsed.Libraries.Count) throw Invalid();
        if (loaderLibraries[0].Rules?.Count > 0 || mappings[0].Rules?.Count > 0) throw Unsupported();
        foreach (var library in parsed.Libraries)
        {
            if (library.Artifact == null || library.Classifiers.Count != 0 || library.Natives.Count != 0 ||
                library.Coordinate.Classifier != null || library.Coordinate.Extension != "jar") throw Unsupported();
            // Preserve rules and arguments for the shared resolver. Reject alternate targets that could alias shared files.
            if (library.Artifact.Path != library.Coordinate.RelativePath) throw Invalid();
        }
        return node;
    }
    internal static string Sha1(string text)
    {
        var hash = text.Trim();
        if (hash.Length != 40 || !hash.All(char.IsAsciiHexDigit)) throw Invalid();
        return hash.ToLowerInvariant();
    }
    internal static bool Equivalent(JsonObject a, JsonObject b)
    {
        a = (JsonObject)a.DeepClone(); b = (JsonObject)b.DeepClone();
        // Fabric Meta regenerates timestamps on each request; they do not change installation behavior.
        foreach (var field in new[] { "time", "releaseTime" }) { a.Remove(field); b.Remove(field); }
        return JsonNode.DeepEquals(a, b);
    }
    private static string Text(JsonNode? node, string key) => node?[key]?.GetValue<string>() ?? throw Invalid();
    private static bool Bool(JsonNode? node, string key) => node?[key]?.GetValue<bool>() ?? throw Invalid();
    internal static FabricException Invalid() => new(FabricErrorCode.InvalidMetadata, "Fabric 元数据或 Maven SHA-1 格式无效。");
    private static FabricException Unsupported() => new(FabricErrorCode.UnsupportedFormat,
        "不支持此 Fabric 安装格式；仅支持 Fabric Meta V2 的 launcherMeta 1/2 和 Knot 客户端，不会跳过必要步骤。");
}
