# 选择下载源

用 `FMCore.Downloads.DownloadSource` 选择官方、BMCLAPI 或自己的服务。把选好的对象赋给安装器的 `Source` 即可。

## 选择 BMCLAPI

```csharp
using FMCore.Downloads;
using FMCore.Installer;

var installer = new MinecraftInstaller("1.21.10", ".minecraft")
{
    Source = DownloadSource.BmclApi
};
await installer.InstallAsync();
```

用官方源时，改成 `DownloadSource.Official`，或省略 `Source`。Fabric、Forge、NeoForge、游戏文件补全和资源补全使用同样的写法。

## 让用户自己选

```csharp
DownloadSource[] choices = [DownloadSource.Official, DownloadSource.BmclApi];
DownloadSource selectedSource = choices[selectedIndex];
```

`selectedIndex` 来自启动器下拉框等控件。用 `Name` 显示名称，把 `selectedSource` 赋给安装器。选项保存和界面由启动器处理。

## 使用自己的服务

给下载源起一个名字，再说明哪些原地址要换成你的地址。下面只是地址规则示例，`mirror.example.com` 需要换成实际可用服务：

```csharp
var mySource = new DownloadSource("我的下载源", originalUrl =>
{
    var url = new Uri(originalUrl);
    if (url.Host == "maven.fabricmc.net")
        return "https://mirror.example.com/fabric" + url.PathAndQuery;
    return null;
});
```

之后使用 `Source = mySource`。返回 `null` 表示这个地址不替换，继续使用原地址。也可以单独调用 `mySource.GetUrl(originalUrl)` 获得转换后的地址。地址必须是完整 HTTP 或 HTTPS 地址，不能夹带账号信息或 `#` 片段。

只在这个方法里转换地址，不要在里面执行下载。它可能被多个任务同时使用。

## 查询版本时选源

```csharp
using FMCore.Modsloader;

var versions = await FabricInstaller.GetLoaderVersionsAsync(
    "1.21.10", DownloadSource.BmclApi);
```

Forge 和 NeoForge 的 `GetVersionsAsync()` 也能在游戏版本后传入下载源。不传时使用官方源。

## 哪些地方会使用所选源

| 操作 | 说明 |
| --- | --- |
| 游戏、依赖库、资源下载 | 使用所选源；BMCLAPI 只替换它支持的地址 |
| Fabric 版本列表和游戏配置 | 使用所选源 |
| Forge、NeoForge 版本列表、安装包和依赖 | 使用所选源；安装包的大小与校验值仍从官方读取 |
| OptiFine 在线列表和安装包 | 默认 BMCLAPI；自定义服务需兼容它的返回格式；不能直接选择 `Official` |
| OptiFine 本地安装 | 传入已有 JAR，所选源负责原版及依赖下载 |
| Modrinth 查询和文件下载 | 可用自定义源；内置 BMCLAPI 不替换这些地址 |
| Minecraft 原始版本说明、缺失的 Maven 校验信息 | 仍可能访问原来的官方地址 |
| Java 安装、账号登录、补丁工具自己发出的请求 | 不由此设置控制 |

选择 BMCLAPI 不等于所有请求都只访问 BMCLAPI。下载时仍会检查原先声明的大小和校验值。网络下载失败时，可能在原有重试次数和时间内尝试原地址；说明文件查询失败时则直接报告错误。

使用 BMCLAPI 时，请在启动器明显位置标明下载来源，并遵守 [BMCLAPI 使用要求](https://bmclapidoc.bangbang93.com/)。自定义服务的可用性和地址规则由开发者负责。

[普通文件下载](downloads.md) · [完整接口](public-api.md#fmcoredownloads)
