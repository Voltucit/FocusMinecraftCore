# Microsoft 正版登录 · FMCore.Accounts

启动器作者提供自己的 Microsoft 应用 Client ID。FMCore 通过设备代码完成授权，读取 Minecraft 玩家档案并返回可直接用于启动的 MicrosoftAccount。

## 最简登录

```csharp
using FMCore.Accounts;

Console.Write("Microsoft 应用 Client ID：");
string clientId = Console.ReadLine()?.Trim()
    ?? throw new InvalidOperationException("没有读取到 Client ID。");

var auth = new MicrosoftAuthenticator(clientId);
MicrosoftAccount account = await auth.LoginAsync(
    code => Console.WriteLine(
        $"打开 {code.VerificationUri}，输入 {code.UserCode}"));

Console.WriteLine($"玩家：{account.Username}，UUID：{account.Uuid:N}");
```

核心不提供默认 Client ID，不替作者注册应用。应用应支持个人 Microsoft 账号与设备代码授权；相关服务访问配置由应用作者管理。CLI 运行时输入 ID，公开示例不嵌入维护者的实际 ID。

## 构造与主方法

```text
public MicrosoftAuthenticator(string clientId)

public Microsoft.Identity.Client.ITokenCache TokenCache { get; }

public Task<MicrosoftAccount> LoginAsync(
    Action<MicrosoftLoginCode> onLoginCode,
    Action<string>? onProgress = null,
    CancellationToken cancellationToken = default)
```

| 参数 | 含义 |
| --- | --- |
| clientId | 作者提供的应用注册 ID，当前校验为非空 GUID |
| onLoginCode | 必填，用于显示微软返回的登录网址、用户代码与有效期 |
| onProgress | 可选的当前阶段文字，不包含访问令牌 |
| cancellationToken | 取消授权等待及后续请求 |

构造创建 MSAL 客户端，不启动登录。每个对象同一时间只能执行一次 LoginAsync，结束后可再次调用。回调不保证 UI 线程；界面由调用方自行调度。回调异常继续传播。

MicrosoftLoginCode 是记录类型，包含 UserCode、Uri 类型的 VerificationUri、DateTimeOffset 类型的 ExpiresAt 和 Message；不向调用方暴露轮询用的 device_code。

## 登录结果

| MicrosoftAccount 属性 | 含义 |
| --- | --- |
| Username | 服务返回的 Minecraft 玩家名 |
| Uuid | Guid 类型的真实 Minecraft 玩家 UUID |
| AccessToken | 用于游戏启动的 Minecraft 令牌 |
| UserType | 固定 msa |
| ClientId | 本次使用的作者应用 ID |
| Xuid | 服务返回的可选 Xbox 身份声明，缺失时为 null |
| ExpiresAt | Minecraft 会话有效期 |

MicrosoftAccount 的构造方法是 internal，由 LoginAsync 返回。它实现 IMinecraftAccount；ToString 只显示玩家名与 UUID。Minecraft 令牌保存在返回对象中；Microsoft 授权由 MSAL 管理，可通过下述回调接入启动器自己的存储。

## 登录后启动

```csharp
using FMCore.Accounts;
using FMCore.Launch;

if (args.Length != 3)
    throw new ArgumentException("请提供：游戏根目录、版本 ID、Java 路径。");

Console.Write("Microsoft 应用 Client ID：");
string clientId = Console.ReadLine()?.Trim()
    ?? throw new InvalidOperationException("没有读取到 Client ID。");

using CancellationTokenSource canceled = new();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    canceled.Cancel();
};

var auth = new MicrosoftAuthenticator(clientId);
MicrosoftAccount account = await auth.LoginAsync(
    code => Console.WriteLine(
        $"打开 {code.VerificationUri}，输入 {code.UserCode}"),
    Console.WriteLine,
    canceled.Token);

LaunchConfig config = new()
{
    Account = account,
    GameCoreConfig = new() { Root = args[0], Version = args[1] },
    JavaConfig = new() { JavaPath = args[2], MaxMemoryMb = 2048 }
};

int exitCode = await new MinecraftLauncher(config).LaunchAsync(
    onProgress: progress => Console.WriteLine(progress.Message),
    onOutput: Console.WriteLine,
    onError: Console.Error.WriteLine,
    cancellationToken: canceled.Token);

Console.WriteLine($"Java 退出码：{exitCode}");
```

启动器接收 IMinecraftAccount，因此原有 OfflineAccount 调用仍可使用。启动前检查 Microsoft 会话有效期；已过期则要求重新登录。新版与旧版参数均使用账号实际 AccessToken 和 UserType，新版同时使用 ClientId 和可选 Xuid。

## 请求与错误

流程依次是 Microsoft 授权、Xbox 用户验证、XSTS 授权、Minecraft 令牌交换、Minecraft 玩家档案。微软授权轮询使用 Microsoft.Identity.Client；后续 HTTP 请求单次超时 30 秒，响应上限 1 MiB，不自动重试或跟随重定向。

Xbox 请求字段保持协议原大小写，Minecraft 的 identityToken 保持原名称。认证请求不设置共享客户端的默认 Authorization；进度和 SDK 自己的错误文字不打印令牌或完整认证响应。错误保留阶段、HTTP 状态及服务返回的可选数字 XErr。

参数、重复登录、MSAL 授权错误、HTTP、响应解析、取消、请求超时和回调错误按实际原因传播。浏览器完成 Microsoft 授权不代表 Xbox 与 Minecraft 步骤均已完成；以 LoginAsync 成功返回为准。

## 可选缓存接入与授权恢复

TokenCache 开放 MSAL 的缓存回调。LoginAsync 会先查找缓存账号：只有一个账号时尝试静默授权，成功后重新取得 Xbox、XSTS、Minecraft 令牌与玩家档案；没有账号、有多个账号或 MSAL 要求交互时进入设备代码流程。其他错误继续传播，不被当作首次登录。

下面只演示同一进程内共享缓存字节。两个验证器都调用 AttachCache 后，可通过第二个对象的 LoginAsync 恢复授权。

```csharp
using FMCore.Accounts;

byte[]? cacheData = null;

void AttachCache(MicrosoftAuthenticator auth)
{
    auth.TokenCache.SetBeforeAccess(notification =>
    {
        if (cacheData is not null)
            notification.TokenCache.DeserializeMsalV3(
                cacheData, shouldClearExistingCache: true);
    });

    auth.TokenCache.SetAfterAccess(notification =>
    {
        if (!notification.HasStateChanged)
            return;

        byte[] updated = notification.TokenCache.SerializeMsalV3();
        if (cacheData is not null)
            Array.Clear(cacheData);
        cacheData = updated;
    });
}
```

反序列化与序列化使用回调参数 notification.TokenCache。调用方按自己的存储方案读取、保存并保护缓存数据；账号列表、切换和退出也由调用方实现。回调的存储错误会传播，SDK 不接收缓存文件路径、不添加账号存储依赖，也不创建账号文件。

上述内存示例进程退出后失效。要跨进程恢复，启动器作者需要在回调中接入自己的持久化；运行结束后应清理不再使用的缓存字节。

## 当前验收与后续范围

用户已使用自己的 Client ID 完成正版登录并打开游戏。此结果覆盖当前账号与环境下的完整登录启动，不代表所有账号、服务权限、家庭限制、联机服务器和错误分支均已验收。

可选缓存回调已接入，用户已反馈 AuthorizationCacheRestored: True：在同一 CLI 进程中，将缓存交给第二个验证器，未再次显示登录码并返回相同玩家 UUID。此结果不表示跨进程持久化或等待令牌到期后的刷新分支已经实测。

资料：[MSAL 设备代码流程](https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/desktop-mobile/device-code-flow)、[Xbox 身份接口](https://learn.microsoft.com/en-us/gaming/gdk/docs/services/fundamentals/s2s-auth-calls/service-authentication/live-website-authentication)。
