# T03：Java 检测与选择

公开入口为 `FMCore.Java.JavaManager`，无需新运行时依赖，不下载或安装 Java。

T06 补充 `JavaUtil.GetJavasAsync()` / `GetJavaInfoAsync(path)` 及同步便捷方法；启动器配置 `Java.JavaPath` 支持手动路径优先，自动选择继续复用 JavaManager。最简/高级调用见 [P0 SDK](sdk.md)和 [启动配置](launch.md)。

## SDK 用法

| API | 主要参数 | 返回值 |
| --- | --- | --- |
| `JavaUtil.GetJavasAsync(options, progress, ct)` | 可选有界搜索配置、检测进度 | `IReadOnlyList<JavaRuntime>`，仅有效安装 |
| `JavaUtil.GetJavaInfoAsync(path, ct)` | 实际 Java 路径或安装目录 | `JavaRuntime`：可执行文件、版本、架构、厂商 |
| `JavaManager.DetectAsync(progress, ct)` | 管理器构造时选定的检测配置 | `JavaDetectionResult`：Runtimes、Issues、IsTruncated |
| `Select(runtimes, metadata, options)` | 已探测运行时、有效元数据、可选策略 | 兼容 `JavaRuntime`，无匹配则抛异常 |
| `SelectAsync(metadata, customPath, options, progress, ct)` | 可选手动路径优先；其余参数可省略 | 检测并返回兼容运行时 |
| `JavaCompatibility.Check(runtime, metadata, options)` | 已探测运行时与需求 | `JavaCompatibilityResult`，含错误码而非执行游戏 |

最简调用：`var javas = await JavaUtil.GetJavasAsync(cancellationToken: ct)`。手动指定使用 `JavaUtil.GetJavaInfoAsync(path, ct)` 或 `SelectAsync` 的 `customPath`；高级版本选择示例如下。探测失败使用 `JavaException`，调用取消抛 `OperationCanceledException`，完整错误码见文末。

```csharp
using FMCore.Java;
using FMCore.Versions;

static async Task<JavaRuntime> ChooseJavaAsync(VersionManager versions, string versionId, CancellationToken ct)
{
    var resolved = await versions.ResolveAsync(versionId, cancellationToken: ct);
    var java = new JavaManager();
    var progress = new Progress<JavaDetectionProgress>(p =>
        Console.WriteLine($"Java 检测：{p.Completed}/{p.Total}"));
    var detected = await java.DetectAsync(progress, ct);
    // detected.Issues 可用于展示损坏候选；IsTruncated 表示搜索预算用尽。
    return java.Select(detected.Runtimes, resolved.Metadata);
}
```

`SelectAsync(metadata, cancellationToken: ct)` 可一次完成检测与选择；`SelectAsync(metadata, customPath: path, cancellationToken: ct)` 只使用指定路径。不兼容或检测失败时返回错误，不会静默改用其他安装。`ProbeAsync(path, ct)` 单独识别 Java；`JavaCompatibility.Check(runtime, metadata, options)` 返回兼容性结果，包含所需主版本、架构及错误码。

自定义路径必须是无命令行参数、无外围引号的绝对路径，可以是 Java 安装目录、`bin` 目录或 `java.exe`/`java` 文件。支持空格和 Java 安装链接；Windows `javaw.exe` 转为同目录 `java.exe` 检测，避免启动无输出窗口。macOS 支持 `Contents/Home/bin/java`。

## 检测范围与执行

1. 检查 `JAVA_HOME`，再检查 `PATH` 的各目录，最后检查常见安装根目录。
2. Windows 检查 Program Files 下 Java、Adoptium、Microsoft、Corretto、Zulu、BellSoft、Semeru 等目录；用户目录检查 `.jdks`、`.sdkman/candidates/java`、`.jabba/jdk` 等。Linux 检查 `/usr/lib/jvm` 等目录；macOS 检查 `JavaVirtualMachines` 等目录。
3. 每个常见根目录只检查自身及一层子目录，同时识别 macOS bundle 内的固定路径。不递归扫描磁盘，不读取注册表，不保证发现所有启动器私有的深层运行时目录。可通过自定义路径或 `SearchDirectories` 补充。

`JavaDetectionOptions` 可覆盖 JAVA_HOME、PATH 目录和常见根目录列表，空列表会禁用对应来源；环境路径允许外层双引号。配置在管理器构造时快照。默认每次探测超时 5 秒、每路最多 64 Ki 字符、最多 128 个可执行候选，每个根目录最多查看 128 个子目录；超出搜索预算时设置 `IsTruncated`。这些限制可配置，超时上限 1 分钟，输出上限 1 Mi 字符，候选上限 1024。

检测实际执行 `java -XshowSettings:properties -version`，附加 UTF-8 编码属性，读取 `java.version`、`java.home`、`java.vendor`、`os.arch` 和可选的 `sun.arch.data.model`。Java 8 的 `1.8.0_482`、现代版本、补丁/构建号和预发布标识使用同一 `JavaVersion` 解析器。未知架构保留为 `Unknown`，不能通过自动选择；不能用宿主程序的位数代替 Java 位数。

进程使用 `UseShellExecute = false` 和 `ArgumentList`，不经 shell；探测子进程移除 `JAVA_TOOL_OPTIONS`、`JDK_JAVA_OPTIONS`、`_JAVA_OPTIONS` 和 `CLASSPATH`，不修改父进程环境。两路输出并发限量读取，失败不保存或公开原始输出，也不把系统异常作为内部异常带出。非零退出码保留在结构化错误中。超时、取消或输出超限时终止自建探测进程并进行有界清理；调用方取消仍抛出 `OperationCanceledException`。

自动检测记录单个失败并继续；相同可执行路径只运行一次，同安装目录、架构和版本的多个入口去重。进度先报告候选总数，再报告每个候选完成及错误码。`JavaDetectionResult.Issues` 中的路径是诊断数据，应用展示时仍应考虑用户隐私。

## 兼容性策略

- **严格匹配有效元数据的 Java 主版本**，同一主版本内按数字优先选择较新补丁/构建号。不会将 Java 17、21、25 自动视为可互换，也不按游戏版本字符串猜测。
- 继承版本先调用 `VersionManager.ResolveAsync`，再传 `resolved.Metadata`，复用 T02 合并后的 `javaVersion`。例如真实格式的 1.12.2 Fixtures 包含 Java 8，1.21.1/Fabric 继承结果包含 Java 21。
- 没有 Java 要求的自定义或旧元数据返回 `RequirementMissing`。调用方确认要求后可设置 `new JavaSelectionOptions { FallbackMajorVersion = 8 }`；该选项只填补缺失值，不能覆盖已有元数据要求。
- 默认目标为操作系统架构，严格匹配。需要其他架构时可显式设置 `TargetArchitecture`，探测仍必须实际运行成功；不会假定跨架构模拟可用。预发布 Java 默认排除，需显式 `AllowEarlyAccess = true`。
- `selected.GetRuleContext(features)` 根据选定 Java 的架构构建 OS/架构/功能规则上下文，可直接用于 T02 的 Libraries 和 JVM/Game 参数规则。元数据兼容通过不等于加载器、原生库、模组或真实游戏运行已验证。

## 错误与验证

`JavaException.Code` 包括 `InvalidPath`、`NotFound`、`ExecutionFailed`、`ProbeTimeout`、`InvalidOutput`、`OutputLimitExceeded`、`IoError`；兼容性使用 `RequirementMissing`、`MajorVersionMismatch`、`ArchitectureMismatch`、`EarlyAccessNotAllowed`，自动选择无匹配项使用 `NoCompatibleRuntime`。

Windows / .NET 10 下新增 65 项测试，全套 204 项通过，无失败或跳过。Fixtures 和替代探测验证发现顺序、限量枚举、版本/架构解析、严格选择、继承以及无效路径；测试项目自身的子进程验证真实管道并发、UTF-8、退出码、输出超限、超时/取消和进程退出，不要求安装 Java。

2026-10-21 另通过实际 SDK 执行本机 Java：Corretto 8u482、17.0.9、18.0.2，OpenJDK 20.0.2、25.0.2，均识别为 x64。检测记录一个无效环境目录和一个返回非零退出码的 PATH 入口，仍发现 5 套可用安装；按元数据选择 Java 8/17；本机无 Java 21，1.21.1 返回 `NoCompatibleRuntime`，没有使用 Java 25 替代。未启动 Minecraft，未联网下载 Java；Linux、macOS、x86/ARM 的真实执行尚未验证。

## 资料

- [Oracle Java 命令说明](https://docs.oracle.com/en/java/javase/21/docs/specs/man/java.html)：版本查询、属性输出及继承环境选项。
- [Oracle 编码说明](https://docs.oracle.com/en/java/javase/24/intl/supported-encodings.html)：标准输出/错误编码属性。
- [.NET ProcessStartInfo.ArgumentList](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.argumentlist?view=net-10.0)：独立参数及转义。
- [.NET Process.Kill](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.kill?view=net-10.0)：终止进程后的等待及进程树限制。
