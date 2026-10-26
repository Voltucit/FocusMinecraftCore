# FocusMinecraftCore · FMCore

面向 Minecraft Java 版启动器开发者的 C# 核心类库。核心负责游戏相关逻辑，界面和交互由启动器实现。

> **v2.0 开发分支**：2026-10-25 已从 Python 迁回 C#，继续使用 .NET 10、C# 14 

## 当前可以做什么

| 模块 | 已实现能力 |
| --- | --- |
| Versions | 只读发现本地版本、读取基础 JSON、按 ID 查询、分析继承链、解析继承后的 mainClass |
| Java | 查询本机 Java、探测指定可执行文件的版本和架构，允许扩展搜索目录 |
| Accounts | 创建离线身份，生成与 Minecraft Java 规则一致的离线 UUID |
| Launch | 组合显式选择的版本、Java、账号、内存和游戏工作目录；基础依赖解析仍为内部实现 |

版本 ID 与实际文件夹分别处理；自动查询汇总当前工作目录和 `%APPDATA%` 下的 `.minecraft`，单版本查询遇到重名时要求指定根目录。所有这些查询保持只读。

## 设计与运行环境

- 按功能分目录，保持单核心类库，CLI、测试和界面独立。
- 使用强类型模型；Java 探测和版本读取提供可取消的异步入口。
- Java、账号、内存由调用方决定，核心不替用户自动选择或下载。
- 保留文件系统错误与数据诊断，依赖路径检查拒绝越界和 Windows 保留路径。
- 当前目标平台：Windows 10/11；开发需要 .NET 10 SDK 和支持该 SDK 的 Rider。

## 获取和引用

检出本仓库的 `v2.0` 分支，先构建核心解决方案：

```powershell
dotnet build .\FMCore.slnx -c Release
```

在启动器项目中添加对 `FMCore/FMCore.csproj` 的项目引用即可。开发分支暂不打包发布 NuGet；NuGet 上的 `FocusMinecraftCore` 1.x 与本分支的 API 和功能范围不同。

## 中文文档与示例

- [版本查询与继承](docs/versions.md)
- [本机 Java 查询](docs/java.md)
- [离线账号](docs/accounts.md)
- [启动配置与当前限制](docs/launch.md)
- [异步、取消与异常处理](docs/advanced.md)
- [完整公开 API 清单](docs/public-api.md)
- [源码结构与 Rider 开发](docs/architecture.md)
- [独立 C# 示例项目](examples/README.md)
- [开发路线](docs/roadmap.md) · [迁移说明](docs/migration-to-csharp.md) · [变更记录](CHANGELOG.md)

## 当前限制

游戏启动、原生库解压、完整参数生成、下载与安装、正版登录、加载器安装、资源平台和整合包仍按路线分阶段开发。Java 搜索检查环境变量、注册信息、`.jdks` 和额外目录，不承诺递归扫描所有磁盘。基础元数据可读不代表所有加载器兼容，也不代表已通过真实游戏启动。

本地 CLI、自动化测试、游戏测试文件和内部验证报告不上传公开仓库。公开源码和全部示例可以独立构建。

## 许可证

采用 [MIT License](LICENSE)。
