using FMCore.Resources;

namespace FMCore.Modpacks;

internal static class PackRelations
{
    // A published pack may deliberately pin a newer dependency than an individual old Mod's metadata.
    // Keep the original declaration in pack provenance; T11 receipts describe the pack's effective project dependency.
    internal static IReadOnlyList<InstalledMod> Normalize(IReadOnlyList<InstalledMod> mods, bool strict, List<string>? warnings = null)
    {
        var result = mods.Select(mod => mod with { Dependencies = Array.AsReadOnly(mod.Dependencies.Select(dep =>
        {
            if (dep.Type != ModDependencyType.Required || dep.VersionId == null || dep.ProjectId == null ||
                !mods.Any(selected => selected.ProjectId == dep.ProjectId && selected.VersionId != dep.VersionId)) return dep;
            if (strict) throw new ModpackException(ModpackErrorCode.DependencyConflict, "Mod 的平台依赖版本与整合包固定版本不同；严格模式拒绝，未替换任何版本。");
            var path = "mods/" + mod.FileName;
            if (warnings != null && !warnings.Contains(path, StringComparer.Ordinal)) warnings.Add(path);
            return dep with { VersionId = null };
        }).ToArray()) }).ToArray();
        ModrinthResolution.ValidateRelations(result);
        return Array.AsReadOnly(result);
    }
}
