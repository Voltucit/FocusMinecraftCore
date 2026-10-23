using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FMCore.Versions;

internal sealed record LocalProfile(string DirectoryName, string MetadataPath, bool IsCustom);
internal sealed record ProfileOwnership(string Sha1, string SemanticHash);

public sealed partial class VersionManager
{
    private static VersionMetadata AttachLocation(VersionMetadata metadata, LocalProfile source) => new(metadata.Document,
        metadata.Id, metadata.InheritsFrom, metadata.Jar, metadata.Type, metadata.MainClass, metadata.Assets,
        metadata.LegacyGameArguments, metadata.Java, metadata.AssetIndex, metadata.Logging, metadata.Downloads,
        metadata.Libraries, metadata.Arguments) { GeneratedFiles = metadata.GeneratedFiles, LocalProfile = source };
    private string? FindProfilePath(string id)
    {
        var canonical = PathFor(RelativeProfilePath(VersionParser.Id(id)));
        if (File.Exists(canonical)) return canonical;
        var folder = PathFor("versions/" + id);
        if (!Directory.Exists(folder)) return null;
        var candidates = Directory.EnumerateFiles(folder, "*.json", SearchOption.TopDirectoryOnly).Take(2).ToArray();
        if (candidates.Length > 1) throw new VersionException(VersionErrorCode.InvalidMetadata, "版本目录中有多个 JSON，且没有同名启动 JSON，无法确定启动配置。");
        if (candidates.Length == 0) return null;
        return PathFor(Path.GetRelativePath(RootDirectory, candidates[0]).Replace('\\', '/'));
    }

    private async Task<(string Path, VersionMetadata Metadata)> FindLocalAsync(string id, CancellationToken ct)
    {
        if (FindProfilePath(id) is { } direct) return (direct, VersionParser.Parse(await ReadAsync(direct, ct).ConfigureAwait(false)));
        // Parent references can use the JSON ID of a renamed folder. Resolve only an unambiguous match.
        var parent = PathFor("versions");
        (string Path, VersionMetadata Metadata)? found = null;
        if (Directory.Exists(parent))
        {
            var count = 0;
            foreach (var folder in Directory.EnumerateDirectories(parent))
            {
                ct.ThrowIfCancellationRequested();
                if (++count > 4096) throw new VersionException(VersionErrorCode.InvalidMetadata, "本地版本过多，无法安全解析目录别名。");
                try
                {
                    if (FindProfilePath(Path.GetFileName(folder)) is not { } path) continue;
                    var metadata = VersionParser.Parse(await ReadAsync(path, ct).ConfigureAwait(false));
                    if (metadata.Id != id) continue;
                    if (found != null) throw new VersionException(VersionErrorCode.ConflictingProfiles, "多个版本目录声明相同 JSON ID，请使用明确的目录名或修复继承引用。");
                    found = (path, metadata);
                }
                catch (VersionException e) when (e.Code is VersionErrorCode.InvalidMetadata or VersionErrorCode.UnsafePath) { }
            }
        }
        return found ?? throw new VersionException(VersionErrorCode.NotCached, "指定版本的本地启动 JSON 不存在。");
    }

    private string OwnershipPath(string id) => PathFor(".cache/metadata/ownership/" + VersionParser.Id(id) + ".json");
    private async Task<ProfileOwnership?> OwnershipAsync(string id, CancellationToken ct)
    {
        var path = OwnershipPath(id);
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<ProfileOwnership>(await ReadAsync(path, ct).ConfigureAwait(false)) ?? throw new JsonException(); }
        catch (JsonException) { throw new VersionException(VersionErrorCode.InvalidMetadata, "官方元数据来源记录损坏，请先检查本地缓存。"); }
    }
    private static string SemanticHash(VersionMetadata metadata) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(metadata.ToJson().ToJsonString())));
    private Task SaveOwnershipAsync(string id, string sha1, VersionMetadata metadata, CancellationToken ct) => StoreAsync(OwnershipPath(id),
        JsonSerializer.SerializeToUtf8Bytes(new ProfileOwnership(sha1, SemanticHash(metadata))), ct);
    private string JarPath(string directory, string file) => PathFor($"versions/{VersionParser.Id(directory)}/{VersionParser.Id(file)}.jar");
    private string LocalJar(string directory, string metadataId)
    {
        var canonical = JarPath(directory, directory);
        if (File.Exists(canonical) || directory == metadataId) return Path.GetRelativePath(RootDirectory, canonical).Replace('\\', '/');
        var named = JarPath(directory, metadataId);
        return Path.GetRelativePath(RootDirectory, File.Exists(named) ? named : canonical).Replace('\\', '/');
    }
}
