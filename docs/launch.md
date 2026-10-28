# 启动游戏

使用 `FMCore.Launch`。填一次设置，再调用 `LaunchAsync()`；按钮点击事件里也使用这个方法。

## 最短调用

下面启动的是已经安装好的游戏：

```csharp
using FMCore.Accounts;
using FMCore.Launch;

LaunchConfig config = new()
{
    Account = OfflineAccount.Create("Steve"),
    GameCoreConfig = new() { Root = ".minecraft", Version = "1.21.10" },
    JavaConfig = new()
    {
        JavaPath = "C:/Java/jdk-25/bin/java.exe",
        MaxMemoryMb = 2048
    }
};

var launcher = new MinecraftLauncher(config);
int exitCode = await launcher.LaunchAsync();
```

`JavaPath` 填用户选择的 Java，`MaxMemoryMb` 的单位是 MB。游戏根目录和版本由启动器选择。首次下载游戏用[原版安装](installation.md)，已有游戏缺文件用[文件补全](game-files.md)。

`LaunchAsync()` 会一直等到游戏退出，并读取完最后的日志，然后返回退出码。收到 `JavaStarted` 只表示 Java 已经运行，不能据此显示“已进入主菜单”。

## 显示进度和日志

已有上面的 `launcher` 时：

```csharp
int exitCode = await launcher.LaunchAsync(
    onProgress: progress => Console.WriteLine(progress.Message),
    onOutput: Console.WriteLine,
    onError: Console.Error.WriteLine);
```

`onOutput`、`onError` 每次收到一行游戏输出。`onError` 里也可能有普通提示，不能只凭它被调用就判断游戏失败。

这些方法可能在后台执行。WPF 等界面程序更新控件时，要切回界面线程，见[进度与错误处理](advanced.md)。

## 接到启动按钮

下面是已有 WPF 按钮 `LaunchButton` 和上面 `launcher` 的窗口代码。绑定一次即可：

```csharp
LaunchButton.Click += async (_, _) =>
{
    LaunchButton.IsEnabled = false;
    try
    {
        await launcher.LaunchAsync();
    }
    catch (Exception error)
    {
        System.Windows.MessageBox.Show(error.Message);
    }
    finally
    {
        LaunchButton.IsEnabled = true;
    }
};
```

不需要再套 `Task.Run`。这段代码会在游戏退出后恢复按钮。

## 设置说明

| 设置 | 填什么 |
| --- | --- |
| `Account` | 离线账号，或登录方法返回的 Microsoft、外置账号 |
| `GameCoreConfig.Root` | 含 `versions`、`assets`、`libraries` 的根目录 |
| `GameCoreConfig.Version` | 版本 JSON 中的 ID |
| `GameCoreConfig.GameDirectory` | 可选，存档和设置的保存位置；默认使用所选版本目录 |
| `JavaConfig.JavaPath` | `java.exe` 或 `javaw.exe` 路径 |
| `JavaConfig.MaxMemoryMb` | 游戏最大内存，必须大于 0 |
| `JavaConfig.JvmArguments` | 可选，额外 Java 设置，每一项单独放一个字符串 |
| `JavaConfig.AuthlibInjectorPath` | 外置账号需要的 authlib-injector JAR 路径 |
| `GameWindowConfig` | 可选，窗口的 `Width` 和 `Height` 一起设置 |

例如，在 `config` 中增加 `GameWindowConfig = new() { Width = 1280, Height = 720 }` 即可设置窗口。路径含空格时照常填写，不要自己添加引号。需要改变设置时创建新的配置和启动器对象。

Microsoft 登录返回的账号直接填入 `Account`，见 [Microsoft 登录](microsoft-login.md)。外置账号还需设置 `AuthlibInjectorPath`，见[外置验证](yggdrasil-login.md)。

## 安装加载器后启动

把安装结果中的 `VersionId` 和 `MinecraftRoot` 分别填入 `GameCoreConfig.Version` 和 `Root`。Fabric、Forge、NeoForge、OptiFine 都沿用上面的启动方式，不需要另一套启动方法。

## 启动前检查与取消

可选的 `await launcher.CheckAsync()` 用于“检查设置”按钮，不要求每次启动前都调用。文件补全成功后也不必再完整扫描一次。区别见[检查说明](diagnostics.md)。

取消方式见[取消操作](advanced.md)。游戏运行中取消会强制关闭本次游戏及它启动的子程序，不等于游戏内的“保存并退出”。同一个 `launcher` 同时只能执行一次检查或启动。

## 使用范围

当前面向 Windows。实际运行过的样本包括 x64 Java 8 下的原版 1.8.9，以及 x64 Java 25 下的 1.21.10 和所选加载器版本；不能据此保证所有组合可用。

- 新版游戏按最低 Java 要求检查，不要求必须等于某个主版本；高版本 Java 是否兼容具体 Mod，仍需实际确认。
- 1.16.5 这类使用新版启动参数、旧式原生库说明的组合暂未支持。
- 暂不支持 `virtual`、`map_to_resources` 和只有 `assets` 字段的旧资源存放方式。
- 旧式启动参数暂不支持用双引号把多个词拼成一个参数。
- 原生库会解压到所选版本目录中的独立 `natives` 文件夹，目前用完后暂时保留。解压会检查文件路径和压缩内容。
- 当前没有真实 x86、ARM64 游戏运行结果。

缺文件、Java 不适用、账号过期或设置不正确时，会返回对应错误。完整参数和错误类型见[接口参考](public-api.md#fmcorelaunch)。原来的 `PreparedLaunchConfig` 和 `Launcher.RunAsync` 留给需要自行准备启动资料的用法，普通启动直接使用本页写法。
