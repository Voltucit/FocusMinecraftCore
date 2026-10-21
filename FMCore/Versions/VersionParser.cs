using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FMCore.Downloader;

namespace FMCore.Versions;

public static class VersionParser
{
    public static VersionManifest ParseManifest(string json) => Read(() =>
    {
        var document = Root(json);
        var latest = Object(document["latest"]);
        var release = Id(Text(latest, "release")!);
        var snapshot = Id(Text(latest, "snapshot")!);
        var entries = Array(document["versions"]).Select(value =>
        {
            var item = Object(value);
            DateTimeOffset? released = Text(item, "releaseTime", false) is string time
                ? DateTimeOffset.Parse(time, System.Globalization.CultureInfo.InvariantCulture) : null;
            return new VersionEntry(Id(Text(item, "id")!), Text(item, "type")!, Url(Text(item, "url")!), Hash(Text(item, "sha1")!)!, released);
        }).ToArray();
        if (entries.Select(e => e.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != entries.Length ||
            !entries.Any(e => e.Id == release) || !entries.Any(e => e.Id == snapshot)) throw Invalid();
        return new VersionManifest(release, snapshot, System.Array.AsReadOnly(entries));
    });

    public static VersionMetadata Parse(string json) => Read(() => ParseObject(Root(json)));

    internal static VersionMetadata ParseObject(JsonObject document)
    {
        var id = Id(Text(document, "id")!);
        var parent = OptionalId(document, "inheritsFrom");
        var jar = OptionalId(document, "jar");
        var downloads = new Dictionary<string, DownloadArtifact>(StringComparer.Ordinal);
        if (document["downloads"] != null)
            foreach (var (name, value) in Object(document["downloads"])) downloads.Add(name, Artifact(Object(value)));
        var libraries = document["libraries"] == null ? [] : Array(document["libraries"]).Select(v => Library(Object(v))).ToArray();
        var arguments = document["arguments"] == null ? null : Object(document["arguments"]);
        var legacy = Text(document, "minecraftArguments", false);
        var game = arguments?["game"] != null ? Arguments(Array(arguments["game"]))
            : legacy == null ? [] : LegacyTokens(legacy).Select(t => new ArgumentValue(System.Array.AsReadOnly(new[] { t }), null)).ToArray();
        var jvm = arguments?["jvm"] == null ? [] : Arguments(Array(arguments["jvm"]));
        JavaRequirement? java = null;
        if (document["javaVersion"] is JsonNode javaNode)
        {
            var info = Object(javaNode);
            var major = info["majorVersion"]!.GetValue<int>();
            if (major < 1 || major > 999) throw Invalid();
            java = new(Text(info, "component", false), major);
        }
        AssetIndexInfo? assets = null;
        if (document["assetIndex"] is JsonNode assetNode)
        {
            var info = Object(assetNode);
            assets = new(Id(Text(info, "id")!), Artifact(info), Size(info, "totalSize"));
        }
        LoggingConfiguration? logging = null;
        if (document["logging"] != null && Object(document["logging"])["client"] is JsonNode loggingNode)
        {
            var info = Object(loggingNode);
            var file = Object(info["file"]);
            logging = new(Text(info, "argument")!, Text(info, "type", false), Id(Text(file, "id")!), Artifact(file));
        }
        return new(document, id, parent, jar, Text(document, "type", false), Text(document, "mainClass", false),
            OptionalId(document, "assets"), legacy, java, assets, logging,
            new ReadOnlyDictionary<string, DownloadArtifact>(downloads), System.Array.AsReadOnly(libraries),
            new(System.Array.AsReadOnly(jvm), System.Array.AsReadOnly(game), arguments?["jvm"] != null));
    }

    private static LibraryMetadata Library(JsonObject value)
    {
        var name = Text(value, "name")!;
        var coordinate = MavenCoordinate.Parse(name);
        DownloadArtifact? artifact;
        var classifiers = new Dictionary<string, DownloadArtifact>(StringComparer.Ordinal);
        if (value["downloads"] != null)
        {
            var downloads = Object(value["downloads"]);
            artifact = downloads["artifact"] == null ? null : Artifact(Object(downloads["artifact"]), coordinate.RelativePath);
            if (downloads["classifiers"] != null)
                foreach (var (key, info) in Object(downloads["classifiers"]))
                {
                    Id(key);
                    classifiers.Add(key, Artifact(Object(info), (coordinate with { Classifier = key }).RelativePath));
                }
        }
        else
        {
            var repository = Url(Text(value, "url", false) ?? "https://libraries.minecraft.net/");
            var url = new Uri(repository.AbsoluteUri.TrimEnd('/') + "/" + coordinate.RelativePath);
            artifact = new(url, Hash(Text(value, "sha1", false)), Size(value, "size"), coordinate.RelativePath);
        }
        var natives = new Dictionary<string, string>(StringComparer.Ordinal);
        if (value["natives"] != null)
            foreach (var (key, info) in Object(value["natives"]))
            {
                var classifier = info!.GetValue<string>();
                Id(classifier.Replace("${arch}", "64", StringComparison.Ordinal));
                natives.Add(key, classifier);
            }
        var excludes = value["extract"] == null || Object(value["extract"])["exclude"] == null ? []
            : Array(Object(value["extract"])["exclude"]).Select(n =>
            {
                var exclude = n!.GetValue<string>();
                Relative(exclude.TrimEnd('/', '\\'));
                return exclude;
            }).ToArray();
        return new(name, coordinate, artifact, new ReadOnlyDictionary<string, DownloadArtifact>(classifiers),
            new ReadOnlyDictionary<string, string>(natives), System.Array.AsReadOnly(excludes), Rules(value));
    }

    private static ArgumentValue[] Arguments(JsonArray array) => array.Select(value =>
    {
        if (value is JsonValue text) return new ArgumentValue(System.Array.AsReadOnly(new[] { text.GetValue<string>() }), null);
        var item = Object(value);
        var values = item["value"] is JsonArray multiple ? multiple.Select(v => v!.GetValue<string>()).ToArray() : new[] { Text(item, "value")! };
        return new ArgumentValue(System.Array.AsReadOnly(values), Rules(item));
    }).ToArray();

    private static IReadOnlyList<VersionRule>? Rules(JsonObject value)
    {
        if (value["rules"] == null) return null;
        var rules = Array(value["rules"]).Select(v =>
        {
            var rule = Object(v);
            var action = Text(rule, "action");
            if (action is not "allow" and not "disallow") throw Invalid();
            var os = rule["os"] == null ? null : Object(rule["os"]);
            var features = new Dictionary<string, bool>(StringComparer.Ordinal);
            if (rule["features"] != null)
                foreach (var (key, flag) in Object(rule["features"])) features.Add(key, flag!.GetValue<bool>());
            var arch = os == null ? null : Text(os, "arch", false);
            var version = os == null ? null : Text(os, "version", false);
            foreach (var pattern in new[] { arch, version }.OfType<string>())
                _ = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
            return new VersionRule(action == "allow", os == null ? null : Text(os, "name", false), arch, version,
                new ReadOnlyDictionary<string, bool>(features));
        }).ToArray();
        return System.Array.AsReadOnly(rules);
    }

    private static DownloadArtifact Artifact(JsonObject value, string? fallbackPath = null) => new(Url(Text(value, "url")!),
        Hash(Text(value, "sha1", false)), Size(value, "size"), Text(value, "path", false) is string path ? Relative(path) : fallbackPath);

    internal static string Id(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 180 || id.IndexOfAny(['/', '\\']) >= 0)
            throw new VersionException(VersionErrorCode.UnsafePath, "版本或文件标识符不安全。");
        return Relative(id);
    }

    private static string Relative(string path)
    {
        try { DownloadPath.ValidateRelative(path); return path; }
        catch (DownloadException) { throw new VersionException(VersionErrorCode.UnsafePath, "元数据包含不安全的文件路径。"); }
    }
    private static string? OptionalId(JsonObject obj, string key) => Text(obj, key, false) is string id ? Id(id) : null;
    private static JsonObject Root(string json) => Object(JsonNode.Parse(json, documentOptions: new JsonDocumentOptions { MaxDepth = 64 }));
    private static JsonObject Object(JsonNode? node) => node as JsonObject ?? throw Invalid();
    private static JsonArray Array(JsonNode? node) => node as JsonArray ?? throw Invalid();
    private static string? Text(JsonObject obj, string key, bool required = true) => obj[key] == null
        ? required ? throw Invalid() : null : obj[key]!.GetValue<string>();
    private static long? Size(JsonObject obj, string key)
    {
        var size = obj[key]?.GetValue<long>();
        return size is < 0 ? throw Invalid() : size;
    }
    private static Uri Url(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.Scheme is "https" or "http" && uri.UserInfo.Length == 0 ? uri : throw Invalid();
    private static string? Hash(string? hash) => hash == null || hash.Length == 40 && hash.All(char.IsAsciiHexDigit) ? hash : throw Invalid();
    private static VersionException Invalid() => new(VersionErrorCode.InvalidMetadata, "版本 JSON 缺少必要字段或字段格式无效。");
    private static T Read<T>(Func<T> parse)
    {
        try { return parse(); }
        catch (Exception e) when (e is JsonException or InvalidOperationException or FormatException or ArgumentException or OverflowException or NullReferenceException)
        { throw Invalid(); }
    }

    // Legacy arguments are tokens, never a shell command. Keep variables unexpanded for Launch.
    private static IReadOnlyList<string> LegacyTokens(string text)
    {
        var tokens = new List<string>();
        var token = new StringBuilder();
        char? quote = null;
        var started = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\\' && i + 1 < text.Length && (text[i + 1] == quote || text[i + 1] == '\\')) { token.Append(text[++i]); started = true; }
            else if (quote != null) { if (c == quote) quote = null; else token.Append(c); }
            else if (c is '\'' or '"') { quote = c; started = true; }
            else if (char.IsWhiteSpace(c)) { if (started) { tokens.Add(token.ToString()); token.Clear(); started = false; } }
            else { token.Append(c); started = true; }
        }
        if (quote != null) throw Invalid();
        if (started) tokens.Add(token.ToString());
        return tokens.AsReadOnly();
    }
}
