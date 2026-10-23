using System.Text.Json;

namespace FMCore.Resources;

// Optional presentation metadata must not break resource queries or trigger extra HTTP calls.
internal static class ResourceImages
{
    internal static Uri? Url(JsonElement value, string name)
    {
        var text = Text(value, name);
        if (text == null || text.Length > 8192 || !Uri.TryCreate(text, UriKind.Absolute, out var uri) ||
            uri.Scheme != "https" || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0) return null;
        foreach (var part in uri.Query.TrimStart('?').Split('&'))
        {
            var key = Uri.UnescapeDataString(part.Split('=')[0]).Replace("-", "").Replace("_", "");
            if (key.Equals("key", StringComparison.OrdinalIgnoreCase) || key.Contains("apikey", StringComparison.OrdinalIgnoreCase) ||
                key.Contains("token", StringComparison.OrdinalIgnoreCase) || key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("authorization", StringComparison.OrdinalIgnoreCase)) return null;
        }
        return uri;
    }

    internal static string? Text(JsonElement value, string name) => value.ValueKind == JsonValueKind.Object &&
        value.TryGetProperty(name, out var text) && text.ValueKind == JsonValueKind.String && text.GetString() is { Length: <= 32768 } result ? result : null;

    internal static IReadOnlyList<ResourceImage> Gallery(JsonElement value, bool curseForge)
    {
        if (!value.TryGetProperty(curseForge ? "screenshots" : "gallery", out var array) || array.ValueKind != JsonValueKind.Array)
            return [];
        var images = new List<ResourceImage>();
        foreach (var item in array.EnumerateArray().Take(512))
        {
            if (item.ValueKind != JsonValueKind.Object || Url(item, "url") is not { } url) continue;
            images.Add(new(url)
            {
                ThumbnailUrl = Url(item, "thumbnailUrl"), Title = Text(item, "title"), Description = Text(item, "description"),
                Id = Integer(item, "id"), Ordering = Integer(item, "ordering"),
                Featured = item.TryGetProperty("featured", out var featured) && featured.ValueKind == JsonValueKind.True,
                CreatedAt = item.TryGetProperty("created", out var date) && date.ValueKind == JsonValueKind.String && date.TryGetDateTimeOffset(out var parsed) ? parsed : null
            });
        }
        return images.AsReadOnly();
    }
    private static int? Integer(JsonElement value, string name) => value.TryGetProperty(name, out var number) &&
        number.ValueKind == JsonValueKind.Number && number.TryGetInt32(out var result) ? result : null;
}
