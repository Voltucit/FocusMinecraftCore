# Launch 测试说明

`LaunchTestContext.cs` 复用安装 Fixtures 和内存 HTTP 响应，注入 Java 探测结果。`JavaProcessFixture` 的 game 分支是真实测试子进程，覆盖 stdout/stderr、退出、等待、停止、子进程树和参数回显；它不是 Minecraft，也不能替代真实游戏启动验证。

参数测试复用 `Installer/1.5.2.json`、`1.6.4.json`、`1.7.10.json`、`1.16.5.json` 的真实官方格式，资源布局由模拟索引验证。令牌和 Client ID 均为虚构测试值，回显仅限测试进程，并由 SDK 过滤后断言，不保存真实凭据。

独立 SDK 调用方的真实 Microsoft/离线 1.16.5 启动记录另外保存在 `docs/validation/t06-p0.json`；实际加密凭据和游戏输出不作为 Fixtures 提交。
