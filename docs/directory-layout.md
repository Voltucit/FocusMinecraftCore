# Minecraft 根目录与版本隔离

本文说明当前未发布源码的目录调整。已经发布的包请查阅对应 Git Tag 的文档。公开 API 签名保持不变，不需要额外的目录管理器。

## 根目录由宿主指定

启动器明确传入 Minecraft 根目录，例如 `.minecraft` 或用户选择的绝对路径。FMCore 不自动改用 AppData、桌面或其他安装位置。

构造管理器、本地版本/实例列表，以及尚无状态的 Mod/整包查询，不会因为目录不存在就创建目录。安装游戏、创建实例、安装 Mod 或导入整合包等写入操作会按需创建指定根目录及必要子目录。没有内容需要写入时，不提前创建所有 mods/config/saves 文件夹；游戏或安装流程在实际使用时创建它们。

```text
<Minecraft根目录>/
  assets/                         公共资源索引和对象
  libraries/                      公共依赖库
  versions/
    1.20.4/
      1.20.4.json                  启动元数据
      1.20.4.jar                   原版客户端
      natives/                    运行文件
      .fmcore/instance.json        可选的托管实例配置
      mods/ config/ saves/        默认实例的独立游戏数据
    Survival/
      .fmcore/instance.json        VersionId 可为 1.20.4 或已安装加载器 ID
      mods/ config/ saves/        自定义实例的独立游戏数据
    Creative/
      .fmcore/instance.json
      mods/ config/ saves/
  .fmcore/instance-locks/          目录外稳定互斥锁
```

## 版本 ID 与目录名分别管理

- `GameCoreConfig.VersionId` / `InstanceConfig.VersionId` 决定实际解析和启动哪个版本，包括 Fabric、Forge 等完整 profile ID。
- `GameCoreConfig.InstanceId` / `InstanceInfo.Id` 决定 `versions` 下的目录名。默认使用启动版本 ID，自定义时不改变启动版本。
- `InstanceConfig.Name` 只控制界面显示名称，不作为路径。
- 相同启动版本的多个实例需要不同的目录名。修改托管实例的 VersionId 或显示名称，不自动重命名、搬动实例目录。

最简创建会使用 `versions/1.20.4`。传入不同的 instanceId，例如 Survival、Creative，可以为同一版本保留不同的存档、Mod 和设置。完整源码见[独立示例](examples/instance-version-directories.cs)。重复创建相同目录不会覆盖旧实例，而是返回 AlreadyExists。

目录名沿用安全版本标识符规则：长度 1–180，允许大小写、内部空格和中文，拒绝路径分隔符、绝对路径、穿越片段、Windows 设备名、尾随点/空格、链接和 `.fmcore` 保留名称。Windows 下大小写不同的同名目录也会冲突。

## 启动与共享资源

默认隔离启动将工作目录及 `${game_directory}`（游戏的 gameDir 参数）设为 `<root>/versions/<InstanceId 或 VersionId>`，Classpath 仍解析实际 VersionId 的客户端和根目录 libraries，assets 仍在共享根目录。目录叫 Survival 不要求存在名为 Survival 的启动元数据。

推荐用 `instance.CreateLaunchConfig(account)` 绑定已读取的实例快照，也可在原 LaunchConfig 的 GameCore 中分别设置 RootDirectory、VersionId 和 InstanceId。原 Isolate 和显式 GameDirectory 高级配置能力继续保留，托管实例仍会校验其固定路径及快照，不能借此重定向它的安装或删除目标。

## Mod 与整合包

ModrinthInstaller、CurseForgeInstaller 和 ModpackInstaller 的 rootDirectory 都是 Minecraft 根目录；第二个 instanceId 参数是 versions 下的目录名，不再指向 instances 容器。传入 InstanceInfo 的 Mod 安装入口自动使用同一目录。

例如目录名为 Survival，则 Mod 写入 `<root>/versions/Survival/mods`，覆盖配置写入对应 config。显式 ModEnvironment 只声明 Minecraft/加载器兼容条件，不会把目录名称当作 Minecraft 版本，也不替开发者安装加载器。

ModpackInstaller 在构造时使用明确的目录名；整包解析/运行时安装得到的 VersionId 写入实例配置，两者独立。实例数据、平台来源记录和整包备份/回滚仍使用原事务及共同锁，普通 Mod 安装不能绕过整包更新锁。查询已有未完成事务可能执行既有恢复流程，这属于必要的恢复写入；空查询不会创建新根目录。

## 已安装版本、重名和删除

InstanceManager 可在仅含合法同名版本 JSON、可选客户端 JAR/natives 的已安装版本目录中附加实例配置。存在托管配置、未知游戏数据、损坏元数据或不安全路径时拒绝接管，不用覆盖来解决冲突。

删除托管实例会移除它的 mods、config、saves、日志和控制记录等游戏数据，但保留该目录同名版本 JSON、客户端 JAR 及 natives；也不会删除根目录 assets/libraries 或兄弟实例。保留运行文件时版本目录继续存在。删除仍需宿主先取得用户确认，并遵循原进度、取消及安全预检查规则。

整合包覆盖和恢复不允许修改同目录版本 JSON/JAR/natives。这些文件继续由 Installer/Loaders 负责，避免包内容覆盖启动元数据或客户端。

1.0.2 补充：已有外部目录可以显式使用 GameCoreConfig.GameDirectory 保留原布局（包括 instances）；以下迁移仅针对改用新版默认托管布局的情况。外部目录不强制迁移，详见[第三方兼容](compatibility.md)。

## 旧 instances 目录迁移

默认托管/资源安装流程不新建旧 instances 容器；1.0.2 显式指定外部 GameDirectory 时，游戏可以继续使用原 instances 工作目录。发现旧同名目录且没有新版托管配置时，会明确报告需要迁移；旧托管实例不会被列表静默忽略。FMCore 不自动移动、合并、删除或覆盖真实游戏数据。

迁移应由宿主在游戏停止、没有安装/更新任务时执行：先完整备份旧实例到工作目录之外，再将实例的游戏数据和 `.fmcore` 控制记录迁移到 `versions/<原实例ID>`。如果目标已有版本运行文件，保留目标 JSON/JAR/natives，不直接用整个目录覆盖；目标已有另一份托管实例或不同用户数据时，先解决名称冲突。原实例 ID 和控制记录保持一致，迁移后重新读取 InstanceInfo，不复用旧快照。

只有旧版裸游戏数据、没有托管配置时，迁移完毕后应将旧同名目录移出原 instances 位置再使用新布局；SDK 不会猜测两个目录哪一份是最新存档。整包迁移须保留完整控制记录及未清理备份，以便继续按原事务恢复和回滚。

## 错误与恢复

路径/链接、重名、权限/文件系统、占用、快照变化和事务恢复失败继续使用现有模块的结构化错误码。写入只在指定根目录内按需创建，不会在失败时切换位置。取消、哈希校验、未知文件保护、备份和失败恢复沿用原实现；部分安装留下的可验证缓存允许重试复用。

本地只读与 CacheOnly 查询不创建目录；选择联网并缓存元数据的查询仍遵循其缓存策略，不将缓存下载描述为纯只读操作。
