# 完整控制台 Minecraft 启动器

此项目演示版本查询 → 用户选择本机 Java → 安装 → 账号授权 → 配置 → 游戏启动 → 实时日志和退出。所有业务调用均使用 FMCore 公开 SDK；不自行拼 Classpath、OAuth、下载任务或安装事务。

## 项目结构与完整源码

```text
docs/sample-launcher/
├─ SampleLauncher.csproj
└─ Program.cs
FMCore/
└─ FMCore.csproj
```

完整文件：[SampleLauncher.csproj](sample-launcher/SampleLauncher.csproj)、[Program.cs](sample-launcher/Program.cs)。源文件直接发布在文档目录，不依赖本地 CLI/测试/内部类型。项目显式使用 net10.0 / C# 14、ProjectReference，IsPackable=false。复制到独立目录时调整项目引用位置；使用自行构建的本地包时可将 ProjectReference 替换成 FocusMinecraftCore 的 PackageReference。使用完整功能时引用 FocusMinecraftCore 1.0.0。

## 前置条件

- Windows 10/11，.NET 10 SDK；可在 Rider 打开示例 csproj，或从仓库根目录执行命令。
- 安装与目标 Minecraft 元数据兼容的 Java。程序展示本机 Java 后要求用户输入路径，不自动下载或猜主版本。需要下载时单独使用 JavaRuntimeUtil。
- 显式决定根目录、游戏版本、内存 MiB 和账号方式。示例只处理官方原版；加载器使用安装返回的 VersionId 接入通用启动 API。
- 能访问 Mojang/Microsoft/Minecraft 等官方元数据、文件及认证服务。网络或文件错误会明确失败。
- 正版模式需开发者自己的 Microsoft 应用 Client ID、相关服务访问权限、设备码授权及拥有游戏资格的账号。仅设置 Client ID 不等于授权通过。离线账号不赋予正版或服务器访问资格。

## 编译与运行

```powershell
dotnet build docs/sample-launcher/SampleLauncher.csproj -c Release
dotnet run --project docs/sample-launcher/SampleLauncher.csproj
```

无参数时交互输入，Java 和内存均没有自动推荐。也可显式传入参数：

```powershell
dotnet run --project docs/sample-launcher/SampleLauncher.csproj -- .minecraft 1.16.5 'C:/Java/jre8/bin/java.exe' 2048 offline:Player
```

Java 路径和 2048 MiB 仅为明确配置示例，应替换为用户选择的可用值。正版调用：

```powershell
$env:FMCORE_MICROSOFT_CLIENT_ID = '你自己合法注册的应用 Client ID'
dotnet run --project docs/sample-launcher/SampleLauncher.csproj -- .minecraft 1.16.5 'C:/Java/jre8/bin/java.exe' 2048 microsoft
```

程序会提示验证网址和本次设备授权码；回调返回后 SDK 轮询并完成 Microsoft → Xbox/XSTS → Minecraft 资格和资料流程。账号刷新凭据默认由当前用户 DPAPI 保存，不输出访问令牌。

## 日志、停止与退出

stdout/stderr、状态及退出事件分别监听；LaunchResult.Error 处理启动准备失败，GameExitResult 处理进程退出。游戏创建成功后 Ctrl+C 显式调用 StopAsync，await using 释放锁和私有 native。可在末尾加 `--stop-after 15` 用于人工验收后的有限运行；自动停止返回 0 只表示停止流程成功，不表示主菜单、世界或联机通过。

日志保存为游戏根目录下 `.fmcore/sample-game.jsonl`。进程 Running 与退出码均不能单独证明游戏渲染成功；实际验收应观察窗口、音频/纹理日志及退出过程。取消等待后不能假定游戏已经结束，本项目会显式停止。

## 验证边界

示例需要独立编译和具备条件时实际运行。当前轮次具体结果保存在维护者本地，不随公开仓库发布内部报告；历史实机组合见对应模块手册。正版授权、外部服务权限、世界和服务器连接必须各自验证，不能用 Mock 或编译结果代替。

本轮在 Windows x64 实际运行本项目：Minecraft 1.16.5、Corretto 1.8.0_482、显式 2048 MiB、离线身份，在隔离副本上完成安装复用、启动与日志监听，观察到音频/纹理初始化日志，再由 SDK 定时停止、收到退出事件并保存日志。未验证世界、服务器或本轮正版授权；不把自动停止返回 0 当作游戏功能全部通过。
