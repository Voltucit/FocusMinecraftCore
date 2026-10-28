# 安装 Java

使用 `FMCore.Java` 下载 Minecraft 官方提供的 Windows x64 Java，并返回启动时可直接使用的路径。

## 最短调用

```csharp
using FMCore.Java;

var installer = new JavaInstaller(25, ".minecraft/runtime/java-25");
var result = await installer.InstallAsync();
string javaPath = result.Java.Executable;
```

把 `javaPath` 填入 `LaunchConfig.JavaConfig.JavaPath` 即可。安装目录由启动器选择，不会修改系统的 `JAVA_HOME`、`PATH` 或注册信息。

## 可选设置和结果

| 内容 | 用途 |
| --- | --- |
| 第一个参数 `majorVersion` | 可选 Java 8、16、17、21、25；具体小版本由官方决定 |
| 第二个参数 `installDirectory` | 保存这个 Java 的目录 |
| `maxConcurrency` | 同时处理的文件数，默认 4 |
| `result.Java` | 安装后实际读取到的 Java 信息 |
| `result.DownloadedFiles`、`ReusedFiles` | 新下载和继续使用的文件数 |

显示进度只需：

```csharp
var result = await installer.InstallAsync(
    progress => Console.WriteLine(progress.Message));
```

已有正确文件会继续使用，内容不符会报错，不直接覆盖已有 Java 文件。下载完成后会运行 `java.exe`，确认实际版本。网络、文件或最终运行检查失败时，不会返回成功结果。

当前只支持官方清单中的 Windows x64 运行时，不提供任意 JDK 发行版、其他系统和位数、自动升级或卸载。此入口没有 `Source` 设置。需要使用自己安装的 Java 时，直接传它的路径即可。

[查询已有 Java](java.md) · [启动游戏](launch.md) · [完整接口](public-api.md#fmcorejava)
