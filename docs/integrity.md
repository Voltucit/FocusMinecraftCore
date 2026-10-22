# 完整性检查与按需修复

本手册对应 1.0.0。每项操作的 using、参数与调用均在独立源码中；编译/运行方式见[示例目录](examples.md)。

## 最简调用

```csharp
using FMCore.Installer;

var result = await new GameInstaller(".minecraft").VerifyAsync("1.16.5");
Console.WriteLine(result);
```


入口仍为 `FMCore.Installer.GameInstaller` 的 `VerifyAsync` / `RepairAsync`。1.0.0 包含原生文件检查；相对早期 P0 预览版，默认校验范围有所扩大，见下文兼容说明。

## 最简完整示例

检查默认仅读取缓存元数据和本地文件，不联网、不创建或修改游戏文件。修复复用现有安装流程，只下载缺失或未通过现有元数据校验的文件；完整文件直接复用。

独立、可编译的调用代码见[本模块示例](examples.md#integrity)。

未缓存版本元数据时，检查会返回已有版本模块的结构化异常，不会暗中下载元数据。缺少或损坏资源索引时，`AssetsChecked=false`，不能据此认为所有资源对象已经检查。

## 检查范围和返回值

| 范围 | 校验来源 | 行为 |
| --- | --- | --- |
| 客户端、Libraries、原生压缩包、日志配置 | `CreatePlan` 对平台规则及继承解析后的清单 | 复用 Downloader 的大小/哈希校验 |
| 资源索引和资源对象 | 同一个索引请求、`AssetManager.CreateDownloads` | 索引校验通过后才能枚举对象 |
| 旧版 virtual / resources 副本 | 与生成布局共用的目标选择逻辑 | 按索引 SHA-1/大小检查，不另建资源下载规则 |
| 解压后的原生文件（T08） | 同一批 `plan.NativeArchives`、条目排除/冲突/限额规则 | 将本地文件与压缩包条目的长度和 SHA-256 比较，不维护另一份原生下载清单 |

`InstallationCheckResult.Files` 包含本次实际枚举的结果，`Problems` 包含缺失、大小错误及哈希错误。`AssetsChecked` 表示资源对象是否可枚举；新增 `NativeFilesChecked` 表示是否实际完成原生文件检查。原生压缩包缺失/损坏时，不解析该无效来源，`NativeFilesChecked=false`，压缩包自身的问题仍在 `Problems` 中。

`IsValid` 针对选择的检查范围；主动跳过原生检查不代表原生文件有效。`FileVerificationState.Unverified` 表示元数据没有提供大小和哈希；仅提供大小时也不能证明文件经过密码学哈希认证。本地原生文件与本地压缩包相符，同样不代表无哈希来源的压缩包已经过官方真实性验证。

## 高级配置、取消和独立实例

以下示例使用本地已缓存的版本，并依据实际选中的 Java 构建规则上下文。`GameDirectory` 为共享根下的相对目录，仅影响实例资源副本；共享客户端、Libraries 和资源对象仍位于根目录。

独立、可编译的调用代码见[本模块示例](examples.md#integrity)。

主流程仍使用 `InstallAsync` 的下载复用、安装锁、原子替换、旧版布局生成和安全解压。只损坏解压后的 DLL 时，使用已验证的原生压缩包重新生成目录，HTTP 下载为零。原生目录为 SDK 专用生成目录，修复会重建整个该目录，移除其中过期/额外文件；不是仅替换某个 DLL。

修复不清理未知游戏文件，不修改存档、Mod、用户设置或其他版本客户端；共享清单文件仅在需要时修复。它不是全目录事务，受控取消或失败后，已完成的有效下载保留供重试。不得为正在运行的游戏绕过文件锁强制替换 DLL。

`NativeExtractor.VerifyAsync(plan.NativeArchives, plan.NativeDirectory, progress, ct)` 可直接供高级调用方检查原生文件；它与 `ExtractAsync` 使用同一个有界读取器，复用路径、链接、条目排除和同名冲突规则。`NativeExtractionProgress` 的计数单位为压缩包；新增的 `InstallationStage.NativeVerification` 在现有枚举末尾追加，原阶段数值未变。

## 兼容性变化与限制

`VerifyAsync` 的签名保持不变，但当前源码默认检查解压后的原生文件；因此过去漏掉的 DLL 缺失/损坏现在会使 `IsValid=false`。只需旧的下载文件检查范围时，设置 `VerifyNativeFiles=false`，并检查 `NativeFilesChecked`。启动模块对每次新建的私有 natives 目录显式跳过这一提前检查，随后仍安全解压，避免把尚未创建的目录当作安装损坏。

检查不会执行所有 Java 类、验证世界或 Mod 的语义，也不能证明游戏必然可以启动。错误沿用 `InstallationException`、`AssetException`、`VersionException`、`DownloadException`；取消抛 `OperationCanceledException`。日志信号不会自动触发修复，调用方应结合[日志与诊断](logging.md)保留证据后决定下一步。

T08 在 Windows x64 使用真实临时文件、Fixtures 和模拟下载，覆盖各类文件缺失/同大小损坏、按需请求范围、原生文件零下载修复及存档/Mod/其他版本保护。未以这些测试替代真实游戏或其他平台验收。

T09 处理器产物与支持文件同样进入既有安装清单，GeneratedFiles 不自动加入 Classpath；普通远程文件仍按需下载，生成文件无法由通用 RepairAsync 重新生成时明确返回 LocalFileUnavailable，应重新运行相应加载器的 InstallAsync。没有新增平行校验规则。

## 独立调用示例

- [完整性校验](examples/game-verify.cs)
- [文件修复](examples/game-repair.cs)
- [高级校验范围（高级）](examples/game-verify-advanced.cs)
