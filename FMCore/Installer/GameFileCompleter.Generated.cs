using System.Security.Cryptography;

namespace FMCore.Installer;

public sealed partial class GameFileCompleter
{
    private static async Task VerifyGeneratedFilesAsync(
        IReadOnlyList<GameDownloadFile> files,
        Action<string> onProgress,
        CancellationToken cancellationToken)
    {
        for (int index = 0; index < files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GameDownloadFile file = files[index];

            string expectedSha1 = file.DeclaredSha1
                ?? throw new InvalidDataException("生成文件缺少 SHA-1 声明。");

            long expectedSize = file.DeclaredSize
                ?? throw new InvalidDataException("生成文件缺少大小声明。");

            onProgress(
                $"校验本地生成文件 {index + 1}/{files.Count}：{file.Name}");

            string missingMessage =
                "本地生成文件缺失，不能直接下载。"
                + "请重新运行对应加载器安装器生成："
                + Environment.NewLine + file.FilePath;

            try
            {
                await using FileStream stream = new(
                    file.FilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    64 * 1024,
                    useAsync: true);

                bool valid = stream.Length == expectedSize;

                if (valid)
                {
                    byte[] hash = await SHA1.HashDataAsync(
                        stream, cancellationToken).ConfigureAwait(false);

                    valid = Convert.ToHexString(hash).Equals(
                        expectedSha1, StringComparison.OrdinalIgnoreCase);
                }

                if (!valid)
                {
                    throw new InvalidDataException(
                        "本地生成文件校验失败，文件已保留。"
                        + "请先备份并移走此文件，再运行对应加载器安装器重新生成："
                        + Environment.NewLine + file.FilePath);
                }
            }
            catch (FileNotFoundException error)
            {
                throw new FileNotFoundException(
                    missingMessage, file.FilePath, error);
            }
            catch (DirectoryNotFoundException error)
            {
                throw new FileNotFoundException(
                    missingMessage, file.FilePath, error);
            }
        }
    }
}
