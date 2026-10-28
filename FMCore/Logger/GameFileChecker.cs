using System.Runtime.InteropServices;
using System.Security.Cryptography;
using FMCore.Downloads;
using FMCore.Installer;

namespace FMCore.Logger;

public sealed class GameFileChecker
{
    private readonly string _versionId;
    private readonly string _root;
    private readonly string _architecture;

    public GameFileChecker(
        string versionId,
        string minecraftRoot,
        Architecture? targetArchitecture = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(minecraftRoot);

        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("当前文件检查只支持 Windows。");

        _versionId = versionId;
        _root = PathSafety.RequireDirectoryPath(minecraftRoot);
        _architecture = (targetArchitecture ?? RuntimeInformation.OSArchitecture) switch
        {
            Architecture.X86 => "x86",
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => throw new NotSupportedException("只支持 x86、x64 或 arm64 目标架构。")
        };
    }

    public Task<GameFileCheckResult> CheckAsync(
        Action<GameFileCheckProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.Run(
            () => CheckCoreAsync(onProgress, cancellationToken),
            cancellationToken);
    }

    private async Task<GameFileCheckResult> CheckCoreAsync(
        Action<GameFileCheckProgress>? onProgress,
        CancellationToken cancellationToken)
    {
        void Report(int count, int total, string? path)
        {
            cancellationToken.ThrowIfCancellationRequested();
            onProgress?.Invoke(new GameFileCheckProgress(count, total, path));
            cancellationToken.ThrowIfCancellationRequested();
        }

        Report(0, 0, null);

        // 使用已有本地解析器，不访问 Maven 或其他在线服务。
        var binaries = await GameFileDownloadPlanner.ReadAsync(
            _versionId, _root, _architecture, cancellationToken).ConfigureAwait(false);

        var metadata = await ResourceDownloadPlanner.ReadMetadataAsync(
            _versionId, _root, cancellationToken).ConfigureAwait(false);

        // 现有资源清单首项固定为资源索引。
        DownloadRequest index = metadata[0];
        ResourceDownloadPlanner.ValidateIndexSize(index);

        int checkedFiles = 0;
        int verifiedFiles = 0;
        int knownFiles = checked(binaries.Count + metadata.Count);
        List<GameFileIssue> issues = [];

        async Task<bool> CheckOneAsync(
            string name, string path, long? size, string? sha1,
            bool generated = false)
        {
            GameFileIssue? issue = await InspectFileAsync(
                name, path, size, sha1, generated, cancellationToken).ConfigureAwait(false);

            checkedFiles++;
            if (issue is null)
                verifiedFiles++;
            else
                issues.Add(issue);

            Report(checkedFiles, knownFiles, path);
            return issue is null;
        }

        foreach (var file in binaries)
        {
            await CheckOneAsync(
                file.Name, file.FilePath, file.DeclaredSize,
                file.DeclaredSha1, file.IsLocallyGenerated).ConfigureAwait(false);
        }

        bool indexVerified = false;
        for (int i = 0; i < metadata.Count; i++)
        {
            DownloadRequest request = metadata[i];
            bool valid = await CheckOneAsync(
                i == 0 ? "assetIndex" : "logging",
                request.FilePath, request.Size, request.Sha1).ConfigureAwait(false);

            if (i == 0)
                indexVerified = valid;
        }

        bool objectsEnumerated = false;
        if (indexVerified)
        {
            // 解析器会再次校验它实际读取的索引字节。
            // 不从缺失或校验失败的索引推断资源文件清单。
            var assets = await ResourceDownloadPlanner.ReadAssetObjectsAsync(
                index, _root, cancellationToken).ConfigureAwait(false);

            objectsEnumerated = true;
            knownFiles = checked(knownFiles + assets.Requests.Count);

            foreach (DownloadRequest request in assets.Requests)
            {
                await CheckOneAsync(
                    "assetObject", request.FilePath,
                    request.Size, request.Sha1).ConfigureAwait(false);
            }
        }

        Report(checkedFiles, knownFiles, null);

        return new GameFileCheckResult(
            checkedFiles, verifiedFiles, objectsEnumerated,
            Array.AsReadOnly(issues.ToArray()));
    }

    private static async Task<GameFileIssue?> InspectFileAsync(
        string name,
        string path,
        long? expectedSize,
        string? expectedSha1,
        bool generated,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        GameFileIssue Issue(GameFileIssueKind kind, string message) =>
            new(name, path, kind, message, generated);

        FileStream stream;
        try
        {
            stream = new FileStream(
                path, FileMode.Open, FileAccess.Read, FileShare.Read,
                64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
        catch (FileNotFoundException)
        {
            return Issue(GameFileIssueKind.Missing,
                generated ? "本地生成文件缺失，请重新运行对应加载器安装器。" : "文件缺失。");
        }
        catch (DirectoryNotFoundException)
        {
            return Issue(GameFileIssueKind.Missing,
                generated ? "本地生成文件缺失，请重新运行对应加载器安装器。" : "文件所在目录缺失。");
        }

        await using (stream)
        {
            if (expectedSize is long size && stream.Length != size)
                return Issue(GameFileIssueKind.SizeMismatch,
                    $"预期 {size} 字节，实际 {stream.Length} 字节。");

            List<string> missing = [];
            if (expectedSize is null)
                missing.Add("size");
            if (string.IsNullOrWhiteSpace(expectedSha1))
                missing.Add("sha1");

            if (missing.Count != 0)
                return Issue(GameFileIssueKind.MetadataMissing,
                    $"缺少 {string.Join("、", missing)} 声明，无法离线确认文件内容。");

            byte[] hash = await SHA1.HashDataAsync(
                stream, cancellationToken).ConfigureAwait(false);

            string actualSha1 = Convert.ToHexString(hash).ToLowerInvariant();
            if (!actualSha1.Equals(expectedSha1, StringComparison.OrdinalIgnoreCase))
                return Issue(GameFileIssueKind.Sha1Mismatch,
                    $"SHA-1 不符。预期 {expectedSha1}，实际 {actualSha1}。");

            return null;
        }
    }
}
