using System.Text.Json;
using FMCore.Downloader;

namespace FMCore.Assets;

public enum AssetErrorCode { InvalidIndex, UnsafePath, MissingObject, HashMismatch, IoError }
public sealed class AssetException(AssetErrorCode code, string message) : Exception(message)
{
    public AssetErrorCode Code { get; } = code;
}

public sealed record AssetObject(string Name, string Hash, long Size)
{
    public string RelativePath => $"assets/objects/{Hash[..2]}/{Hash}";
}

public sealed record AssetIndex(IReadOnlyList<AssetObject> Objects, bool Virtual, bool MapToResources)
{
    public static AssetIndex Parse(string json)
    {
        try
        {
            if (json.Length > 16 * 1024 * 1024) throw Invalid();
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
            var root = document.RootElement;
            var objects = new List<AssetObject>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var sizes = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (var item in root.GetProperty("objects").EnumerateObject())
            {
                if (objects.Count >= 200_000 || !names.Add(item.Name) || item.Name.Contains('\\')) throw Invalid();
                DownloadPath.ValidateRelative(item.Name);
                var hash = item.Value.GetProperty("hash").GetString();
                var size = item.Value.GetProperty("size").GetInt64();
                if (hash == null || hash.Length != 40 || !hash.All(char.IsAsciiHexDigit) || size < 0) throw Invalid();
                hash = hash.ToLowerInvariant();
                if (sizes.TryGetValue(hash, out var existing) && existing != size) throw Invalid();
                sizes[hash] = size;
                objects.Add(new(item.Name, hash, size));
            }
            return new(objects.AsReadOnly(), Flag("virtual"), Flag("map_to_resources"));
            bool Flag(string key) => root.TryGetProperty(key, out var flag) && flag.GetBoolean();
        }
        catch (DownloadException) { throw new AssetException(AssetErrorCode.UnsafePath, "资源索引包含不安全的逻辑路径。"); }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentException)
        { throw Invalid(); }
    }

    private static AssetException Invalid() => new(AssetErrorCode.InvalidIndex, "资源索引结构、大小、对象哈希或名称无效。");
}
