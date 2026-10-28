using FMCore.Downloads;

namespace FMCore.Modsloader;

public sealed class NeoForgeInstaller
{
    private readonly ForgeInstaller _installer;

    public string VersionId => _installer.VersionId;
    public DownloadSource Source { get; init; } = DownloadSource.Official;

    public NeoForgeInstaller(
        string gameVersion,
        string neoForgeVersion,
        string minecraftRoot,
        string javaPath,
        string? versionId = null,
        int maxConcurrency = 4,
        int processorMemoryMb = 2048)
    {
        _installer = new ForgeInstaller(
            true, gameVersion, neoForgeVersion, minecraftRoot, javaPath,
            versionId, maxConcurrency, processorMemoryMb);
    }

    public static Task<IReadOnlyList<string>> GetVersionsAsync(
        string gameVersion,
        CancellationToken cancellationToken = default)
        => GetVersionsAsync(
            gameVersion, DownloadSource.Official, cancellationToken);

    public static Task<IReadOnlyList<string>> GetVersionsAsync(
        string gameVersion,
        DownloadSource source,
        CancellationToken cancellationToken = default)
        => ForgeInstaller.GetNeoForgeVersionsAsync(
            gameVersion, source, cancellationToken);

    public async Task<NeoForgeInstallResult> InstallAsync(
        Action<NeoForgeInstallProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        ForgeInstallResult result = await _installer.InstallWithSourceAsync(
            Source,
            onProgress is null ? null : progress =>
                onProgress(new NeoForgeInstallProgress(
                    MapStage(progress.Stage),
                    progress.Message,
                    progress.InstallerDownload,
                    progress.Download)),
            cancellationToken).ConfigureAwait(false);

        return new NeoForgeInstallResult(
            result.VersionId,
            result.GameVersion,
            result.ForgeVersion,
            result.MinecraftRoot,
            result.VersionDirectory);
    }

    private static NeoForgeInstallStage MapStage(ForgeInstallStage stage)
        => stage switch
        {
            ForgeInstallStage.CheckingJava => NeoForgeInstallStage.CheckingJava,
            ForgeInstallStage.DownloadingInstaller => NeoForgeInstallStage.DownloadingInstaller,
            ForgeInstallStage.PreparingBaseGame => NeoForgeInstallStage.PreparingBaseGame,
            ForgeInstallStage.DownloadingLibraries => NeoForgeInstallStage.DownloadingLibraries,
            ForgeInstallStage.RunningProcessors => NeoForgeInstallStage.RunningProcessors,
            ForgeInstallStage.WritingProfile => NeoForgeInstallStage.WritingProfile,
            ForgeInstallStage.CompletingResources => NeoForgeInstallStage.CompletingResources,
            ForgeInstallStage.Completed => NeoForgeInstallStage.Completed,
            _ => throw new InvalidOperationException("未知的加载器安装阶段。")
        };
}

