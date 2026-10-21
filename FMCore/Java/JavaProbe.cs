using System.Diagnostics;
using System.Text;

namespace FMCore.Java;

internal static class JavaProbe
{
    internal static async Task<JavaRuntime> ProbeAsync(string path, JavaDetectionOptions options, CancellationToken cancellationToken)
    {
        var executable = ResolveExecutable(path);
        var start = new ProcessStartInfo(executable)
        {
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        start.ArgumentList.Add("-Dfile.encoding=UTF-8");
        start.ArgumentList.Add("-Dstdout.encoding=UTF-8");
        start.ArgumentList.Add("-Dstderr.encoding=UTF-8");
        start.ArgumentList.Add("-XshowSettings:properties");
        start.ArgumentList.Add("-version");
        // Probe only the runtime itself; inherited options can inject agents, spoof properties, or expose secrets.
        ClearInheritedOptions(start);
        var output = await JavaProcess.RunAsync(start, options.ProbeTimeout, options.MaxOutputCharacters, cancellationToken).ConfigureAwait(false);
        if (output.ExitCode != 0) throw new JavaException(JavaErrorCode.ExecutionFailed, "Java 检测进程返回非零退出码。", output.ExitCode);
        return Parse(executable, output.StandardOutput + "\n" + output.StandardError);
    }

    internal static void ClearInheritedOptions(ProcessStartInfo start)
    {
        foreach (var key in new[] { "JAVA_TOOL_OPTIONS", "JDK_JAVA_OPTIONS", "_JAVA_OPTIONS", "CLASSPATH" }) start.Environment.Remove(key);
    }

    internal static string ResolveExecutable(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || path.Any(char.IsControl) || path.Contains('"') || !Path.IsPathFullyQualified(path))
                throw new JavaException(JavaErrorCode.InvalidPath, "Java 路径必须是完整文件路径或安装目录，不能包含命令行参数。");
            path = Path.GetFullPath(path);
            var name = OperatingSystem.IsWindows() ? "java.exe" : "java";
            if (Directory.Exists(path))
            {
                var alternatives = new[] { Path.Combine(path, "bin", name), Path.Combine(path, name), Path.Combine(path, "Contents", "Home", "bin", name) };
                return alternatives.FirstOrDefault(File.Exists) ?? throw new JavaException(JavaErrorCode.NotFound, "指定目录中未找到 Java 可执行文件。");
            }
            var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (OperatingSystem.IsWindows() && Path.GetFileName(path).Equals("javaw.exe", comparison))
                path = Path.Combine(Path.GetDirectoryName(path)!, name);
            if (!Path.GetFileName(path).Equals(name, comparison)) throw new JavaException(JavaErrorCode.InvalidPath, "请选择 Java 可执行文件或安装目录。");
            if (!File.Exists(path)) throw new JavaException(JavaErrorCode.NotFound, "Java 可执行文件不存在或链接已失效。");
            return path;
        }
        catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
        { throw new JavaException(JavaErrorCode.InvalidPath, "Java 路径格式无效。"); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { throw new JavaException(JavaErrorCode.IoError, "无法访问 Java 路径。"); }
    }

    internal static JavaRuntime Parse(string executable, string output)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);
        using var lines = new StringReader(output);
        while (lines.ReadLine() is string line)
        {
            var equals = line.IndexOf('=');
            if (equals < 0) continue;
            var key = line[..equals].Trim();
            if (key is not ("java.version" or "java.home" or "java.vendor" or "os.arch" or "sun.arch.data.model")) continue;
            var value = line[(equals + 1)..].Trim();
            if (value.Length == 0 || value.Any(char.IsControl) || properties.TryGetValue(key, out var previous) && previous != value) throw Invalid();
            properties[key] = value;
        }
        if (!properties.TryGetValue("java.version", out var version) || !properties.TryGetValue("java.home", out var home) ||
            !Path.IsPathFullyQualified(home) || !properties.TryGetValue("os.arch", out var architecture)) throw Invalid();
        var arch = architecture.ToLowerInvariant() switch
        {
            "x86" or "i386" or "i486" or "i586" or "i686" => JavaArchitecture.X86,
            "amd64" or "x86_64" or "x64" => JavaArchitecture.X64,
            "aarch64" or "arm64" => JavaArchitecture.Arm64,
            "arm" or "arm32" or "armv7l" => JavaArchitecture.Arm32,
            _ => JavaArchitecture.Unknown
        };
        if (properties.TryGetValue("sun.arch.data.model", out var bits) && (bits is not ("32" or "64") ||
            arch is JavaArchitecture.X86 or JavaArchitecture.Arm32 && bits != "32" ||
            arch is JavaArchitecture.X64 or JavaArchitecture.Arm64 && bits != "64")) throw Invalid();
        return new(executable, home, JavaVersion.Parse(version), arch, properties.GetValueOrDefault("java.vendor"));
    }

    private static JavaException Invalid() => new(JavaErrorCode.InvalidOutput, "Java 未返回有效的版本、安装目录或架构属性。");
}
