using FMCore.Authentication;
using FMCore.Java;
using FMCore.Launch;
using FMCore.Versions;

var root = Environment.GetEnvironmentVariable("FMCORE_MINECRAFT_ROOT") ?? throw new InvalidOperationException("请指定已有根目录。");
var folder = Environment.GetEnvironmentVariable("FMCORE_VERSION_FOLDER") ?? throw new InvalidOperationException("请指定实际版本目录名。");
var javaPath = Environment.GetEnvironmentVariable("FMCORE_JAVA") ?? throw new InvalidOperationException("请显式选择兼容 Java。");
var memory = int.Parse(Environment.GetEnvironmentVariable("FMCORE_MEMORY_MB") ?? throw new InvalidOperationException("请明确最大堆内存 MiB。"));
// 用户已信任并选择该安装；先展示信任提示，再由宿主自己的交互确认是否调用启动。
var version = await GameCoreUtil.GetGameCoreAsync(folder, root);
foreach (var warning in version.Warnings) Console.WriteLine(warning);
var java = await JavaUtil.GetJavaInfoAsync(javaPath);
using var accounts = new AccountManager(Path.Combine(root, ".fmcore", "example-accounts"));
var identity = await accounts.LoginOfflineAsync("Player");
var basic = LaunchConfig.Create(version.Id, root, identity, java, memory);
var config = basic with
{
    // 空字符串表示根目录；也可传根目录内相对路径，如 third-party/Survival。
    // 未设置该环境变量时，继续使用 versions/<实际版本目录名>。
    GameCore = basic.GameCore with { AutoInstall = false, GameDirectory = Environment.GetEnvironmentVariable("FMCORE_GAME_DIRECTORY") },
    Java = basic.Java with { Selection = new() { TargetArchitecture = java.Architecture, FallbackMajorVersion = java.Version.Major } }
};
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
await using var launcher = new MinecraftLauncher(config);
launcher.OutputReceived += (_, e) => Console.WriteLine(e.Text);
launcher.ErrorReceived += (_, e) => Console.Error.WriteLine(e.Text);
var result = await launcher.LaunchAsync(cancellationToken: cancellation.Token);
if (!result.Success) { Console.Error.WriteLine(result.Error); return; }
try { Console.WriteLine((await launcher.WaitForExitAsync(cancellation.Token)).ExitCode); }
catch (OperationCanceledException) { await launcher.StopAsync(); }
