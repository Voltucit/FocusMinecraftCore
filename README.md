# FocusMinecraftCore · FMCore

面向 Minecraft Java 版启动器开发者的 C# 核心类库。启动器填写账号、游戏和 Java 设置，FMCore 完成版本读取、Java 探测、参数准备、进程与日志处理。

> 当前为 `v2.0` 开发分支，使用 .NET 10 和 C# 14。分组配置与统一启动入口已通过真实游戏验收；单文件与并发下载、总体进度和校验复用已验收，有限重试已通过本地 HTTP 样本验收，近期速度与剩余时间已验收，资源索引、对象与日志补全、保留备份后修复及再次复用已验收，统一游戏文件补全已在新版样本连续两次校验并复用 4476 个文件，原版 1.21.10 安装与启动、BMCLAPI 下载源、在线版本列表已验收，旧式原生库声明已接入并通过 1.8.9 元数据验收。开发版尚未发布 NuGet，接口与已发布的 1.x 不同。

## 最简安装

命名空间：FMCore.Installer。安装不需要 Java 路径：

```csharp
using FMCore.Installer;

var installer = new MinecraftInstaller("1.21.10", ".minecraft");
MinecraftInstallResult result = await installer.InstallAsync();
Console.WriteLine(result.VersionDirectory);
```

在线版本列表使用 `await MinecraftInstaller.GetVersionsAsync()`。进度、下载源、取消和支持范围见 [原版安装](docs/installation.md)。

## 最简启动

命名空间：`FMCore.Accounts`、`FMCore.Launch`。示例中的目录、版本、Java 路径和内存由启动器开发者选择：

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

公开调用顺序为：**填写 LaunchConfig → 创建 MinecraftLauncher → 调用 LaunchAsync**。启动器不必先手动读取 MinecraftVersion 或探测 JavaInfo。

`LaunchAsync` 在 Java 退出并完成日志收尾后返回整数退出码。进度 `JavaStarted` 表示 Java 已创建，不表示已进入主菜单。窗口、额外 JVM 参数、日志和取消均可选，见 [启动手册](docs/launch.md)。

## 功能支持

| 功能 | 当前状态 |
| --- | --- |
| 本地版本查询 | 已实现；版本目录、按 ID 查询、同根继承与入口 |
| Java 查询 | 已实现；指定程序探测和本机候选发现 |
| 离线身份 | 已实现；兼容 Minecraft Java 的离线 UUID |
| 分组启动配置 | 已实现；账号、游戏、Java 和可选窗口配置 |
| 统一启动入口 | 已实现 MinecraftLauncher.LaunchAsync；新版样本已实际验收 |
| 进度与日志 | 实际阶段、标准输出与错误回调、退出码和取消收尾 |
| 窗口与 JVM 参数 | 已在 1.21.10 Fabric 配合 Java 25 上验收 |
| 单文件下载 | 已验收：流式下载、大小与 SHA-1 校验、已有文件复用 |
| 多文件并发 | 已验收：可配置并发上限、总体字节进度、再次全部复用 |
| 有限重试 | 已验收：HTTP 503 后恢复、达到次数上限停止 |
| 下载速度与剩余时间 | 已验收：单文件、混合复用批次与全部复用样本 |
| 游戏资源补全 | 已验收：索引、对象、日志配置、校验复用与保留备份后修复 |
| 游戏文件补全 | 已提供 GameFileCompleter；新版样本全量校验与再次复用已验收 |
| 原版安装与在线版本列表 | 已提供 MinecraftInstaller；原版 1.21.10 安装并启动、列表查询已验收 |
| 可选下载源 | 默认官方；BMCLAPI 原版安装与启动已验收 |
| 旧式原生库下载声明 | 已接入统一清单；原版 1.8.9 元数据验收通过 |
| Java 安装 | 已提供 JavaInstaller；Windows x64 Java 25 安装及用于启动游戏已验收 |
| Microsoft 正版登录 | 设备代码登录、玩家档案、游戏启动与内存缓存恢复已验收；Client ID 和缓存存储由启动器作者提供 |
| 外置验证 | 当前开发 Yggdrasil，计划使用 LittleSkin 验收 |
| 加载器安装、资源平台与整合包 | 待开发 |

实际启动样本包括已安装的原版 1.21.10 配合 x64 Java 25、1.8.9 原版配合 x64 Java 8，以及 1.21.10 Fabric 0.17.3 配合 x64 Java 25；分组入口在后者上验收。这不代表所有版本、加载器或架构都已验证。新版参数搭配旧式 natives 映射目前仍不支持。

## 获取与引用

使用 Windows 10/11、.NET 10 SDK 和支持 C# 14 的开发环境。在启动器项目中引用本仓库的 `FMCore/FMCore.csproj`。核心解决方案为 `FMCore.slnx`。

本分支采用源码项目引用。1.x NuGet 包的 API 与本文不同。

## 文档导航

| 主题 | 内容 |
| --- | --- |
| [原版安装](docs/installation.md) | 在线版本列表、安装、下载源、进度与取消 |
| [游戏启动](docs/launch.md) | 最简调用、分组配置、方法参数、进度、控制台和按钮示例 |
| [游戏文件补全](docs/game-files.md) | 客户端、依赖、资源与日志的一次异步调用 |
| [资源补全](docs/resources.md) | 仅资源的简单调用、阶段进度、结果与支持范围 |
| [文件下载](docs/downloads.md) | 单文件、并发批次、进度、校验、复用与失败清理 |
| [公开 API](docs/public-api.md) | 当前公开类型、签名、参数和返回 |
| [版本查询](docs/versions.md) | 列表、重名、继承链与入口 |
| [Java 安装](docs/java-installation.md) | 指定大版本和目录，下载校验后返回 Java 路径 |
| [Java 查询](docs/java.md) | 路径、版本、架构和候选发现 |
| [账号](docs/accounts.md) | 统一启动账号、离线身份与正版登录入口 |
| [Microsoft 登录](docs/microsoft-login.md) | 作者提供 Client ID，授权后直接用于启动 |
| [异步与错误处理](docs/advanced.md) | 取消、回调线程、退出与收尾错误 |
| [源码结构](docs/architecture.md) | 简单入口和底层流程的关系 |
| [独立示例](examples/README.md) | 查询与分组配置打印示例 |

[开发路线](docs/roadmap.md)、[阶段记录](docs/stages/stage-02.md)、[迁移说明](docs/migration-to-csharp.md)和[变更记录](CHANGELOG.md)说明规划与历史。当前功能以实际源码、公开 API 和功能手册为准。

## 数据与执行约定

Java、账号、内存和位置由调用方选择。默认工作目录为 `versions/<实际版本目录名>`，公共 `assets`、`libraries` 可共享。父版本不跨 Minecraft 根目录查找。

查询保持只读。启动会创建必要目录、准备原生库并执行 Java；当前游戏启动流程不会自动下载缺失文件。Java 搜索不默认全盘递归。本地 CLI、测试、游戏测试数据和内部约定不作为公开 SDK 分发。

## 许可证

采用 [MIT License](LICENSE)。
