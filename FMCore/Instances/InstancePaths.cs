using FMCore.Downloader;
using FMCore.Utilities;
using FMCore.Versions;

namespace FMCore.Instances;

internal static partial class InstanceFiles
{
    internal static string Relative(string id) => "versions/" + Id(id);

    internal static bool IsVersionRuntimePath(string directory, string path)
    {
        var relative = Path.GetRelativePath(directory, path).Replace('\\', '/');
        var id = Path.GetFileName(directory);
        var top = relative.Split('/')[0];
        return GameFiles.PathComparer.Equals(top, id + ".json") ||
            GameFiles.PathComparer.Equals(top, id + ".jar") ||
            GameFiles.PathComparer.Equals(relative.Split('/')[0], "natives");
    }

    internal static async Task<bool> CanAttachRuntimeAsync(string directory, string id, CancellationToken ct)
    {
        foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
        {
            ct.ThrowIfCancellationRequested();
            DownloadPath.CheckLinks(entry);
            if (!IsVersionRuntimePath(directory, entry)) return false;
            if (Directory.Exists(entry) != GameFiles.PathComparer.Equals(Path.GetFileName(entry), "natives")) return false;
        }
        var metadata = GameFiles.Resolve(directory, id + ".json");
        if (!File.Exists(metadata)) return false;
        try { return VersionParser.Parse(await GameFiles.ReadJsonAsync(metadata, ct).ConfigureAwait(false)).Id == id; }
        catch (Exception error) when (error is VersionException or InvalidDataException) { return false; }
    }

    internal static void EnsureCurrentLayout(string root, string id)
    {
        if (File.Exists(GameFiles.Resolve(root, Relative(id) + "/" + Metadata))) return;
        if (Directory.Exists(GameFiles.Resolve(root, "instances/" + Id(id))))
            throw new InstanceException(InstanceErrorCode.Conflict,
                "检测到旧 instances 目录。请先备份并将该实例数据迁移到 versions 下的同名目录；SDK 不会自动搬动或覆盖旧数据。");
    }

    internal static void CheckLegacyInstances(string root)
    {
        var parent = GameFiles.Resolve(root, "instances");
        if (!Directory.Exists(parent)) return;
        foreach (var directory in Directory.EnumerateDirectories(parent))
        {
            DownloadPath.CheckLinks(directory);
            if (File.Exists(GameFiles.Resolve(directory, Metadata))) EnsureCurrentLayout(root, Path.GetFileName(directory));
        }
    }
}
