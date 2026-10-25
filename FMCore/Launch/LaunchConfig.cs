using FMCore.Accounts;
using FMCore.Internal;
using FMCore.Java;
using FMCore.Versions;

namespace FMCore.Launch;

/// <summary>启动设置。目前只组合和校验配置，尚未执行 Minecraft。</summary>
public sealed class LaunchConfig
{
    public MinecraftVersion Version { get; }
    public JavaInfo Java { get; }
    public OfflineAccount Account { get; }
    public int MaxMemoryMb { get; }
    public string GameDirectory { get; }

    public LaunchConfig(MinecraftVersion version, JavaInfo java, OfflineAccount account,
        int maxMemoryMb, string? gameDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(java);
        ArgumentNullException.ThrowIfNull(account);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxMemoryMb);
        Version = version;
        Java = java;
        Account = account;
        MaxMemoryMb = maxMemoryMb;
        GameDirectory = PathSafety.RequireDirectoryPath(gameDirectory ?? version.Directory);
    }
}
