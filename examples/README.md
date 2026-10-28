# 使用示例

先看最短调用，再按需要加进度或取消。下面的 Markdown 示例可放入已经引用 FMCore 的项目；标明的变量来自启动器界面或已有设置。

| 想做什么 | 示例 |
| --- | --- |
| 安装后启动、补全后启动、接到按钮 | [组合使用](launcher-workflow.md) |
| 检查文件 | [文件检查](file-check.md) |
| 检查启动设置 | [启动检查](launch-check.md) |
| 安装 Fabric | [Fabric](fabric-installation.md) |
| 安装 Forge | [Forge](forge-installation.md) |
| 安装 NeoForge | [NeoForge](neoforge-installation.md) |
| 安装 OptiFine | [OptiFine](optifine-installation.md) |
| 外置账号启动 | [外置验证](yggdrasil-launch.md) |
| 搜索和下载社区资源 | [Modrinth](modrinth-resources.md) |

## 已有的独立小项目

这几个项目适合查看一个功能的完整控制台代码，不需要另外建测试工程：

| 项目 | 用途 |
| --- | --- |
| [ListVersions](ListVersions/Program.cs) | 列出本地版本 |
| [GetVersion](GetVersion/Program.cs) | 查询指定版本 |
| [InheritanceChain](InheritanceChain/Program.cs) | 查看父版本 |
| [ResolveMainClass](ResolveMainClass/Program.cs) | 读取游戏入口 |
| [ReadJavaInfo](ReadJavaInfo/Program.cs) | 查看指定 Java |
| [ListJavas](ListJavas/Program.cs) | 查找可用 Java |
| [CreateOfflineAccount](CreateOfflineAccount/Program.cs) | 创建离线身份 |
| [ShowLaunchConfig](ShowLaunchConfig/Program.cs) | 展示已填写的启动设置 |

例如在仓库根目录运行：

```powershell
dotnet run --project examples/ListVersions -- ".minecraft"
```

各项目的参数要求见它自己的 `Program.cs`。`ShowLaunchConfig` 只展示设置，不会启动游戏。生成全部小项目可运行 `dotnet build examples/Examples.slnx -c Release`。

[文档目录](../docs/README.md) · [进度与取消](../docs/advanced.md) · [接口参考](../docs/public-api.md)
