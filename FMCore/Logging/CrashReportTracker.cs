using System.Text;
using FMCore.Downloader;
using FMCore.Utilities;

namespace FMCore.Logging;

// Tracks filenames present before process creation. Timestamps alone cannot distinguish historical files with future/modified dates.
internal sealed class CrashReportTracker
{
    private readonly string directory;
    private readonly LoggingOptions options;
    private readonly HashSet<string> baseline = new(GameFiles.PathComparer);
    private readonly List<string> warnings = [];
    private readonly DateTimeOffset started = DateTimeOffset.UtcNow;
    private bool complete = true;

    internal CrashReportTracker(string directory, LoggingOptions options)
    {
        this.directory = directory;
        this.options = options;
        if (!options.CollectCrashReports) return;
        try { foreach (var path in Candidates()) baseline.Add(path); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or DownloadException)
        { complete = false; warnings.Add("崩溃报告基线读取失败，已禁用本次自动报告归属判断。"); }
    }

    internal async Task<GameDiagnosticReport> CompleteAsync(LauncherLogger logger, int processId, int exitCode, bool stopped,
        CancellationToken ct)
    {
        var reports = new List<CrashReportInfo>();
        if (options.CollectCrashReports && complete)
        {
            try
            {
                var paths = Candidates().ToArray();
                if (complete)
                    foreach (var path in paths)
                    {
                        ct.ThrowIfCancellationRequested();
                        if (baseline.Contains(path)) continue;
                        DownloadPath.CheckLinks(path);
                        var modified = new DateTimeOffset(File.GetLastWriteTimeUtc(path));
                        if (modified < started.AddSeconds(-2) || modified > DateTimeOffset.UtcNow.AddSeconds(2)) continue;
                        var name = Path.GetFileName(path);
                        var pidFile = name.StartsWith("hs_err_pid", StringComparison.Ordinal);
                        if (pidFile && name != $"hs_err_pid{processId}.log") continue;
                        if (reports.Count == options.MaxCrashReports) { warnings.Add("新崩溃报告数量超过读取上限，其余报告未读取。"); break; }
                        var relative = Path.GetRelativePath(directory, path).Replace('\\', '/');
                        try
                        {
                            var (text, truncated) = await ReadAsync(path, ct).ConfigureAwait(false);
                            foreach (var line in text.Split('\n'))
                                logger.WriteCore(LogLevel.Error, line.TrimEnd('\r'), LogSource.CrashReport, relative, false);
                            reports.Add(new(logger.Redact(relative), modified, truncated, pidFile));
                        }
                        catch (Exception e) when (e is IOException or UnauthorizedAccessException or DownloadException)
                        { warnings.Add("新崩溃报告不可读取，原文件保持不变。"); }
                    }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or DownloadException)
            { warnings.Add("崩溃报告目录不可访问，诊断仅使用已捕获的日志。"); }
        }
        return GameDiagnostics.Analyze(logger.GetEntries(), exitCode, stopped) with
        { CrashReports = reports.AsReadOnly(), Warnings = Array.AsReadOnly(warnings.Distinct().ToArray()), DroppedEntries = logger.DroppedEntries };
    }

    private IEnumerable<string> Candidates()
    {
        DownloadPath.CheckLinks(directory);
        var count = 0;
        var crash = GameFiles.Resolve(directory, "crash-reports");
        var sources = new List<IEnumerable<string>>();
        if (Directory.Exists(crash)) sources.Add(Directory.EnumerateFiles(crash, "*.txt", SearchOption.TopDirectoryOnly));
        if (Directory.Exists(directory)) sources.Add(Directory.EnumerateFiles(directory, "hs_err_pid*.log", SearchOption.TopDirectoryOnly));
        foreach (var source in sources)
            foreach (var path in source)
            {
                if (++count > options.MaxReportCandidates)
                {
                    complete = false;
                    warnings.Add("报告扫描超过上限，已禁用本次自动报告归属判断，避免把未扫描的历史文件当作新报告。");
                    yield break;
                }
                DownloadPath.CheckLinks(path);
                yield return path;
            }
    }

    private async Task<(string Text, bool Truncated)> ReadAsync(string path, CancellationToken ct)
    {
        DownloadPath.CheckLinks(path);
        await using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 8192, FileOptions.Asynchronous);
        var bytes = new byte[options.MaxCrashReportBytes + 1];
        var count = 0;
        while (count < bytes.Length)
        {
            var read = await file.ReadAsync(bytes.AsMemory(count), ct).ConfigureAwait(false);
            if (read == 0) break;
            count += read;
        }
        var truncated = count > options.MaxCrashReportBytes;
        var text = Encoding.UTF8.GetString(bytes, 0, Math.Min(count, options.MaxCrashReportBytes));
        if (truncated)
        {
            var boundary = text.LastIndexOf('\n');
            text = boundary < 0 ? "[oversized report line omitted]" : text[..boundary];
        }
        return (text, truncated);
    }
}
