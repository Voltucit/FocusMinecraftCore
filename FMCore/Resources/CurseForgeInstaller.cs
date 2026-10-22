using System.Globalization;
using System.Security.Cryptography;
using FMCore.Downloader;
using FMCore.Instances;
using FMCore.Modpacks;
using FMCore.Utilities;

namespace FMCore.Resources;

/// <summary>Installs CurseForge Minecraft Mods with required dependencies, policy enforcement and shared transaction recovery.</summary>
public sealed class CurseForgeInstaller
{
    private static readonly HttpClient SharedDownloads = new() { Timeout = Timeout.InfiniteTimeSpan };
    private readonly ResourceInstance context;
    private readonly ResourceFiles files;
    private readonly CurseForgeClient client;
    private readonly DownloadManager downloads;

    /// <summary>Uses a managed instance snapshot and its local Minecraft/loader metadata. API and download connections remain separate.</summary>
    public CurseForgeInstaller(InstanceInfo instance, CurseForgeClient client, DownloadOptions? downloadOptions = null, HttpClient? downloadClient = null)
        : this((instance ?? throw new ArgumentNullException(nameof(instance))).RootDirectory, instance.Id, null, instance, client, downloadOptions, downloadClient) { }
    /// <summary>Uses root/instances/instanceId with an explicit environment. Existing managed instances must use the snapshot constructor.</summary>
    public CurseForgeInstaller(string rootDirectory, string instanceId, ModEnvironment environment, CurseForgeClient client,
        DownloadOptions? downloadOptions = null, HttpClient? downloadClient = null)
        : this(rootDirectory, instanceId, ModrinthMetadata.Environment(environment), null, client, downloadOptions, downloadClient) { }
    private CurseForgeInstaller(string root, string id, ModEnvironment? target, InstanceInfo? instance, CurseForgeClient client,
        DownloadOptions? options, HttpClient? downloadClient)
    {
        ArgumentNullException.ThrowIfNull(client);
        context = new(root, id, target, instance); files = new(context.GameDirectory, "curseforge"); this.client = client;
        var http = downloadClient ?? SharedDownloads;
        if (http.DefaultRequestHeaders.Any(h => h.Key.Equals("x-api-key", StringComparison.OrdinalIgnoreCase) ||
            h.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase) || h.Key.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase) ||
            h.Key.Equals("Cookie", StringComparison.OrdinalIgnoreCase))) throw new ResourceException(ResourceErrorCode.InvalidRequest, "文件下载连接必须独立且不得配置认证凭据。");
        try { downloads = new(http, context.GameDirectory, options); }
        catch (DownloadException) { throw ModrinthMetadata.InvalidRequest(); }
    }
    /// <summary>Absolute isolated game directory.</summary>
    public string GameDirectory => context.GameDirectory;
    internal CurseForgeInstaller WithPackScope(PackResourceScope scope) { context.PackScope = scope; files.PackScope = scope; return this; }
    /// <summary>Reads only CurseForge ownership receipts, recovering interrupted updates under the shared instance lock.</summary>
    public Task<IReadOnlyList<InstalledMod>> GetInstalledAsync(CancellationToken cancellationToken = default) => context.ExecuteAsync(async (environment, ct) =>
        ResourceInstance.Freeze((await files.ReadAsync(environment, ct).ConfigureAwait(false)).Mods), cancellationToken);
    /// <summary>Installs a compatible release (or an explicit file ID) and required dependencies. Forbidden automatic downloads fail explicitly.</summary>
    public Task<ModInstallResult> InstallAsync(int modId, int? fileId = null, IProgress<ResourceProgress>? progress = null,
        CancellationToken cancellationToken = default) => context.ExecuteAsync((environment, ct) => InstallCoreAsync(modId, fileId, environment, progress, ct), cancellationToken);
    /// <summary>Checks managed projects for newer compatible publication dates; never changes active files.</summary>
    public Task<IReadOnlyList<CurseForgeUpdate>> CheckUpdatesAsync(CancellationToken cancellationToken = default) => context.ExecuteAsync(async (environment, ct) =>
    {
        var receipt = await files.ReadAsync(environment, ct).ConfigureAwait(false);
        await files.VerifyOwnedAsync(receipt.Mods, ct).ConfigureAwait(false);
        var resolver = new CurseForgeResolution(client, environment, receipt.Mods);
        var result = new List<CurseForgeUpdate>();
        foreach (var mod in receipt.Mods)
        {
            var latest = await resolver.LatestAsync(Number(mod.ProjectId), ct).ConfigureAwait(false);
            if (latest.Id != Number(mod.VersionId) && latest.PublishedAt > mod.PublishedAt) result.Add(new(ResourceInstance.Freeze([mod])[0], latest));
        }
        return (IReadOnlyList<CurseForgeUpdate>)result.AsReadOnly();
    }, cancellationToken);
    /// <summary>Updates one managed project and necessary dependencies; failed downloads or commits retain/restore the previous complete files.</summary>
    public Task<ModInstallResult> UpdateAsync(int modId, IProgress<ResourceProgress>? progress = null, CancellationToken cancellationToken = default) =>
        context.ExecuteAsync(async (environment, ct) =>
        {
            CurseForgeMetadata.Id(modId);
            var receipt = await files.ReadAsync(environment, ct).ConfigureAwait(false);
            var installed = receipt.Mods.FirstOrDefault(m => m.ProjectId == Text(modId)) ?? throw new ResourceException(ResourceErrorCode.NotFound, "没有该项目的 CurseForge 安装记录。");
            var latest = await new CurseForgeResolution(client, environment, receipt.Mods).LatestAsync(modId, ct).ConfigureAwait(false);
            return await InstallCoreAsync(modId, latest.PublishedAt > installed.PublishedAt ? latest.Id : Number(installed.VersionId), environment, progress, ct).ConfigureAwait(false);
        }, cancellationToken);

    private async Task<ModInstallResult> InstallCoreAsync(int modId, int? fileId, ModEnvironment environment, IProgress<ResourceProgress>? progress, CancellationToken ct)
    {
        CurseForgeMetadata.Id(modId); if (fileId is { } exact) CurseForgeMetadata.Id(exact);
        var before = await files.ReadAsync(environment, ct).ConfigureAwait(false);
        await files.VerifyOwnedAsync(before.Mods, ct).ConfigureAwait(false);
        var otherFiles = new ResourceFiles(GameDirectory);
        var foreign = await otherFiles.ReadAsync(environment, ct).ConfigureAwait(false);
        await otherFiles.VerifyOwnedAsync(foreign.Mods, ct).ConfigureAwait(false);
        progress?.Report(new(ResourceStage.Resolving));
        var resolver = new CurseForgeResolution(client, environment, before.Mods);
        var plan = await resolver.ResolveAsync(modId, fileId, ct).ConfigureAwait(false);
        var replacements = new List<InstalledMod>(); var transfers = new List<CurseForgeFile>();
        foreach (var file in plan)
        {
            var old = before.Mods.FirstOrDefault(m => m.ProjectId == Text(file.ModId) && m.VersionId == Text(file.Id) &&
                m.FileName == file.FileName && m.Size == file.Size);
            var sameBytes = old != null && await GameFiles.HasSha1Async(files.ModPath(old), file.Sha1!, file.Size, ct).ConfigureAwait(false);
            var dependencies = CurseForgeMetadata.Dependencies(file);
            var value = sameBytes ? old! : new InstalledMod(Text(file.ModId), Text(file.Id), file.DisplayName, file.PublishedAt,
                file.FileName, new string('0', 128), file.Size, DateTimeOffset.UtcNow, dependencies) { Source = "curseforge" };
            if (sameBytes && (value.VersionNumber != file.DisplayName || value.PublishedAt != file.PublishedAt || !value.Dependencies.SequenceEqual(dependencies)))
                value = value with { VersionNumber = file.DisplayName, PublishedAt = file.PublishedAt, Dependencies = dependencies };
            replacements.Add(value); if (!sameBytes) transfers.Add(file);
        }
        var all = Combine(before.Mods, replacements);
        ModrinthResolution.ValidateRelations(all);
        foreach (var mod in replacements)
        {
            var old = before.Mods.FirstOrDefault(m => GameFiles.PathComparer.Equals(m.FileName, mod.FileName));
            if (old != null && old.ProjectId != mod.ProjectId || old == null && (File.Exists(files.ModPath(mod)) || Directory.Exists(files.ModPath(mod)))) throw ModrinthMetadata.Conflict();
        }
        await CheckUnmanagedAsync(before.Mods, all, plan, resolver, ct).ConfigureAwait(false);
        var reused = replacements.All(m => before.Mods.Contains(m));
        if (!reused)
        {
            progress?.Report(new(ResourceStage.Downloading));
            await downloads.DownloadManyAsync(transfers.Select(file => new DownloadRequest(file.DownloadUrl!, TransferPath(file))
                { ExpectedHash = file.Sha1, ExpectedSize = file.Size, HashAlgorithm = DownloadHashAlgorithm.Sha1 }),
                progress == null ? null : new ForwardProgress(progress), ct).ConfigureAwait(false);
            foreach (var file in transfers)
            {
                var path = files.At(TransferPath(file));
                await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
                var hash = Convert.ToHexStringLower(await SHA512.HashDataAsync(input, ct).ConfigureAwait(false));
                input.Position = 0;
                await GameFiles.WriteAtomicAsync(files.At(ResourceFiles.Cache(hash, "curseforge")),
                    (stream, token) => input.CopyToAsync(stream, token), ct).ConfigureAwait(false);
                var index = replacements.FindIndex(m => m.ProjectId == Text(file.ModId)); replacements[index] = replacements[index] with { Sha512 = hash };
            }
            all = Combine(before.Mods, replacements);
            // Cross-platform identical content is a duplicate even if the file was renamed.
            if (all.Any(m => foreign.Mods.Any(n => n.Sha512 == m.Sha512))) throw ModrinthMetadata.Conflict();
            progress?.Report(new(ResourceStage.Committing));
            await files.CommitAsync(before, new(1, Guid.NewGuid(), environment, all), ct).ConfigureAwait(false);
        }
        progress?.Report(new(ResourceStage.Completed));
        return new(ResourceInstance.Freeze(replacements), reused);
    }
    internal async Task CheckUnmanagedAsync(IReadOnlyList<InstalledMod> owned, IReadOnlyList<InstalledMod> next,
        IReadOnlyList<CurseForgeFile> plan, CurseForgeResolution resolver, CancellationToken ct, IReadOnlySet<string>? additionalOwnedNames = null)
    {
        var directory = files.At("mods"); if (!Directory.Exists(directory)) return;
        var unknown = new List<(string Path, string Sha1)>(); var entries = 0; long bytes = 0;
        foreach (var path in Directory.EnumerateFileSystemEntries(directory))
        {
            ct.ThrowIfCancellationRequested();
            if (++entries > 1024) throw new ResourceException(ResourceErrorCode.Unsupported, "mods 目录条目超过检查范围。");
            var name = Path.GetFileName(path); files.At("mods/" + ModrinthMetadata.FileName(name));
            if (additionalOwnedNames?.Contains(name) == true || owned.Any(m => GameFiles.PathComparer.Equals(m.FileName, name)) || Directory.Exists(path) || !name.EndsWith(".jar", StringComparison.OrdinalIgnoreCase)) continue;
            await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
            bytes += input.Length;
            if (input.Length > 512L * 1024 * 1024 || bytes > 2L * 1024 * 1024 * 1024) throw new ResourceException(ResourceErrorCode.Unsupported, "用户文件超过安全检查范围。");
            unknown.Add((path, Convert.ToHexStringLower(await SHA1.HashDataAsync(input, ct).ConfigureAwait(false))));
        }
        if (unknown.Count == 0) return;
        var identities = plan.Select(f => f.ModId).Concat(next.SelectMany(m => m.Dependencies.Where(d => d.Type == ModDependencyType.Incompatible).Select(d => Number(d.ProjectId!)))).Distinct();
        foreach (var project in identities)
        {
            var known = await resolver.AllAsync(project, ct).ConfigureAwait(false);
            if (known.Any(file => file.Sha1 != null && unknown.Any(u => u.Sha1 == file.Sha1))) throw ModrinthMetadata.Conflict();
        }
    }
    private static IReadOnlyList<InstalledMod> Combine(IReadOnlyList<InstalledMod> previous, IReadOnlyList<InstalledMod> next)
    {
        var all = previous.Where(m => next.All(n => n.ProjectId != m.ProjectId)).Concat(next).OrderBy(m => Number(m.ProjectId)).ToArray();
        if (all.Length > 512 || all.Select(m => m.FileName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != all.Length) throw ModrinthMetadata.Conflict();
        return Array.AsReadOnly(all);
    }
    private static string TransferPath(CurseForgeFile file) => $".fmcore/resources/curseforge-downloads/{file.ModId}-{file.Id}-{file.Sha1}.jar";
    private static string Text(int value) => value.ToString(CultureInfo.InvariantCulture);
    private static int Number(string value) => int.Parse(value, CultureInfo.InvariantCulture);
    private sealed class ForwardProgress(IProgress<ResourceProgress> target) : IProgress<DownloadProgress>
    { public void Report(DownloadProgress value) => target.Report(new(ResourceStage.Downloading, value)); }
}
