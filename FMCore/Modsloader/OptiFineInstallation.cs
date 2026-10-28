using FMCore.Downloads;

namespace FMCore.Modsloader;

public enum OptiFineInstallStage
{
    ReadingInstaller,
    CheckingJava,
    PreparingBaseGame,
    ApplyingPatches,
    WritingFiles,
    Completed,
    ReadingVersions,
    DownloadingInstaller
}

public sealed record OptiFineInstallProgress(
    OptiFineInstallStage Stage,
    string Message,
    DownloadBatchProgress? Download = null,
    DownloadProgress? InstallerDownload = null);

public sealed record OptiFineInstallResult(
    string VersionId,
    string GameVersion,
    string OptiFineVersion,
    string MinecraftRoot,
    string VersionDirectory);

public sealed record OptiFineInstallVersion(
    string GameVersion,
    string Edition,
    string FileName,
    string DownloadUrl)
{
    public bool IsPreview => FileName.StartsWith(
        "preview_", StringComparison.OrdinalIgnoreCase);
}

