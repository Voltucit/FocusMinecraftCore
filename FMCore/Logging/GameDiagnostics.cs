using FMCore.Launch;

namespace FMCore.Logging;

/// <summary>Conservative signal matching. Findings are evidence-backed candidates, never a cause guessed from a numeric exit code.</summary>
public static class GameDiagnostics
{
    /// <summary>Analyzes sanitized logger entries. Original evidence is retained even when no known error can be recognized.</summary>
    public static GameDiagnosticReport Analyze(IEnumerable<LogEntry> entries, int? exitCode = null, bool stoppedByCaller = false,
        LaunchError? launchError = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(entries);
        var evidence = new List<LogEntry>();
        var groups = new Dictionary<DiagnosticCode, List<LogEntry>>();
        foreach (var entry in entries.Take(10000))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (entry == null) continue;
            evidence.Add(entry);
            if (entry.Source == LogSource.Launcher) continue; // SDK/host explanatory text is not JVM error evidence.
            var text = entry.Message;
            if (Contains("java.lang.OutOfMemoryError") || Contains("Could not reserve enough space") || Contains("insufficient memory for the Java Runtime Environment")) Add(DiagnosticCode.OutOfMemory);
            if (Contains("UnsupportedClassVersionError") || Contains("compiled by a more recent version of the Java Runtime")) Add(DiagnosticCode.JavaIncompatible);
            if (Contains("ClassNotFoundException") || Contains("NoClassDefFoundError") && !Contains("Could not initialize class")) Add(DiagnosticCode.MissingDependency);
            if (Contains("UnsatisfiedLinkError") || Contains("Failed to locate library: lwjgl") || Contains("Can't load AMD 64-bit .dll on a IA 32-bit platform")) Add(DiagnosticCode.NativeLibraryError);
            bool Contains(string value) => text.Contains(value, StringComparison.OrdinalIgnoreCase);
            void Add(DiagnosticCode code) { if (!groups.TryGetValue(code, out var list)) groups[code] = list = []; if (list.Count < 8) list.Add(entry); }
        }
        if (launchError is { Code: LaunchErrorCode.JavaError, DetailCode: "MajorVersionMismatch" or "ArchitectureMismatch" or "EarlyAccessNotAllowed" or "NoCompatibleRuntime" })
            groups.TryAdd(DiagnosticCode.JavaIncompatible, []);
        var findings = groups.Select(pair => Finding(pair.Key, pair.Value.AsReadOnly())).ToArray();
        if (findings.Length == 0) findings = [Finding(DiagnosticCode.Unknown, Array.AsReadOnly(evidence.TakeLast(8).ToArray()))];
        var summary = stoppedByCaller ? "进程由启动器停止；日志信号不等同于停止原因。"
            : findings[0].Code == DiagnosticCode.Unknown ? "证据不足，无法可靠判断原因；请保留脱敏日志和退出信息。"
            : "发现错误信号，请结合证据排查；这些信号不证明最终退出原因。";
        return new(exitCode, stoppedByCaller, summary, Array.AsReadOnly(findings), evidence.AsReadOnly()) { PreparationError = launchError };
    }

    private static DiagnosticFinding Finding(DiagnosticCode code, IReadOnlyList<LogEntry> evidence) => code switch
    {
        DiagnosticCode.OutOfMemory => new(code, "日志出现内存分配失败信号。", "检查可用内存、堆上限、原生内存和线程数量；不要仅凭此信号盲目增加 Xmx。", evidence),
        DiagnosticCode.JavaIncompatible => new(code, "日志或结构化检查显示 Java/类文件兼容性问题。", "按版本元数据核对 Java 主版本、架构和预览版策略，不直接换成最新 Java。", evidence),
        DiagnosticCode.MissingDependency => new(code, "日志出现类加载失败信号。", "先校验安装清单中的依赖与 Classpath，再检查上游异常和加载器兼容性。", evidence),
        DiagnosticCode.NativeLibraryError => new(code, "日志出现原生库加载失败信号。", "核对平台与 Java 架构，检查原生压缩包及解压文件；系统库或驱动问题仍需进一步证据。", evidence),
        _ => new(DiagnosticCode.Unknown, "没有足够证据确定已知原因。", "保留原始游戏文件、脱敏证据和退出信息进行人工分析。", evidence)
    };
}
