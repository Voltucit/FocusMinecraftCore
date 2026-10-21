using FMCore.Java;

namespace FMCore.Tests.Fixtures;

internal static class JavaTestContext
{
    internal static CancellationToken Ct => TestContext.Current.CancellationToken;
    internal static string ExecutableName => OperatingSystem.IsWindows() ? "java.exe" : "java";
    internal static JavaDetectionOptions EmptyOptions => new() { JavaHome = null, PathDirectories = [], SearchDirectories = [] };
    internal static JavaRuntime Runtime(string version = "21.0.6", JavaArchitecture arch = JavaArchitecture.X64,
        string? home = null) => new(Path.Combine(home ?? Path.GetTempPath(), "bin", ExecutableName), home ?? Path.GetTempPath(),
            JavaVersion.Parse(version), arch, "Fixture vendor");
    internal static string Fixture(string name) => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Java", name));
    internal static string Candidate(DownloadTestContext directory, string relativeHome)
    {
        var path = Path.GetFullPath(directory.At(Path.Combine(relativeHome, "bin", ExecutableName)));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "Non-executable discovery fixture. The probe is substituted in discovery unit tests only.");
        return path;
    }
}
