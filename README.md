# FocusMinecraftCore · FMCore

面向 Minecraft Java 版启动器开发者的 C# 核心类库。启动器填写账号、游戏和 Java 设置，FMCore 完成版本读取、Java 探测、参数准备、进程与日志处理。

> 当前为 `v2.0` 开发分支，使用 .NET 10 和 C# 14。分组配置与统一启动入口已通过真实游戏验收；下载与资源补全进入第 3 阶段开发，尚无已验收的下载入口。开发版尚未发布 NuGet，接口与已发布的 1.x 不同。

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
| 下载、安装与资源补全 | 第 3 阶段开发中，尚未交付 |
| Java 安装、Microsoft 和外置验证 | 待开发 |
| 加载器安装、资源平台与整合包 | 待开发 |

实际启动样本为 1.8.9 原版配合 x64 Java 8，以及 1.21.10 Fabric 0.17.3 配合 x64 Java 25；分组入口在后者上验收。这不代表所有版本、加载器或架构都已验证。新版参数搭配旧式 natives 映射目前仍不支持。

## 获取与引用

使用 Windows 10/11、.NET 10 SDK 和支持 C# 14 的开发环境。在启动器项目中引用本仓库的 `FMCore/FMCore.csproj`。核心解决方案为 `FMCore.slnx`。

本分支采用源码项目引用。1.x NuGet 包的 API 与本文不同。

## 文档导航

| 主题 | 内容 |
| --- | --- |
| [游戏启动](docs/launch.md) | 最简调用、分组配置、方法参数、进度、控制台和按钮示例 |
| [公开 API](docs/public-api.md) | 当前公开类型、签名、参数和返回 |
| [版本查询](docs/versions.md) | 列表、重名、继承链与入口 |
| [Java 查询](docs/java.md) | 路径、版本、架构和候选发现 |
| [离线账号](docs/accounts.md) | 用户名、UUID 和认证边界 |
| [异步与错误处理](docs/advanced.md) | 取消、回调线程、退出与收尾错误 |
| [源码结构](docs/architecture.md) | 简单入口和底层流程的关系 |
| [独立示例](examples/README.md) | 查询与已解析配置示例；包含当前同步提示 |

[开发路线](docs/roadmap.md)、[阶段记录](docs/stages/stage-02.md)、[迁移说明](docs/migration-to-csharp.md)和[变更记录](CHANGELOG.md)说明规划与历史。当前功能以实际源码、公开 API 和功能手册为准。

## 数据与执行约定

Java、账号、内存和位置由调用方选择。默认工作目录为 `versions/<实际版本目录名>`，公共 `assets`、`libraries` 可共享。父版本不跨 Minecraft 根目录查找。

查询保持只读。启动会创建必要目录、准备原生库并执行 Java；当前不会自动下载缺失文件。Java 搜索不默认全盘递归。本地 CLI、测试、游戏测试数据和内部约定不作为公开 SDK 分发。

## 许可证

采用 [MIT License](LICENSE)。
