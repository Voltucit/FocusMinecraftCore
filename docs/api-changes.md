# T10 API 调整与迁移

此次只增加便捷入口，没有删除、改名或改变既有公开签名；不新增业务项目、依赖或通用封装层。

| 新入口 | 解决的问题 | 底层复用及兼容性 |
| --- | --- | --- |
| DownloadManager.DownloadFileAsync(url, destinationPath, options?, progress?, cancellationToken?) | 单文件只需 URL 与目标文件 | 原 DownloadManager / DownloadRequest；以目标父目录为安全根，原路径/URL/原子替换校验不变 |
| DownloadManager.DownloadAsync(url, relativePath, progress?, cancellationToken?) | 已有下载器无需手建基础请求 | 原 DownloadAsync(DownloadRequest, ...)；需要哈希/大小仍用原请求模型 |
| GameCoreUtil.GetAvailableVersionsAsync(root?, cachePolicy?, cancellationToken?) | 查询官方目录无需手动组装下载器 | 原 VersionManager.GetManifestAsync，返回原 VersionManifest；本地查询 API 不变 |
| LaunchConfig.Create(versionId, rootDirectory, account, java, maxMemoryMb, minMemoryMb = 0) | 直接传身份与 Java 对象，减少重复嵌套 | 原 LaunchConfig / MinecraftLauncher；返回原配置，可用 with；不改变原属性默认值 |

## 配置差异

Create 工厂要求调用方明确账号、Java 与最大内存；可选最小内存默认 0，省略 Xms。原 `new JavaConfig()` 仍保留 MinMemoryMb=512 / MaxMemoryMb=2048。使用工厂是主动选择新便捷方式，不迁移现有调用也能继续工作。配置校验仍由 MinecraftLauncher 执行；工厂不自动推荐、下载 Java 或计算内存。

下载便捷方法没有哈希/大小，因此每次重新下载。需要校验/复用时，按[高级下载示例](examples/download-advanced.cs)传 DownloadRequest；不把文件存在等同于有效。静态目标路径入口只允许该目标父目录，现有实例入口仍要求根内相对路径。

## 各模块审查结果

| 模块 | 处理 |
| --- | --- |
| Downloader / Versions / Launch | 上述四项兼容新增 |
| Java / Java Runtime | 已有简单查询、主版本与精确安装、JavaRuntime 返回值；保留原入口 |
| Installer / Assets / 完整性 | InstallAsync 内部编排，Verify/Repair 共用计划；保留原实现，补独立示例 |
| Accounts | 离线便捷登录和完整设备码授权已具备；保留授权回调与令牌刷新 |
| Loaders | 四种已适配加载器复用普通安装/启动；不引入新的加载器接口体系 |
| Resources | 保留平台差异；明确批量检查、单项更新、依赖和失败恢复，未新增跨 Mod 原子批量 API |
| Modpacks | 复用整包事务、计划、备份、确认清理、回滚及包内授权更新 |
| Instances / 高级设置 | 保留快照修订、窗口/服务器/独立堆与参数冲突校验 |
| Logging | 保留过滤、事件、缓存、诊断及显式导出；补独立示例 |

## 维护规则

后续新增/调整 API 必须同时修改相关中文模块手册、独立示例、公开 API 清单和 CHANGELOG；重新编译示例并执行核心测试。必要破坏性调整应先列出原因、影响和迁移方案取得用户确认。1.0.0 继续保留这些入口。正式发布须另行获得用户确认。

## 1.0.0 版本元数据

程序集简单名称和命名空间仍为 FMCore；AssemblyVersion / FileVersion 为 1.0.0.0，包与产品版本为 1.0.0。公开方法、类型、枚举值不变。Modrinth 两个构造函数的默认 User-Agent 联系 URL 更新为规范仓库地址，显式传入值仍照常使用；旧调用方编译入的旧默认值通过 GitHub 重定向保持可访问。LaunchConfig.LauncherVersion 默认值同步为 1.0.0，可由宿主显式覆盖。
