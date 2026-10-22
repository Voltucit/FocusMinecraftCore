using System.Text.Json;
using FMCore.Downloader;
using FMCore.Utilities;

namespace FMCore.Logging;

/// <summary>UI-independent bounded logging with credential filtering, real-time events and optional atomic JSONL export.</summary>
public sealed class LauncherLogger
{
    private const int MaxLine = 16 * 1024;
    private readonly object sync = new();
    private readonly Queue<LogEntry> entries = new();
    private readonly HashSet<string> secrets = new(StringComparer.Ordinal);
    private readonly int capacity;
    private SensitiveDataFilter filter;
    private long dropped;
    /// <summary>Fires on the writing thread. Exceptions from subscribers are isolated; handlers must not block game I/O.</summary>
    public event EventHandler<LogEntry>? EntryReceived;
    /// <summary>Number of entries discarded since the most recent launch session/reset.</summary>
    public long DroppedEntries { get { lock (sync) return dropped; } }

    /// <summary>Creates a logger. Optional caller-known credentials are filtered even when printed without a field name.</summary>
    public LauncherLogger(LoggingOptions? options = null, IEnumerable<string>? sensitiveValues = null)
    {
        capacity = (options ?? new()).Validate().MaxEntries;
        foreach (var value in sensitiveValues ?? []) if (!string.IsNullOrEmpty(value)) secrets.Add(value);
        filter = new(secrets);
    }

    /// <summary>Writes one entry; generic token/password fields, bearer tokens and supplied sensitive values are filtered before storage or events.</summary>
    public LogEntry Write(LogLevel level, string message, LogSource source = LogSource.Launcher)
        => WriteCore(level, message, source, null, false);

    /// <summary>Returns an immutable snapshot. A new launch clears the owning launcher's buffer to exclude older sessions.</summary>
    public IReadOnlyList<LogEntry> GetEntries() { lock (sync) return Array.AsReadOnly(entries.ToArray()); }

    /// <summary>Atomically exports the current sanitized snapshot as UTF-8 JSONL to an explicitly chosen file. Existing contents are replaced; symlinks are rejected.</summary>
    /// <exception cref="LoggingException">UnsafePath or IoError. Cancellation preserves any previous complete file.</exception>
    public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var full = Path.GetFullPath(path);
            DownloadPath.CheckLinks(full);
            var snapshot = GetEntries();
            await GameFiles.WriteAtomicAsync(full, async (output, ct) =>
            {
                foreach (var entry in snapshot)
                {
                    ct.ThrowIfCancellationRequested();
                    await JsonSerializer.SerializeAsync(output, entry, cancellationToken: ct).ConfigureAwait(false);
                    await output.WriteAsync(new byte[] { (byte)'\n' }, ct).ConfigureAwait(false);
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (e is DownloadException or ArgumentException or NotSupportedException)
        { throw new LoggingException(LoggingErrorCode.UnsafePath, "日志导出路径无效或包含链接。"); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { throw new LoggingException(LoggingErrorCode.IoError, "无法导出日志文件。"); }
    }

    internal string Redact(string text) { lock (sync) return filter.Redact(text); }
    internal void AddSecrets(params string[] values)
    {
        lock (sync)
        {
            foreach (var value in values) if (!string.IsNullOrEmpty(value)) secrets.Add(value);
            filter = new(secrets);
            var previous = entries.ToArray();
            entries.Clear();
            foreach (var entry in previous) entries.Enqueue(new(entry.Timestamp, entry.Level, entry.Source, filter.Redact(entry.Message), entry.IsTruncated,
                entry.FileName == null ? null : filter.Redact(entry.FileName)));
        }
    }
    internal void BeginSession() { lock (sync) { entries.Clear(); dropped = 0; } }
    internal LogEntry WriteCore(LogLevel level, string message, LogSource source, string? fileName, bool truncated)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!Enum.IsDefined(level) || !Enum.IsDefined(source)) throw new ArgumentOutOfRangeException(nameof(level));
        LogEntry entry;
        lock (sync)
        {
            truncated |= message.Length > MaxLine;
            entry = new(DateTimeOffset.UtcNow, level, source,
                truncated ? "[oversized log line omitted]" : filter.Redact(message), truncated,
                fileName == null ? null : filter.Redact(fileName));
            if (entries.Count == capacity) { entries.Dequeue(); dropped++; }
            entries.Enqueue(entry);
        }
        foreach (EventHandler<LogEntry> handler in EntryReceived?.GetInvocationList() ?? [])
            try { handler(this, entry); } catch (Exception) { /* Host log sinks cannot invalidate a game process. */ }
        return entry;
    }
}
