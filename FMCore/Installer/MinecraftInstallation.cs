using FMCore.Downloads;

namespace FMCore.Installer;

public enum MinecraftInstallStage
{
    ReadingManifest,
    DownloadingVersionJson,
    CompletingFiles,
    Completed
}

public sealed record MinecraftInstallProgress(
    MinecraftInstallStage Stage,
    string Message,
    DownloadBatchProgress? Download = null);

public sealed record MinecraftInstallResult(
    string VersionId,
    string MinecraftRoot,
    string VersionDirectory,
    GameFileCompletionResult Files);
