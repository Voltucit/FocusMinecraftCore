using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FMCore.Installer;
using FMCore.Launch;

namespace FMCore.Modsloader;

public sealed partial class ForgeInstaller
{
    private async Task<Dictionary<string, string>> ReadDataAsync(
        ZipArchive archive,
        JsonObject install,
        string installerPath,
        string clientJar,
        CancellationToken cancellationToken)
    {
        Dictionary<string, string> result = new(StringComparer.Ordinal);

        if (install["data"] is JsonObject entries)
        {
            foreach (var pair in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Regex.IsMatch(pair.Key, @"\A[A-Za-z_][A-Za-z0-9_]*\z"))
                    throw new InvalidDataException("Forge 数据项名称无效。");

                if (pair.Value is not JsonObject entry)
                    throw new InvalidDataException("Forge 数据项必须是对象。");

                if (entry["client"] is null)
                    continue;

                string value = ReadText(entry, "client");

                if (value.StartsWith('[') && value.EndsWith(']'))
                {
                    result[pair.Key] = ArtifactPath(value[1..^1]);
                }
                else if (value.StartsWith('\'') && value.EndsWith('\'')
                         && value.Length >= 2)
                {
                    result[pair.Key] = value[1..^1];
                }
                else
                {
                    byte[] bytes = await ReadEntryAsync(
                        archive, value, 64 * 1024 * 1024,
                        cancellationToken).ConfigureAwait(false);

                    string directory = Path.GetDirectoryName(installerPath)!;
                    string path = PathSafety.Resolve(
                        Path.Combine(directory, "data-" + pair.Key + ".bin"));

                    if (!PathSafety.IsWithin(path, directory))
                        throw new InvalidDataException("Forge 安装数据路径越界。");

                    // 校验值来自已经通过官方 SHA-1 验证的安装包内容。
                    ExpectedFile expected = new(
                        path, Convert.ToHexString(SHA1.HashData(bytes)), bytes.Length);

                    if (File.Exists(path))
                    {
                        await RequireFileAsync(
                            expected, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await WriteNewFileAsync(
                            path, bytes, cancellationToken).ConfigureAwait(false);
                    }

                    result[pair.Key] = path;
                }
            }
        }
        else if (install["data"] is not null)
        {
            throw new InvalidDataException("Forge data 必须是对象。");
        }

        result["SIDE"] = "client";
        result["MINECRAFT_JAR"] = clientJar;
        result["MINECRAFT_VERSION"] = _gameVersion;
        result["ROOT"] = _root;
        result["INSTALLER"] = installerPath;
        result["LIBRARY_DIR"] = PathSafety.RequireDirectoryPath(
            Path.Combine(_root, "libraries"));

        return result;
    }

    private string ExpandProcessorValue(
        string value,
        IReadOnlyDictionary<string, string> data)
    {
        if (value.StartsWith('[') && value.EndsWith(']'))
            return ArtifactPath(value[1..^1]);

        return Regex.Replace(
            value,
            @"\{([A-Za-z_][A-Za-z0-9_]*)\}",
            match => data.TryGetValue(match.Groups[1].Value, out string? result)
                ? result
                : throw new InvalidDataException(
                    $"Forge 数据项不存在：{match.Groups[1].Value}"));
    }

    private async Task RunProcessorsAsync(
        JsonObject install,
        IReadOnlyDictionary<string, string> data,
        string javaPath,
        Action<string> onLog,
        CancellationToken cancellationToken)
    {
        if (install["processors"] is not JsonArray processors)
            throw new InvalidDataException("安装包缺少 processors 列表。");

        List<JsonObject> selected = [];

        foreach (JsonNode? node in processors)
        {
            if (node is not JsonObject processor)
                throw new InvalidDataException("处理器必须是对象。");

            if (processor["sides"] is JsonArray sides)
            {
                if (!sides.Any(side => side?.GetValue<string>() == "client"))
                    continue;
            }
            else if (processor["sides"] is not null)
            {
                throw new InvalidDataException("处理器 sides 必须是数组。");
            }

            selected.Add(processor);
        }

        for (int index = 0; index < selected.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            JsonObject processor = selected[index];
            List<ExpectedFile> outputs = [];

            if (processor["outputs"] is JsonObject declared)
            {
                foreach (var pair in declared)
                {
                    string path = PathSafety.Resolve(
                        ExpandProcessorValue(pair.Key, data));

                    if (!PathSafety.IsWithin(path, _root)
                        || Directory.Exists(path))
                        throw new InvalidDataException("处理器产物路径无效或越界。");

                    string hash = ExpandProcessorValue(
                        pair.Value?.GetValue<string>()
                            ?? throw new InvalidDataException("处理器缺少产物校验值。"),
                        data);

                    outputs.Add(new ExpectedFile(path, ValidateSha1(hash)));
                }
            }
            else if (processor["outputs"] is not null)
            {
                throw new InvalidDataException("处理器 outputs 必须是对象。");
            }

            bool ready = outputs.Count > 0;

            foreach (ExpectedFile file in outputs)
            {
                bool matches = await MatchesAsync(
                    file, cancellationToken).ConfigureAwait(false);

                if (File.Exists(file.Path) && !matches)
                    throw new InvalidDataException(
                        $"已有生成文件校验失败，文件已保留：{file.Path}");

                ready &= matches;
            }

            onLog($"处理器 {index + 1}/{selected.Count}"
                + (ready ? "：产物校验通过，复用。" : "：正在执行……"));

            if (ready)
                continue;

            foreach (ExpectedFile file in outputs)
                Directory.CreateDirectory(Path.GetDirectoryName(file.Path)!);

            string jar = ArtifactPath(ReadText(processor, "jar"));
            List<string> classPath = [jar];

            if (processor["classpath"] is JsonArray dependencies)
            {
                foreach (JsonNode? node in dependencies)
                {
                    string name = node?.GetValue<string>()
                        ?? throw new InvalidDataException("处理器 classpath 项无效。");
                    classPath.Add(ArtifactPath(name));
                }
            }
            else if (processor["classpath"] is not null)
            {
                throw new InvalidDataException("处理器 classpath 必须是数组。");
            }

            foreach (string path in classPath)
            {
                if (!File.Exists(path))
                    throw new FileNotFoundException("缺少处理器依赖。", path);
            }

            using ZipArchive tool = ZipFile.OpenRead(jar);
            byte[] manifest = await ReadEntryAsync(
                tool, "META-INF/MANIFEST.MF", 2 * 1024 * 1024,
                cancellationToken).ConfigureAwait(false);

            string manifestText = Encoding.UTF8.GetString(manifest)
                .Replace("\r\n ", "").Replace("\n ", "");

            Match main = Regex.Match(
                manifestText, @"^Main-Class: ([^\r\n]+)",
                RegexOptions.Multiline | RegexOptions.IgnoreCase);

            if (!main.Success)
                throw new InvalidDataException("处理器 JAR 缺少 Main-Class。");

            ProcessStartInfo startInfo = new()
            {
                FileName = javaPath,
                WorkingDirectory = _root,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            startInfo.ArgumentList.Add($"-Xmx{_processorMemoryMb}M");
            startInfo.ArgumentList.Add("-Dfile.encoding=UTF-8");
            startInfo.ArgumentList.Add("-cp");
            startInfo.ArgumentList.Add(string.Join(";", classPath));
            startInfo.ArgumentList.Add(main.Groups[1].Value.Trim());

            if (processor["args"] is not JsonArray arguments)
                throw new InvalidDataException("处理器缺少 args。");

            foreach (JsonNode? node in arguments)
            {
                string value = node?.GetValue<string>()
                    ?? throw new InvalidDataException("处理器参数必须是字符串。");
                startInfo.ArgumentList.Add(ExpandProcessorValue(value, data));
            }

            foreach (string key in new[]
                     { "JAVA_TOOL_OPTIONS", "JDK_JAVA_OPTIONS", "_JAVA_OPTIONS" })
                startInfo.Environment.Remove(key);

            using CancellationTokenSource deadline = new(TimeSpan.FromMinutes(10));
            using CancellationTokenSource linked =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, deadline.Token);

            try
            {
                int exitCode = await Launcher.RunProcessAsync(
                    startInfo, onLog, onLog, linked.Token,
                    onProgress: null).ConfigureAwait(false);

                if (exitCode != 0)
                    throw new InvalidOperationException(
                        $"Forge 处理器 {index + 1} 失败，退出码 {exitCode}。");
            }
            catch (OperationCanceledException error)
                when (!cancellationToken.IsCancellationRequested
                      && deadline.IsCancellationRequested)
            {
                throw new TimeoutException("Forge 单个处理器运行超过 10 分钟。", error);
            }

            foreach (ExpectedFile file in outputs)
                await RequireFileAsync(file, cancellationToken).ConfigureAwait(false);
        }
    }
}
