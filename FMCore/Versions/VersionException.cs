namespace FMCore.Versions;

public enum VersionErrorCode
{
    InvalidMetadata,
    UnsafePath,
    NotFound,
    NotCached,
    InheritanceCycle,
    InheritanceTooDeep,
    RuleEvaluation,
    IoError,
    /// <summary>多个目录声明同一 JSON ID，继承或别名查询无法唯一定位。</summary>
    ConflictingProfiles
}

/// <summary>Metadata errors omit raw JSON, service URLs and underlying exception text.</summary>
public sealed class VersionException : Exception
{
    public VersionErrorCode Code { get; }
    internal VersionException(VersionErrorCode code, string message) : base(message) => Code = code;
}

public enum VersionCachePolicy
{
    PreferCache,
    Refresh,
    CacheOnly
}
