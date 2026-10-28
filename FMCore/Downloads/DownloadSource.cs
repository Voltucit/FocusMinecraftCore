namespace FMCore.Downloads;

public sealed class DownloadSource
{
    private readonly Func<string, string?> _resolveUrl;

    public static DownloadSource Official { get; } =
        new("官方", _ => null);

    public static DownloadSource BmclApi { get; } =
        new("BMCLAPI（bangbang93）", DownloadSourcePolicy.GetBmclUrl);

    public string Name { get; }

    public DownloadSource(
        string name,
        Func<string, string?> resolveUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(resolveUrl);

        Name = name;
        _resolveUrl = resolveUrl;
    }

    public string GetUrl(string originalUrl)
    {
        Uri original = RequireHttpUrl(originalUrl);
        string? mapped = _resolveUrl(original.AbsoluteUri);

        return mapped is null
            ? original.AbsoluteUri
            : RequireHttpUrl(mapped).AbsoluteUri;
    }

    public override string ToString() => Name;

    private static Uri RequireHttpUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? url)
            || url.Scheme is not ("http" or "https")
            || url.UserInfo.Length != 0
            || url.Fragment.Length != 0)
        {
            throw new ArgumentException(
                "下载地址必须是有效的 HTTP(S) 绝对地址，不能包含用户信息或片段。",
                nameof(value));
        }

        return url;
    }
}

internal static class DownloadSourcePolicy
{
    private const string MirrorRoot =
        "https://bmclapi2.bangbang93.com";

    internal static DownloadRequest Apply(
        DownloadRequest request,
        DownloadSource source,
        bool replaceInvalidFiles)
    {
        ArgumentNullException.ThrowIfNull(source);

        string selectedUrl = source.GetUrl(request.Url);
        bool changed = !string.Equals(
            selectedUrl, request.Url, StringComparison.Ordinal);

        return new DownloadRequest
        {
            Url = selectedUrl,
            FallbackUrl = changed ? request.Url : request.FallbackUrl,
            FilePath = request.FilePath,
            Size = request.Size,
            Sha1 = request.Sha1,
            Timeout = request.Timeout,
            MaxRetries = request.MaxRetries,
            RetryDelay = request.RetryDelay,
            ReplaceInvalidFiles = replaceInvalidFiles
        };
    }

    internal static string? GetBmclUrl(string originalUrl)
    {
        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out Uri? url)
            || url.Scheme is not ("http" or "https")
            || !url.IsDefaultPort
            || url.UserInfo.Length != 0
            || url.Query.Length != 0
            || url.Fragment.Length != 0)
        {
            return null;
        }

        string path = url.AbsolutePath;

        string? mappedPath = url.Host.ToLowerInvariant() switch
        {
            "resources.download.minecraft.net" => "/assets" + path,

            "libraries.minecraft.net"
                or "maven.fabricmc.net"
                or "maven.minecraftforge.net" => "/maven" + path,

            "files.minecraftforge.net"
                when path.StartsWith("/maven/", StringComparison.Ordinal)
                => path,

            "maven.neoforged.net"
                when path.StartsWith("/releases/", StringComparison.Ordinal)
                => "/maven" + path["/releases".Length..],

            "meta.fabricmc.net" => "/fabric-meta" + path,

            "piston-data.mojang.com"
                or "launcher.mojang.com"
                or "launchermeta.mojang.com"
                when path.EndsWith("/client.jar", StringComparison.OrdinalIgnoreCase)
                => path,

            _ => null
        };

        return mappedPath is null ? null : MirrorRoot + mappedPath;
    }
}

