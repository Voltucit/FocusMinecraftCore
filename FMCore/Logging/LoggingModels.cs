using FMCore.Launch;

namespace FMCore.Logging;

/// <summary>SDK log severity; game stderr does not by itself prove a fatal error.</summary>
public enum LogLevel { Debug, Information, Warning, Error }
/// <summary>Origin of preserved, sanitized evidence.</summary>
public enum LogSource { Launcher, StandardOutput, StandardError, CrashReport }
/// <summary>Structured logging failures. Automatic diagnostic collection records warnings instead of failing an owned game.</summary>
public enum LoggingErrorCode { InvalidOptions, UnsafePath, IoError }
/// <summary>Logging failure without raw content or inner exception details.</summary>
public sealed class LoggingException(LoggingErrorCode code, string message) : Exception(message)
{
    /// <summary>Machine-readable failure category.</summary>
    public LoggingErrorCode Code { get; } = code;
}

/// <summary>Bounded in-memory logs and automatic, current-session crash-report collection. No log file is written automatically.</summary>
public sealed record LoggingOptions
{
    /// <summary>Maximum retained log entries, 1–10000; old entries are evicted.</summary>
    public int MaxEntries { get; init; } = 2000;
    /// <summary>Read new reports in this game's crash-reports directory and matching hs_err_pid files after exit.</summary>
    public bool CollectCrashReports { get; init; } = true;
    /// <summary>Maximum candidate paths enumerated per session scan, 1–4096. An incomplete baseline disables automatic attribution.</summary>
    public int MaxReportCandidates { get; init; } = 512;
    /// <summary>Maximum new crash reports read per session, 1–32.</summary>
    public int MaxCrashReports { get; init; } = 8;
    /// <summary>Maximum bytes read from each report, 1024–16777216. Partial final lines are omitted.</summary>
    public int MaxCrashReportBytes { get; init; } = 1024 * 1024;

    internal LoggingOptions Validate()
    {
        if (MaxEntries is < 1 or > 10000 || MaxReportCandidates is < 1 or > 4096 || MaxCrashReports is < 1 or > 32 ||
            MaxCrashReportBytes is < 1024 or > 16 * 1024 * 1024)
            throw new LoggingException(LoggingErrorCode.InvalidOptions, "日志容量或崩溃报告读取限制无效。");
        return this with { };
    }
}

/// <summary>Immutable sanitized entry. Message text is retained verbatim except credential/ANSI filtering and explicit length limits.</summary>
public sealed class LogEntry : EventArgs
{
    /// <summary>UTC capture time.</summary>
    public DateTimeOffset Timestamp { get; }
    /// <summary>Log severity.</summary>
    public LogLevel Level { get; }
    /// <summary>Evidence origin.</summary>
    public LogSource Source { get; }
    /// <summary>Sanitized text; oversized lines are omitted in full.</summary>
    public string Message { get; }
    /// <summary>Whether an oversized original line was omitted.</summary>
    public bool IsTruncated { get; }
    /// <summary>Optional sanitized report path, relative to the game directory.</summary>
    public string? FileName { get; }
    internal LogEntry(DateTimeOffset timestamp, LogLevel level, LogSource source, string message, bool truncated, string? fileName)
        => (Timestamp, Level, Source, Message, IsTruncated, FileName) = (timestamp, level, source, message, truncated, fileName);
}

/// <summary>Evidence categories, not proof of the game's ultimate exit cause.</summary>
public enum DiagnosticCode { Unknown, OutOfMemory, JavaIncompatible, MissingDependency, NativeLibraryError }
/// <summary>A detected signal, suggested checks and the exact sanitized entries supporting it.</summary>
public sealed record DiagnosticFinding(DiagnosticCode Code, string Message, string Recommendation, IReadOnlyList<LogEntry> Evidence);
/// <summary>A newly observed report candidate. It is not automatically proof that the file belongs to the game process.</summary>
public sealed record CrashReportInfo(string RelativePath, DateTimeOffset LastWriteTime, bool IsTruncated, bool ProcessIdMatched);
/// <summary>Bounded diagnostics retaining evidence and collection limitations without guessing a cause from the exit code.</summary>
public sealed record GameDiagnosticReport(int? ExitCode, bool StoppedByCaller, string Summary,
    IReadOnlyList<DiagnosticFinding> Findings, IReadOnlyList<LogEntry> Evidence)
{
    /// <summary>New report candidates from this session. Pre-existing paths are excluded even if modified.</summary>
    public IReadOnlyList<CrashReportInfo> CrashReports { get; init; } = [];
    /// <summary>Unreadable paths, limits or other evidence-collection limitations.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
    /// <summary>Entries evicted from the bounded log buffer.</summary>
    public long DroppedEntries { get; init; }
    /// <summary>Optional structured preparation failure; no process may have been created.</summary>
    public LaunchError? PreparationError { get; init; }
}
