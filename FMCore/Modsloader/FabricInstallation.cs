using FMCore.Downloads;

namespace FMCore.Modsloader;

public sealed record FabricLoaderVersion(string Version, bool Stable);

public enum FabricInstallStage
{
    ReadingMetadata,
    InstallingBaseGame,
    WritingProfile,
    CompletingFiles,
    Completed
}

public sealed record FabricInstallProgress(
    FabricInstallStage Stage,
    string Message,
    DownloadBatchProgress? Download = null);

public sealed record FabricInstallResult(
    string VersionId,
    string GameVersion,
    string LoaderVersion,
    string MinecraftRoot,
    string VersionDirectory);
