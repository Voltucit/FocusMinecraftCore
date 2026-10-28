# 检查启动设置示例

先按[启动说明](../docs/launch.md)填好 `config`，再调用：

```csharp
using FMCore.Launch;

var launcher = new MinecraftLauncher(config);
var result = await launcher.CheckAsync();
if (!result.ConfigurationVerified)
    Console.WriteLine(result.Issue!.Message);
```

会运行 Java 读取版本并检查启动设置，但不启动 Minecraft、不下载、不完整扫描所有文件内容。通过不表示已经打开游戏。

这是独立检查功能，不要求每次启动都执行。普通启动直接调用 `launcher.LaunchAsync()`。

[检查范围和返回结果](../docs/diagnostics.md)
