namespace FMCore.Versions;

/// <summary>A read-only inventory result. HasClientJar means a resolved, nonempty file exists, not that the game is complete.</summary>
public sealed record LocalVersionInfo(string Id, string? Type, string? MetadataPath, string? ClientJarId,
    string? ClientJarPath, bool HasClientJar, VersionErrorCode? ErrorCode)
{
    /// <summary>JSON 中的原始 id，可与用于查询/启动的目录名 Id 不同。</summary>
    public string? MetadataId { get; init; }
    /// <summary>实际版本目录的绝对路径。</summary>
    public string? VersionDirectory { get; init; }
}
