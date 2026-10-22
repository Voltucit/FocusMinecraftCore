using FMCore.Downloader;
using FMCore.Utilities;

namespace FMCore.Java;

internal static class RuntimeInstallation
{
    internal static async Task<JavaRuntime> InstallAsync(JavaRuntimeVersion version, JavaRuntimeOptions options,
        IProgress<JavaRuntimeProgress>? progress, CancellationToken ct,
        Func<string, CancellationToken, Task<JavaRuntime>>? probe = null)
    {
        ct.ThrowIfCancellationRequested(); RuntimeCatalog.Validate(version);
        probe ??= new JavaManager(new() { ProbeTimeout = options.ProbeTimeout }).ProbeAsync;
        string? root = null; string? key = null; FileStream? lease = null;
        try
        {
            root = RuntimeFiles.Root(options); key = RuntimeFiles.Key(version);
            var downloads = new DownloadManager(options.HttpClient ?? RuntimeCatalog.Http, root, options.Downloads);
            GameFiles.CreateDirectory(Path.GetDirectoryName(RuntimeFiles.At(root, ".fmcore/locks/" + key))!);
            try { lease = new(RuntimeFiles.At(root, ".fmcore/locks/" + key), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException e) when ((e.HResult & 0xffff) is 32 or 33 or 11) { throw new JavaException(JavaErrorCode.RuntimeBusy, "此 Java 包正在安装、校验或恢复，请稍后重试。"); }
            progress?.Report(new(JavaRuntimeStage.Recovering));
            await RecoverAsync(root, key, ct).ConfigureAwait(false);
            var target = RuntimeFiles.At(root, key); var stage = ".fmcore/staging/" + key; var backup = ".fmcore/backups/" + key;
            var owned = await RuntimeFiles.OwnedAsync(root, key, version, ct).ConfigureAwait(false);
            if (Directory.Exists(target) && !owned) throw RuntimeFiles.Conflict();
            if (Directory.Exists(target))
            {
                var receipt = await RuntimeFiles.ReceiptAsync(target, key, ct).ConfigureAwait(false);
                if (receipt != null && receipt.FullVersion == version.FullVersion && await RuntimeFiles.VerifyAsync(target, receipt, progress, ct).ConfigureAwait(false))
                {
                    try
                    {
                        var existing = await ProbeAsync(probe, target, version, ct).ConfigureAwait(false);
                        progress?.Report(new(JavaRuntimeStage.Reused)); return existing;
                    }
                    catch (JavaException e) when (e.Code is JavaErrorCode.ExecutionFailed or JavaErrorCode.InvalidOutput or JavaErrorCode.NotFound or JavaErrorCode.RuntimeValidationFailed) { }
                }
            }
            var request = new DownloadRequest(version.DownloadUri, ".fmcore/downloads/" + version.Sha256 + ".zip")
            { ExpectedHash = version.Sha256, HashAlgorithm = DownloadHashAlgorithm.Sha256, ExpectedSize = version.SizeIsApproximate ? null : version.Size };
            // Resolve approximate vendor lengths from an already hash-verified cache or the official ZIP headers.
            request = request with { ExpectedSize = await ExactSizeAsync(downloads, request, version, options, ct).ConfigureAwait(false) };
            progress?.Report(new(JavaRuntimeStage.Downloading));
            await downloads.DownloadAsync(request, new InlineProgress<DownloadProgress>(p => progress?.Report(new(JavaRuntimeStage.Downloading, Download: p))), ct).ConfigureAwait(false);
            RuntimeFiles.DeleteTree(root, stage);
            var staging = RuntimeFiles.At(root, stage);
            var manifest = await RuntimeArchive.ExtractAsync(RuntimeFiles.At(root, request.RelativePath), staging, version, progress, ct).ConfigureAwait(false);
            RuntimeFiles.CheckNoUnexpected(target, manifest);
            progress?.Report(new(JavaRuntimeStage.Validating));
            await ProbeAsync(probe, staging, version, ct).ConfigureAwait(false);
            await RuntimeFiles.WriteAsync(RuntimeFiles.At(staging, RuntimeFiles.ReceiptName), manifest, ct).ConfigureAwait(false);
            if (!owned) await RuntimeFiles.ClaimAsync(root, key, version, ct).ConfigureAwait(false);
            var transaction = new RuntimeTransaction(1, key, manifest.InstallationId, Directory.Exists(target), false);
            await RuntimeFiles.WriteAsync(Journal(root, key), transaction, ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested(); progress?.Report(new(JavaRuntimeStage.Committing));
            DownloadPath.CheckLinks(target);
            if (Directory.Exists(target))
            {
                GameFiles.CreateDirectory(Path.GetDirectoryName(RuntimeFiles.At(root, backup))!);
                Directory.Move(target, RuntimeFiles.At(root, backup));
            }
            Directory.Move(staging, target);
            var runtime = await ProbeAsync(probe, target, version, ct).ConfigureAwait(false);
            await RuntimeFiles.WriteAsync(Journal(root, key), transaction with { Committed = true }, ct).ConfigureAwait(false);
            await RecoverAsync(root, key, CancellationToken.None).ConfigureAwait(false);
            progress?.Report(new(JavaRuntimeStage.Completed)); return runtime;
        }
        catch (Exception error)
        {
            if (root != null && key != null && lease != null)
            {
                try
                {
                    await RecoverAsync(root, key, CancellationToken.None).ConfigureAwait(false);
                    RuntimeFiles.DeleteTree(root, ".fmcore/staging/" + key);
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException or JavaException or DownloadException)
                { throw new JavaException(JavaErrorCode.RuntimeRecoveryFailed, "Java 安装恢复未完成，已保留恢复记录和备份；请重试同一精确包。"); }
            }
            throw Map(error);
        }
        finally { lease?.Dispose(); }
    }
    private static async Task<JavaRuntime> ProbeAsync(Func<string, CancellationToken, Task<JavaRuntime>> probe, string target, JavaRuntimeVersion expected, CancellationToken ct)
    {
        var executable = Path.Combine(target, "bin", "java.exe"); DownloadPath.CheckLinks(executable);
        var runtime = await probe(executable, ct).ConfigureAwait(false);
        var actual = runtime.Version; var version = expected.Version;
        var home = ComparisonPath(runtime.HomeDirectory);
        var expectedHome = ComparisonPath(target);
        if (runtime.Architecture != expected.Architecture || actual.IsEarlyAccess || (actual.Major, actual.Minor, actual.Security, actual.Patch) != (version.Major, version.Minor, version.Security, version.Patch) ||
            !(home.Equals(expectedHome, StringComparison.OrdinalIgnoreCase) || home.StartsWith(expectedHome + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            throw new JavaException(JavaErrorCode.RuntimeValidationFailed, "实际 Java 的版本、架构或安装目录与所选包不一致。");
        return runtime;
    }
    private static string ComparisonPath(string path)
    {
        var full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);
        if (!OperatingSystem.IsWindows()) return full;
        // Windows/.NET 可为同一实际目录返回长路径前缀；只归一化比较形式，不改变文件操作或链接校验。
        if (full.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase)) return @"\\" + full[8..];
        if (full.Length >= 7 && full.StartsWith(@"\\?\", StringComparison.Ordinal) &&
            char.IsAsciiLetter(full[4]) && full[5] == ':' && full[6] == '\\') return full[4..];
        return full;
    }
    private static async Task<long> ExactSizeAsync(DownloadManager downloads, DownloadRequest request, JavaRuntimeVersion version, JavaRuntimeOptions options, CancellationToken ct)
    {
        if (!version.SizeIsApproximate) return version.Size;
        var cached = RuntimeFiles.At(downloads.RootDirectory, request.RelativePath);
        if (File.Exists(cached) && new FileInfo(cached).Length is > 0 and <= 2L * 1024 * 1024 * 1024 &&
            (await downloads.VerifyAsync(request, ct).ConfigureAwait(false)).State == FileVerificationState.Valid) return new FileInfo(cached).Length;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct); timeout.CancelAfter(options.QueryTimeout);
        try
        {
            using var head = new HttpRequestMessage(HttpMethod.Head, version.DownloadUri);
            using var response = await (options.HttpClient ?? RuntimeCatalog.Http).SendAsync(head, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            if (response.StatusCode != System.Net.HttpStatusCode.OK || response.Content.Headers.ContentLength is not (> 0 and <= 2L * 1024 * 1024 * 1024) ||
                response.RequestMessage?.RequestUri is { } final && (final.Scheme != "https" || final.Host != version.DownloadUri.Host))
                throw new JavaException(JavaErrorCode.RuntimeMetadataInvalid, "官方 Java ZIP 未返回有效的精确长度，未开始下载。");
            return response.Content.Headers.ContentLength.Value;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { throw new JavaException(JavaErrorCode.RuntimeQueryTimeout, "读取官方 Java ZIP 精确长度超时。"); }
        catch (HttpRequestException) { throw new JavaException(JavaErrorCode.RuntimeDownloadFailed, "无法读取官方 Java ZIP 响应头。"); }
    }
    private static string Journal(string root, string key) => RuntimeFiles.At(root, ".fmcore/transactions/" + key + ".json");
    internal static async Task RecoverAsync(string root, string key, CancellationToken ct)
    {
        var path = Journal(root, key);
        if (!File.Exists(path))
        {
            if (Directory.Exists(RuntimeFiles.At(root, ".fmcore/backups/" + key))) throw new JavaException(JavaErrorCode.RuntimeRecoveryFailed, "Java 备份缺少事务记录，未自动删除。");
            RuntimeFiles.DeleteTree(root, ".fmcore/staging/" + key);
            return;
        }
        var state = await RuntimeFiles.ReadAsync<RuntimeTransaction>(path, ct).ConfigureAwait(false);
        var owner = RuntimeFiles.At(root, ".fmcore/owners/" + key);
        if (state == null || state.SchemaVersion != 1 || state.Key != key || state.InstallationId == Guid.Empty || !File.Exists(owner) || new FileInfo(owner).Length > 256 ||
            await File.ReadAllTextAsync(owner, ct).ConfigureAwait(false) != key[^64..]) throw new JavaException(JavaErrorCode.RuntimeRecoveryFailed, "Java 安装恢复记录或所有权标记损坏，未自动删除任何安装。");
        var target = RuntimeFiles.At(root, key); var backup = ".fmcore/backups/" + key; var saved = RuntimeFiles.At(root, backup);
        var staging = ".fmcore/staging/" + key;
        var current = Directory.Exists(target) ? await RuntimeFiles.ReceiptAsync(target, key, ct).ConfigureAwait(false) : null;
        if (state.Committed)
        {
            if (current?.InstallationId != state.InstallationId) throw new JavaException(JavaErrorCode.RuntimeRecoveryFailed, "已提交的 Java 安装目录或清单不一致，备份已保留。");
            RuntimeFiles.DeleteTree(root, backup);
        }
        else if (Directory.Exists(saved))
        {
            if (Directory.Exists(target))
            {
                if (current?.InstallationId != state.InstallationId) throw RuntimeFiles.Conflict();
                RuntimeFiles.DeleteTree(root, key);
            }
            Directory.Move(saved, target);
        }
        else if (!state.HadPrevious && Directory.Exists(target))
        {
            if (current?.InstallationId != state.InstallationId) throw RuntimeFiles.Conflict();
            RuntimeFiles.DeleteTree(root, key);
        }
        else if (state.HadPrevious && !Directory.Exists(target)) throw new JavaException(JavaErrorCode.RuntimeRecoveryFailed, "Java 原安装及其备份均缺失，无法完成恢复。");
        else if (state.HadPrevious && current?.InstallationId == state.InstallationId) throw new JavaException(JavaErrorCode.RuntimeRecoveryFailed, "Java 原安装备份缺失，未自动删除现有安装。");
        RuntimeFiles.DeleteTree(root, staging);
        DownloadPath.CheckLinks(path); File.Delete(path);
    }
    private static Exception Map(Exception error) => error switch
    {
        OperationCanceledException or JavaException => error,
        DownloadException e => new JavaException(e.Code == DownloadErrorCode.UnsafePath ? JavaErrorCode.RuntimeUnsafePath : e.Code is DownloadErrorCode.HashMismatch or DownloadErrorCode.SizeMismatch ? JavaErrorCode.RuntimeIntegrityFailed : JavaErrorCode.RuntimeDownloadFailed, "Java 包下载、校验或安全路径处理失败。"),
        InvalidDataException or OverflowException => RuntimeArchive.Invalid(),
        IOException or UnauthorizedAccessException => new JavaException(JavaErrorCode.IoError, "无法读写 Java 托管目录或安装文件。"),
        ArgumentException or NotSupportedException => new JavaException(JavaErrorCode.RuntimeInvalidOptions, "Java Runtime 路径或配置无效。"),
        _ => error
    };
    private sealed class InlineProgress<T>(Action<T> report) : IProgress<T> { public void Report(T value) => report(value); }
}
