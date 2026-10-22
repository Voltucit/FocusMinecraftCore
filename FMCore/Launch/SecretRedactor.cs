using FMCore.Authentication;
using FMCore.Logging;

namespace FMCore.Launch;

internal sealed class SecretRedactor(LaunchIdentity identity)
{
    private readonly SensitiveDataFilter filter = new(identity.IsAuthenticated ? [identity.LegacySession, identity.AccessToken] : []);
    internal string Redact(string text) => filter.Redact(text);
}
