# 独立功能示例

每个 `.cs` 是完整独立控制台入口，包含 using、参数和调用。一次只编译/运行一个功能，不引用 CLI、测试或内部 API。基础示例省略可选配置，高级示例按需加入进度、取消和错误处理。

要求 Windows 10/11、.NET 10 SDK、C# 14。在仓库根目录运行：

```powershell
dotnet build docs/examples/FMCore.Examples.csproj -c Release -p:Example=download-file
dotnet run --project docs/examples/FMCore.Examples.csproj -p:Example=download-file
```

`Example` 改为下表文件名（无扩展名）。FMCORE_* 是示例需要的环境变量，应设置真实值。安装/更新/删除会修改选定目录；编译不等于执行在线功能。通用异常、进度与资源生命周期见[高级指南](advanced.md)。

## downloader

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 单文件直接下载 | [download-file.cs](examples/download-file.cs) | FMCORE_URL 为合法 HTTPS 文件。无校验元数据时每次重新下载。 |
| 校验、进度与取消（高级） | [download-advanced.cs](examples/download-advanced.cs) | FMCORE_SHA256 来自可信元数据；不把 URL 写入日志。 |
| 批量下载 | [download-batch.cs](examples/download-batch.cs) | 按输入顺序返回，各文件独立提交。 |
| 仅校验本地文件 | [download-verify.cs](examples/download-verify.cs) | 不联网、不写入；示例 URL 仅标识来源，Unverified 不等于有效。 |
| 已有根目录直接下载 URL | [download-relative.cs](examples/download-relative.cs) | 目标始终为根内相对路径，复用同一个下载器的并发限制。 |

## versions

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 官方可用版本 | [versions-available.cs](examples/versions-available.cs) | 返回 VersionManifest；不安装游戏。 |
| 本地版本列表 | [versions-installed.cs](examples/versions-installed.cs) | 返回 GameCoreInfo 列表；HasClientJar 不证明完整安装。 |
| 指定本地版本 | [versions-info.cs](examples/versions-info.cs) | 需要本地元数据。 |
| 继承解析 | [versions-resolve.cs](examples/versions-resolve.cs) | ResolveAsync 合并继承，GetVersionAsync 保留单个 profile。 |
| 原始版本元数据 | [versions-profile.cs](examples/versions-profile.cs) | ResolveAsync 合并继承，GetVersionAsync 保留单个 profile。 |
| 保存本地元数据 | [versions-save.cs](examples/versions-save.cs) | 不安装或信任加载器文件。 |
| 缓存刷新与取消（高级） | [versions-advanced.cs](examples/versions-advanced.cs) | CacheOnly 不联网；Refresh 强制刷新。 |

## java

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 本机 Java 列表 | [java-list.cs](examples/java-list.cs) | 实际有界运行探测，返回现有 JavaRuntime，不下载。 |
| 探测指定 Java | [java-info.cs](examples/java-info.cs) | FMCORE_JAVA 是可执行文件、home 或 bin。 |
| 检查用户选择的 Java | [java-select.cs](examples/java-select.cs) | 显式 Java 必须兼容元数据；不替宿主决定主版本。 |
| 扫描边界、进度与问题（高级） | [java-advanced.cs](examples/java-advanced.cs) | 检测不会自动下载 Java。 |

## java-runtime

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 可下载 Runtime 查询 | [runtime-available.cs](examples/runtime-available.cs) | 明确查询 Java 21；不安装。 |
| 按主版本安装 | [runtime-install.cs](examples/runtime-install.cs) | 示例明确选择 21；返回原有 JavaRuntime，不修改系统 Java。 |
| 安装用户选中的精确包 | [runtime-exact.cs](examples/runtime-exact.cs) | 精确对象包含厂商、版本与校验值，不重建下载 URL。 |
| Runtime 高级安装（高级） | [runtime-advanced.cs](examples/runtime-advanced.cs) | 仅 Windows；Zulu 大小可能近似，SHA-256 始终严格校验。 |

## installer

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 原版安装 | [game-install.cs](examples/game-install.cs) | 安装内部处理依赖；校验不含 Mod/存档；计划不下载全部游戏文件。 |
| 安装计划 | [game-plan.cs](examples/game-plan.cs) | 安装内部处理依赖；校验不含 Mod/存档；计划不下载全部游戏文件。 |
| 安装进度与取消（高级） | [game-install-advanced.cs](examples/game-install-advanced.cs) | 其他模块异常处理见高级指南。 |

## integrity

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 完整性校验 | [game-verify.cs](examples/game-verify.cs) | 安装内部处理依赖；校验不含 Mod/存档；计划不下载全部游戏文件。 |
| 文件修复 | [game-repair.cs](examples/game-repair.cs) | 安装内部处理依赖；校验不含 Mod/存档；计划不下载全部游戏文件。 |
| 高级校验范围（高级） | [game-verify-advanced.cs](examples/game-verify-advanced.cs) | 缺索引时不宣称完整校验通过。 |

## authentication

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 离线登录 | [accounts-offline.cs](examples/accounts-offline.cs) | LaunchIdentity 可直接传入启动配置。 |
| 正版设备码登录 | [accounts-microsoft.cs](examples/accounts-microsoft.cs) | 需要合法应用注册、服务访问权限与正版账号；必须展示授权步骤，不打印令牌。 |
| 账号列表 | [accounts-list.cs](examples/accounts-list.cs) | 仅公开资料。 |
| 切换当前账号 | [accounts-switch.cs](examples/accounts-switch.cs) | FMCORE_ACCOUNT_ID 为保存的账号；删除不撤销服务器授权。 |
| 持久化默认账号 | [accounts-default.cs](examples/accounts-default.cs) | FMCORE_ACCOUNT_ID 为保存的账号；删除不撤销服务器授权。 |
| 刷新身份 | [accounts-refresh.cs](examples/accounts-refresh.cs) | FMCORE_ACCOUNT_ID 为保存的账号；删除不撤销服务器授权。 |
| 获取启动身份 | [accounts-identity.cs](examples/accounts-identity.cs) | FMCORE_ACCOUNT_ID 为保存的账号；删除不撤销服务器授权。 |
| 删除本地账号 | [accounts-delete.cs](examples/accounts-delete.cs) | FMCORE_ACCOUNT_ID 为保存的账号；删除不撤销服务器授权。 |
| 授权进度与取消（高级） | [accounts-advanced.cs](examples/accounts-advanced.cs) | 回调迅速返回；不自动降级离线。 |

## fabric

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 查询加载器版本 | [fabric-versions.cs](examples/fabric-versions.cs) | 查询不执行安装器。 |
| 安装加载器 | [fabric-install.cs](examples/fabric-install.cs) | VersionId 复用原启动接口；官方安装器需要本机兼容 Java。 |
| 安装高级配置（高级） | [fabric-advanced.cs](examples/fabric-advanced.cs) | 依赖/Java/网络错误保留原异常类型。 |
| 支持的游戏版本 | [fabric-games.cs](examples/fabric-games.cs) | 参数和返回值见模块手册；取消默认不取消。 |

## forge

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 查询加载器版本 | [forge-versions.cs](examples/forge-versions.cs) | 查询不执行安装器。 |
| 安装加载器 | [forge-install.cs](examples/forge-install.cs) | VersionId 复用原启动接口；官方安装器需要本机兼容 Java。 |
| 安装高级配置（高级） | [forge-advanced.cs](examples/forge-advanced.cs) | 依赖/Java/网络错误保留原异常类型。 |

## neoforge

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 查询加载器版本 | [neoforge-versions.cs](examples/neoforge-versions.cs) | 查询不执行安装器。 |
| 安装加载器 | [neoforge-install.cs](examples/neoforge-install.cs) | VersionId 复用原启动接口；官方安装器需要本机兼容 Java。 |
| 安装高级配置（高级） | [neoforge-advanced.cs](examples/neoforge-advanced.cs) | 依赖/Java/网络错误保留原异常类型。 |

## optifine

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 查询加载器版本 | [optifine-versions.cs](examples/optifine-versions.cs) | 查询不执行安装器。 |
| 安装加载器 | [optifine-install.cs](examples/optifine-install.cs) | VersionId 复用原启动接口；官方安装器需要本机兼容 Java。 |
| 安装高级配置（高级） | [optifine-advanced.cs](examples/optifine-advanced.cs) | 依赖/Java/网络错误保留原异常类型。 |
| 使用合法本地安装器 | [optifine-local.cs](examples/optifine-local.cs) | 从官方合法取得原始 JAR，不绕过限制。 |

## instances

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 创建实例 | [instances-create.cs](examples/instances-create.cs) | 创建配置，不安装游戏。 |
| 列举实例 | [instances-list.cs](examples/instances-list.cs) | 参数和返回值见模块手册；取消默认不取消。 |
| 读取实例 | [instances-info.cs](examples/instances-info.cs) | FMCORE_INSTANCE 是托管实例 ID。 |
| 更新实例快照 | [instances-update.cs](examples/instances-update.cs) | 过时修订会报 Conflict，重新读取后由用户决定重试。 |
| 删除指定实例 | [instances-delete.cs](examples/instances-delete.cs) | 删除实例存档/配置/Mod，执行前自行备份；不删除共享文件。 |
| 高级实例配置（高级） | [instances-advanced.cs](examples/instances-advanced.cs) | 2048 MiB 是示例明确选择，不是推荐算法。 |

## modrinth

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 资源搜索 | [modrinth-search.cs](examples/modrinth-search.cs) | 搜索结果需再验证文件兼容性；CurseForge 需要合法 Key。 |
| 项目资料 | [modrinth-project.cs](examples/modrinth-project.cs) | 参数和返回值见模块手册；取消默认不取消。 |
| 筛选文件版本 | [modrinth-versions.cs](examples/modrinth-versions.cs) | CurseForge 是单页，按 Index/ResultCount 翻页；安装器内部遍历全部页。 |
| 来源清单 | [modrinth-installed.cs](examples/modrinth-installed.cs) | FMCORE_INSTANCE 必须已有对应加载器；包内内容使用 ModpackInstaller。 |
| 批量检查更新 | [modrinth-check.cs](examples/modrinth-check.cs) | FMCORE_INSTANCE 必须已有对应加载器；包内内容使用 ModpackInstaller。 |
| 安装一个 Mod 及必要依赖 | [modrinth-install.cs](examples/modrinth-install.cs) | FMCORE_INSTANCE 必须已有对应加载器；包内内容使用 ModpackInstaller。 |
| 更新单个 Mod | [modrinth-update.cs](examples/modrinth-update.cs) | FMCORE_INSTANCE 必须已有对应加载器；包内内容使用 ModpackInstaller。 |
| 单 Mod 更新进度与取消（高级） | [modrinth-advanced.cs](examples/modrinth-advanced.cs) | 没有跨 Mod 原子批量更新；逐项更新各自提交，后项失败不撤销前项。 |
| 安装明确选中的文件（高级） | [modrinth-install-exact.cs](examples/modrinth-install-exact.cs) | 先查询，再把选中的版本 ID / 文件 ID 传入；安装仍验证环境和依赖。 |

## curseforge

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 资源搜索 | [curseforge-search.cs](examples/curseforge-search.cs) | 搜索结果需再验证文件兼容性；CurseForge 需要合法 Key。 |
| 项目资料 | [curseforge-project.cs](examples/curseforge-project.cs) | 参数和返回值见模块手册；取消默认不取消。 |
| 筛选文件版本 | [curseforge-versions.cs](examples/curseforge-versions.cs) | CurseForge 是单页，按 Index/ResultCount 翻页；安装器内部遍历全部页。 |
| 来源清单 | [curseforge-installed.cs](examples/curseforge-installed.cs) | FMCORE_INSTANCE 必须已有对应加载器；包内内容使用 ModpackInstaller。 |
| 批量检查更新 | [curseforge-check.cs](examples/curseforge-check.cs) | FMCORE_INSTANCE 必须已有对应加载器；包内内容使用 ModpackInstaller。 |
| 安装一个 Mod 及必要依赖 | [curseforge-install.cs](examples/curseforge-install.cs) | FMCORE_INSTANCE 必须已有对应加载器；包内内容使用 ModpackInstaller。 |
| 更新单个 Mod | [curseforge-update.cs](examples/curseforge-update.cs) | FMCORE_INSTANCE 必须已有对应加载器；包内内容使用 ModpackInstaller。 |
| 单 Mod 更新进度与取消（高级） | [curseforge-advanced.cs](examples/curseforge-advanced.cs) | 没有跨 Mod 原子批量更新；逐项更新各自提交，后项失败不撤销前项。 |
| 指定文件资料 | [curseforge-file.cs](examples/curseforge-file.cs) | 文件必须属于项目 306612；不打印签名 URL。 |
| 安装明确选中的文件（高级） | [curseforge-install-exact.cs](examples/curseforge-install-exact.cs) | 先查询，再把选中的版本 ID / 文件 ID 传入；安装仍验证环境和依赖。 |

## modpacks

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 只解析整合包 | [pack-read.cs](examples/pack-read.cs) | 支持 mrpack v1 和 CurseForge manifest v1；不提取安装。 |
| 导入整包 | [pack-import.cs](examples/pack-import.cs) | 默认完整 Runtime；CurseForge 包需配置合法平台客户端。 |
| 安装整包 | [pack-install.cs](examples/pack-install.cs) | 默认完整 Runtime；CurseForge 包需配置合法平台客户端。 |
| 整包更新计划 | [pack-plan.cs](examples/pack-plan.cs) | 默认完整 Runtime；CurseForge 包需配置合法平台客户端。 |
| 整包事务更新 | [pack-update.cs](examples/pack-update.cs) | 默认完整 Runtime；CurseForge 包需配置合法平台客户端。 |
| 读取整包状态 | [pack-installed.cs](examples/pack-installed.cs) | 参数和返回值见模块手册；取消默认不取消。 |
| 导出整包引用 | [pack-export.cs](examples/pack-export.cs) | 默认不导出个人文件；二进制 override 需合法再分发许可。 |
| 确认正常使用 | [pack-confirm.cs](examples/pack-confirm.cs) | 更新 ID 来自原结果；确认只在用户验证正常使用之后；清理后不可回滚。 |
| 清理已确认备份 | [pack-cleanup.cs](examples/pack-cleanup.cs) | 更新 ID 来自原结果；确认只在用户验证正常使用之后；清理后不可回滚。 |
| 主动回滚整包 | [pack-rollback.cs](examples/pack-rollback.cs) | 更新 ID 来自原结果；确认只在用户验证正常使用之后；清理后不可回滚。 |
| 包内 Mod 批量检查 | [pack-check-mods.cs](examples/pack-check-mods.cs) | 只检查，与整包更新入口独立。 |
| 授权更新包内单 Mod | [pack-update-mod.cs](examples/pack-update-mod.cs) | authorize 代表宿主已经取得授权；仍使用整包事务。 |
| 整包高级更新（高级） | [pack-advanced.cs](examples/pack-advanced.cs) | 不要自行操作控制目录或清理事务。 |
| 导入 CurseForge 整包 | [pack-curseforge-import.cs](examples/pack-curseforge-import.cs) | 合法 Key 仅用于元数据，下载连接不携带 Key；不绕过分发限制。 |
| 整包验证启动器 | [pack-validation-launcher.cs](examples/pack-validation-launcher.cs) | 使用包已有实例配置；启动后不自动确认，需用户确认正常使用。 |

## logging

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 写入脱敏日志 | [logs-write.cs](examples/logs-write.cs) | 参数和返回值见模块手册；取消默认不取消。 |
| 日志独立导出 | [logs-save.cs](examples/logs-save.cs) | 只有显式调用才写文件。 |
| 诊断已有日志 | [logs-analyze.cs](examples/logs-analyze.cs) | 日志缓存有界；诊断不凭退出码断言根因。 |
| 容量、过滤与事件（高级） | [logs-advanced.cs](examples/logs-advanced.cs) | 已知秘密可显式传入；回调应简短。 |

## launch

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 启动与等待退出 | [launch-minimal.cs](examples/launch-minimal.cs) | Java 与 2048 MiB 为显式选择；联网补齐游戏文件；Running 只表示创建进程。 |
| 状态、日志、退出与取消（高级） | [launch-advanced.cs](examples/launch-advanced.cs) | 等待取消不等于停止；本例显式 StopAsync，await using 释放。 |
| 启动已有实例 | [launch-instance.cs](examples/launch-instance.cs) | 使用实例已有的 Java/内存设置，过时快照会失败。 |

## advanced-launch

| 功能 | 完整代码 | 参数、返回值与条件 |
| --- | --- | --- |
| 窗口与全屏 | [settings-window.cs](examples/settings-window.cs) | 配置示例；全屏实际模式由游戏和显示器决定。 完整启动见 launch-minimal。 |
| 内存与 JVM 参数 | [settings-memory.cs](examples/settings-memory.cs) | 0 省略 Xms；内存由宿主决定。 完整启动见 launch-minimal。 |
| 服务器地址 | [settings-server.cs](examples/settings-server.cs) | 替换实际服务器；游戏负责连接及认证。 完整启动见 launch-minimal。 |
| 渐进配置启动（高级） | [settings-advanced.cs](examples/settings-advanced.cs) | 高级选项沿用原有配置与校验。 |
| 自定义游戏参数 | [settings-game-arguments.cs](examples/settings-game-arguments.cs) | 每项一个 token；不允许覆盖身份、版本或路径。这里只配置，不运行。 |
