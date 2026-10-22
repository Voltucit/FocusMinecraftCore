# 高级使用指南

基础入口与高级配置复用同一业务实现。常用功能可省略进度、取消与选项；复杂操作按需传配置对象，不需要 DI、工厂或全局管理器。独立完整代码见[高级示例](examples.md)。

## 异步与进度

I/O 和进程操作优先调用 Async 方法，CancellationToken 默认不取消。同步 Java/GameCore 便捷查询会阻塞线程，GUI 使用异步版本。

| 操作 | 进度信息 |
| --- | --- |
| 下载 | DownloadProgress.State、RelativePath、BytesReceived、TotalBytes、Attempt |
| 安装/修复/检查 | InstallationProgress.Stage、CompletedFiles、TotalFiles、嵌套下载 |
| Java 检测 | JavaDetectionProgress.Completed、Total、ErrorCode |
| Java 安装 | JavaRuntimeProgress.Stage、文件计数、嵌套下载 |
| 正版认证 | AuthenticationProgress.Stage；等待用户授权不能伪造百分比 |
| 加载器 | FabricInstallationProgress / LoaderInstallationProgress 阶段与嵌套安装 |
| Mod | ResourceProgress.Stage、嵌套下载；解析阶段没有可信总量 |
| 整包 | ModpackProgress.Stage、文件计数、嵌套下载/安装 |
| 启动 | LaunchProgress.Stage、嵌套安装 |

阶段之间的完成量不一定累加；总量未知时显示状态或不确定进度。Progress<T> 捕获创建时的 SynchronizationContext，回调可能晚于任务完成；不要依赖“最后一次回调已到达”作为事务成功标志，以 await 返回结果为准。自定义 IProgress 回调应快速、线程安全、不抛异常；不要在日志/退出事件里同步等待启动器操作。

## 取消、事务与重试

传调用方的 CancellationToken；捕获 OperationCanceledException 与业务异常分别处理。取消下载会保留旧完整文件并清理本次临时文件；已成功共享文件可复用。原版安装可再次调用相同入口恢复，批次不等于整目录事务。

资源/整包/Runtime 有自己的锁、来源记录和恢复日志；下次操作执行恢复。遇到 RecoveryRequired 或冲突应保留数据并提示处理，不手工删除控制目录。实例删除可取消且可能部分删除，应读取状态后用原入口重试。

LaunchAsync 的取消作用于准备阶段；进程创建成功后，WaitForExitAsync 的取消只取消等待。要停止游戏，显式 await StopAsync()；await using 负责停止拥有的进程并释放锁/私有 native 目录。Running 不表示主菜单就绪，须结合游戏日志、退出结果和实际游戏状态。

## 错误处理

| 类别 | 获取方式 |
| --- | --- |
| 参数/配置 | 标准 ArgumentException 系列，或模块 InvalidRequest / InvalidConfig 等 Code |
| 网络/HTTP/超时 | DownloadException、VersionException、JavaException、ResourceException 的 Code 与可选 HTTP 状态 |
| 文件/路径 | 模块 IoError / UnsafePath / HashMismatch 等 Code |
| 认证 | AuthenticationException.Code / Stage；CurseForge 为 ResourceException.AuthenticationFailed |
| 业务/冲突/恢复 | LoaderException、InstanceException、ModpackException、ResourceException 的 Code |
| 启动准备 | LaunchAsync 返回 LaunchResult.Success / Error；非法配置或进程控制也可能抛 LaunchException |
| 游戏退出 | GameExitResult.ExitCode / StoppedByCaller / Diagnostics；退出码不等于根因 |

调用路径可能传播依赖模块的异常，例如加载器安装中的 JavaException、DownloadException；只捕获 LoaderException 不代表涵盖全部失败。完整处理应在宿主操作边界分别捕获取消、需要展示的模块异常、标准参数和文件异常。不要使用空 catch；对无法分类的异常显示通用提示，避免打印原始对象、请求 URL 或完整命令行。

## 资源生命周期与配置

AccountManager 用 using；MinecraftLauncher 用 await using。调用方传入的 HttpClient/handler/安全令牌存储仍由调用方持有，按对应契约释放，核心不会替调用方释放共享 HTTP 连接。CurseForgeClient 用 using；ModrinthClient 使用共享 HTTP，无 Dispose API。DownloadManager、VersionManager、GameInstaller 无需 using。

LaunchConfig.Create 直接消费已选账号与 Java 对象，并要求明确最大内存；使用 with 增量设置 Window、Server、Java.Arguments、GameArguments。原配置对象及默认值继续保留。每个自定义参数是一个 token，不拼接 Shell 字符串，参数去重与冲突校验由 SDK 执行。

需要代理时复用配置好的 HttpClient 构造 Downloader/平台客户端；账号 Key 不放入文件下载连接的默认头。Java 查询与安装分开，精确安装传同一个 JavaRuntimeVersion；SDK 不猜测 Minecraft 应使用的主版本。

## 敏感数据

只显示账号公开资料，LaunchIdentity 不当作可打印配置。不要记录 AccessToken、刷新令牌、Cookie、Authorization、签名 URL、API Key、完整启动参数。Windows 默认 DPAPI 绑定当前用户；自定义 IAccountTokenStore 必须按接口安全保存凭据。设备码属于必要的用户授权信息，只在当前授权界面展示。

Logger 在事件、缓存、诊断和导出之前过滤通用凭据字段和已知秘密；宿主自己的秘密可传入 sensitiveValues。该过滤不代替宿主对任意自定义文本的审查。日志有界且不自动导出，显式 SaveAsync 才写文件。

首次整包导入/安装同样返回待确认事务；确认用户正常使用（或明确回滚）之后，才能继续更新/修改。不能只按“是否替换旧包”判断是否需要确认。
