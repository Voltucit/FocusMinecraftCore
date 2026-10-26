using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using FMCore.Installer;

namespace FMCore.Java;

/// <summary>查询本机 Java。不会推荐、安装 Java 或修改环境变量。</summary>
public static class JavaReader
{
    public static Task<JavaInfo> GetJavaInfoAsync(string javaPath, CancellationToken cancellationToken = default)
        => GetJavaInfoCoreAsync(javaPath, ReadOutputAsync, cancellationToken);

    public static async Task<IReadOnlyList<JavaInfo>> GetJavasAsync(IEnumerable<string>? searchPaths = null,
        Action<string, Exception>? onError = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<string> candidates = await Task.Run(
            () => JavaDiscovery.FindCandidates(searchPaths), cancellationToken).ConfigureAwait(false);
        return await ProbeCandidatesAsync(candidates, GetJavaInfoAsync, onError, cancellationToken).ConfigureAwait(false);
    }

    internal static async Task<IReadOnlyList<JavaInfo>> ProbeCandidatesAsync(IEnumerable<string> candidates,
        Func<string, CancellationToken, Task<JavaInfo>> probe, Action<string, Exception>? onError,
        CancellationToken cancellationToken)
    {
        List<JavaInfo> results = [];
        foreach (string candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try { results.Add(await probe(candidate, cancellationToken).ConfigureAwait(false)); }
            catch (Exception error) when (error is IOException or InvalidOperationException
                or Win32Exception or TimeoutException or UnauthorizedAccessException or InvalidDataException)
            {
                if (onError is null)
                    Trace.TraceWarning("Java 探测失败：{0}；{1}", candidate, error.Message);
                else
                    onError(candidate, error);
            }
        }
        return results;
    }

    internal static async Task<JavaInfo> GetJavaInfoCoreAsync(string javaPath,
        Func<string, bool, CancellationToken, Task<string>> probe, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string executable = PathSafety.Resolve(javaPath);
        string output;
        try { output = await probe(executable, true, cancellationToken).ConfigureAwait(false); }
        catch (JavaExitCodeException)
        {
            output = await probe(executable, false, cancellationToken).ConfigureAwait(false);
        }
        return ParseOutput(executable, output);
    }

    internal static JavaInfo ParseOutput(string executable, string output)
    {
        Match versionMatch = Regex.Match(output, "^(?:openjdk|java)\\s+version\\s+\"([^\"]+)\"",
            RegexOptions.Multiline | RegexOptions.CultureInvariant);
        if (!versionMatch.Success)
            throw new InvalidDataException($"无法识别 Java 版本信息：{executable}");
        string version = versionMatch.Groups[1].Value;
        string numbers = version.StartsWith("1.", StringComparison.Ordinal) ? version[2..] : version;
        Match majorMatch = Regex.Match(numbers, @"^(\d+)(?:[._+\-]|$)", RegexOptions.CultureInvariant);
        if (!majorMatch.Success || !int.TryParse(majorMatch.Groups[1].Value, out int major) || major < 1)
            throw new InvalidDataException($"Java 主版本无效：{version}");

        string? architecture = null;
        foreach (string line in output.Split('\n'))
        {
            int equals = line.IndexOf('=');
            if (equals < 0 || line[..equals].Trim() != "os.arch")
                continue;
            architecture = line[(equals + 1)..].Trim().ToLowerInvariant() switch
            {
                "x86" or "i386" or "i486" or "i586" or "i686" => "x86",
                "amd64" or "x86_64" or "x64" => "x64",
                "aarch64" or "arm64" => "arm64",
                _ => null
            };
            break;
        }
        return new JavaInfo(executable, version, major, architecture);
    }

    private static async Task<string> ReadOutputAsync(string executable, bool includeProperties,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(executable))
            throw new FileNotFoundException("找不到 Java 可执行文件。", executable);
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        using CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        ProcessStartInfo start = new(executable)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8
        };
        foreach (string key in new[] { "JAVA_TOOL_OPTIONS", "JDK_JAVA_OPTIONS", "_JAVA_OPTIONS" })
            start.Environment.Remove(key);
        if (includeProperties)
            start.ArgumentList.Add("-XshowSettings:properties");
        start.ArgumentList.Add("-version");
        using Process process = new() { StartInfo = start };
        process.Start();
        Task<string> stdout = process.StandardOutput.ReadToEndAsync(linked.Token);
        Task<string> stderr = process.StandardError.ReadToEndAsync(linked.Token);
        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            string[] outputs = await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
            if (process.ExitCode != 0)
                throw new JavaExitCodeException($"Java 信息查询失败（退出码 {process.ExitCode}）：{executable}");
            string output = string.Join('\n', outputs).Trim();
            if (output.Length == 0)
                throw new InvalidDataException($"Java 未返回版本信息：{executable}");
            return output;
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            try { await Task.WhenAll(stdout, stderr).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            cancellationToken.ThrowIfCancellationRequested();
            throw new TimeoutException($"Java 信息查询超过 10 秒：{executable}");
        }
    }

    internal sealed class JavaExitCodeException(string message) : InvalidOperationException(message);
}
