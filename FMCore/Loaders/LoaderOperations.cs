using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FMCore.Downloader;
using FMCore.Installer;
using FMCore.Utilities;
using FMCore.Versions;

namespace FMCore.Loaders;

// Concrete shared file operations; no public loader framework or parallel version/download model.
internal sealed class LoaderOperations
{
    internal DownloadManager Downloads { get; }
    internal VersionManager Versions { get; }
    internal string Root => Downloads.RootDirectory;
    private readonly string cache;
    internal LoaderOperations(DownloadManager downloads, VersionManager? versions, string name, Uri endpoint)
    {
        Downloads = downloads; Versions = versions ?? new(downloads);
        if (!GameFiles.PathComparer.Equals(Root, Versions.RootDirectory)) throw new ArgumentException("加载器与版本管理器必须使用同一根目录。");
        if (!endpoint.IsAbsoluteUri || endpoint.Scheme != "https" || endpoint.UserInfo.Length != 0 || endpoint.Query.Length != 0 || endpoint.Fragment.Length != 0)
            throw new ArgumentException("加载器服务必须使用无凭据、查询或片段的 HTTPS 根地址。");
        cache = ".cache/loaders/" + name + "/" + Key(endpoint.AbsoluteUri);
    }
    internal static string Key(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    internal async Task<T> ReadAsync<T>(Uri source, VersionCachePolicy policy, Func<string, T> parse, CancellationToken ct,
        IProgress<DownloadProgress>? progress = null)
    {
        if (!Enum.IsDefined(policy)) throw new ArgumentOutOfRangeException(nameof(policy));
        var path = GameFiles.Resolve(Root, cache + "/" + Key(source.AbsoluteUri) + ".metadata");
        if (policy != VersionCachePolicy.Refresh && File.Exists(path))
        {
            try { return parse(await GameFiles.ReadJsonAsync(path, ct).ConfigureAwait(false)); }
            catch (Exception e) when (policy == VersionCachePolicy.PreferCache && e is JsonException or InvalidDataException or
                InvalidOperationException or FormatException or LoaderException or VersionException) { }
        }
        if (policy == VersionCachePolicy.CacheOnly) throw new LoaderException(LoaderErrorCode.NotCached, "所需加载器元数据或校验信息尚未缓存。");
        var relative = cache + "/incoming/" + Guid.NewGuid().ToString("N") + ".tmp";
        var temporary = GameFiles.Resolve(Root, relative);
        try
        {
            await Downloads.DownloadAsync(new(source, relative), progress, ct).ConfigureAwait(false);
            var text = await GameFiles.ReadJsonAsync(temporary, ct).ConfigureAwait(false);
            var result = parse(text);
            await GameFiles.WriteAtomicAsync(path, async (s, token) => await s.WriteAsync(Encoding.UTF8.GetBytes(text), token).ConfigureAwait(false), ct).ConfigureAwait(false);
            return result;
        }
        finally { if (File.Exists(temporary)) { DownloadPath.CheckLinks(temporary); File.Delete(temporary); } }
    }
    internal FileStream AcquireLease()
    {
        var path = GameFiles.Resolve(Root, ".cache/loaders/install.lock");
        GameFiles.CreateDirectory(Path.GetDirectoryName(path)!);
        try { return new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
        catch (IOException e) when ((e.HResult & 0xffff) is 32 or 33 or 11)
        { throw new LoaderException(LoaderErrorCode.InstallationBusy, "同一根目录已有加载器安装任务。"); }
    }
    internal static string Sha1(string text)
    {
        var hash = text.Trim();
        if (hash.Length != 40 || !hash.All(char.IsAsciiHexDigit)) throw Invalid();
        return hash.ToLowerInvariant();
    }
    internal async Task CheckProfileAsync(JsonObject profile, string game, CancellationToken ct)
    {
        var id = VersionParser.Id(profile["id"]!.GetValue<string>());
        if (id.Equals(game, StringComparison.OrdinalIgnoreCase)) throw new LoaderException(LoaderErrorCode.ProfileConflict, "加载器不能覆盖原版 ID。");
        var path = Versions.GetMetadataPath(id);
        if (File.Exists(path) && !FabricMetadata.Equivalent(VersionParser.Parse(await GameFiles.ReadJsonAsync(path, ct).ConfigureAwait(false)).ToJson(), profile))
            throw new LoaderException(LoaderErrorCode.ProfileConflict, "目标 ID 已有不同配置，请使用新的 VersionId。");
    }
    internal async Task<InstallationResult> CompleteAsync(ResolvedVersion parent, JsonObject profile, InstallationOptions options,
        IProgress<LoaderInstallationProgress>? progress, CancellationToken ct)
    {
        await CheckProfileAsync(profile, parent.Metadata.Id, ct).ConfigureAwait(false);
        var path = Versions.GetMetadataPath(profile["id"]!.GetValue<string>());
        var existed = File.Exists(path);
        var child = VersionParser.ParseObject(profile);
        var resolved = new ResolvedVersion(VersionInheritance.Merge(parent.Metadata, child), parent.ClientJarId, parent.ClientDownload,
            Array.AsReadOnly(parent.InheritanceChain.Append(child.Id).ToArray()));
        var installed = await new GameInstaller(Downloads, Versions).InstallResolvedAsync(resolved, options,
            new InlineProgress<InstallationProgress>(p => progress?.Report(new(LoaderInstallationStage.Dependencies,
                p.CompletedFiles, p.TotalFiles, p))), ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        if (existed)
        {
            if (!File.Exists(path)) throw new LoaderException(LoaderErrorCode.ProfileConflict, "安装期间版本配置已被删除。");
            await CheckProfileAsync(profile, parent.Metadata.Id, ct).ConfigureAwait(false);
        }
        else
        {
            GameFiles.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = Path.Combine(Path.GetDirectoryName(path)!, ".fmcore-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                    await JsonSerializer.SerializeAsync(stream, profile, cancellationToken: ct).ConfigureAwait(false);
                ct.ThrowIfCancellationRequested(); DownloadPath.CheckLinks(path);
                try { File.Move(temporary, path, false); }
                catch (IOException) when (File.Exists(path)) { throw new LoaderException(LoaderErrorCode.ProfileConflict, "安装期间版本配置被其他应用创建。"); }
            }
            finally { if (File.Exists(temporary)) { DownloadPath.CheckLinks(temporary); File.Delete(temporary); } }
        }
        return installed;
    }
    internal static async Task<T> RunAsync<T>(SemaphoreSlim gate, Func<Task<T>> action, CancellationToken ct)
    {
        await gate.WaitAsync(ct).ConfigureAwait(false);
        try { return await action().ConfigureAwait(false); }
        catch (VersionException e) when (e.Code == VersionErrorCode.UnsafePath)
        { throw new LoaderException(LoaderErrorCode.UnsafePath, "加载器元数据路径不安全。"); }
        catch (DownloadException e) when (e.Code == DownloadErrorCode.UnsafePath)
        { throw new LoaderException(LoaderErrorCode.UnsafePath, "加载器路径越界或包含链接。"); }
        catch (Exception e) when (e is JsonException or InvalidDataException or InvalidOperationException or FormatException or System.Xml.XmlException or NullReferenceException or OverflowException ||
            e is VersionException { Code: VersionErrorCode.InvalidMetadata }) { throw Invalid(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { throw new LoaderException(LoaderErrorCode.IoError, "加载器文件操作失败。"); }
        finally { gate.Release(); }
    }
    internal static LoaderException Invalid() => new(LoaderErrorCode.InvalidMetadata, "加载器元数据或校验信息无效。");
    internal static LoaderException Unsupported() => new(LoaderErrorCode.UnsupportedFormat, "尚不支持此安装格式，未跳过任何必要步骤。");
    internal sealed class InlineProgress<T>(Action<T> action) : IProgress<T> { public void Report(T value) => action(value); }
}
