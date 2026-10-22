# 账号管理

本手册对应当前源码。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Authentication;

using var accounts = new AccountManager(".fmcore/accounts");
var identity = await accounts.LoginOfflineAsync("Player");
Console.WriteLine(identity.Account.Name);
```


`FMCore.Authentication.AccountManager` 统一管理 Microsoft 正版与离线账号，面向 WPF、Avalonia、WinUI 等宿主。无需新增项目、GUI 依赖或认证框架。所有 I/O 操作异步，支持 `CancellationToken`。

T05 阶段通过模拟认证和本地测试。T06 已使用调用方提供的 Client ID 和交互式 Microsoft 授权，真实验证登录、游戏资格/玩家资料、刷新及 Windows x64 下 1.16.5 正版启动；未验证多人服务器联机。此处为历史实机验收范围，详细记录保留在维护者本地。

## 公开接口

| API | 主要参数 | 返回值 |
| --- | --- | --- |
| `LoginOfflineAsync(name, ct)` | 合法昵称；不联网 | 可直接传入启动配置的 `LaunchIdentity` |
| `LoginMicrosoftAsync(options, showDeviceCode, progress, ct)` | 自己的 Client ID；必填设备码提示回调；可选进度 | 通过资格/资料认证并保存后的 `LaunchIdentity` |
| `GetAccountsAsync(ct)` | 可选取消 | `AccountSnapshot`：Accounts、SelectedAccountId、DefaultAccountId |
| `SwitchAsync(id, ct)` / `SetDefaultAsync(id, ct)` | 账号 ID；默认账号允许 null 清除 | 选中的 `AccountInfo` / 无返回值任务 |
| `RefreshAsync(id, progress, ct)` | 账号 ID | 重新认证/刷新的 `LaunchIdentity`；离线不联网 |
| `GetLaunchIdentityAsync(id, progress, ct)` | 可选 ID，省略时使用所选/默认账号 | 适用于启动的身份，正版按需刷新 |
| `DeleteAsync(id, ct)` | 明确要删除的账号 ID | `bool` 是否找到并删除账号及对应凭据 |

以上进度与取消均可省略；业务失败使用 `AuthenticationException.Code`，取消使用 `OperationCanceledException`。管理器与安全存储生命周期、完整正版与离线调用及错误码见下文，不要求调用方读取或拼接令牌。

独立、可编译的调用代码见[本模块示例](examples.md#authentication)。

| 方法 | 行为 |
| --- | --- |
| `CreateOfflineAccountAsync(name, ct)` | 无网络创建并选择离线账号，返回 `AccountInfo`；相同昵称复用已有账号 |
| `LoginOfflineAsync(name, ct)` | 便捷创建/选择离线账号并返回 `LaunchIdentity`，可直接赋给 `LaunchConfig.Account` |
| `LoginMicrosoftAsync(options, showDeviceCode, progress, ct)` | 完整交互认证，保存并选择账号，返回 `LaunchIdentity` |
| `GetAccountsAsync(ct)` | 返回资料列表、当前选择和持久化默认账号，无网络 |
| `SwitchAsync(id, ct)` | 选择已有账号，不请求网络，不立即验证凭据 |
| `SetDefaultAsync(id, ct)` | 持久化下次打开的默认账号；传 `null` 清除默认值 |
| `GetLaunchIdentityAsync(id, progress, ct)` | 获取指定或当前账号的启动身份，必要时刷新正版令牌 |
| `RefreshAsync(id, progress, ct)` | 正版强制刷新并重验资格/资料；离线直接返回离线身份 |
| `DeleteAsync(id, ct)` | 删除安全凭据及本地资料，返回是否存在；不撤销 Microsoft 服务器授权 |

各方法的参数允许省略可选进度及取消令牌，建议 UI 始终传入取消令牌。`AccountInfo.Id` 是 FMCore 的本地账号键，`Uuid` 是 Minecraft 玩家身份；两者用途不同。同一玩家再次正版登录会更新已有账号，保留其本地键。

新建/登录会选择该账号；第一个账号同时成为默认账号。`SwitchAsync` 的选择只属于当前管理器，重启后优先使用保存的默认账号；未设置默认时取列表第一个。`SetDefaultAsync` 不强制切换已选账号。删除默认账号后默认值顺延至第一个账号，删除当前账号后按默认值/第一个账号回退；空列表的两个值均为 `null`。最多保存 256 个账号。

同一管理器串行处理存储操作，多个管理器对同一目录的同时操作可返回 `StoreBusy`。交互登录等待用户时不持有存储锁，刷新期间持锁以协调令牌轮换和删除。共享一个长期存在的管理器即可；释放前应先等待正在执行的调用结束。

## 完整调用示例

以下代码可放入调用方 .NET 10 控制台项目的 `Program.cs`，项目引用 `FMCore`，开启隐式 using。默认只执行离线账号流程；传入 `--microsoft` 时才执行正版登录。`FMCORE_CLIENT_ID` 必须由调用方设置为自己合法注册、支持个人 Microsoft 账号和公共客户端设备授权，并满足 Xbox/Minecraft 服务访问条件的应用 ID；库不提供可借用的 ID，也不需要密码或客户端密钥。

独立、可编译的调用代码见[本模块示例](examples.md#authentication)。

WPF/Avalonia/WinUI 应把设备授权提示显示在窗口中，可由用户点击打开 `VerificationUri`；仅在 Microsoft 官方页面输入账号信息。核心不直接打开浏览器或采集密码。回调可能在后台线程执行，宿主须通过自身 Dispatcher 切回 UI，并在完成提示展示后返回，不能在回调内等待登录完成。`DeviceCodeInfo.UserCode` 是短期授权信息，仅展示给当前用户，完成、取消或过期后从界面清除。

`Progress<T>` 在创建时捕获同步上下文，可在 UI 线程创建；`AccountsChanged` 是普通 .NET 事件，宿主自行调度 UI 更新。事件只包含资料快照，处理器应快速返回；订阅者抛出的异常被忽略，避免已保存成功的操作被误报为失败。事件不提供跨进程广播。

## 认证、刷新与启动语义

正版采用 `consumers` authority 的 OAuth 设备码流程，scope 为 `XboxLive.signin offline_access`。核心遵循服务提供的轮询间隔，处理 `authorization_pending`、`slow_down`、拒绝与过期。随后依次请求 Xbox 用户令牌、XSTS、Minecraft 访问令牌、`/entitlements/license` 和 `/minecraft/profile`；仅匹配游戏资格条目并获得有效玩家资料后保存正版账号。Xbox 与 XSTS 的用户哈希必须一致。Minecraft 端点当前按服务响应契约实现及模拟验证，尚未进行真实服务互通验证。

刷新重新完成 Xbox/XSTS 和 Minecraft 资格/资料检查，更新昵称和验证时间；玩家 UUID 不允许变化。服务轮换后的 Microsoft 刷新令牌先写入安全存储，再访问下游服务；轮换提交使用独立的 10 秒超时，避免用户取消丢失已经收到的新刷新凭据。因此取消可能短暂等待安全存储提交完成。提交失败就报错，不继续返回启动身份。下游失败保留已安全保存的新刷新令牌，便于重试。

Minecraft 访问令牌仅在当前管理器内缓存；距过期不足 60 秒时自动刷新，重启后首次请求正版启动身份也需刷新。强制刷新失败会移除旧内存会话，不能继续用旧会话绕过失败。`AccountInfo.HasGameEntitlement`、`LastVerifiedAt` 表示上次成功认证的结果，列表读取不保证此刻仍有资格，启动前应调用 `GetLaunchIdentityAsync`。调用方已取得的短期身份应即时使用，不要长期自行缓存。

| 启动信息 | Microsoft | 离线 |
| --- | --- | --- |
| `Account.Kind` / `IsAuthenticated` | `Microsoft` / `true` | `Offline` / `false` |
| `Account.Name` / `Uuid` | 服务返回的昵称 / 无连字符 UUID | 输入昵称 / 稳定无连字符 UUID |
| `UserType` | `msa` | `legacy` |
| `AccessToken` | 短期 Minecraft 访问令牌 | 字符串 `0` |
| `LegacySession` | `token:<accessToken>:<uuid>` | 字符串 `0` |
| `ClientId` / `Xuid` / `ExpiresAt` | 应用 ID / 服务提供时的 XUID / 令牌期限 | 均为 `null` |

Launch 已自动将这些数据用于 `${auth_player_name}`、`${auth_uuid}`、`${auth_access_token}`、`${auth_session}`、`${user_type}`、`${clientid}`、`${auth_xuid}`，无需调用方拼接，见 [启动示例](launch.md)。传入 `LaunchConfig.Accounts` 可在启动前复用管理器刷新身份。离线账号没有正版访问资格，不能因此通过正版服务器认证；在线失败只会返回错误，切换离线账号须调用方明确操作。

离线 UUID 为 UTF-8 字节串 `OfflinePlayer:<昵称>` 的 MD5，设置 UUID v3/变体位并按网络字节序生成。例如 `Notch` → `b50ad385-829d-3141-a216-7e7d7539ba7f`，`Steve` → `5627dd98-e6be-3c21-b8a8-e92344183641`。纯函数 `OfflineIdentity.CreateUuid` 按输入原文计算；账号创建限制为兼容的 1–16 位 ASCII 字母、数字、下划线，不裁剪、转小写或把在线 UUID 当作离线 UUID。

## 安全存储

- `accounts.json`：schema 1，仅账号资料与默认账号键，无令牌。损坏、未知字段、重复身份、无效离线 UUID 会返回 `StorageCorrupted`，不静默覆盖。
- Windows `tokens/<本地账号键>.dat`：DPAPI `CurrentUser` 密文，带账号独立熵，只保存刷新令牌及必要的 Client ID/玩家 UUID。不能跨 Windows 用户或直接复制到另一台机器使用；迁移时应重新登录。DPAPI 不隔离同一系统用户下的其他进程。
- Microsoft、Xbox、XSTS、Minecraft 的访问令牌只在内存中流转。`LaunchIdentity.AccessToken`、`LegacySession` 和设备短码均不参与默认 JSON 序列化；这无法阻止宿主显式读取后泄露，勿记录请求、授权头、进程完整参数、对象反射或内存转储。
- 认证 HTTP 客户端固定 HTTPS 目标，禁用自动重定向和 Cookie；返回体限量读取，错误仅包含固定说明、错误码、阶段及安全的状态码，不携带服务原文、令牌或内部异常。核心不自建认证服务，也不保存密码。
- 非 Windows 默认没有密钥环实现；离线账号仍可使用，在线功能在发请求前返回 `StorageUnavailable`。可在任何平台传入 `IAccountTokenStore`，由宿主连接操作系统凭据库，禁止明文文件、可逆编码或与令牌同目录保存密钥的伪加密实现。

`IAccountTokenStore` 只有三个异步方法：`ReadAsync` 返回缺失时的 `null` 或**新分配、交给调用方所有**的明文字节副本，管理器读取后清零；`WriteAsync` 在返回前完成安全、原子的替换，不能保留传入的临时缓冲区；`DeleteAsync` 对缺失项视为成功。三者均须支持取消，不应输出敏感值。秘密内容为内部格式，宿主只负责安全存取，不解析或改变内容。接口为真实平台凭据集成保留，测试的内存实现不能作为产品持久化方案。

新增的 `System.Security.Cryptography.ProtectedData` 是 Microsoft 官方 DPAPI 包，版本集中于 `Directory.Packages.props`；用途仅为当前用户加密，无额外第三方认证依赖。账号目录建议放在应用的每用户数据目录，不放入游戏版本、整合包、导出配置或仓库。

## 错误与验证范围

处理 `AuthenticationException.Code`，必要时结合 `Stage`、`StatusCode` 和 `XboxError`；取消使用 `OperationCanceledException`。常见错误：`ClientRejected`（应用/权限被拒）、`AuthorizationDeclined`、`DeviceCodeExpired`、`ReauthenticationRequired`、`GameNotOwned`、`ProfileMissing`、`IdentityMismatch`、`NetworkError`、`RequestTimeout`、`InvalidResponse`、`StorageUnavailable`、`StorageCorrupted`、`StorageError`、`StoreBusy`。XSTS 失败的数字错误码留给宿主提供相应的账号设置、家庭或区域提示，不把所有错误解释为网络故障。

T05 新增 77 项自动化测试：完整请求契约、轮询、拒绝、资格不足、资料缺失、轮换/取消/撤销、UUID、共存/切换/删除/默认账号、并发锁、配置损坏、路径链接、泄漏防护；认证 HTTP 为 Mock，DPAPI 使用本机真实 API 和虚构凭据。T06 全套 402 项通过，并单独执行真实 Microsoft 认证/刷新及正版/离线 1.16.5 启动；真实授权由用户在官方页面完成，令牌只保存为本机 DPAPI 密文。未验证联机或 Linux/macOS 系统密钥环。完整调用示例另从文档提取后单独编译，离线流程实际执行通过。

协议参考：[Microsoft 设备授权](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-device-code)、[Microsoft OAuth 与刷新](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth2-auth-code-flow)、[Xbox 网站认证契约](https://learn.microsoft.com/en-us/gaming/gdk/docs/services/fundamentals/s2s-auth-calls/service-authentication/live-website-authentication)、[RFC 8628](https://www.rfc-editor.org/rfc/rfc8628)、[Microsoft DPAPI 包](https://www.nuget.org/packages/System.Security.Cryptography.ProtectedData/10.0.10)。Minecraft 资格响应兼容性另核对了 [Prism 上游资格请求](https://github.com/PrismLauncher/PrismLauncher/blob/develop/launcher/minecraft/auth/steps/EntitlementsStep.cpp)和[响应解析](https://github.com/PrismLauncher/PrismLauncher/blob/develop/launcher/minecraft/auth/Parsers.cpp)；未借用其 Client ID，亦未以这些资料代替真实账号验证。

## 独立调用示例

- [离线登录](examples/accounts-offline.cs)
- [正版设备码登录](examples/accounts-microsoft.cs)
- [账号列表](examples/accounts-list.cs)
- [切换当前账号](examples/accounts-switch.cs)
- [持久化默认账号](examples/accounts-default.cs)
- [刷新身份](examples/accounts-refresh.cs)
- [获取启动身份](examples/accounts-identity.cs)
- [删除本地账号](examples/accounts-delete.cs)
- [授权进度与取消（高级）](examples/accounts-advanced.cs)
