using System.IO.Compression;
using System.Text.Json.Nodes;
using FMCore.Installer;
using FMCore.Java;
using FMCore.Versions;

namespace FMCore.Launch;

internal static class NativeExtractor
{
    // 当前阶段的保护上限，不是 Minecraft 格式本身的限制。
    private const int MaxArchives = 64;
    private const int MaxEntries = 4096;
    private const long MaxArchiveBytes = 128L * 1024 * 1024;
    private const long MaxInputBytes = 512L * 1024 * 1024;
    private const long MaxFileBytes = 64L * 1024 * 1024;
    private const long MaxTotalBytes = 256L * 1024 * 1024;

    private static readonly uint[] CrcTable = CreateCrcTable();

    // 保存解压条目和相对路径，避免预检后重新打开、重新选择条目。
    private sealed record PlannedFile(
        ZipArchiveEntry Entry,
        string RelativePath);

    internal static async Task<string?> ExtractWindowsAsync(
        MinecraftVersion version,
        string minecraftRoot,
        JavaInfo java,
        CancellationToken cancellationToken = default,
        IReadOnlyList<JsonObject>? selectedLibraries = null,
        string? outputDirectory = null)
    {
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(java);
        ArgumentException.ThrowIfNullOrWhiteSpace(minecraftRoot);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException(
                "当前原生库解压只支持 Windows。");

        cancellationToken.ThrowIfCancellationRequested();

        string versionDirectory = Path.GetFullPath(version.Directory);
        CheckNoLinks(versionDirectory);

        if (!Directory.Exists(versionDirectory))
            throw new DirectoryNotFoundException(
                $"版本目录不存在：{versionDirectory}");

        if (outputDirectory is not null)
        {
            outputDirectory = PathSafety.RequireDirectoryPath(outputDirectory);
            if (!PathSafety.IsWithin(outputDirectory, versionDirectory)
                || PathSafety.Comparer.Equals(outputDirectory, versionDirectory))
                throw new InvalidDataException("原生库输出目录必须位于当前版本目录内部。");

            CheckNoLinks(outputDirectory);
            if (Path.Exists(outputDirectory))
                throw new IOException($"输出路径已存在：{outputDirectory}");
        }

        List<ZipArchive> archives = [];
        List<PlannedFile> plan = [];

        Dictionary<string, HashSet<string>> seenArchives =
            new(PathSafety.Comparer);

        HashSet<string> fileNames =
            new(StringComparer.OrdinalIgnoreCase);

        HashSet<string> directoryNames =
            new(StringComparer.OrdinalIgnoreCase);

        List<string> createdFiles = [];
        List<string> createdDirectories = [];

        int entryCount = 0;
        long inputBytes = 0;
        long plannedBytes = 0;

        try
        {
            // 第一部分：只读取和检查，不创建输出目录。
            foreach (JsonObject library
                     in selectedLibraries
                        ?? LibraryResolver.GetWindowsLibraryEntries(version, minecraftRoot))
            {
                cancellationToken.ThrowIfCancellationRequested();

                string? archivePath =
                    LibraryResolver.GetSelectedWindowsNativeLibraryPath(
                        library, minecraftRoot, java);

                if (archivePath is null)
                    continue;

                HashSet<string> excludes = ReadExcludes(library);

                if (seenArchives.TryGetValue(
                        archivePath, out HashSet<string>? previous))
                {
                    if (!previous.SetEquals(excludes))
                        throw new InvalidDataException(
                            $"同一个原生库的排除规则不一致：{archivePath}");

                    continue;
                }

                if (seenArchives.Count >= MaxArchives)
                    throw new InvalidDataException("原生库包数量超过上限。");

                seenArchives.Add(archivePath, excludes);

                ZipArchive archive = OpenArchive(archivePath, out long size);
                archives.Add(archive);

                inputBytes = checked(inputBytes + size);

                if (inputBytes > MaxInputBytes)
                    throw new InvalidDataException(
                        "原生库压缩包总大小超过上限。");

                if (archive.Entries.Count > MaxEntries - entryCount)
                    throw new InvalidDataException(
                        "原生库压缩条目总数超过上限。");

                entryCount += archive.Entries.Count;

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string name = entry.FullName.Replace('\\', '/');
                    bool isDirectory =
                        name.EndsWith("/", StringComparison.Ordinal);

                    string relative =
                        isDirectory ? name[..^1] : name;

                    // 即使被排除，也不接受危险路径和链接条目。
                    PathSafety.ValidateWindowsRelativePath(relative);
                    ValidateEntryType(entry, isDirectory);

                    if (excludes.Any(prefix =>
                            name.StartsWith(
                                prefix, StringComparison.Ordinal)))
                        continue;

                    RegisterPath(
                        relative, isDirectory,
                        fileNames, directoryNames);

                    if (isDirectory)
                    {
                        if (entry.Length != 0)
                            throw new InvalidDataException(
                                $"目录条目不应包含文件内容：{name}");

                        continue;
                    }

                    if (entry.Length < 0 || entry.Length > MaxFileBytes)
                        throw new InvalidDataException(
                            $"原生库文件大小超过上限：{name}");

                    plannedBytes = checked(plannedBytes + entry.Length);

                    if (plannedBytes > MaxTotalBytes)
                        throw new InvalidDataException(
                            "原生库解压总大小超过上限。");

                    plan.Add(new PlannedFile(entry, relative));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (plan.Count == 0)
                return null;

            // 第二部分：全部预检通过后，才创建本次专用目录。
            outputDirectory ??= Path.Combine(
                versionDirectory, $"natives-{Guid.NewGuid():N}");

            CreateDirectory(outputDirectory, createdDirectories);

            byte[] buffer = new byte[81920];

            foreach (PlannedFile item in plan)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string target = Path.GetFullPath(Path.Combine(
                    outputDirectory,
                    item.RelativePath.Replace(
                        '/', Path.DirectorySeparatorChar)));

                if (!PathSafety.IsWithin(target, outputDirectory))
                    throw new InvalidDataException(
                        "解压目标越出了本次原生库目录。");

                EnsureParents(
                    outputDirectory,
                    item.RelativePath,
                    createdDirectories);

                CheckNoLinks(target);

                using Stream source = item.Entry.Open();

                // CreateNew：已有同名文件就失败，绝不覆盖。
                await using FileStream destination = new(
                    target,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 81920,
                    useAsync: true);

                createdFiles.Add(target);

                long written = 0;
                uint crc = uint.MaxValue;

                while (true)
                {
                    int count = await source.ReadAsync(
                        buffer.AsMemory(), cancellationToken);

                    if (count == 0)
                        break;

                    written = checked(written + count);

                    // 不只相信 ZIP 声明的大小，也检查实际读出的大小。
                    if (written > item.Entry.Length
                        || written > MaxFileBytes)
                        throw new InvalidDataException(
                            $"原生库实际大小异常：{item.RelativePath}");

                    crc = UpdateCrc(crc, buffer, count);

                    await destination.WriteAsync(
                        buffer.AsMemory(0, count), cancellationToken);
                }

                if (written != item.Entry.Length
                    || ~crc != item.Entry.Crc32)
                    throw new InvalidDataException(
                        $"原生库文件内容校验失败：{item.RelativePath}");
            }

            cancellationToken.ThrowIfCancellationRequested();
            return outputDirectory;
        }
        catch (Exception originalError)
        {
            List<Exception> cleanupErrors =
                Cleanup(createdFiles, createdDirectories);

            if (cleanupErrors.Count != 0)
            {
                cleanupErrors.Insert(0, originalError);

                throw new AggregateException(
                    "原生库解压失败，且部分本次创建的内容未能清理。",
                    cleanupErrors);
            }

            throw;
        }
        finally
        {
            foreach (ZipArchive archive in archives)
                archive.Dispose();
        }
    }

    private static HashSet<string> ReadExcludes(JsonObject library)
    {
        // 原生库 JAR 常带有同名签名和模块说明，不属于需要解压的系统库。
        HashSet<string> result = new(StringComparer.Ordinal) { "META-INF/" };

        if (!library.TryGetPropertyValue("extract", out JsonNode? node))
            return result;

        if (node is not JsonObject extract)
            throw new InvalidDataException(
                "原生库 extract 必须是对象。");

        if (!extract.TryGetPropertyValue("exclude", out JsonNode? list))
            return result;

        if (list is not JsonArray array)
            throw new InvalidDataException(
                "原生库 extract.exclude 必须是列表。");

        foreach (JsonNode? item in array)
        {
            if (item is not JsonValue value
                || !value.TryGetValue<string>(out string? text)
                || string.IsNullOrWhiteSpace(text))
                throw new InvalidDataException(
                    "原生库排除项必须是非空字符串。");

            string prefix = text.Replace('\\', '/');

            string path = prefix.EndsWith("/", StringComparison.Ordinal)
                ? prefix[..^1]
                : prefix;

            PathSafety.ValidateWindowsRelativePath(path);
            result.Add(prefix);
        }

        return result;
    }

    private static ZipArchive OpenArchive(
        string path, out long size)
    {
        FileStream stream = new(
            path, FileMode.Open, FileAccess.Read, FileShare.Read);

        try
        {
            size = stream.Length;

            if (size > MaxArchiveBytes)
                throw new InvalidDataException(
                    $"原生库压缩包过大：{path}");

            // archive.Dispose() 会同时关闭这里的 stream。
            return new ZipArchive(
                stream, ZipArchiveMode.Read, leaveOpen: false);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    private static void ValidateEntryType(
        ZipArchiveEntry entry, bool isDirectory)
    {
        uint attributes = unchecked((uint)entry.ExternalAttributes);
        uint unixType = (attributes >> 16) & 0xF000u;

        // Unix：0 表示未声明，8000 是普通文件，4000 是目录。
        // 符号链接、设备等其他类型一律拒绝。
        if (unixType != 0
            && unixType != (isDirectory ? 0x4000u : 0x8000u))
            throw new InvalidDataException(
                $"不支持的原生库条目类型：{entry.FullName}");

        if ((attributes & (uint)FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException(
                $"原生库不能包含链接条目：{entry.FullName}");

        if (!isDirectory
            && (attributes & (uint)FileAttributes.Directory) != 0)
            throw new InvalidDataException(
                $"原生库目录标记不一致：{entry.FullName}");
    }

    private static void RegisterPath(
        string relative,
        bool isDirectory,
        HashSet<string> files,
        HashSet<string> directories)
    {
        string[] parts = relative.Split('/');
        string current = "";

        for (int index = 0; index < parts.Length; index++)
        {
            current = index == 0
                ? parts[index]
                : current + "/" + parts[index];

            bool directory =
                index < parts.Length - 1 || isDirectory;

            if (directory)
            {
                if (files.Contains(current))
                    throw new InvalidDataException(
                        $"原生库文件和目录冲突：{current}");

                directories.Add(current);
            }
            else
            {
                if (directories.Contains(current) || !files.Add(current))
                    throw new InvalidDataException(
                        $"原生库出现重名文件或目录冲突：{current}");
            }
        }
    }

    private static void EnsureParents(
        string outputDirectory,
        string relative,
        List<string> createdDirectories)
    {
        string[] parts = relative.Split('/');
        string current = outputDirectory;

        for (int index = 0; index < parts.Length - 1; index++)
        {
            current = Path.Combine(current, parts[index]);
            CheckNoLinks(current);

            if (!Directory.Exists(current))
                CreateDirectory(current, createdDirectories);
        }
    }

    private static void CreateDirectory(
        string path, List<string> createdDirectories)
    {
        CheckNoLinks(path);

        if (Path.Exists(path))
            throw new IOException($"输出路径已存在：{path}");

        string parent = Path.GetDirectoryName(path)!;

        if (!Directory.Exists(parent))
            throw new DirectoryNotFoundException(
                $"输出父目录不存在：{parent}");

        Directory.CreateDirectory(path);
        createdDirectories.Add(path);
    }

    private static void CheckNoLinks(string path)
    {
        string full = Path.GetFullPath(path);
        string root = Path.GetPathRoot(full)!;
        string current = root;

        string[] parts = full[root.Length..].Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries);

        foreach (string part in parts)
        {
            current = Path.Combine(current, part);

            FileAttributes attributes;

            try
            {
                attributes = File.GetAttributes(current);
            }
            catch (FileNotFoundException)
            {
                return;
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }

            if ((attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException(
                    $"原生库输出路径不能经过链接或目录联接点：{current}");
        }
    }

    private static List<Exception> Cleanup(
        List<string> files, List<string> directories)
    {
        List<Exception> errors = [];

        // 只处理本次记录下来的文件，不扫描其他用户文件。
        for (int index = files.Count - 1; index >= 0; index--)
        {
            string path = files[index];

            try
            {
                CheckNoLinks(path);
                File.Delete(path);
            }
            catch (Exception error)
            {
                errors.Add(new IOException(
                    $"无法清理本次创建的文件：{path}", error));
            }
        }

        // 从内层向外层删除，并且只允许删除空目录。
        for (int index = directories.Count - 1; index >= 0; index--)
        {
            string path = directories[index];

            try
            {
                CheckNoLinks(path);

                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: false);
            }
            catch (Exception error)
            {
                errors.Add(new IOException(
                    $"无法清理本次创建的目录：{path}", error));
            }
        }

        return errors;
    }

    private static uint[] CreateCrcTable()
    {
        uint[] table = new uint[256];

        for (int index = 0; index < table.Length; index++)
        {
            uint value = (uint)index;

            for (int bit = 0; bit < 8; bit++)
                value = (value & 1) != 0
                    ? (value >> 1) ^ 0xEDB88320u
                    : value >> 1;

            table[index] = value;
        }

        return table;
    }

    private static uint UpdateCrc(
        uint crc, byte[] buffer, int count)
    {
        for (int index = 0; index < count; index++)
            crc = CrcTable[(int)((crc ^ buffer[index]) & 0xFF)]
                ^ (crc >> 8);

        return crc;
    }
}
