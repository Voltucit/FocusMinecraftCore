using FMCore.Resources;
using FMCore.Utilities;

namespace FMCore.Modpacks;

// Only the pack entry point can establish this scope, while holding the ordinary instance lease.
// No public bypass flag and no second business implementation of T11's dependency/update selection.
internal sealed class PackResourceScope
{
    private readonly string directory;
    internal ModEnvironment Environment { get; }
    private readonly Func<string, ModReceipt, ModReceipt, CancellationToken, Task>? commit;
    internal PackResourceScope(string directory, ModEnvironment environment, Func<string, ModReceipt, ModReceipt, CancellationToken, Task>? commit = null)
    { this.directory = directory; Environment = environment; this.commit = commit; }
    internal bool IsFor(string path) => GameFiles.PathComparer.Equals(directory, path);
    internal static async Task<bool> CommitAsync(string directory, string source, ModReceipt before, ModReceipt after, CancellationToken ct, PackResourceScope? current = null)
    {
        if (current?.commit != null && current.IsFor(directory)) { await current.commit(source, before, after, ct).ConfigureAwait(false); return true; }
        var state = await new PackFiles(directory).ReadAsync(ct).ConfigureAwait(false);
        if (state == null) return false;
        if (state.Installed.PendingUpdateId != null || state.Files.Any(f =>
            f.Mod?.Source == source && (!before.Mods.Any(m => Same(m, f.Mod)) || !after.Mods.Any(m => Same(m, f.Mod))) ||
            f.File.Path.StartsWith("mods/", StringComparison.OrdinalIgnoreCase) && after.Mods.Any(m =>
                string.Equals(f.File.Path, "mods/" + m.FileName, StringComparison.OrdinalIgnoreCase) && m.Sha512 != f.File.Sha512)))
            throw new ResourceException(ResourceErrorCode.ModpackLocked, "整合包托管 Mod 默认锁定；请通过 ModpackInstaller 明确授权独立更新，或先确认/回滚待处理更新。");
        return false;
    }
    private static bool Same(InstalledMod a, InstalledMod b) => a.Source == b.Source && a.ProjectId == b.ProjectId && a.VersionId == b.VersionId &&
        a.FileName == b.FileName && a.Sha512 == b.Sha512 && a.Size == b.Size;
}
