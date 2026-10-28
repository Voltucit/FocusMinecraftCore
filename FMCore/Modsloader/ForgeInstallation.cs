using FMCore.Downloads;

namespace FMCore.Modsloader;

public enum ForgeInstallStage
{
    CheckingJava,
    DownloadingInstaller,
    PreparingBaseGame,
    DownloadingLibraries,
    RunningProcessors,
    WritingProfile,
    CompletingResources,
    Completed
}

public sealed record ForgeInstallProgress(
    ForgeInstallStage Stage,
    string Message,
    DownloadProgress? InstallerDownload = null,
    DownloadBatchProgress? Download = null);

public sealed record ForgeInstallResult(
    string VersionId,
    string GameVersion,
    string ForgeVersion,
    string MinecraftRoot,
    string VersionDirectory);
