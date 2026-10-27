using FMCore.Downloads;

namespace FMCore.Installer;

public enum ResourceCompletionStage
{
    ReadingMetadata,
    CompletingMetadata,
    ReadingAssetObjects,
    CompletingAssetObjects,
    Completed
}

public sealed record ResourceCompletionProgress(
    ResourceCompletionStage Stage,
    string Message,
    DownloadBatchProgress? Download = null);

public sealed record ResourceFileBackup(
    string FilePath,
    string BackupPath);

public sealed record ResourceCompletionResult(
    int MetadataFiles,
    int AssetFiles,
    int DownloadedFiles,
    int ReusedFiles)
{
    public int TotalFiles => MetadataFiles + AssetFiles;

    public IReadOnlyList<ResourceFileBackup> Backups { get; init; } =
        Array.Empty<ResourceFileBackup>();

    public int RepairedFiles => Backups.Count;
}
