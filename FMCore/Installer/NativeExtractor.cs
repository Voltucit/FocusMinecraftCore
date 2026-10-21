using System.IO.Compression;
using System.Security.Cryptography;
using FMCore.Downloader;
using FMCore.Utilities;

namespace FMCore.Installer;

/// <summary>Rebuilds an SDK-owned native directory. Its selection input comes from GameInstaller.CreatePlan.</summary>
public sealed class NativeExtractor
{
    private readonly string root;
    private readonly NativeExtractionOptions options;

    public NativeExtractor(string rootDirectory, NativeExtractionOptions? options = null)
    {
        root = Path.GetFullPath(rootDirectory);
        DownloadPath.CheckLinks(root);
        this.options = options ?? new();
        if (this.options.MaxEntries is < 1 or > 100_000 || this.options.MaxEntryBytes is < 1 or > 4L * 1024 * 1024 * 1024 ||
            this.options.MaxTotalBytes < this.options.MaxEntryBytes || this.options.MaxTotalBytes > 16L * 1024 * 1024 * 1024)
            throw new ArgumentOutOfRangeException(nameof(options));
    }

    public async Task<string> ExtractAsync(IReadOnlyList<NativeArchive> archives, string relativeDirectory,
        IProgress<NativeExtractionProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(archives);
        var target = GameFiles.Resolve(root, relativeDirectory);
        var parent = Path.GetDirectoryName(target)!;
        var staging = Path.Combine(parent, $".fmcore-natives-{Guid.NewGuid():N}");
        var backup = Path.Combine(parent, $".fmcore-natives-{Guid.NewGuid():N}");
        var committed = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var archive in archives)
            {
                var path = GameFiles.Resolve(root, archive.RelativePath);
                if (!File.Exists(path) || archive.Size is long size && new FileInfo(path).Length != size ||
                    archive.Sha1 != null && !await GameFiles.HasSha1Async(path, archive.Sha1, archive.Size, cancellationToken).ConfigureAwait(false))
                    throw Invalid("原生依赖文件缺失或校验失败。");
                foreach (var exclude in archive.Excludes) DownloadPath.ValidateRelative(exclude.TrimEnd('/', '\\'));
            }
            if (Directory.Exists(target)) CheckTree(target);
            GameFiles.CreateDirectory(staging);
            var outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var count = 0;
            long total = 0;
            progress?.Report(new(0, archives.Count));
            for (var i = 0; i < archives.Count; i++)
            {
                var archive = archives[i];
                await using var input = new FileStream(GameFiles.Resolve(root, archive.RelativePath), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
                using var zip = new ZipArchive(input, ZipArchiveMode.Read);
                var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in zip.Entries)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var name = entry.FullName;
                    var directory = name.EndsWith('/') || (entry.ExternalAttributes & (int)FileAttributes.Directory) != 0;
                    var clean = directory ? name.TrimEnd('/') : name;
                    if (name.Contains('\\') || !names.Add(clean)) throw Unsafe();
                    DownloadPath.ValidateRelative(clean);
                    var unixType = (entry.ExternalAttributes >> 16) & 0xf000;
                    if (unixType is not (0 or 0x8000 or 0x4000) || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0) throw Unsafe();
                    if (++count > options.MaxEntries || entry.Length > options.MaxEntryBytes || total > options.MaxTotalBytes - entry.Length)
                        throw new InstallationException(InstallationErrorCode.ArchiveLimitExceeded, "原生依赖压缩包超过条目或解压大小限制。");
                    total += entry.Length;
                    // JAR signatures, indexes and module descriptors are not native runtime files.
                    if (directory || name.StartsWith("META-INF/", StringComparison.OrdinalIgnoreCase) ||
                        archive.Excludes.Any(prefix => name.StartsWith(prefix.Replace('\\', '/'), StringComparison.Ordinal))) continue;
                    if (outputs.TryGetValue(clean, out var previous) && previous != clean)
                        throw new InstallationException(InstallationErrorCode.ConflictingFiles, "原生依赖包含大小写冲突的文件。");
                    outputs[clean] = clean;
                    var destination = GameFiles.Resolve(staging, clean);
                    await GameFiles.WriteAtomicAsync(destination, async (output, ct) =>
                    {
                        await using var content = entry.Open();
                        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                        var buffer = new byte[81920];
                        long length = 0;
                        int read;
                        while ((read = await content.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                        {
                            length += read;
                            if (length > entry.Length || length > options.MaxEntryBytes) throw Invalid("原生依赖条目大小无效。");
                            hash.AppendData(buffer, 0, read);
                            await output.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                        }
                        if (length != entry.Length) throw Invalid("原生依赖条目内容不完整。");
                        if (File.Exists(destination))
                        {
                            await using var existing = File.OpenRead(destination);
                            var existingHash = await SHA256.HashDataAsync(existing, ct).ConfigureAwait(false);
                            if (!hash.GetHashAndReset().AsSpan().SequenceEqual(existingHash))
                                throw new InstallationException(InstallationErrorCode.ConflictingFiles, "不同原生依赖包含内容冲突的同名文件。");
                        }
                    }, cancellationToken).ConfigureAwait(false);
                }
                progress?.Report(new(i + 1, archives.Count));
            }
            cancellationToken.ThrowIfCancellationRequested();
            DownloadPath.CheckLinks(target);
            if (Directory.Exists(target)) Directory.Move(target, backup);
            try { Directory.Move(staging, target); committed = true; }
            catch
            {
                if (Directory.Exists(backup) && !Directory.Exists(target)) Directory.Move(backup, target);
                throw;
            }
            return target;
        }
        catch (InvalidDataException) { throw Invalid("原生依赖不是有效的 ZIP/JAR 压缩包。"); }
        catch (DownloadException e) when (e.Code == DownloadErrorCode.UnsafePath) { throw Unsafe(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { throw new InstallationException(InstallationErrorCode.IoError, "原生依赖解压或目录替换失败。"); }
        finally
        {
            DeleteOwnedDirectory(staging);
            if (committed) DeleteOwnedDirectory(backup);
        }
    }

    private void DeleteOwnedDirectory(string path)
    {
        var full = Path.GetFullPath(path);
        var relative = Path.GetRelativePath(root, full);
        if (!GameFiles.PathComparer.Equals(GameFiles.Resolve(root, relative), full) || !Path.GetFileName(full).StartsWith(".fmcore-natives-", StringComparison.Ordinal)) throw Unsafe();
        if (!Directory.Exists(full)) return;
        CheckTree(full);
        Directory.Delete(full, recursive: true);
    }

    private static void CheckTree(string path)
    {
        var pending = new Stack<string>();
        pending.Push(path);
        while (pending.TryPop(out var directory))
        {
            DownloadPath.CheckLinks(directory);
            foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
            {
                DownloadPath.CheckLinks(entry);
                if (Directory.Exists(entry)) pending.Push(entry);
            }
        }
    }

    private static InstallationException Invalid(string message) => new(InstallationErrorCode.InvalidArchive, message);
    private static InstallationException Unsafe() => new(InstallationErrorCode.UnsafePath, "原生依赖压缩包或解压目录包含不安全的路径、链接或特殊文件。");
}
