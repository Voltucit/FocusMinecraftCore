using System.Text.Json.Nodes;
using FMCore.Installer;
using FMCore.Versions;

namespace FMCore.Modsloader;

/// <summary>删除安装器本次创建、且已经不再被任何版本继承的原版父版本。</summary>
internal static class BaseVersionCleanup
{
    private static readonly string[] UserDataFolders =
    [
        "mods",
        "saves",
        "config",
        "resourcepacks",
        "shaderpacks",
        "logs",
        "crash-reports"
    ];

    public static async Task RemoveIfUnusedAsync(
        string root,
        string gameVersion,
        CancellationToken cancellationToken)
    {
        string versionsRoot = PathSafety.RequireDirectoryPath(
            Path.Combine(root, "versions"));
        string directory = PathSafety.RequireDirectoryPath(
            Path.Combine(versionsRoot, gameVersion));

        if (!PathSafety.Comparer.Equals(
                Path.GetDirectoryName(directory), versionsRoot)
            || !File.Exists(Path.Combine(directory, gameVersion + ".json"))
            || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
        {
            return;
        }

        // 安装器刚创建的原版目录不应有这些内容；出现时说明已被当作用户实例使用。
        foreach (string name in UserDataFolders)
        {
            if (Directory.Exists(Path.Combine(directory, name)))
                return;
        }

        if (File.Exists(Path.Combine(directory, "options.txt")))
            return;

        MinecraftVersionReader reader = new();
        IReadOnlyList<MinecraftVersion> versions =
            await reader.GetVersionsAsync(root, cancellationToken)
                .ConfigureAwait(false);

        foreach (MinecraftVersion version in versions)
        {
            if (PathSafety.Comparer.Equals(version.Id, gameVersion))
                continue;

            IReadOnlyList<MinecraftVersion> chain;
            try
            {
                chain = reader.GetInheritanceChain(version, versions);
            }
            catch
            {
                // 其他版本配置损坏时保留父版本，避免清理动作扩大问题。
                return;
            }

            if (chain.Any(item =>
                    PathSafety.Comparer.Equals(item.Id, gameVersion)))
            {
                return;
            }

            JsonObject data = MinecraftVersionReader.ReadVersionJson(version.Directory);
            if (data["jar"] is JsonValue jar
                && jar.TryGetValue<string>(out string? jarId)
                && PathSafety.Comparer.Equals(jarId, gameVersion))
            {
                return;
            }
        }

        try
        {
            Directory.Delete(directory, true);
        }
        catch (IOException)
        {
            // 文件仍被占用时保留目录，由启动器侧后续隐藏。
        }
        catch (UnauthorizedAccessException)
        {
            // 权限不足时保留目录，由启动器侧后续隐藏。
        }
    }
}
