# 从 Python 学习版迁回 C#

日期：2026-10-25。保留业务进度和按功能分目录的结构，使用 .NET 10、C# 14 和 Rider。原 Python 代码、测试与测试数据由维护者本地保留，作为迁移对照；不是启动时的 Python 依赖。

## 接口对应

| Python | C# |
| --- | --- |
| MinecraftVersion | FMCore.Versions.MinecraftVersion |
| get_versions(root) | new MinecraftVersionReader().GetVersions(root) / GetVersionsAsync(root) |
| get_version(root, id) | new MinecraftVersionReader().GetVersion(id, root) / GetVersionAsync(id, root) |
| get_inheritance_chain(version, versions) | reader.GetInheritanceChain(version, versions) |
| resolve_main_class(version, versions) | reader.ResolveMainClass(version, versions) |
| get_java_info(path) | await JavaReader.GetJavaInfoAsync(path) |
| get_javas(search_paths, on_error) | await JavaReader.GetJavasAsync(searchPaths, onError) |
| create_offline_account(name) | OfflineAccount.Create(name) |
| LaunchConfig | FMCore.Launch.LaunchConfig |
| checks.py | 本地 FMCore.Cli |
| tests/ | 本地 FMCore.Tests |

命名空间、程序集仍为 FMCore。最初 C# 的 GetVersionDirectories 继续保留。当前目标是追平 Python 已完成的功能，不是恢复全部 1.x 实现，也不是对 1.x API 作兼容承诺。

## 有意调整

- C# 的 GetVersion 参数顺序为版本 ID、可选根目录；与 Python 原有 root、id 顺序不同。
- Path 对象换为可直接用于 .NET 文件 API 的路径字符串，探测结果和读取器结果使用绝对路径。
- dataclass 换为 record 或只读配置；修改 LaunchConfig 时重新构造，避免绕过构造校验。
- Java 探测为 Async API，支持取消，保持每次进程 10 秒超时与同 Java 的属性查询后备。
- Python 的运行时类型错误部分由 C# 编译器提前阻止；其余错误按 .NET 参数、文件、JSON、数据、超时和取消异常表达。
- 显式元数据路径优先，路径越界、Windows 保留名称和目录联接点检查保留；不以 Path.GetFullPath 冒充链接目标解析。
- 目录扫描按名称稳定排序，便于 UI 与测试；不承诺与文件系统原始枚举顺序一致。

## 迁移验收范围

Python 原有 21 项测试作为行为基线，C# 将这些场景迁移并补充异步、架构、取消、超时、进程环境隔离及真实临时目录联接点检查。具体结果由维护者在本地保存，公开阶段说明仅记录验证范围。

本次 C# 自动化测试 39 项通过、无跳过；8 个独立项目和手册中的 9 个独立 C# 代码块编译通过。CLI 与示例共 22 项实际调用通过。两个真实根目录的 7 个版本、6 份 Java、3 个离线 UUID 和 1.8.9 普通依赖路径与 Python 对照结果一致。

本地真实版本 JSON 与 Java 探测用于核实调用流程；不把受控测试进程、临时目录或元数据读取描述为真实 Minecraft 启动。真实 x86、ARM64 Java 与不同加载器游戏尚未完成启动验收。

## 后续学习方式

先从 Versions/MinecraftVersion.cs 和 MinecraftVersionReader 的目录读取开始。一次只解释并修改一个小功能，说明用途、C# 语法和验证方法；用户亲自编写后再继续原生库和完整启动流程。下载、账号 OAuth、加载器、资源平台和整合包继续按路线开发。
