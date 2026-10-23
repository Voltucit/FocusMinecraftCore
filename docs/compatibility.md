# 第三方 Minecraft 目录与通用版本兼容

本页对应 **1.0.2 维护候选**，尚未公开发布。1.0.1 的安装命令仍可安装已有正式版，但使用本文新增属性需要当前源码或本地 1.0.2 包。

## 只读导入已有目录

宿主明确传入 Minecraft 根目录，调用现有 GameCoreUtil.GetGameCoresAsync(root) 即可读取本地版本。这里“导入”指将查询结果纳入启动器自己的界面或列表，不是复制、保存或迁移用户的文件。不存在的根目录返回空列表，不创建目录；扫描不联网、不执行 Java，也不覆盖 JSON、JAR、Mod、配置和存档。

扫描 `versions` 下各版本目录的顶层 JSON，优先同名 JSON，没有同名文件时必须只有一个 JSON 候选。损坏/不明确的版本保留错误记录。不会递归把任意 JSON 当作游戏版本，也不把只有 `.fmcore/instance.json` 的数据目录当成可启动版本。

独立可编译示例：[只读扫描导入](examples/external-directory-scan.cs)。调用方负责保存自己选择的根目录和用户偏好，FMCore 不擅自选择安装位置。

## 区分不同的版本身份

| 信息 | 读取入口 | 用途 |
| --- | --- | --- |
| 实际版本文件夹名称 | GameCoreInfo.Id / DirectoryName、ResolvedVersion.DirectoryName | 查询和 LaunchConfig.GameCore.VersionId 的本地键 |
| JSON 原始 id | GameCoreInfo.MetadataId、ResolvedVersion.Metadata.Id | 版本声明及 `${version_name}`，保留原值 |
| 基础 Minecraft 版本提示 | MinecraftVersion | 优先 clientVersion，其次继承链、jar 或 JSON id；自定义元数据可能无法得出官方版本 |
| 版本 JSON/目录 | MetadataPath、VersionDirectory | 呈现实际文件位置，不要求文件名和 JSON id 一样 |
| 实际客户端 JAR | ClientJarPath、ResolvedVersion.ClientJarRelativePath | 按继承/明确 jar 引用及真实目录解析，不用显示名称猜测 |
| 游戏工作目录 | LaunchConfig.GameCore.GameDirectory / Isolate / InstanceId | 与以上身份独立；由宿主明确选择 |

例如 `versions/Survival/original.json` 中的 id 可以是 `1.16.5`，客户端也可以叫 `1.16.5.jar`；查询和启动使用 `Survival`，不会要求宿主修改 JSON。继承可以引用唯一的 JSON ID 别名；多个目录声明同一别名且无法定位时返回 ConflictingProfiles，宿主应选择实际目录，不静默任选一个。明确 jar 引用支持只有 JAR、没有同名 JSON 的本地客户端来源。

示例：[自定义版本查询](examples/external-version-query.cs)。

## 通用启动与安装器分开

启动依据主类、继承、参数、平台规则、客户端、库及资源进行准备，不以加载器名称为准入名单。加载器识别用于展示和资源兼容筛选；未知第三方主类可标记 Custom，无法推导加载器版本时 Version 为空字符串，不编造版本号。

已识别的 Fabric、Forge、NeoForge、Quilt、LiteLoader、OptiFine 或 Custom 都可以进入同一 MinecraftLauncher 流程。只有完整性检查和 Java/参数校验通过才创建进程；进程创建成功仍不等于渲染、进入世界或联机成功。

FMCore 内置的加载器自动安装器仍只有 Fabric、Forge、NeoForge、OptiFine；不提供 Quilt/LiteLoader 自动安装器。Mod 安装 API 的平台环境约束也不随通用启动能力自动扩展。

## 保留外部游戏工作目录

- 自动新建隔离目录使用 `<root>/versions/<InstanceId 或 VersionId>`。
- 外部非隔离安装使用 Isolate=false，或 GameDirectory 设为空字符串，工作目录为根目录。
- 外部独立目录使用显式根内相对 GameDirectory，例如 `third-party/Survival` 或已有 `instances/LegacySlot`，不要求搬到 versions。
- 元数据中的 gameDir、账号身份等受控选项按宿主选择生成，不在磁盘上修改原 JSON；其他有效的 tweakClass 和自定义游戏参数继续保留。
- libraries/assets 仍从 Minecraft 根目录共享。显式工作目录和 JVM 文件路径当前要求位于选定根目录内，不支持借元数据或相对越界访问根目录外的共享库/实例。

外部目录的游戏数据留在原位，FMCore 不自动转成托管实例或接管删除。此前“旧 instances 迁移”说明只适用于想切换至新版默认托管布局的调用方；显式工作目录启动不强制迁移。

示例：[启动已有版本](examples/external-version-launch.cs)。选择兼容 Java 和内存仍由开发者负责；元数据缺少 Java 要求时可以明确设置 FallbackMajorVersion，不自动下载 Java。

## 文件完整性与信任

SDK 下载的官方 JSON 保存来源 SHA-1 和语义摘要，继续支持校验、刷新与损坏缓存修复。已有外部配置、改过主类/依赖/参数的配置，以及和官方 ID 冲突的第三方配置，不自动用原版 JSON 覆盖；即使 Refresh 也保留外部配置。IsCustom 只表示本地自定义/未证实官方来源，不是禁用标签，也不是恶意判定。

没有下载 URL 时，可复用已有客户端、库及已有本地资源索引。历史 HTTP 地址也允许在本地文件完整性校验通过后复用，但不会放开非回环 HTTP 下载；文件缺失时需补齐原安装或使用安全来源。旧式 libraries 的 checksums 列表会按其声明的 SHA-1 备选值校验（包括 native JAR），带 libraries 前缀的依赖路径不会重复拼接。发布者声明的大小/哈希仍校验，资源对象仍按索引哈希校验；缺少发布者大小/哈希的本地文件在 VerifyAsync 中返回 Unverified，仅表示存在且非空，不能证明来源或完整性。没有本地文件且没有有效下载信息时明确失败，不构造虚假地址，也不下载“猜测的最新版”。

AutoInstall=false 只校验已有安装并按需生成本轮 natives/旧资源运行布局。自动启动保留校验不符的外部文件，返回错误；显式 InstallAsync / RepairAsync 属于写入/修复操作，可按已声明来源修复依赖，应由宿主说明行为。缺少文件时，日志/诊断列出相对路径与 Missing/HashMismatch 等状态。

任何第三方主类、Mod、Java agent 都是可执行代码，宿主应在首次启动前展示 Warnings 并获得自己的用户信任。路径分隔符/设备名/越界/链接、继承深度/循环、大小/哈希、ZIP 解压边界继续校验；Classpath、模块路径和 Java agent 路径须位于根目录内。不接收通过 `@参数文件`、`-jar` 或另一模块入口覆盖选中主类的启动 JSON。不能把本 SDK 的检查描述为恶意 Mod 沙箱。

无法解析的配置、缺少依赖、不安全路径、缺少 Java 条件分别使用现有错误码；只在存在多个不明确版本身份时追加 ConflictingProfiles，不把这些情况统一归为“加载器不支持”。

## 验证范围

回归测试覆盖原版、Forge、Fabric、NeoForge、Quilt、LiteLoader、OptiFine、未知主类、旧 Forge 同名冲突、JSON/文件夹不一致、继承别名/循环、共享文件、隔离/非隔离/显式旧目录、依赖缺失、损坏元数据和越界路径。测试中的模拟 HTTP、样本 JSON 与模拟进程不等于这些加载器全部实机兼容。

本轮真实启动使用本地已有样本的私有副本，运行结果及准确组合单独记录；Quilt、LiteLoader、未知主类和旧 Forge 1.7.10 目前只承诺已覆盖对应解析/参数回归，不声称已经实机启动。历史真实组合保留于各加载器手册。外部启动器任意私有数据库、全局库目录或专有参数不在自动导入范围内。

### 本轮本地真实启动组合

以下均为 Windows x64、离线身份、显式选择 2048 MiB、已下载文件的独立副本；观察到音频及纹理初始化，最后由 SDK 停止，原版本 JSON/JAR 哈希不变。未测试进入世界、服务器、正版认证或全量冷下载。

| 已有安装/目录 | Minecraft / 加载器 | Java | 实际范围 |
| --- | --- | --- | --- |
| 重命名的 Imported Vanilla 目录 | 1.16.5，JSON id 和 JAR 名保留 1.16.5 | Corretto 8u482 | 非同名 JSON、JAR，显式 third-party/Vanilla 工作目录 |
| 原版根目录工作区 | 1.16.5 | Corretto 8u482 | 非隔离启动、日志留在根目录 |
| 已有 Fabric | 1.16.5 / 0.19.5 | Corretto 8u482 | versions 下隔离启动 |
| 已有 Forge | 1.16.5 / 36.2.42 | Corretto 8u482 | versions 下隔离启动；最终观察 30 秒完成初始化 |
| 已有 OptiFine | 1.16.5 / HD U G8 | Corretto 8u482 | versions 下隔离启动 |
| 已有 NeoForge | 1.20.4 / 20.4.253 | Corretto 17.0.9 | 独立根目录，versions 下隔离启动 |

Forge 在最终并行验证的首次 15 秒窗口内仍在资源加载，未见异常；保留该记录，单独延长至 30 秒后确认初始化完成。不据此宣称所有 Forge 版本或首次启动速度均已保证。
