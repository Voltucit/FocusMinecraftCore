using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace FMCore.Authentication;

internal sealed record MicrosoftCredential(int SchemaVersion, Guid Uuid, string ClientId, string RefreshToken)
{
    public override string ToString() => "Protected Microsoft refresh credential";
}
internal sealed record MicrosoftAuthenticationResult(Guid Uuid, string Name, string AccessToken, DateTimeOffset ExpiresAt,
    string RefreshToken, string? Xuid)
{
    public override string ToString() => "Microsoft authentication result";
}

internal sealed class MicrosoftAuthentication
{
    private const string Authority = "https://login.microsoftonline.com/consumers/oauth2/v2.0/";
    private const string Scope = "XboxLive.signin offline_access";
    private readonly HttpClient http;
    private readonly TimeProvider clock;
    private readonly Func<TimeSpan, CancellationToken, Task> delay;

    internal MicrosoftAuthentication(HttpClient http, TimeProvider clock, Func<TimeSpan, CancellationToken, Task>? delay = null)
    { this.http = http; this.clock = clock; this.delay = delay ?? ((interval, ct) => Task.Delay(interval, ct)); }

    internal async Task<MicrosoftAuthenticationResult> LoginAsync(MicrosoftLoginOptions options,
        Func<DeviceCodeInfo, CancellationToken, Task> showCode, IProgress<AuthenticationProgress>? progress, CancellationToken ct)
    {
        options.Validate();
        progress?.Report(new(AuthenticationStage.DeviceAuthorization));
        using var device = await SendAsync(Form("devicecode", ("client_id", options.ClientId), ("scope", Scope)),
            AuthenticationStage.DeviceAuthorization, options.RequestTimeout, ct).ConfigureAwait(false);
        EnsureSuccess(device);
        var root = device.Document.RootElement;
        var deviceCode = Text(root, "device_code");
        var userCode = Text(root, "user_code", 128);
        var deadline = clock.GetUtcNow().AddSeconds(Seconds(root, "expires_in", 3600));
        var interval = TimeSpan.FromSeconds(root.TryGetProperty("interval", out _) ? Seconds(root, "interval", 60) : 5);
        if (!Uri.TryCreate(Text(root, "verification_uri", 2048), UriKind.Absolute, out var verification) ||
            verification.Scheme != "https" || !verification.IsDefaultPort || verification.UserInfo.Length != 0 ||
            verification.Query.Length != 0 || verification.Fragment.Length != 0 ||
            verification.Host is not ("microsoft.com" or "www.microsoft.com" or "login.microsoftonline.com" or "login.live.com")) throw Invalid(AuthenticationStage.DeviceAuthorization);
        progress?.Report(new(AuthenticationStage.WaitingForAuthorization));
        try { await showCode(new(userCode, verification, deadline), ct).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { throw new AuthenticationException(AuthenticationErrorCode.InvalidInput, "授权提示回调执行失败。", AuthenticationStage.DeviceAuthorization); }
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var remaining = deadline - clock.GetUtcNow();
            if (remaining <= TimeSpan.Zero) throw new AuthenticationException(AuthenticationErrorCode.DeviceCodeExpired, "Microsoft 授权码已过期。", AuthenticationStage.WaitingForAuthorization);
            await delay(remaining < interval ? remaining : interval, ct).ConfigureAwait(false);
            if (clock.GetUtcNow() >= deadline) throw new AuthenticationException(AuthenticationErrorCode.DeviceCodeExpired, "Microsoft 授权码已过期。", AuthenticationStage.WaitingForAuthorization);
            progress?.Report(new(AuthenticationStage.MicrosoftToken));
            using var token = await SendAsync(Form("token", ("grant_type", "urn:ietf:params:oauth:grant-type:device_code"),
                ("client_id", options.ClientId), ("device_code", deviceCode)), AuthenticationStage.MicrosoftToken, options.RequestTimeout, ct).ConfigureAwait(false);
            if (!token.Success && token.Error == "authorization_pending") continue;
            if (!token.Success && token.Error == "slow_down") { interval += TimeSpan.FromSeconds(5); continue; }
            EnsureSuccess(token);
            var (access, refresh) = OAuthTokens(token.Document.RootElement, null);
            return await CompleteAsync(access, refresh, options.RequestTimeout, progress, ct).ConfigureAwait(false);
        }
    }

    internal async Task<MicrosoftAuthenticationResult> RefreshAsync(MicrosoftCredential credential,
        Func<string, Task> saveRotatedRefresh, IProgress<AuthenticationProgress>? progress, CancellationToken ct)
    {
        var options = new MicrosoftLoginOptions(credential.ClientId);
        options.Validate();
        progress?.Report(new(AuthenticationStage.MicrosoftToken));
        using var token = await SendAsync(Form("token", ("grant_type", "refresh_token"), ("client_id", credential.ClientId),
            ("scope", Scope), ("refresh_token", credential.RefreshToken)), AuthenticationStage.MicrosoftToken, options.RequestTimeout, ct).ConfigureAwait(false);
        EnsureSuccess(token);
        var (access, refresh) = OAuthTokens(token.Document.RootElement, credential.RefreshToken);
        // Persist rotation before downstream services can fail; cancellation must not discard a newly issued refresh token.
        await saveRotatedRefresh(refresh).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        return await CompleteAsync(access, refresh, options.RequestTimeout, progress, ct).ConfigureAwait(false);
    }

    private async Task<MicrosoftAuthenticationResult> CompleteAsync(string microsoftAccess, string refresh, TimeSpan timeout,
        IProgress<AuthenticationProgress>? progress, CancellationToken ct)
    {
        progress?.Report(new(AuthenticationStage.XboxUserToken));
        using var xbox = await SendAsync(Json("https://user.auth.xboxlive.com/user/authenticate", new
        {
            Properties = new { AuthMethod = "RPS", SiteName = "user.auth.xboxlive.com", RpsTicket = "d=" + microsoftAccess },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        }), AuthenticationStage.XboxUserToken, timeout, ct).ConfigureAwait(false);
        EnsureSuccess(xbox);
        var userToken = Text(xbox.Document.RootElement, "Token");
        var userHash = UserClaim(xbox.Document.RootElement, "uhs");
        progress?.Report(new(AuthenticationStage.XstsToken));
        using var xsts = await SendAsync(Json("https://xsts.auth.xboxlive.com/xsts/authorize", new
        {
            Properties = new { SandboxId = "RETAIL", UserTokens = new[] { userToken } },
            RelyingParty = "rp://api.minecraftservices.com/",
            TokenType = "JWT"
        }), AuthenticationStage.XstsToken, timeout, ct).ConfigureAwait(false);
        EnsureSuccess(xsts);
        var xstsHash = UserClaim(xsts.Document.RootElement, "uhs");
        if (xstsHash != userHash) throw new AuthenticationException(AuthenticationErrorCode.IdentityMismatch, "Xbox 与 XSTS 身份不一致。", AuthenticationStage.XstsToken);
        var xstsToken = Text(xsts.Document.RootElement, "Token");
        string? xuid = null;
        var claims = xsts.Document.RootElement.GetProperty("DisplayClaims").GetProperty("xui")[0];
        if (claims.TryGetProperty("xid", out var xid))
        {
            xuid = xid.GetString();
            if (string.IsNullOrEmpty(xuid) || xuid.Length > 32 || !xuid.All(char.IsAsciiDigit)) throw Invalid(AuthenticationStage.XstsToken);
        }
        progress?.Report(new(AuthenticationStage.MinecraftToken));
        using var minecraft = await SendAsync(Json("https://api.minecraftservices.com/authentication/login_with_xbox", new
        { identityToken = $"XBL3.0 x={xstsHash};{xstsToken}" }), AuthenticationStage.MinecraftToken, timeout, ct).ConfigureAwait(false);
        EnsureSuccess(minecraft);
        var access = Text(minecraft.Document.RootElement, "access_token");
        Bearer(minecraft.Document.RootElement, AuthenticationStage.MinecraftToken);
        var expiresAt = clock.GetUtcNow().AddSeconds(Seconds(minecraft.Document.RootElement, "expires_in", 172800));
        progress?.Report(new(AuthenticationStage.Entitlements));
        using var entitlements = await SendAsync(Get("https://api.minecraftservices.com/entitlements/license?requestId=" + Guid.NewGuid().ToString("D"), access),
            AuthenticationStage.Entitlements, timeout, ct).ConfigureAwait(false);
        EnsureSuccess(entitlements);
        var items = entitlements.Document.RootElement.GetProperty("items");
        if (items.ValueKind != JsonValueKind.Array) throw Invalid(AuthenticationStage.Entitlements);
        if (!items.EnumerateArray().Any(item => item.GetProperty("name").GetString() is "game_minecraft" or "product_minecraft"))
            throw new AuthenticationException(AuthenticationErrorCode.GameNotOwned, "未获得 Minecraft Java 版游戏访问资格。", AuthenticationStage.Entitlements);
        progress?.Report(new(AuthenticationStage.Profile));
        using var profile = await SendAsync(Get("https://api.minecraftservices.com/minecraft/profile", access), AuthenticationStage.Profile, timeout, ct).ConfigureAwait(false);
        EnsureSuccess(profile);
        var id = Text(profile.Document.RootElement, "id", 32);
        var name = Text(profile.Document.RootElement, "name", 16);
        if (id.Length != 32 || !Guid.TryParseExact(id, "N", out var uuid) || uuid == Guid.Empty || !OfflineIdentity.ValidName(name)) throw Invalid(AuthenticationStage.Profile);
        if (expiresAt <= clock.GetUtcNow()) throw new AuthenticationException(AuthenticationErrorCode.ReauthenticationRequired, "Minecraft 访问令牌已过期，请重新认证。", AuthenticationStage.Profile);
        return new(uuid, name, access, expiresAt, refresh, xuid);
    }

    private async Task<AuthResponse> SendAsync(HttpRequestMessage request, AuthenticationStage stage, TimeSpan timeout, CancellationToken ct)
    {
        using (request)
        using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct))
        {
            deadline.CancelAfter(timeout);
            try
            {
                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
                if ((int)response.StatusCode is >= 300 and < 400 || response.RequestMessage?.RequestUri is Uri final && final != request.RequestUri)
                    throw Invalid(stage);
                if (response.Content.Headers.ContentLength > 1024 * 1024) throw Invalid(stage);
                await using var input = await response.Content.ReadAsStreamAsync(deadline.Token).ConfigureAwait(false);
                using var output = new MemoryStream();
                var buffer = new byte[8192];
                int count;
                while ((count = await input.ReadAsync(buffer, deadline.Token).ConfigureAwait(false)) > 0)
                {
                    if (output.Length + count > 1024 * 1024) throw Invalid(stage);
                    output.Write(buffer, 0, count);
                }
                JsonDocument document;
                try { document = JsonDocument.Parse(output.GetBuffer().AsMemory(0, (int)output.Length), new JsonDocumentOptions { MaxDepth = 16 }); }
                catch (JsonException) when (!response.IsSuccessStatusCode) { document = JsonDocument.Parse("{}"); }
                if (document.RootElement.ValueKind != JsonValueKind.Object) { document.Dispose(); throw Invalid(stage); }
                return new(document, response.StatusCode, stage);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            { throw new AuthenticationException(AuthenticationErrorCode.RequestTimeout, "认证请求超时。", stage); }
            catch (Exception e) when (e is HttpRequestException or IOException)
            { throw new AuthenticationException(AuthenticationErrorCode.NetworkError, "认证服务连接失败。", stage); }
            catch (JsonException) { throw Invalid(stage); }
        }
    }

    internal static async Task<T> ParseSafelyAsync<T>(Func<Task<T>> operation)
    {
        try { return await operation().ConfigureAwait(false); }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentOutOfRangeException)
        { throw Invalid(null); }
    }

    private static (string Access, string Refresh) OAuthTokens(JsonElement root, string? oldRefresh)
    {
        Bearer(root, AuthenticationStage.MicrosoftToken);
        _ = Seconds(root, "expires_in", 172800);
        var refresh = root.TryGetProperty("refresh_token", out _) ? Text(root, "refresh_token") : oldRefresh ?? throw Invalid(AuthenticationStage.MicrosoftToken);
        return (Text(root, "access_token"), refresh);
    }
    private static void Bearer(JsonElement root, AuthenticationStage stage)
    { if (!Text(root, "token_type", 32).Equals("Bearer", StringComparison.OrdinalIgnoreCase)) throw Invalid(stage); }
    private static string UserClaim(JsonElement root, string name)
    {
        var items = root.GetProperty("DisplayClaims").GetProperty("xui");
        if (items.GetArrayLength() != 1) throw Invalid(AuthenticationStage.XstsToken);
        return Text(items[0], name, 256);
    }
    private static string Text(JsonElement root, string key, int limit = 32768)
    {
        var value = root.GetProperty(key).GetString();
        return string.IsNullOrWhiteSpace(value) || value.Length > limit || value.Any(char.IsControl) ? throw Invalid(null) : value;
    }
    private static int Seconds(JsonElement root, string key, int max)
    {
        var value = root.GetProperty(key).GetInt32();
        return value <= 0 || value > max ? throw Invalid(null) : value;
    }
    private static HttpRequestMessage Form(string path, params (string Key, string Value)[] values) =>
        new(HttpMethod.Post, Authority + path) { Content = new FormUrlEncodedContent(values.Select(v => new KeyValuePair<string, string>(v.Key, v.Value))) };
    private static HttpRequestMessage Json<T>(string uri, T payload)
    {
        // Xbox contract member names are case-sensitive; JsonContent's Web defaults would camel-case them.
        var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = JsonContent.Create(payload, options: new JsonSerializerOptions()) };
        if (uri.Contains("xboxlive.com", StringComparison.Ordinal)) request.Headers.Add("x-xbl-contract-version", "1");
        return request;
    }
    private static HttpRequestMessage Get(string uri, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static void EnsureSuccess(AuthResponse response)
    {
        if (response.Success) return;
        var code = response.Error switch
        {
            "authorization_declined" or "access_denied" => AuthenticationErrorCode.AuthorizationDeclined,
            "expired_token" => AuthenticationErrorCode.DeviceCodeExpired,
            "invalid_grant" => AuthenticationErrorCode.ReauthenticationRequired,
            "invalid_client" or "unauthorized_client" or "invalid_scope" => AuthenticationErrorCode.ClientRejected,
            _ => response.Stage switch
            {
                AuthenticationStage.DeviceAuthorization or AuthenticationStage.MicrosoftToken => AuthenticationErrorCode.MicrosoftRejected,
                AuthenticationStage.XboxUserToken => AuthenticationErrorCode.XboxRejected,
                AuthenticationStage.XstsToken => AuthenticationErrorCode.XstsRejected,
                AuthenticationStage.Profile when response.Status == HttpStatusCode.NotFound => AuthenticationErrorCode.ProfileMissing,
                _ => AuthenticationErrorCode.MinecraftRejected
            }
        };
        long? xboxError = response.Document.RootElement.TryGetProperty("XErr", out var number) && number.TryGetInt64(out var value) ? value : null;
        throw new AuthenticationException(code, "认证服务拒绝了请求，请根据阶段、状态码或 Xbox 错误码处理。", response.Stage, response.Status, xboxError);
    }
    private static AuthenticationException Invalid(AuthenticationStage? stage) => new(AuthenticationErrorCode.InvalidResponse, "认证服务响应格式无效。", stage);

    private sealed class AuthResponse(JsonDocument document, HttpStatusCode status, AuthenticationStage stage) : IDisposable
    {
        internal JsonDocument Document => document;
        internal HttpStatusCode Status => status;
        internal AuthenticationStage Stage => stage;
        internal bool Success => (int)status is >= 200 and < 300;
        internal string? Error => document.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String ? error.GetString() : null;
        public void Dispose() => document.Dispose();
    }
}
