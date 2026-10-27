using System.Text.Json.Nodes;
using Microsoft.Identity.Client;

namespace FMCore.Accounts;

public sealed partial class MicrosoftAuthenticator
{
    private readonly string _clientId;
    private readonly IPublicClientApplication _application;
    public ITokenCache TokenCache => _application.UserTokenCache;
    private int _running;

    public MicrosoftAuthenticator(string clientId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        if (!Guid.TryParse(clientId, out Guid id)
            || id == Guid.Empty)
        {
            throw new ArgumentException(
                "Client ID 必须是微软应用注册中的应用程序 ID。",
                nameof(clientId));
        }

        _clientId = id.ToString("D");

        _application = PublicClientApplicationBuilder
            .Create(_clientId)
            .WithAuthority(
                "https://login.microsoftonline.com/consumers")
            .Build();
    }

    public async Task<MicrosoftAccount> LoginAsync(
        Action<MicrosoftLoginCode> onLoginCode,
        Action<string>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(onLoginCode);
        cancellationToken.ThrowIfCancellationRequested();

        if (Interlocked.CompareExchange(
                ref _running, 1, 0) != 0)
        {
            throw new InvalidOperationException(
                "这个验证器正在执行登录。");
        }

        try
        {
            void Report(string message)
            {
                cancellationToken.ThrowIfCancellationRequested();
                onProgress?.Invoke(message);
            }

            Report("尝试恢复 Microsoft 授权……");

            AuthenticationResult? microsoft =
                await TryRestoreAuthorizationAsync(
                    cancellationToken).ConfigureAwait(false);

            if (microsoft is null)
            {
                Report("等待 Microsoft 账号授权……");

                microsoft =
                    await _application.AcquireTokenWithDeviceCode(
                            ["XboxLive.signin"],
                            code =>
                            {
                                cancellationToken
                                    .ThrowIfCancellationRequested();

                                onLoginCode(new MicrosoftLoginCode(
                                    code.UserCode,
                                    new Uri(code.VerificationUrl),
                                    code.ExpiresOn,
                                    code.Message));

                                return Task.CompletedTask;
                            })
                        .ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);
            }
            else
            {
                Report("已恢复 Microsoft 授权，无需输入登录码。");
            }

            Report("验证 Xbox 账号……");

            JsonObject xbox = await PostJsonAsync(
                "https://user.auth.xboxlive.com/user/authenticate",
                new
                {
                    Properties = new
                    {
                        AuthMethod = "RPS",
                        SiteName = "user.auth.xboxlive.com",
                        RpsTicket = "d=" + microsoft.AccessToken
                    },
                    RelyingParty = "http://auth.xboxlive.com",
                    TokenType = "JWT"
                },
                "Xbox 账号验证",
                cancellationToken).ConfigureAwait(false);

            string xboxToken = ReadText(xbox, "Token");

            Report("获取 Xbox 服务授权……");

            JsonObject xsts = await PostJsonAsync(
                "https://xsts.auth.xboxlive.com/xsts/authorize",
                new
                {
                    Properties = new
                    {
                        SandboxId = "RETAIL",
                        UserTokens = new[] { xboxToken }
                    },
                    RelyingParty = "rp://api.minecraftservices.com/",
                    TokenType = "JWT"
                },
                "Xbox 服务授权",
                cancellationToken).ConfigureAwait(false);

            string xstsToken = ReadText(xsts, "Token");

            if (xsts["DisplayClaims"]?["xui"]
                    is not JsonArray identities
                || identities.Count == 0
                || identities[0] is not JsonObject identity)
            {
                throw new InvalidDataException(
                    "Xbox 授权响应缺少用户身份。");
            }

            string userHash = ReadText(identity, "uhs");
            string? xuid = identity["xid"]?.GetValue<string>();

            Report("获取 Minecraft 登录令牌……");

            DateTimeOffset requestedAt = DateTimeOffset.UtcNow;

            JsonObject minecraft = await PostJsonAsync(
                "https://api.minecraftservices.com/authentication/login_with_xbox",
                new
                {
                    identityToken =
                        $"XBL3.0 x={userHash};{xstsToken}"
                },
                "Minecraft 登录",
                cancellationToken).ConfigureAwait(false);

            string accessToken =
                ReadText(minecraft, "access_token");

            if (minecraft["expires_in"] is not JsonValue expiry
                || !expiry.TryGetValue<int>(out int seconds)
                || seconds <= 0)
            {
                throw new InvalidDataException(
                    "Minecraft 响应缺少有效的令牌有效期。");
            }

            Report("读取 Minecraft 玩家档案……");

            JsonObject profile = await GetProfileAsync(
                accessToken,
                cancellationToken).ConfigureAwait(false);

            string username = ReadText(profile, "name");
            string uuidText = ReadText(profile, "id");

            if (!Guid.TryParseExact(
                    uuidText,
                    "N",
                    out Guid uuid))
            {
                throw new InvalidDataException(
                    "Minecraft 玩家 UUID 格式无效。");
            }

            MicrosoftAccount account = new(
                username,
                uuid,
                accessToken,
                _clientId,
                xuid,
                requestedAt.AddSeconds(seconds));

            Report("Microsoft 登录完成。");

            return account;
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }
}
