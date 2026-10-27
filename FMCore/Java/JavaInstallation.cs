using FMCore.Downloads;

namespace FMCore.Java;

public enum JavaInstallStage
{
    ReadingMetadata,
    DownloadingFiles,
    VerifyingJava,
    Completed
}

public sealed record JavaInstallProgress(
    JavaInstallStage Stage,
    string Message,
    DownloadBatchProgress? Download = null);

public sealed record JavaInstallResult(
    JavaInfo Java,
    string InstallDirectory,
    int TotalFiles,
    int DownloadedFiles,
    int ReusedFiles);
