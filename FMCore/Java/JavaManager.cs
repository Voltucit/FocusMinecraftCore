using FMCore.Versions;

namespace FMCore.Java;

/// <summary>Bounded Java detection, real process probing and conservative version/architecture selection.</summary>
public sealed class JavaManager
{
    private readonly JavaDetectionOptions options;
    private readonly Func<string, CancellationToken, Task<JavaRuntime>> probe;

    /// <summary>Uses JAVA_HOME, PATH and common installation roots by default, with bounded per-runtime probes.</summary>
    public JavaManager(JavaDetectionOptions? options = null) : this(options, null) { }

    internal JavaManager(JavaDetectionOptions? options, Func<string, CancellationToken, Task<JavaRuntime>>? probe)
    {
        options ??= new();
        if (options.ProbeTimeout <= TimeSpan.Zero || options.ProbeTimeout > TimeSpan.FromMinutes(1))
            throw new ArgumentOutOfRangeException(nameof(options.ProbeTimeout));
        if (options.MaxOutputCharacters is < 1024 or > 1024 * 1024) throw new ArgumentOutOfRangeException(nameof(options.MaxOutputCharacters));
        if (options.MaxCandidates is < 1 or > 1024) throw new ArgumentOutOfRangeException(nameof(options.MaxCandidates));
        ArgumentNullException.ThrowIfNull(options.PathDirectories);
        ArgumentNullException.ThrowIfNull(options.SearchDirectories);
        this.options = options with { PathDirectories = options.PathDirectories.ToArray(), SearchDirectories = options.SearchDirectories.ToArray() };
        this.probe = probe ?? ((path, ct) => JavaProbe.ProbeAsync(path, this.options, ct));
    }

    /// <summary>Accepts an absolute java/java.exe path, Windows javaw.exe (probes its java.exe sibling), Java home or bin directory.</summary>
    public Task<JavaRuntime> ProbeAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return probe(path, cancellationToken);
    }

    /// <summary>Discovers and probes runtimes, retaining failed candidates as structured issues and reporting progress.</summary>
    public async Task<JavaDetectionResult> DetectAsync(IProgress<JavaDetectionProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var (paths, issues, truncated) = JavaDiscovery.Collect(options, cancellationToken);
        var runtimes = new List<JavaRuntime>();
        var identities = new HashSet<string>(JavaDiscovery.PathComparer);
        progress?.Report(new(0, paths.Count, null, null));
        for (var i = 0; i < paths.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            JavaErrorCode? error = null;
            try
            {
                var runtime = await probe(paths[i], cancellationToken).ConfigureAwait(false);
                // JAVA_HOME, PATH shims and directory links can point at the same installation.
                if (identities.Add(runtime.HomeDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + "\n" +
                    runtime.Architecture + "\n" + runtime.Version.Text)) runtimes.Add(runtime);
            }
            catch (JavaException e) { error = e.Code; issues.Add(new(paths[i], e.Code, e.ExitCode)); }
            progress?.Report(new(i + 1, paths.Count, paths[i], error));
        }
        return new(runtimes.AsReadOnly(), issues.AsReadOnly(), truncated);
    }

    /// <summary>Selects a compatible runtime from existing probe results using resolved metadata; newer major versions do not substitute for required ones.</summary>
    public JavaRuntime Select(IEnumerable<JavaRuntime> runtimes, VersionMetadata version, JavaSelectionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(runtimes);
        ArgumentNullException.ThrowIfNull(version);
        options ??= new();
        options.Validate();
        if (version.Java == null && options.FallbackMajorVersion == null) throw JavaCompatibility.Failure(JavaErrorCode.RequirementMissing);
        return runtimes.Where(runtime => JavaCompatibility.Check(runtime, version, options).IsCompatible)
            .OrderByDescending(runtime => runtime.Version)
            .FirstOrDefault() ?? throw JavaCompatibility.Failure(JavaErrorCode.NoCompatibleRuntime);
    }

    /// <summary>An explicit path is authoritative: invalid or incompatible custom Java fails without silently switching installations.</summary>
    public async Task<JavaRuntime> SelectAsync(VersionMetadata version, string? customPath = null, JavaSelectionOptions? options = null,
        IProgress<JavaDetectionProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);
        options ??= new();
        options.Validate();
        cancellationToken.ThrowIfCancellationRequested();
        if (version.Java == null && options.FallbackMajorVersion == null) throw JavaCompatibility.Failure(JavaErrorCode.RequirementMissing);
        if (customPath != null)
        {
            var runtime = await ProbeAsync(customPath, cancellationToken).ConfigureAwait(false);
            var compatibility = JavaCompatibility.Check(runtime, version, options);
            if (compatibility.ErrorCode is JavaErrorCode error) throw JavaCompatibility.Failure(error);
            return runtime;
        }
        var detected = await DetectAsync(progress, cancellationToken).ConfigureAwait(false);
        return Select(detected.Runtimes, version, options);
    }
}
