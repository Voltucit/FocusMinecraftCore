namespace FMCore.Java;

internal static class JavaDiscovery
{
    internal static StringComparer PathComparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    internal static IReadOnlyList<string> CommonDirectories()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var roots = new List<string>();
        void Under(string parent, params string[] children)
        {
            if (string.IsNullOrWhiteSpace(parent)) return;
            roots.AddRange(children.Select(child => Path.Combine(parent, child)));
        }
        Under(home, ".jdks", Path.Combine(".sdkman", "candidates", "java"), Path.Combine(".jabba", "jdk"));
        if (OperatingSystem.IsWindows())
        {
            foreach (var programFiles in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetEnvironmentVariable("ProgramW6432") ?? "" })
                Under(programFiles, "Java", "Eclipse Adoptium", "Eclipse Foundation", "Microsoft", "Amazon Corretto", "Zulu", "BellSoft", "Semeru");
            Under(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Path.Combine("Programs", "Eclipse Adoptium"),
                Path.Combine("Programs", "Microsoft"), Path.Combine("Programs", "Java"));
        }
        else if (OperatingSystem.IsMacOS())
        {
            roots.AddRange(["/Library/Java/JavaVirtualMachines", "/System/Library/Java/JavaVirtualMachines", "/opt/homebrew/opt", "/usr/local/opt"]);
            Under(home, Path.Combine("Library", "Java", "JavaVirtualMachines"));
        }
        else if (OperatingSystem.IsLinux()) roots.AddRange(["/usr/lib/jvm", "/usr/lib64/jvm", "/usr/java", "/opt/java", "/opt/jdk"]);
        return roots.Distinct(PathComparer).ToArray();
    }

    internal static (List<string> Paths, List<JavaDetectionIssue> Issues, bool Truncated) Collect(JavaDetectionOptions options, CancellationToken ct)
    {
        var paths = new List<string>();
        var issues = new List<JavaDetectionIssue>();
        var seen = new HashSet<string>(PathComparer);
        var truncated = false;
        var name = OperatingSystem.IsWindows() ? "java.exe" : "java";

        void Add(string path, bool reportMissing = false)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (paths.Count >= options.MaxCandidates) { truncated = true; return; }
                var executable = JavaProbe.ResolveExecutable(path);
                if (seen.Add(executable)) paths.Add(executable);
            }
            catch (JavaException e)
            {
                if (e.Code != JavaErrorCode.NotFound || reportMissing)
                    issues.Add(new(e.Code == JavaErrorCode.InvalidPath ? null : path, e.Code));
            }
        }

        string? DirectoryPath(string path)
        {
            path = path.Trim();
            if (path.Length >= 2 && path[0] == '"' && path[^1] == '"') path = path[1..^1];
            try
            {
                if (!Path.IsPathFullyQualified(path) || path.Any(char.IsControl) || path.Contains('"'))
                    throw new ArgumentException();
                return Path.GetFullPath(path);
            }
            catch (Exception e) when (e is ArgumentException or NotSupportedException or PathTooLongException)
            { issues.Add(new(null, JavaErrorCode.InvalidPath)); return null; }
        }

        if (!string.IsNullOrWhiteSpace(options.JavaHome) && DirectoryPath(options.JavaHome) is string javaHome) Add(javaHome, true);
        foreach (var entry in options.PathDirectories)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(entry)) continue;
            if (DirectoryPath(entry) is string directory) Add(Path.Combine(directory, name));
        }
        foreach (var entry in options.SearchDirectories)
        {
            ct.ThrowIfCancellationRequested();
            if (paths.Count >= options.MaxCandidates) { truncated = true; break; }
            if (DirectoryPath(entry) is not string root) continue;
            try
            {
                if (!Directory.Exists(root)) continue;
                Add(root);
                // Read one directory level only, including non-Java entries in the per-root budget.
                var read = 0;
                foreach (var child in Directory.EnumerateDirectories(root))
                {
                    ct.ThrowIfCancellationRequested();
                    if (++read > options.MaxCandidates || paths.Count >= options.MaxCandidates) { truncated = true; break; }
                    Add(child);
                }
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            { issues.Add(new(root, JavaErrorCode.IoError)); }
        }
        return (paths, issues, truncated);
    }
}
