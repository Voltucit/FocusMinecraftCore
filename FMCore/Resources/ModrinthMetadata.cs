using System.Text.Json;
using FMCore.Downloader;

namespace FMCore.Resources;

internal static class ModrinthMetadata
{
    internal static ResourceException Invalid() => new(ResourceErrorCode.InvalidMetadata, "Modrinth 返回的数据格式无效、过大或不受支持。");
    internal static ResourceException InvalidRequest() => new(ResourceErrorCode.InvalidRequest, "资源查询参数、标识符或目标环境无效。");
    internal static ResourceException Conflict() => new(ResourceErrorCode.Conflict, "Mod 依赖、版本、文件名或用户文件发生冲突；未覆盖未知文件。");
    internal static string Token(string value)
    {
        if (string.IsNullOrEmpty(value) || value is "." or ".." || value.Length > 180 || value.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_' or '.' or '+')))
            throw InvalidRequest();
        return value;
    }
    internal static ModEnvironment Environment(ModEnvironment value)
    {
        if (value == null) throw InvalidRequest();
        Token(value.MinecraftVersion);
        if (value.Loader is not ("fabric" or "forge" or "neoforge"))
            throw new ResourceException(ResourceErrorCode.Unsupported, "Mod 安装目前支持 Fabric、Forge 和 NeoForge 目标环境。");
        return value;
    }
    internal static bool Compatible(ModrinthVersion version, ModEnvironment target) =>
        version.GameVersions.Contains(target.MinecraftVersion, StringComparer.Ordinal) && version.Loaders.Contains(target.Loader, StringComparer.Ordinal);

    internal static string FileName(string value)
    {
        try
        {
            if (value.Length > 240 || value.Contains('/') || value.Contains('\\')) throw new ArgumentException();
            DownloadPath.ValidateRelative(value);
            return value;
        }
        catch (Exception e) when (e is ArgumentException or DownloadException)
        { throw new ResourceException(ResourceErrorCode.UnsafePath, "资源文件名不安全。"); }
    }
    internal static bool Hash(string value, int length) => value != null && value.Length == length && value.All(char.IsAsciiHexDigit);

    internal static ModrinthSearchResult Search(JsonElement root) => Parse(() => new ModrinthSearchResult(
        System.Array.AsReadOnly(Array(root, "hits", 100).Select(p => Project(p, true)).ToArray()), Number(root, "offset"), Number(root, "limit"), Number(root, "total_hits")));
    internal static ModrinthProject Project(JsonElement value, bool search = false) => Parse(() => new ModrinthProject(
        Token(Text(value, search ? "project_id" : "id")), Token(Text(value, "slug")), Text(value, "title"), Text(value, "description"), Text(value, "project_type")));
    internal static IReadOnlyList<ModrinthVersion> Versions(JsonElement root) => Parse(() =>
    {
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() > 20000) throw Invalid();
        var versions = root.EnumerateArray().Select(Version).ToArray();
        if (versions.Select(v => v.Id).Distinct(StringComparer.Ordinal).Count() != versions.Length) throw Invalid();
        return System.Array.AsReadOnly(versions);
    });
    internal static ModrinthVersion Version(JsonElement value) => Parse(() =>
    {
        var files = Array(value, "files", 128).Select(File).ToArray();
        var channel = Text(value, "version_type");
        if (channel is not ("release" or "beta" or "alpha") || files.Length == 0 || files.Count(f => f.Primary) > 1 ||
            files.Select(f => f.FileName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != files.Length) throw Invalid();
        return new ModrinthVersion(Token(Text(value, "id")), Token(Text(value, "project_id")), Text(value, "name"), Text(value, "version_number"), channel,
            value.GetProperty("date_published").GetDateTimeOffset(), Strings(value, "game_versions"), Strings(value, "loaders"),
            System.Array.AsReadOnly(files), System.Array.AsReadOnly(Array(value, "dependencies", 512).Select(Dependency).ToArray()));
    });
    private static ModrinthFile File(JsonElement value)
    {
        var uri = new Uri(Text(value, "url"), UriKind.Absolute);
        if (uri.Scheme != "https" || uri.Host != "cdn.modrinth.com" || !uri.IsDefaultPort || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0) throw Invalid();
        var hashes = value.GetProperty("hashes");
        var sha512 = Text(hashes, "sha512"); var sha1 = Optional(hashes, "sha1");
        var size = value.GetProperty("size").GetInt64();
        if (!Hash(sha512, 128) || sha1 != null && !Hash(sha1, 40) || size <= 0) throw Invalid();
        return new(FileName(Text(value, "filename")), uri, sha512.ToLowerInvariant(), sha1?.ToLowerInvariant(), size,
            value.GetProperty("primary").GetBoolean(), Optional(value, "file_type"));
    }
    private static ModDependency Dependency(JsonElement value)
    {
        var project = Optional(value, "project_id"); var version = Optional(value, "version_id");
        if (project != null) Token(project); if (version != null) Token(version);
        var type = Text(value, "dependency_type") switch
        {
            "required" => ModDependencyType.Required, "optional" => ModDependencyType.Optional,
            "incompatible" => ModDependencyType.Incompatible, "embedded" => ModDependencyType.Embedded, _ => throw Invalid()
        };
        return new(project, version, Optional(value, "file_name"), type);
    }
    internal static ModrinthFile Artifact(ModrinthVersion version)
    {
        var file = version.Files.FirstOrDefault(f => f.Primary) ?? version.Files.FirstOrDefault() ?? throw Invalid();
        if (!file.FileName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) || file.FileType != null)
            throw new ResourceException(ResourceErrorCode.Unsupported, "所选版本的主文件不是可安装的 Mod JAR。");
        return file;
    }
    private static IReadOnlyList<string> Strings(JsonElement value, string name) => System.Array.AsReadOnly(Array(value, name, 1024)
        .Select(v => Token(v.GetString() ?? throw Invalid())).ToArray());
    private static IEnumerable<JsonElement> Array(JsonElement value, string name, int limit)
    {
        var array = value.GetProperty(name);
        if (array.ValueKind != JsonValueKind.Array || array.GetArrayLength() > limit) throw Invalid();
        return array.EnumerateArray();
    }
    private static string Text(JsonElement value, string name)
    {
        var text = value.GetProperty(name).GetString() ?? throw Invalid();
        if (text.Length > 32768) throw Invalid();
        return text;
    }
    private static string? Optional(JsonElement value, string name) => !value.TryGetProperty(name, out var element) || element.ValueKind == JsonValueKind.Null ? null : Text(value, name);
    private static int Number(JsonElement value, string name)
    { var result = value.GetProperty(name).GetInt32(); return result >= 0 ? result : throw Invalid(); }
    private static T Parse<T>(Func<T> parse)
    {
        try { return parse(); }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentException ||
            e is ResourceException { Code: ResourceErrorCode.InvalidRequest }) { throw Invalid(); }
    }
}
