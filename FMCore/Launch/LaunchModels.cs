using FMCore.Installer;

namespace FMCore.Launch;

/// <summary>Lifecycle states. Running means the OS process started, not that the Minecraft main menu is ready.</summary>
public enum LaunchState { Idle, Preparing, Starting, Running, Stopping, Exited, Failed, Canceled }
/// <summary>Preparation stages; installation progress contains detailed download/file counts.</summary>
public enum LaunchStage { Metadata, Java, Account, Installation, Arguments, Process }
/// <summary>Stable launch failure categories. DetailCode in LaunchError retains the originating module's enum name.</summary>
public enum LaunchErrorCode { InvalidConfig, AlreadyRunning, InstanceBusy, VersionError, JavaError, AuthenticationError, InstallationError, MissingFiles, InvalidArguments, UnsafePath, ProcessStartFailed, ProcessControlFailed, IoError, UnexpectedError }
/// <summary>Sanitized structured failure, with no raw command line, credentials or inner exceptions.</summary>
public sealed record LaunchError(LaunchErrorCode Code, string Message, string? DetailCode = null);
/// <summary>Thrown for invalid configuration or process-control misuse; LaunchAsync reports operational failures in its result.</summary>
public sealed class LaunchException(LaunchErrorCode code, string message) : Exception(message)
{
    /// <summary>Machine-readable failure category.</summary>
    public LaunchErrorCode Code { get; } = code;
}
/// <summary>Process creation outcome. Success does not promise successful rendering or server authentication.</summary>
public sealed record LaunchResult(bool Success, int? ProcessId, LaunchError? Error);
/// <summary>Actual process completion, after redirected output has drained.</summary>
public sealed record GameExitResult(int ProcessId, int ExitCode, bool StoppedByCaller, DateTimeOffset ExitedAt);
/// <summary>Preparation progress and optional detailed installer progress.</summary>
public sealed record LaunchProgress(LaunchStage Stage, InstallationProgress? Installation = null);
/// <summary>Lifecycle notification. Handlers must return promptly and marshal UI updates themselves.</summary>
public sealed class LaunchStateChangedEventArgs(LaunchState state, LaunchError? error = null) : EventArgs
{
    /// <summary>New lifecycle state.</summary>
    public LaunchState State { get; } = state;
    /// <summary>Sanitized failure when entering Failed.</summary>
    public LaunchError? Error { get; } = error;
}
/// <summary>A sanitized output line. Excessively long lines are omitted rather than emitting partial secrets.</summary>
public sealed class GameLogEventArgs(string text, bool isTruncated = false) : EventArgs
{
    /// <summary>Redacted text, without a newline.</summary>
    public string Text { get; } = text;
    /// <summary>Whether an oversized line was replaced.</summary>
    public bool IsTruncated { get; } = isTruncated;
    /// <summary>UTC time when the SDK forwarded the line.</summary>
    public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;
}
/// <summary>Process exit notification after stdout/stderr drainage and release of instance resources.</summary>
public sealed class GameExitedEventArgs(GameExitResult result) : EventArgs
{
    /// <summary>Exit code and whether StopAsync requested termination.</summary>
    public GameExitResult Result { get; } = result;
}
