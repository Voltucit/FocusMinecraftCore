using System.Runtime.Versioning;
using FMCore.Installer;
using Microsoft.Win32;

namespace FMCore.Java;

internal static class JavaDiscovery
{
    internal static IReadOnlyList<string> FindCandidates(IEnumerable<string>? searchPaths)
    {
        List<string> candidates = [];
        string? javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
            candidates.Add(Path.Combine(javaHome.Trim().Trim('"'), "bin", "java.exe"));
        foreach (string entry in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            string directory = entry.Trim().Trim('"');
            if (directory.Length > 0)
                candidates.Add(Path.Combine(directory, "java.exe"));
        }

        List<string> directories = [Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".jdks")];
        if (OperatingSystem.IsWindows())
            directories.AddRange(FindRegistryJavaHomes());
        if (searchPaths is not null)
            directories.AddRange(searchPaths.Select(PathSafety.RequireDirectoryPath));

        foreach (string directory in directories)
        {
            if (!Directory.Exists(directory))
                continue;
            candidates.Add(Path.Combine(directory, "java.exe"));
            candidates.Add(Path.Combine(directory, "bin", "java.exe"));
            foreach (string child in Directory.EnumerateDirectories(directory).Order(PathSafety.Comparer))
                candidates.Add(Path.Combine(child, "bin", "java.exe"));
        }
        return candidates.Where(File.Exists).Select(PathSafety.Resolve).Distinct(PathSafety.Comparer).ToArray();
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<string> FindRegistryJavaHomes()
    {
        string[] names = [@"SOFTWARE\JavaSoft\Java Runtime Environment", @"SOFTWARE\JavaSoft\Java Development Kit",
            @"SOFTWARE\JavaSoft\JRE", @"SOFTWARE\JavaSoft\JDK"];
        HashSet<string> homes = new(PathSafety.Comparer);
        foreach (RegistryHive hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
        foreach (RegistryView view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view);
            foreach (string name in names)
            {
                using RegistryKey? root = baseKey.OpenSubKey(name, writable: false);
                if (root is null)
                    continue;
                foreach (string version in root.GetSubKeyNames())
                {
                    using RegistryKey? key = root.OpenSubKey(version, writable: false);
                    if (key?.GetValue("JavaHome") is string value && !string.IsNullOrWhiteSpace(value))
                        homes.Add(PathSafety.Resolve(Environment.ExpandEnvironmentVariables(value.Trim().Trim('"'))));
                }
            }
        }
        return homes;
    }
}
