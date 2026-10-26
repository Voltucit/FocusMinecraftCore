# 游戏启动 · FMCore.Launch

开发者填写分组配置，创建 `MinecraftLauncher`，再调用 `LaunchAsync`。FMCore 内部读取所选版本、探测指定 Java、检查启动文件并执行现有启动流程。

适用于 `v2.0` 开发分支、Windows 10/11、.NET 10、C# 14。游戏需预先完整安装；当前不会自动补全文件。

## 最简启动

以下为完整的控制台代码示例，项目需已引用 FMCore。目录、版本和 Java 路径是示例值，实际调用由启动器提供：

```csharp
using FMCore.Accounts;
using FMCore.Launch;

LaunchConfig config = new()
{
    Account = OfflineAccount.Create("Steve"),

    GameCoreConfig = new()
    {
        Root = ".minecraft",
        Version = "1.21.10"
    },

    JavaConfig = new()
    {
        JavaPath = "C:/Java/jdk-25/bin/java.exe",
        MaxMemoryMb = 4096
    }
};

var launcher = new MinecraftLauncher(config);
int exitCode = await launcher.LaunchAsync(ReportProgress);

static void ReportProgress(LaunchProgress progress)
{
    Console.WriteLine($"[{progress.Stage}] {progress.Message}");
}
```

`ReportProgress` 是调用方自己定义的方法，其参数类型为 `LaunchProgress`。可以用它显示当前步骤，不需要解析游戏日志来判断准备到了哪一步。

## 命名空间

- `FMCore.Accounts`：离线身份。
- `FMCore.Launch`：分组配置、启动入口和阶段进度。
- `FMCore.Versions`、`FMCore.Java`：仅在需要单独查询或使用高级入口时引用。

## LaunchConfig 配置

配置类使用无参构造及对象初始化器。`required` 成员必须填写，`init` 成员只在创建时赋值。创建配置本身不启动游戏。

| 属性 | 类型 | 要求 |
| --- | --- | --- |
| Account | OfflineAccount | 必填，通常使用 OfflineAccount.Create |
| GameCoreConfig | GameCoreConfig | 必填，指定游戏根目录、版本和可选工作目录 |
| JavaConfig | JavaConfig | 必填，指定 Java、内存和可选额外 JVM 参数 |
| GameWindowConfig | GameWindowConfig? | 可省略，指定初始窗口宽高 |

### GameCoreConfig

| 属性 | 类型 | 含义 |
| --- | --- | --- |
| Root | string | 必填，Minecraft 根目录，包含 versions、libraries、assets |
| Version | string | 必填，版本 JSON 中的 ID；不是完整版本文件夹路径 |
| GameDirectory | string? | 可选游戏工作目录，默认所选版本的实际目录 |

版本 ID 和目录名分别处理。父版本只在同一个根目录中查找。自定义工作目录不会改变版本 ID，也不会复制已有存档、Mod 或配置。

### JavaConfig

| 属性 | 类型 | 含义 |
| --- | --- | --- |
| JavaPath | string | 必填，调用方选择的 Java 可执行文件路径 |
| MaxMemoryMb | int | 必填，最大堆内存 MB，必须为正整数 |
| JvmArguments | IEnumerable&lt;string&gt;? | 可选额外 JVM 选项，每个字符串是一条完整参数 |

新版清单声明最低 Java 主版本时，所选 Java 必须达到该值；达到最低版本不代表所有 Mod 都兼容。SDK 只探测调用方指定的 Java，不自动替换。

### GameWindowConfig

`Width`、`Height` 均为 `int?`。必须同时省略或同时提供正整数：

```csharp
GameWindowConfig = new()
{
    Width = 1280,
    Height = 720
}
```

这是 `LaunchConfig` 初始化器中的属性片段。新版依赖清单的自定义分辨率模板，旧版追加宽高参数。旧式模板已经指定宽高时当前不支持覆盖。

初始窗口尺寸仍可能受游戏内全屏、宽高覆盖设置或 Mod 影响。当前没有强制窗口模式的公开选项。

### 额外 JVM 参数

这是 `JavaConfig` 初始化器中的可选属性片段：

```csharp
JvmArguments =
[
    "-XX:+UseG1GC",
    "-Dfmcore.note=Hello FMCore"
]
```

参数保持单项中的空格和顺序，不再次分词。需要取值的长选项使用单项形式，例如 `--add-opens=java.base/java.lang=ALL-UNNAMED`。Java 25 可使用 `-Xlog:gc` 验证日志；调用方应选择所选 Java 支持的参数。

最大内存、classpath、启动入口、原生库路径、编码和日志配置路径由基础流程管理，不通过额外参数重复设置。完整拦截项见 [PreparedLaunchConfig](public-api.md#preparedlaunchconfig)。

## MinecraftLauncher 构造与方法

以下为接口声明：

```text
public MinecraftLauncher(LaunchConfig config)

public Task<int> LaunchAsync(
    Action<LaunchProgress>? onProgress = null,
    Action<string>? onOutput = null,
    Action<string>? onError = null,
    CancellationToken cancellationToken = default)
```

构造方法检查必填对象、版本 ID、内存和路径，确定绝对路径并复制 JVM 参数集合。它不读取版本 JSON、不运行 Java、不创建游戏目录。宽高配对和额外 JVM 选项等校验在启动准备中复用已解析配置的规则。

相对路径按创建启动器时的工作目录确定。相同启动器实例在一次启动或运行尚未结束时拒绝再次调用；操作结束后可重新调用。需要更换设置时创建新的配置和启动器。

| 方法参数 | 用途 |
| --- | --- |
| onProgress | 可选阶段回调，接收 LaunchProgress |
| onOutput | 可选 Java 标准输出逐行回调 |
| onError | 可选 Java 标准错误逐行回调 |
| cancellationToken | 取消准备，或尝试强制停止本次已经启动的进程树 |

返回 `Task<int>`，Java 退出并完成日志收尾后任务结束，结果为 Java 退出码。当前不返回进程对象或“主菜单已打开”的状态对象。

## LaunchProgress 进度

```text
public sealed record LaunchProgress(LaunchStage Stage, string Message)
```

| LaunchStage | 报告时机 |
| --- | --- |
| ReadingVersion | 开始读取所选版本 |
| DetectingJava | 开始探测指定 Java |
| PreparingGame | 检查启动文件并准备原生库 |
| StartingJava | 即将创建 Java 进程 |
| JavaStarted | Java 进程已创建，Message 包含 PID |
| GameExited | Java 正常走完退出和日志收尾流程，Message 包含退出码 |

这是阶段进度，没有百分比。`GameExited` 也可能携带非零退出码；异常或取消通过异常返回，不保证发出该阶段。`JavaStarted` 不代表已进入游戏主菜单。

准备和进程处理在后台执行。进度、标准输出与错误回调不保证 UI 线程，且可能交错；界面程序自行调度更新并处理共享数据同步。回调应及时返回。回调抛异常会使操作失败，进程仍运行时触发相应收尾。

## 完整控制台示例

项目引用 FMCore，五个运行参数依次为 Minecraft 根目录、版本 ID、Java 路径、离线用户名和内存 MB：

```csharp
using System.Globalization;
using FMCore.Accounts;
using FMCore.Launch;

if (args.Length != 5)
{
    Console.Error.WriteLine(
        "参数：Minecraft根目录 版本ID Java路径 用户名 内存MB");
    return 2;
}

using CancellationTokenSource canceled = new();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    canceled.Cancel();
};

try
{
    LaunchConfig config = new()
    {
        Account = OfflineAccount.Create(args[3]),
        GameCoreConfig = new()
        {
            Root = args[0],
            Version = args[1]
        },
        JavaConfig = new()
        {
            JavaPath = args[2],
            MaxMemoryMb = int.Parse(
                args[4], CultureInfo.InvariantCulture)
        }
    };

    var launcher = new MinecraftLauncher(config);

    int exitCode = await launcher.LaunchAsync(
        ReportProgress,
        onOutput: Console.WriteLine,
        onError: Console.Error.WriteLine,
        cancellationToken: canceled.Token);

    Console.WriteLine($"GameExitCode: {exitCode}");
    return exitCode;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("操作已取消。");
    return 130;
}
catch (Exception error)
{
    Console.Error.WriteLine($"失败：{error.Message}");
    return 1;
}

static void ReportProgress(LaunchProgress progress)
{
    Console.WriteLine($"[{progress.Stage}] {progress.Message}");
}
```

130 是该控制台程序自己定义的取消退出码；SDK 本身通过异常表示取消。

## WPF 启动按钮

先在已有窗口中准备好 `MinecraftLauncher launcher`。XAML 放置：

```xml
<Button x:Name="LaunchButton" Content="启动游戏" />
```

在窗口初始化、`InitializeComponent()` 之后绑定一次。以下片段需引用 `FMCore.Launch` 与 `System.Windows`：

```csharp
LaunchButton.Click += async (_, _) =>
{
    LaunchButton.IsEnabled = false;

    try
    {
        int exitCode = await launcher.LaunchAsync(
            progress => Console.WriteLine(progress.Message));

        MessageBox.Show($"游戏已退出，退出码：{exitCode}");
    }
    catch (Exception error)
    {
        MessageBox.Show(error.Message);
    }
    finally
    {
        LaunchButton.IsEnabled = true;
    }
};
```

入口内部已把同步准备放在后台，按钮不需要再套一层 `Task.Run`。示例的进度输出供调试使用；WPF 窗口没有控制台时，应接入自己的日志或通过 `Dispatcher` 更新控件。按钮在整个运行期间禁用，退出后恢复。

## 日志、取消与异常

标准输出和标准错误并行读取。Java 的标准错误也可能包含普通提示，不能只凭一次 `onError` 回调判断失败。SDK 不累计整份日志，也不额外保存完整日志文件。

取消是强制停止本次进程树，不是游戏内的保存并退出。正常退出后的日志排空最多 5 秒；异常或取消时，停止等待最多 10 秒，日志收尾最多 5 秒。它们不限制游戏总运行时长。

常见异常包括：

| 异常 | 原因举例 |
| --- | --- |
| ArgumentNullException / ArgumentException | 必填对象或路径无效、宽高未配对、JVM 条目不符合约定 |
| ArgumentOutOfRangeException | 内存或窗口尺寸不是正整数 |
| InvalidOperationException | 同一个启动器实例已有一次操作未结束 |
| FileNotFoundException / DirectoryNotFoundException | Java、客户端、依赖、索引、资源目录或日志配置缺失 |
| InvalidDataException / System.Text.Json.JsonException | 元数据、继承链、路径、压缩包或支持的模板无效 |
| NotSupportedException / PlatformNotSupportedException | 不支持的格式、过低 Java 主版本或非 Windows 平台 |
| OperationCanceledException / TimeoutException | 调用方取消、Java 探测或收尾超时 |
| AggregateException | 原操作和收尾同时失败，需查看 InnerExceptions |

操作系统、权限、文件和回调错误继续传播。进程使用 UTF-8 设置及解码，仅在该子进程环境中移除三个 Java 注入选项变量；系统环境保持原样。详见 [异步与错误处理](advanced.md)。

## 高级入口与兼容迁移

原来的构造式配置已经改名为 `PreparedLaunchConfig`，保存已取得的 MinecraftVersion、JavaInfo 和其他已校验设置。`Launcher.RunAsync`、`RunLegacyAsync` 仍是公开高级入口，接收这个类型。

新的普通调用使用 `LaunchConfig` 和 `MinecraftLauncher`。内部参数构造器、资源解析器和解压器不作为公开入口。完整声明见 [公开 API](public-api.md)。

## 当前范围

已实际启动 1.8.9 原版配合 x64 Java 8，以及 1.21.10 Fabric 0.17.3 配合 x64 Java 25。分组入口、阶段、窗口和 JVM 参数在后者上验收。

旧式参数暂不支持双引号分组。新版参数流程暂不支持旧式 natives 映射；virtual、map_to_resources 资源映射、更旧的仅 assets 字段格式、正版认证与自动补全尚未实现。没有真实 x86 或 ARM64 游戏运行证据。

原生库使用版本目录中的独立 natives 目录，成功或后续启动失败后暂时保留。旧式解压校验路径、链接、冲突、长度与 CRC32，失败仅清理本次创建内容。上限为 64 包、4096 条目、单包 128 MiB、总输入 512 MiB、单文件 64 MiB、总输出 256 MiB；CRC32 不代替来源和清单哈希认证。
