using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using FMCore.Downloader;
using FMCore.Utilities;

namespace FMCore.Java;

internal sealed record RuntimeFile(string Path, long Size, string Sha256);
internal sealed record RuntimeReceipt(int SchemaVersion, string Key, string FullVersion, Guid InstallationId, List<RuntimeFile> Files);
internal sealed record RuntimeTransaction(int SchemaVersion, string Key, Guid InstallationId, bool HadPrevious, bool Committed);

// SDK-owned runtime boundaries and receipts. Never use archive/API/journal paths as deletion roots.
internal static class RuntimeFiles
{
    internal const string ReceiptName = ".fmcore-runtime.json";
    internal const int MaxFiles = 30000;
    internal const long MaxTotal = 4L * 1024 * 1024 * 1024;
    internal static string DefaultDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FMCore", "runtimes");
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, MaxDepth = 12 };
    internal static string Key(JavaRuntimeVersion version) => $"{version.Distribution.ToString().ToLowerInvariant()}-{version.MajorVersion}-{version.Architecture.ToString().ToLowerInvariant()}-{version.PackageType.ToString().ToLowerInvariant()}-{version.Sha256}";
    internal static string Root(JavaRuntimeOptions options)
    {
        var path = options.InstallationDirectory ?? DefaultDirectory;
        if (string.IsNullOrWhiteSpace(path) || path.Any(char.IsControl)) throw new JavaException(JavaErrorCode.RuntimeInvalidOptions, "Java 托管安装根目录无效。");
        var root = Path.GetFullPath(path); DownloadPath.CheckLinks(root);
        return root;
    }
    internal static string At(string root, string relative) => GameFiles.Resolve(root, relative);
    internal static Task WriteAsync<T>(string path, T value, CancellationToken ct) => GameFiles.WriteAtomicAsync(path,
        (stream, token) => JsonSerializer.SerializeAsync(stream, value, Json, token), ct);
    internal static async Task<T?> ReadAsync<T>(string path, CancellationToken ct) where T : class
    {
        DownloadPath.CheckLinks(path);
        if (!File.Exists(path)) return null;
        try { return JsonSerializer.Deserialize<T>(await GameFiles.ReadJsonAsync(path, ct).ConfigureAwait(false), Json); }
        catch (Exception e) when (e is JsonException or InvalidDataException or NotSupportedException) { return null; }
    }
    internal static async Task<RuntimeReceipt?> ReceiptAsync(string directory, string key, CancellationToken ct)
    {
        var value = await ReadAsync<RuntimeReceipt>(At(directory, ReceiptName), ct).ConfigureAwait(false);
        if (value == null || value.SchemaVersion != 1 || value.Key != key || value.InstallationId == Guid.Empty || value.Files == null || value.Files.Count is < 1 or > MaxFiles || value.FullVersion == null) return null;
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase); long total = 0;
        try
        {
            foreach (var file in value.Files)
            {
                if (file == null || file.Path == null || file.Path.Contains('\\') || file.Path.Equals(ReceiptName, StringComparison.OrdinalIgnoreCase) || !paths.Add(file.Path) ||
                    file.Size < 0 || file.Size > RuntimeArchive.MaxEntry || file.Sha256 == null || file.Sha256.Length != 64 || !file.Sha256.All(char.IsAsciiHexDigit)) return null;
                At(directory, file.Path); total = checked(total + file.Size);
            }
        }
        catch (OverflowException) { return null; }
        return total > MaxTotal || !paths.Contains("bin/java.exe") ? null : value;
    }
    internal static List<string> Tree(string directory)
    {
        var files = new List<string>(); var pending = new Stack<string>(); pending.Push(directory); var count = 0;
        while (pending.TryPop(out var current))
        {
            if (Path.GetRelativePath(directory, current).Split(Path.DirectorySeparatorChar).Length > 32) throw Conflict();
            DownloadPath.CheckLinks(current);
            foreach (var path in Directory.EnumerateFileSystemEntries(current))
            {
                if (++count > MaxFiles * 2) throw Conflict();
                DownloadPath.CheckLinks(path);
                if (Directory.Exists(path)) pending.Push(path); else files.Add(path);
            }
        }
        return files;
    }
    internal static void CheckNoUnexpected(string target, RuntimeReceipt receipt)
    {
        if (!Directory.Exists(target)) return;
        var expected = receipt.Files.Select(f => f.Path).Append(ReceiptName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (Tree(target).Any(path => !expected.Contains(Path.GetRelativePath(target, path).Replace('\\', '/')))) throw Conflict();
    }
    internal static async Task<bool> VerifyAsync(string target, RuntimeReceipt receipt, IProgress<JavaRuntimeProgress>? progress, CancellationToken ct)
    {
        CheckNoUnexpected(target, receipt);
        var completed = 0;
        foreach (var file in receipt.Files)
        {
            ct.ThrowIfCancellationRequested();
            var path = At(target, file.Path);
            if (!File.Exists(path) || new FileInfo(path).Length != file.Size) return false;
            await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
            if (!Convert.ToHexString(await SHA256.HashDataAsync(input, ct).ConfigureAwait(false)).Equals(file.Sha256, StringComparison.OrdinalIgnoreCase)) return false;
            progress?.Report(new(JavaRuntimeStage.Verifying, ++completed, receipt.Files.Count));
        }
        return true;
    }
    internal static async Task<bool> OwnedAsync(string root, string key, JavaRuntimeVersion version, CancellationToken ct)
    {
        var marker = At(root, ".fmcore/owners/" + key);
        if (!File.Exists(marker)) return false;
        if (new FileInfo(marker).Length > 256 || await File.ReadAllTextAsync(marker, ct).ConfigureAwait(false) != version.Sha256) throw Conflict();
        return true;
    }
    internal static async Task ClaimAsync(string root, string key, JavaRuntimeVersion version, CancellationToken ct)
    {
        await GameFiles.WriteAtomicAsync(At(root, ".fmcore/owners/" + key), async (stream, token) =>
            await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes(version.Sha256), token).ConfigureAwait(false), ct).ConfigureAwait(false);
    }
    internal static void DeleteTree(string root, string relative)
    {
        // Resolve once, validate the complete tree before deleting, then recheck each entry; never recurse through links.
        var directory = At(root, relative);
        if (!Directory.Exists(directory)) return;
        Tree(directory);
        Delete(directory);
        static void Delete(string path)
        {
            DownloadPath.CheckLinks(path);
            foreach (var entry in Directory.EnumerateFileSystemEntries(path))
            {
                DownloadPath.CheckLinks(entry);
                if (Directory.Exists(entry)) Delete(entry); else File.Delete(entry);
            }
            DownloadPath.CheckLinks(path); Directory.Delete(path);
        }
    }
    internal static JavaException Conflict() => new(JavaErrorCode.RuntimeConflict, "Java 托管目录存在非本次管理的文件、无效所有权记录或冲突状态，未覆盖或删除。");
}
