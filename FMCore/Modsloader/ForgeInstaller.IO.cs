using System.Security.Cryptography;
using System.Text.Json.Nodes;
using FMCore.Installer;
using FMCore.Versions;

namespace FMCore.Modsloader;

public sealed partial class ForgeInstaller
{
    private sealed record ExpectedFile(string Path, string Sha1, long? Size = null);

    private static async Task<bool> MatchesAsync(
        ExpectedFile file,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(file.Path))
            return false;

        await using FileStream stream = new(
            file.Path, FileMode.Open, FileAccess.Read,
            FileShare.Read, 64 * 1024, useAsync: true);

        if (file.Size is long size && stream.Length != size)
            return false;

        byte[] hash = await SHA1.HashDataAsync(
            stream, cancellationToken).ConfigureAwait(false);

        return Convert.ToHexString(hash).Equals(
            file.Sha1, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task RequireFileAsync(
        ExpectedFile file,
        CancellationToken cancellationToken)
    {
        if (!await MatchesAsync(file, cancellationToken).ConfigureAwait(false))
            throw new InvalidDataException(
                $"Forge 产物缺失或内容校验失败：{file.Path}");
    }

    private static void CheckExistingProfile(
        string directory,
        string jsonPath,
        JsonObject expected)
    {
        if (!File.Exists(jsonPath))
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
                "已有 Forge 实例配置不同，文件已保留；请换一个 versionId。");
    }

    private static async Task WriteNewFileAsync(
        string path,
        byte[] bytes,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string directory = Path.GetDirectoryName(path)
            ?? throw new InvalidDataException("文件没有有效父目录。");

        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, $".forge-{Guid.NewGuid():N}.tmp");
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
                await stream.FlushAsync(
                    cancellationToken).ConfigureAwait(false);
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
                catch (Exception cleanupError)
                {
                    throw new AggregateException(
                        "写入 Forge 文件及清理临时文件均失败。",
                        error, cleanupError);
                }
            }

            throw;
        }
    }
}
