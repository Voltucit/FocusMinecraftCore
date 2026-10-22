using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FMCore.Downloader;
using FMCore.Installer;
using FMCore.Java;
using FMCore.Utilities;
using FMCore.Versions;

namespace FMCore.Loaders;

/// <summary>An official OptiFine catalog entry. DownloadPage is the public official mirror page, not a transient signed file URL.</summary>
public sealed record OptiFineVersion(string MinecraftVersion, string Version, string FileName, bool IsPreview, Uri DownloadPage);

/// <summary>Queries the official website and installs standalone OptiFine by invoking its original installer and patcher.</summary>
public sealed partial class OptiFineInstaller
{
    /// <summary>Official site; no third-party mirrors or redistributed JARs are bundled.</summary>
    public static Uri OfficialSiteUri { get; } = new("https://optifine.net/");
    private readonly LoaderOperations files;
    private readonly Uri site;
    private readonly JavaManager? java;
    private readonly Func<ProcessStartInfo, ProcessStartInfo>? transform;
    private readonly SemaphoreSlim gate = new(1, 1);
    /// <summary>Absolute shared game root.</summary>
    public string RootDirectory => files.Root;
    /// <summary>Creates a ready-to-use standalone OptiFine installer.</summary>
    public OptiFineInstaller(string rootDirectory = ".minecraft") : this(new DownloadManager(rootDirectory)) { }
    /// <summary>Reuses caller-owned downloads and optional same-root version manager; requests use the official website.</summary>
    public OptiFineInstaller(DownloadManager downloads, VersionManager? versions = null) : this(downloads, versions, OfficialSiteUri, null, null) { }
    internal OptiFineInstaller(DownloadManager downloads, VersionManager? versions, Uri site, JavaManager? java,
        Func<ProcessStartInfo, ProcessStartInfo>? transform)
    {
        files = new(downloads, versions, "optifine", site); this.site = site; this.java = java; this.transform = transform;
    }
    /// <summary>Queries official releases/previews for a game, preserving website order. CacheOnly uses only cached catalog HTML.</summary>
    public Task<IReadOnlyList<OptiFineVersion>> GetVersionsAsync(string minecraftVersion,
        VersionCachePolicy cachePolicy = VersionCachePolicy.Refresh, CancellationToken cancellationToken = default) => LoaderOperations.RunAsync(gate, () =>
    {
        VersionParser.Id(minecraftVersion);
        return files.ReadAsync(new(site, "downloads"), cachePolicy, s => Catalog(s, minecraftVersion), cancellationToken);
    }, cancellationToken);
    /// <summary>Downloads from the official page then installs; null selects the first non-preview website entry. A gated page fails explicitly and InstallLocalAsync remains available.</summary>
    public Task<LoaderInstallationResult> InstallAsync(string minecraftVersion, string? optiFineVersion = null,
        LoaderInstallOptions? options = null, IProgress<LoaderInstallationProgress>? progress = null,
        CancellationToken cancellationToken = default) => LoaderOperations.RunAsync(gate, async () =>
    {
        options ??= new(); ArgumentNullException.ThrowIfNull(options.Installation); VersionParser.Id(minecraftVersion);
        using var lease = files.AcquireLease();
        var policy = options.Installation.CachePolicy;
        var versions = await files.ReadAsync(new(site, "downloads"), policy, s => Catalog(s, minecraftVersion), cancellationToken).ConfigureAwait(false);
        var selected = optiFineVersion == null ? versions.FirstOrDefault(v => !v.IsPreview)
            : versions.FirstOrDefault(v => v.Version == optiFineVersion || v.FileName == optiFineVersion);
        if (selected == null) throw new LoaderException(LoaderErrorCode.UnsupportedCombination, "官网没有请求的 OptiFine 版本或没有可自动选择的正式版。");
        var relative = ".cache/loaders/installers/optifine/" + selected.FileName;
        var destination = GameFiles.Resolve(files.Root, relative);
        var stamp = destination + ".sha256";
        var cached = false;
        if (File.Exists(destination) && File.Exists(stamp))
        {
            var expected = (await GameFiles.ReadJsonAsync(stamp, cancellationToken).ConfigureAwait(false)).Trim();
            if (expected.Length == 64 && expected.All(char.IsAsciiHexDigit))
            {
                await using var input = File.OpenRead(destination);
                cached = Convert.ToHexString(await SHA256.HashDataAsync(input, cancellationToken).ConfigureAwait(false)).Equals(expected, StringComparison.OrdinalIgnoreCase);
            }
        }
        if (!cached)
        {
            // The signed link is short lived: never reuse a cached download page token.
            var direct = await files.ReadAsync(selected.DownloadPage, VersionCachePolicy.Refresh, ParseDownloadLink, cancellationToken).ConfigureAwait(false);
            var request = new DownloadRequest(direct, relative);
            await files.Downloads.DownloadAsync(request, new LoaderOperations.InlineProgress<DownloadProgress>(p =>
                progress?.Report(new(LoaderInstallationStage.InstallerDownload, Download: p))), cancellationToken).ConfigureAwait(false);
            ValidateInstaller(destination);
            await using var input = File.OpenRead(destination);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(input, cancellationToken).ConfigureAwait(false));
            await GameFiles.WriteAtomicAsync(stamp, async (s, ct) => await s.WriteAsync(Encoding.ASCII.GetBytes(hash), ct).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
        }
        return await InstallCoreAsync(destination, selected, options, progress, cancellationToken).ConfigureAwait(false);
    }, cancellationToken);
    /// <summary>Installs an unmodified, caller-obtained official JAR; filename and the installer's own version must agree. Does not contact the OptiFine website.</summary>
    public Task<LoaderInstallationResult> InstallLocalAsync(string installerPath, LoaderInstallOptions? options = null,
        IProgress<LoaderInstallationProgress>? progress = null, CancellationToken cancellationToken = default) => LoaderOperations.RunAsync(gate, async () =>
    {
        options ??= new(); ArgumentNullException.ThrowIfNull(options.Installation);
        var path = Path.GetFullPath(installerPath); DownloadPath.CheckLinks(path);
        var entry = ParseFile(Path.GetFileName(path)) ?? throw LoaderOperations.Unsupported();
        using var lease = files.AcquireLease();
        return await InstallCoreAsync(path, entry, options, progress, cancellationToken).ConfigureAwait(false);
    }, cancellationToken);
    private async Task<LoaderInstallationResult> InstallCoreAsync(string jar, OptiFineVersion selected, LoaderInstallOptions options,
        IProgress<LoaderInstallationProgress>? progress, CancellationToken ct)
    {
        if (options.InstallerTimeout <= TimeSpan.Zero || options.InstallerTimeout > TimeSpan.FromHours(1)) throw new ArgumentOutOfRangeException(nameof(options));
        ValidateInstaller(jar);
        string installerHash;
        await using (var input = File.OpenRead(jar)) installerHash = Convert.ToHexString(await SHA256.HashDataAsync(input, ct).ConfigureAwait(false)).ToLowerInvariant();
        var receipt = new JsonObject { ["name"] = "OptiFine", ["version"] = selected.Version, ["installerSha256"] = installerHash, ["format"] = 1 };
        var id = VersionParser.Id(options.VersionId ?? selected.MinecraftVersion + "-OptiFine_" + selected.Version);
        var game = selected.MinecraftVersion;
        if (id.Equals(game, StringComparison.OrdinalIgnoreCase)) throw new LoaderException(LoaderErrorCode.ProfileConflict, "OptiFine 不能覆盖原版 ID。");
        progress?.Report(new(LoaderInstallationStage.Metadata));
        var parent = await files.Versions.ResolveAsync(game, options.Installation.CachePolicy, ct).ConfigureAwait(false);
        var path = files.Versions.GetMetadataPath(id);
        if (File.Exists(path))
        {
            var existing = VersionParser.Parse(await GameFiles.ReadJsonAsync(path, ct).ConfigureAwait(false));
            if (existing.InheritsFrom != game || !JsonNode.DeepEquals(existing.ToJson()["fmcoreInstaller"], receipt))
                throw new LoaderException(LoaderErrorCode.ProfileConflict, "目标已有不同 OptiFine 安装器或配置。");
            var checkedFiles = await new GameInstaller(files.Downloads, files.Versions).VerifyAsync(id, options.Installation with { CachePolicy = VersionCachePolicy.CacheOnly }, cancellationToken: ct).ConfigureAwait(false);
            if (checkedFiles.IsValid)
            {
                var reused = await files.CompleteAsync(parent, existing.ToJson(), options.Installation, progress, ct).ConfigureAwait(false);
                progress?.Report(new(LoaderInstallationStage.Completed, 1, 1));
                return new("OptiFine", game, selected.Version, id, reused);
            }
        }
        var runtime = await (java ?? new JavaManager(options.JavaDetection)).SelectAsync(parent.Metadata, options.JavaPath, cancellationToken: ct).ConfigureAwait(false);
        var installOptions = options.Installation with { Context = runtime.GetRuleContext(options.Installation.Context.Features) };
        await new GameInstaller(files.Downloads, files.Versions).InstallAsync(game, installOptions,
            new LoaderOperations.InlineProgress<InstallationProgress>(p => progress?.Report(new(LoaderInstallationStage.Dependencies, p.CompletedFiles, p.TotalFiles, p))), ct).ConfigureAwait(false);
        var work = GameFiles.Resolve(files.Root, ".cache/loaders/work/optifine/" + installerHash);
        GameFiles.CreateDirectory(work);
        await InstallerLoader.CopyAsync(files.Versions.GetMetadataPath(game), GameFiles.Resolve(work, $"versions/{game}/{game}.json"), ct).ConfigureAwait(false);
        await InstallerLoader.CopyAsync(GameFiles.Resolve(files.Root, $"versions/{parent.ClientJarId}/{parent.ClientJarId}.jar"), GameFiles.Resolve(work, $"versions/{game}/{game}.jar"), ct).ConfigureAwait(false);
        await File.WriteAllTextAsync(GameFiles.Resolve(work, "launcher_profiles.json"), "{\"profiles\":{}}", ct).ConfigureAwait(false);
        var helper = GameFiles.Resolve(work, "FmcoreOptifineBridge.class");
        await GameFiles.WriteAtomicAsync(helper, async (s, token) => await s.WriteAsync(OptiFineBridge.Bytes, token).ConfigureAwait(false), ct).ConfigureAwait(false);
        var start = new ProcessStartInfo(runtime.ExecutablePath) { WorkingDirectory = work, StandardOutputEncoding = Encoding.UTF8, StandardErrorEncoding = Encoding.UTF8 };
        foreach (var arg in new[] { "-Djava.awt.headless=true", "-cp", work + Path.PathSeparator + jar, "FmcoreOptifineBridge", work,
            Path.GetFileNameWithoutExtension(selected.FileName).Replace("preview_", "", StringComparison.Ordinal) }) start.ArgumentList.Add(arg);
        JavaProbe.ClearInheritedOptions(start);
        progress?.Report(new(LoaderInstallationStage.Processors, Message: "运行原始 OptiFine 安装器及补丁器"));
        int exit;
        try { exit = await InstallerLoader.RunInstallerAsync(transform?.Invoke(start) ?? start, options.InstallerTimeout, work, ct).ConfigureAwait(false); }
        catch (LoaderException e) when (e.ExitCode == 42) { throw LoaderOperations.Unsupported(); }
        catch (LoaderException e) when (e.ExitCode == 43) { throw new LoaderException(LoaderErrorCode.UnsupportedCombination, "安装器自身版本与文件名不匹配。", 43); }
        var vendorId = game + "-OptiFine_" + selected.Version;
        var vendorProfile = GameFiles.Resolve(work, $"versions/{vendorId}/{vendorId}.json");
        if (!File.Exists(vendorProfile)) throw new LoaderException(LoaderErrorCode.MissingOutput, "OptiFine 安装器未生成预期版本配置。", exit);
        var metadata = VersionParser.Parse(await GameFiles.ReadJsonAsync(vendorProfile, ct).ConfigureAwait(false));
        if (metadata.Id != vendorId || metadata.InheritsFrom != game || metadata.Jar != null && metadata.Jar != game ||
            metadata.MainClass != "net.minecraft.launchwrapper.Launch" || metadata.Downloads.Count != 0 ||
            !metadata.Arguments.Game.SelectMany(a => a.Values).Contains("optifine.OptiFineTweaker") ||
            !metadata.Libraries.Any(l => l.Coordinate.Group == "optifine" && l.Coordinate.Artifact == "OptiFine" && l.Coordinate.Version == game + "_" + selected.Version))
            throw LoaderOperations.Unsupported();
        var profile = metadata.ToJson(); profile["id"] = id;
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var nodes = profile["libraries"]!.AsArray();
        progress?.Report(new(LoaderInstallationStage.Verification));
        for (var i = 0; i < metadata.Libraries.Count; i++)
        {
            var library = metadata.Libraries[i]; var relative = "libraries/" + library.Coordinate.RelativePath;
            var produced = GameFiles.Resolve(work, relative);
            if (!File.Exists(produced) || new FileInfo(produced).Length == 0) throw new LoaderException(LoaderErrorCode.MissingOutput, "OptiFine 安装器没有生成必要的依赖。", exit);
            using (var archive = ZipFile.OpenRead(produced))
            {
                InstallerArchive.ValidateEntries(archive);
                if (!archive.Entries.Any(e => e.Length > 0)) throw new LoaderException(LoaderErrorCode.MissingOutput, "OptiFine 生成的依赖为空。", exit);
            }
            await using var input = File.OpenRead(produced);
            var hash = Convert.ToHexString(await SHA1.HashDataAsync(input, ct).ConfigureAwait(false)).ToLowerInvariant();
            nodes[i]!["downloads"] = new JsonObject { ["artifact"] = new JsonObject { ["url"] = "", ["path"] = library.Coordinate.RelativePath, ["sha1"] = hash, ["size"] = input.Length } };
            await InstallerLoader.CopyAsync(produced, GameFiles.Resolve(files.Root, relative), ct).ConfigureAwait(false); known.Add(relative);
        }
        var extra = new JsonArray();
        foreach (var produced in InstallerLoader.EnumerateFiles(work, "libraries").Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(work, produced).Replace('\\', '/');
            if (known.Contains(relative)) continue;
            await using var input = File.OpenRead(produced); var hash = Convert.ToHexString(await SHA1.HashDataAsync(input, ct).ConfigureAwait(false)).ToLowerInvariant();
            extra.Add(new JsonObject { ["path"] = relative, ["sha1"] = hash, ["size"] = input.Length });
            await InstallerLoader.CopyAsync(produced, GameFiles.Resolve(files.Root, relative), ct).ConfigureAwait(false);
        }
        profile["fmcoreGeneratedFiles"] = extra; profile["fmcoreInstaller"] = receipt;
        var installed = await files.CompleteAsync(parent, profile, installOptions, progress, ct).ConfigureAwait(false);
        progress?.Report(new(LoaderInstallationStage.Completed, 1, 1));
        return new("OptiFine", game, selected.Version, id, installed, exit);
    }
    private static void ValidateInstaller(string path)
    {
        DownloadPath.CheckLinks(path);
        if (!File.Exists(path)) throw new LoaderException(LoaderErrorCode.DownloadUnavailable, "OptiFine 安装器文件不存在。");
        try
        {
            using var zip = ZipFile.OpenRead(path); InstallerArchive.ValidateEntries(zip);
            if (zip.GetEntry("optifine/Installer.class") == null || zip.GetEntry("optifine/Patcher.class") == null) throw LoaderOperations.Unsupported();
        }
        catch (InvalidDataException) { throw new LoaderException(LoaderErrorCode.InvalidArchive, "OptiFine 下载内容不是有效安装器 JAR；可从官网取得后使用 InstallLocalAsync。"); }
    }
    internal IReadOnlyList<OptiFineVersion> Catalog(string html, string game)
    {
        var all = CatalogLinks().Matches(html).Select(m => WebUtility.HtmlDecode(Uri.UnescapeDataString(m.Groups[1].Value)))
            .Distinct(StringComparer.Ordinal).Select(ParseFile).OfType<OptiFineVersion>().ToArray();
        if (all.Length == 0) throw new LoaderException(LoaderErrorCode.UnsupportedFormat, "OptiFine 官网目录格式已变化，未返回伪造版本列表。");
        return all.Where(v => v.MinecraftVersion == game).ToArray();
    }
    private OptiFineVersion? ParseFile(string filename)
    {
        var match = Filename().Match(filename); if (!match.Success) return null;
        VersionParser.Id(filename); var game = VersionParser.Id(match.Groups[2].Value); var version = "HD_U_" + match.Groups[3].Value;
        return new(game, version, filename, match.Groups[1].Success || version.Contains("pre", StringComparison.OrdinalIgnoreCase), new(site, "adloadx?f=" + Uri.EscapeDataString(filename)));
    }
    private Uri ParseDownloadLink(string html)
    {
        var urls = DownloadLinks().Matches(html).Select(m => new Uri(site, WebUtility.HtmlDecode(m.Groups[1].Value)))
            .Where(u => u.Scheme == "https" && u.Host == site.Host && u.AbsolutePath == new Uri(site, "downloadx").AbsolutePath).Distinct().ToArray();
        if (urls.Length != 1) throw new LoaderException(LoaderErrorCode.DownloadUnavailable, "官网未提供可用下载链接；请手动从官网取得 JAR 后使用 InstallLocalAsync。");
        return urls[0];
    }
    [GeneratedRegex(@"(?:adloadx|adload)\?f=([^""'<>&\s]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex CatalogLinks();
    [GeneratedRegex(@"^(preview_)?OptiFine_([0-9][A-Za-z0-9.\-]*)_HD_U_([A-Za-z0-9_]+)\.jar$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex Filename();
    [GeneratedRegex(@"href\s*=\s*[""']([^""'<>]*downloadx\?[^""'<>]+)[""']", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 1000)]
    private static partial Regex DownloadLinks();
}
