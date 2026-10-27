# 独立 C# 调用示例

各目录是独立控制台项目，只引用 FMCore 核心类库。环境为 .NET 10、C# 14、Windows 10/11。真实启动的最简调用采用 `LaunchConfig → MinecraftLauncher → LaunchAsync`，完整控制台入口见 [启动手册](../docs/launch.md#完整控制台示例)。

## 已有项目

| 项目 | 功能 | 参数 |
| --- | --- | --- |
| ListVersions | 查询版本列表 | [Minecraft根目录] |
| GetVersion | 查询一个版本 | 版本ID [Minecraft根目录] |
| InheritanceChain | 读取继承链 | 版本ID [Minecraft根目录] |
| ResolveMainClass | 解析最终入口 | 版本ID [Minecraft根目录] |
| ReadJavaInfo | 探测指定 Java | Java完整路径 |
| ListJavas | 查询候选 Java | [额外目录...] |
| CreateOfflineAccount | 离线身份 | 用户名 |
| ShowLaunchConfig | 打印分组启动配置 | 版本ID Java路径 用户名 内存MB [根目录] [工作目录] |

ShowLaunchConfig 使用分组 LaunchConfig 打印配置，不运行游戏。它不代替真实启动验收。原版安装与在线版本列表的完整调用见 [安装手册](../docs/installation.md)。

## 使用与核对

路径、版本、账号和内存由调用方提供；带空格的路径加引号。从仓库根目录可运行离线身份示例：

```powershell
dotnet run --project ./examples/CreateOfflineAccount -- Steve
```

开发者需要构建示例时使用：

```powershell
dotnet build ./examples/Examples.slnx -c Release
```

这不是每轮功能验收的固定要求；本轮验收继续通过现有 CLI。异步、取消和错误语义见 [高级说明](../docs/advanced.md)，实际类型签名见 [公开 API](../docs/public-api.md)。
