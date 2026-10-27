# Java 运行时安装 · FMCore.Java

JavaInstaller 安装 Mojang 官方清单提供的 Windows x64 游戏运行时，完成文件校验后实际执行 Java 探测，并返回可直接用于启动游戏的路径。

## 最简用法

```csharp
using FMCore.Java;

var installer = new JavaInstaller(25, ".minecraft/runtime/java-25");
JavaInstallResult result = await installer.InstallAsync();
Console.WriteLine(result.Java.Executable);
Console.WriteLine($"Java {result.Java.Version}，架构 {result.Java.Architecture}");
```

目录由调用方指定。SDK 只在该目录创建清单要求的文件和目录，不修改 JAVA_HOME、PATH 或注册信息。查询已有 Java 使用 [JavaReader](java.md)。

## 构造方法与主方法

```text
public JavaInstaller(
    int majorVersion,
    string installDirectory,
    int maxConcurrency = 4)

public Task<JavaInstallResult> InstallAsync(
    Action<JavaInstallProgress>? onProgress = null,
    CancellationToken cancellationToken = default)
```

| 参数 | 含义 |
| --- | --- |
| majorVersion | 支持选择 8、16、17、21 或 25；对应官方组件，实际补丁版本由官方决定 |
| installDirectory | 此运行时的安装根目录，java.exe 位于其 bin 下 |
| maxConcurrency | 文件操作最大并发数，默认 4，须大于 0 |
| onProgress | 阶段与可选的下载批次，不保证 UI 线程 |
| cancellationToken | 取消元数据请求、下载或最终探测 |

构造检查平台、参数并固定路径，不执行安装。相对目录按构造时的工作目录确定。每个对象同时只允许一次安装，结束后可再次使用。

## 进度与结果

```csharp
using FMCore.Java;

using CancellationTokenSource canceled = new();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    canceled.Cancel();
};

var installer = new JavaInstaller(25, ".minecraft/runtime/java-25");
JavaInstallResult result = await installer.InstallAsync(
    progress =>
    {
        Console.WriteLine($"[{progress.Stage}] {progress.Message}");
        if (progress.Download is { } download)
            Console.WriteLine($"文件 {download.CompletedFiles}/{download.TotalFiles}，字节 {download.Percentage:F1}%");
    },
    canceled.Token);

Console.WriteLine($"下载：{result.DownloadedFiles}");
Console.WriteLine($"复用：{result.ReusedFiles}");
Console.WriteLine($"启动用路径：{result.Java.Executable}");
```

阶段依次为 ReadingMetadata、DownloadingFiles、VerifyingJava、Completed。Download 为当前文件批次统计；字节进度 100% 不代替最终 Java 探测成功。JavaInstallProgress 的 Stage、Message、Download 类型分别是 JavaInstallStage、string、DownloadBatchProgress?。

| JavaInstallResult 成员 | 含义 |
| --- | --- |
| Java | 已实际探测的 JavaInfo，包含 Executable、Version、MajorVersion 和 Architecture |
| InstallDirectory | 规范化后的安装目录 |
| TotalFiles | 本次文件清单中的普通文件数，不包含目录 |
| DownloadedFiles | 本次下载完成的文件数 |
| ReusedFiles | 已通过大小及 SHA-1 校验的本地复用文件数 |

将 result.Java.Executable 赋给 JavaConfig.JavaPath 即可使用该运行时。游戏启动仍由 MinecraftLauncher 负责，具体配置见 [游戏启动](launch.md)。

## 下载、校验与异常

先从官方目录取得组件声明，再按声明大小和 SHA-1 校验运行时文件清单。清单只在内存中读取；文件使用 raw 下载声明，不处理 LZMA。相对路径、真实目标目录、重复目标和文件/目录冲突在文件下载前检查；当前只支持 file 和 directory 条目。

FileDownloader 负责单文件校验、并发、有限重试、临时文件及取消收尾。已有合格文件复用；不匹配文件保留并报错，不自动备份替换或混合升级运行时。失败保留已完成文件与目录，相同清单再次执行可以继续校验复用。

元数据请求单次超时 30 秒、响应上限 8 MiB；每个文件使用下载器默认 5 分钟总预算和额外 2 次重试。最终 Java 探测沿用 JavaReader 的每个进程 10 秒超时，并核对所选主版本及 x64 架构。只有任务成功返回才表示这些步骤均完成。

无效参数、平台或主版本不支持、官方组件缺失、JSON/校验/路径错误、HTTP、磁盘、权限、回调、取消和超时按实际原因传播；批次失败可能是 AggregateException。回调错误也会使操作失败，已完成数据保留。

## 当前验收与限制

用户已按安装 Windows x64 Java 25、再使用其 java.exe 启动原版 1.21.10 的步骤反馈成功。本次没有记录实际输出中的完整补丁版本与文件数量，不把其他主版本、升级或故障分支当作已全部实测。

当前只安装官方游戏运行时，未提供任意 JDK 发行版选择、其他系统或架构、Java 更新与卸载，也未接入镜像选项。调用方选择安装位置并管理其生命周期。
