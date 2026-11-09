using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FMCore.Installer;
using FMCore.Versions;

namespace FMCore.Launch;

internal static class ModernArgumentBuilder
{
    private static readonly Regex Placeholder =
        new(@"\$\{([^{}]+)\}", RegexOptions.CultureInvariant);

    internal static ProcessStartInfo Build(
        PreparedLaunchConfig config,
        string minecraftRoot,
        string nativesDirectory)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(nativesDirectory);

        IReadOnlyList<MinecraftVersion> chain =
            LibraryResolver.GetValidatedChain(
                config.Version, minecraftRoot);

        CheckJavaVersion(chain, config);

        Dictionary<string, bool> features =
            new(StringComparer.Ordinal)
            {
                ["has_custom_resolution"] =
                    config.WindowWidth.HasValue
            };

        ModernArgumentTemplates templates =
            ModernArgumentReader.Read(
                config.Version,
                minecraftRoot,
                config.Java,
                features);

        var libraries = ModernLibraryResolver.ReadLibraries(
            config.Version,
            minecraftRoot,
            config.Java,
            features);

        // 新版参数也可能搭配旧式分类压缩包。先检查文件，启动时再安全解压。
        foreach (var library in libraries)
        {
            string? archive = LibraryResolver.GetSelectedWindowsNativeLibraryPath(
                library, minecraftRoot, config.Java);
            if (archive is not null && !File.Exists(archive))
                throw new FileNotFoundException(
                    "缺少原生库文件，请先补全游戏文件。", archive);
        }

        string classPath =
            ModernLibraryResolver.GetClassPath(
                config.Version,
                minecraftRoot,
                config.Java,
                features);

        AssetPaths assets =
            LaunchResourceResolver.ResolveAssets(
                config.Version, minecraftRoot, config.GameDirectory);

        LoggingConfig? logging =
            LaunchResourceResolver.ResolveLogging(
                config.Version, minecraftRoot);

        string versionDirectory =
            PathSafety.RequireDirectoryPath(config.Version.Directory);

        string nativePath =
            PathSafety.RequireDirectoryPath(nativesDirectory);

        if (!PathSafety.IsWithin(nativePath, versionDirectory)
            || PathSafety.Comparer.Equals(
                nativePath, versionDirectory))
            throw new InvalidDataException(
                "原生库工作目录必须位于当前版本目录内部。");

        string mainClass = chain
            .Select(item => item.MainClass)
            .FirstOrDefault(item => item is not null)
            ?? throw new InvalidDataException("缺少 mainClass。");

        if (!Regex.IsMatch(
                mainClass,
                @"\A[A-Za-z_$][A-Za-z0-9_$]*(?:\.[A-Za-z_$][A-Za-z0-9_$]*)*\z",
                RegexOptions.CultureInvariant))
            throw new InvalidDataException("mainClass 格式无效。");

        string versionType = chain
            .Select(item => item.VersionType)
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))
            ?? "custom";

        Dictionary<string, string> values =
            new(StringComparer.Ordinal)
            {
                ["auth_player_name"] = config.Account.Username,
                ["auth_uuid"] = config.Account.Uuid.ToString("N"),
                ["auth_access_token"] = config.Account.AccessToken,
                ["auth_session"] = $"token:{config.Account.AccessToken}:{config.Account.Uuid:N}",
                ["user_type"] = config.Account.UserType,
                ["user_properties"] = "{}",
                ["clientid"] = config.Account.ClientId,
                ["auth_xuid"] = config.Account.Xuid ?? "0",

                ["version_name"] = config.Version.Id,
                ["version_type"] = versionType,
                ["game_directory"] = config.GameDirectory,
                ["assets_root"] = assets.AssetsRoot,
                ["game_assets"] = assets.GameAssets,
                ["assets_index_name"] = assets.IndexId,

                ["natives_directory"] = nativePath,
                ["library_directory"] =
                    PathSafety.RequireDirectoryPath(
                        Path.Combine(minecraftRoot, "libraries")),
                ["classpath"] = classPath,
                ["classpath_separator"] = ";",

                ["launcher_name"] = "FMCore",
                ["launcher_version"] =
                    typeof(Launcher).Assembly.GetName()
                        .Version?.ToString() ?? "unknown"
            };

        if (config.WindowWidth is int width
            && config.WindowHeight is int height)
        {
            values["resolution_width"] =
                width.ToString(CultureInfo.InvariantCulture);

            values["resolution_height"] =
                height.ToString(CultureInfo.InvariantCulture);
        }

        ProcessStartInfo info = new()
        {
            FileName = config.Java.Executable,
            WorkingDirectory = config.GameDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        string memory = config.MaxMemoryMb.ToString(
            CultureInfo.InvariantCulture);

        info.ArgumentList.Add($"-Xmx{memory}M");

        if (config.Java.MajorVersion >= 24
            && !templates.Jvm.Contains(
                "--enable-native-access=ALL-UNNAMED",
                StringComparer.Ordinal))
        {
            info.ArgumentList.Add(
                "--enable-native-access=ALL-UNNAMED");
        }

        foreach (string template in templates.Jvm)
            info.ArgumentList.Add(Expand(template, values));

        // 默认值按选项名称补缺。手动指定 GC 时，不能再叠加版本默认的另一种 GC。
        HashSet<string> configuredOptions = config.JvmArguments
            .Concat(info.ArgumentList)
            .Select(GetJvmOptionName)
            .ToHashSet(StringComparer.Ordinal);
        bool customCollector = config.JvmArguments.Any(argument =>
            argument.StartsWith("-XX:+", StringComparison.Ordinal)
            && IsCollectorOption(GetJvmOptionName(argument)));
        bool customG1 = config.JvmArguments.Contains(
            "-XX:+UseG1GC", StringComparer.Ordinal);

        foreach (string template in templates.UserJvm)
        {
            string option = GetJvmOptionName(template);
            if (IsMemoryOption(template)
                || configuredOptions.Contains(option)
                || customCollector && (IsCollectorOption(option)
                    || !customG1 && option.StartsWith("-XX:G1", StringComparison.Ordinal)))
                continue;

            string expanded = Expand(template, values);
            if (!configuredOptions.Add(GetJvmOptionName(expanded)))
                continue;

            info.ArgumentList.Add(expanded);
        }

        if (logging is not null)
            info.ArgumentList.Add(logging.Argument);

        // 使用清单中的 Fabric 启动主类。
        info.ArgumentList.Add(mainClass);

        foreach (string template in templates.Game)
            info.ArgumentList.Add(Expand(template, values));

        foreach (string argument in config.GameArguments)
            info.ArgumentList.Add(argument);

        if (config.Fullscreen
            && !info.ArgumentList.Contains("--fullscreen"))
        {
            info.ArgumentList.Add("--fullscreen");
        }


        return info;
    }

    // 开关的正反值、同名参数的不同取值，都属于同一个选项。
    private static string GetJvmOptionName(string argument)
    {
        if (argument.StartsWith("-XX:", StringComparison.Ordinal))
        {
            string name = argument[4..];
            if (name.StartsWith('+') || name.StartsWith('-'))
                name = name[1..];
            int separator = name.IndexOf('=');
            return "-XX:" + (separator < 0 ? name : name[..separator]);
        }

        if (argument.StartsWith("-D", StringComparison.Ordinal)
            || argument.StartsWith("--", StringComparison.Ordinal))
        {
            int separator = argument.IndexOf('=');
            return separator < 0 ? argument : argument[..separator];
        }

        return argument;
    }

    private static bool IsCollectorOption(string option) => option is
        "-XX:UseG1GC" or "-XX:UseZGC" or "-XX:UseParallelGC"
        or "-XX:UseSerialGC" or "-XX:UseShenandoahGC"
        or "-XX:UseConcMarkSweepGC" or "-XX:UseEpsilonGC";

    // 内存由调用方的 MaxMemoryMb 决定，默认用户参数里的内存项一律忽略。
    private static bool IsMemoryOption(string template) =>
        template.StartsWith("-Xmx", StringComparison.Ordinal)
        || template.StartsWith("-Xms", StringComparison.Ordinal);

    private static void CheckJavaVersion(
        IReadOnlyList<MinecraftVersion> chain,
        PreparedLaunchConfig config)
    {
        foreach (MinecraftVersion version in chain)
        {
            JsonObject data =
                MinecraftVersionReader.ReadVersionJson(
                    version.Directory);

            if (!data.TryGetPropertyValue(
                    "javaVersion", out JsonNode? node))
                continue;

            if (node is not JsonObject javaVersion
                || javaVersion["majorVersion"] is not JsonValue value
                || !value.TryGetValue<int>(out int required)
                || required <= 0)
                throw new InvalidDataException(
                    "javaVersion.majorVersion 必须是正整数。");

            if (config.Java.MajorVersion < required)
                throw new NotSupportedException(
                    $"该版本要求至少 Java {required}，" +
                    $"当前选择的是 Java {config.Java.MajorVersion}。");

            return;
        }
    }

    private static string Expand(
        string template,
        IReadOnlyDictionary<string, string> values)
    {
        string remaining = Placeholder.Replace(template, "");

        if (remaining.Contains("${", StringComparison.Ordinal))
            throw new InvalidDataException(
                $"占位符格式不完整：{template}");

        return Placeholder.Replace(template, match =>
        {
            string name = match.Groups[1].Value;

            if (!values.TryGetValue(name, out string? value))
                throw new NotSupportedException(
                    $"暂未支持参数占位符：{name}");

            return value;
        });
    }
}
