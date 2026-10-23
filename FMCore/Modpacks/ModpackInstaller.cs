using System.Globalization;
using System.Security.Cryptography;
using FMCore.Authentication;
using FMCore.Downloader;
using FMCore.Installer;
using FMCore.Instances;
using FMCore.Loaders;
using FMCore.Launch;
using FMCore.Resources;
using FMCore.Utilities;

namespace FMCore.Modpacks;

/// <summary>Imports, installs and transactionally updates one isolated client modpack. Requires Windows; no UI or dependency injection.</summary>
public sealed partial class ModpackInstaller
{
    private static readonly HttpClient SharedDownloads = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly string root;
    private readonly string id;
    private readonly ModpackOptions options;
    private readonly ModrinthClient modrinth;
    private readonly HttpClient http;
    private readonly PackFiles files;
    /// <summary>写入时自动创建 root/versions/instanceId；instanceId 是独立目录名，与整包解析得到的启动版本 ID 分别管理。实例事务不接管版本 JSON/JAR/natives。</summary>
    public ModpackInstaller(string rootDirectory, string instanceId, ModpackOptions? options = null)
    {
        try
        {
            if (!OperatingSystem.IsWindows()) throw PackArchive.Unsupported();
            root = Path.GetFullPath(rootDirectory); id = InstanceFiles.Id(instanceId);
            GameDirectory = GameFiles.Resolve(root, InstanceFiles.Relative(id)); files = new(GameDirectory);
            this.options = options ?? new(); modrinth = this.options.Modrinth ?? new();
            http = this.options.DownloadClient ?? SharedDownloads;
            if (http.DefaultRequestHeaders.Any(h => h.Key.Equals("x-api-key", StringComparison.OrdinalIgnoreCase) ||
                h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) || h.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase) ||
                h.Key.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase))) throw Invalid();
            if (this.options.SourceId != null) PackArchive.Token(this.options.SourceId);
            ArgumentNullException.ThrowIfNull(this.options.Downloads);
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException) { throw Invalid(); }
        catch (DownloadException) { throw PackArchive.Unsafe(); }
        catch (InstanceException) { throw PackArchive.Unsafe(); }
    }
    /// <summary>Absolute instance game directory.</summary>
    public string GameDirectory { get; }
    /// <summary>Imports an archive into this instance; same complete transaction and runtime behavior as InstallAsync.</summary>
    public Task<ModpackInstallResult> ImportAsync(string archivePath, IProgress<ModpackProgress>? progress = null,
        CancellationToken cancellationToken = default) => InstallAsync(archivePath, progress, cancellationToken);
    /// <summary>Installs exact pack files and runtime, preserving user content. Existing pack updates require UpdateAsync.</summary>
    public Task<ModpackInstallResult> InstallAsync(string archivePath, IProgress<ModpackProgress>? progress = null,
        CancellationToken cancellationToken = default) => ExecuteAsync(ct => InstallCoreAsync(archivePath, false, progress, ct), cancellationToken);
    /// <summary>Applies a new archive as one transaction. A preceding unconfirmed update must first be confirmed or rolled back.</summary>
    public Task<ModpackInstallResult> UpdateAsync(string archivePath, IProgress<ModpackProgress>? progress = null,
        CancellationToken cancellationToken = default) => ExecuteAsync(ct => InstallCoreAsync(archivePath, true, progress, ct), cancellationToken);
    /// <summary>Reads installed state and automatically recovers incomplete transactions. Does not make network calls.</summary>
    public Task<InstalledModpack?> GetInstalledAsync(CancellationToken cancellationToken = default) => ExecuteAsync(async ct =>
        (await files.ReadAsync(ct).ConfigureAwait(false))?.Installed, cancellationToken, readOnly: true);
    /// <summary>Creates the standard launcher from freshly read instance settings for manual validation. Inspect logs/exit; call ConfirmUpdateAsync only after explicit good-use confirmation.</summary>
    public Task<MinecraftLauncher> CreateValidationLauncherAsync(LaunchIdentity account, CancellationToken cancellationToken = default) => ExecuteAsync(async ct =>
    {
        ArgumentNullException.ThrowIfNull(account);
        if (await files.ReadAsync(ct).ConfigureAwait(false) == null) throw PackFiles.Conflict();
        var instance = await new InstanceManager(root).GetAsync(id, ct).ConfigureAwait(false);
        return new MinecraftLauncher(instance.CreateLaunchConfig(account));
    }, cancellationToken);
    /// <summary>Parses a replacement pack, resolves exact platform references and reports additions/removals/replacements/conflicts without downloading file bodies.</summary>
    public Task<ModpackUpdatePlan> GetUpdatePlanAsync(string archivePath, CancellationToken cancellationToken = default) => ExecuteAsync(async ct =>
    {
        using var archive = await PackArchive.OpenAsync(archivePath, ct).ConfigureAwait(false);
        var target = await ResolveAsync(archive.Info, ct).ConfigureAwait(false);
        var before = await files.ReadAsync(ct).ConfigureAwait(false);
        return (await PlanAsync(before, target, ct).ConfigureAwait(false)).Plan;
    }, cancellationToken, readOnly: true);
    /// <summary>Explicitly confirms good use of the current update, then optionally cleans only its old backups. Process creation never calls this automatically.</summary>
    public Task<ModpackCleanupResult> ConfirmUpdateAsync(Guid updateId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(ct => files.ConfirmAsync(updateId, ct), cancellationToken);
    /// <summary>Cleans only a previously confirmed update; failures are returned per safe relative backup path.</summary>
    public Task<ModpackCleanupResult> CleanupAsync(Guid updateId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(ct => files.CleanupAsync(updateId, ct), cancellationToken);
    /// <summary>Restores the latest uncleaned update, its platform records and instance configuration. User-edited targets cause a conflict, never forced deletion.</summary>
    public Task RollbackAsync(Guid updateId, CancellationToken cancellationToken = default) => ExecuteAsync(async ct =>
    { await files.RollbackAsync(updateId, ct).ConfigureAwait(false); return true; }, cancellationToken);

    private async Task<ModpackInstallResult> InstallCoreAsync(string path, bool updating, IProgress<ModpackProgress>? progress, CancellationToken ct)
    {
        progress?.Report(new(ModpackStage.Reading));
        using var archive = await PackArchive.OpenAsync(path, ct).ConfigureAwait(false);
        var before = await files.ReadAsync(ct).ConfigureAwait(false);
        if (updating && before == null) throw new ModpackException(ModpackErrorCode.NotFound, "此实例尚未安装整合包。");
        if (before?.Installed.PendingUpdateId != null) throw Pending();
        progress?.Report(new(ModpackStage.Resolving));
        var target = await ResolveAsync(archive.Info, ct).ConfigureAwait(false);
        if (before != null && !updating)
        {
            if (!Equivalent(before, target) || before.Installed.IsModified) throw PackFiles.Conflict();
            var check = await PlanAsync(before, target, ct).ConfigureAwait(false);
            if (check.Plan.Changes.Count != 0) throw PackFiles.Conflict();
            return new(before.Installed, Guid.Empty, true);
        }
        var (plan, selected) = await PlanAsync(before, target, ct).ConfigureAwait(false);
        if (!plan.CanApply) throw PackFiles.Conflict();
        var operation = Guid.NewGuid();
        // Entirely prepare the shared runtime before changing the isolated instance. Shared cache files are reusable on failure.
        var versionId = await PrepareRuntimeAsync(target.Info, operation, progress, ct).ConfigureAwait(false);
        await StageAsync(operation, selected, archive, progress, ct).ConfigureAwait(false);
        var materialized = new List<PackOwnedFile>();
        foreach (var item in selected)
        {
            var stamp = await ResourceFiles.StampAsync(files.New(operation, item.File.Path), ct).ConfigureAwait(false) ?? throw PackFiles.Conflict();
            if (item.File.Sha512 != null && stamp.Sha512 != item.File.Sha512) throw PackFiles.Conflict();
            var mod = item.Mod == null ? null : item.Mod with { Sha512 = stamp.Sha512, Size = stamp.Size };
            materialized.Add(item with { File = item.File with { Sha512 = stamp.Sha512, Size = stamp.Size,
                Downloads = target.Info.Format == ModpackFormat.CurseForge ? [] : item.File.Downloads }, Mod = mod });
        }
        var packInfo = target.Info with { Files = Array.AsReadOnly(materialized.Select(f => f.File).ToArray()) };
        var installed = new InstalledModpack(Guid.NewGuid(), packInfo, options.SourceId ?? before?.Installed.SourceId,
            DateTimeOffset.UtcNow, operation, options.InstallRuntime, false);
        var after = new PackState(1, installed, materialized.AsReadOnly(), options.AutoCleanupAfterConfirmation);
        var entries = await DeltaAsync(before, after, operation, ct).ConfigureAwait(false);
        await StageInstanceAsync(versionId, target.Info.Name, operation, entries, ct).ConfigureAwait(false);
        await StageReceiptsAsync(before, after, operation, entries, ct).ConfigureAwait(false);
        await files.CommitAsync(before, after, operation, entries, progress, ct).ConfigureAwait(false);
        progress?.Report(new(ModpackStage.Completed));
        return new(installed, operation, false);
    }
    private sealed record ResolvedPack(ModpackInfo Info, IReadOnlyList<PackOwnedFile> Files, IReadOnlyList<CurseForgeFile> CurseForgeFiles);
    private async Task<ResolvedPack> ResolveAsync(ModpackInfo info, CancellationToken ct)
    {
        var result = new List<PackOwnedFile>();
        var cfFiles = new List<CurseForgeFile>();
        var warnings = new List<string>();
        foreach (var reference in info.Files.Where(f => !f.Optional || options.IncludeOptionalFiles))
        {
            ct.ThrowIfCancellationRequested();
            _ = files.At(reference.Path);
            if (reference.IsOverride || !reference.Path.StartsWith("mods/", StringComparison.Ordinal)) { result.Add(new(reference)); continue; }
            if (info.Loader == null) throw PackArchive.Unsupported();
            var environment = new ModEnvironment(info.MinecraftVersion, info.Loader) { IncludePrereleases = true };
            if (info.Format == ModpackFormat.CurseForge)
            {
                var client = options.CurseForge ?? throw new ModpackException(ModpackErrorCode.InvalidRequest, "CurseForge 整合包需要调用方提供带合法 API Key 的 CurseForgeClient。");
                var file = await client.GetFileAsync(reference.ProjectId!.Value, reference.FileId!.Value, ct).ConfigureAwait(false);
                CurseForgeMetadata.Downloadable(await client.GetProjectAsync(reference.ProjectId.Value, ct).ConfigureAwait(false), file);
                if (!CurseForgeMetadata.Compatible(file, environment)) Compatibility(reference.Path);
                var pinned = reference with { Path = PackArchive.ContentPath("mods/" + file.FileName), Size = file.Size, Sha1 = file.Sha1, Downloads = [file.DownloadUrl!] };
                cfFiles.Add(file);
                result.Add(new(pinned, new InstalledMod(Number(file.ModId), Number(file.Id), file.DisplayName, file.PublishedAt,
                    file.FileName, "", file.Size, DateTimeOffset.UtcNow, CurseForgeMetadata.Dependencies(file)) { Source = "curseforge" }));
            }
            else
            {
                var version = await modrinth.GetVersionByHashAsync(reference.Sha512!, ct).ConfigureAwait(false);
                if (version == null) { result.Add(new(reference)); continue; }
                var artifact = version.Files.FirstOrDefault(f => f.Sha512 == reference.Sha512 && f.Size == reference.Size && f.FileType == null);
                if (artifact == null || artifact.Sha1 != reference.Sha1 ||
                    (await modrinth.GetProjectAsync(version.ProjectId, ct).ConfigureAwait(false)).ProjectType != "mod") throw PackArchive.Invalid();
                if (!ModrinthMetadata.Compatible(version, environment)) Compatibility(reference.Path);
                result.Add(new(reference, new(version.ProjectId, version.Id, version.VersionNumber, version.PublishedAt,
                    Path.GetFileName(reference.Path), reference.Sha512!, reference.Size, DateTimeOffset.UtcNow, version.Dependencies)));
            }
        }
        if (result.Select(f => f.File.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != result.Count ||
            result.Where(f => f.Mod != null).Select(f => f.Mod!.Source + ":" + f.Mod.ProjectId).Distinct(StringComparer.Ordinal).Count() != result.Count(f => f.Mod != null)) throw PackFiles.Conflict();
        foreach (var source in new[] { "modrinth", "curseforge" })
            PackRelations.Normalize(result.Where(f => f.Mod?.Source == source).Select(f => f.Mod!).ToArray(), options.RequireDeclaredModCompatibility, warnings); // Never resolve a missing dependency to Latest.
        return new(info with { Files = Array.AsReadOnly(result.Select(f => f.File).ToArray()), CompatibilityWarnings = warnings.AsReadOnly() }, result.AsReadOnly(), cfFiles.AsReadOnly());
        void Compatibility(string path)
        {
            if (options.RequireDeclaredModCompatibility) throw new ModpackException(ModpackErrorCode.DependencyConflict, "固定 Mod 的平台兼容性标签未声明整合包目标；严格模式拒绝安装，未改选其他版本。");
            warnings.Add(path);
        }
    }
    private async Task<(ModpackUpdatePlan Plan, IReadOnlyList<PackOwnedFile> Files)> PlanAsync(PackState? before, ResolvedPack target, CancellationToken ct)
    {
        var changes = new List<ModpackChange>(); var selected = new List<PackOwnedFile>();
        foreach (var item in target.Files)
        {
            var old = before?.Files.FirstOrDefault(f => string.Equals(f.File.Path, item.File.Path, StringComparison.OrdinalIgnoreCase));
            var actual = await files.StampAsync(item.File.Path, ct).ConfigureAwait(false);
            if (actual != null && (old == null || actual != PackFiles.Stamp(old.File)))
            {
                if (IsConfiguration(item.File.Path) && options.PreserveModifiedConfigurations)
                { changes.Add(new(item.File.Path, ModpackChangeKind.Preserve, "保留用户配置；不再以整合包文件所有权覆盖它。")); continue; }
                changes.Add(new(item.File.Path, ModpackChangeKind.Conflict, "目标文件不属于此整合包，或已被用户修改。"));
            }
            else if (old != null && actual == null) changes.Add(new(item.File.Path, ModpackChangeKind.Conflict, "原托管文件已被外部删除，请先恢复或处理冲突。"));
            else if (old == null) changes.Add(new(item.File.Path, ModpackChangeKind.Add, "新增清单文件。"));
            else if (item.File.Sha512 != null && old.File.Sha512 != item.File.Sha512 || old.File.Sha1 != item.File.Sha1 || old.File.FileId != item.File.FileId)
                changes.Add(new(item.File.Path, ModpackChangeKind.Replace, "替换为清单指定的固定版本。"));
            selected.Add(item);
        }
        foreach (var old in before?.Files ?? [])
        {
            if (target.Files.Any(f => string.Equals(f.File.Path, old.File.Path, StringComparison.OrdinalIgnoreCase))) continue;
            var actual = await files.StampAsync(old.File.Path, ct).ConfigureAwait(false);
            if (actual != PackFiles.Stamp(old.File))
                changes.Add(new(old.File.Path, IsConfiguration(old.File.Path) && options.PreserveModifiedConfigurations ? ModpackChangeKind.Preserve : ModpackChangeKind.Conflict, "旧文件存在用户修改；保留原文件。"));
            else changes.Add(new(old.File.Path, ModpackChangeKind.Delete, "移出活动目录，保留到本次更新备份。"));
        }
        // Existing T11 ownership cannot be silently adopted by a pack, even when hashes happen to match.
        foreach (var source in new[] { "modrinth", "curseforge" })
        {
            var receiptPath = ".fmcore/resources/" + source + ".json";
            if (!File.Exists(files.At(receiptPath))) continue;
            var receipt = await new ResourceFiles(GameDirectory, source).ReadAsync(null, ct).ConfigureAwait(false);
            foreach (var owned in receipt.Mods)
                if (before?.Files.Any(f => f.Mod?.Source == source && f.Mod.ProjectId == owned.ProjectId) != true &&
                    selected.Any(f => f.Mod?.Source == source && f.Mod.ProjectId == owned.ProjectId))
                    changes.Add(new("mods/" + owned.FileName, ModpackChangeKind.Conflict, "已有独立 Mod 的来源记录；不能自动接管。"));
        }
        if (target.Info.Loader != null && !changes.Any(c => c.Kind == ModpackChangeKind.Conflict))
        {
            var environment = new ModEnvironment(target.Info.MinecraftVersion, target.Info.Loader) { IncludePrereleases = true };
            var names = (before?.Files ?? []).Where(f => f.File.Path.StartsWith("mods/", StringComparison.OrdinalIgnoreCase))
                .Select(f => Path.GetFileName(f.File.Path)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var oldMods = (before?.Files ?? []).Where(f => f.Mod != null).Select(f => f.Mod!).ToArray();
            var nextMods = selected.Where(f => f.Mod != null).Select(f => f.Mod!).ToArray();
            try
            {
                if (nextMods.Any(m => m.Source == "modrinth"))
                    await new ModrinthInstaller(root, id, environment, modrinth, options.Downloads).CheckUnmanagedAsync(oldMods,
                        nextMods.Where(m => m.Source == "modrinth").ToArray(), ct, names).ConfigureAwait(false);
                if (target.CurseForgeFiles.Count != 0)
                    await new CurseForgeInstaller(root, id, environment, options.CurseForge!, options.Downloads, http).CheckUnmanagedAsync(oldMods,
                        nextMods.Where(m => m.Source == "curseforge").ToArray(), target.CurseForgeFiles,
                        new CurseForgeResolution(options.CurseForge!, environment, []), ct, names).ConfigureAwait(false);
            }
            catch (ResourceException e) when (e.Code is ResourceErrorCode.Conflict or ResourceErrorCode.DependencyConflict)
            { changes.Add(new("mods", ModpackChangeKind.Conflict, "T11 检测到已有用户 Mod 与整合包项目重复或存在声明冲突。")); }
        }
        return (new(before?.Installed.Revision, target.Info, changes.AsReadOnly()), selected.AsReadOnly());
    }
    private async Task StageAsync(Guid operation, IReadOnlyList<PackOwnedFile> selected, PackArchive archive, IProgress<ModpackProgress>? progress, CancellationToken ct)
    {
        var downloads = new DownloadManager(http, GameDirectory, options.Downloads);
        progress?.Report(new(ModpackStage.Downloading, 0, selected.Count));
        foreach (var item in selected)
        {
            ct.ThrowIfCancellationRequested();
            var file = item.File; var destination = files.New(operation, file.Path);
            if (file.IsOverride)
            {
                await GameFiles.WriteAtomicAsync(destination, async (output, token) =>
                { await using var input = new ArchiveEntryStream(archive.Overrides[file.Path]); await input.CopyToAsync(output, token).ConfigureAwait(false); }, ct).ConfigureAwait(false);
            }
            else if (file.Sha512 != null && await files.MatchesAsync(files.At(file.Path), PackFiles.Stamp(file), ct).ConfigureAwait(false))
                await ResourceFiles.CopyAsync(files.At(file.Path), destination, ct).ConfigureAwait(false);
            else
            {
                DownloadException? last = null;
                var cached = PackFiles.Cache(file);
                foreach (var uri in file.Downloads)
                {
                    try
                    {
                        await downloads.DownloadAsync(new(uri, cached)
                        { ExpectedHash = file.Sha512 ?? file.Sha1, ExpectedSize = file.Size, HashAlgorithm = file.Sha512 == null ? DownloadHashAlgorithm.Sha1 : DownloadHashAlgorithm.Sha512 },
                            new InlineProgress<DownloadProgress>(d => progress?.Report(new(ModpackStage.Downloading, Download: d))), ct).ConfigureAwait(false);
                        last = null; break;
                    }
                    catch (DownloadException error) { last = error; }
                }
                if (last != null) throw last;
                await ResourceFiles.CopyAsync(files.At(cached), destination, ct).ConfigureAwait(false);
            }
            // mrpack requires both hashes. Downloader's stronger hash alone does not validate contradictory SHA-1 metadata.
            if (file.Sha1 != null && !await GameFiles.HasSha1Async(destination, file.Sha1, file.Size, ct).ConfigureAwait(false))
                throw new ModpackException(ModpackErrorCode.DownloadFailed, "整合包文件 SHA-1 与清单不符。");
        }
    }
    private async Task<string> PrepareRuntimeAsync(ModpackInfo pack, Guid operation, IProgress<ModpackProgress>? progress, CancellationToken ct)
    {
        var profile = pack.Loader switch
        { "fabric" => $"fabric-loader-{pack.LoaderVersion}-{pack.MinecraftVersion}", "forge" => $"{pack.MinecraftVersion}-forge-{pack.LoaderVersion}",
            "neoforge" => $"{pack.MinecraftVersion}-neoforge-{pack.LoaderVersion}", null => pack.MinecraftVersion, _ => throw PackArchive.Unsupported() };
        if (!options.InstallRuntime) return profile;
        progress?.Report(new(ModpackStage.Runtime));
        var downloads = new DownloadManager(http, root, options.Downloads);
        // Installer layouts and natives are staged outside the live instance; launcher materializes its normal per-instance layouts later.
        var install = new InstallationOptions { GameDirectory = ".cache/modpacks/runtime/" + operation.ToString("N") };
        var common = new InlineProgress<InstallationProgress>(p => progress?.Report(new(ModpackStage.Runtime, Installation: p)));
        return pack.Loader switch
        {
            null => (await new GameInstaller(downloads).InstallAsync(pack.MinecraftVersion, install, common, ct).ConfigureAwait(false)).Plan.Version.Metadata.Id,
            "fabric" => (await new FabricInstaller(downloads).InstallAsync(pack.MinecraftVersion, pack.LoaderVersion,
                new() { Installation = install }, new InlineProgress<FabricInstallationProgress>(p => progress?.Report(new(ModpackStage.Runtime, Installation: p.Installation, Download: p.Download))), ct).ConfigureAwait(false)).VersionId,
            "forge" => (await new ForgeInstaller(downloads).InstallAsync(pack.MinecraftVersion, pack.LoaderVersion,
                new() { Installation = install, JavaPath = options.JavaPath }, new InlineProgress<LoaderInstallationProgress>(p => progress?.Report(new(ModpackStage.Runtime, Installation: p.Installation, Download: p.Download))), ct).ConfigureAwait(false)).VersionId,
            "neoforge" => (await new NeoForgeInstaller(downloads).InstallAsync(pack.MinecraftVersion, pack.LoaderVersion,
                new() { Installation = install, JavaPath = options.JavaPath }, new InlineProgress<LoaderInstallationProgress>(p => progress?.Report(new(ModpackStage.Runtime, Installation: p.Installation, Download: p.Download))), ct).ConfigureAwait(false)).VersionId,
            _ => throw PackArchive.Unsupported()
        };
    }
    private async Task<List<PackDelta>> DeltaAsync(PackState? before, PackState after, Guid operation, CancellationToken ct)
    {
        var entries = new List<PackDelta>();
        foreach (var path in (before?.Files.Select(f => f.File.Path) ?? []).Concat(after.Files.Select(f => f.File.Path)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var old = before?.Files.FirstOrDefault(f => string.Equals(f.File.Path, path, StringComparison.OrdinalIgnoreCase));
            var next = after.Files.FirstOrDefault(f => string.Equals(f.File.Path, path, StringComparison.OrdinalIgnoreCase));
            var current = await files.StampAsync(path, ct).ConfigureAwait(false);
            if (old != null && current != PackFiles.Stamp(old.File))
            {
                if (IsConfiguration(path) && options.PreserveModifiedConfigurations && next == null) continue;
                throw PackFiles.Conflict();
            }
            var desired = next == null ? null : PackFiles.Stamp(next.File);
            if (current != desired) entries.Add(new(path, current, desired));
        }
        return entries;
    }
    private async Task StageInstanceAsync(string versionId, string name, Guid operation, List<PackDelta> entries, CancellationToken ct)
    {
        InstanceDocument document;
        if (File.Exists(files.At(InstanceFiles.Metadata)))
        {
            document = await InstanceFiles.ReadAsync(GameDirectory, id, ct).ConfigureAwait(false);
            if (document.Config.VersionId == versionId) return;
            document = document with { Revision = Guid.NewGuid(), UpdatedAt = DateTimeOffset.UtcNow, Config = document.Config with { VersionId = versionId } };
        }
        else document = new(1, id, Guid.NewGuid(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            InstanceFiles.Freeze(new() { Name = name.Length > 128 ? name[..128] : name, VersionId = versionId, JavaPath = options.JavaPath }));
        await StageControlAsync(InstanceFiles.Metadata, document, operation, entries, ct).ConfigureAwait(false);
    }
    private async Task StageReceiptsAsync(PackState? before, PackState after, Guid operation, List<PackDelta> entries, CancellationToken ct)
    {
        foreach (var source in new[] { "modrinth", "curseforge" })
        {
            var path = ".fmcore/resources/" + source + ".json";
            var previous = File.Exists(files.At(path)) ? await new ResourceFiles(GameDirectory, source).ReadAsync(null, ct).ConfigureAwait(false) : null;
            var unmanaged = previous?.Mods.Where(m => before?.Files.Any(f => f.Mod?.Source == source && f.Mod.ProjectId == m.ProjectId) != true).ToArray() ?? [];
            if (unmanaged.Length > 0 && previous!.Environment != Environment(after)) throw PackFiles.Conflict();
            var mods = unmanaged.Concat(after.Files.Where(f => f.Mod?.Source == source).Select(f => f.Mod!)).ToArray();
            if (mods.Length == 0 && previous == null) continue;
            if (mods.Length > 512) throw PackArchive.Limit();
            await StageControlAsync(path, new ModReceipt(1, Guid.NewGuid(), Environment(after), PackRelations.Normalize(mods, options.RequireDeclaredModCompatibility)), operation, entries, ct).ConfigureAwait(false);
        }
    }
    private async Task StageControlAsync<T>(string path, T value, Guid operation, List<PackDelta> entries, CancellationToken ct)
    {
        await files.WriteBytesAsync(files.New(operation, path), PackFiles.Bytes(value), ct).ConfigureAwait(false);
        var next = await ResourceFiles.StampAsync(files.New(operation, path), ct).ConfigureAwait(false);
        var old = await files.StampAsync(path, ct).ConfigureAwait(false);
        if (old != next) entries.Add(new(path, old, next));
    }
    private async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken ct, bool readOnly = false)
    {
        ct.ThrowIfCancellationRequested();
        try
        {
            InstanceFiles.EnsureCurrentLayout(root, id);
            if (readOnly && !Directory.Exists(GameFiles.Resolve(GameDirectory, ".fmcore/modpacks")) &&
                !Directory.Exists(GameFiles.Resolve(GameDirectory, ".fmcore/resources")) && !InstanceLock.IsInUse(root, InstanceFiles.Relative(id)))
                return await action(ct).ConfigureAwait(false);
            using var lease = InstanceLock.Acquire(root, InstanceFiles.Relative(id));
            using var legacy = InstanceLock.AcquireLegacy(GameDirectory);
            await files.RecoverAsync(ct).ConfigureAwait(false);
            foreach (var source in new[] { "modrinth", "curseforge" }) await new ResourceFiles(GameDirectory, source).RecoverAsync(ct).ConfigureAwait(false);
            return await action(ct).ConfigureAwait(false);
        }
        catch (ResourceException e) { throw new ModpackException(e.Code switch
        { ResourceErrorCode.DownloadRestricted => ModpackErrorCode.DownloadRestricted, ResourceErrorCode.DependencyConflict => ModpackErrorCode.DependencyConflict,
            ResourceErrorCode.Conflict => ModpackErrorCode.Conflict, ResourceErrorCode.ModpackLocked => ModpackErrorCode.UpdateLocked,
            ResourceErrorCode.Busy => ModpackErrorCode.Busy, ResourceErrorCode.RecoveryRequired => ModpackErrorCode.RecoveryRequired,
            ResourceErrorCode.UnsafePath => ModpackErrorCode.UnsafePath, _ => ModpackErrorCode.DownloadFailed }, "资源解析、下载或更新失败；整合包事务和旧文件已保留。") { ResourceError = e.Code }; }
        catch (DownloadException e) { throw new ModpackException(e.Code == DownloadErrorCode.UnsafePath ? ModpackErrorCode.UnsafePath : ModpackErrorCode.DownloadFailed,
            "整合包下载或路径校验失败，未提交的活动文件保持原状。") { DownloadError = e.Code }; }
        catch (InstanceException e) { throw new ModpackException(e.Code == InstanceErrorCode.Busy ? ModpackErrorCode.Busy : ModpackErrorCode.Conflict, e.Message); }
        catch (Exception e) when (e is FabricException or LoaderException or InstallationException or FMCore.Versions.VersionException or FMCore.Java.JavaException)
        { throw new ModpackException(ModpackErrorCode.RuntimeFailed, "整合包要求的准确游戏或加载器未能完成安装；活动实例未改变。"); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { throw new ModpackException(ModpackErrorCode.IoError, "整合包文件读写失败；未确认更新的备份和恢复记录已保留。"); }
    }
    private static bool IsConfiguration(string path) => path.StartsWith("config/", StringComparison.OrdinalIgnoreCase) || path.Equals("options.txt", StringComparison.OrdinalIgnoreCase);
    private static bool Equivalent(PackState before, ResolvedPack target) => before.Installed.Pack.Format == target.Info.Format && before.Installed.Pack.Version == target.Info.Version &&
        before.Installed.Pack.MinecraftVersion == target.Info.MinecraftVersion && before.Installed.Pack.Loader == target.Info.Loader && before.Installed.Pack.LoaderVersion == target.Info.LoaderVersion &&
        before.Files.Count == target.Files.Count && before.Files.All(old => target.Files.Any(next => next.File.Path == old.File.Path && next.File.Sha1 == old.File.Sha1 &&
            (next.File.Sha512 == null || next.File.Sha512 == old.File.Sha512) && next.File.FileId == old.File.FileId));
    private static ModEnvironment Environment(PackState state) => new(state.Installed.Pack.MinecraftVersion, state.Installed.Pack.Loader ?? "fabric") { IncludePrereleases = true };
    private static string Number(int id) => id.ToString(CultureInfo.InvariantCulture);
    private static ModpackException Invalid() => new(ModpackErrorCode.InvalidRequest, "整合包实例、来源标识符或下载连接配置无效。");
    private static ModpackException Pending() => new(ModpackErrorCode.ConfirmationRequired, "上次更新尚未确认；请先确认正常使用或回滚，再进行下一次修改。");
    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T> { public void Report(T value) => report(value); }
}
