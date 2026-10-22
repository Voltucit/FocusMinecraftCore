using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using FMCore.Downloader;
using FMCore.Modpacks;
using FMCore.Utilities;

namespace FMCore.Resources;

internal sealed record ModReceipt(int SchemaVersion, Guid Revision, ModEnvironment Environment, IReadOnlyList<InstalledMod> Mods);
internal sealed record ModTransaction(int SchemaVersion, ModReceipt Before, ModReceipt After);

// No URLs or credentials in the receipt/journal. Content-addressed staging/backups are never scanned as installed mods.
internal sealed class ResourceFiles(string gameDirectory, string platform = "modrinth")
{
    internal PackResourceScope? PackScope { get; set; }
    private string ReceiptPath => $".fmcore/resources/{Platform}.json";
    private string Platform => platform is "modrinth" or "curseforge" ? platform : throw Corrupt();
    private string JournalPath => $".fmcore/resources/{Platform}-transaction.json";
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, MaxDepth = 24,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
    internal string At(string path) => GameFiles.Resolve(gameDirectory, path);
    internal string ModPath(InstalledMod mod) => At("mods/" + ModrinthMetadata.FileName(mod.FileName));
    internal static string Cache(string hash) => Cache(hash, "modrinth");
    internal static string Cache(string hash, string source) => $".fmcore/resources/{source}-cache/{hash}.jar";
    private string Backup(InstalledMod mod) => At($".fmcore/resources/{Platform}-backup/" + mod.Sha512 + ".jar");
    private string Staged(InstalledMod mod) => At($".fmcore/resources/{Platform}-staged/" + mod.Sha512 + "-" +
        Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(mod.ProjectId))) + ".jar");

    internal async Task<ModReceipt> ReadAsync(ModEnvironment? environment, CancellationToken ct)
    {
        ModReceipt result;
        if (!File.Exists(At(ReceiptPath)))
        {
            if (environment == null) throw Corrupt();
            return new(1, Guid.Empty, environment, []);
        }
        try { result = JsonSerializer.Deserialize<ModReceipt>(await GameFiles.ReadJsonAsync(At(ReceiptPath), ct).ConfigureAwait(false), Json) ?? throw Corrupt(); }
        catch (Exception e) when (e is JsonException or InvalidDataException or NotSupportedException) { throw Corrupt(); }
        Validate(result);
        if (environment != null && !SameTarget(result.Environment, environment)) throw Corrupt();
        return result;
    }

    internal async Task VerifyOwnedAsync(IEnumerable<InstalledMod> mods, CancellationToken ct)
    {
        foreach (var mod in mods)
            if (!await MatchesAsync(ModPath(mod), mod, ct).ConfigureAwait(false)) throw ModrinthMetadata.Conflict();
    }

    internal async Task CommitAsync(ModReceipt before, ModReceipt after, CancellationToken ct)
    {
        if (await PackResourceScope.CommitAsync(gameDirectory, Platform, before, after, ct, PackScope).ConfigureAwait(false)) return;
        var removed = Removed(before, after).ToArray();
        var added = Removed(after, before).ToArray();
        // All downloads and complete backups exist before the durable journal and the first active-file change.
        await VerifyOwnedAsync(before.Mods, ct).ConfigureAwait(false);
        foreach (var mod in added)
        {
            var target = ModPath(mod);
            var old = removed.FirstOrDefault(m => GameFiles.PathComparer.Equals(m.FileName, mod.FileName));
            if ((File.Exists(target) || Directory.Exists(target)) && old == null) throw ModrinthMetadata.Conflict();
            var cached = At(Cache(mod.Sha512, Platform));
            if (!await MatchesAsync(cached, mod, ct).ConfigureAwait(false)) throw ModrinthMetadata.Conflict();
            await CopyAsync(cached, Staged(mod), ct).ConfigureAwait(false);
            if (!await MatchesAsync(Staged(mod), mod, ct).ConfigureAwait(false)) throw ModrinthMetadata.Conflict();
        }
        foreach (var mod in removed)
        {
            await CopyAsync(ModPath(mod), Backup(mod), ct).ConfigureAwait(false);
            if (!await MatchesAsync(Backup(mod), mod, ct).ConfigureAwait(false)) throw ModrinthMetadata.Conflict();
        }
        ct.ThrowIfCancellationRequested();
        await WriteAsync(JournalPath, new ModTransaction(1, before, after), ct).ConfigureAwait(false);
        try
        {
            await VerifyOwnedAsync(before.Mods, ct).ConfigureAwait(false);
            foreach (var mod in removed)
            {
                ct.ThrowIfCancellationRequested();
                if (!await MatchesAsync(ModPath(mod), mod, ct).ConfigureAwait(false)) throw ModrinthMetadata.Conflict();
                File.Delete(ModPath(mod));
            }
            GameFiles.CreateDirectory(At("mods"));
            foreach (var mod in added)
            {
                ct.ThrowIfCancellationRequested();
                File.Move(Staged(mod), ModPath(mod)); // Never overwrite an unknown file, including a concurrent external writer.
            }
            await VerifyOwnedAsync(after.Mods, ct).ConfigureAwait(false);
            await WriteAsync(ReceiptPath, after, ct).ConfigureAwait(false); // Commit point.
        }
        catch
        {
            // Ignore cancellation while restoring old files. A blocked recovery keeps journal/backups for the next operation.
            await RecoverAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        // A cleanup failure after the commit point does not turn a successful update into a reported failure.
        try { File.Delete(At(JournalPath)); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }

    internal async Task RecoverAsync(CancellationToken ct)
    {
        var path = At(JournalPath);
        if (!File.Exists(path)) return;
        ModTransaction transaction;
        try { transaction = JsonSerializer.Deserialize<ModTransaction>(await GameFiles.ReadJsonAsync(path, ct).ConfigureAwait(false), Json) ?? throw Corrupt(); }
        catch (Exception e) when (e is JsonException or InvalidDataException or NotSupportedException) { throw Corrupt(); }
        if (transaction.SchemaVersion != 1) throw Corrupt();
        Validate(transaction.Before, true); Validate(transaction.After);
        if (!SameTarget(transaction.Before.Environment, transaction.After.Environment) || transaction.Before.Revision == transaction.After.Revision) throw Corrupt();
        var current = await ReadAsync(transaction.Before.Environment, ct).ConfigureAwait(false);
        if (current.Revision == transaction.After.Revision) { File.Delete(path); return; }
        if (current.Revision != transaction.Before.Revision) throw Recovery();
        var removed = Removed(transaction.Before, transaction.After).ToArray();
        var added = Removed(transaction.After, transaction.Before).ToArray();
        foreach (var old in removed)
            if (!await MatchesAsync(Backup(old), old, ct).ConfigureAwait(false)) throw Recovery();
        // Complete preflight before deleting even a known new file. User edits are preserved for manual conflict resolution.
        foreach (var mod in added)
        {
            var target = ModPath(mod);
            if (!File.Exists(target) && !Directory.Exists(target)) continue;
            var old = removed.FirstOrDefault(m => GameFiles.PathComparer.Equals(m.FileName, mod.FileName));
            if (!await MatchesAsync(target, mod, ct).ConfigureAwait(false) &&
                (old == null || !await MatchesAsync(target, old, ct).ConfigureAwait(false))) throw Recovery();
        }
        foreach (var old in removed)
        {
            var target = ModPath(old);
            if (Directory.Exists(target)) throw Recovery();
            if (File.Exists(target) && !await MatchesAsync(target, old, ct).ConfigureAwait(false) &&
                !added.Any(m => GameFiles.PathComparer.Equals(m.FileName, old.FileName))) throw Recovery();
        }
        foreach (var mod in added)
        {
            var old = removed.FirstOrDefault(m => GameFiles.PathComparer.Equals(m.FileName, mod.FileName));
            if (old != null && await MatchesAsync(ModPath(mod), old, ct).ConfigureAwait(false)) continue;
            if (await MatchesAsync(ModPath(mod), mod, ct).ConfigureAwait(false)) File.Delete(ModPath(mod));
        }
        GameFiles.CreateDirectory(At("mods"));
        foreach (var old in removed)
        {
            if (await MatchesAsync(ModPath(old), old, ct).ConfigureAwait(false)) continue;
            if (File.Exists(ModPath(old)) || Directory.Exists(ModPath(old))) throw Recovery();
            await CopyAsync(Backup(old), Staged(old), ct).ConfigureAwait(false);
            File.Move(Staged(old), ModPath(old));
        }
        await VerifyOwnedAsync(transaction.Before.Mods, ct).ConfigureAwait(false);
        File.Delete(path); // Receipt never changed before the commit point.
    }

    private static IEnumerable<InstalledMod> Removed(ModReceipt a, ModReceipt b) => a.Mods.Where(m =>
        !b.Mods.Any(n => n.ProjectId == m.ProjectId && n.FileName == m.FileName && n.Sha512 == m.Sha512));
    private static bool SameTarget(ModEnvironment a, ModEnvironment b) => a.MinecraftVersion == b.MinecraftVersion && a.Loader == b.Loader;
    private Task WriteAsync<T>(string relative, T value, CancellationToken ct) => GameFiles.WriteAtomicAsync(At(relative),
        (stream, token) => JsonSerializer.SerializeAsync(stream, value, Json, token), ct);
    internal static Task CopyAsync(string source, string target, CancellationToken ct) => GameFiles.WriteAtomicAsync(target, async (stream, token) =>
    {
        DownloadPath.CheckLinks(source);
        await using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
        await input.CopyToAsync(stream, token).ConfigureAwait(false);
    }, ct);
    private static Task<bool> MatchesAsync(string path, InstalledMod mod, CancellationToken ct) => MatchesAsync(path, mod.Sha512, mod.Size, ct);
    internal static async Task<bool> MatchesAsync(string path, string sha512, long size, CancellationToken ct) =>
        await StampAsync(path, ct).ConfigureAwait(false) is { } actual && actual.Sha512 == sha512 && actual.Size == size;
    internal static async Task<ContentStamp?> StampAsync(string path, CancellationToken ct)
    {
        DownloadPath.CheckLinks(path);
        if (!File.Exists(path)) return null;
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
        if (input.Length > 2L * 1024 * 1024 * 1024) throw new ResourceException(ResourceErrorCode.Unsupported, "文件超过有界校验限制。");
        var size = input.Length;
        return new(Convert.ToHexStringLower(await SHA512.HashDataAsync(input, ct).ConfigureAwait(false)), size);
    }
    private void Validate(ModReceipt value, bool empty = false)
    {
        try
        {
            if (value == null || value.SchemaVersion != 1 || !empty && value.Revision == Guid.Empty || value.Mods == null || value.Mods.Count > 512) throw Corrupt();
            ModrinthMetadata.Environment(value.Environment);
            foreach (var mod in value.Mods)
            {
                if (mod.Source != Platform) throw Corrupt();
                if (Platform == "curseforge" && (!NumericId(mod.ProjectId) || !NumericId(mod.VersionId))) throw Corrupt();
                ModrinthMetadata.Token(mod.ProjectId); ModrinthMetadata.Token(mod.VersionId); ModrinthMetadata.FileName(mod.FileName);
                if (!mod.FileName.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) || !ModrinthMetadata.Hash(mod.Sha512, 128) ||
                    mod.Sha512 != mod.Sha512.ToLowerInvariant() || mod.Size <= 0 || mod.Dependencies == null || mod.Dependencies.Count > 512 ||
                    mod.PublishedAt == default || mod.InstalledAt == default || mod.VersionNumber == null || mod.VersionNumber.Length > 32768) throw Corrupt();
                foreach (var dep in mod.Dependencies)
                {
                    if (dep.ProjectId != null) ModrinthMetadata.Token(dep.ProjectId);
                    if (Platform == "curseforge" && (dep.ProjectId == null || !NumericId(dep.ProjectId) || dep.VersionId != null && !NumericId(dep.VersionId))) throw Corrupt();
                    if (dep.VersionId != null) ModrinthMetadata.Token(dep.VersionId);
                    if (!Enum.IsDefined(dep.Type)) throw Corrupt();
                }
            }
            if (value.Mods.Select(m => m.ProjectId).Distinct(StringComparer.Ordinal).Count() != value.Mods.Count ||
                value.Mods.Select(m => m.FileName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != value.Mods.Count) throw Corrupt();
        }
        catch (Exception e) when (e is ArgumentException or NullReferenceException || e is ResourceException { Code: not ResourceErrorCode.StorageCorrupted }) { throw Corrupt(); }
    }
    private static bool NumericId(string value) => int.TryParse(value, System.Globalization.NumberStyles.None,
        System.Globalization.CultureInfo.InvariantCulture, out var id) && id > 0 && id.ToString(System.Globalization.CultureInfo.InvariantCulture) == value;
    internal static ResourceException Corrupt() => new(ResourceErrorCode.StorageCorrupted, "Mod 来源清单或事务记录损坏、环境不匹配或格式不受支持。");
    private static ResourceException Recovery() => new(ResourceErrorCode.RecoveryRequired, "上次 Mod 更新未完成，且文件已被外部修改；已保留旧文件备份，请处理冲突后重试。");
}
