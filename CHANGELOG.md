# 变更记录


## 2.1.2

- 优化自动选择

## 2.1.1

- 修改了下载逻辑，支持并发和自动下载源

## 2.1.0

- 启动参数支持 `primary_jar_name`、`game_jar_name`、`client_jar_name`、`version_jar_name` 常见别名；主 jar 按版本 `jar` 字段或继承链底部版本解析。
- 新增 `LaunchConfig.PlaceholderOverrides` 和 `PreparedLaunchConfig.PlaceholderOverrides`：调用方可补充版本 JSON 中的非标准占位符，标准占位符不可覆盖。
- 未识别的占位符不再阻断启动，保留原样并记录警告；新增 `UnknownPlaceholderReporter`，供启动器收集未解析占位符并在启动失败时提示用户。

## 2.0.9

- 修复 26.3 默认 GC 与手动 GC 同时启用导致启动失败的问题；默认 JVM 参数按名称补缺，不再覆盖手动设置。

## 2.0.8

- Fabric、Forge、NeoForge 和 OptiFine 安装器新增 `RemoveCreatedBaseVersion`：开启后，本次自动补装的原版父版本在生成独立实例后会删除；已有原版、仍被其他版本继承或包含用户数据时保留。默认关闭，保持原有行为。

## 2.0.7

- 新增 `FileDownloader.UserAgent`：可为所有下载请求统一设置 User-Agent，默认空字符串行为与之前一致，用于镜像站要求标识调用方名称与版本的场景。

## 2.0.6

- 启动结束后删除本次使用的原生库工作目录，修复每次启动都会在版本目录留下一个 `natives-*` 文件夹（约 6 MB）的问题；游戏退出或被取消时都会清理，文件仍被占用时保留该目录，不影响启动结果。

## 2.0.5

- 修复 Forge 1.20 等安装包缺少或未填写 path 字段时安装失败的问题。
- 支持 NeoForge 1.20.1 旧分支的版本查询、下载和安装。
- 支持 OptiFine 使用旧式启动参数的游戏版本，保持独立配置与客户端。

## 2.0.4

- 支持 26.3 及之后的版本格式：读取 arguments 中新增的 default-user-jvm 默认用户参数，兼容没有 rules 的条目和 os.versionRange 系统版本范围。
- 默认用户参数只补缺失项，不覆盖调用方指定的内存与 JVM 参数。

## 2.0.3

- 所有已支持的加载器安装结果均保存完整配置和独立客户端，移除普通版不影响启动；支持转换已有版本，存档、Mod 和设置按版本隔离。
- 修复 1.6.4 等旧版资源映射、文件补全与完整性检查，并适配旧版启动资源目录和会话参数。




