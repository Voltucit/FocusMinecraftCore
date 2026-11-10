using FMCore.Accounts;

namespace FMCore.Launch;

public sealed class LaunchConfig
{
    public required IMinecraftAccount Account { get; init; }

    public required GameCoreConfig GameCoreConfig { get; init; }

    public required JavaConfig JavaConfig { get; init; }



    public GameWindowConfig? GameWindowConfig { get; init; }

    /// <summary>版本 JSON 中非标准占位符的可选覆盖；标准占位符不会被覆盖。</summary>
    public IReadOnlyDictionary<string, string>? PlaceholderOverrides { get; init; }

    /// <summary>遇到未覆盖的占位符时回调名称；启动器可据此记录并在失败时提示用户。</summary>
    public Action<string>? UnknownPlaceholderReporter { get; init; }
}

public sealed class GameCoreConfig
{
    public required string Root { get; init; }

    public required string Version { get; init; }

    public string? GameDirectory { get; init; }

    public IEnumerable<string>? GameArguments { get; init; }
}

public sealed class JavaConfig
{
    public required string JavaPath { get; init; }

    public required int MaxMemoryMb { get; init; }

    public IEnumerable<string>? JvmArguments { get; init; }

    public string? AuthlibInjectorPath { get; init; }
}

public sealed class GameWindowConfig
{
    public int? Width { get; init; }

    public int? Height { get; init; }

    public bool Fullscreen { get; init; }
}
