namespace FMCore.Resources;

internal sealed class CurseForgeResolution(CurseForgeClient client, ModEnvironment target, IReadOnlyList<InstalledMod> installed)
{
    private readonly Dictionary<int, CurseForgeProject> projects = [];
    private readonly Dictionary<int, IReadOnlyList<CurseForgeFile>> versions = [];
    private readonly Dictionary<int, CurseForgeFile> selected = [];
    private readonly HashSet<int> visiting = [];
    internal async Task<IReadOnlyList<CurseForgeFile>> ResolveAsync(int project, int? fileId, CancellationToken ct)
    {
        var root = fileId is { } id ? await client.GetFileAsync(project, id, ct).ConfigureAwait(false) : await LatestAsync(project, ct).ConfigureAwait(false);
        await VisitAsync(root, 0, ct).ConfigureAwait(false);
        return Array.AsReadOnly(selected.Values.OrderBy(v => v.ModId).ToArray());
    }
    private async Task VisitAsync(CurseForgeFile file, int depth, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (depth > 32 || visiting.Contains(file.ModId)) throw Dependency();
        if (selected.TryGetValue(file.ModId, out var chosen)) { if (chosen.Id != file.Id) throw Dependency(); return; }
        if (selected.Count >= 128) throw Dependency();
        CurseForgeMetadata.Downloadable(await ProjectAsync(file.ModId, ct).ConfigureAwait(false), file);
        if (!CurseForgeMetadata.Compatible(file, target)) throw new ResourceException(ResourceErrorCode.Incompatible, "Mod 或必要依赖不适配目标 Minecraft/加载器。");
        selected.Add(file.ModId, file); visiting.Add(file.ModId);
        foreach (var dependency in file.Dependencies.Where(d => d.RelationType == CurseForgeRelationType.RequiredDependency))
        {
            CurseForgeFile next;
            if (selected.TryGetValue(dependency.ModId, out var pending)) next = pending;
            else if (installed.FirstOrDefault(m => m.ProjectId == dependency.ModId.ToString(System.Globalization.CultureInfo.InvariantCulture)) is { } existing)
                next = await client.GetFileAsync(dependency.ModId, int.Parse(existing.VersionId, System.Globalization.CultureInfo.InvariantCulture), ct).ConfigureAwait(false);
            else next = await LatestAsync(dependency.ModId, ct).ConfigureAwait(false);
            await VisitAsync(next, depth + 1, ct).ConfigureAwait(false);
        }
        visiting.Remove(file.ModId);
    }
    internal async Task<CurseForgeProject> ProjectAsync(int project, CancellationToken ct)
    {
        if (!projects.TryGetValue(project, out var value))
        {
            value = await client.GetProjectAsync(project, ct).ConfigureAwait(false);
            if (value.ClassId != 6) throw new ResourceException(ResourceErrorCode.Unsupported, "此入口只安装 Minecraft Mod。");
            if (value.AllowModDistribution == false || !value.IsAvailable) throw CurseForgeMetadata.Restricted();
            projects.Add(project, value);
        }
        return value;
    }
    internal async Task<IReadOnlyList<CurseForgeFile>> AllAsync(int project, CancellationToken ct)
    {
        if (versions.TryGetValue(project, out var cached)) return cached;
        var all = new List<CurseForgeFile>(); int? total = null;
        for (var index = 0; ; index += 50)
        {
            var page = await client.GetFilesAsync(project, target with { IncludePrereleases = true }, index, 50, ct).ConfigureAwait(false);
            if (page.TotalCount > 10000) throw new ResourceException(ResourceErrorCode.Unsupported, "文件列表超过 CurseForge 的分页范围，请明确指定文件 ID。");
            if (total != null && total != page.TotalCount || page.ResultCount != Math.Min(50, Math.Max(0, page.TotalCount - index))) throw CurseForgeMetadata.Invalid();
            total = page.TotalCount; all.AddRange(page.Files);
            if (index + 50 >= total) break;
        }
        if (all.Select(f => f.Id).Distinct().Count() != all.Count) throw CurseForgeMetadata.Invalid();
        return versions[project] = all.AsReadOnly();
    }
    internal async Task<CurseForgeFile> LatestAsync(int project, CancellationToken ct)
    {
        await ProjectAsync(project, ct).ConfigureAwait(false);
        var files = await AllAsync(project, ct).ConfigureAwait(false);
        var file = files.Where(f => !f.IsServerPack && !f.ExposeAsAlternative && (target.IncludePrereleases || f.ReleaseType == 1))
            .OrderByDescending(f => f.PublishedAt).ThenByDescending(f => f.Id).FirstOrDefault() ??
            throw new ResourceException(ResourceErrorCode.Incompatible, "项目没有适配目标 Minecraft/加载器及渠道的客户端 Mod 文件。");
        CurseForgeMetadata.Downloadable(await ProjectAsync(project, ct).ConfigureAwait(false), file);
        return file;
    }
    private static ResourceException Dependency() => new(ResourceErrorCode.DependencyConflict, "必要依赖存在循环、矛盾文件版本或超过解析层级/数量限制。");
}
