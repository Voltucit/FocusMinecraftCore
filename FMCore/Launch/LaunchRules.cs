using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FMCore.Java;
using FMCore.Versions;

namespace FMCore.Launch;

internal static class LaunchRules
{
    private static readonly HashSet<string> KnownFeatures =
        new(StringComparer.Ordinal)
        {
            "is_demo_user",
            "has_custom_resolution",
            "has_quick_plays_support",
            "is_quick_play_singleplayer",
            "is_quick_play_multiplayer",
            "is_quick_play_realms"
        };

    internal static bool IsAllowedForArchitecture(
        JsonObject entry,
        string? targetArchitecture,
        IReadOnlyDictionary<string, bool>? features = null)
    {
        ArgumentNullException.ThrowIfNull(entry);


        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(
                "当前规则解析只支持 Windows。");

        if (!entry.TryGetPropertyValue(
                "rules", out JsonNode? rulesNode))
            return true;

        if (rulesNode is not JsonArray rules)
            throw new InvalidDataException(
                "rules 必须是列表。");

        if (rules.Count == 0)
            return true;

        bool allowed = false;

        foreach (JsonNode? node in rules)
        {
            if (node is not JsonObject rule)
                throw new InvalidDataException(
                    "规则必须是 JSON 对象。");

            if (rule.Any(pair =>
                    pair.Key is not ("action" or "os" or "features")))
                throw new NotSupportedException(
                    "规则中包含暂未支持的字段。");

            string action =
                MinecraftVersionReader.ReadRequiredString(
                    rule, "action", "规则缺少有效的 action。");

            if (action is not ("allow" or "disallow"))
                throw new InvalidDataException(
                    "规则 action 必须是 allow 或 disallow。");

            bool matches = true;

            if (rule.TryGetPropertyValue(
                    "os", out JsonNode? osNode))
            {
                if (osNode is not JsonObject os)
                    throw new InvalidDataException(
                        "规则 os 必须是 JSON 对象。");

                if (os.Any(pair =>
                        pair.Key is not ("name" or "arch" or "version")))
                    throw new NotSupportedException(
                        "当前支持 os.name、os.arch 和 os.version。");

                if (os.ContainsKey("name"))
                {
                    string name =
                        MinecraftVersionReader.ReadRequiredString(
                            os,
                            "name",
                            "os.name 必须是非空字符串。");

                    matches &= name == "windows";
                }

                // 架构条件读取 arch。
                if (os.ContainsKey("arch"))
                {
                    string pattern =
                        MinecraftVersionReader.ReadRequiredString(
                            os,
                            "arch",
                            "os.arch 必须是非空字符串。");

                    string architecture = targetArchitecture switch
                    {
                        "x86" => "x86",
                        "x64" => "amd64",
                        "arm64" => "aarch64",
                        _ => throw new NotSupportedException(
                            "无法根据未知 Java 架构判断 os.arch。")
                    };

                    matches &= MatchesPattern(architecture, pattern);
                }

                // 系统版本条件读取 version，两项判断彼此独立。
                if (os.ContainsKey("version"))
                {
                    string pattern =
                        MinecraftVersionReader.ReadRequiredString(
                            os,
                            "version",
                            "os.version 必须是非空字符串。");

                    Version systemVersion = Environment.OSVersion.Version;

                    string versionText =
                        $"{systemVersion.Major}.{systemVersion.Minor}";

                    matches &= MatchesPattern(versionText, pattern);
                }
            }

            if (rule.TryGetPropertyValue(
                    "features", out JsonNode? featuresNode))
            {
                if (featuresNode is not JsonObject requiredFeatures)
                    throw new InvalidDataException(
                        "规则 features 必须是 JSON 对象。");

                foreach (var pair in requiredFeatures)
                {
                    if (!KnownFeatures.Contains(pair.Key))
                        throw new NotSupportedException(
                            $"暂未支持功能条件：{pair.Key}");

                    if (pair.Value is not JsonValue value
                        || !value.TryGetValue<bool>(out bool expected))
                        throw new InvalidDataException(
                            $"功能条件 {pair.Key} 必须是布尔值。");

                    bool actual = features is not null
                        && features.TryGetValue(
                            pair.Key, out bool enabled)
                        && enabled;

                    matches &= actual == expected;
                }
            }

            // 最后一条匹配的规则决定结果。
            if (matches)
                allowed = action == "allow";
        }

        return allowed;
    }

    private static bool MatchesPattern(
        string input,
        string pattern)
    {
        try
        {
            return Regex.IsMatch(
                input,
                pattern,
                RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100));
        }
        catch (Exception error)
            when (error is ArgumentException
                      or RegexMatchTimeoutException)
        {
            throw new InvalidDataException(
                "系统条件的正则表达式无效或匹配超时。",
                error);
        }
    }

    internal static bool IsAllowed(
        JsonObject entry,
        JavaInfo java,
        IReadOnlyDictionary<string, bool>? features = null)
    {
        ArgumentNullException.ThrowIfNull(java);

        return IsAllowedForArchitecture(
            entry,
            java.Architecture,
            features);
    }
}
