# 本机 Java 查询

命名空间 FMCore.Java。JavaReader 用于发现和探测已有 Java；JavaInstaller 可安装 Mojang 提供的 Windows x64 游戏运行时，见 [Java 安装](java-installation.md)。查询本身不下载或安装。

## 获取指定 Java 的信息

```csharp
using FMCore.Java;

if (args.Length != 1)
    throw new ArgumentException("请提供 java.exe 的完整路径。");
JavaInfo java = await JavaReader.GetJavaInfoAsync(args[0]);
Console.WriteLine($"Java {java.MajorVersion}，{java.Version}，{java.Architecture ?? "未知架构"}");
```

返回可执行文件绝对路径、完整版本、主版本和可选架构。查询会实际执行所选程序；请只探测用户信任的 Java。架构来自该 Java 的 os.arch，不从 Windows 位数推断。

## 列出可用 Java

```csharp
using FMCore.Java;

foreach (JavaInfo java in await JavaReader.GetJavasAsync())
    Console.WriteLine($"Java {java.MajorVersion}：{java.Executable}");
```

候选来自 JAVA_HOME、PATH、用户 .jdks、JavaSoft 注册信息。安装在其他盘可以通过这些来源或 searchPaths 发现；额外路径检查目录本身、bin 和直属安装子目录。不会全盘递归，也不保证找到所有未登记的安装。

## 指定范围、取消和失败诊断

```csharp
using FMCore.Java;

using CancellationTokenSource canceled = new(TimeSpan.FromSeconds(30));
IReadOnlyList<JavaInfo> javas = await JavaReader.GetJavasAsync(
    searchPaths: args,
    onError: (path, error) => Console.Error.WriteLine($"{path}：{error.Message}"),
    cancellationToken: canceled.Token);
Console.WriteLine($"可用 Java：{javas.Count}");
```

单个候选失败继续，其诊断通过回调或 Trace 警告提供；回调自己的错误不会被吞掉。回调可能运行于后台线程，UI 更新需由启动器切回界面线程。

每次探测进程超时 10 秒；属性查询非零退出时退回同一 Java 的 -version。超时、取消和文件错误不重试、不替换 Java。进程不使用 shell，移除影响探测的三项 Java 注入环境变量，异常不附带完整系统属性。

独立项目：[指定 Java](../examples/ReadJavaInfo/Program.cs)、[Java 列表](../examples/ListJavas/Program.cs)。真实版本读取不等于 Minecraft 启动验收，未知架构返回 null，由后续启动检查明确处理。
