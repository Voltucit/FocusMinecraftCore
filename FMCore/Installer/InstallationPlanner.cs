using FMCore.Downloader;
using FMCore.Utilities;
using FMCore.Versions;

namespace FMCore.Installer;

internal static class InstallationPlanner
{
    internal static InstallationPlan Create(ResolvedVersion version, InstallationOptions options, string root)
    {
        var metadata = version.Metadata;
        var context = options.Context;
        ArgumentNullException.ThrowIfNull(context);
        var arch = context.Architecture.ToLowerInvariant() switch
        {
            "x86" or "i386" or "i486" or "i586" or "i686" => "x86",
            "amd64" or "x86_64" or "x64" => "x64",
            "aarch64" or "arm64" => "arm64",
            "arm" or "arm32" or "armv7l" => "arm32",
            _ => throw new InstallationException(InstallationErrorCode.UnsupportedPlatform, "无法识别目标 Java 架构。")
        };
        if (context.OperatingSystem is not ("windows" or "linux" or "osx"))
            throw new InstallationException(InstallationErrorCode.UnsupportedPlatform, "无法识别目标操作系统。");
        context = context with { Architecture = arch switch { "x64" => "amd64", "arm64" => "aarch64", "arm32" => "arm", _ => "x86" } };
        if (options.GameDirectory.Length != 0) GameFiles.Resolve(root, options.GameDirectory);
        var natives = options.NativeDirectory ?? $"versions/{VersionParser.Id(metadata.Id)}/natives/{context.OperatingSystem}-{arch}";
        GameFiles.Resolve(root, natives);
        if (version.ClientDownload == null) throw new InstallationException(InstallationErrorCode.MissingClient, "版本未提供客户端文件下载信息。");
        var client = Request(version.ClientDownload, $"versions/{VersionParser.Id(version.ClientJarId)}/{version.ClientJarId}.jar");
        var assetIndex = metadata.AssetIndex ?? throw new InstallationException(InstallationErrorCode.MissingAssetIndex, "版本未提供可用资源索引；不支持已停用的旧资源服务。");
        GameFiles.Resolve(root, $"assets/indexes/{VersionParser.Id(assetIndex.Id)}.json");
        var files = new Dictionary<string, DownloadRequest>(GameFiles.PathComparer);
        var classpath = new List<string>();
        var archives = new Dictionary<string, NativeArchive>(GameFiles.PathComparer);
        foreach (var library in metadata.GetLibraries(context))
        {
            // A library can need both its classpath JAR and a native archive.
            if (library.Artifact is DownloadArtifact artifact)
            {
                var request = Request(artifact, "libraries/" + (artifact.Path ?? library.Coordinate.RelativePath));
                Add(request);
                if (!classpath.Contains(request.RelativePath, GameFiles.PathComparer)) classpath.Add(request.RelativePath);
            }
            var classifier = library.GetNativeClassifier(context);
            if (classifier == null) continue;
            if (!library.Classifiers.TryGetValue(classifier, out var native))
                throw new InstallationException(InstallationErrorCode.MissingNativeClassifier, "目标平台所需的原生依赖描述缺失。");
            var path = ("libraries/" + (native.Path ?? (library.Coordinate with { Classifier = classifier }).RelativePath)).Replace('\\', '/');
            var nativeRequest = Request(native, path);
            Add(nativeRequest);
            var checkedRequest = files[path];
            var archive = new NativeArchive(path, checkedRequest.ExpectedHash, checkedRequest.ExpectedSize,
                library.ExtractExcludes.Select(p => p.Replace('\\', '/')).Distinct(StringComparer.Ordinal).ToArray());
            if (archives.TryGetValue(path, out var previous))
                // A shared archive must retain every file required by either applicable declaration.
                archive = archive with
                {
                    Excludes = previous.Excludes.SelectMany(a => archive.Excludes.Select(b =>
                    a.StartsWith(b, StringComparison.Ordinal) ? a : b.StartsWith(a, StringComparison.Ordinal) ? b : null))
                    .OfType<string>().Distinct(StringComparer.Ordinal).ToArray()
                };
            archives[path] = archive;
        }
        foreach (var file in metadata.GeneratedFiles)
            Add(new(new Uri("about:blank"), file.RelativePath) { ExpectedHash = file.Sha1, ExpectedSize = file.Size, LocalOnly = true });
        var logging = metadata.Logging is LoggingConfiguration log
            ? Request(log.File, $"assets/log_configs/{VersionParser.Id(log.FileId)}") : null;
        GameFiles.Resolve(root, client.RelativePath);
        if (logging != null) GameFiles.Resolve(root, logging.RelativePath);
        classpath.Add(client.RelativePath);
        return new(version, context, options.GameDirectory, natives, client, Array.AsReadOnly(files.Values.ToArray()),
            classpath.AsReadOnly(), Array.AsReadOnly(archives.Values.ToArray()), assetIndex, logging);

        void Add(DownloadRequest request)
        {
            GameFiles.Resolve(root, request.RelativePath);
            if (files.TryGetValue(request.RelativePath, out var existing))
            {
                if (existing.ExpectedSize is long a && request.ExpectedSize is long b && a != b ||
                    existing.ExpectedHash != null && request.ExpectedHash != null && !existing.ExpectedHash.Equals(request.ExpectedHash, StringComparison.OrdinalIgnoreCase) ||
                    existing.Source != request.Source && (existing.ExpectedHash == null || request.ExpectedHash == null))
                    throw new InstallationException(InstallationErrorCode.ConflictingFiles, "多个依赖声明了冲突的目标文件。");
                files[request.RelativePath] = existing with
                { ExpectedHash = existing.ExpectedHash ?? request.ExpectedHash, ExpectedSize = existing.ExpectedSize ?? request.ExpectedSize };
            }
            else files.Add(request.RelativePath, request);
        }
    }

    private static DownloadRequest Request(DownloadArtifact artifact, string path) => new(artifact.Url, path.Replace('\\', '/'))
    { ExpectedHash = artifact.Sha1, ExpectedSize = artifact.Size, LocalOnly = artifact.IsLocalOnly };
}
