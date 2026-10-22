using FMCore.Downloader;
using FMCore.Launch;
using FMCore.Modpacks;
using FMCore.Utilities;

namespace FMCore.Instances;

/// <summary>Creates and manages isolated instances under a shared game root. No account, network, database or dependency injection is required.</summary>
public sealed class InstanceManager
{
    /// <summary>Absolute root for shared versions, libraries, assets and isolated instance directories.</summary>
    public string RootDirectory { get; }

    /// <summary>Uses .minecraft by default. Construction and empty listing do not create directories.</summary>
    /// <exception cref="InstanceException">The root is invalid, inaccessible or contains a symbolic link/reparse point.</exception>
    public InstanceManager(string rootDirectory = ".minecraft")
    {
        RootDirectory = Guard(() =>
        {
            if (string.IsNullOrWhiteSpace(rootDirectory)) throw new InstanceException(InstanceErrorCode.UnsafePath, "游戏根目录无效。");
            var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootDirectory));
            DownloadPath.CheckLinks(root);
            return root;
        });
    }

    /// <summary>Creates an instance with default settings and a generated ID; does not install game files.</summary>
    public Task<InstanceInfo> CreateAsync(string name, string versionId, CancellationToken cancellationToken = default)
        => CreateAsync(new() { Name = name, VersionId = versionId }, cancellationToken: cancellationToken);

    /// <summary>Creates persistent settings and an isolated directory. An optional safe lowercase ID is independent of the display name and version.</summary>
    /// <exception cref="InstanceException">InvalidConfig, UnsafePath, AlreadyExists, Busy or IoError.</exception>
    public Task<InstanceInfo> CreateAsync(InstanceConfig config, string? instanceId = null, CancellationToken cancellationToken = default) => GuardAsync(async () =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        var frozen = InstanceFiles.Freeze(config);
        var id = InstanceFiles.Id(instanceId ?? Guid.NewGuid().ToString("N"));
        var relative = Relative(id);
        var directory = GameFiles.Resolve(RootDirectory, relative);
        using var lease = InstanceLock.Acquire(RootDirectory, relative);
        if (Path.Exists(directory)) throw new InstanceException(InstanceErrorCode.AlreadyExists, "实例目标目录已存在，不会覆盖或接管现有目录。");
        var now = DateTimeOffset.UtcNow;
        var document = new InstanceDocument(1, id, Guid.NewGuid(), now, now, frozen);
        var temporary = GameFiles.Resolve(RootDirectory, $"instances/.fmcore-create-{Guid.NewGuid():N}");
        try
        {
            await InstanceFiles.WriteAsync(temporary, document, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            GameFiles.Resolve(RootDirectory, relative);
            Directory.Move(temporary, directory);
            return Info(document, directory, false);
        }
        finally
        {
            // Only our known staging entries; never recursively remove a directory supplied by a caller.
            try
            {
                var path = GameFiles.Resolve(temporary, InstanceFiles.Metadata);
                File.Delete(path);
                if (Directory.Exists(Path.GetDirectoryName(path))) Directory.Delete(Path.GetDirectoryName(path)!);
                if (Directory.Exists(temporary)) Directory.Delete(temporary);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or DownloadException) { }
        }
    });

    /// <summary>Lists managed instances, sorted by ID. Unmanaged directories are ignored; damaged documents fail explicitly rather than being silently omitted.</summary>
    public Task<IReadOnlyList<InstanceInfo>> GetInstancesAsync(CancellationToken cancellationToken = default) => GuardAsync<IReadOnlyList<InstanceInfo>>(async () =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parent = GameFiles.Resolve(RootDirectory, "instances");
        if (!Directory.Exists(parent)) return Array.Empty<InstanceInfo>();
        var results = new List<InstanceInfo>();
        foreach (var directory in Directory.EnumerateDirectories(parent).Order(StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            DownloadPath.CheckLinks(directory);
            var id = Path.GetFileName(directory);
            if (id.StartsWith(".fmcore-create-", StringComparison.Ordinal)) continue;
            var metadata = GameFiles.Resolve(directory, InstanceFiles.Metadata);
            if (!File.Exists(metadata)) continue;
            try { results.Add(await GetAsync(id, cancellationToken).ConfigureAwait(false)); }
            catch (InstanceException e) when (e.Code == InstanceErrorCode.NotFound) { /* Concurrent completed deletion. */ }
        }
        return results.AsReadOnly();
    });

    /// <summary>Reads a managed instance and its advisory usage state without changing its directory or configuration.</summary>
    public Task<InstanceInfo> GetAsync(string instanceId, CancellationToken cancellationToken = default) => GuardAsync(async () =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        var id = InstanceFiles.Id(instanceId);
        var directory = GameFiles.Resolve(RootDirectory, Relative(id));
        var document = await InstanceFiles.ReadAsync(directory, id, cancellationToken).ConfigureAwait(false);
        return Info(document, directory, InstanceLock.IsInUse(RootDirectory, Relative(id)));
    });

    /// <summary>Updates settings using the supplied snapshot's revision. The instance ID, directory and creation time stay fixed.</summary>
    /// <exception cref="InstanceException">Busy while preparing/running, Conflict for stale snapshots; other structured storage/configuration errors.</exception>
    public Task<InstanceInfo> UpdateAsync(InstanceInfo instance, InstanceConfig config, CancellationToken cancellationToken = default) => GuardAsync(async () =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        CheckOwner(instance);
        var frozen = InstanceFiles.Freeze(config);
        var directory = GameFiles.Resolve(RootDirectory, Relative(instance.Id));
        using var lease = InstanceLock.Acquire(RootDirectory, Relative(instance.Id));
        using var legacy = InstanceLock.AcquireLegacy(directory);
        try
        {
            var packFiles = new PackFiles(directory);
            await packFiles.RecoverAsync(cancellationToken).ConfigureAwait(false);
            if ((await packFiles.ReadAsync(cancellationToken).ConfigureAwait(false))?.Installed.PendingUpdateId != null)
                throw Conflict();
        }
        catch (ModpackException) { throw Conflict(); }
        var previous = await InstanceFiles.ReadAsync(directory, instance.Id, cancellationToken).ConfigureAwait(false);
        if (previous.Revision != instance.Revision) throw Conflict();
        var document = previous with { Config = frozen, Revision = Guid.NewGuid(), UpdatedAt = DateTimeOffset.UtcNow };
        await InstanceFiles.WriteAsync(directory, document, cancellationToken).ConfigureAwait(false);
        return Info(document, directory, false);
    });

    /// <summary>Deletes only the identified managed directory. Shared versions/libraries/assets and sibling instances are never deletion targets.</summary>
    /// <remarks>Preflight rejects all links before deleting any data. Mid-deletion cancellation/failure may leave partial data; the configuration is removed last to support retry. Final cleanup I/O failure can leave an unmanaged empty directory.</remarks>
    /// <exception cref="InstanceException">Busy, NotFound, UnsafePath, StorageCorrupted or IoError. Unmanaged/corrupt directories are never adopted for deletion.</exception>
    public async Task DeleteAsync(string instanceId, IProgress<InstanceDeletionProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        await GuardAsync(async () =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = InstanceFiles.Id(instanceId);
            var directory = GameFiles.Resolve(RootDirectory, Relative(id));
            using var lease = InstanceLock.Acquire(RootDirectory, Relative(id));
            // Respect the previous launch lock too; release its file handle before Windows directory deletion.
            using (InstanceLock.AcquireLegacy(directory))
                await InstanceFiles.ReadAsync(directory, id, cancellationToken).ConfigureAwait(false);
            await Task.Run(() => InstanceFiles.Delete(directory, progress, cancellationToken), cancellationToken).ConfigureAwait(false);
            return true;
        }).ConfigureAwait(false);
    }

    internal async Task ValidateLaunchAsync(InstanceInfo expected, string relative, string versionId, CancellationToken ct)
    {
        CheckOwner(expected);
        var actualPath = relative.Length == 0 ? RootDirectory : GameFiles.Resolve(RootDirectory, relative);
        if (!GameFiles.PathComparer.Equals(actualPath, expected.GameDirectory) || versionId != expected.Config.VersionId) throw Conflict();
        var current = await GetAsync(expected.Id, ct).ConfigureAwait(false);
        if (current.Revision != expected.Revision) throw Conflict();
    }

    private void CheckOwner(InstanceInfo instance)
    {
        if (instance == null || !GameFiles.PathComparer.Equals(RootDirectory, instance.RootDirectory))
            throw new InstanceException(InstanceErrorCode.InvalidConfig, "实例快照不属于此游戏根目录。");
    }
    private InstanceInfo Info(InstanceDocument document, string directory, bool busy) => new(document.Id, RootDirectory, directory,
        document.Config, document.Revision, document.CreatedAt, document.UpdatedAt, busy);
    private static string Relative(string id) => "instances/" + id;
    private static InstanceException Conflict() => new(InstanceErrorCode.Conflict, "实例配置已变化，请重新读取后操作。");

    private static T Guard<T>(Func<T> operation)
    {
        try { return operation(); }
        catch (Exception e) when (e is DownloadException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { throw Failure(e); }
    }
    private static async Task<T> GuardAsync<T>(Func<Task<T>> operation)
    {
        try { return await operation().ConfigureAwait(false); }
        catch (Exception e) when (e is DownloadException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { throw Failure(e); }
    }
    private static InstanceException Failure(Exception error) => error is DownloadException { Code: DownloadErrorCode.UnsafePath } or ArgumentException or NotSupportedException
        ? new(InstanceErrorCode.UnsafePath, "实例路径无效，或包含链接及不安全片段。")
        : new(InstanceErrorCode.IoError, "无法访问实例目录或配置文件。");
}
