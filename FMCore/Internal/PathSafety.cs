namespace FMCore.Internal;

internal static class PathSafety
{
    internal static StringComparer Comparer => OperatingSystem.IsWindows()
        ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    // GetFullPath 只消除词法上的 . 和 ..；还需要解析已存在的符号链接和目录联接点。
    internal static string Resolve(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (path == "~" || path.StartsWith("~/", StringComparison.Ordinal)
            || path.StartsWith("~\\", StringComparison.Ordinal))
        {
            path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                path.Length == 1 ? "" : path[2..]);
        }

        string full = Path.GetFullPath(path);
        string root = Path.GetPathRoot(full)!;
        string current = root;
        string[] parts = full[root.Length..].Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);
        for (int index = 0; index < parts.Length; index++)
        {
            current = Path.Combine(current, parts[index]);
            FileAttributes attributes;
            try { attributes = File.GetAttributes(current); }
            catch (FileNotFoundException) { return AppendRemaining(current, parts, index + 1); }
            catch (DirectoryNotFoundException) { return AppendRemaining(current, parts, index + 1); }

            if ((attributes & FileAttributes.ReparsePoint) != 0)
            {
                FileSystemInfo info = (attributes & FileAttributes.Directory) != 0
                    ? new DirectoryInfo(current) : new FileInfo(current);
                FileSystemInfo? target = info.ResolveLinkTarget(returnFinalTarget: true);
                if (target is null)
                    throw new IOException($"无法解析文件系统链接：{current}");
                current = target.FullName;
            }
            if (index < parts.Length - 1 && (attributes & FileAttributes.Directory) == 0)
                throw new IOException($"路径中的父级被文件占用：{current}");
        }
        return Path.TrimEndingDirectorySeparator(current);
    }

    private static string AppendRemaining(string current, string[] parts, int start)
    {
        for (int index = start; index < parts.Length; index++)
            current = Path.Combine(current, parts[index]);
        return Path.TrimEndingDirectorySeparator(current);
    }

    internal static string RequireDirectoryPath(string path)
    {
        string result = Resolve(path);
        if (File.Exists(result))
            throw new IOException($"路径应为目录，但已被文件占用：{result}");
        return result;
    }

    internal static bool IsWithin(string target, string root)
    {
        string relative = Path.GetRelativePath(root, target);
        return !Path.IsPathRooted(relative) && relative != ".."
            && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal)
            && !relative.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal);
    }

    internal static void ValidateWindowsRelativePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (path.StartsWith('/') || path.StartsWith('\\') || path.Contains(':'))
            throw new InvalidDataException("依赖文件路径必须是安全的相对路径。");
        string[] parts = path.Replace('\\', '/').Split('/');
        foreach (string part in parts)
        {
            if (part is "" or "." or ".." || part.EndsWith(' ') || part.EndsWith('.')
                || part.Any(c => c < 32 || "<>:\"|?*".Contains(c)))
                throw new InvalidDataException("依赖文件路径包含不安全或 Windows 保留字符。");
            string stem = part.Split('.')[0];
            if (stem.Equals("CON", StringComparison.OrdinalIgnoreCase)
                || stem.Equals("PRN", StringComparison.OrdinalIgnoreCase)
                || stem.Equals("AUX", StringComparison.OrdinalIgnoreCase)
                || stem.Equals("NUL", StringComparison.OrdinalIgnoreCase)
                || (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                    || stem.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))
                    && "123456789¹²³".Contains(stem[3])))
                throw new InvalidDataException("依赖文件路径包含 Windows 保留名称。");
        }
    }
}
