using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using FMCore.Assets;
using FMCore.Authentication;
using FMCore.Downloader;
using FMCore.Installer;
using FMCore.Java;
using FMCore.Utilities;
using FMCore.Versions;

namespace FMCore.Launch;

/// <summary>Prepares and manages one Minecraft process at a time. Reusable after exit; dispose asynchronously to stop an owned game.</summary>
public sealed class MinecraftLauncher : IAsyncDisposable
{
    private readonly LaunchConfig config;
    private readonly DownloadManager downloads;
    private readonly JavaManager javas;
    private readonly Func<ProcessStartInfo, ProcessStartInfo>? transform;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly object sync = new();
    private volatile LaunchState state;
    private CancellationTokenSource? preparing;
    private Session? active;
    private Task<GameExitResult>? completion;
    private int? exitCode;
    private int disposed;

    /// <summary>State updates; handlers run on the executing/background thread and must not block on launcher operations.</summary>
    public event EventHandler<LaunchStateChangedEventArgs>? StateChanged;
    /// <summary>Real-time, token-redacted standard output. The SDK does not persist raw output.</summary>
    public event EventHandler<GameLogEventArgs>? OutputReceived;
    /// <summary>Real-time, token-redacted standard error.</summary>
    public event EventHandler<GameLogEventArgs>? ErrorReceived;
    /// <summary>Fires after process exit, stream drainage and release of instance resources.</summary>
    public event EventHandler<GameExitedEventArgs>? Exited;
    /// <summary>Current lifecycle state; Running indicates process creation, not main-menu readiness.</summary>
    public LaunchState State => state;
    /// <summary>Current owned process ID, or null when no game is active.</summary>
    public int? ProcessId { get { lock (sync) return active?.Id; } }
    /// <summary>Most recent exit code, reset when a new process starts.</summary>
    public int? ExitCode { get { lock (sync) return exitCode; } }

    /// <summary>Creates a launcher with validated, copied configuration and automatic module wiring; no dependency injection is required.</summary>
    public MinecraftLauncher(LaunchConfig config) : this(config, null, null, null) { }

    internal MinecraftLauncher(LaunchConfig config, DownloadManager? downloads, JavaManager? javas,
        Func<ProcessStartInfo, ProcessStartInfo>? transform = null)
    {
        ArgumentNullException.ThrowIfNull(config);
        this.config = Freeze(config);
        try { this.downloads = downloads ?? new(this.config.GameCore.RootDirectory, this.config.Downloads); }
        catch (DownloadException) { throw new LaunchException(LaunchErrorCode.InvalidConfig, "游戏根目录或下载配置无效。"); }
        if (!GameFiles.PathComparer.Equals(Path.GetFullPath(this.config.GameCore.RootDirectory), this.downloads.RootDirectory))
            throw new LaunchException(LaunchErrorCode.InvalidConfig, "启动器与下载器必须使用相同根目录。");
        try { this.javas = javas ?? new(this.config.Java.Detection); }
        catch (ArgumentException) { throw new LaunchException(LaunchErrorCode.InvalidConfig, "Java 检测配置无效。"); }
        this.transform = transform;
    }

    /// <summary>Resolves metadata/Java/account, installs or verifies files, and starts the process. Cancellation before process creation aborts preparation.</summary>
    /// <returns>Success with a process ID, or a sanitized structured failure. Cancellation throws OperationCanceledException.</returns>
    public async Task<LaunchResult> LaunchAsync(IProgress<LaunchProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        FileStream? lease = null;
        string? privateDirectory = null;
        CancellationTokenSource? linked = null;
        try
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref disposed) != 0, this);
            if (active != null) return new(false, null, new(LaunchErrorCode.AlreadyRunning, "此启动器已有游戏进程。"));
            linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            lock (sync) preparing = linked;
            var ct = linked.Token;
            SetState(LaunchState.Preparing);
            var root = downloads.RootDirectory;
            var game = GameDirectory();
            var gamePath = game.Length == 0 ? root : GameFiles.Resolve(root, game);
            GameFiles.CreateDirectory(gamePath);
            var lockPath = GameFiles.Resolve(root, (game.Length == 0 ? "" : game + "/") + ".fmcore/instance.lock");
            GameFiles.CreateDirectory(Path.GetDirectoryName(lockPath)!);
            try { lease = new(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
            catch (IOException error) when ((error.HResult & 0xffff) is 32 or 33 or 11)
            { throw new LaunchException(LaunchErrorCode.InstanceBusy, "此游戏实例正在被另一个启动任务或进程使用。"); }
            progress?.Report(new(LaunchStage.Metadata));
            var versions = new VersionManager(downloads);
            var policy = config.GameCore.AutoInstall ? config.GameCore.CachePolicy : VersionCachePolicy.CacheOnly;
            var version = await versions.ResolveAsync(config.GameCore.VersionId, policy, ct).ConfigureAwait(false);
            progress?.Report(new(LaunchStage.Java));
            var java = await javas.SelectAsync(version.Metadata, config.Java.JavaPath, config.Java.Selection, cancellationToken: ct).ConfigureAwait(false);
            if (java.Architecture is JavaArchitecture.X86 or JavaArchitecture.Arm32 && config.Java.MaxMemoryMb > 1536)
                throw new LaunchException(LaunchErrorCode.InvalidConfig, "32 位 Java 的最大内存设置不能超过 1536 MiB。");
            progress?.Report(new(LaunchStage.Account));
            var identity = config.Accounts == null ? config.Account : await config.Accounts.GetLaunchIdentityAsync(config.Account?.Account.Id, cancellationToken: ct).ConfigureAwait(false);
            if (identity == null) throw new LaunchException(LaunchErrorCode.AuthenticationError, "请提供账号身份或包含所选账号的 AccountManager。");
            if (config.Account != null && (identity.Account.Kind != config.Account.Account.Kind || identity.Account.Uuid != config.Account.Account.Uuid))
                throw new LaunchException(LaunchErrorCode.AuthenticationError, "启动账号与请求的身份不一致。");
            if (identity.IsAuthenticated && (identity.ExpiresAt == null || identity.ExpiresAt <= DateTimeOffset.UtcNow.AddSeconds(60)))
                throw new LaunchException(LaunchErrorCode.AuthenticationError, "正版启动身份已过期或即将过期，请通过 AccountManager 刷新。");
            var features = new Dictionary<string, bool>(config.Features, StringComparer.Ordinal) { ["has_custom_resolution"] = true };
            privateDirectory = $".fmcore/launch/{Guid.NewGuid():N}";
            var options = new InstallationOptions
            {
                Context = java.GetRuleContext(features),
                CachePolicy = policy,
                GameDirectory = game,
                NativeDirectory = privateDirectory + "/natives"
            };
            var installer = new GameInstaller(downloads, versions);
            var installProgress = new InlineProgress<InstallationProgress>(p => progress?.Report(new(LaunchStage.Installation, p)));
            progress?.Report(new(LaunchStage.Installation));
            InstallationResult installation;
            if (config.GameCore.AutoInstall)
                installation = await installer.InstallAsync(config.GameCore.VersionId, options, installProgress, ct).ConfigureAwait(false);
            else
            {
                var check = await installer.VerifyAsync(config.GameCore.VersionId, options with { VerifyAssetLayouts = false }, installProgress, ct).ConfigureAwait(false);
                if (!check.IsValid) throw new LaunchException(LaunchErrorCode.MissingFiles, "游戏文件缺失或损坏，请修复安装或启用 AutoInstall。");
                var plan = installer.CreatePlan(check.Version, options);
                var nativeDirectory = await new NativeExtractor(root).ExtractAsync(plan.NativeArchives, plan.NativeDirectory, cancellationToken: ct).ConfigureAwait(false);
                var assets = new AssetManager(downloads);
                var index = AssetIndex.Parse(await GameFiles.ReadJsonAsync(GameFiles.Resolve(root, $"assets/indexes/{plan.AssetIndex.Id}.json"), ct).ConfigureAwait(false));
                var layout = await assets.MaterializeAsync(index, plan.AssetIndex.Id, game, cancellationToken: ct).ConfigureAwait(false);
                installation = new(plan, GameFiles.Resolve(root, plan.Client.RelativePath), plan.ClasspathEntries.Select(p => GameFiles.Resolve(root, p)).ToArray(),
                    nativeDirectory, layout, plan.Logging == null ? null : GameFiles.Resolve(root, plan.Logging.RelativePath), 0, 0, 0);
            }
            var compatibility = JavaCompatibility.Check(java, installation.Plan.Version.Metadata, config.Java.Selection);
            if (compatibility.ErrorCode is JavaErrorCode incompatible) throw JavaCompatibility.Failure(incompatible);
            progress?.Report(new(LaunchStage.Arguments));
            var start = LaunchArguments.Build(config, java, installation, identity);
            if (transform != null) start = transform(start); // Internal process fixture seam, never exposed by the SDK.
            ct.ThrowIfCancellationRequested();
            progress?.Report(new(LaunchStage.Process));
            SetState(LaunchState.Starting);
            ct.ThrowIfCancellationRequested();
            var process = new Process { StartInfo = start };
            try
            {
                if (!process.Start()) throw new LaunchException(LaunchErrorCode.ProcessStartFailed, "无法创建游戏进程。");
            }
            catch { process.Dispose(); throw; }
            var session = new Session(process, lease, root, privateDirectory, new(identity));
            lease = null;
            privateDirectory = null;
            lock (sync) { active = session; exitCode = null; completion = session.Completion.Task; }
            SetState(LaunchState.Running);
            _ = Task.Run(() => ObserveAsync(session));
            return new(true, session.Id, null);
        }
        catch (OperationCanceledException) { SetState(LaunchState.Canceled); throw; }
        catch (Exception error) when (error is not ObjectDisposedException)
        {
            var failure = Failure(error);
            SetState(LaunchState.Failed, failure);
            return new(false, null, failure);
        }
        finally
        {
            lock (sync) preparing = null;
            linked?.Dispose();
            lease?.Dispose();
            if (privateDirectory != null) Cleanup(downloads.RootDirectory, privateDirectory);
            gate.Release();
        }
    }

    /// <summary>Cancels preparation or terminates the active process tree, then waits for exit/output drainage. Calling cancellation only stops waiting.</summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (sync) preparing?.Cancel();
        Task<GameExitResult>? waiting = null;
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (active != null)
            {
                var session = active;
                waiting = session.Completion.Task;
                if (!session.Process.HasExited)
                {
                    session.Stopped = true;
                    SetState(LaunchState.Stopping);
                    try { session.Process.Kill(entireProcessTree: true); }
                    catch (InvalidOperationException) when (session.Process.HasExited) { }
                    catch (Exception error) when (error is Win32Exception or InvalidOperationException or NotSupportedException)
                    { throw new LaunchException(LaunchErrorCode.ProcessControlFailed, "无法停止游戏进程。"); }
                }
            }
        }
        finally { gate.Release(); }
        if (waiting != null) await waiting.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Waits for the last started game. Canceling this wait does not stop the game; use StopAsync to terminate it.</summary>
    public Task<GameExitResult> WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        lock (sync) return (completion ?? throw new LaunchException(LaunchErrorCode.ProcessControlFailed, "尚未启动游戏进程。")).WaitAsync(cancellationToken);
    }

    private async Task ObserveAsync(Session session)
    {
        var stdout = PumpAsync(session.Process.StandardOutput, false, session);
        var stderr = PumpAsync(session.Process.StandardError, true, session);
        await session.Process.WaitForExitAsync().ConfigureAwait(false);
        try { await Task.WhenAll(stdout, stderr).WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false); }
        catch (TimeoutException)
        {
            session.LogCancellation.Cancel();
            session.Process.StandardOutput.Dispose();
            session.Process.StandardError.Dispose();
            await Task.WhenAll(stdout, stderr).ConfigureAwait(false);
        }
        var result = new GameExitResult(session.Id, session.Process.ExitCode, session.Stopped, DateTimeOffset.UtcNow);
        session.Lease.Dispose();
        Cleanup(session.Root, session.PrivateDirectory);
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            lock (sync) { if (active == session) active = null; exitCode = result.ExitCode; }
            session.Process.Dispose();
            session.LogCancellation.Dispose();
            SetState(LaunchState.Exited);
            session.Completion.TrySetResult(result);
        }
        finally { gate.Release(); }
        Notify(Exited, new(result));
    }

    private async Task PumpAsync(StreamReader stream, bool isError, Session session)
    {
        var buffer = new char[2048];
        var line = new StringBuilder();
        var oversized = false;
        try
        {
            int count;
            while ((count = await stream.ReadAsync(buffer, session.LogCancellation.Token).ConfigureAwait(false)) > 0)
                for (var i = 0; i < count; i++)
                {
                    var character = buffer[i];
                    if (character == '\n') { Emit(); continue; }
                    if (oversized) continue;
                    if (line.Length == 16 * 1024) { oversized = true; line.Clear(); }
                    else line.Append(character);
                }
            if (line.Length != 0 || oversized) Emit();
        }
        catch (Exception error) when (error is IOException or ObjectDisposedException or OperationCanceledException) { }

        void Emit()
        {
            Notify(isError ? ErrorReceived : OutputReceived, new(oversized ? "[oversized game output omitted]" : session.Redactor.Redact(line.ToString().TrimEnd('\r')), oversized));
            line.Clear();
            oversized = false;
        }
    }

    private string GameDirectory()
    {
        var game = config.GameCore;
        var relative = game.GameDirectory ?? (game.Isolate ? "instances/" + VersionParser.Id(game.InstanceId ?? game.VersionId) : "");
        if (relative.Length != 0) GameFiles.Resolve(downloads.RootDirectory, relative);
        return relative.Replace('\\', '/').TrimEnd('/');
    }

    private static LaunchConfig Freeze(LaunchConfig value)
    {
        if (value.GameCore == null || value.Java == null || value.Window == null || value.Java.Selection == null || value.Java.Detection == null ||
            value.Downloads == null || value.Java.Arguments == null || value.GameArguments == null || value.Features == null ||
            value.Window.Width is < 1 or > 16384 || value.Window.Height is < 1 or > 16384 ||
            value.Java.MinMemoryMb < 1 || value.Java.MaxMemoryMb < value.Java.MinMemoryMb || value.Java.MaxMemoryMb > 1024 * 1024 ||
            string.IsNullOrWhiteSpace(value.LauncherName) || string.IsNullOrWhiteSpace(value.LauncherVersion))
            throw new LaunchException(LaunchErrorCode.InvalidConfig, "启动配置、窗口尺寸或内存设置无效。");
        string root;
        try
        {
            VersionParser.Id(value.GameCore.VersionId);
            value.Java.Selection.Validate();
            root = Path.GetFullPath(value.GameCore.RootDirectory);
        }
        catch (Exception error) when (error is VersionException or ArgumentException or NotSupportedException or IOException)
        { throw new LaunchException(LaunchErrorCode.InvalidConfig, "版本、游戏根目录或 Java 选择配置无效。"); }
        var arguments = value.Java.Arguments.Concat(value.GameArguments).ToArray();
        if (arguments.Length > 512 || arguments.Any(a => a == null || a.Length > 32768 || a.Any(char.IsControl)) ||
            value.LauncherName.Length > 128 || value.LauncherVersion.Length > 128 || value.LauncherName.Any(char.IsControl) || value.LauncherVersion.Any(char.IsControl))
            throw new LaunchException(LaunchErrorCode.InvalidConfig, "启动配置包含无效参数。");
        return value with
        {
            GameCore = value.GameCore with { RootDirectory = root },
            Java = value.Java with { Arguments = value.Java.Arguments.ToArray() },
            GameArguments = value.GameArguments.ToArray(),
            Features = new Dictionary<string, bool>(value.Features, StringComparer.Ordinal)
        };
    }

    private void SetState(LaunchState value, LaunchError? error = null) { state = value; Notify(StateChanged, new(value, error)); }
    private void Notify<T>(EventHandler<T>? handlers, T args) where T : EventArgs
    {
        if (handlers == null) return;
        foreach (EventHandler<T> handler in handlers.GetInvocationList())
            try { handler(this, args); } catch (Exception) { /* Host event failures cannot invalidate an owned game process. */ }
    }
    private static LaunchError Failure(Exception error) => error switch
    {
        LaunchException e => new(e.Code, e.Message),
        VersionException e => new(LaunchErrorCode.VersionError, "版本元数据处理失败。", e.Code.ToString()),
        JavaException e => new(LaunchErrorCode.JavaError, "Java 检测或兼容性检查失败。", e.Code.ToString()),
        AuthenticationException e => new(LaunchErrorCode.AuthenticationError, "账号认证或刷新失败。", e.Code.ToString()),
        InstallationException e => new(LaunchErrorCode.InstallationError, "游戏安装或原生依赖处理失败。", e.Code.ToString()),
        AssetException e => new(LaunchErrorCode.InstallationError, "游戏资源处理失败。", e.Code.ToString()),
        DownloadException e => new(e.Code == DownloadErrorCode.UnsafePath ? LaunchErrorCode.UnsafePath : LaunchErrorCode.InstallationError, "游戏文件下载或校验失败。", e.Code.ToString()),
        Win32Exception => new(LaunchErrorCode.ProcessStartFailed, "无法创建游戏进程。"),
        IOException or UnauthorizedAccessException => new(LaunchErrorCode.IoError, "无法访问游戏文件或进程。"),
        _ => new(LaunchErrorCode.UnexpectedError, "启动操作失败。")
    };

    private static void Cleanup(string root, string relative)
    {
        try
        {
            var path = GameFiles.Resolve(root, relative);
            if (!Directory.Exists(path)) return;
            Check(path);
            Directory.Delete(path, recursive: true);
            static void Check(string directory)
            {
                DownloadPath.CheckLinks(directory);
                foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
                {
                    DownloadPath.CheckLinks(entry);
                    if (Directory.Exists(entry)) Check(entry);
                }
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or DownloadException) { }
    }

    /// <summary>Stops any owned process/preparation, waits for resource release, and disposes this launcher. Caller-owned account managers remain usable.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        await StopAsync().ConfigureAwait(false);
        gate.Dispose();
    }

    private sealed class InlineProgress<T>(Action<T> action) : IProgress<T> { public void Report(T value) => action(value); }
    private sealed class Session(Process process, FileStream lease, string root, string privateDirectory, SecretRedactor redactor)
    {
        internal Process Process { get; } = process;
        internal int Id { get; } = process.Id;
        internal FileStream Lease { get; } = lease;
        internal string Root { get; } = root;
        internal string PrivateDirectory { get; } = privateDirectory;
        internal SecretRedactor Redactor { get; } = redactor;
        internal CancellationTokenSource LogCancellation { get; } = new();
        internal TaskCompletionSource<GameExitResult> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal bool Stopped { get; set; }
    }
}
