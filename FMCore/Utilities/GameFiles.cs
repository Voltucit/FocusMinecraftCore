using System.Security.Cryptography;
using System.Text;
using FMCore.Downloader;

namespace FMCore.Utilities;

internal static class GameFiles
{
    internal static StringComparer PathComparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    internal static string Resolve(string root, string relative)
    {
        try { return DownloadPath.Resolve(root, relative); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { throw new DownloadException(DownloadErrorCode.IoError, "无法访问游戏文件路径。"); }
        catch (Exception e) when (e is ArgumentException or NotSupportedException)
        { throw new DownloadException(DownloadErrorCode.UnsafePath, "游戏文件路径格式无效。"); }
    }
    internal static void CreateDirectory(string path)
    {
        DownloadPath.CheckLinks(path);
        Directory.CreateDirectory(path);
        DownloadPath.CheckLinks(path);
    }

    internal static async Task<bool> HasSha1Async(string path, string hash, long? size, CancellationToken ct)
    {
        DownloadPath.CheckLinks(path);
        if (!File.Exists(path) || size is long expected && new FileInfo(path).Length != expected) return false;
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Convert.ToHexString(await SHA1.HashDataAsync(input, ct).ConfigureAwait(false)).Equals(hash, StringComparison.OrdinalIgnoreCase);
    }

    internal static async Task<string> ReadJsonAsync(string path, CancellationToken ct)
    {
        const int limit = 16 * 1024 * 1024;
        DownloadPath.CheckLinks(path);
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
        if (input.Length > limit) throw new InvalidDataException();
        using var output = new MemoryStream();
        var buffer = new byte[81920];
        int count;
        while ((count = await input.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            if (output.Length + count > limit) throw new InvalidDataException();
            output.Write(buffer, 0, count);
        }
        return Encoding.UTF8.GetString(output.GetBuffer(), 0, (int)output.Length);
    }

    internal static async Task WriteAtomicAsync(string path, Func<Stream, CancellationToken, Task> write, CancellationToken ct)
    {
        DownloadPath.CheckLinks(path);
        CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = Path.Combine(Path.GetDirectoryName(path)!, $".fmcore-{Guid.NewGuid():N}.tmp");
        var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous);
        try
        {
            await using (stream) { await write(stream, ct).ConfigureAwait(false); await stream.FlushAsync(ct).ConfigureAwait(false); }
            ct.ThrowIfCancellationRequested();
            DownloadPath.CheckLinks(path);
            File.Move(temporary, path, true);
        }
        finally { File.Delete(temporary); }
    }
}
