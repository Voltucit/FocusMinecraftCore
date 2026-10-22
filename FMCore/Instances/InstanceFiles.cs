using System.Text.Json;
using System.Text.Json.Serialization;
using FMCore.Downloader;
using FMCore.Launch;
using FMCore.Utilities;
using FMCore.Versions;

namespace FMCore.Instances;

internal sealed record InstanceDocument(int SchemaVersion, string Id, Guid Revision, DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt, InstanceConfig Config);

internal static class InstanceFiles
{
    internal const string Metadata = ".fmcore/instance.json";
    private const int SizeLimit = 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 16,
        WriteIndented = true
    };

    internal static string Id(string id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > 80 || id[0] is not (>= 'a' and <= 'z' or >= '0' and <= '9') ||
            id.Any(c => c is not (>= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_' or '.')))
            throw new InstanceException(InstanceErrorCode.UnsafePath, "实例 ID 必须是 1–80 位小写字母、数字、点、下划线或连字符，并以字母或数字开头。");
        DownloadPath.ValidateRelative(id);
        return id;
    }

    internal static InstanceConfig Freeze(InstanceConfig value)
    {
        if (value == null || string.IsNullOrWhiteSpace(value.Name) || value.Name.Length > 128 || value.Name.Any(char.IsControl) ||
            value.JavaPath is { } java && (string.IsNullOrWhiteSpace(java) || java.Length > 4096 || java.Any(char.IsControl)) ||
            !LaunchSettings.ValidMemory(value.MinMemoryMb, value.MaxMemoryMb) ||
            value.Window == null || value.Window.Width is < 1 or > 16384 || value.Window.Height is < 1 or > 16384 ||
            value.JvmArguments == null || value.GameArguments == null)
            throw Invalid();
        try { VersionParser.Id(value.VersionId); }
        catch (Exception e) when (e is VersionException or ArgumentException) { throw Invalid(); }
        var jvm = value.JvmArguments.ToArray();
        var game = value.GameArguments.ToArray();
        var all = jvm.Concat(game).ToArray();
        if (all.Length > 512 || all.Any(a => a == null || a.Length > 32768 || a.Any(char.IsControl)) || all.Sum(a => (long)a.Length) > 128 * 1024)
            throw Invalid();
        GameServerConfig? server;
        try
        {
            LaunchArguments.ValidateCustomArguments(jvm, game);
            server = LaunchSettings.FreezeServer(value.Server);
            LaunchSettings.ValidateGame(game, value.Window, server);
        }
        catch (LaunchException) { throw Invalid(); }
        return value with { JvmArguments = Array.AsReadOnly(jvm), GameArguments = Array.AsReadOnly(game), Window = value.Window with { }, Server = server };
    }

    internal static async Task<InstanceDocument> ReadAsync(string directory, string id, CancellationToken ct)
    {
        var path = GameFiles.Resolve(directory, Metadata);
        try
        {
            await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 8192, FileOptions.Asynchronous);
            if (input.Length > SizeLimit) throw Corrupt();
            using var buffer = new MemoryStream();
            var bytes = new byte[8192];
            int count;
            while ((count = await input.ReadAsync(bytes, ct).ConfigureAwait(false)) != 0)
            {
                if (buffer.Length + count > SizeLimit) throw Corrupt();
                buffer.Write(bytes, 0, count);
            }
            var stored = JsonSerializer.Deserialize<InstanceDocument>(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), JsonOptions) ?? throw Corrupt();
            if (stored.SchemaVersion != 1 || stored.Id != id || stored.Revision == Guid.Empty || stored.CreatedAt == default || stored.UpdatedAt == default)
                throw Corrupt();
            return stored with { Config = Freeze(stored.Config) };
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        { throw new InstanceException(InstanceErrorCode.NotFound, "未找到此 FMCore 托管实例。"); }
        catch (Exception e) when (e is JsonException or NotSupportedException or ArgumentException || e is InstanceException { Code: InstanceErrorCode.InvalidConfig })
        { throw Corrupt(); }
    }

    internal static Task WriteAsync(string directory, InstanceDocument document, CancellationToken ct) => GameFiles.WriteAtomicAsync(
        GameFiles.Resolve(directory, Metadata), (stream, token) => JsonSerializer.SerializeAsync(stream, document, JsonOptions, token), ct);

    // Preflight everything before the first deletion. Delete individual entries without recursive Directory.Delete or following links.
    internal static void Delete(string directory, IProgress<InstanceDeletionProgress>? progress, CancellationToken ct)
    {
        var files = new List<string>();
        var directories = new List<string>();
        var pending = new Stack<string>();
        pending.Push(directory);
        var metadata = GameFiles.Resolve(directory, Metadata);
        var control = Path.GetDirectoryName(metadata)!;
        while (pending.TryPop(out var current))
        {
            ct.ThrowIfCancellationRequested();
            Check(current);
            directories.Add(current);
            foreach (var entry in Directory.EnumerateFileSystemEntries(current))
            {
                ct.ThrowIfCancellationRequested();
                Check(entry);
                if ((File.GetAttributes(entry) & FileAttributes.Directory) != 0) pending.Push(entry);
                else if (!GameFiles.PathComparer.Equals(entry, metadata)) files.Add(entry);
            }
        }
        progress?.Report(new(0, files.Count));
        var completed = 0;
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            Check(file);
            File.Delete(file);
            progress?.Report(new(++completed, files.Count));
        }
        foreach (var child in directories.AsEnumerable().Reverse())
        {
            if (GameFiles.PathComparer.Equals(child, directory) || GameFiles.PathComparer.Equals(child, control)) continue;
            ct.ThrowIfCancellationRequested();
            Check(child);
            Directory.Delete(child); // Fails safely if uncooperative writers created new files.
        }
        ct.ThrowIfCancellationRequested();
        Check(metadata);
        Check(control);
        Check(directory);
        File.Delete(metadata);
        Directory.Delete(control);
        Directory.Delete(directory);

        void Check(string path)
        {
            var relative = Path.GetRelativePath(directory, path);
            if (relative != ".") GameFiles.Resolve(directory, relative);
            else DownloadPath.CheckLinks(directory);
        }
    }

    internal static InstanceException Corrupt() => new(InstanceErrorCode.StorageCorrupted, "实例配置已损坏、过大或使用不支持的格式。");
    private static InstanceException Invalid() => new(InstanceErrorCode.InvalidConfig, "实例名称、版本、Java、内存、窗口或附加参数无效。");
}
