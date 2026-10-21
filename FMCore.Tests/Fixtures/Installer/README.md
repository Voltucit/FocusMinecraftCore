# T04 安装素材

获取日期：2026-10-21。以下为 Mojang 公开响应原文，不包含账号或用户目录；版本 JSON 按官方版本清单 SHA-1 校验，资源索引按各版本 `assetIndex.sha1` 校验。测试默认只读本地文件。

| 版本 JSON | 官方 SHA-1 | 资源索引文件 | 索引 ID / SHA-1 |
| --- | --- | --- | --- |
| 1.16.5.json | fba9f7833e858a1257d810d21a3a9e3c967f9077 | 1.16.5-assets.json | 1.16 / f3c4aa96e12951cd2781b3e1c0e8ab82bf719cf2 |
| 1.7.10.json | ed5d8789ed29872ea2ef1c348302b0c55e3f3468 | 1.7.10-assets.json | 1.7.10 / 1863782e33ce7b584fc45b037325a1964e095d3e |
| 1.6.4.json | b71bae449192fbbe1582ff32fb3765edf0b9b0a8 | 1.6.4-assets.json | legacy / 770572e819335b6c0a053f8378ad88eda189fc14 |
| 1.5.2.json | 924a2dcd8bdc31f8e9d36229811c298b3537bbc7 | 1.5.2-assets.json | pre-1.6 / 3d8e55480977e32acd9844e545177e69a52f594b |

版本来源为 [Mojang 官方清单](https://piston-meta.mojang.com/mc/game/version_manifest_v2.json) 指向的 `https://piston-meta.mojang.com/v1/packages/<SHA-1>/<版本>.json`；索引来源记录在对应版本 JSON 的 `assetIndex.url` 中。

1.16.5 样本用于验证普通依赖/原生依赖并存、重复依赖和平台选择；现代原生 JAR 布局继续使用 Versions 目录下的 1.21.1 样本。1.6.4 的真实索引包含 `virtual: true`；1.5.2 包含 `map_to_resources: true`。这些索引的读取测试不下载对象。

`InstallationTestContext` 使用相同 JSON 字段格式生成小型模拟安装：客户端/普通 JAR/日志为测试字节，原生 JAR 是在内存中生成的 ZIP，资源对象由实际内容 SHA-1 寻址；模拟 URL 使用 `example.test`，不会访问公网。故障、取消、安全解压和重复安装均在临时目录验证。实际官方 1.16.5 安装另见 `docs/installer.md` 与其验证记录。
