using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FMCore.Accounts;

public sealed class YggdrasilAuthenticator
{
    private static readonly HttpClient Client = new(
        new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(30),
        MaxResponseContentBufferSize = 1024 * 1024
    };

    public Uri ApiRoot { get; }

    public YggdrasilAuthenticator(string apiRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiRoot);

        if (!Uri.TryCreate(apiRoot.Trim(), UriKind.Absolute, out Uri? uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || uri.UserInfo.Length != 0
            || uri.Query.Length != 0
            || uri.Fragment.Length != 0)
        {
            throw new ArgumentException(
                "请提供不含账号、查询参数或片段的 HTTPS API 根地址。",
                nameof(apiRoot));
        }

        ApiRoot = new Uri(uri.AbsoluteUri.TrimEnd('/'));
    }

    public async Task<YggdrasilAccount> LoginAsync(
        string username,
        string password,
        Func<IReadOnlyList<YggdrasilProfile>, CancellationToken, Task<Guid>>?
            selectProfile = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        cancellationToken.ThrowIfCancellationRequested();

        string loginName = username.Trim();
        string clientToken = Guid.NewGuid().ToString("N");

        JsonObject response = ReadObject(await SendAsync(
            "authserver/authenticate",
            new
            {
                username = loginName,
                password,
                clientToken,
                requestUser = true,
                agent = new { name = "Minecraft", version = 1 }
            },
            "外置账号登录",
            cancellationToken).ConfigureAwait(false));

        string accessToken = ReadAccessToken(response, clientToken);
        YggdrasilProfile profile;

        if (response["selectedProfile"] is not null)
        {
            profile = ReadProfile(response["selectedProfile"]);
        }
        else
        {
            if (response["availableProfiles"] is not JsonArray available)
                throw new InvalidDataException("登录响应缺少角色列表。");

            YggdrasilProfile[] profiles = available
                .Select(ReadProfile)
                .ToArray();

            if (profiles.Length == 0)
            {
                throw new InvalidOperationException(
                    "该账号没有游戏角色，请先在验证服务网站创建角色。");
            }

            if (profiles.Select(item => item.Uuid).Distinct().Count()
                != profiles.Length)
            {
                throw new InvalidDataException("验证服务返回了重复角色 UUID。");
            }

            Guid selected;

            if (profiles.Length == 1)
            {
                selected = profiles[0].Uuid;
            }
            else
            {
                if (selectProfile is null)
                {
                    throw new InvalidOperationException(
                        "该账号有多个角色，请提供 selectProfile 回调选择角色。");
                }

                selected = await selectProfile(
                    Array.AsReadOnly(profiles),
                    cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            profile = profiles.FirstOrDefault(item => item.Uuid == selected)
                ?? throw new ArgumentException("选择的角色不属于本次角色列表。");

            response = ReadObject(await SendAsync(
                "authserver/refresh",
                new
                {
                    accessToken,
                    clientToken,
                    requestUser = true,
                    selectedProfile = new
                    {
                        id = profile.Uuid.ToString("N"),
                        name = profile.Name
                    }
                },
                "外置账号角色绑定",
                cancellationToken).ConfigureAwait(false));

            accessToken = ReadAccessToken(response, clientToken);
            YggdrasilProfile bound = ReadProfile(response["selectedProfile"]);

            if (bound.Uuid != selected)
                throw new InvalidDataException("验证服务绑定了不同的角色。");

            profile = bound;
        }

        cancellationToken.ThrowIfCancellationRequested();

        return new YggdrasilAccount(
            ApiRoot, loginName, profile, accessToken, clientToken);
    }

    public async Task<YggdrasilAccount> RestoreAsync(
        string loginName,
        Guid profileUuid,
        string accessToken,
        string clientToken,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(loginName);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientToken);

        if (profileUuid == Guid.Empty)
        {
            throw new ArgumentException(
                "恢复账号时必须提供有效的角色 UUID。",
                nameof(profileUuid));
        }

        cancellationToken.ThrowIfCancellationRequested();

        JsonObject response = ReadObject(await SendAsync(
            "authserver/refresh",
            new
            {
                accessToken,
                clientToken,
                requestUser = true
            },
            "外置账号授权恢复",
            cancellationToken).ConfigureAwait(false));

        string updatedAccessToken =
            ReadAccessToken(response, clientToken);

        YggdrasilProfile profile =
            ReadProfile(response["selectedProfile"]);

        if (profile.Uuid != profileUuid)
        {
            throw new InvalidDataException(
                "恢复后的角色 UUID 与保存的账号不一致，请重新登录。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        return new YggdrasilAccount(
            ApiRoot,
            loginName.Trim(),
            profile,
            updatedAccessToken,
            clientToken);
    }

    internal static async Task<string[]> GetLaunchArgumentsAsync(
        YggdrasilAccount account,
        string agentPath,
        CancellationToken cancellationToken)
    {
        YggdrasilAuthenticator auth = new(account.ApiRoot.AbsoluteUri);

        _ = await auth.SendAsync(
            "authserver/validate",
            new
            {
                accessToken = account.AccessToken,
                clientToken = account.ClientToken
            },
            "外置验证凭证检查（失效时请重新登录）",
            cancellationToken,
            HttpStatusCode.NoContent).ConfigureAwait(false);

        byte[] metadata = await auth.SendAsync(
            "", null, "外置验证服务配置读取",
            cancellationToken).ConfigureAwait(false);

        _ = ReadText(ReadObject(metadata), "signaturePublickey");

        return
        [
            $"-javaagent:{agentPath}={account.ApiRoot.AbsoluteUri}",
            "-Dauthlibinjector.yggdrasil.prefetched="
                + Convert.ToBase64String(metadata)
        ];
    }

    private async Task<byte[]> SendAsync(
        string relativePath,
        object? payload,
        string step,
        CancellationToken cancellationToken,
        HttpStatusCode expectedStatus = HttpStatusCode.OK)
    {
        Uri uri = relativePath.Length == 0
            ? ApiRoot
            : new Uri(ApiRoot.AbsoluteUri.TrimEnd('/') + "/" + relativePath);

        using HttpRequestMessage request = new(
            payload is null ? HttpMethod.Get : HttpMethod.Post,
            uri);

        if (payload is not null)
        {
            request.Content = JsonContent.Create(
                payload, options: JsonSerializerOptions.Default);
        }

        request.Headers.Accept.ParseAdd("application/json");

        try
        {
            using HttpResponseMessage response = await Client.SendAsync(
                request,
                HttpCompletionOption.ResponseContentRead,
                cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"{step}失败：HTTP {(int)response.StatusCode}。",
                    null,
                    response.StatusCode);
            }

            if (response.StatusCode != expectedStatus)
                throw new InvalidDataException($"{step}返回了非预期的 HTTP 状态。");

            return await response.Content.ReadAsByteArrayAsync(
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException error)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"{step}请求超过 30 秒。", error);
        }
    }

    private static JsonObject ReadObject(byte[] bytes)
        => JsonNode.Parse(bytes) as JsonObject
            ?? throw new InvalidDataException("验证服务未返回 JSON 对象。");

    private static string ReadText(JsonObject value, string key)
    {
        if (value[key] is not JsonValue field
            || !field.TryGetValue<string>(out string? text)
            || string.IsNullOrWhiteSpace(text)
            || text.Contains('\0'))
        {
            throw new InvalidDataException($"验证响应缺少有效的 {key}。");
        }

        return text;
    }

    private static string ReadAccessToken(JsonObject response, string expected)
    {
        if (ReadText(response, "clientToken") != expected)
            throw new InvalidDataException("验证响应的 clientToken 不一致。");

        return ReadText(response, "accessToken");
    }

    private static YggdrasilProfile ReadProfile(JsonNode? node)
    {
        if (node is not JsonObject profile
            || !Guid.TryParseExact(ReadText(profile, "id"), "N", out Guid uuid)
            || uuid == Guid.Empty)
        {
            throw new InvalidDataException("验证响应包含无效角色 UUID。");
        }

        return new YggdrasilProfile(uuid, ReadText(profile, "name"));
    }
}
