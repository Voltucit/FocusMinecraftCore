using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using FMCore.Authentication;
using FMCore.Installer;
using FMCore.Java;
using FMCore.Utilities;
using FMCore.Downloader;

namespace FMCore.Launch;

internal static partial class LaunchArguments
{
    internal static ProcessStartInfo Build(LaunchConfig config, JavaRuntime java, InstallationResult installation, LaunchIdentity identity)
    {
        var metadata = installation.Plan.Version.Metadata;
        var context = installation.Plan.Context with { Features = LaunchSettings.Features(config, installation.Plan.Context.Features) };
        if (!LaunchSettings.ValidMemory(config.Java.MinMemoryMb, config.Java.MaxMemoryMb)) throw new LaunchException(LaunchErrorCode.InvalidConfig, "内存范围无效。");
        var main = metadata.MainClass;
        if (main == null || !MainClassPattern().IsMatch(main)) throw Invalid("版本未提供有效的主类。");
        var root = Path.GetFullPath(config.GameCore.RootDirectory);
        var game = installation.Plan.GameDirectory.Length == 0 ? root : Path.GetFullPath(Path.Combine(root, installation.Plan.GameDirectory));
        var classpath = string.Join(context.OperatingSystem == "windows" ? ';' : ':', installation.ClasspathEntries);
        var variables = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["auth_player_name"] = identity.Account.Name,
            ["auth_uuid"] = identity.Uuid,
            ["auth_access_token"] = identity.AccessToken,
            ["auth_session"] = identity.LegacySession,
            ["user_type"] = identity.UserType,
            ["user_properties"] = "{}",
            ["profile_properties"] = "{}",
            ["clientid"] = identity.ClientId ?? "",
            ["auth_xuid"] = identity.Xuid ?? "",
            ["version_name"] = metadata.Id,
            ["version_type"] = metadata.Type ?? "release",
            ["game_directory"] = game,
            ["assets_root"] = installation.Assets.AssetsRootDirectory,
            ["assets_index_name"] = installation.Plan.AssetIndex.Id,
            ["game_assets"] = installation.Assets.GameAssetsDirectory,
            ["natives_directory"] = installation.NativeDirectory,
            ["library_directory"] = Path.Combine(root, "libraries"),
            ["classpath"] = classpath,
            ["classpath_separator"] = context.OperatingSystem == "windows" ? ";" : ":",
            ["primary_jar"] = installation.ClientJarPath,
            ["launcher_name"] = config.LauncherName,
            ["launcher_version"] = config.LauncherVersion,
            ["resolution_width"] = config.Window.Width.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["resolution_height"] = config.Window.Height.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
        var jvm = NormalizeMetadataJvm(metadata.Arguments.GetJvmArguments(context).Where(a => !LaunchSettings.IsHeapArgument(a)).Select(Expand).ToList(), root,
            context.OperatingSystem == "windows" ? ';' : ':', classpath);
        // The vendor ignoreList names the child profile JAR; inherited vanilla files keep their parent ID in FMCore.
        if (main == "cpw.mods.bootstraplauncher.BootstrapLauncher")
            for (var i = 0; i < jvm.Count; i++)
                if (jvm[i].StartsWith("-DignoreList=", StringComparison.Ordinal) &&
                    !jvm[i][13..].Split(',').Contains(Path.GetFileName(installation.ClientJarPath), StringComparer.Ordinal))
                    jvm[i] += "," + Path.GetFileName(installation.ClientJarPath);
        jvm.RemoveAll(a => a.StartsWith("-Djava.library.path=", StringComparison.Ordinal));
        jvm.Add("-Djava.library.path=" + installation.NativeDirectory);
        if (!jvm.Contains("-cp") && !jvm.Contains("-classpath") && !jvm.Contains("--class-path")) jvm.AddRange(["-cp", classpath]);
        if (context.OperatingSystem == "osx" && !jvm.Contains("-XstartOnFirstThread")) jvm.Add("-XstartOnFirstThread");
        if (installation.LoggingPath != null && metadata.Logging != null)
            jvm.Add(Expand(metadata.Logging.Argument.Replace("${path}", installation.LoggingPath, StringComparison.Ordinal)));
        if (config.Java.MinMemoryMb != 0) jvm.Add($"-Xms{config.Java.MinMemoryMb}M");
        jvm.Add($"-Xmx{config.Java.MaxMemoryMb}M");
        ValidateCustomArguments(config.Java.Arguments, config.GameArguments);
        jvm = LaunchSettings.MergeJvm(jvm, config.Java.Arguments.Select(Expand).ToArray());
        var gameArguments = LaunchSettings.GameArguments(config, installation.Plan.Version, context,
            NormalizeGameArguments(metadata.Arguments.GetGameArguments(context).Select(Expand).ToArray(), variables), Expand);
        var start = new ProcessStartInfo(java.ExecutablePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = game,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        JavaProbe.ClearInheritedOptions(start);
        foreach (var argument in jvm.Concat([main]).Concat(gameArguments))
        {
            if (argument.Contains('\0')) throw Invalid("启动参数包含无效字符。");
            start.ArgumentList.Add(argument);
        }
        return start;

        string Expand(string value)
        {
            var expanded = PlaceholderPattern().Replace(value, match => variables.TryGetValue(match.Groups[1].Value, out var replacement)
                ? replacement : throw Invalid("启动参数包含尚未支持的占位符。"));
            if (expanded.Contains("${", StringComparison.Ordinal)) throw Invalid("启动参数包含无效占位符。");
            return expanded;
        }
    }

    internal static void ValidateCustomArguments(IEnumerable<string> jvm, IEnumerable<string> game)
    {
        LaunchSettings.ValidateJvm(jvm);
        LaunchSettings.ValidateGame(game.ToArray());
    }

    private static IReadOnlyList<string> NormalizeGameArguments(IReadOnlyList<string> arguments, IReadOnlyDictionary<string, string> variables)
    {
        var managed = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["--gameDir"] = "game_directory", ["--assetsDir"] = "game_assets", ["--assetIndex"] = "assets_index_name", ["--version"] = "version_name",
            ["--username"] = "auth_player_name", ["--uuid"] = "auth_uuid", ["--accessToken"] = "auth_access_token", ["--session"] = "auth_session",
            ["--userType"] = "user_type", ["--clientId"] = "clientid", ["--xuid"] = "auth_xuid"
        };
        var result = new List<string>();
        var emitted = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i]; var equals = argument.IndexOf('=');
            var key = equals < 0 ? argument : argument[..equals];
            if (!managed.TryGetValue(key, out var variable)) { result.Add(argument); continue; }
            if (equals < 0 && (++i >= arguments.Count || arguments[i].StartsWith("--", StringComparison.Ordinal)))
                throw Invalid("版本参数缺少必要选项值。");
            if (emitted.Add(key)) result.AddRange([key, variables[variable]]);
        }
        return result;
    }

    private static List<string> NormalizeMetadataJvm(List<string> arguments, string root, char separator, string verifiedClasspath)
    {
        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i]; var equals = argument.IndexOf('=');
            var key = equals < 0 ? argument : argument[..equals];
            if (argument.StartsWith('@') || key is "-jar" or "-m" or "--module")
                throw Invalid("启动 JSON 不能通过参数文件或另一入口绕过选定的主类。");
            if (key is "-cp" or "-classpath" or "--class-path" or "-p" or "--module-path")
            {
                var index = equals < 0 ? ++i : i;
                if (index >= arguments.Count) throw Invalid("版本的 Classpath/模块路径参数缺少值。");
                var paths = equals < 0 ? arguments[index] : argument[(equals + 1)..];
                var normalized = paths == verifiedClasspath ? paths : string.Join(separator, paths.Split(separator).Select(NormalizePath));
                arguments[index] = equals < 0 ? normalized : key + "=" + normalized;
            }
            else if (key == "-DlibraryDirectory" && equals >= 0) arguments[i] = key + "=" + NormalizePath(argument[(equals + 1)..]);
            else if (argument.StartsWith("-javaagent:", StringComparison.Ordinal))
            {
                var value = argument[11..]; var split = value.IndexOf('=');
                arguments[i] = "-javaagent:" + NormalizePath(split < 0 ? value : value[..split]) + (split < 0 ? "" : value[split..]);
            }
        }
        return arguments;

        string NormalizePath(string value)
        {
            try
            {
                // Java classpath wildcards are restricted to a validated directory inside the selected root.
                var wildcard = value.EndsWith("/*", StringComparison.Ordinal) || value.EndsWith("\\*", StringComparison.Ordinal);
                var path = wildcard ? value[..^2] : value;
                if (path.Length == 0) throw Invalid("Classpath 中包含空路径。");
                var full = Path.GetFullPath(path, root);
                var relative = Path.GetRelativePath(root, full);
                full = relative == "." ? root : GameFiles.Resolve(root, relative);
                DownloadPath.CheckLinks(full);
                return wildcard ? Path.Combine(full, "*") : full;
            }
            catch (Exception e) when (e is DownloadException or ArgumentException or NotSupportedException)
            { throw new LaunchException(LaunchErrorCode.UnsafePath, "版本 JVM 路径必须位于指定 Minecraft 根目录内，不能包含链接或越界路径。"); }
        }
    }

    private static LaunchException Invalid(string message) => new(LaunchErrorCode.InvalidArguments, message);
    [GeneratedRegex(@"\$\{([A-Za-z0-9_]+)\}")]
    private static partial Regex PlaceholderPattern();
    [GeneratedRegex(@"\A[A-Za-z_$][A-Za-z0-9_$]*(?:\.[A-Za-z_$][A-Za-z0-9_$]*)*\z")]
    private static partial Regex MainClassPattern();
}
