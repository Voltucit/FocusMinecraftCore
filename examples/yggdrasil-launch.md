# 外置账号启动示例

`username`、`password` 来自登录窗口，`javaPath` 是所选 Java，`agentPath` 是已准备好的 authlib-injector JAR 路径。

```csharp
using FMCore.Accounts;
using FMCore.Launch;

var auth = new YggdrasilAuthenticator("https://littleskin.cn/api/yggdrasil");
var account = await auth.LoginAsync(username, password);

LaunchConfig config = new()
{
    Account = account,
    GameCoreConfig = new() { Root = ".minecraft", Version = "1.21.10" },
    JavaConfig = new()
    {
        JavaPath = javaPath,
        MaxMemoryMb = 2048,
        AuthlibInjectorPath = agentPath
    }
};
await new MinecraftLauncher(config).LaunchAsync();
```

单角色时直接使用；多角色需要提供选择方法。核心会检查账号并加好启动设置，不用自己拼 Java 参数。服务器也需要支持对应验证服务。

登录恢复、角色选择和账号保存的分工见[外置验证说明](../docs/yggdrasil-login.md)。真实密码和登录凭据不要写进源码或日志。
