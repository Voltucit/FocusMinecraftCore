# 实例级高级启动设置（T13）

本手册对应 1.0.0。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Launch;

var config = new LaunchConfig { Window = new() { Width = 1280, Height = 720, Fullscreen = true } };
Console.WriteLine(config);
```


本手册 API 包含于 FocusMinecraftCore 1.0.0，支持 NuGet 包或 `FMCore/FMCore.csproj` 项目引用。面向 .NET 10 / C# 14，支持范围仅 Windows。

沿用 `FMCore.Launch.LaunchConfig`、`JavaConfig`、`GameWindowConfig` 和 `FMCore.Instances.InstanceConfig`，新增可选 `GameServerConfig`。无需新的设置管理器、DI 或手动拼接启动命令。

## 直接启动：完整独立示例

以下 `Program.cs` 使用当前公开 API。提前安装 Java 8；游戏为 1.16.5，首次安装需要联网。`Server` 是可选项，设为 `null` 即正常进入游戏；示例会尝试进入本机 25565 端口的服务器。

独立、可编译的调用代码见[本模块示例](examples.md#advanced-launch)。

正版身份以 `AccountManager` 返回的 `LaunchIdentity` 替换 `Account`，可另传 `Accounts` 自动刷新，见[账号](authentication.md)。自动加入服务器不授予正版资格、不绕过服务器认证或游戏的连接确认。`LaunchAsync` 成功只表示进程创建，不表示服务器连接成功；配置、日志、停止和 GUI 回调见[启动手册](launch.md)。

## 配置、默认值及优先级

| 配置 | 默认值 | 规则 |
| --- | --- | --- |
| `JavaConfig.MinMemoryMb` / `InstanceConfig.MinMemoryMb` | `512` | 单位 MiB；`0` 不生成 `-Xms`，正数生成一次；不得为负或超过最大值 |
| `MaxMemoryMb` | `2048` | 单位 MiB，范围 1–1048576；32 位 Java 仍限制最多 1536 MiB，探测后检查 |
| `JavaConfig.Arguments` / `InstanceConfig.JvmArguments` | `[]` | 每项为一个完整参数，带值选项使用 `=value`，不手动加 Shell 引号 |
| `Window.Width` / `Height` | `854` / `480` | 各为 1–16384 像素；全屏显示模式由游戏与显示器决定 |
| `Window.Fullscreen` | `false` | 为 true 时生成一次 `--fullscreen`，false 时不生成 |
| `LaunchConfig.Server` / `InstanceConfig.Server` | `null` | 无自动加入目标；非 null 使用下列主机与端口 |
| `GameServerConfig.Host` | `""`，需指定 | 域名、IPv4 或 IPv6；IPv6 可带方括号；不包含协议、路径、凭据或端口 |
| `GameServerConfig.Port` | `25565` | 范围 1–65535 |

`Host` 规范化为小写域名、IDN ASCII 或标准 IP 文本；本阶段不进行 DNS 查询或连接预检。无效地址的错误信息不会回显输入。`GameServerConfig.ToString()` 不输出服务器地址。

内存配置是唯一来源：移除版本元数据中的 `-Xms`、`-Xmx`、`-XX:InitialHeapSize=` 和 `-XX:MaxHeapSize=` 后，再生成配置值。自定义 JVM 参数不得再写这些选项或覆盖 Classpath、主类。省略 Xms 时 JVM 自行选择初始堆；上限只校验参数范围，不承诺物理内存足够。不执行智能推荐、自动分配或 Windows 内存清理。

JVM 参数处理常见冲突：相同完整参数去重；自定义 `-Dkey=value`、`-XX:Name=value`、`-XX:+/-Name`、`-Xss` 及常见 GC 选择覆盖元数据的同类默认值。自定义列表内部同一选项不同值、相反开关或多个不同的正向 GC 选择返回 `InvalidArguments`。例如 `-Dmode=a` 重复两次只生成一次，`-Dmode=a` 与 `-Dmode=b` 则失败。SDK 保留 UTF-8 输出约定，不接受改成其他编码。此检查不代替 Java 自身对所有 JVM 开关及其版本支持的校验；指定 Java 仍须匹配游戏元数据。

窗口和服务器优先使用强类型属性。元数据中的窗口默认值由配置替换；`GameArguments` 同时出现相同窗口值/服务器目标时去重，不同值报冲突。支持 `--key value` 与 `--key=value`；`--fullscreen=false` 与 `Window.Fullscreen=false` 对应不生成全屏选项。手工 `--server/--port` 或 `--quickPlayMultiplayer` 会规范化为统一目标，不能再指定不同目标或同时自动进入单人世界/Realms。

## 不同游戏版本的服务器参数

先读取继承后的真实版本元数据与 OS、架构、功能规则；以客户端来源版本判断旧版兼容性，不用加载器 profile 的自定义名字猜测。

| 元数据 / 客户端 | 生成结果 |
| --- | --- |
| 元数据声明且当前平台规则允许 `--quickPlayMultiplayer` | `--quickPlayMultiplayer host:port`；IPv6 为 `[host]:port` |
| 可识别的 1.6–1.19 正式版、相应 `-preN` / `-rcN`，或 13w16–23w13 范围的快照 | `--server host --port port` |
| 无 Quick Play 声明的未知格式、1.20+、1.5 及更早格式 | 有服务器目标时返回 `UnsupportedGameSettings`；不会静默忽略或猜测参数 |

Quick Play 从 23w14a 引入，1.20 移除了旧的 `--server/--port`；因此优先消费元数据声明。[Mojang 23w14a 说明](https://feedback.minecraft.net/hc/en-us/articles/14594878948237-Minecraft-Java-Edition-Snapshot-23w14a)、[1.20 发行说明](https://feedback.minecraft.net/hc/en-us/articles/16499677456781-Minecraft-Java-Edition-1-20-Trails-Tales)。这些资料说明参数行为，不表示 FMCore 已实际启动所有相应版本。

SDK 不为自动进入服务器开启可选 Quick Play 历史记录，不生成 `--quickPlayPath`，不会遗留未解析的占位符。窗口及 Quick Play 的受控 feature 由 SDK 决定，其他 `Features` 仍保留。不配置服务器时保持普通启动，不因未知的服务器语法报错。Java 堆开关含义见 [Oracle Java 命令文档](https://docs.oracle.com/en/java/javase/25/docs/specs/man/java.html)。

## 实例保存、读取和修改：独立示例

配置写入实例自己的 `.fmcore/instance.json`，游戏版本、Libraries、Assets 沿用共享根。不要序列化含账号的 `LaunchConfig` 作为实例存储。

独立、可编译的调用代码见[本模块示例](examples.md#advanced-launch)。

更新返回新修订快照；更新后重建启动器，不修改已创建启动器的配置。运行中或准备中的实例返回 `Busy`，旧快照返回 `Conflict`；整合包待确认事务继续禁止冲突修改，见[实例手册](instances.md)。`UpdateAsync` 支持取消，沿用原子配置写入和路径保护。

## 返回值、错误与兼容性

配置对象无 I/O。`MinecraftLauncher` 构造时检查内存、窗口与服务器等基础配置；无效内存/窗口返回 `LaunchException.InvalidConfig`，无效地址返回 `InvalidArguments`。准备中的参数冲突通过 `LaunchResult.Error.Code = InvalidArguments`，不支持的服务器语法通过 `UnsupportedGameSettings` 返回；取消为 `OperationCanceledException`。`InstanceManager` 的创建/修改返回 `InstanceInfo`，校验失败为 `InstanceException.InvalidConfig`，读取损坏配置为 `StorageCorrupted`；详见现有模块手册。

保留原有签名、整数类型及默认值；新增 `Server` 默认 null，旧 JSON 缺少此字段可继续读取。新配置会包含新增字段，旧 SDK 的严格 JSON 读取器未承诺能读取新配置；需要降级时先备份配置。新错误码追加于枚举末尾，原有数值不变。

行为收紧：原先追加冲突参数依赖 JVM/游戏决定最后值的用法现在拒绝；请把内存移入 `MinMemoryMb/MaxMemoryMb`，窗口移入 `Window`，服务器移入 `Server`，每个自定义 JVM 选项只保留一个值。旧配置本来含有冲突值时可能读取失败，需要由维护者修正；不静默改写已保存文件。

## 验收范围

Windows 自动化覆盖可省略/独立内存、范围、别名冲突、参数去重、窗口、地址安全、配置持久化、修订、官方真实格式的 1.16.5 / 1.19.4 / 23w14a / 1.20.4 元数据及 OS 规则。实际文件系统与子进程边界使用本地测试；Java 探测、下载和游戏进程在集成用例中使用模拟器。全量 878 项（T13 新增 59 项）通过。仅 PackageReference 的独立调用方通过实例持久化、修改、清除直连及两次模拟启动；50 段文档示例独立编译，未执行示例中的联网/登录。真实 Java 1.8.0_482 / 17.0.9 分别运行 SDK 捕获的两组堆参数，核对最大堆 256 MiB 及显式初始堆 64 MiB；省略 Xms 时不对 JVM 选出的初始值作固定承诺。公开与本地完整方案 Release 构建、核心/符号包生成均通过，原有公开签名全部保留；SourceLink 本地映射通过，未上传的源码不声称远程可取。

本阶段未新增真实 Minecraft 画面、全屏切换、服务器连接或正版认证验收；T06/T09 历史实机结果不替代 T13 的这些验证。没有将本阶段代码上传 NuGet 或开始自动安装 Java。

## 独立调用示例

- [窗口与全屏](examples/settings-window.cs)
- [内存与 JVM 参数](examples/settings-memory.cs)
- [服务器地址](examples/settings-server.cs)
- [渐进配置启动（高级）](examples/settings-advanced.cs)
- [自定义游戏参数](examples/settings-game-arguments.cs)
