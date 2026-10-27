namespace FMCore.Downloads;

public enum DownloadSource
{
    Official,
    BmclApi
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
        string? mirrorUrl =
            source == DownloadSource.BmclApi
                ? GetMirrorUrl(request.Url)
                : null;

        return new DownloadRequest
        {
            Url = mirrorUrl ?? request.Url,

            FallbackUrl = mirrorUrl is null
                ? request.FallbackUrl
                : request.Url,

            FilePath = request.FilePath,
            Size = request.Size,
            Sha1 = request.Sha1,
            Timeout = request.Timeout,
            MaxRetries = request.MaxRetries,
            RetryDelay = request.RetryDelay,
            ReplaceInvalidFiles = replaceInvalidFiles
        };
    }

    private static string? GetMirrorUrl(string originalUrl)
    {
        if (!Uri.TryCreate(
                originalUrl,
                UriKind.Absolute,
                out Uri? url)
            || url is null
            || url.Scheme is not ("http" or "https")
            || !url.IsDefaultPort
            || url.UserInfo.Length != 0
            || url.Query.Length != 0
            || url.Fragment.Length != 0)
        {
            return null;
        }

        string? prefix =
            url.Host.ToLowerInvariant() switch
            {
                "resources.download.minecraft.net"
                    => "/assets",

                "libraries.minecraft.net"
                    or "maven.fabricmc.net"
                    => "/maven",

                "piston-data.mojang.com"
                    or "launcher.mojang.com"
                    or "launchermeta.mojang.com"
                    when url.AbsolutePath.EndsWith(
                        "/client.jar",
                        StringComparison.OrdinalIgnoreCase)
                    => "",

                _ => null
            };

        return prefix is null
            ? null
            : MirrorRoot + prefix + url.AbsolutePath;
    }
}
