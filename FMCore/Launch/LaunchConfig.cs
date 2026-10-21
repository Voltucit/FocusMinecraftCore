using System.Diagnostics;
using System.Text.Json.Serialization;
using FMCore.Authentication;
using FMCore.Downloader;
using FMCore.Java;
using FMCore.Versions;

namespace FMCore.Launch;

/// <summary>Shared game files and the working directory for one instance. Instance directories stay inside RootDirectory.</summary>
public sealed record GameCoreConfig
{
    /// <summary>Shared versions, libraries and assets root. Defaults to .minecraft under the current directory.</summary>
    public string RootDirectory { get; init; } = ".minecraft";
    /// <summary>The installed or official version ID, for example 1.16.5.</summary>
    public string VersionId { get; init; } = "";
    /// <summary>Use instances/{InstanceId or VersionId} for settings, saves and logs. Enabled by default.</summary>
    public bool Isolate { get; init; } = true;
    /// <summary>Optional safe instance name; defaults to VersionId.</summary>
    public string? InstanceId { get; init; }
    /// <summary>Explicit working directory relative to RootDirectory; overrides automatic isolation.</summary>
    public string? GameDirectory { get; init; }
    /// <summary>Automatically install/repair files before starting. False uses only cached metadata and verified local downloads.</summary>
    public bool AutoInstall { get; init; } = true;
    /// <summary>Metadata cache policy when automatic installation is enabled.</summary>
    public VersionCachePolicy CachePolicy { get; init; } = VersionCachePolicy.PreferCache;
}

/// <summary>Java selection and memory settings. Each additional JVM entry is one argument, not a shell command.</summary>
[DebuggerDisplay("Java launch configuration")]
public sealed record JavaConfig
{
    /// <summary>Explicit Java executable/home. Takes priority over discovery; an incompatible path fails without fallback.</summary>
    public string? JavaPath { get; init; }
    /// <summary>Initial heap in MiB, default 512.</summary>
    public int MinMemoryMb { get; init; } = 512;
    /// <summary>Maximum heap in MiB, default 2048.</summary>
    public int MaxMemoryMb { get; init; } = 2048;
    /// <summary>Additional JVM switches. Use complete switches such as -Dkey=value; memory/classpath/main-class overrides are rejected.</summary>
    [JsonIgnore] public IReadOnlyList<string> Arguments { get; init; } = [];
    /// <summary>Advanced compatibility policy; metadata requirements are authoritative.</summary>
    public JavaSelectionOptions Selection { get; init; } = new();
    /// <summary>Bounded runtime discovery/probe settings.</summary>
    public JavaDetectionOptions Detection { get; init; } = new();
    /// <summary>Does not expose custom JVM arguments.</summary>
    public override string ToString() => "Java launch configuration";
}

/// <summary>Initial game window settings; supported modern/legacy argument forms are handled automatically.</summary>
public sealed record GameWindowConfig
{
    /// <summary>Window width in pixels, default 854.</summary>
    public int Width { get; init; } = 854;
    /// <summary>Window height in pixels, default 480.</summary>
    public int Height { get; init; } = 480;
    /// <summary>Start in fullscreen mode.</summary>
    public bool Fullscreen { get; init; }
}

/// <summary>Unified, UI-independent launch configuration. Account is a T05 LaunchIdentity; Accounts optionally refreshes it just before launch.</summary>
[DebuggerDisplay("Minecraft launch configuration")]
public sealed record LaunchConfig
{
    /// <summary>Version, shared directory and instance settings.</summary>
    public GameCoreConfig GameCore { get; init; } = new();
    /// <summary>Manual/automatic Java selection, memory and extra JVM switches.</summary>
    public JavaConfig Java { get; init; } = new();
    /// <summary>Window size and fullscreen settings.</summary>
    public GameWindowConfig Window { get; init; } = new();
    /// <summary>Microsoft or offline identity returned by AccountManager. Never persists launch credentials.</summary>
    [JsonIgnore] public LaunchIdentity? Account { get; init; }
    /// <summary>Optional caller-owned account manager. Refreshes Account or uses its currently selected account when Account is null.</summary>
    [JsonIgnore] public AccountManager? Accounts { get; init; }
    /// <summary>Additional game argument tokens; identity, version and directory overrides are rejected.</summary>
    [JsonIgnore] public IReadOnlyList<string> GameArguments { get; init; } = [];
    /// <summary>Optional version feature conditions. has_custom_resolution is derived from Window.</summary>
    public IReadOnlyDictionary<string, bool> Features { get; init; } = new Dictionary<string, bool>();
    /// <summary>Download concurrency/retry settings for automatic installation.</summary>
    public DownloadOptions Downloads { get; init; } = new();
    /// <summary>Launcher brand substituted in official metadata.</summary>
    public string LauncherName { get; init; } = "FMCore";
    /// <summary>Launcher version substituted in official metadata.</summary>
    public string LauncherVersion { get; init; } = "0.1.0";
    /// <summary>Does not expose accounts or custom launch arguments.</summary>
    public override string ToString() => "Minecraft launch configuration";
}
