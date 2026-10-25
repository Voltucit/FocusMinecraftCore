# 启动配置

当前只提供配置对象，尚未提供真正启动 Minecraft 的入口。完整依赖合并、原生库、启动参数、进程和日志仍在第 2 阶段继续开发。

## 组合显式选择的设置

```csharp
using FMCore.Accounts;
using FMCore.Java;
using FMCore.Launch;
using FMCore.Versions;

if (args.Length != 4)
    throw new ArgumentException("参数：Minecraft根目录、版本ID、java.exe路径、用户名。");
MinecraftVersion version = new MinecraftVersionReader().GetVersion(args[1], args[0]);
JavaInfo java = await JavaReader.GetJavaInfoAsync(args[2]);
OfflineAccount account = OfflineAccount.Create(args[3]);
LaunchConfig config = new(version, java, account, maxMemoryMb: 2048);
Console.WriteLine(config.GameDirectory);
```

2048 是示例中开发者明确选择的内存，不是 SDK 自动分配值。MaxMemoryMb 必须大于 0，不自动推荐账号、Java 或内存。

默认 GameDirectory 是 version.Directory。通过构造器的 gameDirectory 参数可以指定其他实例目录或现有非隔离目录；不会更改启动版本 ID。规划中的新建隔离实例采用 `根目录/versions/<版本或实例名称>/`，公共 assets、libraries 保持共享。当前构造配置不创建目录，也未实现实例安装或重名事务。

参数对象为空、内存非正、目录空白和已有文件占用目录均有明确异常。配置属性只读；需要调整时重新构造配置。与 Python 可变数据类相比，这是有意采用的 C# 配置校验方式。

[独立完整配置示例](../examples/ShowLaunchConfig/Program.cs)接受版本、Java、账号、内存和可选根目录/工作目录，实际调用全部通过公开 SDK。它只打印配置，不会启动游戏。
