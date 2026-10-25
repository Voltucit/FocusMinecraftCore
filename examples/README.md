# 独立 C# 调用示例

每个目录是独立控制台项目，只引用 FMCore 核心类库。环境：.NET 10 SDK、C# 14、Windows 10/11。从仓库根目录执行，路径参数请替换为实际值。

| 项目 | 功能 | 参数 |
| --- | --- | --- |
| ListVersions | 查询版本列表 | [Minecraft根目录] |
| GetVersion | 查询一个版本 | 版本ID [Minecraft根目录] |
| InheritanceChain | 读取继承链 | 版本ID [Minecraft根目录] |
| ResolveMainClass | 解析最终入口 | 版本ID [Minecraft根目录] |
| ReadJavaInfo | 探测指定 Java | java.exe完整路径 |
| ListJavas | 查询可用 Java | [额外搜索目录...] |
| CreateOfflineAccount | 创建离线身份 | 用户名 |
| ShowLaunchConfig | 组合并打印配置 | 版本ID java.exe路径 用户名 内存MB [Minecraft根目录] [工作目录] |

统一编译：

```powershell
dotnet build .\examples\Examples.slnx -c Release
```

不依赖本机游戏即可运行的离线身份示例：

```powershell
dotnet run --project .\examples\CreateOfflineAccount -- Steve
```

默认查询示例：

```powershell
dotnet run --project .\examples\ListVersions
dotnet run --project .\examples\ListJavas
```

其他示例按表格传入参数；带空格的路径需加引号。完整源代码和项目引用都在各目录中，无需本地 CLI 或测试项目。

GetVersion 按 JSON ID 查询，重名时应指定根目录。继承解析需要完整父版本集合。ShowLaunchConfig 不启动游戏，不创建实例。配置的账号、Java 和内存由参数明确提供，不使用 SDK 自动推荐值。

异步取消和错误处理见[高级使用指南](../docs/advanced.md)。完整可运行的 Minecraft 启动器将在第 2 阶段真正完成启动后提供，当前不放置虚构启动 API。
