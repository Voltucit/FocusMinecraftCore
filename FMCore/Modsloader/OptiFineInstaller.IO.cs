using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using FMCore.Installer;
using FMCore.Launch;
using FMCore.Versions;

namespace FMCore.Modsloader;

public sealed partial class OptiFineInstaller
{
    private string LibraryPath(string coordinate)
    {
        string root = PathSafety.RequireDirectoryPath(Path.Combine(_root, "libraries"));
        string path = PathSafety.Resolve(
            Path.Combine(root, LibraryResolver.MavenToPath(coordinate)));

        if (!PathSafety.IsWithin(path, root))
            throw new InvalidDataException("依赖路径越出了 libraries。");

        return path;
    }

    private static JsonObject LocalLibrary(string coordinate, byte[] bytes)
        => new()
        {
            ["name"] = coordinate,
            ["downloads"] = new JsonObject
            {
                ["artifact"] = new JsonObject
                {
                    ["path"] = LibraryResolver.MavenToPath(coordinate).Replace('\\', '/'),
                    ["url"] = "",
                    ["size"] = bytes.LongLength,
                    ["sha1"] = Convert.ToHexString(SHA1.HashData(bytes)).ToLowerInvariant()
                }
            }
        };

    private static async Task<byte[]> ReadEntryAsync(
        ZipArchive archive,
        string name,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        ZipArchiveEntry entry = archive.GetEntry(name)
            ?? throw new InvalidDataException($"安装包缺少文件：{name}");

        if (entry.Length < 0 || entry.Length > maximumBytes)
            throw new InvalidDataException($"安装包文件超过大小限制：{name}");

        byte[] bytes = new byte[(int)entry.Length];
        await using Stream stream = entry.Open();
        await stream.ReadExactlyAsync(
            bytes.AsMemory(), cancellationToken).ConfigureAwait(false);
        return bytes;
    }

    private static async Task<byte[]> ReadPatchedJarAsync(
        string path,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (new FileInfo(path).Length > maximumBytes)
            throw new InvalidDataException("补丁产物超过大小限制。");

        using ZipArchive source = ZipFile.OpenRead(path);
        using MemoryStream buffer = new();

        using (ZipArchive target = new(
            buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            long total = 0;

            foreach (ZipArchiveEntry entry in source.Entries
                .OrderBy(item => item.FullName, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (entry.Length < 0 || entry.Length > maximumBytes - total)
                    throw new InvalidDataException("补丁产物解压后超过大小限制。");

                total += entry.Length;
                ZipArchiveEntry output = target.CreateEntry(
                    entry.FullName, CompressionLevel.Optimal);
                output.LastWriteTime = new DateTimeOffset(
                    1980, 1, 1, 0, 0, 0, TimeSpan.Zero);
                output.ExternalAttributes = 0;

                await using Stream input = entry.Open();
                await using Stream destination = output.Open();
                await input.CopyToAsync(
                    destination, cancellationToken).ConfigureAwait(false);
            }
        }

        if (buffer.Length > maximumBytes)
            throw new InvalidDataException("规范化后的补丁产物超过大小限制。");

        return buffer.ToArray();
    }

    private static async Task PublishAsync(
        string path,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(path))
        {
            await using FileStream existing = new(
                path, FileMode.Open, FileAccess.Read,
                FileShare.Read, 64 * 1024, useAsync: true);

            if (existing.Length == bytes.LongLength)
            {
                byte[] actual = await SHA1.HashDataAsync(
                    existing, cancellationToken).ConfigureAwait(false);

                if (actual.AsSpan().SequenceEqual(SHA1.HashData(bytes)))
                    return;
            }

            throw new IOException(
                $"已有文件与本次产物不同，文件已保留；请先备份移走：{path}");
        }

        string directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(
            directory, ".optifine-" + Guid.NewGuid().ToString("N") + ".tmp");
        bool created = false;

        try
        {
            await using (FileStream stream = new(
                temporary, FileMode.CreateNew, FileAccess.Write,
                FileShare.None, 64 * 1024, useAsync: true))
            {
                created = true;
                await stream.WriteAsync(
                    bytes.AsMemory(), cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, path, overwrite: false);
        }
        catch (Exception error)
        {
            if (created)
            {
                try
                {
                    File.Delete(temporary);
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(
                        "写入失败，临时文件清理也失败。", error, cleanup);
                }
            }

            throw;
        }
    }

    private static void CheckProfile(
        string directory,
        string path,
        JsonObject expected)
    {
        if (!File.Exists(path))
            return;

        JsonObject actual = MinecraftVersionReader.ReadVersionJson(directory);
        JsonObject left = actual.DeepClone().AsObject();
        JsonObject right = expected.DeepClone().AsObject();

        foreach (string key in new[] { "time", "releaseTime" })
        {
            left.Remove(key);
            right.Remove(key);
        }

        if (!JsonNode.DeepEquals(left, right))
            throw new IOException(
                "已有实例配置与本次 OptiFine 不同，文件已保留；请换一个实例 ID。");
    }

    private static void ValidateName(string value)
    {
        PathSafety.ValidateWindowsRelativePath(value);

        if (value.Contains('/') || value.Contains('\\'))
            throw new ArgumentException("实例 ID 必须是安全的单个名称。");
    }
}
