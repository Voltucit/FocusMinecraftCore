using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using FMCore.Authentication;

namespace FMCore.Tests.Fixtures;

internal sealed record AuthRequest(string Host, string Path, string Method, string Body, string? Authorization);

internal sealed class AuthenticationTestContext : IDisposable
{
    private readonly DownloadTestContext directory = new();
    private readonly HttpClient http;
    internal string Root => directory.Root;
    internal AccountManager Manager { get; }
    internal MemoryTokenStore Tokens { get; }
    internal AuthClock Clock { get; } = new();
    internal ConcurrentQueue<AuthRequest> Requests { get; } = new();
    internal Queue<string> PollErrors { get; } = new();
    internal Func<AuthRequest, CancellationToken, Task<HttpResponseMessage?>>? Override { get; set; }
    internal Action<TimeSpan>? OnDelay { get; set; }
    internal List<TimeSpan> Delays { get; } = [];
    internal int OAuthCalls { get; private set; }
    internal string ProfileUuid { get; set; } = "123456781234123412341234567890ab";
    internal string ProfileName { get; set; } = "MockPlayer";
    internal DeviceCodeInfo? DisplayedCode { get; private set; }
    internal static MicrosoftLoginOptions Options => new("11111111-2222-3333-4444-555555555555");
    internal static CancellationToken Ct => TestContext.Current.CancellationToken;

    internal AuthenticationTestContext(MemoryTokenStore? tokenStore = null, bool useDefaultStore = false)
    {
        Tokens = tokenStore ?? new();
        http = new(new DelegateHandler(async (request, ct) =>
        {
            var recorded = new AuthRequest(request.RequestUri!.Host, request.RequestUri.AbsolutePath, request.Method.Method,
                request.Content == null ? "" : await request.Content.ReadAsStringAsync(ct), request.Headers.Authorization?.ToString());
            Requests.Enqueue(recorded);
            if (Override != null && await Override(recorded, ct) is HttpResponseMessage custom) return custom;
            return Respond(recorded);
        }));
        Manager = new(Root, useDefaultStore ? null : Tokens, http, Clock, (interval, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            Delays.Add(interval);
            Clock.Advance(interval);
            OnDelay?.Invoke(interval);
            return Task.CompletedTask;
        });
    }

    internal Task<LaunchIdentity> LoginAsync(IProgress<AuthenticationProgress>? progress = null, CancellationToken? ct = null) =>
        Manager.LoginMicrosoftAsync(Options, (code, token) => { token.ThrowIfCancellationRequested(); DisplayedCode = code; return Task.CompletedTask; }, progress, ct ?? Ct);

    internal HttpResponseMessage Respond(AuthRequest request)
    {
        switch (request.Path)
        {
            case "/consumers/oauth2/v2.0/devicecode":
                return Json(new { device_code = "fixture-device-secret", user_code = "MOCK-CODE", verification_uri = "https://microsoft.com/devicelogin", expires_in = 900, interval = 1 });
            case "/consumers/oauth2/v2.0/token":
                OAuthCalls++;
                if (request.Body.Contains("device_code=", StringComparison.Ordinal) && PollErrors.TryDequeue(out var error))
                    return Json(new { error, error_description = "fixture-sensitive-error-description" }, HttpStatusCode.BadRequest);
                return Json(new { token_type = "Bearer", access_token = "fixture-microsoft-access", refresh_token = "fixture-refresh-" + OAuthCalls, expires_in = 3600 });
            case "/user/authenticate":
                return Json(new { Token = "fixture-xbox-token", DisplayClaims = new { xui = new[] { new { uhs = "fixture-uhs" } } } });
            case "/xsts/authorize":
                return Json(new { Token = "fixture-xsts-token", DisplayClaims = new { xui = new[] { new { uhs = "fixture-uhs", xid = "1234567890123" } } } });
            case "/authentication/login_with_xbox":
                return Json(new { access_token = "fixture-minecraft-access", token_type = "Bearer", expires_in = 3600 });
            case "/entitlements/license":
                return Json(new { items = new[] { new { name = "game_minecraft", signature = "fixture-signature" } } });
            case "/minecraft/profile":
                return Json(new { id = ProfileUuid, name = ProfileName, skins = Array.Empty<object>(), capes = Array.Empty<object>() });
            default: throw new InvalidOperationException("Unexpected mock endpoint.");
        }
    }

    internal static HttpResponseMessage Json<T>(T body, HttpStatusCode status = HttpStatusCode.OK) => new(status)
    { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
    internal string At(string path) => directory.At(path);
    internal AccountManager Reopen() => new(Root, Tokens, http, Clock, (interval, ct) =>
    { ct.ThrowIfCancellationRequested(); Clock.Advance(interval); return Task.CompletedTask; });
    internal void AssertNoTemporaryFiles() => Assert.Empty(Directory.EnumerateFiles(Root, "*.tmp", SearchOption.AllDirectories));
    public void Dispose() { Manager.Dispose(); http.Dispose(); directory.Dispose(); }
}

internal sealed class AuthClock : TimeProvider
{
    private DateTimeOffset current = new(2026, 10, 21, 0, 0, 0, TimeSpan.Zero);
    public override DateTimeOffset GetUtcNow() => current;
    internal void Advance(TimeSpan duration) => current += duration;
}

internal sealed class MemoryTokenStore : IAccountTokenStore
{
    internal ConcurrentDictionary<Guid, byte[]> Values { get; } = new();
    internal bool FailWrite { get; set; }
    internal bool FailDelete { get; set; }
    internal Action? OnWrite { get; set; }
    public Task<byte[]?> ReadAsync(Guid accountId, CancellationToken cancellationToken = default)
    { cancellationToken.ThrowIfCancellationRequested(); return Task.FromResult(Values.TryGetValue(accountId, out var bytes) ? bytes.ToArray() : null); }
    public Task WriteAsync(Guid accountId, ReadOnlyMemory<byte> payload, CancellationToken cancellationToken = default)
    {
        OnWrite?.Invoke();
        cancellationToken.ThrowIfCancellationRequested();
        if (FailWrite) throw new IOException("fixture-refresh-secret-storage-error");
        Values[accountId] = payload.ToArray();
        return Task.CompletedTask;
    }
    public Task DeleteAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (FailDelete) throw new IOException("fixture-refresh-secret-storage-error");
        Values.TryRemove(accountId, out _);
        return Task.CompletedTask;
    }
}
