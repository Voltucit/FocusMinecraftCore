namespace FMCore.Java;

/// <summary>Convenient Java discovery/probing APIs. Use JavaManager for diagnostics and selection policy.</summary>
public static class JavaUtil
{
    /// <summary>Finds Java installations in JAVA_HOME, PATH and bounded common directories, then actually probes each runtime.</summary>
    public static async Task<IReadOnlyList<JavaRuntime>> GetJavasAsync(JavaDetectionOptions? options = null,
        IProgress<JavaDetectionProgress>? progress = null, CancellationToken cancellationToken = default) =>
        (await new JavaManager(options).DetectAsync(progress, cancellationToken).ConfigureAwait(false)).Runtimes;

    /// <summary>Actually executes the specified Java to obtain version and architecture, with timeout and output limits.</summary>
    public static Task<JavaRuntime> GetJavaInfoAsync(string javaPath, CancellationToken cancellationToken = default) =>
        new JavaManager().ProbeAsync(javaPath, cancellationToken);

    /// <summary>Blocking convenience API. GUI applications should use GetJavasAsync to keep the UI responsive.</summary>
    public static IReadOnlyList<JavaRuntime> GetJavas(JavaDetectionOptions? options = null) =>
        Task.Run(() => GetJavasAsync(options)).GetAwaiter().GetResult();

    /// <summary>Blocking convenience API. GUI applications should use GetJavaInfoAsync.</summary>
    public static JavaRuntime GetJavaInfo(string javaPath) => Task.Run(() => GetJavaInfoAsync(javaPath)).GetAwaiter().GetResult();
}
