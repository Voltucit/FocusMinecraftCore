namespace FMCore.Java;

public enum JavaErrorCode
{
    InvalidPath, NotFound, ExecutionFailed, ProbeTimeout, InvalidOutput, OutputLimitExceeded,
    RequirementMissing, MajorVersionMismatch, ArchitectureMismatch, EarlyAccessNotAllowed, NoCompatibleRuntime, IoError
}

/// <summary>Does not retain child-process output, environment values, or native exception messages.</summary>
public sealed class JavaException(JavaErrorCode code, string message, int? exitCode = null) : Exception(message)
{
    public JavaErrorCode Code { get; } = code;
    public int? ExitCode { get; } = exitCode;
}
