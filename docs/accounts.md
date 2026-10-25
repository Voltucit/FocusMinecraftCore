# 离线账号

```csharp
using FMCore.Accounts;

OfflineAccount account = OfflineAccount.Create("Steve");
Console.WriteLine(account.Username);
Console.WriteLine(account.Uuid);
```

用户名接受 1–16 个英文字母、数字或下划线，保留大小写。返回 OfflineAccount，包含 Username 和 Guid 类型的 Uuid，可直接传入 LaunchConfig。

Steve 对应 `5627dd98-e6be-3c21-b8a8-e92344183641`。算法与 Java 的 UUID.nameUUIDFromBytes 一致，输入为 UTF-8 编码的 OfflinePlayer: 加原始用户名；C# Guid 显式按网络字节顺序构造。这里的 MD5 用于兼容离线身份格式，不用于保存密码或认证。

null 抛 ArgumentNullException，格式错误抛 ArgumentException。创建是同步内存操作，不联网、不保存账号、不表示获得正版授权。直接调用模型构造器不会校验名称或重算 UUID，通常使用 Create。

[独立示例源代码](../examples/CreateOfflineAccount/Program.cs)。Microsoft 授权登录与令牌刷新属于后续阶段，当前没有相应公开 API。
