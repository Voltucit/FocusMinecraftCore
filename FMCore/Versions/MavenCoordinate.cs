using FMCore.Downloader;

namespace FMCore.Versions;

public sealed record MavenCoordinate(string Group, string Artifact, string Version, string? Classifier, string Extension)
{
    internal string ConflictKey => $"{Group}:{Artifact}:{Classifier}@{Extension}";
    public string RelativePath => $"{Group.Replace('.', '/')}/{Artifact}/{Version}/{Artifact}-{Version}{(Classifier == null ? "" : "-" + Classifier)}.{Extension}";

    public static MavenCoordinate Parse(string name)
    {
        var extension = name.Split('@');
        var parts = extension[0].Split(':');
        if (extension.Length > 2 || parts.Length is < 3 or > 4 || parts.Any(string.IsNullOrWhiteSpace) ||
            parts[0].Split('.').Any(string.IsNullOrEmpty)) throw Invalid();
        var coordinate = new MavenCoordinate(parts[0], parts[1], parts[2], parts.Length == 4 ? parts[3] : null,
            extension.Length == 2 ? extension[1] : "jar");
        if (parts.Any(p => p.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '.' and not '-' and not '_' and not '+')) ||
            coordinate.Extension.Length == 0 || coordinate.Extension.Any(c => !char.IsAsciiLetterOrDigit(c))) throw Invalid();
        try { DownloadPath.ValidateRelative(coordinate.RelativePath); }
        catch (DownloadException) { throw new VersionException(VersionErrorCode.UnsafePath, "Maven 坐标包含不安全路径。"); }
        return coordinate;
    }

    private static VersionException Invalid() => new(VersionErrorCode.InvalidMetadata, "Maven 坐标格式无效。");
}
