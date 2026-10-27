using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FMCore.Accounts;

public sealed partial class MicrosoftAuthenticator
{
    private static readonly HttpClient Client = new(
        new HttpClientHandler
        {
            AllowAutoRedirect = false
        })
    {
        Timeout = TimeSpan.FromSeconds(30),
        MaxResponseContentBufferSize = 1024 * 1024
    };

    private static async Task<JsonObject> PostJsonAsync(
        string url,
        object payload,
        string step,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Post,
            url)
        {
            Content = JsonContent.Create(
                payload,
                options: JsonSerializerOptions.Default)
        };

        if (request.RequestUri!.Host.EndsWith(
                ".xboxlive.com",
                StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Add("x-xbl-contract-version", "1");
        }

        return await SendJsonAsync(
            request,
            step,
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<JsonObject> GetProfileAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(
            HttpMethod.Get,
            "https://api.minecraftservices.com/minecraft/profile");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        return await SendJsonAsync(
            request,
            "Minecraft 玩家档案读取",
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task<JsonObject> SendJsonAsync(
        HttpRequestMessage request,
        string step,
        CancellationToken cancellationToken)
    {
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/json"));

        try
        {
            using HttpResponseMessage response =
                await Client.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    cancellationToken).ConfigureAwait(false);

            byte[] bytes =
                await response.Content.ReadAsByteArrayAsync(
                    cancellationToken).ConfigureAwait(false);

            JsonObject? body = null;

            try
            {
                body = JsonNode.Parse(bytes) as JsonObject;
            }
            catch (JsonException)
                when (!response.IsSuccessStatusCode)
            {
                // 错误页可能不是 JSON，仍按 HTTP 失败报告。
            }

            if (!response.IsSuccessStatusCode)
            {
                string details = "";

                if (body?["XErr"] is JsonValue value
                    && value.TryGetValue<long>(out long xboxError))
                {
                    details = $"，Xbox 错误码 {xboxError}";
                }

                throw new HttpRequestException(
                    $"{step}失败：HTTP " +
                    $"{(int)response.StatusCode}{details}。",
                    null,
                    response.StatusCode);
            }

            return body
                ?? throw new InvalidDataException(
                    $"{step}未返回有效的 JSON 对象。");
        }
        catch (OperationCanceledException error)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"{step}请求超过 30 秒。",
                error);
        }
    }

    private static string ReadText(
        JsonObject value,
        string key)
    {
        string? text = value[key]?.GetValue<string>();

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidDataException(
                $"认证响应缺少有效的 {key}。");
        }

        return text;
    }
}
