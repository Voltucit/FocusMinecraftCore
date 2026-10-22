namespace FMCore.Resources;

// Deliberately no general dependency solver: contradictory pins/cycles fail explicitly before downloading.
internal sealed class ModrinthResolution(ModrinthClient client, ModEnvironment target, IReadOnlyList<InstalledMod> installed)
{
    private readonly Dictionary<string, ModrinthVersion> selected = new(StringComparer.Ordinal);
    private readonly HashSet<string> visiting = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ModrinthProject> projects = new(StringComparer.Ordinal);
    internal async Task<IReadOnlyList<ModrinthVersion>> ResolveAsync(string project, string? version, CancellationToken ct)
    {
        var canonical = await ProjectAsync(project, ct).ConfigureAwait(false);
        var root = version == null ? await LatestAsync(canonical.Id, ct).ConfigureAwait(false) : await client.GetVersionAsync(version, ct).ConfigureAwait(false);
        if (root.ProjectId != canonical.Id) throw ModrinthMetadata.Invalid();
        await VisitAsync(root, 0, ct).ConfigureAwait(false);
        return Array.AsReadOnly(selected.Values.OrderBy(v => v.ProjectId, StringComparer.Ordinal).ToArray());
    }
    private async Task VisitAsync(ModrinthVersion version, int depth, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (depth > 32 || visiting.Contains(version.ProjectId)) throw Dependency();
        if (selected.TryGetValue(version.ProjectId, out var previous))
        { if (previous.Id != version.Id) throw Dependency(); return; }
        if (selected.Count >= 128) throw Dependency();
        if (!ModrinthMetadata.Compatible(version, target)) throw new ResourceException(ResourceErrorCode.Incompatible, "Mod 或必要依赖不适配目标 Minecraft/加载器。");
        await ProjectAsync(version.ProjectId, ct).ConfigureAwait(false);
        ModrinthMetadata.Artifact(version);
        visiting.Add(version.ProjectId); selected.Add(version.ProjectId, version);
        foreach (var dep in version.Dependencies.Where(d => d.Type == ModDependencyType.Required))
        {
            ModrinthVersion next;
            if (dep.VersionId != null)
            {
                next = await client.GetVersionAsync(dep.VersionId, ct).ConfigureAwait(false);
                if (dep.ProjectId != null && dep.ProjectId != next.ProjectId) throw ModrinthMetadata.Invalid();
            }
            else if (dep.ProjectId != null)
            {
                if (selected.TryGetValue(dep.ProjectId, out var chosen)) next = chosen;
                else if (installed.FirstOrDefault(m => m.ProjectId == dep.ProjectId) is { } existing)
                {
                    next = await client.GetVersionAsync(existing.VersionId, ct).ConfigureAwait(false);
                    if (next.ProjectId != dep.ProjectId) throw ModrinthMetadata.Invalid();
                }
                else next = await LatestAsync(dep.ProjectId, ct).ConfigureAwait(false);
            }
            else throw Dependency(); // External filename-only dependency cannot be resolved safely.
            await VisitAsync(next, depth + 1, ct).ConfigureAwait(false);
        }
        visiting.Remove(version.ProjectId);
    }
    private async Task<ModrinthProject> ProjectAsync(string id, CancellationToken ct)
    {
        if (!projects.TryGetValue(id, out var project))
        {
            project = await client.GetProjectAsync(id, ct).ConfigureAwait(false);
            if (project.ProjectType != "mod") throw new ResourceException(ResourceErrorCode.Unsupported, "此入口只安装 Mod，不安装整合包、资源包或光影包。");
            projects[project.Id] = project; projects[project.Slug] = project;
        }
        return project;
    }
    private async Task<ModrinthVersion> LatestAsync(string project, CancellationToken ct)
    {
        var versions = await client.GetVersionsAsync(project, target, ct).ConfigureAwait(false);
        if (versions.Any(v => v.ProjectId != project)) throw ModrinthMetadata.Invalid();
        return versions.FirstOrDefault() ?? throw new ResourceException(ResourceErrorCode.Incompatible, "项目没有适配目标环境的版本。");
    }
    internal static void ValidateRelations(IReadOnlyList<InstalledMod> mods)
    {
        foreach (var mod in mods)
        foreach (var dep in mod.Dependencies)
        {
            var matches = mods.Where(m => (dep.ProjectId == null || m.ProjectId == dep.ProjectId) &&
                (dep.VersionId == null || m.VersionId == dep.VersionId));
            if (dep.Type == ModDependencyType.Required && (dep.ProjectId == null && dep.VersionId == null || !matches.Any())) throw Dependency();
            if (dep.Type == ModDependencyType.Incompatible && dep.ProjectId == null && dep.VersionId == null) throw Dependency();
            if (dep.Type == ModDependencyType.Incompatible && (dep.ProjectId != null || dep.VersionId != null) && matches.Any()) throw ModrinthMetadata.Conflict();
        }
    }
    private static ResourceException Dependency() => new(ResourceErrorCode.DependencyConflict, "必要依赖存在循环、矛盾版本、过多层级或无法自动解析的外部文件。");
}
