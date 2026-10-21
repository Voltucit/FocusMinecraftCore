namespace FMCore.Versions;

/// <summary>A read-only inventory result. HasClientJar means a resolved, nonempty file exists, not that the game is complete.</summary>
public sealed record LocalVersionInfo(string Id, string? Type, string? MetadataPath, string? ClientJarId,
    string? ClientJarPath, bool HasClientJar, VersionErrorCode? ErrorCode);
