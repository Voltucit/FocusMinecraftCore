using FMCore.Authentication;
using FMCore.Downloader;
using FMCore.Installer;
using FMCore.Java;
using FMCore.Launch;
using FMCore.Versions;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

if (args is ["--help"])
{
    Console.WriteLine("无参数交互选择，或：<根目录> <官方版本ID> <Java路径> <最大内存MiB> <offline:昵称|microsoft> [--stop-after <秒>]");
    Console.WriteLine("Microsoft 模式需要 FMCORE_MICROSOFT_CLIENT_ID。Ctrl+C 取消/停止；退出日志保存在根目录 .fmcore/sample-game.jsonl。");
    return 0;
}
if (args.Length != 0 && args.Length != 5 && !(args.Length == 7 && args[5] == "--stop-after"))
{ Console.Error.WriteLine("参数无效，使用 --help 查看用法。"); return 2; }
using var cancellation = new CancellationTokenSource();
ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; cancellation.Cancel(); };
Console.CancelKeyPress += cancel;
MinecraftLauncher? launcher = null;
try
{
    var ct = cancellation.Token;
    var root = args.Length > 0 ? args[0] : Ask("游戏根目录");
    var catalog = await GameCoreUtil.GetAvailableVersionsAsync(root, cancellationToken: ct);
    foreach (var entry in catalog.Versions.Take(10)) Console.WriteLine($"{entry.Id} ({entry.Type})");
    var versionId = args.Length > 0 ? args[1] : Ask("要安装的官方 Minecraft 版本 ID");
    if (!catalog.Versions.Any(v => v.Id == versionId)) throw new ArgumentException();

    // 这里只展示本机 Java；选择与内存决策属于宿主/用户，不自动下载。
    var javas = await JavaUtil.GetJavasAsync(cancellationToken: ct);
    foreach (var runtime in javas) Console.WriteLine($"Java {runtime.Version.Text}: {runtime.ExecutablePath}");
    var javaPath = args.Length > 0 ? args[2] : Ask("选中的 Java 可执行路径/home");
    var maxMemory = int.Parse(args.Length > 0 ? args[3] : Ask("最大 Java 堆内存 MiB"));
    if (maxMemory is < 1 or > 1048576) throw new ArgumentException();
    var loginMode = args.Length > 0 ? args[4] : Ask("账号模式：offline:昵称 或 microsoft");
    var seconds = args.Length == 7 ? int.Parse(args[6]) : 0;
    if (seconds is < 0 or > 86400) throw new ArgumentException();
    var versions = new VersionManager(new DownloadManager(root));
    var resolved = await versions.ResolveAsync(versionId, cancellationToken: ct);
    var java = await new JavaManager().SelectAsync(resolved.Metadata, customPath: javaPath, cancellationToken: ct);
    var installation = new Progress<InstallationProgress>(p => Console.WriteLine($"安装 {p.Stage}: {p.CompletedFiles}/{p.TotalFiles}"));
    await new GameInstaller(root).InstallAsync(versionId, new() { Context = java.GetRuleContext() }, installation, ct);

    using var accounts = new AccountManager(Path.Combine(root, ".fmcore", "sample-accounts"));
    LaunchIdentity identity;
    if (loginMode.StartsWith("offline:", StringComparison.Ordinal))
        identity = await accounts.LoginOfflineAsync(loginMode[8..], ct);
    else if (loginMode == "microsoft")
    {
        var clientId = Environment.GetEnvironmentVariable("FMCORE_MICROSOFT_CLIENT_ID") ?? throw new ArgumentException();
        identity = await accounts.LoginMicrosoftAsync(new(clientId), (code, token) =>
        {
            Console.WriteLine($"请打开 {code.VerificationUri} 并输入设备授权码 {code.UserCode}。");
            return Task.CompletedTask;
        }, cancellationToken: ct);
    }
    else throw new ArgumentException();

    var basic = LaunchConfig.Create(versionId, root, identity, java, maxMemory);
    var config = basic with { Accounts = accounts, GameCore = basic.GameCore with { AutoInstall = false } };
    await using var ownedLauncher = new MinecraftLauncher(config);
    launcher = ownedLauncher;
    launcher.StateChanged += (_, e) => Console.WriteLine($"状态 {e.State}");
    launcher.OutputReceived += (_, e) => Console.WriteLine(e.Text);
    launcher.ErrorReceived += (_, e) => Console.Error.WriteLine(e.Text);
    launcher.Exited += (_, e) => Console.WriteLine($"退出事件：{e.Result.ExitCode}");
    var progress = new Progress<LaunchProgress>(p => Console.WriteLine($"启动 {p.Stage}"));
    try
    {
        var result = await launcher.LaunchAsync(progress, ct);
        if (!result.Success) { Console.Error.WriteLine(result.Error); return 1; }
        Console.WriteLine($"游戏进程 {result.ProcessId}；进程创建不代表主菜单已就绪。");
        if (seconds > 0) cancellation.CancelAfter(TimeSpan.FromSeconds(seconds));
        var exit = await launcher.WaitForExitAsync(ct);
        Console.WriteLine(exit.Diagnostics?.Summary);
        return exit.ExitCode == 0 ? 0 : 1;
    }
    catch (OperationCanceledException)
    {
        await launcher.StopAsync();
        Console.WriteLine("已停止游戏并释放运行资源。");
        return seconds > 0 ? 0 : 130;
    }
    finally { await launcher.Logger.SaveAsync(Path.Combine(root, ".fmcore", "sample-game.jsonl")); }
}
catch (OperationCanceledException) { Console.Error.WriteLine("准备已取消。"); return 130; }
catch (Exception e) when (e is DownloadException or VersionException or JavaException or InstallationException or AuthenticationException or LaunchException)
{ Console.Error.WriteLine($"{e.GetType().Name}: {e.Message}"); return 1; }
catch (Exception e) when (e is ArgumentException or FormatException or OverflowException)
{ Console.Error.WriteLine("参数或环境变量无效；请核对版本、Java、账号与内存配置。"); return 2; }
catch (Exception e) when (e is IOException or UnauthorizedAccessException)
{ Console.Error.WriteLine("本地文件访问失败，请检查路径和权限。"); return 4; }
finally { Console.CancelKeyPress -= cancel; }

static string Ask(string label)
{
    Console.Write($"{label}：");
    return Console.ReadLine() is { Length: > 0 } value ? value : throw new ArgumentException();
}
