# T02 元数据样本

获取日期：2026-10-21（北京时间）。官方公开元数据不包含账号或令牌，无需替换个人信息。测试默认从本地读取，不访问这些地址。

原样保存的公开响应：

| 文件 | 来源 | 官方 SHA-1 |
| --- | --- | --- |
| official-manifest.json | https://piston-meta.mojang.com/mc/game/version_manifest_v2.json | 列表本身不提供校验和 |
| 1.21.1.json | https://piston-meta.mojang.com/v1/packages/1757c8b1fbfc6b638504e1f6cceb05893f7389d5/1.21.1.json | 1757c8b1fbfc6b638504e1f6cceb05893f7389d5 |
| 1.12.2.json | https://piston-meta.mojang.com/v1/packages/832d95b9f40699d4961394dcf6cf549e65f15dc5/1.12.2.json | 832d95b9f40699d4961394dcf6cf549e65f15dc5 |
| 24w14a.json | https://piston-meta.mojang.com/v1/packages/a8d42b1b816c79663fb1ae1b3607b998a0e6e87e/24w14a.json | a8d42b1b816c79663fb1ae1b3607b998a0e6e87e |
| fabric-1.21.1.json | https://meta.fabricmc.net/v2/versions/loader/1.21.1/0.16.10/profile/json | 接口本身不提供校验和 |

三个 Mojang 版本 JSON 均已按清单 SHA-1 校验。列表和 Fabric 配置是获取当时的快照，不能用来断言未来最新版本。

`inherit-base.json` 与 `inherit-child.json` 是按上述格式构造的测试数据，用于稳定验证覆盖规则、原生依赖及未知字段。`example.test` 地址不会被作为实际远程资源下载。

Fabric 配置只用于 JSON 与继承测试；解析通过不等于 Fabric 已安装或游戏已启动。
