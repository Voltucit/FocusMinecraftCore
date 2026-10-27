using System.Text.Json.Nodes;
using FMCore.Downloads;
using FMCore.Launch;
using FMCore.Versions;

namespace FMCore.Installer;

// Request 为 null，表示缺少生成下载请求所需的信息。
internal sealed record GameDownloadFile(
    string Name,
    string FilePath,
    DownloadRequest? Request,
    IReadOnlyList<string> MissingFields)
{
    internal string? MavenUrl { get; init; }

    internal string? DeclaredSha1 { get; init; }

    internal long? DeclaredSize { get; init; }

    internal bool IsLegacyNativeArchive { get; init; }
}
internal static partial class GameFileDownloadPlanner
{
    internal static Task<IReadOnlyList<GameDownloadFile>> ReadAsync(
        string versionId,
        string minecraftRoot,
        string architecture,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(minecraftRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(architecture);

        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(
            () => Read(
                versionId,
                minecraftRoot,
                architecture,
                cancellationToken),
            cancellationToken);
    }

    private static IReadOnlyList<GameDownloadFile> Read(
        string versionId,
        string minecraftRoot,
        string architecture,
        CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "当前游戏文件清单只支持 Windows。");
        }

        string root =
            PathSafety.RequireDirectoryPath(minecraftRoot);

        MinecraftVersion version =
            new MinecraftVersionReader().GetVersion(
                versionId,
                root);

        List<GameDownloadFile> files = [];

        HashSet<string> targets =
            new(PathSafety.Comparer);

        void Add(GameDownloadFile file)
        {
            if (!targets.Add(file.FilePath))
            {
                throw new InvalidDataException(
                    $"游戏文件声明存在重复目标：{file.FilePath}");
            }

            files.Add(file);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var clientSource =
            LibraryResolver.GetClientJarSource(
                version,
                root);

        Add(CreateFile(
            "client",
            clientSource.FilePath,
            ReadClientDeclaration(clientSource.Owner),
            repositoryUrl: false));

        IReadOnlyList<JsonObject> libraries =
            ModernLibraryResolver.ReadLibrariesForArchitecture(
                version,
                root,
                architecture);

        foreach (JsonObject library in libraries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string name =
                MinecraftVersionReader.ReadRequiredString(
                    library,
                    "name",
                    "依赖缺少有效的 name。");

            string? path =
                LibraryResolver.GetLibraryPath(
                    library,
                    root);

            // 一个依赖可以同时声明普通 JAR 和原生库压缩包。
            if (path is not null)
            {
                JsonObject declaration;
                bool repositoryUrl;

                if (library["downloads"] is null)
                {
                    declaration = library;
                    repositoryUrl = true;
                }
                else
                {
                    if (library["downloads"] is not JsonObject downloads
                        || downloads["artifact"] is not JsonObject artifact)
                    {
                        throw new InvalidDataException(
                            $"依赖 {name} 缺少有效的 downloads.artifact。");
                    }

                    declaration = artifact;
                    repositoryUrl = false;
                }

                Add(CreateFile(
                    name,
                    path,
                    declaration,
                    repositoryUrl));
            }

            GameDownloadFile? native =
                CreateLegacyNativeFile(
                    library,
                    root,
                    architecture);

            if (native is not null)
                Add(native);
        }

        // 防止一个文件目标同时被另一个目标当作父目录。
        foreach (GameDownloadFile file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (string? parent =
                     Path.GetDirectoryName(file.FilePath);
                 parent is not null;
                 parent = Path.GetDirectoryName(parent))
            {
                if (targets.Contains(parent))
                {
                    throw new InvalidDataException(
                        $"游戏文件目标与父目录冲突：{parent}");
                }
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        return Array.AsReadOnly(files.ToArray());
    }

    private static JsonObject ReadClientDeclaration(
        MinecraftVersion owner)
    {
        JsonObject data =
            MinecraftVersionReader.ReadVersionJson(
                owner.Directory);

        if (data["downloads"] is null)
            return new JsonObject();

        if (data["downloads"] is not JsonObject downloads)
        {
            throw new InvalidDataException(
                $"版本 {owner.Id} 的 downloads 必须是对象。");
        }

        if (downloads["client"] is null)
            return new JsonObject();

        return downloads["client"] as JsonObject
            ?? throw new InvalidDataException(
                $"版本 {owner.Id} 的 downloads.client 必须是对象。");
    }

    private static GameDownloadFile CreateFile(
        string name,
        string path,
        JsonObject declaration,
        bool repositoryUrl)
    {
        if (path.Contains(';'))
        {
            throw new NotSupportedException(
                $"classpath 文件路径不能包含分号：{path}");
        }

        List<string> missing = [];

        string? url =
            ReadText(declaration, "url", missing);

        string? sha1 =
            ReadText(declaration, "sha1", missing);

        if (sha1 is not null
            && (sha1.Length != 40
                || !sha1.All(Uri.IsHexDigit)))
        {
            throw new InvalidDataException(
                $"文件 {name} 的 SHA-1 必须为 40 位十六进制字符串。");
        }

        long size = 0;

        if (declaration["size"] is null)
        {
            missing.Add("size");
        }
        else if (declaration["size"] is not JsonValue sizeValue
                 || !sizeValue.TryGetValue<long>(out size)
                 || size < 0)
        {
            throw new InvalidDataException(
                $"文件 {name} 的 size 必须是非负整数。");
        }

        if (url is not null)
        {
            url = ResolveUrl(
                url,
                name,
                repositoryUrl);
        }

        DownloadRequest? request = null;

        if (missing.Count == 0)
        {
            request = FileDownloader.NormalizeRequest(
                new DownloadRequest
                {
                    Url = url!,
                    FilePath = path,
                    Size = size,
                    Sha1 = sha1!
                });
        }

        return new GameDownloadFile(
            name,
            path,
            request,
            Array.AsReadOnly(missing.ToArray()))
        {
            MavenUrl = repositoryUrl ? url : null,
            DeclaredSha1 = sha1,
            DeclaredSize = missing.Contains("size") ? null : size
        };
    }

    private static string? ReadText(
        JsonObject declaration,
        string field,
        List<string> missing)
    {
        if (declaration[field] is null)
        {
            missing.Add(field);
            return null;
        }

        return MinecraftVersionReader.ReadRequiredString(
            declaration,
            field,
            $"下载声明的 {field} 必须是非空字符串。");
    }

    private static string ResolveUrl(
        string value,
        string name,
        bool repositoryUrl)
    {
        if (!Uri.TryCreate(
                value,
                UriKind.Absolute,
                out Uri? uri)
            || uri is null
            || uri.Scheme is not ("http" or "https"))
        {
            throw new InvalidDataException(
                $"文件 {name} 的下载地址必须是完整 HTTP(S) 地址。");
        }

        if (!repositoryUrl)
            return uri.AbsoluteUri;

        if (uri.Query.Length != 0
            || uri.Fragment.Length != 0)
        {
            throw new NotSupportedException(
                $"依赖 {name} 的 Maven 仓库地址不能包含查询或片段。");
        }

        string repository = uri.AbsoluteUri;

        if (!repository.EndsWith('/'))
            repository += "/";

        string relative =
            LibraryResolver.MavenToPath(name)
                .Replace('\\', '/');

        return new Uri(
            new Uri(repository),
            relative).AbsoluteUri;
    }
}
