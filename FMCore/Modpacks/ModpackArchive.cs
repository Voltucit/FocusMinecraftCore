using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using FMCore.Downloader;
using FMCore.Resources;

namespace FMCore.Modpacks;

/// <summary>Bounded, read-only import inspection for .mrpack and CurseForge manifest ZIPs.</summary>
public static class ModpackArchive
{
    /// <summary>Parses client metadata and hashes bundled overrides without extracting anything. Optional entries remain visible in Files.</summary>
    public static async Task<ModpackInfo> ReadAsync(string archivePath, CancellationToken cancellationToken = default)
    {
        try { using var archive = await PackArchive.OpenAsync(archivePath, cancellationToken).ConfigureAwait(false); return archive.Info; }
        catch (DownloadException) { throw PackArchive.Unsafe(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { throw PackArchive.Invalid(); }
    }
}

internal sealed class PackArchive : IDisposable
{
    internal ModpackInfo Info { get; private set; } = null!;
    internal Dictionary<string, ZipArchiveEntry> Overrides { get; } = new(StringComparer.OrdinalIgnoreCase);
    private readonly FileStream stream;
    private readonly ZipArchive zip;
    private PackArchive(FileStream stream) { this.stream = stream; zip = new(stream, ZipArchiveMode.Read, true); }
    internal static async Task<PackArchive> OpenAsync(string path, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows()) throw new ModpackException(ModpackErrorCode.Unsupported, "整合包模块当前仅支持 Windows。");
        ct.ThrowIfCancellationRequested();
        DownloadPath.CheckLinks(Path.GetFullPath(path));
        var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
        PackArchive? pack = null;
        try
        {
            pack = new(input);
            if (pack.zip.Entries.Count > 10000 || input.Length > 2L * 1024 * 1024 * 1024) throw Limit();
            var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
            long size = 0;
            foreach (var entry in pack.zip.Entries)
            {
                ct.ThrowIfCancellationRequested();
                var name = entry.FullName.TrimEnd('/');
                SafeRelative(name);
                if (!entries.TryAdd(name, entry) || ((entry.ExternalAttributes >> 16) & 0xf000) == 0xa000 ||
                    (entry.ExternalAttributes & 0x400) != 0) throw Unsafe();
                size = checked(size + entry.Length);
                if (entry.Length > 512L * 1024 * 1024 || size > 2L * 1024 * 1024 * 1024 ||
                    entry.Length > 1024 * 1024 && entry.Length / Math.Max(1, entry.CompressedLength) > 200) throw Limit();
            }
            foreach (var name in entries.Keys)
                for (var parent = name.LastIndexOf('/'); parent > 0; parent = name.LastIndexOf('/', parent - 1))
                    if (entries.TryGetValue(name[..parent], out var entry) && !entry.FullName.EndsWith('/')) throw Unsafe();
            var mr = entries.GetValueOrDefault("modrinth.index.json"); var cf = entries.GetValueOrDefault("manifest.json");
            if ((mr == null) == (cf == null)) throw Invalid();
            var manifest = mr ?? cf!;
            if (manifest.Length > 2 * 1024 * 1024) throw Limit();
            await using var data = new ArchiveEntryStream(manifest);
            using var document = await JsonDocument.ParseAsync(data, new() { MaxDepth = 24 }, ct).ConfigureAwait(false);
            NoDuplicateProperties(document.RootElement);
            var json = document.RootElement;
            var files = new Dictionary<string, ModpackFile>(StringComparer.OrdinalIgnoreCase);
            string nameText, version, minecraft; string? loader = null, loaderVersion = null;
            var overrideRoot = "overrides";
            if (mr != null)
            {
                if (json.GetProperty("formatVersion").GetInt32() != 1 || Text(json, "game") != "minecraft") throw Unsupported();
                nameText = Text(json, "name"); version = Text(json, "versionId");
                var deps = json.GetProperty("dependencies"); minecraft = Text(deps, "minecraft");
                foreach (var dep in deps.EnumerateObject())
                {
                    if (dep.Name == "minecraft") continue;
                    if (loader != null) throw Unsupported();
                    loader = dep.Name switch { "fabric-loader" => "fabric", "forge" => "forge", "neoforge" => "neoforge", _ => throw Unsupported() };
                    loaderVersion = Text(deps, dep.Name);
                }
                foreach (var file in json.GetProperty("files").EnumerateArray())
                {
                    var filePath = ContentPath(Text(file, "path"));
                    var hashes = file.GetProperty("hashes"); var sha512 = Hash(Text(hashes, "sha512"), 128); var sha1 = Hash(Text(hashes, "sha1"), 40);
                    var length = file.GetProperty("fileSize").GetInt64(); if (length < 0 || length > 512L * 1024 * 1024) throw Limit();
                    var urls = file.GetProperty("downloads").EnumerateArray().Select(v => DownloadUri(v.GetString()!)).ToArray();
                    if (urls.Length is < 1 or > 8) throw Invalid();
                    var optional = false; var unsupported = false;
                    if (file.TryGetProperty("env", out var env))
                    {
                        var client = Text(env, "client"); var server = Text(env, "server");
                        if (client is not ("required" or "optional" or "unsupported") || server is not ("required" or "optional" or "unsupported")) throw Invalid();
                        optional = client == "optional"; unsupported = client == "unsupported";
                    }
                    if (!files.TryAdd(filePath, new(filePath, length, sha512, sha1, Array.AsReadOnly(urls), optional))) throw Unsafe();
                    if (unsupported) files[filePath] = files[filePath] with { Downloads = [] }; // Validated but excluded from client selection below.
                }
            }
            else
            {
                if (Text(json, "manifestType") != "minecraftModpack" || json.GetProperty("manifestVersion").GetInt32() != 1) throw Unsupported();
                nameText = Text(json, "name"); version = Text(json, "version");
                var mc = json.GetProperty("minecraft"); minecraft = Text(mc, "version");
                foreach (var item in mc.GetProperty("modLoaders").EnumerateArray())
                {
                    if (loader != null) throw Unsupported();
                    var id = Text(item, "id"); var split = id.IndexOf('-'); if (split <= 0) throw Unsupported();
                    loader = id[..split] switch { "fabric" => "fabric", "forge" => "forge", "neoforge" => "neoforge", _ => throw Unsupported() };
                    loaderVersion = id[(split + 1)..];
                }
                overrideRoot = ContentPath(Text(json, "overrides"));
                foreach (var file in json.GetProperty("files").EnumerateArray())
                {
                    var project = file.GetProperty("projectID").GetInt32(); var id = file.GetProperty("fileID").GetInt32();
                    if (project <= 0 || id <= 0) throw Invalid();
                    var filePath = "mods/" + project + ".jar";
                    if (!files.TryAdd(filePath, new(filePath, 0, null, null, [], !file.GetProperty("required").GetBoolean(), project, id))) throw Invalid();
                }
            }
            Token(minecraft); if (loaderVersion != null) Token(loaderVersion);
            // Official layering: downloads, common overrides, then client overrides. Duplicates within a layer were rejected above.
            foreach (var prefix in mr != null ? new[] { "overrides/", "client-overrides/" } : new[] { overrideRoot + "/" })
                foreach (var entry in pack.zip.Entries.Where(e => e.FullName.StartsWith(prefix, StringComparison.Ordinal) && !e.FullName.EndsWith('/')))
                {
                    var relative = ContentPath(entry.FullName[prefix.Length..]);
                    await using var bytes = new ArchiveEntryStream(entry);
                    var sha = Convert.ToHexStringLower(await SHA512.HashDataAsync(bytes, ct).ConfigureAwait(false));
                    files[relative] = new(relative, entry.Length, sha, null, [], IsOverride: true);
                    pack.Overrides[relative] = entry;
                }
            var selected = files.Values.Where(f => f.IsOverride || cf != null || f.Downloads.Count != 0).ToArray();
            if (selected.Length > 5000 || selected.Sum(f => f.Size) > 2L * 1024 * 1024 * 1024) throw Limit();
            foreach (var item in selected)
                if (selected.Any(other => other.Path.StartsWith(item.Path + "/", StringComparison.OrdinalIgnoreCase))) throw Unsafe();
            pack.Info = new(mr != null ? ModpackFormat.Modrinth : ModpackFormat.CurseForge, nameText, version, minecraft, loader, loaderVersion, Array.AsReadOnly(selected));
            return pack;
        }
        catch (Exception e) when (e is JsonException or InvalidDataException or InvalidOperationException or KeyNotFoundException or ArgumentException or OverflowException or FormatException)
        { pack?.Dispose(); input.Dispose(); throw Invalid(); }
        catch { pack?.Dispose(); input.Dispose(); throw; }
    }
    internal static string ContentPath(string path)
    {
        SafeRelative(path);
        var top = path.Split('/')[0].ToLowerInvariant();
        if (top.StartsWith('.') || top is "saves" or "screenshots" or "logs" or "crash-reports" or "versions" or "libraries" or "assets" or "runtime" or "accounts" ||
            path.Split('/').Any(p => p.Contains("token", StringComparison.OrdinalIgnoreCase) || p.Contains("credential", StringComparison.OrdinalIgnoreCase) ||
                p.Contains("account", StringComparison.OrdinalIgnoreCase) || p.Contains("secret", StringComparison.OrdinalIgnoreCase)) ||
            top is "launcher_profiles.json" or "launcher_accounts.json" or "servers.dat" or "usercache.json") throw Unsafe();
        return path;
    }
    private static void SafeRelative(string path)
    {
        if (string.IsNullOrEmpty(path) || path.Length > 220 || path.Contains('\\') || path.Split('/').Any(p => p.Length == 0)) throw Unsafe();
        try { DownloadPath.ValidateRelative(path); } catch (DownloadException) { throw Unsafe(); }
    }
    internal static string Hash(string value, int length)
    { if (!ModrinthMetadata.Hash(value, length)) throw Invalid(); return value.ToLowerInvariant(); }
    internal static void Token(string value)
    { if (value.Length > 128 || value.Length == 0 || value.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('.' or '-' or '_' or '+'))) throw Invalid(); SafeRelative(value); }
    internal static Uri DownloadUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.UserInfo.Length != 0 ||
            uri.Fragment.Length != 0 || !uri.IsDefaultPort || value.Any(char.IsWhiteSpace) || uri.Query.Length != 0 ||
            uri.Host is not ("cdn.modrinth.com" or "github.com" or "raw.githubusercontent.com" or "gitlab.com")) throw Invalid();
        return uri;
    }
    private static string Text(JsonElement value, string key)
    { var text = value.GetProperty(key).GetString(); if (string.IsNullOrWhiteSpace(text) || text.Length > 512 || text.Any(char.IsControl)) throw Invalid(); return text; }
    private static void NoDuplicateProperties(JsonElement json)
    {
        if (json.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var p in json.EnumerateObject()) { if (!names.Add(p.Name)) throw Invalid(); NoDuplicateProperties(p.Value); }
        }
        else if (json.ValueKind == JsonValueKind.Array) foreach (var item in json.EnumerateArray()) NoDuplicateProperties(item);
    }
    internal static ModpackException Invalid() => new(ModpackErrorCode.InvalidArchive, "整合包清单或 ZIP 格式无效。");
    internal static ModpackException Unsafe() => new(ModpackErrorCode.UnsafePath, "整合包包含不安全、重复或受保护的路径。");
    internal static ModpackException Limit() => new(ModpackErrorCode.ArchiveLimitExceeded, "整合包超过文件数量、大小或压缩比例限制。");
    internal static ModpackException Unsupported() => new(ModpackErrorCode.Unsupported, "不支持此整合包格式、依赖或加载器组合；不会静默忽略。");
    public void Dispose() { zip.Dispose(); stream.Dispose(); }
}
