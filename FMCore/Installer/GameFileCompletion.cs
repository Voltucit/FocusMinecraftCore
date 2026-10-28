using FMCore.Downloads;

namespace FMCore.Installer;

public enum GameFileCompletionStage
{
    SelectingArchitecture,
    ReadingMetadata,
    VerifyingGeneratedFiles,
    CompletingClientAndLibraries,
    CompletingResources,
    Completed
}

public sealed record GameFileCompletionProgress(
    GameFileCompletionStage Stage,
    string Message,
    DownloadBatchProgress? Download = null);

public sealed record GameFileBackup(
    string FilePath,
    string BackupPath);

public sealed record GameFileCompletionResult(
    int ClientFiles,
    int LibraryFiles,
    int ResourceMetadataFiles,
    int AssetFiles,
    int DownloadedFiles,
    int ReusedFiles)
{
    public int VerifiedGeneratedFiles { get; init; }

    public int ClientAndLibraryFiles =>
        ClientFiles + LibraryFiles;

    public int TotalFiles =>
        ClientAndLibraryFiles
        + ResourceMetadataFiles
        + AssetFiles;

    public IReadOnlyList<GameFileBackup> Backups { get; init; } =
        Array.Empty<GameFileBackup>();

    public int RepairedFiles => Backups.Count;
}
