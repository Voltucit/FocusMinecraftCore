# 第三方与游戏内容说明

FMCore 源码采用 MIT 许可证，版权归 Voltucit 所有。此许可证不授予 Minecraft、Java 或其他第三方内容的权利。

## 依赖

运行时仅引用 Microsoft 的 `System.Security.Cryptography.ProtectedData`，用于 Windows 当前用户 DPAPI 令牌加密；由 NuGet 恢复依赖，FMCore 包不打入该依赖的 DLL。

- 许可证：MIT；版权和完整许可证随依赖包保留，见 [dotnet/runtime LICENSE](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) 及 [第三方声明](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT)。
- xUnit、Microsoft.NET.Test.Sdk 和测试适配器仅用于开发测试，不是 FMCore 的运行时依赖，不随核心包发布。
- SourceLink 使用 .NET SDK 自带工具，不增加 FMCore 运行时依赖。

## Minecraft

Minecraft 是 Mojang/Microsoft 的产品；FMCore 是独立的第三方工具，并非官方产品，未获其认可或赞助。

NuGet 包仅包含 FMCore 程序集、XML 文档及项目说明；符号包提供调试符号。仓库测试素材仅用于元数据解析与规则测试，来源和修改情况见 `FMCore.Tests/Fixtures/` 下各模块的 `README.md`；不包含游戏客户端、原生库、音效、纹理、Java、用户存档或账号令牌。下载产生的 `artifacts/`、`.minecraft/`、`.fmcore/` 和本地账号文件均不纳入源码或发行附件。

游戏内容从官方服务按需下载至用户目录；使用与分发须遵守 [Minecraft EULA](https://www.minecraft.net/en-us/eula) 与[使用准则](https://www.minecraft.net/en-us/usage-guidelines)。离线账号只生成本地身份，不授予购买资格或正版服务器权限。
