# Microsoft 正版登录

使用 `FMCore.Accounts`。启动器作者提供自己的 Microsoft 应用 Client ID，FMCore 返回可以直接启动游戏的账号。

## 最短调用

`clientId` 是作者应用的公开 ID，由启动器设置：

```csharp
using FMCore.Accounts;

var auth = new MicrosoftAuthenticator(clientId);
var account = await auth.LoginAsync(code =>
    Console.WriteLine($"打开 {code.VerificationUri}，输入 {code.UserCode}"));
```

界面启动器可以把网址和登录码显示在窗口中。`LoginAsync()` 成功返回后，才算整个登录完成；浏览器显示授权成功时，Xbox 和 Minecraft 验证可能还在继续。

Client ID 不是密码。核心不提供默认 ID，也不替启动器注册应用。应用需要允许个人 Microsoft 账号通过登录码授权，并满足相关服务的访问要求。

## 显示登录状态

```csharp
var account = await auth.LoginAsync(
    code => Console.WriteLine($"{code.VerificationUri}：{code.UserCode}"),
    onProgress: Console.WriteLine);
```

登录码还带有 `ExpiresAt`，可用于显示过期时间。账号返回 `Username`、`Uuid`、游戏使用的 `AccessToken` 和 `ExpiresAt` 等信息。通常只需把整个 `account` 填入 `LaunchConfig.Account`，见[启动游戏](launch.md)。

## 下次尽量不用重新扫码

核心支持接入 Microsoft 登录库的缓存。它是一份用于恢复登录的数据，保存和加密方式由启动器决定。只有内存缓存时，关闭启动器后不会自动记住登录。

下面演示内存中复用这份数据。创建 `auth` 后、登录前执行：

```csharp
byte[]? savedCache = null;

auth.TokenCache.SetBeforeAccess(data =>
{
    if (savedCache is not null)
        data.TokenCache.DeserializeMsalV3(savedCache, shouldClearExistingCache: true);
});

auth.TokenCache.SetAfterAccess(data =>
{
    if (data.HasStateChanged)
        savedCache = data.TokenCache.SerializeMsalV3();
});
```

实际应用中，在这里接入启动器自己的安全存储。要跨进程记住登录，必须保存并在下次读取这些数据，不要保存用户密码。

每次仍调用 `LoginAsync()`。缓存只有一个账号且授权可用时，会先尝试恢复；没有账号、有多个账号或需要重新授权时，会显示新的登录码。其他网络或服务错误会直接报出。

## 失败和使用范围

同一个 `auth` 同时只能登录一次。账号不具备游戏资格、Xbox 验证失败、授权过期、网络失败等问题，会通过错误返回给启动器。应显示阶段和原因，不要输出登录凭据。取消参数和界面更新见[进度与取消](advanced.md)。

登录及同一进程中的授权恢复已用实际账号运行过；跨进程保存、账号切换与退出由启动器完成。游戏启动前会检查账号是否已知过期，过期时需重新登录。

[完整接口](public-api.md#fmcoreaccounts) · [Microsoft 登录码授权说明](https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/desktop-mobile/device-code-flow)
