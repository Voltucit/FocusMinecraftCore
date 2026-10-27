using System.Globalization;
using System.Net.Http;
using System.Text.Json.Nodes;

namespace FMCore.Installer;

public sealed partial class MinecraftInstaller
{
    public static async Task<
        IReadOnlyList<MinecraftInstallVersion>> GetVersionsAsync(
            CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        JsonArray entries =
            await ReadManifestVersionsAsync(
                cancellationToken).ConfigureAwait(false);

        List<MinecraftInstallVersion> versions = [];

        HashSet<string> ids = new(StringComparer.Ordinal);

        foreach (JsonNode? node in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (node is not JsonObject entry)
            {
                throw new InvalidDataException(
                    "官方版本清单中的版本项必须是 JSON 对象。");
            }

            string id = ReadText(entry, "id");
            string type = ReadText(entry, "type");
            string releaseText = ReadText(entry, "releaseTime");

            if (!ids.Add(id))
            {
                throw new InvalidDataException(
                    $"官方版本清单中存在重复版本：{id}");
            }

            if (!DateTimeOffset.TryParse(
                    releaseText,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTimeOffset releaseTime))
            {
                throw new InvalidDataException(
                    $"版本 {id} 的发布时间无效。");
            }

            versions.Add(new MinecraftInstallVersion(
                id,
                type,
                releaseTime));
        }

        MinecraftInstallVersion[] ordered =
            versions
                .OrderByDescending(version => version.ReleaseTime)
                .ThenBy(
                    version => version.Id,
                    StringComparer.Ordinal)
                .ToArray();

        cancellationToken.ThrowIfCancellationRequested();

        return Array.AsReadOnly(ordered);
    }

    private static async Task<JsonArray> ReadManifestVersionsAsync(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using HttpResponseMessage response =
            await ReadMetadataResponseAsync(
                HttpMethod.Get,
                ManifestUrl,
                cancellationToken).ConfigureAwait(false);

        string text =
            await response.Content.ReadAsStringAsync(
                cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        JsonObject manifest = JsonNode.Parse(text) as JsonObject
            ?? throw new InvalidDataException(
                "官方版本清单必须是 JSON 对象。");

        if (manifest["versions"] is not JsonArray versions)
        {
            throw new InvalidDataException(
                "官方版本清单缺少 versions 列表。");
        }

        return versions;
    }
}
