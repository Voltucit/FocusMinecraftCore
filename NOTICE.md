# 第三方与游戏内容说明

FMCore 源码采用 MIT 许可证，版权归 Voltucit 所有。此许可证不授予 Minecraft、Java 或其他第三方内容的权利。

## 依赖

运行时仅引用 Microsoft 的 `System.Security.Cryptography.ProtectedData`，用于 Windows 当前用户 DPAPI 令牌加密；由 NuGet 恢复依赖，FMCore 包不打入该依赖的 DLL。

- 许可证：MIT；版权和完整许可证随依赖包保留，见 [dotnet/runtime LICENSE](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT) 及 [第三方声明](https://github.com/dotnet/runtime/blob/main/THIRD-PARTY-NOTICES.TXT)。
- SourceLink 使用 .NET SDK 自带工具，不增加 FMCore 运行时依赖。

## Minecraft

Minecraft 是 Mojang/Microsoft 的产品；FMCore 是独立的第三方工具，并非官方产品，未获其认可或赞助。

NuGet 包仅包含 FMCore 程序集、XML 文档及项目说明；符号包提供调试符号。公开仓库仅提供核心源码、必要配置和中文开发者文档，不分发内部测试素材；不包含游戏客户端、原生库、音效、纹理、Java、用户存档或账号令牌。下载产生的 `artifacts/`、`.minecraft/`、`.fmcore/` 和本地账号文件均不纳入源码或发行附件。

游戏内容从官方服务按需下载至用户目录；使用与分发须遵守 [Minecraft EULA](https://www.minecraft.net/en-us/eula) 与[使用准则](https://www.minecraft.net/en-us/usage-guidelines)。离线账号只生成本地身份，不授予购买资格或正版服务器权限。

## 加载器与高清修复

FMCore 通过各项目官方服务获取 Fabric、Forge、NeoForge 及 OptiFine 版本/文件，在用户目录执行必要安装步骤；不在源码、NuGet 或发行附件中附带这些项目的二进制。它们仍受各自许可证约束。OptiFine 版权归 sp614x，未取得许可不得公开再分发，见 [官方版权说明](https://optifine.net/copyright)。OptiFine Java 桥接程序仅含 FMCore 自有的 API 调用代码，采用本仓库 MIT 许可证，不含第三方安装器代码。
