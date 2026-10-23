# 实例管理

本手册对应当前源码。目录布局调整尚未发布，已发布包请以对应 Tag 的文档为准。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Instances;

var instance = await new InstanceManager(".minecraft").CreateAsync("我的实例", "1.16.5");
Console.WriteLine(instance.Id);
```


命名空间：`FMCore.Instances`。1.0.0 包含本模块。使用 FocusMinecraftCore 1.0.0 包或引用 `FMCore/FMCore.csproj`。

`InstanceManager` 只管理自己创建的实例，不联网、不安装版本、不保存账号。每个实例的游戏目录固定为 `<root>/versions/<id>`，版本、Libraries、Assets 和下载缓存仍位于共享根目录。默认实例目录名取启动版本 ID；传入不同自定义目录名可为同一版本创建多个实例。显示名称和后续 VersionId 更新均不改变目录名。删除游戏数据会保留同目录版本 JSON、客户端 JAR 和 natives。

## 最简调用：创建、列表、读取、修改与删除

下面的完整控制台示例仅创建并删除一个新的空实例，不启动游戏：

独立、可编译的调用代码见[本模块示例](examples.md#instances)。

所有异步操作均支持 `CancellationToken`。GUI 使用 `await`，不要在 UI 线程同步等待；`Progress<T>` 可将删除进度投递回创建它时的同步上下文。删除会真正移除该实例的存档、Mod、设置、游戏日志和实例配置，调用方应在用户确认后调用。

## 公开 API

| API | 参数和返回值 | 语义 |
| --- | --- | --- |
| `new InstanceManager(rootDirectory = ".minecraft")` | 共享根目录 | 转为绝对路径，拒绝链接；构造时不创建目录，无需释放管理器 |
| `CreateAsync(name, versionId, cancellationToken)` | 显示名称、版本 ID → `InstanceInfo` | 使用默认设置，目录名默认取 versionId；同名实例拒绝覆盖 |
| `CreateAsync(config, instanceId = null, cancellationToken)` | `InstanceConfig`、可选目录 ID → `InstanceInfo` | 高级配置；使用 `new InstanceConfig { ... }` 明确类型，避免与字符串重载的类型推断歧义 |
| `GetInstancesAsync(cancellationToken)` | → `IReadOnlyList<InstanceInfo>` | 按 ID 排序，仅枚举具有合法实例配置的直接子目录，不创建缺失目录 |
| `GetAsync(instanceId, cancellationToken)` | → `InstanceInfo` | 读取配置及占用状态快照；不会联网或修改文件 |
| `UpdateAsync(instance, config, cancellationToken)` | 已读取的 `InstanceInfo`、新配置 → 新 `InstanceInfo` | 校验 Revision 后原子写入；旧快照不能覆盖更新后的配置 |
| `DeleteAsync(instanceId, progress = null, cancellationToken)` | ID、可选删除进度 | 仅删除该托管实例；禁止删除运行中、配置损坏或未托管目录 |
| `instance.CreateLaunchConfig(account = null)` | 可选 T05 `LaunchIdentity` → `LaunchConfig` | 自动绑定共享根、版本、独立目录及启动设置；也可在结果中设置 `Accounts` |

目录 ID 沿用安全版本标识符规则，长度 1–180，允许大小写、内部空格和中文；拒绝设备名、尾随点/空格、路径分隔符、路径穿越和 `.fmcore` 保留名称。未指定时使用启动版本 ID；同一启动版本的多个实例须传入不同的自定义目录名。名称支持中文，限制为 1–128 字符且不能空白或含控制字符；名称不会用作文件路径。不会覆盖同名托管实例或接管含未知游戏数据的目录；仅含合法同名版本 JSON、可选客户端 JAR/natives 的已安装版本目录可安全附加实例配置。

`InstanceInfo` 提供 `Id`、`RootDirectory`、`GameDirectory`、`Config`、`Revision`、`CreatedAt`、`UpdatedAt` 和 `IsInUse`。它是不可变快照；更新成功后应改用新返回值。`IsInUse` 表示读取时是否存在 SDK 启动、准备或互斥操作，不承诺随后仍空闲。

## 配置与持久化

| `InstanceConfig` 属性 | 默认值 | 作用 |
| --- | --- | --- |
| `Name` / `VersionId` | 空，创建时必须填写 | 显示名称和正式版、快照或已安装的加载器 profile ID；创建时只校验格式 |
| `JavaPath` | `null` | 手动 Java 优先；未填写则由启动模块按元数据选择 |
| `MinMemoryMb` / `MaxMemoryMb` | 512 / 2048 | JVM 堆内存（MiB）；最小为 0 时省略 Xms，非零时不得超过最大值 |
| `JvmArguments` | 空列表 | 每个元素为一个完整 JVM 参数，禁止覆盖内存、Classpath 或主类 |
| `GameArguments` | 空列表 | 每个元素为一个游戏参数；禁止覆盖身份、版本与共享目录 |
| `Window` | 854 × 480，非全屏 | 复用 `GameWindowConfig`；统一替换窗口默认参数 |
| `Server`（T13） | `null` | 可选 `GameServerConfig`，保存 Host / Port，端口默认 25565 |

配置位于 `versions/<id>/.fmcore/instance.json`，有明确格式版本、实例 ID 和修订号，通过同目录临时文件原子替换。JSON 不保存根目录、游戏目录、账号或令牌；实际路径始终由管理器推导，防止篡改配置后重定向删除。每份配置限制 1 MiB，拒绝未知字段、不支持的格式版本和无效配置。创建通过临时目录写好配置；新目录整体提交，已有合法版本目录只新增配置、不覆盖运行文件；未完成的暂存目录不会进入实例列表。

列表忽略未托管目录。若已有托管配置损坏，查询明确抛出 `StorageCorrupted`，不将损坏实例静默当作不存在；删除同样拒绝损坏配置，需先恢复原配置。旧版 `instances/<版本或实例名>` 不再作为活动目录；检测到旧目录时明确提示迁移，SDK 不自动搬动或覆盖数据。迁移步骤见[目录管理](directory-layout.md)。

## 高级配置与启动

完整示例使用离线身份；正版身份可用相同方式传入，也可设置 `Accounts` 由启动器刷新。账号存储位于实例之外，避免删除游戏实例时影响账号。

独立、可编译的调用代码见[本模块示例](examples.md#instances)。

Java 必须提前安装，本阶段不下载 Java；首次启动会按现有规则安装/修复共享游戏文件。需要修改探测策略、功能条件、缓存策略或关闭自动安装时，可对生成的 `LaunchConfig` 使用 `with` 设置已有属性；这些运行时覆盖不会自动写回实例配置。持久化修改请调用 `UpdateAsync`，不要序列化含账号的启动对象代替实例存储。

由实例生成的启动配置携带内部修订快照，启动器取得互斥锁后重新检查。实例已删除或配置已更新时，旧启动配置会失败；不会悄悄重建已删除目录，也不会使用过时版本。请重新 `GetAsync` 并创建启动配置。已有 T06 手工构造 `LaunchConfig` 的方式仍保留，新增互斥保护不改变其方法签名。

## 运行冲突与删除边界

- 稳定互斥文件位于共享根的 `.fmcore/instance-locks/`，按规范化游戏目录散列命名，不随实例删除；启动从准备阶段持有到游戏退出与资源清理。仍尊重旧的实例内部启动锁。
- 创建、修改和删除使用同一互斥机制。冲突立即报 `Busy`；不同实例可并行使用共享游戏文件。锁保护配合 FMCore 的操作，不扫描任意 Java 进程，不能代替操作系统目录权限或约束绕过 SDK 的外部写入。
- 删除前完整遍历，拒绝符号链接、目录联接和其他重解析点；不会递归跟随链接，也不会以配置中的路径作为删除目标。删除每个条目前再次检查边界，最终逐项删除而非递归删除共享根。
- 删除配置最后提交。遍历前取消不删除数据；删除中取消不回滚已删除文件，正常情况下保留配置供重新调用删除。最终清理遇到 I/O 失败可能留下需要检查的空目录，删除不是文件系统事务。
- 共享 `versions/`、`libraries/`、`assets/`、下载缓存、其他实例和目录外的账号均不会作为本实例的删除目标；使用者自行放进该实例的文件会随实例一起删除。

## 错误与验收

业务失败为 `InstanceException`，读取 `Code`：`InvalidConfig`、`UnsafePath`、`NotFound`、`AlreadyExists`、`Busy`、`Conflict`、`StorageCorrupted`、`IoError`；取消为 `OperationCanceledException`。异常不附带原始配置、参数、令牌或内部文件内容。

启动中的实例冲突通过 `LaunchResult.Error.Code = InstanceBusy` 报告；删除或更新导致的旧快照通过 `InvalidConfig` 和 `DetailCode = NotFound/Conflict` 报告。其他状态、日志、退出和停止仍使用[启动模块](launch.md)已有 API。

T07 在 Windows x64 使用临时目录、真实文件锁、独立 SDK 子进程和模拟游戏子进程验证持久化、隔离、共享文件保护、运行/准备冲突、并行实例、取消、修订冲突及链接边界。游戏元数据、下载和 Java 探测在启动集成测试中使用 Fixtures；这不等于重新完成真实 Minecraft 或正版认证验证。Linux/macOS 的实际文件系统和进程锁尚未实机验收。

T12 整合包正在等待确认时，InstanceManager.UpdateAsync 返回 Conflict，防止配置越过整包恢复点。整合包的安装/确认/回滚/清理与游戏共用同一实例锁，游戏运行时不能修改。通过 ModpackInstaller 确认或回滚后，重新读取实例快照即可继续；见 [整合包](modpacks.md)。

## T13 实例级高级设置

`MinMemoryMb = 0` 省略 Xms，默认 512 / 2048 MiB 保留；最小值可以小于最大值。`JvmArguments` 同值去重、冲突拒绝，窗口与服务器的手工重复选项必须与强类型值一致。`Server = null` 清除直连目标，其他有效目标经过地址/端口校验和规范化；保存、读取、修改后由 CreateLaunchConfig 自动带入。

旧配置缺少 Server 字段仍可读取，格式版本保持 1；新配置新增字段，不承诺旧 SDK 能反向读取。创建和修改失败为 InvalidConfig，读取已有冲突配置为 StorageCorrupted，不静默修正。原有运行锁、修订检查与整合包冲突保护继续生效。完整独立代码、范围和迁移说明见[高级启动设置](advanced-launch.md)。

## 独立调用示例

- [创建实例](examples/instances-create.cs)
- [列举实例](examples/instances-list.cs)
- [读取实例](examples/instances-info.cs)
- [更新实例快照](examples/instances-update.cs)
- [删除指定实例](examples/instances-delete.cs)
- [高级实例配置（高级）](examples/instances-advanced.cs)

目录名称、启动版本 ID、共享资源和旧目录迁移规则见[目录管理](directory-layout.md)。
