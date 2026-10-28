# 检查文件示例

```csharp
using FMCore.Logger;

var checker = new GameFileChecker("1.21.10", ".minecraft");
var result = await checker.CheckAsync();
foreach (var issue in result.Issues)
    Console.WriteLine($"{issue.FilePath}：{issue.Message}");
```

这段只查本地文件，不下载、不修复、不运行 Java。`FileContentVerified` 表示本次负责的文件全部检查通过，不表示游戏已经运行。

要修复问题，由启动器另行调用[文件补全](../docs/game-files.md)。缺少校验说明的文件会报告“无法确认”，不能直接当成损坏文件。

[检查范围、返回结果和可选参数](../docs/diagnostics.md)
