using FMCore.Downloader;
using FMCore.Instances;
using FMCore.Modpacks;
using FMCore.Utilities;
using System.Security.Cryptography;

namespace FMCore.Resources;

/// <summary>Installs Modrinth mods into one isolated instance, with ownership receipts, required dependencies and transactional updates.</summary>
public sealed class ModrinthInstaller
{
    private readonly ResourceInstance context;
    private readonly ModrinthClient client;
    private readonly DownloadManager downloads;
    private readonly ResourceFiles files;

    /// <summary>Uses an FMCore instance snapshot and automatically reads Minecraft/loader metadata. A stale/deleted instance fails.</summary>
    public ModrinthInstaller(InstanceInfo instance, ModrinthClient? client = null, DownloadOptions? downloadOptions = null)
        : this((instance ?? throw new ArgumentNullException(nameof(instance))).RootDirectory, instance.Id, null, client, downloadOptions, instance) { }

    /// <summary>Uses root/instances/instanceId with an explicit environment, including before game installation. Existing managed instances must use the snapshot constructor.</summary>
    public ModrinthInstaller(string rootDirectory, string instanceId, ModEnvironment environment,
        ModrinthClient? client = null, DownloadOptions? downloadOptions = null)
        : this(rootDirectory, instanceId, ModrinthMetadata.Environment(environment), client, downloadOptions, null) { }

    private ModrinthInstaller(string rootDirectory, string id, ModEnvironment? environment, ModrinthClient? client,
        DownloadOptions? downloadOptions, InstanceInfo? instance)
    {
        context = new(rootDirectory, id, environment, instance);
        GameDirectory = context.GameDirectory; files = new(GameDirectory);
        this.client = client ?? new();
        try { downloads = new(this.client.Http, GameDirectory, downloadOptions); }
        catch (DownloadException error) { throw ResourceInstance.Map(error); }
    }

    /// <summary>Absolute isolated game directory. Shared versions, libraries and assets are never modified by this module.</summary>
    public string GameDirectory { get; }
    internal ModrinthInstaller WithPackScope(PackResourceScope scope) { context.PackScope = scope; files.PackScope = scope; return this; }

    /// <summary>Reads only FMCore-managed Modrinth receipts, recovering interrupted updates under the instance lock. Does not adopt user mods.</summary>
    public Task<IReadOnlyList<InstalledMod>> GetInstalledAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (environment, ct) => Freeze((await files.ReadAsync(environment, ct).ConfigureAwait(false)).Mods), cancellationToken);

    /// <summary>Installs the newest compatible release (or an explicit version), its required dependencies and an origin receipt.</summary>
    /// <remarks>Unknown/user-modified files cause Conflict. Cancellation/failure before commit restores old files. No authentication fallback or loader installation occurs.</remarks>
    public Task<ModInstallResult> InstallAsync(string projectId, string? versionId = null,
        IProgress<ResourceProgress>? progress = null, CancellationToken cancellationToken = default) =>
        ExecuteAsync((environment, ct) => InstallCoreAsync(projectId, versionId, environment, progress, ct), cancellationToken);

    /// <summary>Checks managed projects for newer compatible releases without changing active files. Unknown user mods are excluded.</summary>
    public Task<IReadOnlyList<ModUpdate>> CheckUpdatesAsync(CancellationToken cancellationToken = default) => ExecuteAsync(async (environment, ct) =>
    {
        var receipt = await files.ReadAsync(environment, ct).ConfigureAwait(false);
        await files.VerifyOwnedAsync(receipt.Mods, ct).ConfigureAwait(false);
        var updates = new List<ModUpdate>();
        foreach (var installed in receipt.Mods)
        {
            var latest = await LatestAsync(installed.ProjectId, environment, ct).ConfigureAwait(false);
            if (latest.Id != installed.VersionId && latest.PublishedAt > installed.PublishedAt) updates.Add(new(installed, latest));
        }
        return (IReadOnlyList<ModUpdate>)updates.AsReadOnly();
    }, cancellationToken);

    /// <summary>Updates one managed project and necessary dependencies. A failed update retains the previous complete files/receipt.</summary>
    public Task<ModInstallResult> UpdateAsync(string projectId, IProgress<ResourceProgress>? progress = null,
        CancellationToken cancellationToken = default) => ExecuteAsync(async (environment, ct) =>
    {
        var project = await client.GetProjectAsync(ModrinthMetadata.Token(projectId), ct).ConfigureAwait(false);
        var receipt = await files.ReadAsync(environment, ct).ConfigureAwait(false);
        var installed = receipt.Mods.FirstOrDefault(m => m.ProjectId == project.Id) ??
            throw new ResourceException(ResourceErrorCode.NotFound, "此实例没有该项目的 Modrinth 安装记录。");
        var latest = await LatestAsync(project.Id, environment, ct).ConfigureAwait(false);
        var version = latest.Id != installed.VersionId && latest.PublishedAt > installed.PublishedAt ? latest.Id : installed.VersionId;
        return await InstallCoreAsync(project.Id, version, environment, progress, ct).ConfigureAwait(false);
    }, cancellationToken);

    private async Task<ModInstallResult> InstallCoreAsync(string projectId, string? versionId, ModEnvironment environment,
        IProgress<ResourceProgress>? progress, CancellationToken ct)
    {
        ModrinthMetadata.Token(projectId); if (versionId != null) ModrinthMetadata.Token(versionId);
        var before = await files.ReadAsync(environment, ct).ConfigureAwait(false);
        await files.VerifyOwnedAsync(before.Mods, ct).ConfigureAwait(false);
        progress?.Report(new(ResourceStage.Resolving));
        var plan = await new ModrinthResolution(client, environment, before.Mods).ResolveAsync(projectId, versionId, ct).ConfigureAwait(false);
        var replacements = plan.Select(v =>
        {
            var artifact = ModrinthMetadata.Artifact(v);
            var existing = before.Mods.FirstOrDefault(m => m.ProjectId == v.ProjectId && m.VersionId == v.Id &&
                m.Sha512 == artifact.Sha512 && m.FileName == artifact.FileName);
            if (existing == null) return new InstalledMod(v.ProjectId, v.Id, v.VersionNumber, v.PublishedAt, artifact.FileName, artifact.Sha512,
                artifact.Size, DateTimeOffset.UtcNow, v.Dependencies);
            if (existing.Size != artifact.Size) throw ModrinthMetadata.Invalid();
            return existing.VersionNumber == v.VersionNumber && existing.PublishedAt == v.PublishedAt && existing.Dependencies.SequenceEqual(v.Dependencies)
                ? existing : existing with { VersionNumber = v.VersionNumber, PublishedAt = v.PublishedAt, Dependencies = v.Dependencies };
        }).ToArray();
        var all = before.Mods.Where(m => replacements.All(n => n.ProjectId != m.ProjectId)).Concat(replacements).OrderBy(m => m.ProjectId, StringComparer.Ordinal).ToArray();
        if (all.Length > 512 || all.Select(m => m.FileName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != all.Length) throw ModrinthMetadata.Conflict();
        ModrinthResolution.ValidateRelations(all);
        await CheckUnmanagedAsync(before.Mods, all, ct).ConfigureAwait(false);
        foreach (var mod in replacements)
        {
            var old = before.Mods.FirstOrDefault(m => GameFiles.PathComparer.Equals(m.FileName, mod.FileName));
            if (old != null && old.ProjectId != mod.ProjectId || old == null && (File.Exists(files.ModPath(mod)) || Directory.Exists(files.ModPath(mod))))
                throw ModrinthMetadata.Conflict();
        }
        var reused = replacements.All(m => before.Mods.Contains(m));
        if (!reused)
        {
            progress?.Report(new(ResourceStage.Downloading));
            var requests = plan.Where(v => !replacements.Where(m => before.Mods.Contains(m)).Any(m => m.VersionId == v.Id)).Select(v =>
            {
                var file = ModrinthMetadata.Artifact(v);
                return new DownloadRequest(file.Url, ResourceFiles.Cache(file.Sha512))
                { ExpectedHash = file.Sha512, ExpectedSize = file.Size, HashAlgorithm = DownloadHashAlgorithm.Sha512 };
            }).DistinctBy(r => r.RelativePath).ToArray();
            await downloads.DownloadManyAsync(requests, progress == null ? null : new ForwardProgress(progress), ct).ConfigureAwait(false);
            progress?.Report(new(ResourceStage.Committing));
            await files.CommitAsync(before, new(1, Guid.NewGuid(), environment, all), ct).ConfigureAwait(false);
        }
        progress?.Report(new(ResourceStage.Completed));
        return new(Freeze(replacements), reused);
    }

    private async Task<ModrinthVersion> LatestAsync(string project, ModEnvironment environment, CancellationToken ct)
    {
        var versions = await client.GetVersionsAsync(project, environment, ct).ConfigureAwait(false);
        if (versions.Any(v => v.ProjectId != project)) throw ModrinthMetadata.Invalid();
        return versions.FirstOrDefault() ?? throw new ResourceException(ResourceErrorCode.Incompatible, "项目没有适配目标环境的版本。");
    }

    internal async Task CheckUnmanagedAsync(IReadOnlyList<InstalledMod> owned, IReadOnlyList<InstalledMod> next, CancellationToken ct, IReadOnlySet<string>? additionalOwnedNames = null)
    {
        var directory = files.At("mods");
        if (!Directory.Exists(directory)) return;
        var count = 0;
        long totalBytes = 0;
        foreach (var path in Directory.EnumerateFileSystemEntries(directory))
        {
            ct.ThrowIfCancellationRequested();
            if (++count > 1024) throw new ResourceException(ResourceErrorCode.Unsupported, "实例 mods 目录条目过多，无法安全完成有界检查。");
            var name = Path.GetFileName(path);
            files.At("mods/" + ModrinthMetadata.FileName(name));
            if (additionalOwnedNames?.Contains(name) == true || owned.Any(m => GameFiles.PathComparer.Equals(m.FileName, name)) || !name.EndsWith(".jar", StringComparison.OrdinalIgnoreCase) || Directory.Exists(path)) continue;
            await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
            totalBytes += input.Length;
            if (input.Length > 512L * 1024 * 1024 || totalBytes > 2L * 1024 * 1024 * 1024)
                throw new ResourceException(ResourceErrorCode.Unsupported, "用户 Mod 超过检查大小限制；未修改该文件。");
            var hash = Convert.ToHexStringLower(await SHA512.HashDataAsync(input, ct).ConfigureAwait(false));
            var identified = await client.GetVersionByHashAsync(hash, ct).ConfigureAwait(false);
            if (identified == null) continue; // Unknown user files remain untouched; no guessed identity or compatibility claim.
            if (next.Any(m => m.ProjectId == identified.ProjectId)) throw ModrinthMetadata.Conflict();
            if (identified.Dependencies.Any(d => d.Type == ModDependencyType.Incompatible && d.ProjectId == null && d.VersionId == null))
                throw new ResourceException(ResourceErrorCode.DependencyConflict, "已识别的手动 Mod 存在无法自动解析的外部冲突声明。");
            foreach (var mod in next)
            {
                if (mod.Dependencies.Any(d => d.Type == ModDependencyType.Incompatible && Matches(d, identified.ProjectId, identified.Id)) ||
                    identified.Dependencies.Any(d => d.Type == ModDependencyType.Incompatible && Matches(d, mod.ProjectId, mod.VersionId)))
                    throw ModrinthMetadata.Conflict();
            }
        }
        static bool Matches(ModDependency d, string project, string version) => (d.ProjectId != null || d.VersionId != null) &&
            (d.ProjectId == null || d.ProjectId == project) && (d.VersionId == null || d.VersionId == version);
    }

    private Task<T> ExecuteAsync<T>(Func<ModEnvironment, CancellationToken, Task<T>> action, CancellationToken ct) => context.ExecuteAsync(action, ct);
    private static IReadOnlyList<InstalledMod> Freeze(IEnumerable<InstalledMod> mods) => ResourceInstance.Freeze(mods);
    private sealed class ForwardProgress(IProgress<ResourceProgress> target) : IProgress<DownloadProgress>
    { public void Report(DownloadProgress value) => target.Report(new(ResourceStage.Downloading, value)); }
}
