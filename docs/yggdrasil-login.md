# 外置账号登录

使用 `FMCore.Accounts` 登录 LittleSkin 等支持 Yggdrasil 的服务。返回的账号直接交给游戏启动方法。

## 最短调用

`username` 和 `password` 来自启动器的登录窗口，不要把真实密码写进源码：

```csharp
using FMCore.Accounts;

var auth = new YggdrasilAuthenticator("https://littleskin.cn/api/yggdrasil");
var account = await auth.LoginAsync(username, password);
```

其他服务填它提供的完整 HTTPS 地址。登录名可以是邮箱或角色名，具体由服务决定。这里只有一个角色时自动使用；多个角色需要启动器提供选择方法，见[接口参考](public-api.md#fmcoreaccounts)。

## 启动游戏

外置账号需要 authlib-injector 这个 JAR 文件，启动器事先准备好它，再把路径传入：

```csharp
using FMCore.Launch;

LaunchConfig config = new()
{
    Account = account,
    GameCoreConfig = new() { Root = gameRoot, Version = versionId },
    JavaConfig = new()
    {
        JavaPath = javaPath,
        MaxMemoryMb = 2048,
        AuthlibInjectorPath = agentPath
    }
};
await new MinecraftLauncher(config).LaunchAsync();
```

`gameRoot`、`versionId`、`javaPath` 和 `agentPath` 是启动器选好的值。FMCore 会检查账号并自动加入 Java 需要的设置，无需自己拼启动参数。JAR 路径允许空格，不能含等号；当前核心不负责下载这个 JAR。

## 恢复登录

启动器保存服务地址、登录名、角色 UUID、`AccessToken` 和 `ClientToken`。下次调用：

```csharp
var account = await auth.RestoreAsync(
    savedLoginName, savedUuid, savedAccessToken, savedClientToken);
```

这些 `saved` 变量来自启动器自己的存储。成功后会得到新账号，要更新保存的数据，再把新账号交给启动方法。旧账号对象不会自动改变。凭据已被撤销或无法恢复时，让用户重新登录。

## 返回内容与失败

`account.Username` 是游戏角色名，`LoginName` 是登录时使用的名字，两者可能不同。角色标识在 `Uuid` 中。账号文件的保存、加密、列表和删除由启动器实现；不要保存密码或把凭据写进日志。

网络、服务地址、角色选择或验证失败时会报错，不会带着失败账号继续启动。进度处理、取消和界面线程见[通用说明](advanced.md)。

已用 LittleSkin 登录、恢复账号并启动 1.21.10。这不表示所有服务或服务器都可用；服务器也需要支持对应的验证服务，不能用外置身份加入只接受 Microsoft 正版账号的服务器。

[调用示例](../examples/yggdrasil-launch.md) · [完整接口](public-api.md#fmcoreaccounts) · [LittleSkin 使用说明](https://manual.littlesk.in/yggdrasil/)
