# 源码结构与开发

核心保持单个类库，按功能分目录。启动器、CLI 和示例引用 FMCore；核心不引用界面框架或本地工具。

## 核心模块

| 目录 | 职责 | 主要公开入口 |
| --- | --- | --- |
| FMCore/Versions/ | 版本发现、JSON、同根继承与入口 | MinecraftVersionReader |
| FMCore/Java/ | Java 候选、版本与架构探测 | JavaReader |
| FMCore/Accounts/ | 离线身份 | OfflineAccount.Create |
| FMCore/Launch/ | 配置、启动准备、进度、进程与日志 | LaunchConfig、MinecraftLauncher |
| FMCore/Installer/ | 共享路径工具；安装尚未实现 | 当前 PathSafety 为内部类型 |
| FMCore/Downloads/ 等保留目录 | 后续下载、安装、实例和资源分类 | 下载进入第 3 阶段，尚无已验收入口 |
| docs/ | 中文手册与公开接口 | public-api.md |
| examples/ | 独立查询与配置示例 | 见 examples/README.md |

## 简单入口与底层流程

1. 启动器填写 LaunchConfig：Account、GameCoreConfig、JavaConfig 和可选 GameWindowConfig。
2. 创建 MinecraftLauncher，确定路径并复制 JVM 选项。
3. LaunchAsync 在后台读取版本、探测指定 Java，并报告阶段。
4. 内部组合 PreparedLaunchConfig，复用已有 Launcher 流程。
5. 解析同根依赖、客户端、资源和日志配置，准备支持格式的原生库。
6. 创建 Java，读取两个日志流，等待退出或处理取消。
7. 完成收尾，报告 GameExited 并返回退出码。

`LaunchConfig` 是填写型配置；`PreparedLaunchConfig` 是已解析模型，原构造式配置已改用这个名称。`Launcher.RunAsync` 和 `RunLegacyAsync` 仍公开供高级调用，普通启动器无需手动组合这一层。

## 实现组件

| 组件 | 作用 |
| --- | --- |
| MinecraftLauncher | 普通公开入口、读取与探测、实例重复调用防护 |
| LaunchProgress / LaunchStage | 阶段与消息，不带百分比 |
| Launcher | 现有准备流程、Java 进程、日志和收尾编排 |
| LibraryResolver / ModernLibraryResolver | 新旧依赖、classpath、客户端及架构选择 |
| LaunchRules | 新版参数与依赖共用规则 |
| LaunchResourceResolver | 索引和标准日志配置 |
| NativeExtractor | 旧式 Windows 原生库安全解压 |
| LegacyArgumentBuilder / ModernArgumentBuilder | 新旧参数构造 |
| ModernArgumentReader | 新版参数数组、条件与继承 |

除明确列为公开入口的类型外，解析器、构造器和 RunWithProgressAsync 不属于公开 SDK。

## 项目与本地工具

公开解决方案为 FMCore.slnx。核心引用路径为 FMCore/FMCore.csproj，目标框架与语言设置来自 Directory.Build.props，SDK 选择来自 global.json。

本机额外保留 FMCore.Cli、FMCore.Tests、FMCore.Local.slnx、游戏测试数据、AGENTS.md、HANDOFF.md 和 docs/validation，不作为公开 SDK 分发。本地 CLI 可通过 InternalsVisibleTo 查看内部步骤；它的调试命令不等于公开接口。

维护者可用现有 CLI 完成功能验收。launch 分支已使用分组配置与实例入口，支持窗口宽高和 --jvm 选项。查询、准备和真实启动要分别理解，配置打印不证明游戏已运行。

## 示例同步

ShowLaunchConfig 用于打印已解析配置，类型应为 PreparedLaunchConfig。当前该源文件还有一处旧 LaunchConfig 构造引用待维护者同步；这不在 CLI 的项目引用内，不能用 CLI 验收代替这个示例的检查。见 [示例说明](../examples/README.md)。

构建只证明编译结果。真实游戏、窗口和 JVM 参数通过维护者提供的实际运行结果确认；本轮未由助手执行构建、自动化测试或游戏。

## 文档与规划

实际接口见 [公开 API](public-api.md)，最简调用和完整程序见 [启动手册](launch.md)。[开发路线](roadmap.md)和[阶段记录](stages/stage-02.md)说明当前交付项及历史；后续规划不当作已可用接口。
