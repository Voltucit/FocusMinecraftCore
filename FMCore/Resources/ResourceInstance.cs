using FMCore.Downloader;
using FMCore.Modpacks;
using FMCore.Instances;
using FMCore.Utilities;
using FMCore.Versions;

namespace FMCore.Resources;

// Both platforms must hold the same lease and recover both journals before touching the active mods directory.
internal sealed class ResourceInstance
{
    internal PackResourceScope? PackScope { get; set; }
    private readonly string root;
    private readonly string id;
    private readonly string relative;
    private readonly ModEnvironment? target;
    private readonly InstanceInfo? instance;
    internal string GameDirectory { get; }
    internal ResourceInstance(string rootDirectory, string instanceId, ModEnvironment? environment, InstanceInfo? instance)
    {
        try
        {
            id = InstanceFiles.Id(instanceId); root = Path.GetFullPath(rootDirectory); relative = InstanceFiles.Relative(id);
            GameDirectory = GameFiles.Resolve(root, relative); target = environment; this.instance = instance;
        }
        catch (InstanceException e) { throw Map(e); }
        catch (DownloadException e) { throw Map(e); }
        catch (Exception e) when (e is ArgumentException or NotSupportedException) { throw ModrinthMetadata.InvalidRequest(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { throw Io(); }
    }
    internal async Task<T> ExecuteAsync<T>(Func<ModEnvironment, CancellationToken, Task<T>> action, CancellationToken ct, bool readOnly = false)
    {
        ct.ThrowIfCancellationRequested();
        if (PackScope is { } scope && scope.IsFor(GameDirectory)) return await action(scope.Environment, ct).ConfigureAwait(false);
        try
        {
            InstanceFiles.EnsureCurrentLayout(root, id);
            if (readOnly && !Directory.Exists(GameFiles.Resolve(GameDirectory, ".fmcore/resources")) &&
                !Directory.Exists(GameFiles.Resolve(GameDirectory, ".fmcore/modpacks")) && !InstanceLock.IsInUse(root, relative))
            {
                if (instance != null) await InstanceFiles.ReadAsync(GameDirectory, id, ct).ConfigureAwait(false);
                // No state or journals exist: return an empty snapshot without creating root/lock directories.
                if (target != null) return await action(target, ct).ConfigureAwait(false);
            }
            using var lease = InstanceLock.Acquire(root, relative);
            using var legacy = InstanceLock.AcquireLegacy(GameDirectory);
            await new PackFiles(GameDirectory).RecoverAsync(ct).ConfigureAwait(false);
            if (instance != null)
            {
                var current = await InstanceFiles.ReadAsync(GameDirectory, id, ct).ConfigureAwait(false);
                if (current.Revision != instance.Revision) throw ModrinthMetadata.Conflict();
            }
            else if (File.Exists(GameFiles.Resolve(GameDirectory, InstanceFiles.Metadata)))
                throw new ResourceException(ResourceErrorCode.InvalidRequest, "托管实例请使用 InstanceInfo 构造入口，以校验版本配置和快照。");
            await new ResourceFiles(GameDirectory).RecoverAsync(ct).ConfigureAwait(false);
            await new ResourceFiles(GameDirectory, "curseforge").RecoverAsync(ct).ConfigureAwait(false);
            var environment = target;
            if (environment == null)
            {
                var core = await GameCoreUtil.GetGameCoreAsync(instance!.Config.VersionId, root, ct).ConfigureAwait(false);
                var loaders = core.Loaders.Where(l => l.Name is "Fabric" or "Forge" or "NeoForge").ToArray();
                if (loaders.Length != 1 || core.MinecraftVersion == null) throw new ResourceException(ResourceErrorCode.Unsupported, "无法从实例元数据确定唯一的 Mod 加载器及 Minecraft 版本。");
                environment = ModrinthMetadata.Environment(new(core.MinecraftVersion, loaders[0].Name.ToLowerInvariant()));
            }
            return await action(environment, ct).ConfigureAwait(false);
        }
        catch (ModpackException e) { throw new ResourceException(e.Code == ModpackErrorCode.UnsafePath ? ResourceErrorCode.UnsafePath : ResourceErrorCode.RecoveryRequired, "整合包事务需要恢复；请通过 ModpackInstaller 处理后重试。"); }
        catch (InstanceException e) { throw Map(e); }
        catch (DownloadException e) { throw Map(e); }
        catch (VersionException) { throw new ResourceException(ResourceErrorCode.InvalidRequest, "实例缺少可解析的本地版本元数据，请先安装加载器。"); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { throw Io(); }
    }
    internal static IReadOnlyList<InstalledMod> Freeze(IEnumerable<InstalledMod> mods) => Array.AsReadOnly(mods.Select(m => m with
    { Dependencies = Array.AsReadOnly(m.Dependencies.ToArray()) }).ToArray());
    private static ResourceException Map(InstanceException e) => new(e.Code switch
    { InstanceErrorCode.Busy => ResourceErrorCode.Busy, InstanceErrorCode.UnsafePath => ResourceErrorCode.UnsafePath,
        InstanceErrorCode.NotFound => ResourceErrorCode.NotFound, _ => ResourceErrorCode.Conflict }, e.Message);
    internal static ResourceException Map(DownloadException e) => new(e.Code == DownloadErrorCode.UnsafePath ? ResourceErrorCode.UnsafePath : ResourceErrorCode.DownloadFailed,
        "资源文件下载或路径校验失败；尚未提交的更新保留旧 Mod。") { DownloadError = e.Code };
    private static ResourceException Io() => new(ResourceErrorCode.IoError, "无法读写实例 Mod 文件；如存在事务记录，其备份已保留。");
}
