namespace FMCore.Java;

public enum JavaErrorCode
{
    InvalidPath, NotFound, ExecutionFailed, ProbeTimeout, InvalidOutput, OutputLimitExceeded,
    RequirementMissing, MajorVersionMismatch, ArchitectureMismatch, EarlyAccessNotAllowed, NoCompatibleRuntime, IoError,
    /// <summary>Runtime 查询或安装不支持当前平台。</summary>
    UnsupportedPlatform,
    /// <summary>Runtime 筛选或安装配置无效。</summary>
    RuntimeInvalidOptions,
    /// <summary>官方版本查询失败。</summary>
    RuntimeQueryFailed,
    /// <summary>官方版本查询超时。</summary>
    RuntimeQueryTimeout,
    /// <summary>官方元数据缺少安全安装所需信息。</summary>
    RuntimeMetadataInvalid,
    /// <summary>没有符合条件的官方包。</summary>
    RuntimeNotAvailable,
    /// <summary>包下载失败。</summary>
    RuntimeDownloadFailed,
    /// <summary>包哈希或大小不符。</summary>
    RuntimeIntegrityFailed,
    /// <summary>ZIP 格式、路径或解压规模无效。</summary>
    RuntimeArchiveInvalid,
    /// <summary>实际运行验证与所选包不符。</summary>
    RuntimeValidationFailed,
    /// <summary>同一托管包正在执行操作。</summary>
    RuntimeBusy,
    /// <summary>存在不属于该安装的文件或所有权冲突。</summary>
    RuntimeConflict,
    /// <summary>中断恢复未完成，需保留恢复记录并重试。</summary>
    RuntimeRecoveryFailed,
    /// <summary>安装目录包含链接或不安全路径。</summary>
    RuntimeUnsafePath
}

/// <summary>Does not retain child-process output, environment values, or native exception messages.</summary>
public sealed class JavaException(JavaErrorCode code, string message, int? exitCode = null) : Exception(message)
{
    public JavaErrorCode Code { get; } = code;
    public int? ExitCode { get; } = exitCode;
}
