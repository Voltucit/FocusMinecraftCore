using System.Runtime.InteropServices;
using FMCore.Versions;

namespace FMCore.Java;

public enum JavaArchitecture { Unknown, X86, X64, Arm32, Arm64 }

public sealed record JavaRuntime(string ExecutablePath, string HomeDirectory, JavaVersion Version,
    JavaArchitecture Architecture, string? Vendor)
{
    public RuleContext GetRuleContext(IReadOnlyDictionary<string, bool>? features = null) => RuleContext.Current with
    {
        Architecture = Architecture switch
        {
            JavaArchitecture.X86 => "x86",
            JavaArchitecture.X64 => "amd64",
            JavaArchitecture.Arm32 => "arm",
            JavaArchitecture.Arm64 => "aarch64",
            _ => throw new JavaException(JavaErrorCode.ArchitectureMismatch, "无法为未知 Java 架构构建规则上下文。")
        },
        Features = features
    };
}

public sealed record JavaSelectionOptions
{
    public JavaArchitecture TargetArchitecture { get; init; } = RuntimeInformation.OSArchitecture switch
    {
        Architecture.X86 => JavaArchitecture.X86,
        Architecture.X64 => JavaArchitecture.X64,
        Architecture.Arm => JavaArchitecture.Arm32,
        Architecture.Arm64 => JavaArchitecture.Arm64,
        _ => JavaArchitecture.Unknown
    };
    /// <summary>Only used when effective version metadata has no javaVersion. Never overrides metadata.</summary>
    public int? FallbackMajorVersion { get; init; }
    public bool AllowEarlyAccess { get; init; }

    internal void Validate()
    {
        if (!Enum.IsDefined(TargetArchitecture) || TargetArchitecture == JavaArchitecture.Unknown)
            throw new ArgumentOutOfRangeException(nameof(TargetArchitecture));
        if (FallbackMajorVersion is < 1 or > 999) throw new ArgumentOutOfRangeException(nameof(FallbackMajorVersion));
    }
}

public sealed record JavaDetectionOptions
{
    public TimeSpan ProbeTimeout { get; init; } = TimeSpan.FromSeconds(5);
    public int MaxOutputCharacters { get; init; } = 64 * 1024;
    public int MaxCandidates { get; init; } = 128;
    public string? JavaHome { get; init; } = Environment.GetEnvironmentVariable("JAVA_HOME");
    public IReadOnlyList<string> PathDirectories { get; init; } =
        (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
    /// <summary>Each root, its immediate children, and macOS Contents/Home are checked. No recursive disk scan.</summary>
    public IReadOnlyList<string> SearchDirectories { get; init; } = JavaDiscovery.CommonDirectories();
}

public sealed record JavaDetectionIssue(string? Path, JavaErrorCode Code, int? ExitCode = null);
public sealed record JavaDetectionProgress(int Completed, int Total, string? ExecutablePath, JavaErrorCode? ErrorCode);
public sealed record JavaDetectionResult(IReadOnlyList<JavaRuntime> Runtimes, IReadOnlyList<JavaDetectionIssue> Issues,
    bool IsTruncated);
public sealed record JavaCompatibilityResult(bool IsCompatible, JavaErrorCode? ErrorCode, int? RequiredMajorVersion,
    JavaArchitecture RequiredArchitecture);
