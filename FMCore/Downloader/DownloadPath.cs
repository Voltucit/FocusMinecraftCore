namespace FMCore.Downloader;

internal static class DownloadPath
{
    internal static string Resolve(string root, string relative)
    {
        ValidateRelative(relative);
        var parts = relative.Replace('\\', '/').Split('/');
        var fullPath = Path.GetFullPath(Path.Combine(root, Path.Combine(parts)));
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var prefix = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(prefix, comparison)) throw Unsafe();
        CheckLinks(fullPath);
        return fullPath;
    }

    internal static void ValidateRelative(string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative)) throw Unsafe();
        var parts = relative.Replace('\\', '/').Split('/');
        foreach (var part in parts)
        {
            if (part.Length == 0 || part is "." or ".." || part.EndsWith('.') || part.EndsWith(' ') ||
                part.Any(char.IsControl) || part.IndexOfAny([':', '*', '?', '"', '<', '>', '|']) >= 0)
                throw Unsafe();
            var stem = part.Split('.')[0].ToUpperInvariant();
            if (stem is "CON" or "PRN" or "AUX" or "NUL" or "CONIN$" or "CONOUT$" ||
                (stem.Length == 4 && (stem.StartsWith("COM", StringComparison.Ordinal) || stem.StartsWith("LPT", StringComparison.Ordinal)) &&
                "123456789¹²³".Contains(stem[3]))) throw Unsafe();
        }
    }

    internal static void CheckLinks(string path)
    {
        for (string? current = path; current != null; current = Path.GetDirectoryName(current))
        {
            try
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw Unsafe();
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
    }

    private static DownloadException Unsafe() => new(DownloadErrorCode.UnsafePath, "目标路径不安全，或包含链接、设备名称及无效路径片段。");
}
