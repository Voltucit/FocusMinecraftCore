using System.Text.Json.Nodes;

namespace FMCore.Versions;

public sealed record VersionEntry(string Id, string Type, Uri Url, string Sha1, DateTimeOffset? ReleaseTime)
{
    public override string ToString() => Id;
}

public sealed record VersionManifest(string LatestRelease, string LatestSnapshot, IReadOnlyList<VersionEntry> Versions);

public sealed record DownloadArtifact(Uri Url, string? Sha1, long? Size, string? Path)
{
    public override string ToString() => Path ?? "Metadata download artifact";
}

public sealed record AssetIndexInfo(string Id, DownloadArtifact Download, long? TotalSize);
public sealed record JavaRequirement(string? Component, int MajorVersion);
public sealed record LoggingConfiguration(string Argument, string? Type, string FileId, DownloadArtifact File);
public sealed record ArgumentValue(IReadOnlyList<string> Values, IReadOnlyList<VersionRule>? Rules);

public sealed record VersionArguments(IReadOnlyList<ArgumentValue> Jvm, IReadOnlyList<ArgumentValue> Game, bool HasJvmArguments)
{
    public IReadOnlyList<string> GetJvmArguments(RuleContext context) => Evaluate(Jvm, context);
    public IReadOnlyList<string> GetGameArguments(RuleContext context) => Evaluate(Game, context);
    private static IReadOnlyList<string> Evaluate(IReadOnlyList<ArgumentValue> values, RuleContext context) =>
        Array.AsReadOnly(values.Where(v => RuleEvaluator.IsAllowed(v.Rules, context)).SelectMany(v => v.Values).ToArray());
}

public sealed record LibraryMetadata(string Name, MavenCoordinate Coordinate, DownloadArtifact? Artifact,
    IReadOnlyDictionary<string, DownloadArtifact> Classifiers, IReadOnlyDictionary<string, string> Natives,
    IReadOnlyList<string> ExtractExcludes, IReadOnlyList<VersionRule>? Rules)
{
    public string? GetNativeClassifier(RuleContext context) => Natives.TryGetValue(context.OperatingSystem, out var classifier)
        ? classifier.Replace("${arch}", context.Is64Bit ? "64" : "32", StringComparison.Ordinal) : null;
    public override string ToString() => Name;
}

/// <summary>Parsed metadata plus a lossless JSON copy for fields not yet needed by the SDK.</summary>
public sealed class VersionMetadata
{
    internal JsonObject Document { get; }
    public string Id { get; }
    public string? InheritsFrom { get; }
    public string? Jar { get; }
    public string? Type { get; }
    public string? MainClass { get; }
    public string? Assets { get; }
    public string? LegacyGameArguments { get; }
    public JavaRequirement? Java { get; }
    public AssetIndexInfo? AssetIndex { get; }
    public LoggingConfiguration? Logging { get; }
    public IReadOnlyDictionary<string, DownloadArtifact> Downloads { get; }
    public IReadOnlyList<LibraryMetadata> Libraries { get; }
    public VersionArguments Arguments { get; }

    internal VersionMetadata(JsonObject document, string id, string? inheritsFrom, string? jar, string? type,
        string? mainClass, string? assets, string? legacyGameArguments, JavaRequirement? java,
        AssetIndexInfo? assetIndex, LoggingConfiguration? logging, IReadOnlyDictionary<string, DownloadArtifact> downloads,
        IReadOnlyList<LibraryMetadata> libraries, VersionArguments arguments)
    {
        Document = document; Id = id; InheritsFrom = inheritsFrom; Jar = jar; Type = type; MainClass = mainClass;
        Assets = assets; LegacyGameArguments = legacyGameArguments; Java = java; AssetIndex = assetIndex; Logging = logging;
        Downloads = downloads; Libraries = libraries; Arguments = arguments;
    }

    public JsonObject ToJson() => (JsonObject)Document.DeepClone();
    public IReadOnlyList<LibraryMetadata> GetLibraries(RuleContext context) =>
        Array.AsReadOnly(Libraries.Where(l => RuleEvaluator.IsAllowed(l.Rules, context)).ToArray());
    public override string ToString() => Id;
}

public sealed record ResolvedVersion(VersionMetadata Metadata, string ClientJarId, DownloadArtifact? ClientDownload,
    IReadOnlyList<string> InheritanceChain);
