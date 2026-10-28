# 选择账号

使用 `FMCore.Accounts`。三种账号都可以直接填入 `LaunchConfig.Account`，启动代码不用换。

| 账号 | 怎么取得 |
| --- | --- |
| Microsoft 正版账号 | [MicrosoftAuthenticator.LoginAsync](microsoft-login.md) |
| LittleSkin 等外置账号 | [YggdrasilAuthenticator.LoginAsync](yggdrasil-login.md) |
| 离线身份 | `OfflineAccount.Create("Steve")` |

## 创建离线身份

```csharp
using FMCore.Accounts;

var account = OfflineAccount.Create("Steve");
```

用户名接受 1 到 16 个英文字母、数字或下划线。`account.Username` 是名字，`account.Uuid` 是按名字生成的玩家标识。同一个名字会得到同一个标识，大小写不同则视为不同名字。

离线身份不等于正版授权，也不能因此加入只接受正版身份的服务器。

## 交给启动方法

```csharp
using FMCore.Launch;

LaunchConfig config = new()
{
    Account = account,
    GameCoreConfig = new() { Root = ".minecraft", Version = "1.21.10" },
    JavaConfig = new() { JavaPath = javaPath, MaxMemoryMb = 2048 }
};
await new MinecraftLauncher(config).LaunchAsync();
```

`javaPath` 是启动器选好的 Java。换成登录方法返回的账号即可使用同一段代码；外置账号还要提供 [authlib-injector 路径](yggdrasil-login.md)。

## 账号保存由谁处理

FMCore 返回账号，不替启动器保存账号文件、管理账号列表或加密数据。Microsoft 登录需要作者自己的 Client ID。账号恢复方法见各登录说明。

不要把密码、登录凭据或完整账号对象写进日志。查看玩家名和 UUID 即可。完整账号字段见[接口参考](public-api.md#fmcoreaccounts)。
