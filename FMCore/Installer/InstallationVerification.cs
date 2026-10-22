using FMCore.Assets;
using FMCore.Downloader;
using FMCore.Utilities;
using FMCore.Versions;

namespace FMCore.Installer;

/// <summary>Checks downloaded files and legacy asset copies. Natives are safely rebuilt by repair and each launch.</summary>
public sealed record InstallationCheckResult(ResolvedVersion Version, IReadOnlyList<FileVerificationResult> Files, bool AssetsChecked)
{
    /// <summary>Whether extracted natives were checked. False when disabled or source archives are missing/damaged.</summary>
    public bool NativeFilesChecked { get; init; }
    /// <summary>Whether all enumerated downloads/layout copies meet available metadata and the resource index was readable.</summary>
    public bool IsValid => AssetsChecked && Files.All(f => f.IsUsable);
    /// <summary>Missing, size-mismatched or hash-mismatched files.</summary>
    public IReadOnlyList<FileVerificationResult> Problems => Files.Where(f => !f.IsUsable).ToArray();
}

public sealed partial class GameInstaller
{
    /// <summary>Checks local installation files without downloading or changing them. CacheOnly is the default for metadata.</summary>
    public async Task<InstallationCheckResult> VerifyAsync(string versionId, InstallationOptions? options = null,
        IProgress<InstallationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        try { return await VerifyCoreAsync(versionId, options, progress, cancellationToken).ConfigureAwait(false); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { throw new InstallationException(InstallationErrorCode.IoError, "安装文件校验失败。"); }
    }

    private async Task<InstallationCheckResult> VerifyCoreAsync(string versionId, InstallationOptions? options,
        IProgress<InstallationProgress>? progress, CancellationToken cancellationToken)
    {
        options ??= new() { CachePolicy = VersionCachePolicy.CacheOnly };
        var version = await versions.ResolveAsync(versionId, options.CachePolicy, cancellationToken).ConfigureAwait(false);
        var plan = CreatePlan(version, options);
        var indexRequest = AssetManager.IndexRequest(plan.AssetIndex);
        var requests = new List<DownloadRequest> { plan.Client, indexRequest };
        requests.AddRange(plan.Libraries);
        if (plan.Logging != null) requests.Add(plan.Logging);
        var readable = false;
        if ((await downloads.VerifyAsync(indexRequest, cancellationToken).ConfigureAwait(false)).IsUsable)
        {
            var index = AssetIndex.Parse(await GameFiles.ReadJsonAsync(GameFiles.Resolve(RootDirectory, indexRequest.RelativePath), cancellationToken).ConfigureAwait(false));
            requests.AddRange(assets.CreateDownloads(index));
            if (options.VerifyAssetLayouts)
                foreach (var (asset, path) in AssetManager.LayoutTargets(index, plan.AssetIndex.Id, plan.GameDirectory)) AddCopy(path, asset);
            readable = true;
        }
        var unique = requests.DistinctBy(r => r.RelativePath, GameFiles.PathComparer).ToArray();
        var results = new List<FileVerificationResult>();
        progress?.Report(new(InstallationStage.Verification, 0, unique.Length));
        foreach (var request in unique)
        {
            results.Add(await downloads.VerifyAsync(request, cancellationToken).ConfigureAwait(false));
            progress?.Report(new(InstallationStage.Verification, results.Count, unique.Length));
        }
        var nativesChecked = false;
        if (options.VerifyNativeFiles && plan.NativeArchives.All(a => results.Any(r => GameFiles.PathComparer.Equals(r.RelativePath, a.RelativePath) && r.IsUsable)))
        {
            var nativeProgress = new InlineProgress<NativeExtractionProgress>(p => progress?.Report(new(InstallationStage.NativeVerification, p.CompletedArchives, p.TotalArchives)));
            results.AddRange(await new NativeExtractor(RootDirectory, options.NativeExtraction).VerifyAsync(plan.NativeArchives,
                plan.NativeDirectory, nativeProgress, cancellationToken).ConfigureAwait(false));
            nativesChecked = true;
        }
        return new(version, results.AsReadOnly(), readable) { NativeFilesChecked = nativesChecked };

        void AddCopy(string path, AssetObject asset) => requests.Add(new(new Uri($"https://resources.download.minecraft.net/{asset.Hash[..2]}/{asset.Hash}"), path)
        { ExpectedHash = asset.Hash, ExpectedSize = asset.Size });
    }

    /// <summary>Repairs installation using the same verified, resumable installation pipeline; no valid full file is redownloaded.</summary>
    public Task<InstallationResult> RepairAsync(string versionId, InstallationOptions? options = null,
        IProgress<InstallationProgress>? progress = null, CancellationToken cancellationToken = default) =>
        InstallAsync(versionId, options, progress, cancellationToken);
}
