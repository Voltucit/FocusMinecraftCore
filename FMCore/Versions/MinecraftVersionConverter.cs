using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using FMCore.Downloads;
using FMCore.Installer;
using FMCore.Launch;

namespace FMCore.Versions;

/// <summary>将已有版本转换为不依赖其他版本目录的完整版本，保留存档、Mod 和设置。</summary>
public sealed class MinecraftVersionConverter
{
    internal const string MetadataKey = "_fmcoreStandalone";
    private readonly string _versionId;
    private readonly string _root;
    private int _running;

    public MinecraftVersionConverter(string versionId, string minecraftRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versionId);
        _versionId = versionId;
        _root = PathSafety.RequireDirectoryPath(minecraftRoot);
    }

    public async Task<MinecraftVersion> MakeIndependentAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0)
            throw new InvalidOperationException("这个版本转换器正在执行操作。");
        try
        {
            return await Task.Run(() => ConvertCoreAsync(cancellationToken),
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }

    private async Task<MinecraftVersion> ConvertCoreAsync(CancellationToken cancellationToken)
    {
        MinecraftVersionReader reader = new();
        MinecraftVersion version = await reader.GetVersionAsync(
            _versionId, _root, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<MinecraftVersion> chain = LibraryResolver.GetValidatedChain(version, _root);
        string folder = Path.GetFileName(version.Directory);
        string jsonPath = ContainedFile(version.Directory, folder + ".json");
        byte[] original = await File.ReadAllBytesAsync(jsonPath, cancellationToken).ConfigureAwait(false);
        JsonObject source = MinecraftVersionReader.ReadVersionJson(version.Directory);

        var client = LibraryResolver.GetClientJarSource(version, _root);
        JsonObject owner = MinecraftVersionReader.ReadVersionJson(client.Owner.Directory);
        JsonObject declaration = owner["downloads"]?["client"] as JsonObject
            ?? throw new InvalidDataException("客户端缺少完整下载声明，无法安全生成独立版本。");
        if (declaration["size"] is not JsonValue length
            || !length.TryGetValue<long>(out long size) || size < 0)
            throw new InvalidDataException("客户端缺少有效的 size。");

        DownloadRequest request = FileDownloader.NormalizeRequest(new DownloadRequest
        {
            Url = MinecraftVersionReader.ReadRequiredString(declaration, "url", "客户端缺少 url。"),
            Size = size,
            Sha1 = MinecraftVersionReader.ReadRequiredString(declaration, "sha1", "客户端缺少 sha1。"),
            FilePath = ContainedFile(version.Directory, folder + ".jar"),
            ReplaceInvalidFiles = false
        });

        // 已经独立的版本仍校验自己的客户端，但不再次改写配置。
        if (chain.Count == 1 && !source.ContainsKey("jar"))
        {
            _ = await FileDownloader.CopyVerifiedAsync(client.FilePath, request,
                cancellationToken).ConfigureAwait(false);
            return version;
        }

        JsonObject result = new();
        JsonObject arguments = new();
        foreach (MinecraftVersion current in chain.Reverse())
        {
            cancellationToken.ThrowIfCancellationRequested();
            JsonObject data = MinecraftVersionReader.ReadVersionJson(current.Directory);
            foreach (var pair in data)
            {
                if (pair.Key is "inheritsFrom" or "jar" or "libraries" or "arguments" || pair.Key == MetadataKey)
                    continue;
                result[pair.Key] = pair.Value?.DeepClone();
            }
            if (!data.TryGetPropertyValue("arguments", out JsonNode? node))
                continue;
            if (node is not JsonObject sections)
                throw new InvalidDataException("arguments 必须是对象。");
            foreach (var pair in sections)
            {
                // 26.x 新增 default-user-jvm，与 jvm/game 一样按原顺序合并。
                if (pair.Key is not ("jvm" or "game" or "default-user-jvm"))
                    throw new NotSupportedException("arguments 中包含暂未支持的参数分类。");
                if (pair.Value is not JsonArray values)
                    throw new InvalidDataException($"arguments.{pair.Key} 必须是列表。");
                if (arguments[pair.Key] is not JsonArray destination)
                {
                    destination = new JsonArray();
                    arguments[pair.Key] = destination;
                }
                foreach (JsonNode? value in values)
                    destination.Add(value?.DeepClone());
            }
        }
        if (arguments.Count != 0 || chain.Any(item =>
                MinecraftVersionReader.ReadVersionJson(item.Directory).ContainsKey("arguments")))
            result["arguments"] = arguments;

        // 保留每一层的依赖顺序和条件，不能在转换时固定为某一种 Java 架构。
        JsonArray libraries = new();
        JsonArray layerSizes = new();
        foreach (var layer in chain.SelectMany(LibraryResolver.ReadLibraryLayers))
        {
            cancellationToken.ThrowIfCancellationRequested();
            layerSizes.Add(layer.Count);
            foreach (JsonObject library in layer)
                libraries.Add(library.DeepClone());
        }
        result["libraries"] = libraries;
        result["id"] = version.Id;
        result["mainClass"] = reader.ResolveMainClass(version, reader.GetVersions(_root));
        JsonObject downloads = result["downloads"] as JsonObject ?? new JsonObject();
        downloads["client"] = declaration.DeepClone();
        result["downloads"] = downloads;
        JsonObject metadata = new()
        {
            ["libraryLayers"] = layerSizes,
            ["sourceSha256"] = Fingerprint(source, content: false)
        };
        result[MetadataKey] = metadata;
        metadata["contentSha256"] = Fingerprint(result, content: true);

        await FileDownloader.CopyVerifiedAsync(
            client.FilePath, request, cancellationToken).ConfigureAwait(false);
        await PublishProfileAsync(jsonPath, original, result, cancellationToken).ConfigureAwait(false);
        return await reader.GetVersionAsync(_versionId, _root, cancellationToken).ConfigureAwait(false);
    }

    // 安装器重装同一版本时，独立配置仍能与原始加载器配置核对；用户修改过的配置不会被覆盖。
    internal static bool MatchesInstalledProfile(JsonObject actual, JsonObject expected)
    {
        if (actual[MetadataKey] is not JsonObject metadata)
            return false;
        return metadata["sourceSha256"] is JsonValue source
            && source.TryGetValue<string>(out string? sourceHash)
            && sourceHash == Fingerprint(expected, content: false)
            && metadata["contentSha256"] is JsonValue content
            && content.TryGetValue<string>(out string? contentHash)
            && contentHash == Fingerprint(actual, content: true);
    }

    private static string Fingerprint(JsonObject data, bool content)
    {
        JsonObject value = data.DeepClone().AsObject();
        value.Remove("time");
        value.Remove("releaseTime");
        if (content && value[MetadataKey] is JsonObject metadata)
        {
            metadata.Remove("sourceSha256");
            metadata.Remove("contentSha256");
        }
        return Convert.ToHexString(SHA256.HashData(
            Encoding.UTF8.GetBytes(Canonicalize(value)!.ToJsonString()))).ToLowerInvariant();
    }

    private static JsonNode? Canonicalize(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            JsonObject result = new();
            foreach (var pair in obj.OrderBy(item => item.Key, StringComparer.Ordinal))
                result[pair.Key] = Canonicalize(pair.Value);
            return result;
        }
        if (node is JsonArray array)
            return new JsonArray(array.Select(Canonicalize).ToArray());
        return node?.DeepClone();
    }

    private static string ContainedFile(string directory, string name)
    {
        string path = PathSafety.Resolve(Path.Combine(directory, name));
        if (!PathSafety.IsWithin(path, directory))
            throw new InvalidDataException("独立版本文件越出了当前版本目录。");
        return path;
    }

    private static async Task PublishProfileAsync(
        string path, byte[] original, JsonObject result, CancellationToken cancellationToken)
    {
        string temporary = Path.Combine(Path.GetDirectoryName(path)!, $".fmcore-{Guid.NewGuid():N}.tmp");
        bool created = false;
        try
        {
            await using (FileStream output = new(temporary, FileMode.CreateNew,
                FileAccess.Write, FileShare.None, 64 * 1024, FileOptions.Asynchronous))
            {
                created = true;
                await JsonSerializer.SerializeAsync(output, result,
                    new JsonSerializerOptions { WriteIndented = true }, cancellationToken).ConfigureAwait(false);
                await output.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            byte[] current = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            if (!current.AsSpan().SequenceEqual(original))
                throw new IOException("转换期间版本配置已被修改，保留当前配置，请重试。");
            cancellationToken.ThrowIfCancellationRequested();
            File.Replace(temporary, path, path + $".fmcore-backup-{Guid.NewGuid():N}.bak");
            created = false;
        }
        finally
        {
            if (created)
                File.Delete(temporary);
        }
    }
}
