using System.Globalization;
using FMCore.Resources;

namespace FMCore.Modpacks;

/// <summary>A compatible single-Mod update candidate; application still requires explicit pack authorization.</summary>
public sealed record ModpackModUpdate(InstalledMod Installed, string AvailableVersionId, string VersionNumber, bool IsLocked = true);

public sealed partial class ModpackInstaller
{
    /// <summary>Checks pack-owned Mods in both platforms using T11 CheckUpdatesAsync. Never changes files and never unlocks them.</summary>
    public Task<IReadOnlyList<ModpackModUpdate>> CheckModUpdatesAsync(CancellationToken cancellationToken = default) => ExecuteAsync(async ct =>
    {
        var state = await files.ReadAsync(ct).ConfigureAwait(false) ?? throw PackFiles.Conflict();
        var result = new List<ModpackModUpdate>();
        var scope = new PackResourceScope(GameDirectory, Environment(state));
        if (state.Files.Any(f => f.Mod?.Source == "modrinth"))
            foreach (var item in await new ModrinthInstaller(root, id, Environment(state), modrinth, options.Downloads).WithPackScope(scope).CheckUpdatesAsync(ct).ConfigureAwait(false))
                if (state.Files.Any(f => f.Mod?.Source == "modrinth" && f.Mod.ProjectId == item.Installed.ProjectId))
                    result.Add(new(item.Installed, item.Available.Id, item.Available.VersionNumber));
        if (state.Files.Any(f => f.Mod?.Source == "curseforge"))
        {
            var client = options.CurseForge ?? throw Invalid();
            foreach (var item in await new CurseForgeInstaller(root, id, Environment(state), client, options.Downloads, http).WithPackScope(scope).CheckUpdatesAsync(ct).ConfigureAwait(false))
                if (state.Files.Any(f => f.Mod?.Source == "curseforge" && f.Mod.ProjectId == item.Installed.ProjectId))
                    result.Add(new(item.Installed, Number(item.Available.Id), item.Available.DisplayName));
        }
        return (IReadOnlyList<ModpackModUpdate>)result.AsReadOnly();
    }, cancellationToken);

    /// <summary>Explicitly authorizes one pack Mod update and its necessary dependencies through T11 UpdateAsync. The result has the same confirm/cleanup/rollback lifecycle as a whole-pack update.</summary>
    /// <remarks>authorize must be true. This changes the pack's pinned content and marks it modified; it is never part of whole-pack automatic updating.</remarks>
    public Task<ModpackInstallResult> UpdateModAsync(string projectId, bool authorize = false, string source = "modrinth",
        IProgress<ModpackProgress>? progress = null, CancellationToken cancellationToken = default) => ExecuteAsync(async ct =>
    {
        if (!authorize) throw new ModpackException(ModpackErrorCode.UpdateLocked, "整合包 Mod 已锁定；调用方必须取得用户授权并明确传入 authorize: true。");
        var state = await files.ReadAsync(ct).ConfigureAwait(false) ?? throw PackFiles.Conflict();
        if (state.Installed.PendingUpdateId != null) throw Pending();
        if (!state.Files.Any(f => f.Mod?.ProjectId == projectId && f.Mod.Source == source)) throw new ModpackException(ModpackErrorCode.NotFound, "此整合包没有可识别来源的指定 Mod。");
        ModpackInstallResult? committed = null;
        var scope = new PackResourceScope(GameDirectory, Environment(state), async (platform, before, after, token) =>
        {
            var operation = Guid.NewGuid(); var selected = new List<PackOwnedFile>();
            // Keep untouched pack files and any independently managed Mods. Only the changed dependency closure joins this pack transaction.
            selected.AddRange(state.Files.Where(f => f.Mod?.Source != platform || !before.Mods.Any(m => m.ProjectId == f.Mod.ProjectId)));
            foreach (var mod in after.Mods)
            {
                var previous = state.Files.FirstOrDefault(f => f.Mod?.Source == platform && f.Mod.ProjectId == mod.ProjectId);
                var old = before.Mods.FirstOrDefault(m => m.ProjectId == mod.ProjectId);
                if (previous == null && old == mod) continue;
                if (previous != null && old == mod) { selected.Add(previous); continue; }
                ModpackFile reference;
                if (platform == "modrinth")
                {
                    var version = await modrinth.GetVersionAsync(mod.VersionId, token).ConfigureAwait(false);
                    var artifact = version.Files.FirstOrDefault(f => f.Sha512 == mod.Sha512 && f.FileName == mod.FileName) ?? throw PackArchive.Invalid();
                    PackArchive.DownloadUri(artifact.Url.AbsoluteUri);
                    reference = new("mods/" + mod.FileName, mod.Size, mod.Sha512, artifact.Sha1, [artifact.Url]);
                }
                else
                {
                    var file = await options.CurseForge!.GetFileAsync(int.Parse(mod.ProjectId, CultureInfo.InvariantCulture), int.Parse(mod.VersionId, CultureInfo.InvariantCulture), token).ConfigureAwait(false);
                    reference = new("mods/" + mod.FileName, mod.Size, mod.Sha512, file.Sha1, [], ProjectId: file.ModId, FileId: file.Id);
                }
                selected.Add(new(reference, mod));
            }
            var installed = state.Installed with { Revision = Guid.NewGuid(), PendingUpdateId = operation, IsModified = true,
                Pack = state.Installed.Pack with { Files = Array.AsReadOnly(selected.Select(f => f.File).ToArray()) } };
            var next = new PackState(1, installed, selected.AsReadOnly(), options.AutoCleanupAfterConfirmation);
            foreach (var item in selected)
            {
                var active = state.Files.FirstOrDefault(f => f.File.Path == item.File.Path);
                if (active?.File.Sha512 == item.File.Sha512) continue;
                var cache = files.At(ResourceFiles.Cache(item.File.Sha512!, platform));
                await ResourceFiles.CopyAsync(cache, files.New(operation, item.File.Path), token).ConfigureAwait(false);
            }
            var entries = await DeltaAsync(state, next, operation, token).ConfigureAwait(false);
            await StageControlAsync(".fmcore/resources/" + platform + ".json", after, operation, entries, token).ConfigureAwait(false);
            await files.CommitAsync(state, next, operation, entries, progress, token).ConfigureAwait(false);
            committed = new(installed, operation, false);
        });
        var forward = new InlineProgress<ResourceProgress>(p => progress?.Report(new(p.Stage switch
        { ResourceStage.Downloading => ModpackStage.Downloading, ResourceStage.Committing => ModpackStage.Committing, ResourceStage.Completed => ModpackStage.Completed, _ => ModpackStage.Resolving }, Download: p.Download)));
        ModInstallResult result;
        if (source == "modrinth") result = await new ModrinthInstaller(root, id, Environment(state), modrinth, options.Downloads).WithPackScope(scope).UpdateAsync(projectId, forward, ct).ConfigureAwait(false);
        else if (source == "curseforge" && int.TryParse(projectId, out var project) && options.CurseForge != null)
            result = await new CurseForgeInstaller(root, id, Environment(state), options.CurseForge, options.Downloads, http).WithPackScope(scope).UpdateAsync(project, forward, ct).ConfigureAwait(false);
        else throw Invalid();
        return committed ?? new(state.Installed, Guid.Empty, result.Reused);
    }, cancellationToken);
}
