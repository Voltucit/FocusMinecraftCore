using FMCore.Assets;
using FMCore.Downloader;
using FMCore.Utilities;
using FMCore.Versions;

namespace FMCore.Installer;

/// <summary>Checks downloaded files and legacy asset copies. Natives are safely rebuilt by repair and each launch.</summary>
public sealed record InstallationCheckResult(ResolvedVersion Version, IReadOnlyList<FileVerificationResult> Files, bool AssetsChecked)
{
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
        var indexRequest = new DownloadRequest(plan.AssetIndex.Download.Url, $"assets/indexes/{plan.AssetIndex.Id}.json")
        { ExpectedHash = plan.AssetIndex.Download.Sha1, ExpectedSize = plan.AssetIndex.Download.Size };
        var requests = new List<DownloadRequest> { plan.Client, indexRequest };
        requests.AddRange(plan.Libraries);
        if (plan.Logging != null) requests.Add(plan.Logging);
        var readable = false;
        if ((await downloads.VerifyAsync(indexRequest, cancellationToken).ConfigureAwait(false)).IsUsable)
        {
            var index = AssetIndex.Parse(await GameFiles.ReadJsonAsync(GameFiles.Resolve(RootDirectory, indexRequest.RelativePath), cancellationToken).ConfigureAwait(false));
            requests.AddRange(assets.CreateDownloads(index));
            foreach (var asset in options.VerifyAssetLayouts ? index.Objects : [])
            {
                if (index.Virtual) AddCopy($"assets/virtual/{plan.AssetIndex.Id}/{asset.Name}", asset);
                if (index.MapToResources) AddCopy((plan.GameDirectory.Length == 0 ? "" : plan.GameDirectory.TrimEnd('/', '\\') + "/") + "resources/" + asset.Name, asset);
            }
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
        return new(version, results.AsReadOnly(), readable);

        void AddCopy(string path, AssetObject asset) => requests.Add(new(new Uri($"https://resources.download.minecraft.net/{asset.Hash[..2]}/{asset.Hash}"), path)
        { ExpectedHash = asset.Hash, ExpectedSize = asset.Size });
    }

    /// <summary>Repairs installation using the same verified, resumable installation pipeline; no valid full file is redownloaded.</summary>
    public Task<InstallationResult> RepairAsync(string versionId, InstallationOptions? options = null,
        IProgress<InstallationProgress>? progress = null, CancellationToken cancellationToken = default) =>
        InstallAsync(versionId, options, progress, cancellationToken);
}
