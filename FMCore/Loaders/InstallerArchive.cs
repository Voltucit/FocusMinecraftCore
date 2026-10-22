using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FMCore.Downloader;
using FMCore.Utilities;
using FMCore.Versions;

namespace FMCore.Loaders;

internal sealed class InstallerArchive : IDisposable
{
    private readonly ZipArchive archive;
    internal JsonObject Install { get; }
    internal JsonObject Version { get; }
    internal InstallerArchive(string path, string game, string loader, string mavenVersion)
    {
        archive = ZipFile.OpenRead(path);
        try
        {
            ValidateEntries(archive);
            Install = ReadJson("install_profile.json");
            // Old versionInfo/universal-copy formats require a different installer API and are explicitly unsupported.
            if (Install["spec"]?.GetValue<int>() is not (0 or 1) || Install["json"]?.GetValue<string>() != "/version.json" ||
                Install["processors"] is not JsonArray || Install["libraries"] is not JsonArray) throw LoaderOperations.Unsupported();
            foreach (var node in Install["processors"]!.AsArray())
            {
                var p = node as JsonObject ?? throw LoaderOperations.Invalid();
                _ = MavenCoordinate.Parse(p["jar"]?.GetValue<string>() ?? throw LoaderOperations.Invalid());
                if (p["sides"] != null && (p["sides"] is not JsonArray sides || sides.Any(v => v?.GetValue<string>() is not ("client" or "server"))))
                    throw LoaderOperations.Unsupported();
                if (p["args"] is not JsonArray args || args.Any(a => a == null)) throw LoaderOperations.Unsupported();
            }
            if (Install["minecraft"]?.GetValue<string>() != game) throw new LoaderException(LoaderErrorCode.UnsupportedCombination, "安装器 Minecraft 版本不匹配。");
            Version = ReadJson("version.json");
            var metadata = VersionParser.ParseObject(Version);
            if (metadata.InheritsFrom != game || metadata.Id != Install["version"]?.GetValue<string>() || metadata.Downloads.Count != 0 || metadata.Jar != null)
                throw LoaderOperations.Unsupported();
            var group = loader == "Forge" ? "net.minecraftforge" : "net.neoforged";
            var artifact = loader == "Forge" ? "forge" : "neoforge";
            var installerMetadata = VersionParser.ParseObject(new JsonObject { ["id"] = "installer", ["libraries"] = Install["libraries"]!.DeepClone() });
            var identityLibraries = metadata.Libraries.Concat(installerMetadata.Libraries).ToArray();
            var pathCoordinate = Install["path"] is JsonValue explicitPath ? MavenCoordinate.Parse(explicitPath.GetValue<string>())
                : identityLibraries.FirstOrDefault(l => l.Coordinate.Group == group && l.Coordinate.Artifact == artifact)?.Coordinate
                    ?? throw LoaderOperations.Invalid();
            if (pathCoordinate.Group != group || pathCoordinate.Artifact != artifact || pathCoordinate.Version != mavenVersion ||
                !identityLibraries.Any(l => l.Coordinate.Group == group && l.Coordinate.Artifact == artifact && l.Coordinate.Version == mavenVersion))
                throw new LoaderException(LoaderErrorCode.UnsupportedCombination, "安装器 Maven 坐标与请求不匹配。");
            if (metadata.MainClass is not ("cpw.mods.modlauncher.Launcher" or "cpw.mods.bootstraplauncher.BootstrapLauncher")) throw LoaderOperations.Unsupported();
            _ = VersionParser.ParseObject(new JsonObject { ["id"] = "installer", ["libraries"] = Install["libraries"]!.DeepClone() });
        }
        catch { archive.Dispose(); throw; }
    }
    internal static void ValidateEntries(ZipArchive archive)
    {
        if (archive.Entries.Count > 50000) throw new LoaderException(LoaderErrorCode.InvalidArchive, "安装器归档条目过多。");
        long total = 0; var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            var name = entry.FullName.TrimEnd('/'); if (name.Length == 0) continue;
            DownloadPath.ValidateRelative(name);
            if (!seen.Add(name) || entry.Length > 512L * 1024 * 1024 || (total += entry.Length) > 2L * 1024 * 1024 * 1024 ||
                ((entry.ExternalAttributes >> 16) & 0xf000) == 0xa000 || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0)
                throw new LoaderException(LoaderErrorCode.InvalidArchive, "安装器归档包含冲突、链接或过大的条目。");
        }
    }
    private JsonObject ReadJson(string name)
    {
        var entry = archive.GetEntry(name) ?? throw LoaderOperations.Unsupported();
        if (entry.Length > 16 * 1024 * 1024) throw new LoaderException(LoaderErrorCode.InvalidArchive, "安装器元数据过大。");
        using var input = new StreamReader(entry.Open());
        return JsonNode.Parse(input.ReadToEnd()) as JsonObject ?? throw LoaderOperations.Invalid();
    }
    internal IReadOnlyDictionary<string, string> Outputs()
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in Install["processors"]!.AsArray().OfType<JsonObject>())
        {
            if (p["sides"] is JsonArray sides && !sides.Any(s => s?.GetValue<string>() == "client")) continue;
            _ = MavenCoordinate.Parse(p["jar"]!.GetValue<string>());
            foreach (var coordinate in p["classpath"]?.AsArray() ?? []) _ = MavenCoordinate.Parse(coordinate!.GetValue<string>());
            if (p["outputs"] is not JsonObject outputs) continue;
            foreach (var (key, value) in outputs)
            {
                var relative = Resolve(key);
                if (!relative.StartsWith("libraries/", StringComparison.Ordinal)) throw LoaderOperations.Unsupported();
                DownloadPath.ValidateRelative(relative);
                var hash = LoaderOperations.Sha1(Resolve(value!.GetValue<string>()));
                if (result.TryGetValue(relative, out var other) && other != hash) throw LoaderOperations.Invalid();
                result[relative] = hash;
            }
        }
        return result;
    }
    internal string Resolve(string value)
    {
        if (value.StartsWith('{') && value.EndsWith('}'))
        {
            var key = value[1..^1];
            value = Install["data"]?[key]?["client"]?.GetValue<string>() ?? throw LoaderOperations.Unsupported();
        }
        if (value.StartsWith('[') && value.EndsWith(']')) return "libraries/" + MavenCoordinate.Parse(value[1..^1]).RelativePath;
        if (value.StartsWith('\'') && value.EndsWith('\'')) return value[1..^1];
        return value;
    }
    internal IReadOnlyList<string> RequiredOutputs()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var processor in Install["processors"]!.AsArray().OfType<JsonObject>())
        {
            if (processor["sides"] is JsonArray sides && !sides.Any(s => s?.GetValue<string>() == "client")) continue;
            var args = processor["args"]?.AsArray().Select(n => n!.GetValue<string>()).ToArray() ?? throw LoaderOperations.Unsupported();
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] is "--output" or "--slim" or "--extra" or "--out-jar")
                {
                    var path = Resolve(args[i + 1]);
                    if (!path.StartsWith("libraries/", StringComparison.Ordinal)) throw LoaderOperations.Unsupported();
                    DownloadPath.ValidateRelative(path); result.Add(path);
                }
        }
        return result.Order(StringComparer.Ordinal).ToArray();
    }
    internal void ValidateClientData()
    {
        foreach (var item in Install["data"]?.AsObject() ?? [])
        {
            var value = item.Value?["client"]?.GetValue<string>() ?? throw LoaderOperations.Unsupported();
            if (value.StartsWith('[') && value.EndsWith(']')) _ = MavenCoordinate.Parse(value[1..^1]);
            else if (value.StartsWith('/')) DownloadPath.ValidateRelative(value[1..]);
            else if (!(value.StartsWith('\'') && value.EndsWith('\''))) throw LoaderOperations.Unsupported();
        }
    }
    public void Dispose() => archive.Dispose();
}
