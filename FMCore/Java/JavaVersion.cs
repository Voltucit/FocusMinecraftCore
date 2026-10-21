using System.Globalization;
using System.Text.RegularExpressions;

namespace FMCore.Java;

public sealed record JavaVersion : IComparable<JavaVersion>
{
    public string Text { get; }
    public int Major { get; }
    public int Minor { get; }
    public int Security { get; }
    public int Patch { get; }
    public int Build { get; }
    public bool IsEarlyAccess { get; }

    private JavaVersion(string text, int major, int minor, int security, int patch, int build, bool earlyAccess)
    { Text = text; Major = major; Minor = minor; Security = security; Patch = patch; Build = build; IsEarlyAccess = earlyAccess; }

    public static JavaVersion Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 200) throw Invalid();
        var legacy = text.StartsWith("1.", StringComparison.Ordinal);
        var pattern = legacy
            ? @"\A1\.(?<major>[1-9][0-9]*)\.0(?:_(?<security>[0-9]+))?(?:-b(?<build>[0-9]+))?(?:[-+][a-zA-Z0-9._+-]+)?\z"
            : @"\A(?<major>[1-9][0-9]*)(?:\.(?<minor>[0-9]+))?(?:\.(?<security>[0-9]+))?(?:\.(?<patch>[0-9]+))?(?:-(?<pre>[a-zA-Z0-9.-]+))?(?:\+(?<build>[0-9]+)(?:-[a-zA-Z0-9._+-]+)?)?\z";
        var match = Regex.Match(text, pattern, RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        if (!match.Success) throw Invalid();
        int Number(string name)
        {
            var value = match.Groups[name].Value;
            return value.Length == 0 ? 0 : int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                ? number : throw Invalid();
        }
        var major = Number("major");
        if (major > 999) throw Invalid();
        return new(text, major, Number("minor"), Number("security"), Number("patch"), Number("build"),
            match.Groups["pre"].Success || legacy && text.Contains("-ea", StringComparison.OrdinalIgnoreCase));
    }

    public int CompareTo(JavaVersion? other)
    {
        if (other == null) return 1;
        var result = (Major, Minor, Security, Patch).CompareTo((other.Major, other.Minor, other.Security, other.Patch));
        if (result != 0) return result;
        result = other.IsEarlyAccess.CompareTo(IsEarlyAccess);
        return result != 0 ? result : Build.CompareTo(other.Build);
    }

    public override string ToString() => Text;
    private static JavaException Invalid() => new(JavaErrorCode.InvalidOutput, "Java 版本格式无法识别。");
}
