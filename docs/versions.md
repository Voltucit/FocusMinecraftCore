# 本地版本查询与继承

本模块只读解析本机已有目录，不下载安装游戏。命名空间为 FMCore.Versions。

## 列出版本

```csharp
using FMCore.Versions;

MinecraftVersionReader reader = new();
foreach (MinecraftVersion version in reader.GetVersions())
    Console.WriteLine($"{version.Id}：{version.Directory}");
```

默认汇总当前工作目录和 `%APPDATA%` 下的 `.minecraft`，不是扫描启动器安装目录或所有磁盘。可向 GetVersions 传入指定 Minecraft 根目录，路径由启动器决定。目录不存在返回空列表，不创建目录；读取错误明确抛出。

目录规则为 `根目录/versions/文件夹/文件夹.json`。Id 来自 JSON，可与文件夹名不同。继承或自定义版本只要满足当前字段解析要求，就能查询；这不表示其已通过启动测试。

## 查询一个版本

```csharp
using FMCore.Versions;

if (args.Length != 2)
    throw new ArgumentException("请提供版本 ID 和 Minecraft 根目录。");
MinecraftVersion version = new MinecraftVersionReader().GetVersion(args[0], args[1]);
Console.WriteLine(version.Directory);
```

GetVersion 的版本 ID 在前，根目录可选。无匹配抛 KeyNotFoundException；重复 ID 抛 InvalidDataException。不同根目录的同 ID 版本在列表中分别保留，单版本查询应显式指定根目录。

## 独立示例

- [版本列表](../examples/ListVersions/Program.cs)：可省略根目录。
- [单版本查询](../examples/GetVersion/Program.cs)：按 JSON ID 查询。
- [继承链](../examples/InheritanceChain/Program.cs)：从子版本到父版本。
- [最终启动入口](../examples/ResolveMainClass/Program.cs)：子版本优先，缺失时继承父版本。

继承解析只使用调用方传入的版本集合，并限定同一根目录。缺少父版本、重名和循环均明确报错，不跨目录借用另一个安装。当前只解析 mainClass 继承，不合并完整 JSON。

异步查询和取消见[高级指南](advanced.md)，参数与异常详见[公开 API 清单](public-api.md)。
