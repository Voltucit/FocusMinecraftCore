using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FMCore.Installer;
using FMCore.Versions;

namespace FMCore.Launch;

internal static class LegacyArgumentBuilder
{
    private static readonly Regex Placeholder =
        new(@"\$\{([^{}]+)\}", RegexOptions.CultureInvariant);

    internal static ProcessStartInfo Build(
        PreparedLaunchConfig config,
        string minecraftRoot,
        string? nativesDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            config.Java.Executable);

        if (!Path.IsPathFullyQualified(config.Java.Executable))
            throw new ArgumentException(
                "所选 Java 必须使用完整路径。");

        var metadata = ReadMetadata(config.Version, minecraftRoot);

        string classPath =
            LibraryResolver.GetWindowsClassPath(
                config.Version, minecraftRoot);

        AssetPaths assets =
            LaunchResourceResolver.ResolveAssets(
                config.Version, minecraftRoot);

        // 已声明的日志配置缺失时，保留原有错误，不静默跳过。
        LoggingConfig? logging =
            LaunchResourceResolver.ResolveLogging(
                config.Version, minecraftRoot);

        IReadOnlyList<string> nativeArchives =
            LibraryResolver.GetWindowsNativeLibraryPaths(
                config.Version, minecraftRoot, config.Java);

        if (nativeArchives.Count > 0 && nativesDirectory is null)
            throw new ArgumentException(
                "该版本需要先解压原生库，并提供解压目录。",
                nameof(nativesDirectory));

        string? nativePath = null;

        if (nativesDirectory is not null)
        {
            nativePath =
                PathSafety.RequireDirectoryPath(nativesDirectory);

            if (!Directory.Exists(nativePath))
                throw new DirectoryNotFoundException(
                    $"原生库解压目录不存在：{nativePath}");
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

        if (nativePath is not null)
            info.ArgumentList.Add(
                "-Djava.library.path=" + nativePath);

        if (logging is not null)
            info.ArgumentList.Add(logging.Argument);

        info.ArgumentList.Add("-cp");
        info.ArgumentList.Add(classPath);
        info.ArgumentList.Add(metadata.MainClass);

        foreach (string argument in ExpandTemplate(
                     metadata.Template,
                     config,
                     assets,
                     metadata.VersionType))
        {
            info.ArgumentList.Add(argument);
        }

        return info;
    }

    // 可以单独核对游戏参数，不要求已经准备好原生库和日志文件。
    internal static IReadOnlyList<string> GetGameArguments(
        PreparedLaunchConfig config,
        string minecraftRoot)
    {
        ArgumentNullException.ThrowIfNull(config);

        var metadata = ReadMetadata(config.Version, minecraftRoot);

        AssetPaths assets =
            LaunchResourceResolver.ResolveAssets(
                config.Version, minecraftRoot);

        return ExpandTemplate(
            metadata.Template,
            config,
            assets,
            metadata.VersionType);
    }

    private static (
        string MainClass,
        string Template,
        string VersionType) ReadMetadata(
        MinecraftVersion version,
        string minecraftRoot)
    {
        IReadOnlyList<MinecraftVersion> chain =
            LibraryResolver.GetValidatedChain(version, minecraftRoot);

        string? template = null;

        foreach (MinecraftVersion current in chain)
        {
            JsonObject data =
                MinecraftVersionReader.ReadVersionJson(
                    current.Directory);

            // 本轮明确只处理旧版格式。
            if (data.ContainsKey("arguments"))
                throw new NotSupportedException(
                    "该继承链包含新版 arguments，请使用后续的新版参数构造流程。");

            if (template is null && data.ContainsKey("minecraftArguments"))
            {
                template =
                    MinecraftVersionReader.ReadRequiredString(
                        data,
                        "minecraftArguments",
                        "minecraftArguments 必须是非空字符串。");
            }
        }

        if (template is null)
            throw new InvalidDataException(
                "当前版本及父版本均未声明 minecraftArguments。");

        string mainClass = chain
            .Select(item => item.MainClass)
            .FirstOrDefault(item => item is not null)
            ?? throw new InvalidDataException(
                "当前版本及父版本均未声明 mainClass。");

        if (!Regex.IsMatch(
                mainClass,
                @"\A[A-Za-z_$][A-Za-z0-9_$]*(?:\.[A-Za-z_$][A-Za-z0-9_$]*)*\z",
                RegexOptions.CultureInvariant))
            throw new InvalidDataException(
                "mainClass 不是当前支持的 Java 类名。");

        string versionType = chain
            .Select(item => item.VersionType)
            .FirstOrDefault(item => !string.IsNullOrWhiteSpace(item))
            ?? "custom";

        return (mainClass, template, versionType);
    }

    private static IReadOnlyList<string> ExpandTemplate(
        string template,
        PreparedLaunchConfig config,
        AssetPaths assets,
        string versionType)
    {
        // 真实 1.8.9 使用普通空白分隔的模板。
        // 引号分组格式暂不猜测解析。
        if (template.Contains('"'))
            throw new NotSupportedException(
                "当前旧版参数模板暂不支持双引号分组。");

        if (template.Contains('\0'))
            throw new InvalidDataException(
                "参数模板不能包含空字符。");

        Dictionary<string, string> values =
            new(StringComparer.Ordinal)
            {
                ["auth_player_name"] = config.Account.Username,
                ["auth_uuid"] = config.Account.Uuid.ToString("N"),
                ["auth_access_token"] = config.Account.AccessToken,
                ["user_type"] = config.Account.UserType,
                ["user_properties"] = "{}",
                ["version_name"] = config.Version.Id,
                ["version_type"] = versionType,
                ["game_directory"] = config.GameDirectory,
                ["assets_root"] = assets.AssetsRoot,
                ["assets_index_name"] = assets.IndexId
            };

        // 先分词，再替换。替换出来的空格不会再次分词。
        string[] tokens = template.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries);

        List<string> result = [];

        foreach (string token in tokens)
        {
            string remaining = Placeholder.Replace(token, "");

            if (remaining.Contains("${", StringComparison.Ordinal))
                throw new InvalidDataException(
                    $"占位符格式不完整：{token}");

            string expanded = Placeholder.Replace(token, match =>
            {
                string name = match.Groups[1].Value;

                if (!values.TryGetValue(name, out string? value))
                    throw new NotSupportedException(
                        $"暂未支持参数占位符：{name}");

                return value;
            });

            result.Add(expanded);
        }
        if (config.WindowWidth is int width
            && config.WindowHeight is int height)
        {
            bool alreadySpecified = result.Any(argument =>
                argument == "--width"
                || argument == "--height"
                || argument.StartsWith(
                    "--width=", StringComparison.Ordinal)
                || argument.StartsWith(
                    "--height=", StringComparison.Ordinal));

            if (alreadySpecified)
                throw new NotSupportedException(
                    "旧版参数模板已经指定窗口大小，暂不支持覆盖。");

            result.Add("--width");
            result.Add(width.ToString(CultureInfo.InvariantCulture));

            result.Add("--height");
            result.Add(height.ToString(CultureInfo.InvariantCulture));
        }

        return result;
    }
}
