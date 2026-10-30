using FMCore.Accounts;

namespace FMCore.Launch;

public sealed class LaunchConfig
{
    public required IMinecraftAccount Account { get; init; }

    public required GameCoreConfig GameCoreConfig { get; init; }

    public required JavaConfig JavaConfig { get; init; }



    public GameWindowConfig? GameWindowConfig { get; init; }
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
