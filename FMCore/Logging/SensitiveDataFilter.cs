using System.Text.RegularExpressions;

namespace FMCore.Logging;

internal sealed partial class SensitiveDataFilter
{
    private readonly string[] secrets;
    internal SensitiveDataFilter(IEnumerable<string>? values = null) => secrets = (values ?? [])
        .Where(s => !string.IsNullOrEmpty(s)).SelectMany(s => new[] { s, Uri.EscapeDataString(s), LowerEscapes(Uri.EscapeDataString(s)), System.Net.WebUtility.UrlEncode(s), LowerEscapes(System.Net.WebUtility.UrlEncode(s)) })
        .Distinct(StringComparer.Ordinal).OrderByDescending(s => s.Length).ToArray();

    private static string LowerEscapes(string text) => Regex.Replace(text, "%[0-9A-Fa-f]{2}", match => match.Value.ToLowerInvariant());
    internal string Redact(string text)
    {
        try
        {
            text = Ansi().Replace(text, "");
            foreach (var secret in secrets) text = text.Replace(secret, "[REDACTED]", StringComparison.Ordinal);
            text = Authorization().Replace(text, "$1[REDACTED]");
            text = Credentials().Replace(text, "$1[REDACTED]");
            text = Jwt().Replace(text, "[REDACTED]");
            return text;
        }
        catch (RegexMatchTimeoutException) { return "[log text omitted: filtering limit]"; }
    }

    [GeneratedRegex(@"\x1B\[[0-?]*[ -/]*[@-~]", RegexOptions.CultureInvariant, 100)]
    private static partial Regex Ansi();
    [GeneratedRegex(@"(?i)(\b(?:Bearer\s+|XBL3\.0\s+x=))[^\s""'<>]+", RegexOptions.CultureInvariant, 100)]
    private static partial Regex Authorization();
    [GeneratedRegex(@"(?i)((?:--)?\b(?:access[_-]?token|refresh[_-]?token|id[_-]?token|token|authorization|api[_-]?key|client[_-]?secret|password|passwd|session)\b[""']?\s*(?:[:=]\s*|\s+))(?:""(?:\\.|[^""\\\r\n])*""?|'[^'\r\n]*'?|[^\s""'&,;<>]+)", RegexOptions.CultureInvariant, 100)]
    private static partial Regex Credentials();
    [GeneratedRegex(@"\beyJ[A-Za-z0-9_-]{8,}\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+", RegexOptions.CultureInvariant, 100)]
    private static partial Regex Jwt();
}
