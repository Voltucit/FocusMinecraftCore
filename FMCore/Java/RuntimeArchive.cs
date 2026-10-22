using System.IO.Compression;
using System.Security.Cryptography;
using FMCore.Downloader;
using FMCore.Utilities;

namespace FMCore.Java;

internal static class RuntimeArchive
{
    internal const long MaxEntry = 512L * 1024 * 1024;
    internal static async Task<RuntimeReceipt> ExtractAsync(string archive, string staging, JavaRuntimeVersion version,
        IProgress<JavaRuntimeProgress>? progress, CancellationToken ct)
    {
        DownloadPath.CheckLinks(archive);
        await using var input = new FileStream(archive, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
        using var zip = new ZipArchive(input, ZipArchiveMode.Read);
        if (zip.Entries.Count is < 1 or > RuntimeFiles.MaxFiles) throw Invalid();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase); long total = 0;
        var files = new List<(ZipArchiveEntry Entry, string Path)>();
        foreach (var entry in zip.Entries)
        {
            ct.ThrowIfCancellationRequested();
            var path = entry.FullName;
            if (path.Contains('\\') || (entry.ExternalAttributes >> 16 & 0xf000) == 0xa000 || (entry.ExternalAttributes & 0x400) != 0) throw Invalid();
            var directory = path.EndsWith('/'); var name = directory ? path[..^1] : path;
            DownloadPath.ValidateRelative(name);
            if (!names.Add(name) || name.Split('/').Length > 24 || entry.Length < 0 || entry.Length > MaxEntry ||
                entry.Length > 1024 * 1024 && entry.Length / Math.Max(entry.CompressedLength, 1) > 1000) throw Invalid();
            total = checked(total + entry.Length); if (total > RuntimeFiles.MaxTotal) throw Invalid();
            if (!directory) files.Add((entry, name));
        }
        if (files.Count == 0) throw Invalid();
        var prefix = "";
        if (!files.Any(f => f.Path.Equals("bin/java.exe", StringComparison.OrdinalIgnoreCase)))
        {
            var top = files[0].Path.Split('/')[0];
            if (!files.All(f => f.Path.StartsWith(top + "/", StringComparison.Ordinal)) || !files.Any(f => f.Path.Equals(top + "/bin/java.exe", StringComparison.OrdinalIgnoreCase))) throw Invalid();
            prefix = top + "/";
        }
        var relativeNames = files.Select(f => f.Path[prefix.Length..]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var name in relativeNames)
        {
            if (name.Equals(RuntimeFiles.ReceiptName, StringComparison.OrdinalIgnoreCase)) throw Invalid();
            var segments = name.Split('/');
            for (var i = 1; i < segments.Length; i++) if (relativeNames.Contains(string.Join('/', segments.Take(i)))) throw Invalid();
        }
        GameFiles.CreateDirectory(staging);
        var receipt = new RuntimeReceipt(1, RuntimeFiles.Key(version), version.FullVersion, Guid.NewGuid(), []);
        foreach (var (entry, original) in files)
        {
            ct.ThrowIfCancellationRequested();
            var name = original[prefix.Length..]; var path = RuntimeFiles.At(staging, name);
            GameFiles.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var source = entry.Open();
            await using var destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256); var buffer = new byte[81920]; long size = 0; int read;
            while ((read = await source.ReadAsync(buffer, ct).ConfigureAwait(false)) != 0)
            {
                size += read; if (size > entry.Length || size > MaxEntry) throw Invalid();
                hash.AppendData(buffer, 0, read); await destination.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
            }
            if (size != entry.Length) throw Invalid();
            receipt.Files.Add(new(name, size, Convert.ToHexStringLower(hash.GetHashAndReset())));
            progress?.Report(new(JavaRuntimeStage.Extracting, receipt.Files.Count, files.Count));
        }
        return receipt;
    }
    internal static JavaException Invalid() => new(JavaErrorCode.RuntimeArchiveInvalid, "Java ZIP 格式、路径、链接、重复文件或解压规模不受支持。");
}
