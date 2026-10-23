using System.Text.Json.Nodes;

namespace FMCore.Versions;

public sealed record VersionEntry(string Id, string Type, Uri Url, string Sha1, DateTimeOffset? ReleaseTime)
{
    public override string ToString() => Id;
}

public sealed record VersionManifest(string LatestRelease, string LatestSnapshot, IReadOnlyList<VersionEntry> Versions);

public sealed record DownloadArtifact(Uri Url, string? Sha1, long? Size, string? Path)
{
    internal IReadOnlyList<string> AlternateSha1 { get; init; } = [];
    // Historical HTTP URLs remain readable as metadata, but never weaken the downloader's TLS policy.
    internal bool MustRemainLocal => IsLocalOnly || Url.Scheme != "https" && !(Url.Scheme == "http" && Url.IsLoopback);
    /// <summary>空 URL 表示仅本地使用的客户端/依赖/索引；Url 为 about:blank，永不进行网络请求。</summary>
    public bool IsLocalOnly => Url.OriginalString == "about:blank";
    public override string ToString() => Path ?? "Metadata download artifact";
}

/// <summary>A processor-generated supporting file, relative to the game root; not automatically part of Classpath.</summary>
public sealed record GeneratedFile(string RelativePath, string Sha1, long Size);

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
    internal LocalProfile? LocalProfile { get; init; }
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
    /// <summary>Additional processor outputs verified by the existing installer. Reinstall the loader if one is missing or damaged.</summary>
    public IReadOnlyList<GeneratedFile> GeneratedFiles { get; internal init; } = [];

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
    IReadOnlyList<string> InheritanceChain)
{
    /// <summary>实际版本目录名；本地查询及 Launch 的 VersionId 使用此名称，不要求等于 JSON id。</summary>
    public string DirectoryName { get; init; } = Metadata.Id;
    /// <summary>本地启动 JSON 的绝对路径；内存构造的版本可为 null。</summary>
    public string? MetadataPath { get; init; }
    /// <summary>客户端 JAR 相对 Minecraft 根目录的路径；为 null 时沿用 ClientJarId 约定。</summary>
    public string? ClientJarRelativePath { get; init; }
    /// <summary>从 clientVersion、继承链或 jar/id 推导的基础版本标识；未知自定义配置可能仅返回其声明值。</summary>
    public string? MinecraftVersion => Metadata.Document["clientVersion"] is JsonValue value && value.TryGetValue<string>(out var declared)
        ? declared : InheritanceChain.Count > 1 ? InheritanceChain[0] : Metadata.Jar ?? Metadata.Id;
    /// <summary>包含本地自定义或无法通过官方来源校验的配置，不表示不支持启动。</summary>
    public bool IsCustom { get; init; }
    /// <summary>固定文案的信任/完整性提示，不包含原始 JSON 或凭据。</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
