using System.IO.Compression;
using System.Text.Json.Nodes;
using FMCore.Installer;

namespace FMCore.Modsloader;

public sealed partial class ForgeInstaller
{
    private readonly bool _isNeoForge;

    internal ForgeInstaller(
        bool isNeoForge,
        string gameVersion,
        string loaderVersion,
        string minecraftRoot,
        string javaPath,
        string? versionId = null,
        int maxConcurrency = 4,
        int processorMemoryMb = 2048)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("当前加载器安装只支持 Windows。");

        ValidateName(gameVersion);
        ValidateName(loaderVersion);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxConcurrency);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(processorMemoryMb);

        _isNeoForge = isNeoForge;
        _gameVersion = gameVersion;
        _forgeVersion = loaderVersion;
        _root = PathSafety.RequireDirectoryPath(minecraftRoot);
        _javaPath = PathSafety.Resolve(javaPath);
        _maxConcurrency = maxConcurrency;
        _processorMemoryMb = processorMemoryMb;

        if (_root.Contains(';'))
            throw new NotSupportedException("Minecraft 根目录不能包含分号。");

        VersionId = versionId ?? (isNeoForge
            ? $"{gameVersion}-NeoForge-{loaderVersion}"
            : $"{gameVersion}-forge-{loaderVersion}");

        ValidateName(VersionId);

        if (PathSafety.Comparer.Equals(VersionId, gameVersion))
            throw new ArgumentException("加载器实例 ID 不能与原版相同。");
    }

    private string PackageCoordinate => _isNeoForge
        ? $"net.neoforged:neoforge:{_forgeVersion}"
        : $"net.minecraftforge:forge:{_gameVersion}-{_forgeVersion}";

    private void ValidatePackageIdentity(JsonObject install)
    {
        if (_isNeoForge)
        {
            if (ReadText(install, "version") != $"neoforge-{_forgeVersion}"
                || install["libraries"] is not JsonArray libraries
                || !libraries.Any(node => node is JsonObject library
                    && library["name"]?.GetValue<string>()
                        == PackageCoordinate + ":universal"))
            {
                throw new InvalidDataException("安装包不是所选 NeoForge 版本。");
            }

            return;
        }

        string coordinate = ReadText(install, "path");
        _ = FMCore.Launch.LibraryResolver.MavenToPath(coordinate);

        if (coordinate != PackageCoordinate
            && !coordinate.StartsWith(PackageCoordinate + ":", StringComparison.Ordinal))
            throw new InvalidDataException("安装包不是所选 Forge 版本。");
    }

    private async Task CheckNeoForgeOutputAsync(
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken)
    {
        if (!data.TryGetValue("PATCHED", out string? declaredPath))
            throw new InvalidDataException("NeoForge 缺少 PATCHED 产物声明。");

        string path = PathSafety.Resolve(declaredPath);

        if (!PathSafety.IsWithin(
                path,
                PathSafety.RequireDirectoryPath(Path.Combine(_root, "libraries"))))
            throw new InvalidDataException("NeoForge 产物越出了 libraries。");

        cancellationToken.ThrowIfCancellationRequested();

        using ZipArchive archive = ZipFile.OpenRead(path);

        ZipArchiveEntry entry =
            archive.GetEntry("net/minecraft/client/main/Main.class")
            ?? throw new InvalidDataException("NeoForge 客户端 JAR 缺少游戏主类。");

        await using Stream stream = entry.Open();
        byte[] magic = new byte[4];

        await stream.ReadExactlyAsync(
            magic.AsMemory(), cancellationToken).ConfigureAwait(false);

        if (!magic.AsSpan().SequenceEqual(new byte[] { 0xCA, 0xFE, 0xBA, 0xBE }))
            throw new InvalidDataException("NeoForge 客户端主类格式无效。");
    }
}
