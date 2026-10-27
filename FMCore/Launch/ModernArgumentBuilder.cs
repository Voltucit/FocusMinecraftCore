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

        if (libraries.Any(item => item.ContainsKey("natives")))
            throw new NotSupportedException(
                "该版本仍使用旧式 natives 映射，稍后单独接入。");

        string classPath =
            ModernLibraryResolver.GetClassPath(
                config.Version,
                minecraftRoot,
                config.Java,
                features);

        AssetPaths assets =
            LaunchResourceResolver.ResolveAssets(
                config.Version, minecraftRoot);

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
                ["user_type"] = config.Account.UserType,
                ["user_properties"] = "{}",
                ["clientid"] = config.Account.ClientId,
                ["auth_xuid"] = config.Account.Xuid ?? "0",

                ["version_name"] = config.Version.Id,
                ["version_type"] = versionType,
                ["game_directory"] = config.GameDirectory,
                ["assets_root"] = assets.AssetsRoot,
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

        if (logging is not null)
            info.ArgumentList.Add(logging.Argument);

        // 使用清单中的 Fabric 启动主类。
        info.ArgumentList.Add(mainClass);

        foreach (string template in templates.Game)
            info.ArgumentList.Add(Expand(template, values));

        return info;
    }

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
