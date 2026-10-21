using System.Text.Json.Nodes;

namespace FMCore.Versions;

internal static class VersionInheritance
{
    internal static VersionMetadata Merge(VersionMetadata parent, VersionMetadata child)
    {
        var result = MergeObjects(parent.Document, child.Document);
        if (child.Document["libraries"] is JsonArray childLibraries)
        {
            var overrides = child.Libraries.Select(l => l.Coordinate.ConflictKey).ToHashSet(StringComparer.Ordinal);
            var libraries = childLibraries.Select(n => n!.DeepClone()).ToList();
            if (parent.Document["libraries"] is JsonArray parentLibraries)
                libraries.AddRange(parentLibraries.Where(n => !overrides.Contains(MavenCoordinate.Parse(n!["name"]!.GetValue<string>()).ConflictKey)).Select(n => n!.DeepClone()));
            result["libraries"] = new JsonArray(libraries.ToArray());
        }
        if (child.Document["arguments"] is JsonObject arguments)
        {
            var merged = result["arguments"] as JsonObject ?? new JsonObject();
            foreach (var (kind, values) in arguments)
            {
                if (kind is not "jvm" and not "game") continue;
                var prefix = parent.Document["arguments"]?[kind] is JsonArray existing ? existing.Select(n => n!.DeepClone()).ToList() : [];
                if (kind == "game" && parent.Document["arguments"]?["game"] == null)
                    prefix.AddRange(parent.Arguments.Game.SelectMany(a => a.Values).Select(v => (JsonNode)JsonValue.Create(v)!));
                prefix.AddRange(values!.AsArray().Select(n => n!.DeepClone()));
                merged[kind] = new JsonArray(prefix.ToArray());
            }
            if (result["arguments"] == null) result["arguments"] = merged;
        }
        if (child.LegacyGameArguments != null && child.Document["arguments"]?["game"] == null && result["arguments"] is JsonObject modern)
            modern.Remove("game");
        return VersionParser.Parse(result.ToJsonString());
    }

    private static JsonObject MergeObjects(JsonObject parent, JsonObject child, string path = "")
    {
        var result = (JsonObject)parent.DeepClone();
        foreach (var (key, value) in child)
        {
            // A new download URL must never inherit the hash/size of the old file.
            var replace = path.Length == 0 && key is "assetIndex" or "javaVersion" ||
                path == "downloads" || path == "logging.client" && key == "file";
            result[key] = !replace && value is JsonObject childObject && parent[key] is JsonObject parentObject
                ? MergeObjects(parentObject, childObject, path.Length == 0 ? key : path + "." + key) : value?.DeepClone();
        }
        return result;
    }
}
