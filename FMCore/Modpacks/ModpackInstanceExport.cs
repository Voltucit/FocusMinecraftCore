using System.Globalization;
using System.Security.Cryptography;
using FMCore.Downloader;
using FMCore.Instances;
using FMCore.Resources;
using FMCore.Versions;

namespace FMCore.Modpacks;

public sealed partial class ModpackInstaller
{
    private async Task<ModpackInfo> PrepareExportAsync(PackState? state, ModpackExportOptions settings, CancellationToken ct)
    {
        if (settings.OverridePaths == null || settings.OverridePaths.Count > 5000 ||
            settings.OverridePaths.Count != 0 && !settings.IncludeOverrides) throw Invalid();
        var paths = settings.OverridePaths.ToArray();
        if (paths.Any(p => p == null)) throw Invalid();
        var pack = state?.Installed.Pack ?? await ReadInstanceExportAsync(ct).ConfigureAwait(false);
        var selected = (state?.Files.Select(f => f.File) ?? pack.Files)
            .Where(f => !f.IsOverride || settings.IncludeOverrides && paths.Length == 0).ToDictionary(f => f.Path, StringComparer.OrdinalIgnoreCase);
        foreach (var path in paths)
        {
            ct.ThrowIfCancellationRequested();
            PackArchive.ContentPath(path);
            if (selected.ContainsKey(path)) throw PackFiles.Conflict();
            var owned = state?.Files.Select(f => f.File).FirstOrDefault(f => f.IsOverride && f.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
            selected.Add(path, owned ?? await ExportFileAsync(path, ct).ConfigureAwait(false) with { IsOverride = true });
        }
        var name = settings.Name ?? pack.Name;
        var version = settings.Version ?? pack.Version;
        if (new[] { name, version }.Any(s => string.IsNullOrWhiteSpace(s) || s.Length > 512 || s.Any(char.IsControl))) throw Invalid();
        if (selected.Count > 5000 || selected.Values.Sum(f => f.Size) > 2L * 1024 * 1024 * 1024) throw PackArchive.Limit();
        await VerifyExportFilesAsync(selected.Values, ct).ConfigureAwait(false);
        return pack with { Name = name, Version = version, Files = Array.AsReadOnly(selected.Values.ToArray()) };
    }

    private async Task VerifyExportFilesAsync(IEnumerable<ModpackFile> selected, CancellationToken ct)
    {
        foreach (var file in selected)
        {
            ct.ThrowIfCancellationRequested();
            PackArchive.ContentPath(file.Path);
            if (!await files.MatchesAsync(files.At(file.Path), PackFiles.Stamp(file), ct).ConfigureAwait(false)) throw PackFiles.Conflict();
        }
    }

    private async Task<ModpackFile> ExportFileAsync(string relative, CancellationToken ct)
    {
        PackArchive.ContentPath(relative);
        await using var input = new FileStream(files.At(relative), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
        if (input.Length > 512L * 1024 * 1024) throw PackArchive.Limit();
        var length = input.Length;
        var sha512 = Convert.ToHexStringLower(await SHA512.HashDataAsync(input, ct).ConfigureAwait(false));
        input.Position = 0;
        var sha1 = Convert.ToHexStringLower(await SHA1.HashDataAsync(input, ct).ConfigureAwait(false));
        return new(relative, length, sha512, sha1, []);
    }

    private async Task<ModpackInfo> ReadInstanceExportAsync(CancellationToken ct)
    {
        if (!Directory.Exists(GameDirectory)) throw new ModpackException(ModpackErrorCode.NotFound, "未找到要导出的游戏实例目录。");
        var instance = File.Exists(files.At(InstanceFiles.Metadata)) ? await InstanceFiles.ReadAsync(GameDirectory, id, ct).ConfigureAwait(false) : null;
        var core = await GameCoreUtil.GetGameCoreAsync(instance?.Config.VersionId ?? id, root, ct).ConfigureAwait(false);
        if (core.MinecraftVersion == null || core.Loaders.Count > 1) throw PackArchive.Unsupported();
        PackArchive.Token(core.MinecraftVersion);
        var identified = core.Loaders.SingleOrDefault();
        var loader = identified?.Name switch { null => null, "Fabric" => "fabric", "Forge" => "forge", "NeoForge" => "neoforge", _ => throw PackArchive.Unsupported() };
        var loaderVersion = identified?.Version;
        if (identified != null)
        {
            var receipt = core.ResolvedVersion!.Metadata.ToJson()["fmcoreInstaller"];
            if (receipt?["name"]?.GetValue<string>() != identified.Name)
            {
                var coordinate = core.ResolvedVersion.Metadata.Libraries.Select(l => l.Coordinate).FirstOrDefault(c =>
                    (c.Group, c.Artifact) == ("net.fabricmc", "fabric-loader") && loader == "fabric" ||
                    (c.Group, c.Artifact) == ("net.minecraftforge", "forge") && loader == "forge" ||
                    c.Group == "net.neoforged" && c.Artifact is "neoforge" or "forge" && loader == "neoforge");
                loaderVersion = coordinate?.Version ?? throw PackArchive.Unsupported();
            }
            if (loader == "forge" && loaderVersion!.StartsWith(core.MinecraftVersion + "-", StringComparison.Ordinal))
                loaderVersion = loaderVersion[(core.MinecraftVersion.Length + 1)..];
            PackArchive.Token(loaderVersion!);
        }
        var receipts = new List<InstalledMod>();
        foreach (var source in new[] { "modrinth", "curseforge" })
        {
            if (!File.Exists(files.At(".fmcore/resources/" + source + ".json"))) continue;
            var storage = new ResourceFiles(GameDirectory, source);
            var receipt = await storage.ReadAsync(null, ct).ConfigureAwait(false);
            if (receipt.Environment.MinecraftVersion != core.MinecraftVersion || receipt.Environment.Loader != loader) throw PackFiles.Conflict();
            await storage.VerifyOwnedAsync(receipt.Mods, ct).ConfigureAwait(false);
            receipts.AddRange(receipt.Mods);
        }
        var references = new List<ModpackFile>();
        foreach (var (folder, projectType) in new[] { ("mods", "mod"), ("resourcepacks", "resourcepack"), ("shaderpacks", "shader") })
        {
            foreach (var relative in ExportResourcePaths(folder, ct))
            {
                ct.ThrowIfCancellationRequested();
                if (folder == "mods" && loader == null) throw PackArchive.Unsupported();
                var local = await ExportFileAsync(relative, ct).ConfigureAwait(false);
                if (local.Size == 0) throw MissingExportSource(relative);
                var owned = folder == "mods" ? receipts.SingleOrDefault(m => m.FileName.Equals(Path.GetFileName(relative), StringComparison.OrdinalIgnoreCase)) : null;
                if (owned?.Source == "curseforge")
                {
                    if (options.CurseForge == null || !int.TryParse(owned.ProjectId, NumberStyles.None, CultureInfo.InvariantCulture, out var projectId) ||
                        !int.TryParse(owned.VersionId, NumberStyles.None, CultureInfo.InvariantCulture, out var fileId)) throw MissingExportSource(relative);
                    var remote = await options.CurseForge.GetFileAsync(projectId, fileId, ct).ConfigureAwait(false);
                    CurseForgeMetadata.Downloadable(await options.CurseForge.GetProjectAsync(projectId, ct).ConfigureAwait(false), remote);
                    if (remote.Sha1 != local.Sha1 || remote.Size != local.Size) throw PackFiles.Conflict();
                    references.Add(local with { Downloads = [PackArchive.DownloadUri(remote.DownloadUrl!.AbsoluteUri)] });
                }
                else
                {
                    var version = await modrinth.GetVersionByHashAsync(local.Sha512!, ct).ConfigureAwait(false) ?? throw MissingExportSource(relative);
                    var artifact = version.Files.FirstOrDefault(f => f.Sha512 == local.Sha512 && f.Sha1 == local.Sha1 && f.Size == local.Size && f.FileType == null);
                    if (artifact == null || (await modrinth.GetProjectAsync(version.ProjectId, ct).ConfigureAwait(false)).ProjectType != projectType) throw MissingExportSource(relative);
                    references.Add(local with { Downloads = [PackArchive.DownloadUri(artifact.Url.AbsoluteUri)] });
                }
                if (references.Count > 5000 || references.Sum(f => f.Size) > 2L * 1024 * 1024 * 1024) throw PackArchive.Limit();
            }
        }
        return new(ModpackFormat.Modrinth, instance?.Config.Name ?? id, "1.0.0", core.MinecraftVersion, loader, loaderVersion, references.AsReadOnly());
    }

    private IEnumerable<string> ExportResourcePaths(string folder, CancellationToken ct)
    {
        var start = files.At(folder);
        if (File.Exists(start)) throw PackFiles.Conflict();
        if (!Directory.Exists(start)) yield break;
        var directories = new Stack<string>(); directories.Push(start);
        var count = 0;
        while (directories.TryPop(out var directory))
            foreach (var path in Directory.EnumerateFileSystemEntries(directory))
            {
                ct.ThrowIfCancellationRequested();
                if (++count > 10000) throw PackArchive.Limit();
                DownloadPath.CheckLinks(path);
                if (Directory.Exists(path))
                {
                    if (folder != "mods") throw MissingExportSource(folder); // Unpacked resource/shader packs have no exact platform file hash.
                    directories.Push(path);
                }
                else if (Path.GetExtension(path).Equals(folder == "mods" ? ".jar" : ".zip", StringComparison.OrdinalIgnoreCase))
                    yield return PackArchive.ContentPath(Path.GetRelativePath(GameDirectory, path).Replace('\\', '/'));
            }
    }

    private static ModpackException MissingExportSource(string relative) => new(ModpackErrorCode.DownloadRestricted,
        $"无法确认资源 {relative} 的准确来源、下载许可或标准清单引用；未打包未知二进制文件。请提供合法平台客户端或处理未识别资源。");
}
