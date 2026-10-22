using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using FMCore.Authentication;
using FMCore.Installer;
using FMCore.Java;

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
        var jvm = metadata.Arguments.GetJvmArguments(context).Where(a => !LaunchSettings.IsHeapArgument(a)).Select(Expand).ToList();
        // The vendor ignoreList names the child profile JAR; inherited vanilla files keep their parent ID in FMCore.
        if (main == "cpw.mods.bootstraplauncher.BootstrapLauncher")
            for (var i = 0; i < jvm.Count; i++)
                if (jvm[i].StartsWith("-DignoreList=", StringComparison.Ordinal) &&
                    !jvm[i][13..].Split(',').Contains(Path.GetFileName(installation.ClientJarPath), StringComparer.Ordinal))
                    jvm[i] += "," + Path.GetFileName(installation.ClientJarPath);
        if (!jvm.Any(a => a.StartsWith("-Djava.library.path=", StringComparison.Ordinal))) jvm.Add("-Djava.library.path=" + installation.NativeDirectory);
        if (!jvm.Contains("-cp") && !jvm.Contains("-classpath") && !jvm.Contains("--class-path")) jvm.AddRange(["-cp", classpath]);
        if (context.OperatingSystem == "osx" && !jvm.Contains("-XstartOnFirstThread")) jvm.Add("-XstartOnFirstThread");
        if (installation.LoggingPath != null && metadata.Logging != null)
            jvm.Add(Expand(metadata.Logging.Argument.Replace("${path}", installation.LoggingPath, StringComparison.Ordinal)));
        if (config.Java.MinMemoryMb != 0) jvm.Add($"-Xms{config.Java.MinMemoryMb}M");
        jvm.Add($"-Xmx{config.Java.MaxMemoryMb}M");
        ValidateCustomArguments(config.Java.Arguments, config.GameArguments);
        jvm = LaunchSettings.MergeJvm(jvm, config.Java.Arguments.Select(Expand).ToArray());
        var gameArguments = LaunchSettings.GameArguments(config, installation.Plan.Version, context,
            metadata.Arguments.GetGameArguments(context).Select(Expand).ToArray(), Expand);
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

    private static LaunchException Invalid(string message) => new(LaunchErrorCode.InvalidArguments, message);
    [GeneratedRegex(@"\$\{([A-Za-z0-9_]+)\}")]
    private static partial Regex PlaceholderPattern();
    [GeneratedRegex(@"\A[A-Za-z_$][A-Za-z0-9_$]*(?:\.[A-Za-z_$][A-Za-z0-9_$]*)*\z")]
    private static partial Regex MainClassPattern();
}
