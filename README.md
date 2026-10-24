# FocusMinecraftCore（FMCore）

FMCore 2.0 是正在使用 Python 重写的 Minecraft Java 版启动核心，面向希望用 Python 开发启动器的开发者。核心提供数据读取和业务接口，界面由启动器自行实现。

## 当前进度

第 1 阶段（基础版本查询与继承解析）已完成，已实现：

- 查询已安装版本，支持显式指定 Minecraft 根目录。
- 自动汇总当前工作目录和 Windows `%APPDATA%` 下的 `.minecraft`；保留不同目录中的同 ID 版本。
- 读取版本 ID、类型、实际目录、父版本 ID 和自身声明的启动入口。
- 解析同一 Minecraft 根目录内的继承链，检查父版本缺失、重名和循环。
- 根据子版本优先的规则解析 `mainClass`。

当前能力用于读取版本元数据；完整依赖解析、安装和游戏启动将在后续阶段实现。


## 中文文档与示例

- [开发路线](docs/roadmap.md)
- [版本模块使用手册](docs/versions.md)
- [公开 API 清单](docs/public-api.md)
- [第 1 阶段记录](docs/stages/stage-01.md)
- [独立示例及运行方法](examples/README.md)

旧 C# 实现保留在 Git 历史与 `v1.0` 分支中；Python 重写在 `v2.0` 分支推进。

## 许可证

本项目采用 [MIT 许可证](LICENSE)。
