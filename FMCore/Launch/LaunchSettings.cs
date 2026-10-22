using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using FMCore.Versions;

namespace FMCore.Launch;

// Shared validation/normalization for direct launches and persisted instances; no parallel launch engine.
internal static partial class LaunchSettings
{
    private static readonly HashSet<string> ManagedGameOptions = new(StringComparer.Ordinal)
        { "--width", "--height", "--fullscreen", "--server", "--port", "--quickPlayMultiplayer" };
    private static readonly HashSet<string> Collectors = new(StringComparer.Ordinal)
        { "UseSerialGC", "UseParallelGC", "UseG1GC", "UseConcMarkSweepGC", "UseZGC", "UseShenandoahGC", "UseEpsilonGC" };
    internal static bool ValidMemory(int min, int max) => min >= 0 && max >= 1 && min <= max && max <= 1024 * 1024;
    internal static bool IsHeapArgument(string arg) => arg.StartsWith("-Xms", StringComparison.OrdinalIgnoreCase) ||
        arg.StartsWith("-Xmx", StringComparison.OrdinalIgnoreCase) || arg.StartsWith("-XX:InitialHeapSize=", StringComparison.Ordinal) ||
        arg.StartsWith("-XX:MaxHeapSize=", StringComparison.Ordinal);

    internal static GameServerConfig? FreezeServer(GameServerConfig? value)
    {
        if (value == null) return null;
        var host = value.Host;
        if (string.IsNullOrEmpty(host) || host.Length > 253 || host.Any(c => char.IsControl(c) || char.IsWhiteSpace(c)) || value.Port is < 1 or > 65535) throw Invalid();
        if (host.StartsWith('[') && host.EndsWith(']'))
        {
            host = host[1..^1];
            if (!IPAddress.TryParse(host, out var bracketed) || bracketed.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6) throw Invalid();
        }
        if (IPAddress.TryParse(host, out var ip)) return value with { Host = ip.ToString() };
        if (host.IndexOfAny(['/', '\\', '@', '?', '#', ':', '[', ']']) >= 0) throw Invalid();
        try { host = new IdnMapping().GetAscii(host); } catch (ArgumentException) { throw Invalid(); }
        if (host.Length > 253 || Uri.CheckHostName(host) != UriHostNameType.Dns) throw Invalid();
        return value with { Host = host.ToLowerInvariant() };
    }
    private static string Endpoint(GameServerConfig server) => (server.Host.Contains(':') ? "[" + server.Host + "]" : server.Host) + ":" + server.Port.ToString(CultureInfo.InvariantCulture);
    private static GameServerConfig ParseEndpoint(string text)
    {
        if (IPAddress.TryParse(text, out _)) return FreezeServer(new() { Host = text })!;
        if (text.IndexOfAny(['/', '\\', '@', '?', '#']) >= 0 || !Uri.TryCreate("minecraft://" + text, UriKind.Absolute, out var uri) ||
            uri.UserInfo.Length != 0 || uri.AbsolutePath is not ("" or "/") ||
            uri.Query.Length != 0 || uri.Fragment.Length != 0 || text.Any(c => char.IsWhiteSpace(c) || char.IsControl(c)) || text.Contains('\\')) throw Invalid();
        return FreezeServer(new() { Host = uri.Host, Port = uri.Port == -1 ? 25565 : uri.Port })!;
    }

    internal static void ValidateJvm(IEnumerable<string> arguments)
    {
        var seen = new List<string>();
        foreach (var arg in arguments)
        {
            if (arg == null || arg.Length > 32768 || arg.Any(char.IsControl) || !arg.StartsWith('-') || IsHeapArgument(arg) ||
                arg.Split('=')[0] is "-cp" or "-classpath" or "--class-path" or "-jar" or "-m" or "--module" or "--module-path") throw Invalid();
            if (seen.Any(previous => SameJvmKey(arg, previous) && previous != arg)) throw Invalid();
            seen.Add(arg);
        }
    }
    private static bool SameJvmKey(string a, string b) => JvmKey(a) is { } key && key == JvmKey(b) ||
        (a.StartsWith("-XX:+", StringComparison.Ordinal) || a.StartsWith("-XX:-", StringComparison.Ordinal)) &&
        (b.StartsWith("-XX:+", StringComparison.Ordinal) || b.StartsWith("-XX:-", StringComparison.Ordinal)) && a[5..] == b[5..];
    private static string? JvmKey(string arg)
    {
        if (arg.StartsWith("-D", StringComparison.Ordinal)) return arg.Split('=')[0];
        if (arg.StartsWith("-XX:+", StringComparison.Ordinal) || arg.StartsWith("-XX:-", StringComparison.Ordinal))
        {
            var name = arg[5..]; return arg[4] == '+' && Collectors.Contains(name) ? "GC selection" : "-XX:" + name;
        }
        if (arg.StartsWith("-XX:", StringComparison.Ordinal)) return arg.Split('=')[0];
        if (arg.StartsWith("-Xss", StringComparison.Ordinal)) return "-Xss";
        return null;
    }
    internal static List<string> MergeJvm(IEnumerable<string> metadata, IReadOnlyList<string> custom)
    {
        ValidateJvm(custom);
        var result = new List<string>();
        foreach (var arg in metadata.Concat(custom))
        {
            var key = JvmKey(arg);
            if (key != null) result.RemoveAll(old => SameJvmKey(old, arg));
            else if (arg.StartsWith('-') && !TakesFollowingValue(arg) && result.Contains(arg, StringComparer.Ordinal)) continue;
            result.Add(arg);
        }
        // Decoder/stream contracts require UTF-8. Matching custom values are deduplicated, conflicting values fail explicitly.
        foreach (var key in new[] { "file.encoding", "stdout.encoding", "stderr.encoding" })
        {
            if (custom.Any(a => JvmKey(a) == "-D" + key && a != "-D" + key + "=UTF-8")) throw Invalid();
            result.RemoveAll(a => JvmKey(a) == "-D" + key);
            result.Add("-D" + key + "=UTF-8");
        }
        return result;
    }
    private static bool TakesFollowingValue(string arg) => arg is "-cp" or "-classpath" or "--class-path" or "-p" or "--module-path" or
        "--upgrade-module-path" or "--add-modules" or "--limit-modules" or "--add-reads" or "--add-exports" or "--add-opens" or "--patch-module";
    internal static Dictionary<string, bool> Features(LaunchConfig config, IReadOnlyDictionary<string, bool>? inherited = null)
    {
        var features = new Dictionary<string, bool>(inherited ?? new Dictionary<string, bool>(), StringComparer.Ordinal);
        foreach (var feature in config.Features) features[feature.Key] = feature.Value;
        foreach (var feature in new Dictionary<string, bool>
        {
        ["has_custom_resolution"] = true,
        // Quick Play logging is optional. Do not create world/server history as a side effect of auto-joining.
        ["has_quick_plays_support"] = false,
        ["is_quick_play_multiplayer"] = false,
        ["is_quick_play_singleplayer"] = false,
            ["is_quick_play_realms"] = false
        }) features[feature.Key] = feature.Value;
        return features;
    }
    internal static void ValidateGame(IReadOnlyList<string> arguments, GameWindowConfig? window = null, GameServerConfig? server = null)
    {
        foreach (var arg in arguments)
            if (arg == null || arg.Length > 32768 || arg.Any(char.IsControl) || arg.Split('=')[0] is "--username" or "--uuid" or "--accessToken" or "--session" or
                "--userType" or "--gameDir" or "--assetsDir" or "--assetIndex" or "--version" or "--clientId" or "--xuid") throw Invalid();
        var options = ReadOptions(arguments, out _);
        if (window != null)
        {
            if (options.TryGetValue("--width", out var width) && width != window.Width.ToString(CultureInfo.InvariantCulture) ||
                options.TryGetValue("--height", out var height) && height != window.Height.ToString(CultureInfo.InvariantCulture) ||
                options.TryGetValue("--fullscreen", out var fullscreen) && fullscreen != window.Fullscreen.ToString().ToLowerInvariant()) throw Invalid();
        }
        var selected = ResolveServer(server, options);
        if (selected != null && arguments.Any(a => a.Split('=')[0] is "--quickPlaySingleplayer" or "--quickPlayRealms")) throw Invalid();
    }
    private static Dictionary<string, string> ReadOptions(IReadOnlyList<string> args, out List<string> remaining, bool metadata = false)
    {
        remaining = []; var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < args.Count; i++)
        {
            var split = args[i].IndexOf('='); var key = split < 0 ? args[i] : args[i][..split];
            if (!ManagedGameOptions.Contains(key)) { remaining.Add(args[i]); continue; }
            string value;
            if (split >= 0) value = args[i][(split + 1)..];
            else if (key == "--fullscreen") value = i + 1 < args.Count && args[i + 1] is "true" or "false" ? args[++i] : "true";
            else
            {
                if (++i >= args.Count || args[i].StartsWith("--", StringComparison.Ordinal)) throw Invalid();
                value = args[i];
            }
            if (value.Length == 0 || !metadata && options.TryGetValue(key, out var previous) && previous != value) throw Invalid();
            options[key] = value;
        }
        return options;
    }
    private static GameServerConfig? ResolveServer(GameServerConfig? configured, Dictionary<string, string> options)
    {
        configured = FreezeServer(configured);
        GameServerConfig? legacy = null;
        if (options.TryGetValue("--server", out var host)) legacy = ParseEndpoint(host);
        if (options.TryGetValue("--port", out var port))
        {
            if (legacy == null || !int.TryParse(port, NumberStyles.None, CultureInfo.InvariantCulture, out var number)) throw Invalid();
            legacy = FreezeServer(legacy with { Port = number });
        }
        var quick = options.TryGetValue("--quickPlayMultiplayer", out var endpoint) ? ParseEndpoint(endpoint) : null;
        var selected = configured ?? legacy ?? quick;
        if (selected != null && (legacy != null && legacy != selected || quick != null && quick != selected)) throw Invalid();
        return selected;
    }
    internal static List<string> GameArguments(LaunchConfig config, ResolvedVersion version, RuleContext context, IReadOnlyList<string> metadata, Func<string, string> expand)
    {
        ValidateGame(config.GameArguments, config.Window, config.Server);
        var customOptions = ReadOptions(config.GameArguments, out var extra);
        var server = ResolveServer(config.Server, customOptions);
        ReadOptions(metadata, out var result, metadata: true); // Remove metadata defaults before emitting exactly one controlled value.
        result.AddRange(["--width", config.Window.Width.ToString(CultureInfo.InvariantCulture), "--height", config.Window.Height.ToString(CultureInfo.InvariantCulture)]);
        if (config.Window.Fullscreen) result.Add("--fullscreen");
        if (server != null)
        {
            var features = Features(config, context.Features); features["is_quick_play_multiplayer"] = true;
            if (version.Metadata.Arguments.Game.Any(a => a.Values.Any(v => v.Split('=')[0] == "--quickPlayMultiplayer") &&
                RuleEvaluator.IsAllowed(a.Rules, context with { Features = features })))
                result.AddRange(["--quickPlayMultiplayer", Endpoint(server)]);
            else if (LegacyServerSupported(version.ClientJarId)) result.AddRange(["--server", server.Host, "--port", server.Port.ToString(CultureInfo.InvariantCulture)]);
            else throw new LaunchException(LaunchErrorCode.UnsupportedGameSettings, "版本未声明 Quick Play，也不是可识别的旧版客户端；不能安全选择服务器启动参数。");
        }
        result.AddRange(extra.Select(expand));
        return result;
    }
    private static bool LegacyServerSupported(string id)
    {
        if (ReleasePattern().IsMatch(id) && Version.TryParse(id.Split('-')[0], out var version)) return version.Major == 1 && version.Minor is >= 6 and <= 19;
        var match = SnapshotPattern().Match(id);
        if (!match.Success) return false;
        var year = int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture); var week = int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        return year is >= 14 and <= 22 || year == 13 && week >= 16 || year == 23 && week < 14;
    }
    internal static LaunchException Invalid() => new(LaunchErrorCode.InvalidArguments, "启动设置存在无效地址、端口、参数值或相互冲突的重复选项；请使用统一内存、窗口和服务器配置。");
    [GeneratedRegex(@"\A1\.[0-9]+(?:\.[0-9]+)?(?:-(?:pre|rc)[0-9]+)?\z", RegexOptions.CultureInvariant)]
    private static partial Regex ReleasePattern();
    [GeneratedRegex(@"\A([0-9]{2})w([0-9]{2})[a-z]\z", RegexOptions.CultureInvariant)]
    private static partial Regex SnapshotPattern();
}
