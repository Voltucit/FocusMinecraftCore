using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using FMCore.Downloader;
using FMCore.Logging;
using FMCore.Utilities;

namespace FMCore.Modpacks;

public sealed partial class ModpackInstaller
{
    /// <summary>Exports exact references from an installed pack or an ordinary instance. Ordinary instances use a Modrinth manifest, with either a .zip or .mrpack destination; existing packs retain their format.</summary>
    /// <remarks>Does not overwrite the destination or alter game files. Unidentified resources fail explicitly; overrides require selection and sensitive-data/distribution checks.</remarks>
    public Task ExportAsync(string destination, ModpackExportOptions? exportOptions = null, CancellationToken cancellationToken = default) => ExecuteAsync(async ct =>
    {
        var settings = exportOptions ?? new();
        var path = Path.GetFullPath(destination); DownloadPath.CheckLinks(path);
        if (File.Exists(path) || Directory.Exists(path) || path.StartsWith(GameDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw PackFiles.Conflict();
        var pack = await PrepareExportAsync(await files.ReadAsync(ct).ConfigureAwait(false), settings, ct).ConfigureAwait(false);
        var manifest = Manifest(pack);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            GameFiles.CreateDirectory(Path.GetDirectoryName(path)!);
            await using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous))
            {
                using var zip = new ZipArchive(output, ZipArchiveMode.Create, true);
                var manifestEntry = zip.CreateEntry(pack.Format == ModpackFormat.Modrinth ? "modrinth.index.json" : "manifest.json");
                await using (var bytes = manifestEntry.Open()) await bytes.WriteAsync(Encoding.UTF8.GetBytes(manifest.ToJsonString()), ct).ConfigureAwait(false);
                foreach (var file in pack.Files.Where(f => f.IsOverride))
                {
                    ct.ThrowIfCancellationRequested();
                    PackArchive.ContentPath(file.Path);
                    if (!await files.MatchesAsync(files.At(file.Path), PackFiles.Stamp(file), ct).ConfigureAwait(false)) throw PackFiles.Conflict();
                    var isText = Path.GetExtension(file.Path).ToLowerInvariant() is ".json" or ".json5" or ".toml" or ".cfg" or ".txt" or ".properties" or ".yaml" or ".yml" or ".ini" or ".js" or ".zs";
                    if (!isText && !settings.AllowBinaryOverrides) throw new ModpackException(ModpackErrorCode.DownloadRestricted, "导出二进制覆盖文件需要调用方明确确认拥有分发权。");
                    if (isText)
                    {
                        if (file.Size > 2 * 1024 * 1024) throw PackArchive.Limit();
                        string text;
                        try { text = await File.ReadAllTextAsync(files.At(file.Path), new UTF8Encoding(false, true), ct).ConfigureAwait(false); }
                        catch (DecoderFallbackException) { throw new ModpackException(ModpackErrorCode.DownloadRestricted, "覆盖文本不是有效 UTF-8，无法安全检查敏感信息；未导出。"); }
                        var filter = new SensitiveDataFilter([]);
                        if (filter.Redact(text) != text || ContainsPrivateKeys(text)) throw new ModpackException(ModpackErrorCode.DownloadRestricted, "覆盖文件可能含凭据或个人身份数据，未导出。");
                    }
                    var entry = zip.CreateEntry("overrides/" + file.Path, CompressionLevel.Optimal);
                    await using var bytes = entry.Open();
                    await using var input = new FileStream(files.At(file.Path), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
                    await input.CopyToAsync(bytes, ct).ConfigureAwait(false);
                }
            }
            // Reuse the same reader as import, so every successful export is a bounded, unambiguous standard archive.
            using (await PackArchive.OpenAsync(temporary, ct).ConfigureAwait(false)) { }
            await VerifyExportFilesAsync(pack.Files, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            DownloadPath.CheckLinks(path); File.Move(temporary, path);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return true;
    }, cancellationToken);
    private static bool ContainsPrivateKeys(string value) => new[] { "password", "accessToken", "refreshToken", "api_key", "apiKey", "clientSecret", "uuid", "username", "email", "authorization" }
        .Any(key => value.Contains(key, StringComparison.OrdinalIgnoreCase));
    private static JsonObject Manifest(ModpackInfo pack)
    {
        if (pack.Format == ModpackFormat.Modrinth)
        {
            var dependencies = new JsonObject { ["minecraft"] = pack.MinecraftVersion };
            if (pack.Loader != null) dependencies[pack.Loader == "fabric" ? "fabric-loader" : pack.Loader] = pack.LoaderVersion;
            var list = new JsonArray();
            foreach (var file in pack.Files.Where(f => !f.IsOverride))
            {
                if (file.Sha1 == null || file.Downloads.Count == 0) throw PackArchive.Unsupported();
                foreach (var uri in file.Downloads) PackArchive.DownloadUri(uri.AbsoluteUri);
                list.Add(new JsonObject { ["path"] = file.Path, ["fileSize"] = file.Size,
                    ["hashes"] = new JsonObject { ["sha512"] = file.Sha512, ["sha1"] = file.Sha1 },
                    ["downloads"] = new JsonArray(file.Downloads.Select(u => (JsonNode?)JsonValue.Create(u.AbsoluteUri)).ToArray()),
                    ["env"] = new JsonObject { ["client"] = file.Optional ? "optional" : "required", ["server"] = "unsupported" } });
            }
            return new() { ["formatVersion"] = 1, ["game"] = "minecraft", ["name"] = pack.Name, ["versionId"] = pack.Version, ["dependencies"] = dependencies, ["files"] = list };
        }
        var mods = new JsonArray();
        foreach (var file in pack.Files.Where(f => !f.IsOverride))
        {
            if (file.ProjectId == null || file.FileId == null) throw PackArchive.Unsupported();
            mods.Add(new JsonObject { ["projectID"] = file.ProjectId.Value, ["fileID"] = file.FileId.Value, ["required"] = !file.Optional });
        }
        return new() { ["manifestType"] = "minecraftModpack", ["manifestVersion"] = 1, ["name"] = pack.Name, ["version"] = pack.Version, ["author"] = "",
            ["minecraft"] = new JsonObject { ["version"] = pack.MinecraftVersion, ["modLoaders"] = pack.Loader == null ? new JsonArray() :
                new JsonArray(new JsonObject { ["id"] = pack.Loader + "-" + pack.LoaderVersion, ["primary"] = true }) }, ["files"] = mods, ["overrides"] = "overrides" };
    }
}
