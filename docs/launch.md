# 原版启动

`FMCore.Launch.MinecraftLauncher` 自动协调版本继承、Java、账号、安装检查、Classpath、条件参数、资源布局和原生依赖。仅引用 FMCore，无需 GUI 框架、DI 容器或手动构建命令行。

## 最简完整示例

| API / 事件 | 输入或内容 | 返回值 / 行为 |
| --- | --- | --- |
| `new MinecraftLauncher(config)` | `LaunchConfig`：GameCore、Java、Window、Account/Accounts | 只持有配置，调用 `LaunchAsync` 才准备游戏 |
| `LaunchAsync(progress, ct)` | 可选阶段进度与准备取消 | `LaunchResult`：Success、ProcessId、Error |
| `WaitForExitAsync(ct)` | 可选等待取消，不终止游戏 | `GameExitResult`：ExitCode、StoppedByCaller、ExitedAt |
| `StopAsync(ct)` / `DisposeAsync()` | 停止当前拥有的游戏 / 异步释放 | 完成准备取消或进程停止及资源清理 |
| `StateChanged` / `OutputReceived` / `ErrorReceived` / `Exited` | 状态、脱敏标准输出/错误、退出结果 | 后台回调，GUI 自行调度；不要阻塞等待启动器 |
| `State` / `ProcessId` / `ExitCode` | 当前状态查询 | 进程未建立或尚未退出时相应值可为 null |

业务启动失败通过 `LaunchResult.Error` 返回；配置或进程控制误用可能抛 `LaunchException`，取消抛 `OperationCanceledException`。下面为最简完整调用，后续高级示例和配置表说明默认值、账号刷新、Java、目录、窗口与参数。

可作为启用隐式 using 的 .NET 10 控制台 `Program.cs`。先安装对应 Java；默认会下载/修复原版游戏。离线登录不需要网络，游戏文件尚未安装时仍需联网安装。

```csharp
using FMCore.Authentication;
using FMCore.Launch;

var ct = CancellationToken.None;
using var accounts = new AccountManager(Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyLauncher", "accounts"));
await using var launcher = new MinecraftLauncher(new()
{
    GameCore = new() { RootDirectory = ".minecraft", VersionId = "1.16.5" },
    Account = await accounts.LoginOfflineAsync("Player", ct)
});
var result = await launcher.LaunchAsync(cancellationToken: ct);
if (!result.Success)
{
    Console.Error.WriteLine($"{result.Error!.Code}: {result.Error.Message}");
    return;
}
var exit = await launcher.WaitForExitAsync(ct);
Console.WriteLine($"游戏退出：{exit.ExitCode}");
```

`LaunchAsync` 在进程创建后返回，**不代表游戏已到主菜单**。`WaitForExitAsync` 保持示例存活并获取真实退出码；异步释放会停止该启动器拥有的游戏。GUI 应把启动器保存为应用/窗口字段，而不是在按钮事件内立即释放。

## 高级完整示例

支持 Ctrl+C、正版/离线显式选择、自动刷新、独立实例、内存、窗口、自定义 JVM 参数、阶段/文件进度和日志。设置 `FMCORE_GAME_ROOT`、`FMCORE_VERSION`、`FMCORE_JAVA` 可覆盖目录、版本和 Java；未指定 Java 时自动选择。账号默认放在用户数据目录，可用 `FMCORE_ACCOUNT_DIRECTORY` 覆盖。

运行时加入 `--microsoft` 才发起正版设备授权，必须配置自己的 `FMCORE_CLIENT_ID`，短码只作为前台提示。两个示例都只调用公开 API，可独立编译。

```csharp
using FMCore.Authentication;
using FMCore.Launch;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var ct = cancellation.Token;
var root = Environment.GetEnvironmentVariable("FMCORE_GAME_ROOT") ?? ".minecraft";
var accountDirectory = Environment.GetEnvironmentVariable("FMCORE_ACCOUNT_DIRECTORY") ?? Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MyLauncher", "accounts");
using var accounts = new AccountManager(accountDirectory);
try
{
    LaunchIdentity identity;
    if (args.Contains("--microsoft", StringComparer.Ordinal))
    {
        var clientId = Environment.GetEnvironmentVariable("FMCORE_CLIENT_ID")
            ?? throw new InvalidOperationException("请配置自己的 FMCORE_CLIENT_ID。");
        identity = await accounts.LoginMicrosoftAsync(new(clientId), (code, token) =>
        {
            token.ThrowIfCancellationRequested();
            if (Console.IsOutputRedirected)
                throw new InvalidOperationException("授权提示只应显示在前台交互界面。");
            Console.WriteLine($"请在 {code.VerificationUri} 输入 {code.UserCode}");
            return Task.CompletedTask; // 展示后返回，核心负责轮询。
        }, cancellationToken: ct);
    }
    else identity = await accounts.LoginOfflineAsync("Player", ct);

    var config = new LaunchConfig
    {
        GameCore = new()
        {
            RootDirectory = root,
            VersionId = Environment.GetEnvironmentVariable("FMCORE_VERSION") ?? "1.16.5",
            InstanceId = "survival",
            Isolate = true,
            AutoInstall = true
        },
        Java = new()
        {
            JavaPath = Environment.GetEnvironmentVariable("FMCORE_JAVA"),
            MinMemoryMb = 512,
            MaxMemoryMb = 4096,
            Arguments = ["-XX:+UseG1GC", "-Dexample.option=value with spaces"]
        },
        Window = new() { Width = 1280, Height = 720, Fullscreen = false },
        Account = identity,
        Accounts = accounts // 启动前自动重取/刷新此账号，调用方保留管理器所有权。
    };
    await using var launcher = new MinecraftLauncher(config);
    launcher.StateChanged += (_, e) => Console.WriteLine($"状态：{e.State}");
    launcher.OutputReceived += (_, e) => Console.WriteLine(e.Text);
    launcher.ErrorReceived += (_, e) => Console.Error.WriteLine(e.Text);
    launcher.Exited += (_, e) => Console.WriteLine($"退出码：{e.Result.ExitCode}");
    var progress = new Progress<LaunchProgress>(p =>
    {
        if (p.Installation is { } files)
            Console.WriteLine($"{files.Stage}: {files.CompletedFiles}/{files.TotalFiles}");
        else Console.WriteLine(p.Stage);
    });

    var result = await launcher.LaunchAsync(progress, ct);
    if (!result.Success)
    {
        Console.Error.WriteLine($"{result.Error!.Code}: {result.Error.Message} ({result.Error.DetailCode})");
        return;
    }
    try { await launcher.WaitForExitAsync(ct); }
    finally { await launcher.StopAsync(); } // 等待被取消时仍清理本示例拥有的进程。
}
catch (OperationCanceledException) when (ct.IsCancellationRequested)
{
    Console.WriteLine("操作已取消。");
}
catch (AuthenticationException error)
{
    Console.Error.WriteLine($"认证失败：{error.Code} / {error.Stage}");
}
catch (LaunchException error)
{
    Console.Error.WriteLine($"配置或进程操作失败：{error.Code}");
}
```

WPF、Avalonia、WinUI 使用相同配置与调用。`Progress<T>` 可在 UI 线程创建以捕获上下文；普通 .NET 事件和设备码回调可能运行在后台，宿主通过自己的 Dispatcher 更新界面。事件处理器应快速返回，不要同步等待启动器操作；处理器异常被隔离，避免阻断日志读取或误报进程状态。

## 配置与默认行为

| 配置 | 默认与含义 |
| --- | --- |
| `GameCore.RootDirectory` / `VersionId` | 当前目录下 `.minecraft` / 必须指定有效版本 ID |
| `Isolate` / `InstanceId` | 默认隔离，工作目录为 `instances/<InstanceId 或 VersionId>` |
| `GameDirectory` | 显式指定相对根目录的工作目录，优先于自动隔离；拒绝越界和目录链接 |
| `AutoInstall` | 默认 `true`，自动下载/修复；`false` 仅使用缓存元数据与本地下载文件，仍生成实例目录、原生依赖和旧版资源映射 |
| `CachePolicy` | 自动安装时默认 `PreferCache`；本地启动使用 `CacheOnly` |
| `JavaPath` | 显式路径优先，实际运行探测后仍须匹配版本/架构，失败不换用其他 Java |
| `MinMemoryMb` / `MaxMemoryMb` | 512 / 2048 MiB；最小不超过最大；32 位 Java 最大 1536 MiB |
| `Java.Selection` / `Detection` | 高级兼容/扫描策略；元数据缺 Java 要求时须显式配置 `FallbackMajorVersion` |
| `Window` | 854×480，非全屏；自动启用自定义分辨率条件 |
| `Account` / `Accounts` | 直接接收 T05 `LaunchIdentity`；可传管理器重取/刷新，未给身份时使用其所选账号 |
| `Java.Arguments` / `GameArguments` | 每个元素一个参数，保留空格，不手动加 shell 引号；不允许覆盖受控的内存、Classpath、主类、账号或资源目录 |
| `Features` | 自定义版本功能条件，`has_custom_resolution` 由窗口配置确定 |
| `Downloads` | 默认下载并发/重试配置，可直接覆盖 |

版本继承、规则和客户端选择直接消费 Versions 模型；同时具有普通 artifact 与 native classifier 的库会保留两者。Java 主版本严格匹配元数据，不把更高主版本当作兼容替代。已安装加载器 profile 可通过其版本 ID 查询元数据，但本阶段没有安装或实测加载器，不据此宣称 Fabric/Forge 等运行已验证。

原生解压目录为共享根下 `.fmcore/launch/<随机 ID>/natives`，每次启动独立，进程退出后清理；设置、存档、游戏日志留在实例工作目录。相同工作目录有进程生命周期锁，第二个启动返回 `InstanceBusy`，不同实例可并行。实例隔离不等于本阶段实现完整实例导入、删除、备份管理。

旧版 `minecraftArguments`、现代 `arguments.jvm/game`、OS/架构/feature 条件、`${auth_session}`、`${game_assets}`、`virtual` 和 `map_to_resources` 均有自动化覆盖。已停用且缺少可用资源索引的古老版本仍返回明确安装错误；旧版真正运行和现代最新版本运行尚未实测。

## 状态、失败和停止

`LaunchAsync` 的业务失败返回 `LaunchResult { Success = false, Error }`，`Error.Code` 为启动分类，`DetailCode` 保留版本/Java/认证/下载等模块错误码名称，不包含服务原文。无效构造配置和进程控制误用抛 `LaunchException`；调用取消抛 `OperationCanceledException`。同启动器已有进程时返回 `AlreadyRunning` 并保持现有状态。

`State` 包含 `Idle → Preparing → Starting → Running → Exited`，以及 `Stopping`、`Failed`、`Canceled`。创建后立即崩溃的游戏仍可能先获得启动成功结果，应继续监听退出码和日志；游戏非零退出是进程结果，不重新解释为成功加载。

`LaunchAsync` 的取消令牌控制准备及创建进程前的取消，成功创建后由 `StopAsync` 或异步释放管理生命周期。`WaitForExitAsync(ct)` 取消只终止等待，不杀游戏。`StopAsync` 可取消准备，或请求结束拥有的进程树并等待主进程退出与输出处理；强制停止可产生非零退出码，`StoppedByCaller` 用于区分。退出后可重用同一启动器。释放前应协调完其他并发调用，调用方传入的 `AccountManager` 由调用方释放。

## 凭据保护与验证

SDK 不公开原始 `Process`、`ProcessStartInfo` 或完整参数字符串。使用独立参数而非 shell；清除继承的 Java 选项注入变量，默认 UTF-8 输出。并发读取 stdout/stderr，隐藏已知访问令牌、旧版 session 及 URL 编码形式；每行超过 16 Ki 字符时整行省略，避免部分令牌穿过截断边界。配置的账号与自定义参数不参与默认 JSON 序列化，诊断字符串也不输出它们。

此过滤覆盖 **FMCore 转发的日志事件**，不能控制游戏、Mod、JVM、系统诊断工具自行写入的文件或系统进程参数可见性。宿主应保护日志收集与诊断导出，不自行记录凭据、原始进程参数或内存转储；旧版游戏协议本身要求访问令牌传入进程。正版刷新/资格检查失败只会报错，不自动切换离线。

验收：Windows / .NET 10 自动化测试 402 项通过；真实游戏版本仅 **1.16.5**，Java **1.8.0_482 x64**。正版与离线均由外部调用方仅通过公开 SDK 启动，观察到窗口、OpenAL 和纹理图集初始化后自动停止，未进入世界或测试多人服务器。Linux/macOS 路径与规则通过模拟/参数测试，未做对应实机启动。公开入口与用法见 [P0 API 清单](sdk.md)，详细实机记录保留在维护者本地。

实现依据：[.NET ArgumentList](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.argumentlist?view=net-10.0)、[并行读取进程输出](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.standardoutput?view=net-10.0)、[等待退出](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.waitforexitasync?view=net-10.0)、[停止进程树](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.kill?view=net-10.0)、[Java 启动命令](https://docs.oracle.com/en/java/javase/21/docs/specs/man/java.html)；参数结构来自已归档的官方版本 JSON Fixtures。
