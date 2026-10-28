# Python 学习版迁移记录

这页记录 2026-10-25 从本地 Python 学习版迁回 C# 时的对应关系，不是当前安装教程。现在接入启动器请看[文档目录](README.md)。运行 FMCore 不需要 Python。

| 以前的写法 | 当前 C# 写法 |
| --- | --- |
| `get_versions(root)` | `new MinecraftVersionReader().GetVersions(root)` |
| `get_version(root, id)` | `new MinecraftVersionReader().GetVersion(id, root)` |
| `get_java_info(path)` | `await JavaReader.GetJavaInfoAsync(path)` |
| `get_javas()` | `await JavaReader.GetJavasAsync()` |
| `create_offline_account(name)` | `OfflineAccount.Create(name)` |
| 启动前自己准备多个步骤 | 填好 `LaunchConfig`，调用 `MinecraftLauncher.LaunchAsync()` |

注意：C# 的单版本查询是版本号在前、根目录在后，和旧 Python 写法顺序不同。

路径现在使用字符串。账号和 Java 信息使用明确的 C# 类型；比较耗时的查询可以等待完成，也可以取消。原有路径检查、同根目录父版本查找和错误提示继续保留。

迁移时完成过受控样本检查。这些历史结果不等于当前全部游戏运行结果；当前范围见[启动说明](launch.md#使用范围)。业务学习资料与早期测试数据只在本地保留。
