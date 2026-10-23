using System.Text.Json;

namespace FMCore.Resources;

internal static class CurseForgeMetadata
{
    internal static ResourceException Invalid() => new(ResourceErrorCode.InvalidMetadata, "CurseForge 响应格式、标识符、哈希或分页数据无效。");
    internal static ResourceException Restricted() => new(ResourceErrorCode.DownloadRestricted, "CurseForge 禁止自动分发、未提供下载地址或文件当前不可公开下载；未尝试其他来源。");
    internal static int Id(int value) => value > 0 ? value : throw ModrinthMetadata.InvalidRequest();
    internal static int Loader(string value) => value switch
    { "forge" => 1, "fabric" => 4, "neoforge" => 6, _ => throw new ResourceException(ResourceErrorCode.Unsupported, "资源安装仅支持 Fabric、Forge 和 NeoForge。") };
    internal static void Pagination(int index, int pageSize)
    { if (index < 0 || pageSize is < 1 or > 50 || index > 10000 - pageSize) throw ModrinthMetadata.InvalidRequest(); }
    internal static bool Compatible(CurseForgeFile file, ModEnvironment target) =>
        file.GameVersions.Contains(target.MinecraftVersion, StringComparer.Ordinal) && file.GameVersions.Contains(target.Loader, StringComparer.OrdinalIgnoreCase);
    internal static void Downloadable(CurseForgeProject project, CurseForgeFile file)
    {
        if (project.GameId != 432 || file.GameId != 432 || project.ClassId != 6) throw new ResourceException(ResourceErrorCode.Unsupported, "此入口仅安装 Minecraft Mod。");
        if (project.Id != file.ModId) throw Invalid();
        if (project.AllowModDistribution == false || !project.IsAvailable || !file.IsAvailable || file.DownloadUrl == null || file.IsEarlyAccessContent) throw Restricted();
        if (file.IsServerPack || file.ExposeAsAlternative || !file.FileName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
            throw new ResourceException(ResourceErrorCode.Unsupported, "不支持将服务端整合包、替代附件或非 JAR 文件作为客户端 Mod 安装。");
        if (file.Sha1 == null) throw new ResourceException(ResourceErrorCode.Unsupported, "文件没有 SHA-1；当前不使用单独 MD5 作为下载校验依据。");
    }
    internal static IReadOnlyList<ModDependency> Dependencies(CurseForgeFile file) => System.Array.AsReadOnly(file.Dependencies.Select(d => new ModDependency(
        d.ModId.ToString(System.Globalization.CultureInfo.InvariantCulture), null, null, d.RelationType switch
        {
            CurseForgeRelationType.RequiredDependency => ModDependencyType.Required,
            CurseForgeRelationType.Incompatible => ModDependencyType.Incompatible,
            CurseForgeRelationType.EmbeddedLibrary or CurseForgeRelationType.Include => ModDependencyType.Embedded,
            _ => ModDependencyType.Optional
        })).ToArray());

    internal static CurseForgeProject Project(JsonElement value) => Parse(() => new CurseForgeProject(
        Id(value.GetProperty("id").GetInt32()), Id(value.GetProperty("gameId").GetInt32()), Text(value, "name"), Text(value, "slug"), Text(value, "summary"),
        value.TryGetProperty("classId", out var kind) && kind.ValueKind != JsonValueKind.Null ? kind.GetInt32() : null,
        value.TryGetProperty("allowModDistribution", out var distribution) && distribution.ValueKind != JsonValueKind.Null ? distribution.GetBoolean() : null,
        value.GetProperty("isAvailable").GetBoolean())
        {
            IconUrl = value.TryGetProperty("logo", out var logo) && logo.ValueKind == JsonValueKind.Object
                ? ResourceImages.Url(logo, "thumbnailUrl") ?? ResourceImages.Url(logo, "url") : null,
            Gallery = ResourceImages.Gallery(value, true)
        });
    internal static CurseForgeFile File(JsonElement value) => Parse(() =>
    {
        string? sha1 = null, md5 = null;
        foreach (var hash in Array(value, "hashes", 8))
        {
            var algorithm = hash.GetProperty("algo").GetInt32(); var text = Text(hash, "value").ToLowerInvariant();
            if (algorithm == 1) { if (sha1 != null || !ModrinthMetadata.Hash(text, 40)) throw Invalid(); sha1 = text; }
            else if (algorithm == 2) { if (md5 != null || !ModrinthMetadata.Hash(text, 32)) throw Invalid(); md5 = text; }
            else throw Invalid();
        }
        var url = Optional(value, "downloadUrl");
        Uri? uri = null;
        if (!string.IsNullOrEmpty(url))
        {
            uri = new(url, UriKind.Absolute);
            if (uri.Scheme != "https" || uri.Host is not ("edge.forgecdn.net" or "mediafilez.forgecdn.net" or "media.forgecdn.net") ||
                uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 || !uri.IsDefaultPort) throw Invalid();
        }
        var channel = value.GetProperty("releaseType").GetInt32(); var size = value.GetProperty("fileLength").GetInt64();
        var date = value.GetProperty("fileDate").GetDateTimeOffset();
        if (channel is < 1 or > 3 || size <= 0 || date == default) throw Invalid();
        var versions = Array(value, "gameVersions", 1024).Select(v => v.GetString() ?? throw Invalid()).ToArray();
        if (versions.Any(v => v.Length > 180 || v.Any(char.IsControl))) throw Invalid();
        var dependencies = Array(value, "dependencies", 512).Select(d =>
        {
            var relation = d.GetProperty("relationType").GetInt32(); if (relation is < 1 or > 6) throw Invalid();
            return new CurseForgeDependency(Id(d.GetProperty("modId").GetInt32()), (CurseForgeRelationType)relation);
        }).ToArray();
        return new CurseForgeFile(Id(value.GetProperty("id").GetInt32()), Id(value.GetProperty("modId").GetInt32()), Id(value.GetProperty("gameId").GetInt32()),
            Text(value, "displayName"), ModrinthMetadata.FileName(Text(value, "fileName")), channel, date, size, sha1, md5, uri,
            value.GetProperty("isAvailable").GetBoolean(), Flag(value, "isServerPack"), Flag(value, "isEarlyAccessContent"), Flag(value, "exposeAsAlternative"),
            System.Array.AsReadOnly(versions), System.Array.AsReadOnly(dependencies));
    });
    internal static (JsonElement[] Items, int Index, int Size, int Total) Page(JsonElement value) => Parse(() =>
    {
        var page = value.GetProperty("pagination");
        var index = page.GetProperty("index").GetInt32(); var size = page.GetProperty("pageSize").GetInt32();
        var total = page.GetProperty("totalCount").GetInt32(); var count = page.GetProperty("resultCount").GetInt32();
        Pagination(index, size);
        var items = Array(value, "data", 50).ToArray();
        if (total < 0 || count != items.Length || count > size || count > Math.Max(0, total - index)) throw Invalid();
        return (items, index, size, total);
    });
    private static IEnumerable<JsonElement> Array(JsonElement value, string property, int limit)
    { var data = value.GetProperty(property); if (data.ValueKind != JsonValueKind.Array || data.GetArrayLength() > limit) throw Invalid(); return data.EnumerateArray(); }
    private static bool Flag(JsonElement value, string property) => value.TryGetProperty(property, out var flag) && flag.GetBoolean();
    private static string? Optional(JsonElement value, string property) => !value.TryGetProperty(property, out var element) || element.ValueKind == JsonValueKind.Null ? null : Text(value, property);
    private static string Text(JsonElement value, string property)
    { var text = value.GetProperty(property).GetString() ?? throw Invalid(); if (text.Length > 32768) throw Invalid(); return text; }
    private static T Parse<T>(Func<T> read)
    {
        try { return read(); }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentException ||
            e is ResourceException { Code: ResourceErrorCode.InvalidRequest }) { throw Invalid(); }
    }
}
