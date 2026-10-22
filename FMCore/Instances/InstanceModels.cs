using System.Diagnostics;
using FMCore.Authentication;
using FMCore.Launch;

namespace FMCore.Instances;

/// <summary>Persistent, account-free settings for one isolated game instance. Version files, libraries and assets remain shared.</summary>
[DebuggerDisplay("Minecraft instance configuration")]
public sealed record InstanceConfig
{
    /// <summary>Display name, 1–128 characters. This is not a directory name.</summary>
    public string Name { get; init; } = "";
    /// <summary>Official or installed version ID. Creating an instance does not download or validate the version.</summary>
    public string VersionId { get; init; } = "";
    /// <summary>Optional Java executable/home. Null selects Java from version metadata at launch.</summary>
    public string? JavaPath { get; init; }
    /// <summary>最小 Java 堆内存（MiB），默认 512；0 表示省略 -Xms，允许与最大内存不同。</summary>
    public int MinMemoryMb { get; init; } = 512;
    /// <summary>最大 Java 堆内存（MiB），默认 2048；按范围和最小值校验，不自动分配。</summary>
    public int MaxMemoryMb { get; init; } = 2048;
    /// <summary>自定义 JVM 参数，每项为完整参数；同值去重、冲突值拒绝。不要在参数中存放凭据。</summary>
    public IReadOnlyList<string> JvmArguments { get; init; } = [];
    /// <summary>Additional game tokens. Identity, shared-directory and version overrides are forbidden.</summary>
    public IReadOnlyList<string> GameArguments { get; init; } = [];
    /// <summary>持久化窗口宽高和全屏设置，复用 GameWindowConfig；启动时覆盖版本默认窗口参数。</summary>
    public GameWindowConfig Window { get; init; } = new();
    /// <summary>实例持久化的自动加入服务器设置；null 表示关闭。旧配置未包含此字段时保持关闭。</summary>
    public GameServerConfig? Server { get; init; }
    /// <summary>Does not expose paths or custom arguments.</summary>
    public override string ToString() => "Minecraft instance configuration";
}

/// <summary>Immutable instance snapshot. Read again to refresh the configuration and advisory usage state.</summary>
public sealed class InstanceInfo
{
    /// <summary>Stable directory ID. Display-name changes do not change it.</summary>
    public string Id { get; }
    /// <summary>Shared root containing versions, libraries, assets and instances.</summary>
    public string RootDirectory { get; }
    /// <summary>Absolute instance directory, always directly below RootDirectory/instances.</summary>
    public string GameDirectory { get; }
    /// <summary>Persisted, account-free settings.</summary>
    public InstanceConfig Config { get; }
    /// <summary>Optimistic concurrency token. UpdateAsync rejects stale snapshots.</summary>
    public Guid Revision { get; }
    /// <summary>Creation time in UTC.</summary>
    public DateTimeOffset CreatedAt { get; }
    /// <summary>Last successful configuration-write time in UTC.</summary>
    public DateTimeOffset UpdatedAt { get; }
    /// <summary>Snapshot of an SDK launch/preparation or conflicting operation. False does not reserve the instance.</summary>
    public bool IsInUse { get; }

    internal InstanceInfo(string id, string root, string directory, InstanceConfig config, Guid revision,
        DateTimeOffset createdAt, DateTimeOffset updatedAt, bool isInUse)
        => (Id, RootDirectory, GameDirectory, Config, Revision, CreatedAt, UpdatedAt, IsInUse) =
            (id, root, directory, config, revision, createdAt, updatedAt, isInUse);

    /// <summary>Creates a normal launch configuration using shared files and this isolated directory. Pass an account or set Accounts on the result.</summary>
    /// <remarks>The launcher rechecks this snapshot under the instance lock. Deleted or changed instances fail instead of being recreated silently.</remarks>
    public LaunchConfig CreateLaunchConfig(LaunchIdentity? account = null) => new()
    {
        GameCore = new() { RootDirectory = RootDirectory, VersionId = Config.VersionId, InstanceId = Id, ManagedInstance = this },
        Java = new() { JavaPath = Config.JavaPath, MinMemoryMb = Config.MinMemoryMb, MaxMemoryMb = Config.MaxMemoryMb, Arguments = Config.JvmArguments },
        Window = Config.Window,
        Server = Config.Server,
        GameArguments = Config.GameArguments,
        Account = account
    };
}

/// <summary>Structured instance-management failure category.</summary>
public enum InstanceErrorCode
{
    /// <summary>Invalid display name or launch settings.</summary>
    InvalidConfig,
    /// <summary>Unsafe ID, path, symbolic link or reparse point.</summary>
    UnsafePath,
    /// <summary>No managed instance exists with this ID.</summary>
    NotFound,
    /// <summary>The destination already exists, including an unmanaged game directory.</summary>
    AlreadyExists,
    /// <summary>An SDK launch/preparation or another mutating operation owns the instance.</summary>
    Busy,
    /// <summary>The configuration changed after the supplied snapshot was read.</summary>
    Conflict,
    /// <summary>Invalid, oversized, mismatched or unsupported instance document.</summary>
    StorageCorrupted,
    /// <summary>File access failed.</summary>
    IoError
}

/// <summary>Sanitized instance failure. Cancellation is reported separately as OperationCanceledException.</summary>
public sealed class InstanceException(InstanceErrorCode code, string message) : Exception(message)
{
    /// <summary>Machine-readable failure category.</summary>
    public InstanceErrorCode Code { get; } = code;
}

/// <summary>Deletion progress after the complete directory preflight. Cancellation may leave partially deleted data with its configuration retained for retry.</summary>
public sealed record InstanceDeletionProgress(int DeletedFiles, int TotalFiles);
