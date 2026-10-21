# Authentication 模拟数据说明

T05 响应集中在 `../AuthenticationTestContext.cs`，由内存 HTTP Handler 按真实字段格式生成，覆盖 Microsoft devicecode/token、Xbox user/authenticate、XSTS authorize、Minecraft login_with_xbox、entitlements/license 与 profile。

所有 `fixture-*`、`MOCK-CODE`、玩家 UUID 和 `11111111-2222-3333-4444-555555555555` Client ID 均为虚构值，不能用于真实登录；未保存、复制真实令牌或其他启动器的应用 ID。错误测试的敏感标记也是虚构内容，用于确认异常与日志边界。内存 token store 仅为 Mock，不提供产品持久化。

认证相关测试不联网；Windows DPAPI 测试实际调用本机加密 API，但处理的仍是虚构凭据，不能视为真实 Microsoft 登录验证。离线 UUID 向量在 `Unit/OfflineAccountTests.cs`，请求契约与账号行为分别在 Integration 和 Security。
