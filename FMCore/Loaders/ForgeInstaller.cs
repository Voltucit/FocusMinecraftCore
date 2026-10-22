using System.Xml;
using System.Xml.Linq;
using FMCore.Downloader;
using FMCore.Java;
using FMCore.Versions;

namespace FMCore.Loaders;

/// <summary>Queries official Forge releases and runs the verified official client installer, including all client processors.</summary>
public sealed class ForgeInstaller
{
    /// <summary>Official Maven repository.</summary>
    public static Uri OfficialMavenUri { get; } = new("https://maven.minecraftforge.net/");
    private readonly InstallerLoader installer;
    /// <summary>Absolute shared game root.</summary>
    public string RootDirectory => installer.Files.Root;
    /// <summary>Creates a ready-to-use Forge installer.</summary>
    public ForgeInstaller(string rootDirectory = ".minecraft") : this(new DownloadManager(rootDirectory)) { }
    /// <summary>Uses caller-configured downloads, optional same-root versions and an HTTPS Maven mirror.</summary>
    public ForgeInstaller(DownloadManager downloads, VersionManager? versions = null, Uri? mavenUri = null)
        : this(downloads, versions, mavenUri, null, null) { }
    internal ForgeInstaller(DownloadManager downloads, VersionManager? versions, Uri? mavenUri, JavaManager? java,
        Func<System.Diagnostics.ProcessStartInfo, System.Diagnostics.ProcessStartInfo>? transform)
    { installer = new("Forge", downloads, versions, mavenUri ?? OfficialMavenUri, "net/minecraftforge/forge", java, transform); }
    /// <summary>Gets Maven releases for a Minecraft version, newest first. Querying an old format does not imply install support.</summary>
    public Task<IReadOnlyList<MavenLoaderVersion>> GetVersionsAsync(string minecraftVersion,
        VersionCachePolicy cachePolicy = VersionCachePolicy.Refresh, CancellationToken cancellationToken = default) =>
        installer.GetVersionsAsync(minecraftVersion, cachePolicy, cancellationToken);
    /// <summary>Installs an explicit Forge version, or the newest stable Maven release for the game. Verifies exit code, profile and processor products.</summary>
    public Task<LoaderInstallationResult> InstallAsync(string minecraftVersion, string? forgeVersion = null,
        LoaderInstallOptions? options = null, IProgress<LoaderInstallationProgress>? progress = null,
        CancellationToken cancellationToken = default) => installer.InstallAsync(minecraftVersion, forgeVersion, options, progress, cancellationToken);
}

internal static class LoaderMaven
{
    internal static string[] Versions(string xml)
    {
        using var reader = XmlReader.Create(new StringReader(xml), new() { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 16 * 1024 * 1024 });
        var document = XDocument.Load(reader);
        return document.Element("metadata")?.Element("versioning")?.Element("versions")?.Elements("version")
            .Select(v => VersionParser.Id(v.Value)).Distinct(StringComparer.Ordinal).ToArray() ?? throw LoaderOperations.Invalid();
    }
    internal static string SortKey(string version) => string.Join('.', version.Split('-', '+')[0].Split('.').Select(p =>
        int.TryParse(p, out var n) ? n.ToString("D10", System.Globalization.CultureInfo.InvariantCulture) : p));
}
