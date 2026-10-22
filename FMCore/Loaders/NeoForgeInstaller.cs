using FMCore.Downloader;
using FMCore.Versions;

namespace FMCore.Loaders;

/// <summary>Queries NeoForge Maven and executes its official client installer with the shared verified installation pipeline.</summary>
public sealed class NeoForgeInstaller
{
    /// <summary>Official NeoForged releases repository.</summary>
    public static Uri OfficialMavenUri { get; } = new("https://maven.neoforged.net/releases/");
    private readonly InstallerLoader installer;
    /// <summary>Absolute shared game root.</summary>
    public string RootDirectory => installer.Files.Root;
    /// <summary>Creates a ready-to-use NeoForge installer.</summary>
    public NeoForgeInstaller(string rootDirectory = ".minecraft") : this(new DownloadManager(rootDirectory)) { }
    /// <summary>Reuses caller-owned networking and optional same-root versions and HTTPS Maven mirror.</summary>
    public NeoForgeInstaller(DownloadManager downloads, VersionManager? versions = null, Uri? mavenUri = null)
        : this(downloads, versions, mavenUri, null, null) { }
    internal NeoForgeInstaller(DownloadManager downloads, VersionManager? versions, Uri? mavenUri, FMCore.Java.JavaManager? java,
        Func<System.Diagnostics.ProcessStartInfo, System.Diagnostics.ProcessStartInfo>? transform)
    { installer = new("NeoForge", downloads, versions, mavenUri ?? OfficialMavenUri, "net/neoforged/neoforge", java, transform); }
    /// <summary>Gets compatible Maven releases (Minecraft 1.20.2–1.21.x naming); historical renamed or year-based schemes are explicitly unsupported.</summary>
    public async Task<IReadOnlyList<MavenLoaderVersion>> GetVersionsAsync(string minecraftVersion,
        VersionCachePolicy cachePolicy = VersionCachePolicy.Refresh, CancellationToken cancellationToken = default)
    { Validate(minecraftVersion); return await installer.GetVersionsAsync(minecraftVersion, cachePolicy, cancellationToken).ConfigureAwait(false); }
    /// <summary>Installs an explicit NeoForge version or the newest stable version, checking actual installer exit and generated artifacts.</summary>
    public async Task<LoaderInstallationResult> InstallAsync(string minecraftVersion, string? neoForgeVersion = null,
        LoaderInstallOptions? options = null, IProgress<LoaderInstallationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    { Validate(minecraftVersion); return await installer.InstallAsync(minecraftVersion, neoForgeVersion, options, progress, cancellationToken).ConfigureAwait(false); }
    private static void Validate(string game)
    {
        VersionParser.Id(game);
        if (!Version.TryParse(game, out var v) || v.Major != 1 || v.Minor is < 20 or > 21 || v.Minor == 20 && v.Build < 2)
            throw new LoaderException(LoaderErrorCode.UnsupportedFormat, "当前 NeoForge 支持 Minecraft 1.20.2–1.21.x 的 Maven 命名；不猜测 1.20.1 重命名或年号格式。");
    }
}
