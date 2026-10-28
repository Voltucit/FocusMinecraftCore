# FMCore 使用文档

先找到要做的事，复制最短示例。需要显示进度、添加取消按钮或处理失败时，再看对应说明。代码里的英文名称是实际接口名，保持原样即可。

## 开始使用

| 想做什么 | 文档 |
| --- | --- |
| 安装引用、查看项目介绍 | [项目首页](../README.md) |
| 下载一个原版游戏 | [原版安装](installation.md) |
| 启动已有游戏 | [游戏启动](launch.md) |
| 在按钮中接入启动 | [组合使用示例](../examples/launcher-workflow.md) |
| 接收进度、取消操作、显示错误 | [进度、取消与错误处理](advanced.md) |

## 按功能查找

| 功能 | 文档 |
| --- | --- |
| 本地版本列表 | [版本查询](versions.md) |
| 查找与安装 Java | [查找 Java](java.md)、[安装 Java](java-installation.md) |
| 账号 | [账号选择](accounts.md)、[Microsoft 登录](microsoft-login.md)、[外置验证](yggdrasil-login.md) |
| 文件下载与修复 | [普通下载](downloads.md)、[下载源](download-sources.md)、[游戏文件](game-files.md)、[游戏资源](resources.md) |
| 检查哪里出问题 | [文件和启动设置检查](diagnostics.md) |
| 安装加载器 | [Fabric](fabric-installation.md)、[Forge](forge-installation.md)、[NeoForge](neoforge-installation.md)、[OptiFine](optifine-installation.md) |
| 搜索社区资源 | [Modrinth](modrinth.md) |

## 需要更多细节时

- [示例目录](../examples/README.md)：按功能查看代码。
- [接口参考](public-api.md)：完整参数、返回内容和错误类型。
- [项目分工](architecture.md)：核心负责什么，启动器负责什么。
- [后续计划](roadmap.md)、[变更记录](../CHANGELOG.md)。
- [本地打包](packaging.md)、[发布准备](release-preparation.md)、[2.0.0 发布说明草稿](releases/v2.0.0.md)。

[Python 迁移记录](migration-to-csharp.md)和 [stages](stages/stage-02.md) 是开发历史，不作为当前接入步骤。
