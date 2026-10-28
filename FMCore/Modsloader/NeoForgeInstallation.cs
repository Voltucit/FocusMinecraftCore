using FMCore.Downloads;

namespace FMCore.Modsloader;

public enum NeoForgeInstallStage
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

public sealed record NeoForgeInstallProgress(
    NeoForgeInstallStage Stage,
    string Message,
    DownloadProgress? InstallerDownload = null,
    DownloadBatchProgress? Download = null);

public sealed record NeoForgeInstallResult(
    string VersionId,
    string GameVersion,
    string NeoForgeVersion,
    string MinecraftRoot,
    string VersionDirectory);
