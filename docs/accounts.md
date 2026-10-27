# 账号 · FMCore.Accounts

LaunchConfig.Account 使用 IMinecraftAccount，可接收 OfflineAccount 或通过 MicrosoftAuthenticator 登录获得的 MicrosoftAccount。Microsoft 正版登录与真实游戏启动已由用户验收，见 [Microsoft 登录](microsoft-login.md)。

## 离线身份

```csharp
using FMCore.Accounts;

OfflineAccount account = OfflineAccount.Create("Steve");
Console.WriteLine(account.Username);
Console.WriteLine(account.Uuid);
```

用户名接受 1–16 个英文字母、数字或下划线，保留大小写。返回 OfflineAccount，包含 Username 和 Guid 类型的 Uuid，可直接赋给 LaunchConfig.Account。

Steve 对应 `5627dd98-e6be-3c21-b8a8-e92344183641`。算法与 Java 的 UUID.nameUUIDFromBytes 一致，输入为 UTF-8 编码的 OfflinePlayer: 加原始用户名；C# Guid 显式按网络字节顺序构造。这里的 MD5 用于兼容离线身份格式，不用于保存密码或认证。

null 抛 ArgumentNullException，格式错误抛 ArgumentException。创建是同步内存操作，不联网、不保存账号、不表示获得正版授权。直接调用模型构造器不会校验名称或重算 UUID，通常使用 Create。

[离线身份示例](../examples/CreateOfflineAccount/Program.cs)。OfflineAccount 的 AccessToken 为 "0"，UserType 为 "legacy"，ClientId 为全零 GUID，Xuid 为 null；这些值只用于离线启动。

## 统一启动账号

```text
public interface IMinecraftAccount
{
    string Username { get; }
    Guid Uuid { get; }
    string AccessToken { get; }
    string UserType { get; }
    string ClientId { get; }
    string? Xuid { get; }
}
```

接口供参数构造读取账号值，不能把任意自建实现当作已经通过正版验证。MicrosoftAccount 由登录入口返回，UserType 为 "msa"；AccessToken 是 Minecraft 登录令牌。Xuid 只取服务实际返回的可选声明，缺失时为 null。SDK 不把令牌写入进度消息，MicrosoftAccount.ToString 只显示玩家名与 UUID。

MicrosoftAuthenticator.TokenCache 已开放 MSAL 缓存回调，LoginAsync 会先尝试恢复唯一缓存账号的授权。用户已通过同一进程中第二个验证器的恢复验收；授权保存、加密、跨进程持久化及账号管理由启动器作者实现。具体调用见 [Microsoft 登录](microsoft-login.md)。
