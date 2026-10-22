using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using FMCore.Downloader;
using FMCore.Installer;
using FMCore.Java;
using FMCore.Logging;
using FMCore.Utilities;
using FMCore.Versions;

namespace FMCore.Loaders;

// Runs the vendor's installer in an SDK-owned staging directory, then imports verified artifacts only.
internal sealed class InstallerLoader
{
    internal LoaderOperations Files { get; }
    private readonly string name, groupPath;
    private readonly Uri maven;
    private readonly JavaManager? java;
    private readonly Func<ProcessStartInfo, ProcessStartInfo>? transform;
    private readonly SemaphoreSlim gate = new(1, 1);
    internal InstallerLoader(string name, DownloadManager downloads, VersionManager? versions, Uri maven, string groupPath,
        JavaManager? java = null, Func<ProcessStartInfo, ProcessStartInfo>? transform = null)
    {
        this.name = name; this.groupPath = groupPath; this.java = java; this.transform = transform;
        Files = new(downloads, versions, name.ToLowerInvariant(), maven);
        this.maven = new(maven.AbsoluteUri.TrimEnd('/') + "/");
    }
    internal Task<IReadOnlyList<MavenLoaderVersion>> GetVersionsAsync(string game, VersionCachePolicy policy, CancellationToken ct) =>
        LoaderOperations.RunAsync(gate, () => VersionsAsync(game, policy, ct), ct);
    private async Task<IReadOnlyList<MavenLoaderVersion>> VersionsAsync(string game, VersionCachePolicy policy, CancellationToken ct)
    {
        VersionParser.Id(game);
        var raw = await Files.ReadAsync(new(maven, groupPath + "/maven-metadata.xml"), policy, LoaderMaven.Versions, ct).ConfigureAwait(false);
        return raw.Where(v => MatchesGame(v, game)).Select(v => new MavenLoaderVersion(game,
            name == "Forge" ? v[(game.Length + 1)..] : v, v, v.Contains("beta", StringComparison.OrdinalIgnoreCase) || v.Contains("alpha", StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(v => LoaderMaven.SortKey(v.Version), StringComparer.Ordinal).ThenBy(v => v.IsPrerelease).ToArray();
    }
    private bool MatchesGame(string version, string game)
    {
        if (name == "Forge") return version.StartsWith(game + "-", StringComparison.Ordinal);
        // NeoForge 20.x/21.x encode Minecraft 1.20.x/1.21.x; future year-based schemas are not guessed.
        var p = game.Split('.');
        if (p.Length is < 2 or > 3 || p[0] != "1" || !int.TryParse(p[1], out var minor) || minor is < 20 or > 21) return false;
        var patch = p.Length == 3 ? p[2] : "0";
        return version.StartsWith(p[1] + "." + patch + ".", StringComparison.Ordinal);
    }
    internal Task<LoaderInstallationResult> InstallAsync(string game, string? version, LoaderInstallOptions? options,
        IProgress<LoaderInstallationProgress>? progress, CancellationToken ct) => LoaderOperations.RunAsync(gate, async () =>
    {
        VersionParser.Id(game); if (version != null) VersionParser.Id(version);
        options ??= new(); ArgumentNullException.ThrowIfNull(options.Installation);
        if (options.InstallerTimeout <= TimeSpan.Zero || options.InstallerTimeout > TimeSpan.FromHours(1)) throw new ArgumentOutOfRangeException(nameof(options));
        using var lease = Files.AcquireLease();
        var policy = options.Installation.CachePolicy;
        progress?.Report(new(LoaderInstallationStage.Metadata));
        var list = await VersionsAsync(game, policy, ct).ConfigureAwait(false);
        var selected = version == null ? list.FirstOrDefault(v => !v.IsPrerelease) : list.FirstOrDefault(v => v.Version == version || v.MavenVersion == version);
        if (selected == null) throw new LoaderException(LoaderErrorCode.UnsupportedCombination, "官方 Maven 列表没有请求的游戏/加载器组合。");
        var leaf = groupPath.Split('/')[^1] + "-" + selected.MavenVersion + "-installer.jar";
        var source = new Uri(maven, groupPath + "/" + selected.MavenVersion + "/" + leaf);
        var sha = await Files.ReadAsync(new(source.AbsoluteUri + ".sha1"), policy, LoaderOperations.Sha1, ct).ConfigureAwait(false);
        var pairKey = LoaderOperations.Key(game + ":" + selected.MavenVersion);
        var relativeJar = ".cache/loaders/installers/" + name.ToLowerInvariant() + "/" + pairKey + ".jar";
        var downloaded = await Files.Downloads.DownloadAsync(new(source, relativeJar) { ExpectedHash = sha },
            new LoaderOperations.InlineProgress<DownloadProgress>(p => progress?.Report(new(LoaderInstallationStage.InstallerDownload, Download: p))), ct).ConfigureAwait(false);
        using var archive = new InstallerArchive(downloaded.FullPath, game, name, selected.MavenVersion);
        archive.ValidateClientData(); var outputs = archive.Outputs(); var requiredOutputs = archive.RequiredOutputs();
        var profile = (JsonObject)archive.Version.DeepClone();
        profile["id"] = VersionParser.Id(options.VersionId ?? profile["id"]!.GetValue<string>());
        var id = profile["id"]!.GetValue<string>();
        if (id.Equals(game, StringComparison.OrdinalIgnoreCase)) throw new LoaderException(LoaderErrorCode.ProfileConflict, "加载器不能覆盖原版 ID。");
        var receipt = new JsonObject { ["name"] = name, ["version"] = selected.Version, ["installerSha1"] = sha, ["format"] = 1 };
        var parent = await Files.Versions.ResolveAsync(game, policy, ct).ConfigureAwait(false);
        var installedProfile = Files.Versions.GetMetadataPath(id);
        if (File.Exists(installedProfile))
        {
            var existing = VersionParser.Parse(await GameFiles.ReadJsonAsync(installedProfile, ct).ConfigureAwait(false));
            var canonical = existing.ToJson(); canonical.Remove("fmcoreGeneratedFiles"); canonical.Remove("fmcoreInstaller");
            if (!FabricMetadata.Equivalent(canonical, profile) || !JsonNode.DeepEquals(existing.ToJson()["fmcoreInstaller"], receipt))
                throw new LoaderException(LoaderErrorCode.ProfileConflict, "目标已有不同安装器或版本配置，请使用新的 VersionId。");
            var check = await new GameInstaller(Files.Downloads, Files.Versions).VerifyAsync(id, options.Installation with { CachePolicy = VersionCachePolicy.CacheOnly }, cancellationToken: ct).ConfigureAwait(false);
            if (check.IsValid)
            {
                var reused = await Files.CompleteAsync(parent, existing.ToJson(), options.Installation, progress, ct).ConfigureAwait(false);
                progress?.Report(new(LoaderInstallationStage.Completed, 1, 1));
                return new LoaderInstallationResult(name, game, selected.Version, id, reused);
            }
        }
        var javas = java ?? new(options.JavaDetection);
        var runtime = await javas.SelectAsync(parent.Metadata, options.JavaPath, cancellationToken: ct).ConfigureAwait(false);
        var installationOptions = options.Installation with { Context = runtime.GetRuleContext(options.Installation.Context.Features) };
        await new GameInstaller(Files.Downloads, Files.Versions).InstallAsync(game, installationOptions,
            new LoaderOperations.InlineProgress<InstallationProgress>(p => progress?.Report(new(LoaderInstallationStage.Dependencies, p.CompletedFiles, p.TotalFiles, p))), ct).ConfigureAwait(false);
        var workspace = GameFiles.Resolve(Files.Root, ".cache/loaders/work/" + name.ToLowerInvariant() + "/" + pairKey);
        GameFiles.CreateDirectory(workspace);
        var baseRelative = $"versions/{game}/{game}";
        await CopyAsync(Files.Versions.GetMetadataPath(game), GameFiles.Resolve(workspace, baseRelative + ".json"), ct).ConfigureAwait(false);
        await CopyAsync(GameFiles.Resolve(Files.Root, $"versions/{parent.ClientJarId}/{parent.ClientJarId}.jar"), GameFiles.Resolve(workspace, baseRelative + ".jar"), ct).ConfigureAwait(false);
        await File.WriteAllTextAsync(GameFiles.Resolve(workspace, "launcher_profiles.json"), "{\"profiles\":{}}", ct).ConfigureAwait(false);
        var installMetadata = VersionParser.ParseObject(new JsonObject { ["id"] = "installer", ["libraries"] = archive.Install["libraries"]!.DeepClone() });
        var requests = installMetadata.Libraries.Concat(VersionParser.ParseObject(profile).Libraries)
            .Where(l => l.Artifact != null).Select(l => new DownloadRequest(l.Artifact!.Url, "libraries/" + (l.Artifact.Path ?? l.Coordinate.RelativePath))
            { ExpectedHash = l.Artifact.Sha1, ExpectedSize = l.Artifact.Size, LocalOnly = l.Artifact.IsLocalOnly }).ToArray();
        if (requests.Any(r => r.ExpectedHash == null)) throw LoaderOperations.Unsupported();
        var unique = requests.GroupBy(r => r.RelativePath, StringComparer.OrdinalIgnoreCase).Select(g =>
        {
            if (g.Select(r => r.ExpectedHash).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 1) throw LoaderOperations.Invalid();
            return g.First();
        }).ToArray();
        await Files.Downloads.DownloadManyAsync(unique.Where(r => !r.LocalOnly),
            new LoaderOperations.InlineProgress<DownloadProgress>(p => progress?.Report(new(LoaderInstallationStage.Dependencies, Download: p))), ct).ConfigureAwait(false);
        foreach (var r in unique.Where(r => !r.LocalOnly))
            await CopyAsync(GameFiles.Resolve(Files.Root, r.RelativePath), GameFiles.Resolve(workspace, r.RelativePath), ct).ConfigureAwait(false);
        foreach (var output in outputs)
        {
            var from = GameFiles.Resolve(Files.Root, output.Key);
            if (await GameFiles.HasSha1Async(from, output.Value, null, ct).ConfigureAwait(false))
                await CopyAsync(from, GameFiles.Resolve(workspace, output.Key), ct).ConfigureAwait(false);
        }
        if (archive.Install["data"]?["MOJMAPS"]?["client"] is JsonValue mappings && parent.Metadata.Downloads.TryGetValue("client_mappings", out var mappingDownload))
        {
            var mappingPath = archive.Resolve(mappings.GetValue<string>());
            var mappingRequest = new DownloadRequest(mappingDownload.Url, mappingPath) { ExpectedHash = mappingDownload.Sha1, ExpectedSize = mappingDownload.Size };
            if (mappingRequest.ExpectedHash == null) throw LoaderOperations.Invalid();
            await Files.Downloads.DownloadAsync(mappingRequest, cancellationToken: ct).ConfigureAwait(false);
            await CopyAsync(GameFiles.Resolve(Files.Root, mappingPath), GameFiles.Resolve(workspace, mappingPath), ct).ConfigureAwait(false);
        }
        progress?.Report(new(LoaderInstallationStage.Processors, Message: "运行官方客户端安装器及其处理器"));
        var start = new ProcessStartInfo(runtime.ExecutablePath) { WorkingDirectory = workspace, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
        start.ArgumentList.Add("-Djava.awt.headless=true"); start.ArgumentList.Add("-jar"); start.ArgumentList.Add(downloaded.FullPath);
        start.ArgumentList.Add("--installClient"); start.ArgumentList.Add(workspace);
        start.ArgumentList.Add("--offline"); start.ArgumentList.Add("--mirror"); start.ArgumentList.Add(maven.AbsoluteUri);
        JavaProbe.ClearInheritedOptions(start);
        var exitCode = await RunInstallerAsync(transform?.Invoke(start) ?? start, options.InstallerTimeout, workspace, ct).ConfigureAwait(false);
        var expectedId = archive.Version["id"]!.GetValue<string>();
        var generatedProfile = GameFiles.Resolve(workspace, $"versions/{expectedId}/{expectedId}.json");
        if (!File.Exists(generatedProfile)) throw new LoaderException(LoaderErrorCode.MissingOutput, "安装器返回成功但没有生成版本配置。", exitCode);
        if (!FabricMetadata.Equivalent(JsonNode.Parse(await GameFiles.ReadJsonAsync(generatedProfile, ct).ConfigureAwait(false))!.AsObject(), archive.Version))
            throw new LoaderException(LoaderErrorCode.MissingOutput, "安装器生成的版本配置与官方模板不一致。", exitCode);
        progress?.Report(new(LoaderInstallationStage.Verification));
        foreach (var output in outputs)
            if (!await GameFiles.HasSha1Async(GameFiles.Resolve(workspace, output.Key), output.Value, null, ct).ConfigureAwait(false))
                throw new LoaderException(LoaderErrorCode.HashMismatch, "处理器产物缺失或 SHA-1 不匹配。", exitCode);
        foreach (var relative in requiredOutputs)
        {
            var file = GameFiles.Resolve(workspace, relative);
            if (!File.Exists(file) || new FileInfo(file).Length == 0) throw new LoaderException(LoaderErrorCode.MissingOutput, "客户端处理器未生成必要文件。", exitCode);
            if (relative.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
            {
                using var product = System.IO.Compression.ZipFile.OpenRead(file);
                InstallerArchive.ValidateEntries(product);
                if (!product.Entries.Any(e => e.Length > 0)) throw new LoaderException(LoaderErrorCode.MissingOutput, "处理器生成了空归档。", exitCode);
            }
        }
        foreach (var r in unique)
            if (!await GameFiles.HasSha1Async(GameFiles.Resolve(workspace, r.RelativePath), r.ExpectedHash!, r.ExpectedSize, ct).ConfigureAwait(false))
                throw new LoaderException(LoaderErrorCode.HashMismatch, "安装器依赖或内嵌产物校验失败。", exitCode);
        var generated = new JsonArray();
        var known = VersionParser.ParseObject(profile).Libraries.Where(l => l.Artifact != null).Select(l => "libraries/" + (l.Artifact!.Path ?? l.Coordinate.RelativePath)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var file in EnumerateFiles(workspace, "libraries").Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(workspace, file).Replace('\\', '/');
            if (!known.Contains(relative))
            {
                await using var input = File.OpenRead(file);
                var hash = Convert.ToHexString(await SHA1.HashDataAsync(input, ct).ConfigureAwait(false)).ToLowerInvariant();
                generated.Add(new JsonObject { ["path"] = relative, ["sha1"] = hash, ["size"] = input.Length });
            }
            await CopyAsync(file, GameFiles.Resolve(Files.Root, relative), ct).ConfigureAwait(false);
        }
        profile["fmcoreGeneratedFiles"] = generated; profile["fmcoreInstaller"] = receipt;
        var installed = await Files.CompleteAsync(parent, profile, installationOptions, progress, ct).ConfigureAwait(false);
        progress?.Report(new(LoaderInstallationStage.Completed, 1, 1));
        return new LoaderInstallationResult(name, game, selected.Version, id, installed, exitCode);
    }, ct);
    internal static async Task<int> RunInstallerAsync(ProcessStartInfo start, TimeSpan timeout, string workspace, CancellationToken ct)
    {
        JavaProcessOutput result;
        try { result = await JavaProcess.RunAsync(start, timeout, 8 * 1024 * 1024, ct).ConfigureAwait(false); }
        catch (JavaException e) when (e.Code == JavaErrorCode.ProbeTimeout)
        { throw new LoaderException(LoaderErrorCode.ProcessTimeout, "加载器安装器超时，进程已停止。"); }
        catch (JavaException) { throw new LoaderException(LoaderErrorCode.ProcessFailed, "加载器安装器无法执行或输出超出限制。"); }
        var filter = new SensitiveDataFilter();
        await File.WriteAllTextAsync(GameFiles.Resolve(workspace, "installer.filtered.log"), filter.Redact(result.StandardOutput + "\n" + result.StandardError), ct).ConfigureAwait(false);
        if (result.ExitCode != 0) throw new LoaderException(LoaderErrorCode.ProcessFailed, "官方安装器返回失败；脱敏日志保存在安装工作目录。", result.ExitCode);
        return result.ExitCode;
    }
    internal static async Task CopyAsync(string from, string to, CancellationToken ct)
    {
        DownloadPath.CheckLinks(from);
        await GameFiles.WriteAtomicAsync(to, async (output, token) =>
        { await using var input = new FileStream(from, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous); await input.CopyToAsync(output, token).ConfigureAwait(false); }, ct).ConfigureAwait(false);
    }
    internal static IEnumerable<string> EnumerateFiles(string root, string relative)
    {
        var pending = new Stack<string>(); pending.Push(GameFiles.Resolve(root, relative)); var count = 0;
        while (pending.TryPop(out var directory))
        {
            DownloadPath.CheckLinks(directory);
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                if (++count > 100000) throw new LoaderException(LoaderErrorCode.InvalidArchive, "安装器输出条目过多。");
                DownloadPath.CheckLinks(entry);
                if (Directory.Exists(entry)) pending.Push(entry); else yield return entry;
            }
        }
    }
}
