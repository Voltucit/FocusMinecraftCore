using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace FMCore.Modsloader;

public sealed partial class OptiFineInstaller
{
    internal static async Task<(string GameVersion, string Edition)> ReadInstallerVersionAsync(
        string path,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using ZipArchive archive = ZipFile.OpenRead(path);
        return await ReadInstallerVersionAsync(
            archive, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<(string GameVersion, string Edition)> ReadInstallerVersionAsync(
        ZipArchive archive,
        CancellationToken cancellationToken)
    {
        _ = archive.GetEntry("optifine/Patcher.class")
            ?? throw new InvalidDataException("文件不是支持的 OptiFine 安装包。");

        string configName = new[]
        {
            "notch/net/optifine/Config.class",
            "net/optifine/Config.class",
            "Config.class"
        }.FirstOrDefault(name => archive.GetEntry(name) is not null)
            ?? throw new InvalidDataException("安装包缺少 OptiFine 版本声明。");

        byte[] config = await ReadEntryAsync(
            archive, configName, 4 * 1024 * 1024,
            cancellationToken).ConfigureAwait(false);

        Match[] matches = Regex.Matches(
                Encoding.Latin1.GetString(config),
                @"OptiFine_([0-9]+(?:\.[0-9]+){1,2})_(HD_U_[A-Za-z0-9_]+)")
            .Cast<Match>().ToArray();

        if (matches.Length == 0
            || matches.Select(match => match.Value).Distinct().Count() != 1)
            throw new InvalidDataException("OptiFine 版本声明缺失或不一致。");

        string gameVersion = matches[0].Groups[1].Value;
        string edition = matches[0].Groups[2].Value;
        return (gameVersion, edition);
    }
}

