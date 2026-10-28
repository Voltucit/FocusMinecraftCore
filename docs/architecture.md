# 核心与启动器怎样分工

FMCore 是供启动器调用的 C# 类库，不包含启动器界面。通常一个功能只需要创建对象，再调用一次方法。

## 常用功能放在哪里

| 代码位置 | 用途 | 开始使用 |
| --- | --- | --- |
| `FMCore.Versions` | 读取已安装游戏和父版本 | [版本查询](versions.md) |
| `FMCore.Java` | 查找、读取和安装 Java | [Java](java.md) |
| `FMCore.Accounts` | 登录账号、创建离线身份、恢复登录 | [账号](accounts.md) |
| `FMCore.Launch` | 启动游戏、接收游戏输出 | [启动](launch.md) |
| `FMCore.Installer` | 原版安装、补全与修复文件 | [安装](installation.md)、[补全](game-files.md) |
| `FMCore.Downloads` | 普通下载、下载进度与下载源 | [下载](downloads.md) |
| `FMCore.Modsloader` | 安装 Fabric、Forge、NeoForge、OptiFine | [文档目录](README.md) |
| `FMCore.ModsResources` | 搜索社区资源、获取详情和文件 | [Modrinth](modrinth.md) |
| `FMCore.Logger` | 返回文件问题和启动设置检查结果 | [检查](diagnostics.md) |

不要为了启动一次游戏，手动调用内部的文件列表生成、参数拼接或原生库解压步骤。`MinecraftLauncher`、安装器和补全器已经安排好各自需要的工作。

## 启动器负责哪些事

- 界面、按钮、实例列表和用户选择。
- Java、账号、内存、游戏根目录及下载源的选择和保存。
- Microsoft 应用 Client ID；账号数据的安全保存、加密和管理。
- Mod、资源包、数据包和光影的安装管理与依赖选择。
- 整个整合包系统，包括识别格式、读取清单、导入、导出、更新和内容选择。

核心提供这些功能可用到的查询、下载、游戏安装和启动方法，不替启动器作上述决定。

## 文件保存规则

调用方决定根目录。游戏版本放在 `versions/版本或实例名称`，共用文件放在 `assets` 和 `libraries`。相对路径按创建对象或执行下载时的工作目录确定，详细规则见对应功能页。

核心只创建实际功能需要的目录，不生成固定的测试文件夹。它不会自动导入其他启动器账号，也不修改系统 Java 设置。遇到已有不同版本配置时，保留并报错。

## 查更详细的实现

使用示例见[文档目录](README.md)。具体参数、返回内容和保留的高级方法见[接口参考](public-api.md)。本地调试程序、测试、游戏数据和教学记录不随核心包发布。
