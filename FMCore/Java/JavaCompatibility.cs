using FMCore.Versions;

namespace FMCore.Java;

public static class JavaCompatibility
{
    /// <summary>Pass resolved metadata for inherited versions. Compatibility is a conservative metadata check, not a game-launch guarantee.</summary>
    public static JavaCompatibilityResult Check(JavaRuntime runtime, VersionMetadata version, JavaSelectionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(version);
        options ??= new();
        options.Validate();
        var major = version.Java?.MajorVersion ?? options.FallbackMajorVersion;
        var error = major == null ? JavaErrorCode.RequirementMissing
            : runtime.Version.Major != major ? JavaErrorCode.MajorVersionMismatch
            : runtime.Architecture != options.TargetArchitecture ? JavaErrorCode.ArchitectureMismatch
            : runtime.Version.IsEarlyAccess && !options.AllowEarlyAccess ? JavaErrorCode.EarlyAccessNotAllowed
            : (JavaErrorCode?)null;
        return new(error == null, error, major, options.TargetArchitecture);
    }

    internal static JavaException Failure(JavaErrorCode code) => new(code, code switch
    {
        JavaErrorCode.RequirementMissing => "版本元数据缺少 Java 要求，请解析版本继承，或明确设置缺失要求时的 Java 主版本。",
        JavaErrorCode.MajorVersionMismatch => "Java 主版本与游戏版本要求不一致。",
        JavaErrorCode.ArchitectureMismatch => "Java 架构与目标架构不一致。",
        JavaErrorCode.EarlyAccessNotAllowed => "当前选择策略未允许 Java 预发布版本。",
        _ => "未检测到满足版本和架构要求的 Java。"
    });
}
