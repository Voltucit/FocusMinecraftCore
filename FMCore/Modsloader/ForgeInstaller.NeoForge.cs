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
        _forgeVersion = isNeoForge && gameVersion == "1.20.1"
            && loaderVersion.StartsWith(gameVersion + "-", StringComparison.Ordinal)
                ? loaderVersion[(gameVersion.Length + 1)..] : loaderVersion;
        _root = PathSafety.RequireDirectoryPath(minecraftRoot);
        _javaPath = PathSafety.Resolve(javaPath);
        _maxConcurrency = maxConcurrency;
        _processorMemoryMb = processorMemoryMb;

        if (_root.Contains(';'))
            throw new NotSupportedException("Minecraft 根目录不能包含分号。");

        VersionId = versionId ?? (isNeoForge
            ? $"{gameVersion}-NeoForge-{_forgeVersion}"
            : $"{gameVersion}-forge-{loaderVersion}");

        ValidateName(VersionId);

        if (PathSafety.Comparer.Equals(VersionId, gameVersion))
            throw new ArgumentException("加载器实例 ID 不能与原版相同。");
    }

    private bool IsLegacyNeoForge => _isNeoForge && _gameVersion == "1.20.1";

    internal string MavenVersion => !_isNeoForge || IsLegacyNeoForge
        ? $"{_gameVersion}-{_forgeVersion}" : _forgeVersion;

    internal string ArtifactName => !_isNeoForge || IsLegacyNeoForge ? "forge" : "neoforge";

    internal string MavenRoot => !_isNeoForge ? ForgeMaven
        : IsLegacyNeoForge ? NeoForgeLegacyMaven : NeoForgeMaven;

    internal string PackageCoordinate => !_isNeoForge
        ? $"net.minecraftforge:forge:{MavenVersion}"
        : $"net.neoforged:{ArtifactName}:{MavenVersion}";

    internal void ValidatePackageIdentity(JsonObject install)
    {
        if (_isNeoForge)
        {
            string expectedVersion = IsLegacyNeoForge
                ? $"{_gameVersion}-forge-{_forgeVersion}" : $"neoforge-{_forgeVersion}";
            if (ReadText(install, "version") != expectedVersion
                || install["libraries"] is not JsonArray libraries
                || !libraries.Any(node => node is JsonObject library
                    && library["name"] is JsonValue name
                    && name.TryGetValue<string>(out string? value)
                    && value == PackageCoordinate + ":universal"))
            {
                throw new InvalidDataException("安装包不是所选 NeoForge 版本。");
            }

            return;
        }

        // 一些正式 Forge 安装包没有 path，或将其写为 null。
        // 这种格式仍声明了目标版本及 Forge universal 库，用它们核对身份。
        if (install["path"] is null)
        {
            if (ReadText(install, "version") != $"{_gameVersion}-forge-{_forgeVersion}"
                || install["libraries"] is not JsonArray forgeLibraries
                || !forgeLibraries.Any(node => node is JsonObject library
                    && library["name"] is JsonValue name
                    && name.TryGetValue<string>(out string? value)
                    && value == PackageCoordinate + ":universal"))
            {
                throw new InvalidDataException("安装包不是所选 Forge 版本。");
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
