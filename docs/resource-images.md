# Mod 图标与详情图片

本页对应 **1.0.2 维护候选**。SDK 不含 WPF、Avalonia 或其他 UI 依赖；仅返回平台已有图片数据，显示、图片下载和界面缓存由启动器负责。

## 常用入口

| 平台 | 搜索图标 | 详情图片 | 调用次数 |
| --- | --- | --- | --- |
| Modrinth | SearchAsync 返回的 Hits[].IconUrl | GetProjectAsync 的 IconUrl / Gallery | 搜索一次即可得到图标；查看详情时单独调用详情接口 |
| CurseForge | SearchAsync 返回的 Mods[].IconUrl | GetProjectAsync 的 IconUrl / Gallery | 复用搜索响应里的 logo/screenshots，不逐项追加详情或图片请求 |

IconUrl 是可空 Uri。Gallery 是只读列表，缺失或 null 时为空。原项目构造函数、参数顺序与解构签名均保持；新增属性按需使用，无图片不影响搜索、详情、安装或更新。

## 平台字段与差异

Modrinth 图标取官方 `icon_url`，图库取 `gallery`。ResourceImage 保留 Url、Title、Description、Featured、CreatedAt、Ordering；API 没提供缩略图时 ThumbnailUrl 为 null。

CurseForge 图标优先 `logo.thumbnailUrl`，回退 `logo.url`；图库取 `screenshots`，保留 Id、Title、Description、Url、ThumbnailUrl。Featured / CreatedAt / Ordering 未由该平台提供时使用默认值，不伪造内容。

字段依据：[Modrinth 搜索](https://docs.modrinth.com/api/operations/searchprojects/)、[Modrinth 详情](https://docs.modrinth.com/api/operations/getproject/)、[CurseForge 官方 API](https://docs.curseforge.com/rest-api/#modasset)。

## 安全与缺失数据

仅返回格式有效且不含用户信息/片段的 HTTPS 地址。可选图片数据损坏、使用 file/javascript/http 地址或含明显密钥/令牌查询参数时忽略该图片；不会因为展示图片异常而拒绝项目。每个项目最多映射 512 张可选图库条目，仍受原响应大小限制。

CurseForge Key 只由 CurseForgeClient 放在固定 API 域的请求头，不附加到图片地址，也不会调用图床；下载图片时宿主应使用不带认证头的独立连接，不将 API Client 的认证配置复制到 UI 图片下载器。

## 独立示例

- [Modrinth 搜索缩略图](examples/modrinth-search-icon.cs)
- [Modrinth 详情图库](examples/modrinth-project-images.cs)
- [CurseForge 搜索缩略图](examples/curseforge-search-icon.cs)
- [CurseForge 详情截图](examples/curseforge-project-images.cs)

CurseForge 示例要求自己的合法 API Key，从环境变量读取，不把 Key 放入代码或输出。代码编译不代表已访问真实服务；缺图、回退、字段映射和请求次数由 Mock 回归覆盖，真实 Modrinth 查询及未具备真实 CurseForge Key 的边界在本地报告分开记录。

本轮另通过公开 SDK 实际查询 Modrinth 的 Sodium 搜索/详情：两处均返回图标，详情返回 6 张图库图片（2026-10-23 的接口结果，图片数量可随平台变化）；没有下载这些图片。CurseForge 仍只完成 Mock 字段与 Key 隔离验证。
