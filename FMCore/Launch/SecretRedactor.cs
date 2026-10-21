using FMCore.Authentication;

namespace FMCore.Launch;

internal sealed class SecretRedactor
{
    private readonly string[] secrets;
    internal SecretRedactor(LaunchIdentity identity)
    {
        secrets = identity.IsAuthenticated
            ? new[] { identity.LegacySession, identity.AccessToken, Uri.EscapeDataString(identity.AccessToken), Uri.EscapeDataString(identity.LegacySession) }
                .Where(s => s.Length != 0).Distinct(StringComparer.Ordinal).OrderByDescending(s => s.Length).ToArray()
            : [];
    }
    internal string Redact(string text)
    {
        foreach (var secret in secrets) text = text.Replace(secret, "[REDACTED]", StringComparison.Ordinal);
        return text;
    }
}
