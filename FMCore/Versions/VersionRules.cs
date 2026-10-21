using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace FMCore.Versions;

public sealed record RuleContext(string OperatingSystem, string Architecture, string OperatingSystemVersion,
    IReadOnlyDictionary<string, bool>? Features = null)
{
    public bool Is64Bit => Architecture is "amd64" or "x86_64" or "x64" or "aarch64" or "arm64";
    public static RuleContext Current => new(System.OperatingSystem.IsWindows() ? "windows" : System.OperatingSystem.IsMacOS() ? "osx" : System.OperatingSystem.IsLinux() ? "linux" : "unknown",
        RuntimeInformation.OSArchitecture switch { System.Runtime.InteropServices.Architecture.X64 => "amd64", System.Runtime.InteropServices.Architecture.Arm64 => "aarch64", _ => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant() },
        Environment.OSVersion.Version.ToString());
}

public sealed record VersionRule(bool Allow, string? OperatingSystem, string? Architecture, string? OperatingSystemVersion,
    IReadOnlyDictionary<string, bool> Features);

public static class RuleEvaluator
{
    public static bool IsAllowed(IReadOnlyList<VersionRule>? rules, RuleContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (rules == null) return true;
        var allowed = false;
        foreach (var rule in rules)
        {
            if (rule.OperatingSystem != null && rule.OperatingSystem != context.OperatingSystem) continue;
            if (rule.Architecture != null && !MatchArchitecture(context.Architecture, rule.Architecture)) continue;
            if (rule.OperatingSystemVersion != null && !Match(context.OperatingSystemVersion, rule.OperatingSystemVersion)) continue;
            if (rule.Features.Any(f => (context.Features?.GetValueOrDefault(f.Key) ?? false) != f.Value)) continue;
            allowed = rule.Allow;
        }
        return allowed;
    }

    private static bool MatchArchitecture(string value, string pattern)
    {
        static string Normalize(string arch) => arch switch { "x64" or "x86_64" => "amd64", "arm64" => "aarch64", "i386" or "i686" => "x86", _ => arch };
        if (pattern is "x64" or "x86_64" or "amd64" or "arm64" or "aarch64" or "x86" or "i386" or "i686") return Normalize(value) == Normalize(pattern);
        return Match(value, $"\\A(?:{pattern})\\z");
    }

    private static bool Match(string value, string pattern)
    {
        try { return Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)); }
        catch (ArgumentException) { throw new VersionException(VersionErrorCode.InvalidMetadata, "规则中的正则表达式无效。"); }
        catch (RegexMatchTimeoutException) { throw new VersionException(VersionErrorCode.RuleEvaluation, "规则匹配超时。"); }
    }
}
