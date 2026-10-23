using System.Text.Json;
using System.Text.Json.Serialization;
using FMCore.Downloader;
using FMCore.Instances;
using FMCore.Resources;
using FMCore.Utilities;

namespace FMCore.Modpacks;

internal sealed record PackOwnedFile(ModpackFile File, InstalledMod? Mod = null);
internal sealed record PackState(int SchemaVersion, InstalledModpack Installed, IReadOnlyList<PackOwnedFile> Files, bool AutoCleanup);
internal sealed record ContentStamp(string Sha512, long Size);
internal sealed record PackDelta(string Path, ContentStamp? Before, ContentStamp? After);
internal sealed record PackTransaction(int SchemaVersion, Guid Id, string Phase, PackState? Before, PackState After, IReadOnlyList<PackDelta> Files);

// One journal covers pack files, platform receipts and instance settings. The state revision is the durable commit point.
internal sealed class PackFiles(string directory)
{
    internal const string StatePath = ".fmcore/modpacks/state.json";
    private const string Updates = ".fmcore/modpacks/updates";
    internal static readonly JsonSerializerOptions Json = new()
    { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, MaxDepth = 32, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
    internal string At(string relative)
    {
        var path = GameFiles.Resolve(directory, relative);
        if (InstanceFiles.IsVersionRuntimePath(directory, path)) throw PackArchive.Unsafe();
        return path;
    }
    internal static string Folder(Guid id) => Updates + "/" + id.ToString("N");
    internal static string Cache(ModpackFile file) => ".fmcore/modpacks/cache/" + (file.Sha1 ?? file.Sha512 ?? throw Corrupt()) + ".bin";
    internal string New(Guid id, string relative) => At(Folder(id) + "/new/" + relative);
    private string Old(Guid id, string relative) => At(Folder(id) + "/old/" + relative);
    private string Journal(Guid id) => At(Folder(id) + "/journal.json");
    internal static byte[] Bytes<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Json);
    internal Task WriteBytesAsync(string path, byte[] bytes, CancellationToken ct) => GameFiles.WriteAtomicAsync(path,
        (output, token) => output.WriteAsync(bytes, token).AsTask(), ct);
    internal async Task<PackState?> ReadAsync(CancellationToken ct)
    {
        var path = At(StatePath);
        if (!File.Exists(path)) return null;
        var state = await ReadJsonAsync<PackState>(path, ct).ConfigureAwait(false);
        ValidateState(state); return state;
    }
    internal async Task<T> ReadJsonAsync<T>(string path, CancellationToken ct)
    {
        try { return JsonSerializer.Deserialize<T>(await GameFiles.ReadJsonAsync(path, ct).ConfigureAwait(false), Json) ?? throw Corrupt(); }
        catch (Exception e) when (e is JsonException or NotSupportedException or InvalidDataException) { throw Corrupt(); }
    }
    internal async Task<ContentStamp?> StampAsync(string relative, CancellationToken ct)
    {
        var path = At(relative);
        if (Directory.Exists(path)) throw Conflict();
        return await ResourceFiles.StampAsync(path, ct).ConfigureAwait(false);
    }
    internal Task<bool> MatchesAsync(string path, ContentStamp stamp, CancellationToken ct) => ResourceFiles.MatchesAsync(path, stamp.Sha512, stamp.Size, ct);
    internal static ContentStamp Stamp(ModpackFile file) => new(file.Sha512 ?? throw Corrupt(), file.Size);
    internal async Task CommitAsync(PackState? before, PackState after, Guid id, IReadOnlyList<PackDelta> entries,
        IProgress<ModpackProgress>? progress, CancellationToken ct)
    {
        var transaction = new PackTransaction(1, id, "prepared", before, after, entries);
        Validate(transaction);
        if ((await ReadAsync(ct).ConfigureAwait(false))?.Installed.Revision != before?.Installed.Revision) throw Conflict();
        if (File.Exists(Journal(id))) throw Conflict();
        progress?.Report(new(ModpackStage.BackingUp, 0, entries.Count));
        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();
            if (await StampAsync(entry.Path, ct).ConfigureAwait(false) != entry.Before) throw Conflict();
            if (entry.After != null && !await MatchesAsync(New(id, entry.Path), entry.After, ct).ConfigureAwait(false)) throw Conflict();
            if (entry.Before != null)
            {
                await ResourceFiles.CopyAsync(At(entry.Path), Old(id, entry.Path), ct).ConfigureAwait(false);
                if (!await MatchesAsync(Old(id, entry.Path), entry.Before, ct).ConfigureAwait(false)) throw Conflict();
            }
        }
        await WriteBytesAsync(Journal(id), Bytes(transaction), ct).ConfigureAwait(false);
        try
        {
            // Remove the complete old set before introducing any new Mod; no mixed-version loading window under the SDK lock.
            foreach (var entry in entries)
            {
                ct.ThrowIfCancellationRequested();
                if (await StampAsync(entry.Path, ct).ConfigureAwait(false) != entry.Before) throw Conflict();
                if (entry.Before != null) File.Delete(At(entry.Path));
            }
            var count = 0;
            progress?.Report(new(ModpackStage.Committing, count, entries.Count));
            foreach (var entry in entries)
            {
                ct.ThrowIfCancellationRequested();
                if (entry.After != null)
                {
                    var target = At(entry.Path); GameFiles.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Move(New(id, entry.Path), target); // Never overwrite concurrent external files.
                }
                progress?.Report(new(ModpackStage.Committing, ++count, entries.Count));
            }
            foreach (var entry in entries)
                if (await StampAsync(entry.Path, ct).ConfigureAwait(false) != entry.After) throw Conflict();
            foreach (var owned in after.Files)
                if (!await MatchesAsync(At(owned.File.Path), Stamp(owned.File), ct).ConfigureAwait(false)) throw Conflict();
            ct.ThrowIfCancellationRequested();
            await WriteBytesAsync(At(StatePath), Bytes(after), ct).ConfigureAwait(false);
        }
        catch
        {
            await RecoverAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        // The revision is already committed. A journal write failure is recoverable and must not imply a failed installation.
        try { await WriteBytesAsync(Journal(id), Bytes(transaction with { Phase = "committed" }), CancellationToken.None).ConfigureAwait(false); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { }
    }
    internal async Task RecoverAsync(CancellationToken ct)
    {
        var root = At(Updates); if (!Directory.Exists(root)) return;
        var folders = Directory.EnumerateFileSystemEntries(root).Take(1001).ToArray();
        if (folders.Length > 1000) throw Corrupt();
        foreach (var path in folders)
        {
            ct.ThrowIfCancellationRequested(); DownloadPath.CheckLinks(path);
            if (!Directory.Exists(path) || !Guid.TryParseExact(Path.GetFileName(path), "N", out var id)) throw Corrupt();
            if (!File.Exists(Journal(id))) continue; // Download/staging interrupted before any active file was touched.
            var journal = await GetTransactionAsync(id, ct).ConfigureAwait(false);
            var state = await ReadAsync(ct).ConfigureAwait(false);
            if (journal.Phase == "prepared")
            {
                if (state?.Installed.Revision == journal.After.Installed.Revision)
                    await SaveTransactionAsync(journal with { Phase = "committed" }, ct).ConfigureAwait(false);
                else
                {
                    if (state?.Installed.Revision != journal.Before?.Installed.Revision) throw Recovery();
                    await RestoreAsync(journal, ct).ConfigureAwait(false);
                }
            }
            else if (journal.Phase == "rollingBack")
            {
                if (state?.Installed.Revision != journal.After.Installed.Revision && state?.Installed.Revision != journal.Before?.Installed.Revision) throw Recovery();
                await RestoreAsync(journal, ct).ConfigureAwait(false);
            }
            else if (journal.Phase == "confirmed" && state?.Installed.PendingUpdateId == id)
                await WriteBytesAsync(At(StatePath), Bytes(state with { Installed = state.Installed with { PendingUpdateId = null } }), ct).ConfigureAwait(false);
        }
    }
    internal async Task RollbackAsync(Guid id, CancellationToken ct)
    {
        var journal = await GetTransactionAsync(id, ct).ConfigureAwait(false);
        if (journal.Phase == "rolledBack") return;
        if (journal.Phase is "cleaned" or "prepared" || (await ReadAsync(ct).ConfigureAwait(false))?.Installed.Revision != journal.After.Installed.Revision) throw Conflict();
        await PreflightRestoreAsync(journal, true, ct).ConfigureAwait(false);
        await SaveTransactionAsync(journal with { Phase = "rollingBack" }, ct).ConfigureAwait(false);
        // Once rollback starts, finish despite caller cancellation; an I/O failure keeps the journal for the next operation.
        await RestoreAsync(journal, CancellationToken.None).ConfigureAwait(false);
    }
    private async Task PreflightRestoreAsync(PackTransaction journal, bool strictAfter, CancellationToken ct)
    {
        foreach (var entry in journal.Files)
        {
            if (entry.Before != null && !await MatchesAsync(Old(journal.Id, entry.Path), entry.Before, ct).ConfigureAwait(false)) throw Recovery();
            var active = await StampAsync(entry.Path, ct).ConfigureAwait(false);
            if (strictAfter ? active != entry.After : active != null && active != entry.Before && active != entry.After) throw Recovery();
        }
    }
    private async Task RestoreAsync(PackTransaction journal, CancellationToken ct)
    {
        await PreflightRestoreAsync(journal, false, ct).ConfigureAwait(false);
        foreach (var entry in journal.Files)
        {
            ct.ThrowIfCancellationRequested();
            var active = await StampAsync(entry.Path, ct).ConfigureAwait(false);
            if (active == entry.Before) continue;
            if (active != null) File.Delete(At(entry.Path));
            if (entry.Before == null) continue;
            // Restore through a temporary sibling; check again before non-overwriting move.
            var temporary = At(Folder(journal.Id) + "/restore/" + entry.Path);
            await ResourceFiles.CopyAsync(Old(journal.Id, entry.Path), temporary, ct).ConfigureAwait(false);
            var target = At(entry.Path); GameFiles.CreateDirectory(Path.GetDirectoryName(target)!); File.Move(temporary, target);
        }
        foreach (var entry in journal.Files)
            if (await StampAsync(entry.Path, ct).ConfigureAwait(false) != entry.Before) throw Recovery();
        if (journal.Before == null) File.Delete(At(StatePath));
        else await WriteBytesAsync(At(StatePath), Bytes(journal.Before), ct).ConfigureAwait(false);
        await SaveTransactionAsync(journal with { Phase = "rolledBack" }, ct).ConfigureAwait(false);
    }
    internal async Task<ModpackCleanupResult> ConfirmAsync(Guid id, CancellationToken ct)
    {
        var journal = await GetTransactionAsync(id, ct).ConfigureAwait(false);
        var current = await ReadAsync(ct).ConfigureAwait(false) ?? throw Conflict();
        if (current.Installed.Revision != journal.After.Installed.Revision || journal.Phase is not ("committed" or "confirmed" or "cleaned")) throw Conflict();
        foreach (var entry in journal.Files.Where(e => !IsConfiguration(e.Path)))
            if (await StampAsync(entry.Path, ct).ConfigureAwait(false) != entry.After) throw Conflict();
        if (journal.Phase != "cleaned") await SaveTransactionAsync(journal with { Phase = "confirmed" }, ct).ConfigureAwait(false);
        await WriteBytesAsync(At(StatePath), Bytes(current with { Installed = current.Installed with { PendingUpdateId = null } }), ct).ConfigureAwait(false);
        return current.AutoCleanup ? await CleanupAsync(id, ct).ConfigureAwait(false) : new(id, 0, []);
    }
    internal async Task<ModpackCleanupResult> CleanupAsync(Guid id, CancellationToken ct)
    {
        var journal = await GetTransactionAsync(id, ct).ConfigureAwait(false);
        if (journal.Phase == "cleaned") return new(id, 0, []);
        if (journal.Phase != "confirmed") throw new ModpackException(ModpackErrorCode.ConfirmationRequired, "必须明确确认本次更新正常使用后，才允许清理备份。");
        var failed = new List<string>(); var deleted = 0;
        foreach (var entry in journal.Files.Where(e => e.Before != null))
        {
            ct.ThrowIfCancellationRequested();
            var relative = Folder(id) + "/old/" + entry.Path;
            try
            {
                var path = At(relative);
                if (!File.Exists(path) && !Directory.Exists(path)) continue;
                if (!await MatchesAsync(path, entry.Before!, ct).ConfigureAwait(false)) { failed.Add(relative); continue; }
                File.Delete(path); deleted++;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or DownloadException) { failed.Add(relative); }
        }
        var current = await ReadAsync(ct).ConfigureAwait(false);
        foreach (var old in journal.Before?.Files ?? [])
        {
            if (old.File.IsOverride || current?.Files.Any(f => f.File.Sha512 == old.File.Sha512) == true) continue;
            var relative = Cache(old.File);
            try
            {
                var path = At(relative); if (!File.Exists(path) && !Directory.Exists(path)) continue;
                if (!await MatchesAsync(path, Stamp(old.File), ct).ConfigureAwait(false)) { failed.Add(relative); continue; }
                File.Delete(path); deleted++;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or DownloadException) { failed.Add(relative); }
        }
        if (failed.Count == 0) await SaveTransactionAsync(journal with { Phase = "cleaned" }, ct).ConfigureAwait(false);
        return new(id, deleted, failed.AsReadOnly());
    }
    internal async Task<PackTransaction> GetTransactionAsync(Guid id, CancellationToken ct)
    {
        if (id == Guid.Empty || !File.Exists(Journal(id))) throw new ModpackException(ModpackErrorCode.NotFound, "未找到此实例的更新记录。");
        var journal = await ReadJsonAsync<PackTransaction>(Journal(id), ct).ConfigureAwait(false);
        Validate(journal); if (journal.Id != id) throw Corrupt(); return journal;
    }
    private Task SaveTransactionAsync(PackTransaction value, CancellationToken ct) => WriteBytesAsync(Journal(value.Id), Bytes(value), ct);
    private static bool IsConfiguration(string path) => path.StartsWith("config/", StringComparison.OrdinalIgnoreCase) || path.Equals("options.txt", StringComparison.OrdinalIgnoreCase);
    private static void ValidateState(PackState value)
    {
        try
        {
            if (value.SchemaVersion != 1 || value.Installed.Revision == Guid.Empty || value.Files.Count > 5000 ||
                value.Installed.InstalledAt == default || !Enum.IsDefined(value.Installed.Pack.Format) ||
                value.Installed.PendingUpdateId == Guid.Empty) throw Corrupt();
            PackArchive.Token(value.Installed.Pack.MinecraftVersion);
            if (value.Installed.Pack.LoaderVersion != null) PackArchive.Token(value.Installed.Pack.LoaderVersion);
            if (value.Installed.Pack.Loader is not (null or "fabric" or "forge" or "neoforge")) throw Corrupt();
            foreach (var item in value.Files)
            {
                PackArchive.ContentPath(item.File.Path); PackArchive.Hash(item.File.Sha512!, 128);
                if (item.File.Size is < 0 or > 536870912) throw Corrupt();
                if (item.Mod != null && (item.File.Path != "mods/" + item.Mod.FileName || item.File.Sha512 != item.Mod.Sha512 || item.File.Size != item.Mod.Size || item.Mod.Source is not ("modrinth" or "curseforge"))) throw Corrupt();
            }
            if (value.Files.Select(f => f.File.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != value.Files.Count) throw Corrupt();
        }
        catch (Exception e) when (e is ArgumentException or NullReferenceException or ModpackException) { throw Corrupt(); }
    }
    private static void Validate(PackTransaction value)
    {
        try
        {
            if (value.SchemaVersion != 1 || value.Id == Guid.Empty || value.Phase is not ("prepared" or "committed" or "confirmed" or "cleaned" or "rollingBack" or "rolledBack") ||
                value.After.Installed.PendingUpdateId != value.Id || value.Before?.Installed.Revision == value.After.Installed.Revision || value.Files.Count > 5010) throw Corrupt();
            ValidateState(value.After); if (value.Before != null) ValidateState(value.Before);
            if (value.Files.Select(e => e.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count() != value.Files.Count) throw Corrupt();
            foreach (var file in value.Files)
            {
                if (file.Path is not (".fmcore/instance.json" or ".fmcore/resources/modrinth.json" or ".fmcore/resources/curseforge.json")) PackArchive.ContentPath(file.Path);
                foreach (var stamp in new[] { file.Before, file.After }.OfType<ContentStamp>())
                    if (stamp.Size is < 0 or > 536870912 || !ModrinthMetadata.Hash(stamp.Sha512, 128)) throw Corrupt();
                if (file.Before == file.After) throw Corrupt();
                // Journal paths must belong to either recorded pack or the narrowly allowed control documents.
                if (!file.Path.StartsWith(".fmcore/", StringComparison.Ordinal) &&
                    value.Before?.Files.Any(f => f.File.Path == file.Path && Stamp(f.File) == file.Before) != true &&
                    !value.After.Files.Any(f => f.File.Path == file.Path && Stamp(f.File) == file.After)) throw Corrupt();
            }
        }
        catch (Exception e) when (e is ArgumentException or NullReferenceException or ModpackException) { throw Corrupt(); }
    }
    internal static ModpackException Conflict() => new(ModpackErrorCode.Conflict, "实例、文件或更新记录已改变；未覆盖用户文件。");
    internal static ModpackException Corrupt() => new(ModpackErrorCode.StorageCorrupted, "整合包安装或事务记录无效；未修改活动文件。");
    private static ModpackException Recovery() => new(ModpackErrorCode.RecoveryRequired, "恢复所需文件缺失、损坏或存在用户修改；已保留事务和备份。");
}
