# 源码结构与 Rider 开发

核心保持单类库，按功能分目录。公开仓库结构：

```text
FMCore.slnx
Directory.Build.props
global.json
FMCore/
  FMCore.csproj
  Versions/
    MinecraftVersion.cs
    MinecraftVersionReader.cs
  Java/
    JavaInfo.cs
    JavaReader.cs
    JavaDiscovery.cs
  Accounts/
    OfflineAccount.cs
  Launch/
    LaunchConfig.cs
    LibraryResolver.cs
  Internal/
    PathSafety.cs
  Downloads/  GameInstance/  Installer/  Logger/
  ModPacks/  Modsloader/  ModsResources/  （保留分类，尚未实现）
examples/
  Examples.slnx
  ListVersions/  GetVersion/  InheritanceChain/  ResolveMainClass/
  ReadJavaInfo/  ListJavas/  CreateOfflineAccount/  ShowLaunchConfig/
docs/
  versions.md  java.md  accounts.md  launch.md  advanced.md
  public-api.md  architecture.md  migration-to-csharp.md  roadmap.md
  stages/stage-01.md  stages/stage-02.md
.github/workflows/
README.md
CHANGELOG.md
LICENSE
```

本地额外保留 FMCore.Cli、FMCore.Tests、FMCore.Local.slnx、FMCoreTestsFiles、AGENTS.md 和 docs/validation，不上传 GitHub、不进入 NuGet。空的后续功能文件夹可以继续保留，但不填入占位业务 API。

## 依赖关系

- Versions 用 System.Text.Json 读取基础元数据，由 Internal 的路径工具处理规范化；不依赖 Java 或账号。
- Java 只依赖 .NET 进程、文件与 Windows 注册表接口，返回 JavaInfo。
- Accounts 的离线身份是独立内存逻辑。
- LaunchConfig 组合 Versions、Java、Accounts 模型；LibraryResolver 复用版本 JSON 读取和路径安全检查。
- 启动器、CLI 和示例引用 FMCore；核心不引用 CLI、测试或界面框架。

当前调用流程为：查询版本 → 明确选择版本与 Java → 创建离线身份 → 构造 LaunchConfig。真正启动步骤尚未实现，不能把配置完成视为游戏启动成功。

## Rider 本地运行

开发者克隆公开仓库后打开 FMCore.slnx 即可构建核心。维护者本机打开 FMCore.Local.slnx，可以选择 FMCore.Cli 运行，或在测试窗口运行 FMCore.Tests。

```powershell
dotnet build .\FMCore.slnx -c Release
dotnet build .\examples\Examples.slnx -c Release
```

维护者本地额外命令：

```powershell
dotnet build .\FMCore.Local.slnx -c Release
dotnet test .\FMCore.Tests\FMCore.Tests.csproj -c Release
dotnet run --project .\FMCore.Cli -- --help
```

CLI 仅调用公开 SDK，不重复实现底层协议。命令包括 versions、version、chain、main-class、java、javas、offline、config。成功退出码 0，参数或操作失败为 1，取消为 130。
