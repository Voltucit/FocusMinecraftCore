using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using FMCore.Installer;
using FMCore.Versions;

namespace FMCore.Modsloader;

public sealed partial class ForgeInstaller
{
    private sealed record ExpectedFile(string Path, string Sha1, long? Size = null);

    // 旧版包将启动库和 universal 放在包内；空 URL 不一定表示由补丁工具生成。
    private async Task RestoreEmbeddedLibrariesAsync(
        ZipArchive archive,
        IReadOnlyList<ExpectedFile> files,
        CancellationToken cancellationToken)
    {
        string libraries = PathSafety.RequireDirectoryPath(Path.Combine(_root, "libraries"));
        foreach (ExpectedFile file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(file.Path))
            {
                await RequireFileAsync(file, cancellationToken).ConfigureAwait(false);
                continue;
            }

            string relative = Path.GetRelativePath(libraries, file.Path).Replace('\\', '/');
            PathSafety.ValidateWindowsRelativePath(relative);
            string entryName = "maven/" + relative;
            if (archive.GetEntry(entryName) is null)
                continue; // 不在包内的文件仍交给补丁工具生成，并在之后校验。

            byte[] bytes = await ReadEntryAsync(
                archive, entryName, 64 * 1024 * 1024, cancellationToken).ConfigureAwait(false);
            if ((file.Size is long size && bytes.LongLength != size)
                || !Convert.ToHexString(SHA1.HashData(bytes)).Equals(
                    file.Sha1, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Forge 包内依赖校验失败：{entryName}");

            await WriteNewFileAsync(file.Path, bytes, cancellationToken).ConfigureAwait(false);
        }
    }

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
