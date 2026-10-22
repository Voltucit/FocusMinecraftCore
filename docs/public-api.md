# 1.0.0 公开 API 完整清单

由 Release 程序集反射生成。包含公开类型、声明的公开/受保护成员、属性访问器（含 init）、事件、参数默认值和枚举常量；包含 record 编译器生成成员，不重复 .NET 基类继承成员。可空注解以源码与 IDE 为准。

195 个导出类型、3280 条记录。相对 T10 没有类型/方法/属性/枚举值移除；两处构造函数默认 User-Agent 的联系链接更新为规范仓库地址，详见[兼容说明](api-changes.md)。

## Assets

```text
F FMCore.Assets.AssetErrorCode.HashMismatch : FMCore.Assets.AssetErrorCode = 3
F FMCore.Assets.AssetErrorCode.InvalidIndex : FMCore.Assets.AssetErrorCode = 0
F FMCore.Assets.AssetErrorCode.IoError : FMCore.Assets.AssetErrorCode = 4
F FMCore.Assets.AssetErrorCode.MissingObject : FMCore.Assets.AssetErrorCode = 2
F FMCore.Assets.AssetErrorCode.UnsafePath : FMCore.Assets.AssetErrorCode = 1
M FMCore.Assets.AssetException..ctor(FMCore.Assets.AssetErrorCode code, System.String message) -> constructor | instance public
M FMCore.Assets.AssetException.get_Code() -> FMCore.Assets.AssetErrorCode | instance public
M FMCore.Assets.AssetIndex..ctor(System.Collections.Generic.IReadOnlyList`1[FMCore.Assets.AssetObject] Objects, System.Boolean Virtual, System.Boolean MapToResources) -> constructor | instance public
M FMCore.Assets.AssetIndex.<Clone>$() -> FMCore.Assets.AssetIndex | instance public
M FMCore.Assets.AssetIndex.Deconstruct(System.Collections.Generic.IReadOnlyList`1[FMCore.Assets.AssetObject]& Objects, System.Boolean& Virtual, System.Boolean& MapToResources) -> System.Void | instance public
M FMCore.Assets.AssetIndex.Equals(FMCore.Assets.AssetIndex other) -> System.Boolean | instance public
M FMCore.Assets.AssetIndex.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Assets.AssetIndex.GetHashCode() -> System.Int32 | instance public
M FMCore.Assets.AssetIndex.Parse(System.String json) -> FMCore.Assets.AssetIndex | static public
M FMCore.Assets.AssetIndex.ToString() -> System.String | instance public
M FMCore.Assets.AssetIndex.get_MapToResources() -> System.Boolean | instance public
M FMCore.Assets.AssetIndex.get_Objects() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Assets.AssetObject] | instance public
M FMCore.Assets.AssetIndex.get_Virtual() -> System.Boolean | instance public
M FMCore.Assets.AssetIndex.op_Equality(FMCore.Assets.AssetIndex left, FMCore.Assets.AssetIndex right) -> System.Boolean | static public
M FMCore.Assets.AssetIndex.op_Inequality(FMCore.Assets.AssetIndex left, FMCore.Assets.AssetIndex right) -> System.Boolean | static public
M FMCore.Assets.AssetIndex.set_MapToResources(System.Boolean value) -> System.Void | instance public
M FMCore.Assets.AssetIndex.set_Objects(System.Collections.Generic.IReadOnlyList`1[FMCore.Assets.AssetObject] value) -> System.Void | instance public
M FMCore.Assets.AssetIndex.set_Virtual(System.Boolean value) -> System.Void | instance public
M FMCore.Assets.AssetIndexResult..ctor(FMCore.Assets.AssetIndex Index, FMCore.Downloader.DownloadResult Download) -> constructor | instance public
M FMCore.Assets.AssetIndexResult.<Clone>$() -> FMCore.Assets.AssetIndexResult | instance public
M FMCore.Assets.AssetIndexResult.Deconstruct(FMCore.Assets.AssetIndex& Index, FMCore.Downloader.DownloadResult& Download) -> System.Void | instance public
M FMCore.Assets.AssetIndexResult.Equals(FMCore.Assets.AssetIndexResult other) -> System.Boolean | instance public
M FMCore.Assets.AssetIndexResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Assets.AssetIndexResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Assets.AssetIndexResult.ToString() -> System.String | instance public
M FMCore.Assets.AssetIndexResult.get_Download() -> FMCore.Downloader.DownloadResult | instance public
M FMCore.Assets.AssetIndexResult.get_Index() -> FMCore.Assets.AssetIndex | instance public
M FMCore.Assets.AssetIndexResult.op_Equality(FMCore.Assets.AssetIndexResult left, FMCore.Assets.AssetIndexResult right) -> System.Boolean | static public
M FMCore.Assets.AssetIndexResult.op_Inequality(FMCore.Assets.AssetIndexResult left, FMCore.Assets.AssetIndexResult right) -> System.Boolean | static public
M FMCore.Assets.AssetIndexResult.set_Download(FMCore.Downloader.DownloadResult value) -> System.Void | instance public
M FMCore.Assets.AssetIndexResult.set_Index(FMCore.Assets.AssetIndex value) -> System.Void | instance public
M FMCore.Assets.AssetLayoutProgress..ctor(System.Int32 CompletedFiles, System.Int32 TotalFiles) -> constructor | instance public
M FMCore.Assets.AssetLayoutProgress.<Clone>$() -> FMCore.Assets.AssetLayoutProgress | instance public
M FMCore.Assets.AssetLayoutProgress.Deconstruct(System.Int32& CompletedFiles, System.Int32& TotalFiles) -> System.Void | instance public
M FMCore.Assets.AssetLayoutProgress.Equals(FMCore.Assets.AssetLayoutProgress other) -> System.Boolean | instance public
M FMCore.Assets.AssetLayoutProgress.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Assets.AssetLayoutProgress.GetHashCode() -> System.Int32 | instance public
M FMCore.Assets.AssetLayoutProgress.ToString() -> System.String | instance public
M FMCore.Assets.AssetLayoutProgress.get_CompletedFiles() -> System.Int32 | instance public
M FMCore.Assets.AssetLayoutProgress.get_TotalFiles() -> System.Int32 | instance public
M FMCore.Assets.AssetLayoutProgress.op_Equality(FMCore.Assets.AssetLayoutProgress left, FMCore.Assets.AssetLayoutProgress right) -> System.Boolean | static public
M FMCore.Assets.AssetLayoutProgress.op_Inequality(FMCore.Assets.AssetLayoutProgress left, FMCore.Assets.AssetLayoutProgress right) -> System.Boolean | static public
M FMCore.Assets.AssetLayoutProgress.set_CompletedFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Assets.AssetLayoutProgress.set_TotalFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Assets.AssetLayoutResult..ctor(System.String AssetsRootDirectory, System.String GameAssetsDirectory, System.String ResourcesDirectory) -> constructor | instance public
M FMCore.Assets.AssetLayoutResult.<Clone>$() -> FMCore.Assets.AssetLayoutResult | instance public
M FMCore.Assets.AssetLayoutResult.Deconstruct(System.String& AssetsRootDirectory, System.String& GameAssetsDirectory, System.String& ResourcesDirectory) -> System.Void | instance public
M FMCore.Assets.AssetLayoutResult.Equals(FMCore.Assets.AssetLayoutResult other) -> System.Boolean | instance public
M FMCore.Assets.AssetLayoutResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Assets.AssetLayoutResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Assets.AssetLayoutResult.ToString() -> System.String | instance public
M FMCore.Assets.AssetLayoutResult.get_AssetsRootDirectory() -> System.String | instance public
M FMCore.Assets.AssetLayoutResult.get_GameAssetsDirectory() -> System.String | instance public
M FMCore.Assets.AssetLayoutResult.get_ResourcesDirectory() -> System.String | instance public
M FMCore.Assets.AssetLayoutResult.op_Equality(FMCore.Assets.AssetLayoutResult left, FMCore.Assets.AssetLayoutResult right) -> System.Boolean | static public
M FMCore.Assets.AssetLayoutResult.op_Inequality(FMCore.Assets.AssetLayoutResult left, FMCore.Assets.AssetLayoutResult right) -> System.Boolean | static public
M FMCore.Assets.AssetLayoutResult.set_AssetsRootDirectory(System.String value) -> System.Void | instance public
M FMCore.Assets.AssetLayoutResult.set_GameAssetsDirectory(System.String value) -> System.Void | instance public
M FMCore.Assets.AssetLayoutResult.set_ResourcesDirectory(System.String value) -> System.Void | instance public
M FMCore.Assets.AssetManager..ctor(FMCore.Downloader.DownloadManager downloads) -> constructor | instance public
M FMCore.Assets.AssetManager.CreateDownloads(FMCore.Assets.AssetIndex index) -> System.Collections.Generic.IReadOnlyList`1[FMCore.Downloader.DownloadRequest] | instance public
M FMCore.Assets.AssetManager.GetIndexAsync(FMCore.Versions.AssetIndexInfo info, System.IProgress`1[FMCore.Downloader.DownloadProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Assets.AssetIndexResult] | instance public
M FMCore.Assets.AssetManager.MaterializeAsync(FMCore.Assets.AssetIndex index, System.String indexId, System.String gameDirectory = "", System.IProgress`1[FMCore.Assets.AssetLayoutProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Assets.AssetLayoutResult] | instance public
M FMCore.Assets.AssetManager.get_RootDirectory() -> System.String | instance public
M FMCore.Assets.AssetObject..ctor(System.String Name, System.String Hash, System.Int64 Size) -> constructor | instance public
M FMCore.Assets.AssetObject.<Clone>$() -> FMCore.Assets.AssetObject | instance public
M FMCore.Assets.AssetObject.Deconstruct(System.String& Name, System.String& Hash, System.Int64& Size) -> System.Void | instance public
M FMCore.Assets.AssetObject.Equals(FMCore.Assets.AssetObject other) -> System.Boolean | instance public
M FMCore.Assets.AssetObject.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Assets.AssetObject.GetHashCode() -> System.Int32 | instance public
M FMCore.Assets.AssetObject.ToString() -> System.String | instance public
M FMCore.Assets.AssetObject.get_Hash() -> System.String | instance public
M FMCore.Assets.AssetObject.get_Name() -> System.String | instance public
M FMCore.Assets.AssetObject.get_RelativePath() -> System.String | instance public
M FMCore.Assets.AssetObject.get_Size() -> System.Int64 | instance public
M FMCore.Assets.AssetObject.op_Equality(FMCore.Assets.AssetObject left, FMCore.Assets.AssetObject right) -> System.Boolean | static public
M FMCore.Assets.AssetObject.op_Inequality(FMCore.Assets.AssetObject left, FMCore.Assets.AssetObject right) -> System.Boolean | static public
M FMCore.Assets.AssetObject.set_Hash(System.String value) -> System.Void | instance public
M FMCore.Assets.AssetObject.set_Name(System.String value) -> System.Void | instance public
M FMCore.Assets.AssetObject.set_Size(System.Int64 value) -> System.Void | instance public
P FMCore.Assets.AssetException.Code : FMCore.Assets.AssetErrorCode | get_Code
P FMCore.Assets.AssetIndex.MapToResources : System.Boolean | get_MapToResources,set_MapToResources[init]
P FMCore.Assets.AssetIndex.Objects : System.Collections.Generic.IReadOnlyList`1[FMCore.Assets.AssetObject] | get_Objects,set_Objects[init]
P FMCore.Assets.AssetIndex.Virtual : System.Boolean | get_Virtual,set_Virtual[init]
P FMCore.Assets.AssetIndexResult.Download : FMCore.Downloader.DownloadResult | get_Download,set_Download[init]
P FMCore.Assets.AssetIndexResult.Index : FMCore.Assets.AssetIndex | get_Index,set_Index[init]
P FMCore.Assets.AssetLayoutProgress.CompletedFiles : System.Int32 | get_CompletedFiles,set_CompletedFiles[init]
P FMCore.Assets.AssetLayoutProgress.TotalFiles : System.Int32 | get_TotalFiles,set_TotalFiles[init]
P FMCore.Assets.AssetLayoutResult.AssetsRootDirectory : System.String | get_AssetsRootDirectory,set_AssetsRootDirectory[init]
P FMCore.Assets.AssetLayoutResult.GameAssetsDirectory : System.String | get_GameAssetsDirectory,set_GameAssetsDirectory[init]
P FMCore.Assets.AssetLayoutResult.ResourcesDirectory : System.String | get_ResourcesDirectory,set_ResourcesDirectory[init]
P FMCore.Assets.AssetManager.RootDirectory : System.String | get_RootDirectory
P FMCore.Assets.AssetObject.Hash : System.String | get_Hash,set_Hash[init]
P FMCore.Assets.AssetObject.Name : System.String | get_Name,set_Name[init]
P FMCore.Assets.AssetObject.RelativePath : System.String | get_RelativePath
P FMCore.Assets.AssetObject.Size : System.Int64 | get_Size,set_Size[init]
T FMCore.Assets.AssetErrorCode : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Assets.AssetException : System.Exception | System.Runtime.Serialization.ISerializable
T FMCore.Assets.AssetIndex : System.Object | System.IEquatable`1[FMCore.Assets.AssetIndex]
T FMCore.Assets.AssetIndexResult : System.Object | System.IEquatable`1[FMCore.Assets.AssetIndexResult]
T FMCore.Assets.AssetLayoutProgress : System.Object | System.IEquatable`1[FMCore.Assets.AssetLayoutProgress]
T FMCore.Assets.AssetLayoutResult : System.Object | System.IEquatable`1[FMCore.Assets.AssetLayoutResult]
T FMCore.Assets.AssetManager : System.Object |
T FMCore.Assets.AssetObject : System.Object | System.IEquatable`1[FMCore.Assets.AssetObject]
```

## Authentication

```text
E FMCore.Authentication.AccountManager.AccountsChanged : System.EventHandler`1[FMCore.Authentication.AccountChangedEventArgs]
F FMCore.Authentication.AccountChangeKind.Added : FMCore.Authentication.AccountChangeKind = 0
F FMCore.Authentication.AccountChangeKind.DefaultChanged : FMCore.Authentication.AccountChangeKind = 3
F FMCore.Authentication.AccountChangeKind.Removed : FMCore.Authentication.AccountChangeKind = 4
F FMCore.Authentication.AccountChangeKind.Selected : FMCore.Authentication.AccountChangeKind = 2
F FMCore.Authentication.AccountChangeKind.Updated : FMCore.Authentication.AccountChangeKind = 1
F FMCore.Authentication.AccountKind.Microsoft : FMCore.Authentication.AccountKind = 1
F FMCore.Authentication.AccountKind.Offline : FMCore.Authentication.AccountKind = 0
F FMCore.Authentication.AuthenticationErrorCode.AccountNotFound : FMCore.Authentication.AuthenticationErrorCode = 1
F FMCore.Authentication.AuthenticationErrorCode.AuthorizationDeclined : FMCore.Authentication.AuthenticationErrorCode = 5
F FMCore.Authentication.AuthenticationErrorCode.ClientRejected : FMCore.Authentication.AuthenticationErrorCode = 7
F FMCore.Authentication.AuthenticationErrorCode.DeviceCodeExpired : FMCore.Authentication.AuthenticationErrorCode = 6
F FMCore.Authentication.AuthenticationErrorCode.GameNotOwned : FMCore.Authentication.AuthenticationErrorCode = 12
F FMCore.Authentication.AuthenticationErrorCode.IdentityMismatch : FMCore.Authentication.AuthenticationErrorCode = 15
F FMCore.Authentication.AuthenticationErrorCode.InvalidInput : FMCore.Authentication.AuthenticationErrorCode = 0
F FMCore.Authentication.AuthenticationErrorCode.InvalidResponse : FMCore.Authentication.AuthenticationErrorCode = 4
F FMCore.Authentication.AuthenticationErrorCode.MicrosoftRejected : FMCore.Authentication.AuthenticationErrorCode = 8
F FMCore.Authentication.AuthenticationErrorCode.MinecraftRejected : FMCore.Authentication.AuthenticationErrorCode = 11
F FMCore.Authentication.AuthenticationErrorCode.NetworkError : FMCore.Authentication.AuthenticationErrorCode = 2
F FMCore.Authentication.AuthenticationErrorCode.ProfileMissing : FMCore.Authentication.AuthenticationErrorCode = 13
F FMCore.Authentication.AuthenticationErrorCode.ReauthenticationRequired : FMCore.Authentication.AuthenticationErrorCode = 14
F FMCore.Authentication.AuthenticationErrorCode.RequestTimeout : FMCore.Authentication.AuthenticationErrorCode = 3
F FMCore.Authentication.AuthenticationErrorCode.StorageCorrupted : FMCore.Authentication.AuthenticationErrorCode = 18
F FMCore.Authentication.AuthenticationErrorCode.StorageError : FMCore.Authentication.AuthenticationErrorCode = 17
F FMCore.Authentication.AuthenticationErrorCode.StorageUnavailable : FMCore.Authentication.AuthenticationErrorCode = 16
F FMCore.Authentication.AuthenticationErrorCode.StoreBusy : FMCore.Authentication.AuthenticationErrorCode = 19
F FMCore.Authentication.AuthenticationErrorCode.XboxRejected : FMCore.Authentication.AuthenticationErrorCode = 9
F FMCore.Authentication.AuthenticationErrorCode.XstsRejected : FMCore.Authentication.AuthenticationErrorCode = 10
F FMCore.Authentication.AuthenticationStage.Completed : FMCore.Authentication.AuthenticationStage = 8
F FMCore.Authentication.AuthenticationStage.DeviceAuthorization : FMCore.Authentication.AuthenticationStage = 0
F FMCore.Authentication.AuthenticationStage.Entitlements : FMCore.Authentication.AuthenticationStage = 6
F FMCore.Authentication.AuthenticationStage.MicrosoftToken : FMCore.Authentication.AuthenticationStage = 2
F FMCore.Authentication.AuthenticationStage.MinecraftToken : FMCore.Authentication.AuthenticationStage = 5
F FMCore.Authentication.AuthenticationStage.Profile : FMCore.Authentication.AuthenticationStage = 7
F FMCore.Authentication.AuthenticationStage.WaitingForAuthorization : FMCore.Authentication.AuthenticationStage = 1
F FMCore.Authentication.AuthenticationStage.XboxUserToken : FMCore.Authentication.AuthenticationStage = 3
F FMCore.Authentication.AuthenticationStage.XstsToken : FMCore.Authentication.AuthenticationStage = 4
M FMCore.Authentication.AccountChangedEventArgs..ctor(FMCore.Authentication.AccountChangeKind change, FMCore.Authentication.AccountSnapshot snapshot) -> constructor | instance public
M FMCore.Authentication.AccountChangedEventArgs.get_Change() -> FMCore.Authentication.AccountChangeKind | instance public
M FMCore.Authentication.AccountChangedEventArgs.get_Snapshot() -> FMCore.Authentication.AccountSnapshot | instance public
M FMCore.Authentication.AccountInfo..ctor(System.Guid Id, FMCore.Authentication.AccountKind Kind, System.String Name, System.Guid Uuid, System.Boolean HasGameEntitlement, System.DateTimeOffset CreatedAt, System.Nullable`1[System.DateTimeOffset] LastVerifiedAt) -> constructor | instance public
M FMCore.Authentication.AccountInfo.<Clone>$() -> FMCore.Authentication.AccountInfo | instance public
M FMCore.Authentication.AccountInfo.Deconstruct(System.Guid& Id, FMCore.Authentication.AccountKind& Kind, System.String& Name, System.Guid& Uuid, System.Boolean& HasGameEntitlement, System.DateTimeOffset& CreatedAt, System.Nullable`1[System.DateTimeOffset]& LastVerifiedAt) -> System.Void | instance public
M FMCore.Authentication.AccountInfo.Equals(FMCore.Authentication.AccountInfo other) -> System.Boolean | instance public
M FMCore.Authentication.AccountInfo.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Authentication.AccountInfo.GetHashCode() -> System.Int32 | instance public
M FMCore.Authentication.AccountInfo.ToString() -> System.String | instance public
M FMCore.Authentication.AccountInfo.get_CreatedAt() -> System.DateTimeOffset | instance public
M FMCore.Authentication.AccountInfo.get_HasGameEntitlement() -> System.Boolean | instance public
M FMCore.Authentication.AccountInfo.get_Id() -> System.Guid | instance public
M FMCore.Authentication.AccountInfo.get_Kind() -> FMCore.Authentication.AccountKind | instance public
M FMCore.Authentication.AccountInfo.get_LastVerifiedAt() -> System.Nullable`1[System.DateTimeOffset] | instance public
M FMCore.Authentication.AccountInfo.get_Name() -> System.String | instance public
M FMCore.Authentication.AccountInfo.get_Uuid() -> System.Guid | instance public
M FMCore.Authentication.AccountInfo.op_Equality(FMCore.Authentication.AccountInfo left, FMCore.Authentication.AccountInfo right) -> System.Boolean | static public
M FMCore.Authentication.AccountInfo.op_Inequality(FMCore.Authentication.AccountInfo left, FMCore.Authentication.AccountInfo right) -> System.Boolean | static public
M FMCore.Authentication.AccountInfo.set_CreatedAt(System.DateTimeOffset value) -> System.Void | instance public
M FMCore.Authentication.AccountInfo.set_HasGameEntitlement(System.Boolean value) -> System.Void | instance public
M FMCore.Authentication.AccountInfo.set_Id(System.Guid value) -> System.Void | instance public
M FMCore.Authentication.AccountInfo.set_Kind(FMCore.Authentication.AccountKind value) -> System.Void | instance public
M FMCore.Authentication.AccountInfo.set_LastVerifiedAt(System.Nullable`1[System.DateTimeOffset] value) -> System.Void | instance public
M FMCore.Authentication.AccountInfo.set_Name(System.String value) -> System.Void | instance public
M FMCore.Authentication.AccountInfo.set_Uuid(System.Guid value) -> System.Void | instance public
M FMCore.Authentication.AccountManager..ctor(System.String directory, FMCore.Authentication.IAccountTokenStore tokenStore = null) -> constructor | instance public
M FMCore.Authentication.AccountManager.CreateOfflineAccountAsync(System.String name, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Authentication.AccountInfo] | instance public
M FMCore.Authentication.AccountManager.DeleteAsync(System.Guid accountId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Boolean] | instance public
M FMCore.Authentication.AccountManager.Dispose() -> System.Void | instance public
M FMCore.Authentication.AccountManager.GetAccountsAsync(System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Authentication.AccountSnapshot] | instance public
M FMCore.Authentication.AccountManager.GetLaunchIdentityAsync(System.Nullable`1[System.Guid] accountId = default, System.IProgress`1[FMCore.Authentication.AuthenticationProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Authentication.LaunchIdentity] | instance public
M FMCore.Authentication.AccountManager.LoginMicrosoftAsync(FMCore.Authentication.MicrosoftLoginOptions options, System.Func`3[FMCore.Authentication.DeviceCodeInfo,System.Threading.CancellationToken,System.Threading.Tasks.Task] showDeviceCode, System.IProgress`1[FMCore.Authentication.AuthenticationProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Authentication.LaunchIdentity] | instance public
M FMCore.Authentication.AccountManager.LoginOfflineAsync(System.String name, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Authentication.LaunchIdentity] | instance public
M FMCore.Authentication.AccountManager.RefreshAsync(System.Guid accountId, System.IProgress`1[FMCore.Authentication.AuthenticationProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Authentication.LaunchIdentity] | instance public
M FMCore.Authentication.AccountManager.SetDefaultAsync(System.Nullable`1[System.Guid] accountId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task | instance public
M FMCore.Authentication.AccountManager.SwitchAsync(System.Guid accountId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Authentication.AccountInfo] | instance public
M FMCore.Authentication.AccountManager.add_AccountsChanged(System.EventHandler`1[FMCore.Authentication.AccountChangedEventArgs] value) -> System.Void | instance public
M FMCore.Authentication.AccountManager.remove_AccountsChanged(System.EventHandler`1[FMCore.Authentication.AccountChangedEventArgs] value) -> System.Void | instance public
M FMCore.Authentication.AccountSnapshot..ctor(System.Collections.Generic.IReadOnlyList`1[FMCore.Authentication.AccountInfo] Accounts, System.Nullable`1[System.Guid] SelectedAccountId, System.Nullable`1[System.Guid] DefaultAccountId) -> constructor | instance public
M FMCore.Authentication.AccountSnapshot.<Clone>$() -> FMCore.Authentication.AccountSnapshot | instance public
M FMCore.Authentication.AccountSnapshot.Deconstruct(System.Collections.Generic.IReadOnlyList`1[FMCore.Authentication.AccountInfo]& Accounts, System.Nullable`1[System.Guid]& SelectedAccountId, System.Nullable`1[System.Guid]& DefaultAccountId) -> System.Void | instance public
M FMCore.Authentication.AccountSnapshot.Equals(FMCore.Authentication.AccountSnapshot other) -> System.Boolean | instance public
M FMCore.Authentication.AccountSnapshot.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Authentication.AccountSnapshot.GetHashCode() -> System.Int32 | instance public
M FMCore.Authentication.AccountSnapshot.ToString() -> System.String | instance public
M FMCore.Authentication.AccountSnapshot.get_Accounts() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Authentication.AccountInfo] | instance public
M FMCore.Authentication.AccountSnapshot.get_DefaultAccountId() -> System.Nullable`1[System.Guid] | instance public
M FMCore.Authentication.AccountSnapshot.get_SelectedAccountId() -> System.Nullable`1[System.Guid] | instance public
M FMCore.Authentication.AccountSnapshot.op_Equality(FMCore.Authentication.AccountSnapshot left, FMCore.Authentication.AccountSnapshot right) -> System.Boolean | static public
M FMCore.Authentication.AccountSnapshot.op_Inequality(FMCore.Authentication.AccountSnapshot left, FMCore.Authentication.AccountSnapshot right) -> System.Boolean | static public
M FMCore.Authentication.AccountSnapshot.set_Accounts(System.Collections.Generic.IReadOnlyList`1[FMCore.Authentication.AccountInfo] value) -> System.Void | instance public
M FMCore.Authentication.AccountSnapshot.set_DefaultAccountId(System.Nullable`1[System.Guid] value) -> System.Void | instance public
M FMCore.Authentication.AccountSnapshot.set_SelectedAccountId(System.Nullable`1[System.Guid] value) -> System.Void | instance public
M FMCore.Authentication.AuthenticationException..ctor(FMCore.Authentication.AuthenticationErrorCode code, System.String message, System.Nullable`1[FMCore.Authentication.AuthenticationStage] stage = default, System.Nullable`1[System.Net.HttpStatusCode] statusCode = default, System.Nullable`1[System.Int64] xboxError = default) -> constructor | instance public
M FMCore.Authentication.AuthenticationException.get_Code() -> FMCore.Authentication.AuthenticationErrorCode | instance public
M FMCore.Authentication.AuthenticationException.get_Stage() -> System.Nullable`1[FMCore.Authentication.AuthenticationStage] | instance public
M FMCore.Authentication.AuthenticationException.get_StatusCode() -> System.Nullable`1[System.Net.HttpStatusCode] | instance public
M FMCore.Authentication.AuthenticationException.get_XboxError() -> System.Nullable`1[System.Int64] | instance public
M FMCore.Authentication.AuthenticationProgress..ctor(FMCore.Authentication.AuthenticationStage Stage) -> constructor | instance public
M FMCore.Authentication.AuthenticationProgress.<Clone>$() -> FMCore.Authentication.AuthenticationProgress | instance public
M FMCore.Authentication.AuthenticationProgress.Deconstruct(FMCore.Authentication.AuthenticationStage& Stage) -> System.Void | instance public
M FMCore.Authentication.AuthenticationProgress.Equals(FMCore.Authentication.AuthenticationProgress other) -> System.Boolean | instance public
M FMCore.Authentication.AuthenticationProgress.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Authentication.AuthenticationProgress.GetHashCode() -> System.Int32 | instance public
M FMCore.Authentication.AuthenticationProgress.ToString() -> System.String | instance public
M FMCore.Authentication.AuthenticationProgress.get_Stage() -> FMCore.Authentication.AuthenticationStage | instance public
M FMCore.Authentication.AuthenticationProgress.op_Equality(FMCore.Authentication.AuthenticationProgress left, FMCore.Authentication.AuthenticationProgress right) -> System.Boolean | static public
M FMCore.Authentication.AuthenticationProgress.op_Inequality(FMCore.Authentication.AuthenticationProgress left, FMCore.Authentication.AuthenticationProgress right) -> System.Boolean | static public
M FMCore.Authentication.AuthenticationProgress.set_Stage(FMCore.Authentication.AuthenticationStage value) -> System.Void | instance public
M FMCore.Authentication.DeviceCodeInfo.ToString() -> System.String | instance public
M FMCore.Authentication.DeviceCodeInfo.get_ExpiresAt() -> System.DateTimeOffset | instance public
M FMCore.Authentication.DeviceCodeInfo.get_UserCode() -> System.String | instance public
M FMCore.Authentication.DeviceCodeInfo.get_VerificationUri() -> System.Uri | instance public
M FMCore.Authentication.IAccountTokenStore.DeleteAsync(System.Guid accountId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task | instance public
M FMCore.Authentication.IAccountTokenStore.ReadAsync(System.Guid accountId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Byte[]] | instance public
M FMCore.Authentication.IAccountTokenStore.WriteAsync(System.Guid accountId, System.ReadOnlyMemory`1[System.Byte] payload, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task | instance public
M FMCore.Authentication.LaunchIdentity.ToString() -> System.String | instance public
M FMCore.Authentication.LaunchIdentity.get_AccessToken() -> System.String | instance public
M FMCore.Authentication.LaunchIdentity.get_Account() -> FMCore.Authentication.AccountInfo | instance public
M FMCore.Authentication.LaunchIdentity.get_ClientId() -> System.String | instance public
M FMCore.Authentication.LaunchIdentity.get_ExpiresAt() -> System.Nullable`1[System.DateTimeOffset] | instance public
M FMCore.Authentication.LaunchIdentity.get_IsAuthenticated() -> System.Boolean | instance public
M FMCore.Authentication.LaunchIdentity.get_LegacySession() -> System.String | instance public
M FMCore.Authentication.LaunchIdentity.get_UserType() -> System.String | instance public
M FMCore.Authentication.LaunchIdentity.get_Uuid() -> System.String | instance public
M FMCore.Authentication.LaunchIdentity.get_Xuid() -> System.String | instance public
M FMCore.Authentication.MicrosoftLoginOptions..ctor(System.String ClientId) -> constructor | instance public
M FMCore.Authentication.MicrosoftLoginOptions.<Clone>$() -> FMCore.Authentication.MicrosoftLoginOptions | instance public
M FMCore.Authentication.MicrosoftLoginOptions.Deconstruct(System.String& ClientId) -> System.Void | instance public
M FMCore.Authentication.MicrosoftLoginOptions.Equals(FMCore.Authentication.MicrosoftLoginOptions other) -> System.Boolean | instance public
M FMCore.Authentication.MicrosoftLoginOptions.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Authentication.MicrosoftLoginOptions.GetHashCode() -> System.Int32 | instance public
M FMCore.Authentication.MicrosoftLoginOptions.ToString() -> System.String | instance public
M FMCore.Authentication.MicrosoftLoginOptions.get_ClientId() -> System.String | instance public
M FMCore.Authentication.MicrosoftLoginOptions.get_RequestTimeout() -> System.TimeSpan | instance public
M FMCore.Authentication.MicrosoftLoginOptions.op_Equality(FMCore.Authentication.MicrosoftLoginOptions left, FMCore.Authentication.MicrosoftLoginOptions right) -> System.Boolean | static public
M FMCore.Authentication.MicrosoftLoginOptions.op_Inequality(FMCore.Authentication.MicrosoftLoginOptions left, FMCore.Authentication.MicrosoftLoginOptions right) -> System.Boolean | static public
M FMCore.Authentication.MicrosoftLoginOptions.set_ClientId(System.String value) -> System.Void | instance public
M FMCore.Authentication.MicrosoftLoginOptions.set_RequestTimeout(System.TimeSpan value) -> System.Void | instance public
M FMCore.Authentication.OfflineIdentity.CreateUuid(System.String name) -> System.Guid | static public
M FMCore.Authentication.WindowsDpapiTokenStore..ctor(System.String directory) -> constructor | instance public
M FMCore.Authentication.WindowsDpapiTokenStore.DeleteAsync(System.Guid accountId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task | instance public
M FMCore.Authentication.WindowsDpapiTokenStore.ReadAsync(System.Guid accountId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Byte[]] | instance public
M FMCore.Authentication.WindowsDpapiTokenStore.WriteAsync(System.Guid accountId, System.ReadOnlyMemory`1[System.Byte] payload, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task | instance public
P FMCore.Authentication.AccountChangedEventArgs.Change : FMCore.Authentication.AccountChangeKind | get_Change
P FMCore.Authentication.AccountChangedEventArgs.Snapshot : FMCore.Authentication.AccountSnapshot | get_Snapshot
P FMCore.Authentication.AccountInfo.CreatedAt : System.DateTimeOffset | get_CreatedAt,set_CreatedAt[init]
P FMCore.Authentication.AccountInfo.HasGameEntitlement : System.Boolean | get_HasGameEntitlement,set_HasGameEntitlement[init]
P FMCore.Authentication.AccountInfo.Id : System.Guid | get_Id,set_Id[init]
P FMCore.Authentication.AccountInfo.Kind : FMCore.Authentication.AccountKind | get_Kind,set_Kind[init]
P FMCore.Authentication.AccountInfo.LastVerifiedAt : System.Nullable`1[System.DateTimeOffset] | get_LastVerifiedAt,set_LastVerifiedAt[init]
P FMCore.Authentication.AccountInfo.Name : System.String | get_Name,set_Name[init]
P FMCore.Authentication.AccountInfo.Uuid : System.Guid | get_Uuid,set_Uuid[init]
P FMCore.Authentication.AccountSnapshot.Accounts : System.Collections.Generic.IReadOnlyList`1[FMCore.Authentication.AccountInfo] | get_Accounts,set_Accounts[init]
P FMCore.Authentication.AccountSnapshot.DefaultAccountId : System.Nullable`1[System.Guid] | get_DefaultAccountId,set_DefaultAccountId[init]
P FMCore.Authentication.AccountSnapshot.SelectedAccountId : System.Nullable`1[System.Guid] | get_SelectedAccountId,set_SelectedAccountId[init]
P FMCore.Authentication.AuthenticationException.Code : FMCore.Authentication.AuthenticationErrorCode | get_Code
P FMCore.Authentication.AuthenticationException.Stage : System.Nullable`1[FMCore.Authentication.AuthenticationStage] | get_Stage
P FMCore.Authentication.AuthenticationException.StatusCode : System.Nullable`1[System.Net.HttpStatusCode] | get_StatusCode
P FMCore.Authentication.AuthenticationException.XboxError : System.Nullable`1[System.Int64] | get_XboxError
P FMCore.Authentication.AuthenticationProgress.Stage : FMCore.Authentication.AuthenticationStage | get_Stage,set_Stage[init]
P FMCore.Authentication.DeviceCodeInfo.ExpiresAt : System.DateTimeOffset | get_ExpiresAt
P FMCore.Authentication.DeviceCodeInfo.UserCode : System.String | get_UserCode
P FMCore.Authentication.DeviceCodeInfo.VerificationUri : System.Uri | get_VerificationUri
P FMCore.Authentication.LaunchIdentity.AccessToken : System.String | get_AccessToken
P FMCore.Authentication.LaunchIdentity.Account : FMCore.Authentication.AccountInfo | get_Account
P FMCore.Authentication.LaunchIdentity.ClientId : System.String | get_ClientId
P FMCore.Authentication.LaunchIdentity.ExpiresAt : System.Nullable`1[System.DateTimeOffset] | get_ExpiresAt
P FMCore.Authentication.LaunchIdentity.IsAuthenticated : System.Boolean | get_IsAuthenticated
P FMCore.Authentication.LaunchIdentity.LegacySession : System.String | get_LegacySession
P FMCore.Authentication.LaunchIdentity.UserType : System.String | get_UserType
P FMCore.Authentication.LaunchIdentity.Uuid : System.String | get_Uuid
P FMCore.Authentication.LaunchIdentity.Xuid : System.String | get_Xuid
P FMCore.Authentication.MicrosoftLoginOptions.ClientId : System.String | get_ClientId,set_ClientId[init]
P FMCore.Authentication.MicrosoftLoginOptions.RequestTimeout : System.TimeSpan | get_RequestTimeout,set_RequestTimeout[init]
T FMCore.Authentication.AccountChangeKind : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Authentication.AccountChangedEventArgs : System.EventArgs |
T FMCore.Authentication.AccountInfo : System.Object | System.IEquatable`1[FMCore.Authentication.AccountInfo]
T FMCore.Authentication.AccountKind : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Authentication.AccountManager : System.Object | System.IDisposable
T FMCore.Authentication.AccountSnapshot : System.Object | System.IEquatable`1[FMCore.Authentication.AccountSnapshot]
T FMCore.Authentication.AuthenticationErrorCode : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Authentication.AuthenticationException : System.Exception | System.Runtime.Serialization.ISerializable
T FMCore.Authentication.AuthenticationProgress : System.Object | System.IEquatable`1[FMCore.Authentication.AuthenticationProgress]
T FMCore.Authentication.AuthenticationStage : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Authentication.DeviceCodeInfo : System.Object |
T FMCore.Authentication.IAccountTokenStore :  |
T FMCore.Authentication.LaunchIdentity : System.Object |
T FMCore.Authentication.MicrosoftLoginOptions : System.Object | System.IEquatable`1[FMCore.Authentication.MicrosoftLoginOptions]
T FMCore.Authentication.OfflineIdentity : System.Object |
T FMCore.Authentication.WindowsDpapiTokenStore : System.Object | FMCore.Authentication.IAccountTokenStore
```

## Downloader

```text
F FMCore.Downloader.DownloadErrorCode.DestinationBusy : FMCore.Downloader.DownloadErrorCode = 2
F FMCore.Downloader.DownloadErrorCode.HashMismatch : FMCore.Downloader.DownloadErrorCode = 6
F FMCore.Downloader.DownloadErrorCode.HttpError : FMCore.Downloader.DownloadErrorCode = 3
F FMCore.Downloader.DownloadErrorCode.InvalidRequest : FMCore.Downloader.DownloadErrorCode = 0
F FMCore.Downloader.DownloadErrorCode.IoError : FMCore.Downloader.DownloadErrorCode = 8
F FMCore.Downloader.DownloadErrorCode.LocalFileUnavailable : FMCore.Downloader.DownloadErrorCode = 9
F FMCore.Downloader.DownloadErrorCode.NetworkError : FMCore.Downloader.DownloadErrorCode = 4
F FMCore.Downloader.DownloadErrorCode.SizeMismatch : FMCore.Downloader.DownloadErrorCode = 7
F FMCore.Downloader.DownloadErrorCode.Timeout : FMCore.Downloader.DownloadErrorCode = 5
F FMCore.Downloader.DownloadErrorCode.UnsafePath : FMCore.Downloader.DownloadErrorCode = 1
F FMCore.Downloader.DownloadHashAlgorithm.Sha1 : FMCore.Downloader.DownloadHashAlgorithm = 0
F FMCore.Downloader.DownloadHashAlgorithm.Sha256 : FMCore.Downloader.DownloadHashAlgorithm = 1
F FMCore.Downloader.DownloadHashAlgorithm.Sha512 : FMCore.Downloader.DownloadHashAlgorithm = 2
F FMCore.Downloader.DownloadState.Completed : FMCore.Downloader.DownloadState = 3
F FMCore.Downloader.DownloadState.Downloading : FMCore.Downloader.DownloadState = 1
F FMCore.Downloader.DownloadState.Retrying : FMCore.Downloader.DownloadState = 2
F FMCore.Downloader.DownloadState.Skipped : FMCore.Downloader.DownloadState = 4
F FMCore.Downloader.DownloadState.Started : FMCore.Downloader.DownloadState = 0
F FMCore.Downloader.FileVerificationState.HashMismatch : FMCore.Downloader.FileVerificationState = 4
F FMCore.Downloader.FileVerificationState.Missing : FMCore.Downloader.FileVerificationState = 2
F FMCore.Downloader.FileVerificationState.SizeMismatch : FMCore.Downloader.FileVerificationState = 3
F FMCore.Downloader.FileVerificationState.Unverified : FMCore.Downloader.FileVerificationState = 1
F FMCore.Downloader.FileVerificationState.Valid : FMCore.Downloader.FileVerificationState = 0
M FMCore.Downloader.DownloadException.get_Attempt() -> System.Int32 | instance public
M FMCore.Downloader.DownloadException.get_Code() -> FMCore.Downloader.DownloadErrorCode | instance public
M FMCore.Downloader.DownloadException.get_RelativePath() -> System.String | instance public
M FMCore.Downloader.DownloadException.get_StatusCode() -> System.Nullable`1[System.Net.HttpStatusCode] | instance public
M FMCore.Downloader.DownloadManager..ctor(System.Net.Http.HttpClient httpClient, System.String rootDirectory, FMCore.Downloader.DownloadOptions options = null) -> constructor | instance public
M FMCore.Downloader.DownloadManager..ctor(System.String rootDirectory, FMCore.Downloader.DownloadOptions options = null) -> constructor | instance public
M FMCore.Downloader.DownloadManager.DownloadAsync(FMCore.Downloader.DownloadRequest request, System.IProgress`1[FMCore.Downloader.DownloadProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Downloader.DownloadResult] | instance public
M FMCore.Downloader.DownloadManager.DownloadAsync(System.String url, System.String relativePath, System.IProgress`1[FMCore.Downloader.DownloadProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Downloader.DownloadResult] | instance public
M FMCore.Downloader.DownloadManager.DownloadFileAsync(System.String url, System.String destinationPath, FMCore.Downloader.DownloadOptions options = null, System.IProgress`1[FMCore.Downloader.DownloadProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Downloader.DownloadResult] | static public
M FMCore.Downloader.DownloadManager.DownloadManyAsync(System.Collections.Generic.IEnumerable`1[FMCore.Downloader.DownloadRequest] requests, System.IProgress`1[FMCore.Downloader.DownloadProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Downloader.DownloadResult]] | instance public
M FMCore.Downloader.DownloadManager.VerifyAsync(FMCore.Downloader.DownloadRequest request, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Downloader.FileVerificationResult] | instance public
M FMCore.Downloader.DownloadManager.get_RootDirectory() -> System.String | instance public
M FMCore.Downloader.DownloadOptions..ctor() -> constructor | instance public
M FMCore.Downloader.DownloadOptions.<Clone>$() -> FMCore.Downloader.DownloadOptions | instance public
M FMCore.Downloader.DownloadOptions.Equals(FMCore.Downloader.DownloadOptions other) -> System.Boolean | instance public
M FMCore.Downloader.DownloadOptions.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Downloader.DownloadOptions.GetHashCode() -> System.Int32 | instance public
M FMCore.Downloader.DownloadOptions.ToString() -> System.String | instance public
M FMCore.Downloader.DownloadOptions.get_AttemptTimeout() -> System.TimeSpan | instance public
M FMCore.Downloader.DownloadOptions.get_MaxConcurrency() -> System.Int32 | instance public
M FMCore.Downloader.DownloadOptions.get_MaxRetries() -> System.Int32 | instance public
M FMCore.Downloader.DownloadOptions.get_MaxRetryDelay() -> System.TimeSpan | instance public
M FMCore.Downloader.DownloadOptions.get_RetryDelay() -> System.TimeSpan | instance public
M FMCore.Downloader.DownloadOptions.op_Equality(FMCore.Downloader.DownloadOptions left, FMCore.Downloader.DownloadOptions right) -> System.Boolean | static public
M FMCore.Downloader.DownloadOptions.op_Inequality(FMCore.Downloader.DownloadOptions left, FMCore.Downloader.DownloadOptions right) -> System.Boolean | static public
M FMCore.Downloader.DownloadOptions.set_AttemptTimeout(System.TimeSpan value) -> System.Void | instance public
M FMCore.Downloader.DownloadOptions.set_MaxConcurrency(System.Int32 value) -> System.Void | instance public
M FMCore.Downloader.DownloadOptions.set_MaxRetries(System.Int32 value) -> System.Void | instance public
M FMCore.Downloader.DownloadOptions.set_MaxRetryDelay(System.TimeSpan value) -> System.Void | instance public
M FMCore.Downloader.DownloadOptions.set_RetryDelay(System.TimeSpan value) -> System.Void | instance public
M FMCore.Downloader.DownloadProgress..ctor(System.String RelativePath, System.Int64 BytesReceived, System.Nullable`1[System.Int64] TotalBytes, System.Int32 Attempt, FMCore.Downloader.DownloadState State) -> constructor | instance public
M FMCore.Downloader.DownloadProgress.<Clone>$() -> FMCore.Downloader.DownloadProgress | instance public
M FMCore.Downloader.DownloadProgress.Deconstruct(System.String& RelativePath, System.Int64& BytesReceived, System.Nullable`1[System.Int64]& TotalBytes, System.Int32& Attempt, FMCore.Downloader.DownloadState& State) -> System.Void | instance public
M FMCore.Downloader.DownloadProgress.Equals(FMCore.Downloader.DownloadProgress other) -> System.Boolean | instance public
M FMCore.Downloader.DownloadProgress.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Downloader.DownloadProgress.GetHashCode() -> System.Int32 | instance public
M FMCore.Downloader.DownloadProgress.ToString() -> System.String | instance public
M FMCore.Downloader.DownloadProgress.get_Attempt() -> System.Int32 | instance public
M FMCore.Downloader.DownloadProgress.get_BytesReceived() -> System.Int64 | instance public
M FMCore.Downloader.DownloadProgress.get_RelativePath() -> System.String | instance public
M FMCore.Downloader.DownloadProgress.get_State() -> FMCore.Downloader.DownloadState | instance public
M FMCore.Downloader.DownloadProgress.get_TotalBytes() -> System.Nullable`1[System.Int64] | instance public
M FMCore.Downloader.DownloadProgress.op_Equality(FMCore.Downloader.DownloadProgress left, FMCore.Downloader.DownloadProgress right) -> System.Boolean | static public
M FMCore.Downloader.DownloadProgress.op_Inequality(FMCore.Downloader.DownloadProgress left, FMCore.Downloader.DownloadProgress right) -> System.Boolean | static public
M FMCore.Downloader.DownloadProgress.set_Attempt(System.Int32 value) -> System.Void | instance public
M FMCore.Downloader.DownloadProgress.set_BytesReceived(System.Int64 value) -> System.Void | instance public
M FMCore.Downloader.DownloadProgress.set_RelativePath(System.String value) -> System.Void | instance public
M FMCore.Downloader.DownloadProgress.set_State(FMCore.Downloader.DownloadState value) -> System.Void | instance public
M FMCore.Downloader.DownloadProgress.set_TotalBytes(System.Nullable`1[System.Int64] value) -> System.Void | instance public
M FMCore.Downloader.DownloadRequest..ctor(System.Uri Source, System.String RelativePath) -> constructor | instance public
M FMCore.Downloader.DownloadRequest.<Clone>$() -> FMCore.Downloader.DownloadRequest | instance public
M FMCore.Downloader.DownloadRequest.Deconstruct(System.Uri& Source, System.String& RelativePath) -> System.Void | instance public
M FMCore.Downloader.DownloadRequest.Equals(FMCore.Downloader.DownloadRequest other) -> System.Boolean | instance public
M FMCore.Downloader.DownloadRequest.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Downloader.DownloadRequest.GetHashCode() -> System.Int32 | instance public
M FMCore.Downloader.DownloadRequest.ToString() -> System.String | instance public
M FMCore.Downloader.DownloadRequest.get_ExpectedHash() -> System.String | instance public
M FMCore.Downloader.DownloadRequest.get_ExpectedSize() -> System.Nullable`1[System.Int64] | instance public
M FMCore.Downloader.DownloadRequest.get_HashAlgorithm() -> FMCore.Downloader.DownloadHashAlgorithm | instance public
M FMCore.Downloader.DownloadRequest.get_LocalOnly() -> System.Boolean | instance public
M FMCore.Downloader.DownloadRequest.get_RelativePath() -> System.String | instance public
M FMCore.Downloader.DownloadRequest.get_Source() -> System.Uri | instance public
M FMCore.Downloader.DownloadRequest.op_Equality(FMCore.Downloader.DownloadRequest left, FMCore.Downloader.DownloadRequest right) -> System.Boolean | static public
M FMCore.Downloader.DownloadRequest.op_Inequality(FMCore.Downloader.DownloadRequest left, FMCore.Downloader.DownloadRequest right) -> System.Boolean | static public
M FMCore.Downloader.DownloadRequest.set_ExpectedHash(System.String value) -> System.Void | instance public
M FMCore.Downloader.DownloadRequest.set_ExpectedSize(System.Nullable`1[System.Int64] value) -> System.Void | instance public
M FMCore.Downloader.DownloadRequest.set_HashAlgorithm(FMCore.Downloader.DownloadHashAlgorithm value) -> System.Void | instance public
M FMCore.Downloader.DownloadRequest.set_LocalOnly(System.Boolean value) -> System.Void | instance public
M FMCore.Downloader.DownloadRequest.set_RelativePath(System.String value) -> System.Void | instance public
M FMCore.Downloader.DownloadRequest.set_Source(System.Uri value) -> System.Void | instance public
M FMCore.Downloader.DownloadResult..ctor(System.String FullPath, System.Int64 Size, System.Int32 Attempts, System.Boolean WasSkipped) -> constructor | instance public
M FMCore.Downloader.DownloadResult.<Clone>$() -> FMCore.Downloader.DownloadResult | instance public
M FMCore.Downloader.DownloadResult.Deconstruct(System.String& FullPath, System.Int64& Size, System.Int32& Attempts, System.Boolean& WasSkipped) -> System.Void | instance public
M FMCore.Downloader.DownloadResult.Equals(FMCore.Downloader.DownloadResult other) -> System.Boolean | instance public
M FMCore.Downloader.DownloadResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Downloader.DownloadResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Downloader.DownloadResult.ToString() -> System.String | instance public
M FMCore.Downloader.DownloadResult.get_Attempts() -> System.Int32 | instance public
M FMCore.Downloader.DownloadResult.get_FullPath() -> System.String | instance public
M FMCore.Downloader.DownloadResult.get_Size() -> System.Int64 | instance public
M FMCore.Downloader.DownloadResult.get_WasSkipped() -> System.Boolean | instance public
M FMCore.Downloader.DownloadResult.op_Equality(FMCore.Downloader.DownloadResult left, FMCore.Downloader.DownloadResult right) -> System.Boolean | static public
M FMCore.Downloader.DownloadResult.op_Inequality(FMCore.Downloader.DownloadResult left, FMCore.Downloader.DownloadResult right) -> System.Boolean | static public
M FMCore.Downloader.DownloadResult.set_Attempts(System.Int32 value) -> System.Void | instance public
M FMCore.Downloader.DownloadResult.set_FullPath(System.String value) -> System.Void | instance public
M FMCore.Downloader.DownloadResult.set_Size(System.Int64 value) -> System.Void | instance public
M FMCore.Downloader.DownloadResult.set_WasSkipped(System.Boolean value) -> System.Void | instance public
M FMCore.Downloader.FileVerificationResult..ctor(System.String RelativePath, FMCore.Downloader.FileVerificationState State) -> constructor | instance public
M FMCore.Downloader.FileVerificationResult.<Clone>$() -> FMCore.Downloader.FileVerificationResult | instance public
M FMCore.Downloader.FileVerificationResult.Deconstruct(System.String& RelativePath, FMCore.Downloader.FileVerificationState& State) -> System.Void | instance public
M FMCore.Downloader.FileVerificationResult.Equals(FMCore.Downloader.FileVerificationResult other) -> System.Boolean | instance public
M FMCore.Downloader.FileVerificationResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Downloader.FileVerificationResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Downloader.FileVerificationResult.ToString() -> System.String | instance public
M FMCore.Downloader.FileVerificationResult.get_IsUsable() -> System.Boolean | instance public
M FMCore.Downloader.FileVerificationResult.get_RelativePath() -> System.String | instance public
M FMCore.Downloader.FileVerificationResult.get_State() -> FMCore.Downloader.FileVerificationState | instance public
M FMCore.Downloader.FileVerificationResult.op_Equality(FMCore.Downloader.FileVerificationResult left, FMCore.Downloader.FileVerificationResult right) -> System.Boolean | static public
M FMCore.Downloader.FileVerificationResult.op_Inequality(FMCore.Downloader.FileVerificationResult left, FMCore.Downloader.FileVerificationResult right) -> System.Boolean | static public
M FMCore.Downloader.FileVerificationResult.set_RelativePath(System.String value) -> System.Void | instance public
M FMCore.Downloader.FileVerificationResult.set_State(FMCore.Downloader.FileVerificationState value) -> System.Void | instance public
P FMCore.Downloader.DownloadException.Attempt : System.Int32 | get_Attempt
P FMCore.Downloader.DownloadException.Code : FMCore.Downloader.DownloadErrorCode | get_Code
P FMCore.Downloader.DownloadException.RelativePath : System.String | get_RelativePath
P FMCore.Downloader.DownloadException.StatusCode : System.Nullable`1[System.Net.HttpStatusCode] | get_StatusCode
P FMCore.Downloader.DownloadManager.RootDirectory : System.String | get_RootDirectory
P FMCore.Downloader.DownloadOptions.AttemptTimeout : System.TimeSpan | get_AttemptTimeout,set_AttemptTimeout[init]
P FMCore.Downloader.DownloadOptions.MaxConcurrency : System.Int32 | get_MaxConcurrency,set_MaxConcurrency[init]
P FMCore.Downloader.DownloadOptions.MaxRetries : System.Int32 | get_MaxRetries,set_MaxRetries[init]
P FMCore.Downloader.DownloadOptions.MaxRetryDelay : System.TimeSpan | get_MaxRetryDelay,set_MaxRetryDelay[init]
P FMCore.Downloader.DownloadOptions.RetryDelay : System.TimeSpan | get_RetryDelay,set_RetryDelay[init]
P FMCore.Downloader.DownloadProgress.Attempt : System.Int32 | get_Attempt,set_Attempt[init]
P FMCore.Downloader.DownloadProgress.BytesReceived : System.Int64 | get_BytesReceived,set_BytesReceived[init]
P FMCore.Downloader.DownloadProgress.RelativePath : System.String | get_RelativePath,set_RelativePath[init]
P FMCore.Downloader.DownloadProgress.State : FMCore.Downloader.DownloadState | get_State,set_State[init]
P FMCore.Downloader.DownloadProgress.TotalBytes : System.Nullable`1[System.Int64] | get_TotalBytes,set_TotalBytes[init]
P FMCore.Downloader.DownloadRequest.ExpectedHash : System.String | get_ExpectedHash,set_ExpectedHash[init]
P FMCore.Downloader.DownloadRequest.ExpectedSize : System.Nullable`1[System.Int64] | get_ExpectedSize,set_ExpectedSize[init]
P FMCore.Downloader.DownloadRequest.HashAlgorithm : FMCore.Downloader.DownloadHashAlgorithm | get_HashAlgorithm,set_HashAlgorithm[init]
P FMCore.Downloader.DownloadRequest.LocalOnly : System.Boolean | get_LocalOnly,set_LocalOnly[init]
P FMCore.Downloader.DownloadRequest.RelativePath : System.String | get_RelativePath,set_RelativePath[init]
P FMCore.Downloader.DownloadRequest.Source : System.Uri | get_Source,set_Source[init]
P FMCore.Downloader.DownloadResult.Attempts : System.Int32 | get_Attempts,set_Attempts[init]
P FMCore.Downloader.DownloadResult.FullPath : System.String | get_FullPath,set_FullPath[init]
P FMCore.Downloader.DownloadResult.Size : System.Int64 | get_Size,set_Size[init]
P FMCore.Downloader.DownloadResult.WasSkipped : System.Boolean | get_WasSkipped,set_WasSkipped[init]
P FMCore.Downloader.FileVerificationResult.IsUsable : System.Boolean | get_IsUsable
P FMCore.Downloader.FileVerificationResult.RelativePath : System.String | get_RelativePath,set_RelativePath[init]
P FMCore.Downloader.FileVerificationResult.State : FMCore.Downloader.FileVerificationState | get_State,set_State[init]
T FMCore.Downloader.DownloadErrorCode : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Downloader.DownloadException : System.Exception | System.Runtime.Serialization.ISerializable
T FMCore.Downloader.DownloadHashAlgorithm : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Downloader.DownloadManager : System.Object |
T FMCore.Downloader.DownloadOptions : System.Object | System.IEquatable`1[FMCore.Downloader.DownloadOptions]
T FMCore.Downloader.DownloadProgress : System.Object | System.IEquatable`1[FMCore.Downloader.DownloadProgress]
T FMCore.Downloader.DownloadRequest : System.Object | System.IEquatable`1[FMCore.Downloader.DownloadRequest]
T FMCore.Downloader.DownloadResult : System.Object | System.IEquatable`1[FMCore.Downloader.DownloadResult]
T FMCore.Downloader.DownloadState : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Downloader.FileVerificationResult : System.Object | System.IEquatable`1[FMCore.Downloader.FileVerificationResult]
T FMCore.Downloader.FileVerificationState : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
```

## FmCore

```text
M FMCore.FmCore..ctor() -> constructor | instance public
```

## Installer

```text
F FMCore.Installer.InstallationErrorCode.ArchiveLimitExceeded : FMCore.Installer.InstallationErrorCode = 7
F FMCore.Installer.InstallationErrorCode.ConflictingFiles : FMCore.Installer.InstallationErrorCode = 5
F FMCore.Installer.InstallationErrorCode.InstallationBusy : FMCore.Installer.InstallationErrorCode = 10
F FMCore.Installer.InstallationErrorCode.InvalidArchive : FMCore.Installer.InstallationErrorCode = 6
F FMCore.Installer.InstallationErrorCode.InvalidMetadata : FMCore.Installer.InstallationErrorCode = 0
F FMCore.Installer.InstallationErrorCode.IoError : FMCore.Installer.InstallationErrorCode = 9
F FMCore.Installer.InstallationErrorCode.MissingAssetIndex : FMCore.Installer.InstallationErrorCode = 2
F FMCore.Installer.InstallationErrorCode.MissingClient : FMCore.Installer.InstallationErrorCode = 1
F FMCore.Installer.InstallationErrorCode.MissingNativeClassifier : FMCore.Installer.InstallationErrorCode = 3
F FMCore.Installer.InstallationErrorCode.UnsafePath : FMCore.Installer.InstallationErrorCode = 8
F FMCore.Installer.InstallationErrorCode.UnsupportedPlatform : FMCore.Installer.InstallationErrorCode = 4
F FMCore.Installer.InstallationStage.AssetIndex : FMCore.Installer.InstallationStage = 3
F FMCore.Installer.InstallationStage.AssetLayout : FMCore.Installer.InstallationStage = 5
F FMCore.Installer.InstallationStage.AssetObjects : FMCore.Installer.InstallationStage = 4
F FMCore.Installer.InstallationStage.Client : FMCore.Installer.InstallationStage = 1
F FMCore.Installer.InstallationStage.Completed : FMCore.Installer.InstallationStage = 8
F FMCore.Installer.InstallationStage.Libraries : FMCore.Installer.InstallationStage = 2
F FMCore.Installer.InstallationStage.Logging : FMCore.Installer.InstallationStage = 7
F FMCore.Installer.InstallationStage.Metadata : FMCore.Installer.InstallationStage = 0
F FMCore.Installer.InstallationStage.NativeVerification : FMCore.Installer.InstallationStage = 10
F FMCore.Installer.InstallationStage.Natives : FMCore.Installer.InstallationStage = 6
F FMCore.Installer.InstallationStage.Verification : FMCore.Installer.InstallationStage = 9
M FMCore.Installer.GameInstaller..ctor(FMCore.Downloader.DownloadManager downloads, FMCore.Versions.VersionManager versions = null) -> constructor | instance public
M FMCore.Installer.GameInstaller..ctor(System.String rootDirectory, FMCore.Downloader.DownloadOptions options = null) -> constructor | instance public
M FMCore.Installer.GameInstaller.CreatePlan(FMCore.Versions.ResolvedVersion version, FMCore.Installer.InstallationOptions options = null) -> FMCore.Installer.InstallationPlan | instance public
M FMCore.Installer.GameInstaller.InstallAsync(System.String versionId, FMCore.Installer.InstallationOptions options = null, System.IProgress`1[FMCore.Installer.InstallationProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Installer.InstallationResult] | instance public
M FMCore.Installer.GameInstaller.RepairAsync(System.String versionId, FMCore.Installer.InstallationOptions options = null, System.IProgress`1[FMCore.Installer.InstallationProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Installer.InstallationResult] | instance public
M FMCore.Installer.GameInstaller.VerifyAsync(System.String versionId, FMCore.Installer.InstallationOptions options = null, System.IProgress`1[FMCore.Installer.InstallationProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Installer.InstallationCheckResult] | instance public
M FMCore.Installer.GameInstaller.get_RootDirectory() -> System.String | instance public
M FMCore.Installer.InstallationCheckResult..ctor(FMCore.Versions.ResolvedVersion Version, System.Collections.Generic.IReadOnlyList`1[FMCore.Downloader.FileVerificationResult] Files, System.Boolean AssetsChecked) -> constructor | instance public
M FMCore.Installer.InstallationCheckResult.<Clone>$() -> FMCore.Installer.InstallationCheckResult | instance public
M FMCore.Installer.InstallationCheckResult.Deconstruct(FMCore.Versions.ResolvedVersion& Version, System.Collections.Generic.IReadOnlyList`1[FMCore.Downloader.FileVerificationResult]& Files, System.Boolean& AssetsChecked) -> System.Void | instance public
M FMCore.Installer.InstallationCheckResult.Equals(FMCore.Installer.InstallationCheckResult other) -> System.Boolean | instance public
M FMCore.Installer.InstallationCheckResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Installer.InstallationCheckResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Installer.InstallationCheckResult.ToString() -> System.String | instance public
M FMCore.Installer.InstallationCheckResult.get_AssetsChecked() -> System.Boolean | instance public
M FMCore.Installer.InstallationCheckResult.get_Files() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Downloader.FileVerificationResult] | instance public
M FMCore.Installer.InstallationCheckResult.get_IsValid() -> System.Boolean | instance public
M FMCore.Installer.InstallationCheckResult.get_NativeFilesChecked() -> System.Boolean | instance public
M FMCore.Installer.InstallationCheckResult.get_Problems() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Downloader.FileVerificationResult] | instance public
M FMCore.Installer.InstallationCheckResult.get_Version() -> FMCore.Versions.ResolvedVersion | instance public
M FMCore.Installer.InstallationCheckResult.op_Equality(FMCore.Installer.InstallationCheckResult left, FMCore.Installer.InstallationCheckResult right) -> System.Boolean | static public
M FMCore.Installer.InstallationCheckResult.op_Inequality(FMCore.Installer.InstallationCheckResult left, FMCore.Installer.InstallationCheckResult right) -> System.Boolean | static public
M FMCore.Installer.InstallationCheckResult.set_AssetsChecked(System.Boolean value) -> System.Void | instance public
M FMCore.Installer.InstallationCheckResult.set_Files(System.Collections.Generic.IReadOnlyList`1[FMCore.Downloader.FileVerificationResult] value) -> System.Void | instance public
M FMCore.Installer.InstallationCheckResult.set_NativeFilesChecked(System.Boolean value) -> System.Void | instance public
M FMCore.Installer.InstallationCheckResult.set_Version(FMCore.Versions.ResolvedVersion value) -> System.Void | instance public
M FMCore.Installer.InstallationException..ctor(FMCore.Installer.InstallationErrorCode code, System.String message) -> constructor | instance public
M FMCore.Installer.InstallationException.get_Code() -> FMCore.Installer.InstallationErrorCode | instance public
M FMCore.Installer.InstallationOptions..ctor() -> constructor | instance public
M FMCore.Installer.InstallationOptions.<Clone>$() -> FMCore.Installer.InstallationOptions | instance public
M FMCore.Installer.InstallationOptions.Equals(FMCore.Installer.InstallationOptions other) -> System.Boolean | instance public
M FMCore.Installer.InstallationOptions.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Installer.InstallationOptions.GetHashCode() -> System.Int32 | instance public
M FMCore.Installer.InstallationOptions.ToString() -> System.String | instance public
M FMCore.Installer.InstallationOptions.get_CachePolicy() -> FMCore.Versions.VersionCachePolicy | instance public
M FMCore.Installer.InstallationOptions.get_Context() -> FMCore.Versions.RuleContext | instance public
M FMCore.Installer.InstallationOptions.get_GameDirectory() -> System.String | instance public
M FMCore.Installer.InstallationOptions.get_NativeExtraction() -> FMCore.Installer.NativeExtractionOptions | instance public
M FMCore.Installer.InstallationOptions.get_VerifyNativeFiles() -> System.Boolean | instance public
M FMCore.Installer.InstallationOptions.op_Equality(FMCore.Installer.InstallationOptions left, FMCore.Installer.InstallationOptions right) -> System.Boolean | static public
M FMCore.Installer.InstallationOptions.op_Inequality(FMCore.Installer.InstallationOptions left, FMCore.Installer.InstallationOptions right) -> System.Boolean | static public
M FMCore.Installer.InstallationOptions.set_CachePolicy(FMCore.Versions.VersionCachePolicy value) -> System.Void | instance public
M FMCore.Installer.InstallationOptions.set_Context(FMCore.Versions.RuleContext value) -> System.Void | instance public
M FMCore.Installer.InstallationOptions.set_GameDirectory(System.String value) -> System.Void | instance public
M FMCore.Installer.InstallationOptions.set_NativeExtraction(FMCore.Installer.NativeExtractionOptions value) -> System.Void | instance public
M FMCore.Installer.InstallationOptions.set_VerifyNativeFiles(System.Boolean value) -> System.Void | instance public
M FMCore.Installer.InstallationPlan..ctor(FMCore.Versions.ResolvedVersion Version, FMCore.Versions.RuleContext Context, System.String GameDirectory, System.String NativeDirectory, FMCore.Downloader.DownloadRequest Client, System.Collections.Generic.IReadOnlyList`1[FMCore.Downloader.DownloadRequest] Libraries, System.Collections.Generic.IReadOnlyList`1[System.String] ClasspathEntries, System.Collections.Generic.IReadOnlyList`1[FMCore.Installer.NativeArchive] NativeArchives, FMCore.Versions.AssetIndexInfo AssetIndex, FMCore.Downloader.DownloadRequest Logging) -> constructor | instance public
M FMCore.Installer.InstallationPlan.<Clone>$() -> FMCore.Installer.InstallationPlan | instance public
M FMCore.Installer.InstallationPlan.Deconstruct(FMCore.Versions.ResolvedVersion& Version, FMCore.Versions.RuleContext& Context, System.String& GameDirectory, System.String& NativeDirectory, FMCore.Downloader.DownloadRequest& Client, System.Collections.Generic.IReadOnlyList`1[FMCore.Downloader.DownloadRequest]& Libraries, System.Collections.Generic.IReadOnlyList`1[System.String]& ClasspathEntries, System.Collections.Generic.IReadOnlyList`1[FMCore.Installer.NativeArchive]& NativeArchives, FMCore.Versions.AssetIndexInfo& AssetIndex, FMCore.Downloader.DownloadRequest& Logging) -> System.Void | instance public
M FMCore.Installer.InstallationPlan.Equals(FMCore.Installer.InstallationPlan other) -> System.Boolean | instance public
M FMCore.Installer.InstallationPlan.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Installer.InstallationPlan.GetHashCode() -> System.Int32 | instance public
M FMCore.Installer.InstallationPlan.ToString() -> System.String | instance public
M FMCore.Installer.InstallationPlan.get_AssetIndex() -> FMCore.Versions.AssetIndexInfo | instance public
M FMCore.Installer.InstallationPlan.get_ClasspathEntries() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Installer.InstallationPlan.get_Client() -> FMCore.Downloader.DownloadRequest | instance public
M FMCore.Installer.InstallationPlan.get_Context() -> FMCore.Versions.RuleContext | instance public
M FMCore.Installer.InstallationPlan.get_GameDirectory() -> System.String | instance public
M FMCore.Installer.InstallationPlan.get_Libraries() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Downloader.DownloadRequest] | instance public
M FMCore.Installer.InstallationPlan.get_Logging() -> FMCore.Downloader.DownloadRequest | instance public
M FMCore.Installer.InstallationPlan.get_NativeArchives() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Installer.NativeArchive] | instance public
M FMCore.Installer.InstallationPlan.get_NativeDirectory() -> System.String | instance public
M FMCore.Installer.InstallationPlan.get_Version() -> FMCore.Versions.ResolvedVersion | instance public
M FMCore.Installer.InstallationPlan.op_Equality(FMCore.Installer.InstallationPlan left, FMCore.Installer.InstallationPlan right) -> System.Boolean | static public
M FMCore.Installer.InstallationPlan.op_Inequality(FMCore.Installer.InstallationPlan left, FMCore.Installer.InstallationPlan right) -> System.Boolean | static public
M FMCore.Installer.InstallationPlan.set_AssetIndex(FMCore.Versions.AssetIndexInfo value) -> System.Void | instance public
M FMCore.Installer.InstallationPlan.set_ClasspathEntries(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Installer.InstallationPlan.set_Client(FMCore.Downloader.DownloadRequest value) -> System.Void | instance public
M FMCore.Installer.InstallationPlan.set_Context(FMCore.Versions.RuleContext value) -> System.Void | instance public
M FMCore.Installer.InstallationPlan.set_GameDirectory(System.String value) -> System.Void | instance public
M FMCore.Installer.InstallationPlan.set_Libraries(System.Collections.Generic.IReadOnlyList`1[FMCore.Downloader.DownloadRequest] value) -> System.Void | instance public
M FMCore.Installer.InstallationPlan.set_Logging(FMCore.Downloader.DownloadRequest value) -> System.Void | instance public
M FMCore.Installer.InstallationPlan.set_NativeArchives(System.Collections.Generic.IReadOnlyList`1[FMCore.Installer.NativeArchive] value) -> System.Void | instance public
M FMCore.Installer.InstallationPlan.set_NativeDirectory(System.String value) -> System.Void | instance public
M FMCore.Installer.InstallationPlan.set_Version(FMCore.Versions.ResolvedVersion value) -> System.Void | instance public
M FMCore.Installer.InstallationProgress..ctor(FMCore.Installer.InstallationStage Stage, System.Int32 CompletedFiles, System.Int32 TotalFiles, FMCore.Downloader.DownloadProgress Download = null) -> constructor | instance public
M FMCore.Installer.InstallationProgress.<Clone>$() -> FMCore.Installer.InstallationProgress | instance public
M FMCore.Installer.InstallationProgress.Deconstruct(FMCore.Installer.InstallationStage& Stage, System.Int32& CompletedFiles, System.Int32& TotalFiles, FMCore.Downloader.DownloadProgress& Download) -> System.Void | instance public
M FMCore.Installer.InstallationProgress.Equals(FMCore.Installer.InstallationProgress other) -> System.Boolean | instance public
M FMCore.Installer.InstallationProgress.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Installer.InstallationProgress.GetHashCode() -> System.Int32 | instance public
M FMCore.Installer.InstallationProgress.ToString() -> System.String | instance public
M FMCore.Installer.InstallationProgress.get_CompletedFiles() -> System.Int32 | instance public
M FMCore.Installer.InstallationProgress.get_Download() -> FMCore.Downloader.DownloadProgress | instance public
M FMCore.Installer.InstallationProgress.get_Stage() -> FMCore.Installer.InstallationStage | instance public
M FMCore.Installer.InstallationProgress.get_TotalFiles() -> System.Int32 | instance public
M FMCore.Installer.InstallationProgress.op_Equality(FMCore.Installer.InstallationProgress left, FMCore.Installer.InstallationProgress right) -> System.Boolean | static public
M FMCore.Installer.InstallationProgress.op_Inequality(FMCore.Installer.InstallationProgress left, FMCore.Installer.InstallationProgress right) -> System.Boolean | static public
M FMCore.Installer.InstallationProgress.set_CompletedFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Installer.InstallationProgress.set_Download(FMCore.Downloader.DownloadProgress value) -> System.Void | instance public
M FMCore.Installer.InstallationProgress.set_Stage(FMCore.Installer.InstallationStage value) -> System.Void | instance public
M FMCore.Installer.InstallationProgress.set_TotalFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Installer.InstallationResult..ctor(FMCore.Installer.InstallationPlan Plan, System.String ClientJarPath, System.Collections.Generic.IReadOnlyList`1[System.String] ClasspathEntries, System.String NativeDirectory, FMCore.Assets.AssetLayoutResult Assets, System.String LoggingPath, System.Int32 DownloadedFiles, System.Int32 SkippedFiles, System.Int64 DownloadedBytes) -> constructor | instance public
M FMCore.Installer.InstallationResult.<Clone>$() -> FMCore.Installer.InstallationResult | instance public
M FMCore.Installer.InstallationResult.Deconstruct(FMCore.Installer.InstallationPlan& Plan, System.String& ClientJarPath, System.Collections.Generic.IReadOnlyList`1[System.String]& ClasspathEntries, System.String& NativeDirectory, FMCore.Assets.AssetLayoutResult& Assets, System.String& LoggingPath, System.Int32& DownloadedFiles, System.Int32& SkippedFiles, System.Int64& DownloadedBytes) -> System.Void | instance public
M FMCore.Installer.InstallationResult.Equals(FMCore.Installer.InstallationResult other) -> System.Boolean | instance public
M FMCore.Installer.InstallationResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Installer.InstallationResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Installer.InstallationResult.ToString() -> System.String | instance public
M FMCore.Installer.InstallationResult.get_Assets() -> FMCore.Assets.AssetLayoutResult | instance public
M FMCore.Installer.InstallationResult.get_ClasspathEntries() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Installer.InstallationResult.get_ClientJarPath() -> System.String | instance public
M FMCore.Installer.InstallationResult.get_DownloadedBytes() -> System.Int64 | instance public
M FMCore.Installer.InstallationResult.get_DownloadedFiles() -> System.Int32 | instance public
M FMCore.Installer.InstallationResult.get_LoggingPath() -> System.String | instance public
M FMCore.Installer.InstallationResult.get_NativeDirectory() -> System.String | instance public
M FMCore.Installer.InstallationResult.get_Plan() -> FMCore.Installer.InstallationPlan | instance public
M FMCore.Installer.InstallationResult.get_SkippedFiles() -> System.Int32 | instance public
M FMCore.Installer.InstallationResult.op_Equality(FMCore.Installer.InstallationResult left, FMCore.Installer.InstallationResult right) -> System.Boolean | static public
M FMCore.Installer.InstallationResult.op_Inequality(FMCore.Installer.InstallationResult left, FMCore.Installer.InstallationResult right) -> System.Boolean | static public
M FMCore.Installer.InstallationResult.set_Assets(FMCore.Assets.AssetLayoutResult value) -> System.Void | instance public
M FMCore.Installer.InstallationResult.set_ClasspathEntries(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Installer.InstallationResult.set_ClientJarPath(System.String value) -> System.Void | instance public
M FMCore.Installer.InstallationResult.set_DownloadedBytes(System.Int64 value) -> System.Void | instance public
M FMCore.Installer.InstallationResult.set_DownloadedFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Installer.InstallationResult.set_LoggingPath(System.String value) -> System.Void | instance public
M FMCore.Installer.InstallationResult.set_NativeDirectory(System.String value) -> System.Void | instance public
M FMCore.Installer.InstallationResult.set_Plan(FMCore.Installer.InstallationPlan value) -> System.Void | instance public
M FMCore.Installer.InstallationResult.set_SkippedFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Installer.NativeArchive..ctor(System.String RelativePath, System.String Sha1, System.Nullable`1[System.Int64] Size, System.Collections.Generic.IReadOnlyList`1[System.String] Excludes) -> constructor | instance public
M FMCore.Installer.NativeArchive.<Clone>$() -> FMCore.Installer.NativeArchive | instance public
M FMCore.Installer.NativeArchive.Deconstruct(System.String& RelativePath, System.String& Sha1, System.Nullable`1[System.Int64]& Size, System.Collections.Generic.IReadOnlyList`1[System.String]& Excludes) -> System.Void | instance public
M FMCore.Installer.NativeArchive.Equals(FMCore.Installer.NativeArchive other) -> System.Boolean | instance public
M FMCore.Installer.NativeArchive.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Installer.NativeArchive.GetHashCode() -> System.Int32 | instance public
M FMCore.Installer.NativeArchive.ToString() -> System.String | instance public
M FMCore.Installer.NativeArchive.get_Excludes() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Installer.NativeArchive.get_RelativePath() -> System.String | instance public
M FMCore.Installer.NativeArchive.get_Sha1() -> System.String | instance public
M FMCore.Installer.NativeArchive.get_Size() -> System.Nullable`1[System.Int64] | instance public
M FMCore.Installer.NativeArchive.op_Equality(FMCore.Installer.NativeArchive left, FMCore.Installer.NativeArchive right) -> System.Boolean | static public
M FMCore.Installer.NativeArchive.op_Inequality(FMCore.Installer.NativeArchive left, FMCore.Installer.NativeArchive right) -> System.Boolean | static public
M FMCore.Installer.NativeArchive.set_Excludes(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Installer.NativeArchive.set_RelativePath(System.String value) -> System.Void | instance public
M FMCore.Installer.NativeArchive.set_Sha1(System.String value) -> System.Void | instance public
M FMCore.Installer.NativeArchive.set_Size(System.Nullable`1[System.Int64] value) -> System.Void | instance public
M FMCore.Installer.NativeExtractionOptions..ctor() -> constructor | instance public
M FMCore.Installer.NativeExtractionOptions.<Clone>$() -> FMCore.Installer.NativeExtractionOptions | instance public
M FMCore.Installer.NativeExtractionOptions.Equals(FMCore.Installer.NativeExtractionOptions other) -> System.Boolean | instance public
M FMCore.Installer.NativeExtractionOptions.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Installer.NativeExtractionOptions.GetHashCode() -> System.Int32 | instance public
M FMCore.Installer.NativeExtractionOptions.ToString() -> System.String | instance public
M FMCore.Installer.NativeExtractionOptions.get_MaxEntries() -> System.Int32 | instance public
M FMCore.Installer.NativeExtractionOptions.get_MaxEntryBytes() -> System.Int64 | instance public
M FMCore.Installer.NativeExtractionOptions.get_MaxTotalBytes() -> System.Int64 | instance public
M FMCore.Installer.NativeExtractionOptions.op_Equality(FMCore.Installer.NativeExtractionOptions left, FMCore.Installer.NativeExtractionOptions right) -> System.Boolean | static public
M FMCore.Installer.NativeExtractionOptions.op_Inequality(FMCore.Installer.NativeExtractionOptions left, FMCore.Installer.NativeExtractionOptions right) -> System.Boolean | static public
M FMCore.Installer.NativeExtractionOptions.set_MaxEntries(System.Int32 value) -> System.Void | instance public
M FMCore.Installer.NativeExtractionOptions.set_MaxEntryBytes(System.Int64 value) -> System.Void | instance public
M FMCore.Installer.NativeExtractionOptions.set_MaxTotalBytes(System.Int64 value) -> System.Void | instance public
M FMCore.Installer.NativeExtractionProgress..ctor(System.Int32 CompletedArchives, System.Int32 TotalArchives) -> constructor | instance public
M FMCore.Installer.NativeExtractionProgress.<Clone>$() -> FMCore.Installer.NativeExtractionProgress | instance public
M FMCore.Installer.NativeExtractionProgress.Deconstruct(System.Int32& CompletedArchives, System.Int32& TotalArchives) -> System.Void | instance public
M FMCore.Installer.NativeExtractionProgress.Equals(FMCore.Installer.NativeExtractionProgress other) -> System.Boolean | instance public
M FMCore.Installer.NativeExtractionProgress.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Installer.NativeExtractionProgress.GetHashCode() -> System.Int32 | instance public
M FMCore.Installer.NativeExtractionProgress.ToString() -> System.String | instance public
M FMCore.Installer.NativeExtractionProgress.get_CompletedArchives() -> System.Int32 | instance public
M FMCore.Installer.NativeExtractionProgress.get_TotalArchives() -> System.Int32 | instance public
M FMCore.Installer.NativeExtractionProgress.op_Equality(FMCore.Installer.NativeExtractionProgress left, FMCore.Installer.NativeExtractionProgress right) -> System.Boolean | static public
M FMCore.Installer.NativeExtractionProgress.op_Inequality(FMCore.Installer.NativeExtractionProgress left, FMCore.Installer.NativeExtractionProgress right) -> System.Boolean | static public
M FMCore.Installer.NativeExtractionProgress.set_CompletedArchives(System.Int32 value) -> System.Void | instance public
M FMCore.Installer.NativeExtractionProgress.set_TotalArchives(System.Int32 value) -> System.Void | instance public
M FMCore.Installer.NativeExtractor..ctor(System.String rootDirectory, FMCore.Installer.NativeExtractionOptions options = null) -> constructor | instance public
M FMCore.Installer.NativeExtractor.ExtractAsync(System.Collections.Generic.IReadOnlyList`1[FMCore.Installer.NativeArchive] archives, System.String relativeDirectory, System.IProgress`1[FMCore.Installer.NativeExtractionProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.String] | instance public
M FMCore.Installer.NativeExtractor.VerifyAsync(System.Collections.Generic.IReadOnlyList`1[FMCore.Installer.NativeArchive] archives, System.String relativeDirectory, System.IProgress`1[FMCore.Installer.NativeExtractionProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Downloader.FileVerificationResult]] | instance public
P FMCore.Installer.GameInstaller.RootDirectory : System.String | get_RootDirectory
P FMCore.Installer.InstallationCheckResult.AssetsChecked : System.Boolean | get_AssetsChecked,set_AssetsChecked[init]
P FMCore.Installer.InstallationCheckResult.Files : System.Collections.Generic.IReadOnlyList`1[FMCore.Downloader.FileVerificationResult] | get_Files,set_Files[init]
P FMCore.Installer.InstallationCheckResult.IsValid : System.Boolean | get_IsValid
P FMCore.Installer.InstallationCheckResult.NativeFilesChecked : System.Boolean | get_NativeFilesChecked,set_NativeFilesChecked[init]
P FMCore.Installer.InstallationCheckResult.Problems : System.Collections.Generic.IReadOnlyList`1[FMCore.Downloader.FileVerificationResult] | get_Problems
P FMCore.Installer.InstallationCheckResult.Version : FMCore.Versions.ResolvedVersion | get_Version,set_Version[init]
P FMCore.Installer.InstallationException.Code : FMCore.Installer.InstallationErrorCode | get_Code
P FMCore.Installer.InstallationOptions.CachePolicy : FMCore.Versions.VersionCachePolicy | get_CachePolicy,set_CachePolicy[init]
P FMCore.Installer.InstallationOptions.Context : FMCore.Versions.RuleContext | get_Context,set_Context[init]
P FMCore.Installer.InstallationOptions.GameDirectory : System.String | get_GameDirectory,set_GameDirectory[init]
P FMCore.Installer.InstallationOptions.NativeExtraction : FMCore.Installer.NativeExtractionOptions | get_NativeExtraction,set_NativeExtraction[init]
P FMCore.Installer.InstallationOptions.VerifyNativeFiles : System.Boolean | get_VerifyNativeFiles,set_VerifyNativeFiles[init]
P FMCore.Installer.InstallationPlan.AssetIndex : FMCore.Versions.AssetIndexInfo | get_AssetIndex,set_AssetIndex[init]
P FMCore.Installer.InstallationPlan.ClasspathEntries : System.Collections.Generic.IReadOnlyList`1[System.String] | get_ClasspathEntries,set_ClasspathEntries[init]
P FMCore.Installer.InstallationPlan.Client : FMCore.Downloader.DownloadRequest | get_Client,set_Client[init]
P FMCore.Installer.InstallationPlan.Context : FMCore.Versions.RuleContext | get_Context,set_Context[init]
P FMCore.Installer.InstallationPlan.GameDirectory : System.String | get_GameDirectory,set_GameDirectory[init]
P FMCore.Installer.InstallationPlan.Libraries : System.Collections.Generic.IReadOnlyList`1[FMCore.Downloader.DownloadRequest] | get_Libraries,set_Libraries[init]
P FMCore.Installer.InstallationPlan.Logging : FMCore.Downloader.DownloadRequest | get_Logging,set_Logging[init]
P FMCore.Installer.InstallationPlan.NativeArchives : System.Collections.Generic.IReadOnlyList`1[FMCore.Installer.NativeArchive] | get_NativeArchives,set_NativeArchives[init]
P FMCore.Installer.InstallationPlan.NativeDirectory : System.String | get_NativeDirectory,set_NativeDirectory[init]
P FMCore.Installer.InstallationPlan.Version : FMCore.Versions.ResolvedVersion | get_Version,set_Version[init]
P FMCore.Installer.InstallationProgress.CompletedFiles : System.Int32 | get_CompletedFiles,set_CompletedFiles[init]
P FMCore.Installer.InstallationProgress.Download : FMCore.Downloader.DownloadProgress | get_Download,set_Download[init]
P FMCore.Installer.InstallationProgress.Stage : FMCore.Installer.InstallationStage | get_Stage,set_Stage[init]
P FMCore.Installer.InstallationProgress.TotalFiles : System.Int32 | get_TotalFiles,set_TotalFiles[init]
P FMCore.Installer.InstallationResult.Assets : FMCore.Assets.AssetLayoutResult | get_Assets,set_Assets[init]
P FMCore.Installer.InstallationResult.ClasspathEntries : System.Collections.Generic.IReadOnlyList`1[System.String] | get_ClasspathEntries,set_ClasspathEntries[init]
P FMCore.Installer.InstallationResult.ClientJarPath : System.String | get_ClientJarPath,set_ClientJarPath[init]
P FMCore.Installer.InstallationResult.DownloadedBytes : System.Int64 | get_DownloadedBytes,set_DownloadedBytes[init]
P FMCore.Installer.InstallationResult.DownloadedFiles : System.Int32 | get_DownloadedFiles,set_DownloadedFiles[init]
P FMCore.Installer.InstallationResult.LoggingPath : System.String | get_LoggingPath,set_LoggingPath[init]
P FMCore.Installer.InstallationResult.NativeDirectory : System.String | get_NativeDirectory,set_NativeDirectory[init]
P FMCore.Installer.InstallationResult.Plan : FMCore.Installer.InstallationPlan | get_Plan,set_Plan[init]
P FMCore.Installer.InstallationResult.SkippedFiles : System.Int32 | get_SkippedFiles,set_SkippedFiles[init]
P FMCore.Installer.NativeArchive.Excludes : System.Collections.Generic.IReadOnlyList`1[System.String] | get_Excludes,set_Excludes[init]
P FMCore.Installer.NativeArchive.RelativePath : System.String | get_RelativePath,set_RelativePath[init]
P FMCore.Installer.NativeArchive.Sha1 : System.String | get_Sha1,set_Sha1[init]
P FMCore.Installer.NativeArchive.Size : System.Nullable`1[System.Int64] | get_Size,set_Size[init]
P FMCore.Installer.NativeExtractionOptions.MaxEntries : System.Int32 | get_MaxEntries,set_MaxEntries[init]
P FMCore.Installer.NativeExtractionOptions.MaxEntryBytes : System.Int64 | get_MaxEntryBytes,set_MaxEntryBytes[init]
P FMCore.Installer.NativeExtractionOptions.MaxTotalBytes : System.Int64 | get_MaxTotalBytes,set_MaxTotalBytes[init]
P FMCore.Installer.NativeExtractionProgress.CompletedArchives : System.Int32 | get_CompletedArchives,set_CompletedArchives[init]
P FMCore.Installer.NativeExtractionProgress.TotalArchives : System.Int32 | get_TotalArchives,set_TotalArchives[init]
T FMCore.Installer.GameInstaller : System.Object |
T FMCore.Installer.InstallationCheckResult : System.Object | System.IEquatable`1[FMCore.Installer.InstallationCheckResult]
T FMCore.Installer.InstallationErrorCode : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Installer.InstallationException : System.Exception | System.Runtime.Serialization.ISerializable
T FMCore.Installer.InstallationOptions : System.Object | System.IEquatable`1[FMCore.Installer.InstallationOptions]
T FMCore.Installer.InstallationPlan : System.Object | System.IEquatable`1[FMCore.Installer.InstallationPlan]
T FMCore.Installer.InstallationProgress : System.Object | System.IEquatable`1[FMCore.Installer.InstallationProgress]
T FMCore.Installer.InstallationResult : System.Object | System.IEquatable`1[FMCore.Installer.InstallationResult]
T FMCore.Installer.InstallationStage : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Installer.NativeArchive : System.Object | System.IEquatable`1[FMCore.Installer.NativeArchive]
T FMCore.Installer.NativeExtractionOptions : System.Object | System.IEquatable`1[FMCore.Installer.NativeExtractionOptions]
T FMCore.Installer.NativeExtractionProgress : System.Object | System.IEquatable`1[FMCore.Installer.NativeExtractionProgress]
T FMCore.Installer.NativeExtractor : System.Object |
```

## Instances

```text
F FMCore.Instances.InstanceErrorCode.AlreadyExists : FMCore.Instances.InstanceErrorCode = 3
F FMCore.Instances.InstanceErrorCode.Busy : FMCore.Instances.InstanceErrorCode = 4
F FMCore.Instances.InstanceErrorCode.Conflict : FMCore.Instances.InstanceErrorCode = 5
F FMCore.Instances.InstanceErrorCode.InvalidConfig : FMCore.Instances.InstanceErrorCode = 0
F FMCore.Instances.InstanceErrorCode.IoError : FMCore.Instances.InstanceErrorCode = 7
F FMCore.Instances.InstanceErrorCode.NotFound : FMCore.Instances.InstanceErrorCode = 2
F FMCore.Instances.InstanceErrorCode.StorageCorrupted : FMCore.Instances.InstanceErrorCode = 6
F FMCore.Instances.InstanceErrorCode.UnsafePath : FMCore.Instances.InstanceErrorCode = 1
M FMCore.Instances.InstanceConfig..ctor() -> constructor | instance public
M FMCore.Instances.InstanceConfig.<Clone>$() -> FMCore.Instances.InstanceConfig | instance public
M FMCore.Instances.InstanceConfig.Equals(FMCore.Instances.InstanceConfig other) -> System.Boolean | instance public
M FMCore.Instances.InstanceConfig.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Instances.InstanceConfig.GetHashCode() -> System.Int32 | instance public
M FMCore.Instances.InstanceConfig.ToString() -> System.String | instance public
M FMCore.Instances.InstanceConfig.get_GameArguments() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Instances.InstanceConfig.get_JavaPath() -> System.String | instance public
M FMCore.Instances.InstanceConfig.get_JvmArguments() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Instances.InstanceConfig.get_MaxMemoryMb() -> System.Int32 | instance public
M FMCore.Instances.InstanceConfig.get_MinMemoryMb() -> System.Int32 | instance public
M FMCore.Instances.InstanceConfig.get_Name() -> System.String | instance public
M FMCore.Instances.InstanceConfig.get_Server() -> FMCore.Launch.GameServerConfig | instance public
M FMCore.Instances.InstanceConfig.get_VersionId() -> System.String | instance public
M FMCore.Instances.InstanceConfig.get_Window() -> FMCore.Launch.GameWindowConfig | instance public
M FMCore.Instances.InstanceConfig.op_Equality(FMCore.Instances.InstanceConfig left, FMCore.Instances.InstanceConfig right) -> System.Boolean | static public
M FMCore.Instances.InstanceConfig.op_Inequality(FMCore.Instances.InstanceConfig left, FMCore.Instances.InstanceConfig right) -> System.Boolean | static public
M FMCore.Instances.InstanceConfig.set_GameArguments(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Instances.InstanceConfig.set_JavaPath(System.String value) -> System.Void | instance public
M FMCore.Instances.InstanceConfig.set_JvmArguments(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Instances.InstanceConfig.set_MaxMemoryMb(System.Int32 value) -> System.Void | instance public
M FMCore.Instances.InstanceConfig.set_MinMemoryMb(System.Int32 value) -> System.Void | instance public
M FMCore.Instances.InstanceConfig.set_Name(System.String value) -> System.Void | instance public
M FMCore.Instances.InstanceConfig.set_Server(FMCore.Launch.GameServerConfig value) -> System.Void | instance public
M FMCore.Instances.InstanceConfig.set_VersionId(System.String value) -> System.Void | instance public
M FMCore.Instances.InstanceConfig.set_Window(FMCore.Launch.GameWindowConfig value) -> System.Void | instance public
M FMCore.Instances.InstanceDeletionProgress..ctor(System.Int32 DeletedFiles, System.Int32 TotalFiles) -> constructor | instance public
M FMCore.Instances.InstanceDeletionProgress.<Clone>$() -> FMCore.Instances.InstanceDeletionProgress | instance public
M FMCore.Instances.InstanceDeletionProgress.Deconstruct(System.Int32& DeletedFiles, System.Int32& TotalFiles) -> System.Void | instance public
M FMCore.Instances.InstanceDeletionProgress.Equals(FMCore.Instances.InstanceDeletionProgress other) -> System.Boolean | instance public
M FMCore.Instances.InstanceDeletionProgress.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Instances.InstanceDeletionProgress.GetHashCode() -> System.Int32 | instance public
M FMCore.Instances.InstanceDeletionProgress.ToString() -> System.String | instance public
M FMCore.Instances.InstanceDeletionProgress.get_DeletedFiles() -> System.Int32 | instance public
M FMCore.Instances.InstanceDeletionProgress.get_TotalFiles() -> System.Int32 | instance public
M FMCore.Instances.InstanceDeletionProgress.op_Equality(FMCore.Instances.InstanceDeletionProgress left, FMCore.Instances.InstanceDeletionProgress right) -> System.Boolean | static public
M FMCore.Instances.InstanceDeletionProgress.op_Inequality(FMCore.Instances.InstanceDeletionProgress left, FMCore.Instances.InstanceDeletionProgress right) -> System.Boolean | static public
M FMCore.Instances.InstanceDeletionProgress.set_DeletedFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Instances.InstanceDeletionProgress.set_TotalFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Instances.InstanceException..ctor(FMCore.Instances.InstanceErrorCode code, System.String message) -> constructor | instance public
M FMCore.Instances.InstanceException.get_Code() -> FMCore.Instances.InstanceErrorCode | instance public
M FMCore.Instances.InstanceInfo.CreateLaunchConfig(FMCore.Authentication.LaunchIdentity account = null) -> FMCore.Launch.LaunchConfig | instance public
M FMCore.Instances.InstanceInfo.get_Config() -> FMCore.Instances.InstanceConfig | instance public
M FMCore.Instances.InstanceInfo.get_CreatedAt() -> System.DateTimeOffset | instance public
M FMCore.Instances.InstanceInfo.get_GameDirectory() -> System.String | instance public
M FMCore.Instances.InstanceInfo.get_Id() -> System.String | instance public
M FMCore.Instances.InstanceInfo.get_IsInUse() -> System.Boolean | instance public
M FMCore.Instances.InstanceInfo.get_Revision() -> System.Guid | instance public
M FMCore.Instances.InstanceInfo.get_RootDirectory() -> System.String | instance public
M FMCore.Instances.InstanceInfo.get_UpdatedAt() -> System.DateTimeOffset | instance public
M FMCore.Instances.InstanceManager..ctor(System.String rootDirectory = ".minecraft") -> constructor | instance public
M FMCore.Instances.InstanceManager.CreateAsync(FMCore.Instances.InstanceConfig config, System.String instanceId = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Instances.InstanceInfo] | instance public
M FMCore.Instances.InstanceManager.CreateAsync(System.String name, System.String versionId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Instances.InstanceInfo] | instance public
M FMCore.Instances.InstanceManager.DeleteAsync(System.String instanceId, System.IProgress`1[FMCore.Instances.InstanceDeletionProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task | instance public
M FMCore.Instances.InstanceManager.GetAsync(System.String instanceId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Instances.InstanceInfo] | instance public
M FMCore.Instances.InstanceManager.GetInstancesAsync(System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Instances.InstanceInfo]] | instance public
M FMCore.Instances.InstanceManager.UpdateAsync(FMCore.Instances.InstanceInfo instance, FMCore.Instances.InstanceConfig config, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Instances.InstanceInfo] | instance public
M FMCore.Instances.InstanceManager.get_RootDirectory() -> System.String | instance public
P FMCore.Instances.InstanceConfig.GameArguments : System.Collections.Generic.IReadOnlyList`1[System.String] | get_GameArguments,set_GameArguments[init]
P FMCore.Instances.InstanceConfig.JavaPath : System.String | get_JavaPath,set_JavaPath[init]
P FMCore.Instances.InstanceConfig.JvmArguments : System.Collections.Generic.IReadOnlyList`1[System.String] | get_JvmArguments,set_JvmArguments[init]
P FMCore.Instances.InstanceConfig.MaxMemoryMb : System.Int32 | get_MaxMemoryMb,set_MaxMemoryMb[init]
P FMCore.Instances.InstanceConfig.MinMemoryMb : System.Int32 | get_MinMemoryMb,set_MinMemoryMb[init]
P FMCore.Instances.InstanceConfig.Name : System.String | get_Name,set_Name[init]
P FMCore.Instances.InstanceConfig.Server : FMCore.Launch.GameServerConfig | get_Server,set_Server[init]
P FMCore.Instances.InstanceConfig.VersionId : System.String | get_VersionId,set_VersionId[init]
P FMCore.Instances.InstanceConfig.Window : FMCore.Launch.GameWindowConfig | get_Window,set_Window[init]
P FMCore.Instances.InstanceDeletionProgress.DeletedFiles : System.Int32 | get_DeletedFiles,set_DeletedFiles[init]
P FMCore.Instances.InstanceDeletionProgress.TotalFiles : System.Int32 | get_TotalFiles,set_TotalFiles[init]
P FMCore.Instances.InstanceException.Code : FMCore.Instances.InstanceErrorCode | get_Code
P FMCore.Instances.InstanceInfo.Config : FMCore.Instances.InstanceConfig | get_Config
P FMCore.Instances.InstanceInfo.CreatedAt : System.DateTimeOffset | get_CreatedAt
P FMCore.Instances.InstanceInfo.GameDirectory : System.String | get_GameDirectory
P FMCore.Instances.InstanceInfo.Id : System.String | get_Id
P FMCore.Instances.InstanceInfo.IsInUse : System.Boolean | get_IsInUse
P FMCore.Instances.InstanceInfo.Revision : System.Guid | get_Revision
P FMCore.Instances.InstanceInfo.RootDirectory : System.String | get_RootDirectory
P FMCore.Instances.InstanceInfo.UpdatedAt : System.DateTimeOffset | get_UpdatedAt
P FMCore.Instances.InstanceManager.RootDirectory : System.String | get_RootDirectory
T FMCore.Instances.InstanceConfig : System.Object | System.IEquatable`1[FMCore.Instances.InstanceConfig]
T FMCore.Instances.InstanceDeletionProgress : System.Object | System.IEquatable`1[FMCore.Instances.InstanceDeletionProgress]
T FMCore.Instances.InstanceErrorCode : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Instances.InstanceException : System.Exception | System.Runtime.Serialization.ISerializable
T FMCore.Instances.InstanceInfo : System.Object |
T FMCore.Instances.InstanceManager : System.Object |
```

## Java

```text
F FMCore.Java.JavaArchitecture.Arm32 : FMCore.Java.JavaArchitecture = 3
F FMCore.Java.JavaArchitecture.Arm64 : FMCore.Java.JavaArchitecture = 4
F FMCore.Java.JavaArchitecture.Unknown : FMCore.Java.JavaArchitecture = 0
F FMCore.Java.JavaArchitecture.X64 : FMCore.Java.JavaArchitecture = 2
F FMCore.Java.JavaArchitecture.X86 : FMCore.Java.JavaArchitecture = 1
F FMCore.Java.JavaDistribution.Temurin : FMCore.Java.JavaDistribution = 0
F FMCore.Java.JavaDistribution.Zulu : FMCore.Java.JavaDistribution = 1
F FMCore.Java.JavaErrorCode.ArchitectureMismatch : FMCore.Java.JavaErrorCode = 8
F FMCore.Java.JavaErrorCode.EarlyAccessNotAllowed : FMCore.Java.JavaErrorCode = 9
F FMCore.Java.JavaErrorCode.ExecutionFailed : FMCore.Java.JavaErrorCode = 2
F FMCore.Java.JavaErrorCode.InvalidOutput : FMCore.Java.JavaErrorCode = 4
F FMCore.Java.JavaErrorCode.InvalidPath : FMCore.Java.JavaErrorCode = 0
F FMCore.Java.JavaErrorCode.IoError : FMCore.Java.JavaErrorCode = 11
F FMCore.Java.JavaErrorCode.MajorVersionMismatch : FMCore.Java.JavaErrorCode = 7
F FMCore.Java.JavaErrorCode.NoCompatibleRuntime : FMCore.Java.JavaErrorCode = 10
F FMCore.Java.JavaErrorCode.NotFound : FMCore.Java.JavaErrorCode = 1
F FMCore.Java.JavaErrorCode.OutputLimitExceeded : FMCore.Java.JavaErrorCode = 5
F FMCore.Java.JavaErrorCode.ProbeTimeout : FMCore.Java.JavaErrorCode = 3
F FMCore.Java.JavaErrorCode.RequirementMissing : FMCore.Java.JavaErrorCode = 6
F FMCore.Java.JavaErrorCode.RuntimeArchiveInvalid : FMCore.Java.JavaErrorCode = 20
F FMCore.Java.JavaErrorCode.RuntimeBusy : FMCore.Java.JavaErrorCode = 22
F FMCore.Java.JavaErrorCode.RuntimeConflict : FMCore.Java.JavaErrorCode = 23
F FMCore.Java.JavaErrorCode.RuntimeDownloadFailed : FMCore.Java.JavaErrorCode = 18
F FMCore.Java.JavaErrorCode.RuntimeIntegrityFailed : FMCore.Java.JavaErrorCode = 19
F FMCore.Java.JavaErrorCode.RuntimeInvalidOptions : FMCore.Java.JavaErrorCode = 13
F FMCore.Java.JavaErrorCode.RuntimeMetadataInvalid : FMCore.Java.JavaErrorCode = 16
F FMCore.Java.JavaErrorCode.RuntimeNotAvailable : FMCore.Java.JavaErrorCode = 17
F FMCore.Java.JavaErrorCode.RuntimeQueryFailed : FMCore.Java.JavaErrorCode = 14
F FMCore.Java.JavaErrorCode.RuntimeQueryTimeout : FMCore.Java.JavaErrorCode = 15
F FMCore.Java.JavaErrorCode.RuntimeRecoveryFailed : FMCore.Java.JavaErrorCode = 24
F FMCore.Java.JavaErrorCode.RuntimeUnsafePath : FMCore.Java.JavaErrorCode = 25
F FMCore.Java.JavaErrorCode.RuntimeValidationFailed : FMCore.Java.JavaErrorCode = 21
F FMCore.Java.JavaErrorCode.UnsupportedPlatform : FMCore.Java.JavaErrorCode = 12
F FMCore.Java.JavaPackageType.Jdk : FMCore.Java.JavaPackageType = 1
F FMCore.Java.JavaPackageType.Jre : FMCore.Java.JavaPackageType = 0
F FMCore.Java.JavaRuntimeStage.Committing : FMCore.Java.JavaRuntimeStage = 6
F FMCore.Java.JavaRuntimeStage.Completed : FMCore.Java.JavaRuntimeStage = 7
F FMCore.Java.JavaRuntimeStage.Downloading : FMCore.Java.JavaRuntimeStage = 3
F FMCore.Java.JavaRuntimeStage.Extracting : FMCore.Java.JavaRuntimeStage = 4
F FMCore.Java.JavaRuntimeStage.Querying : FMCore.Java.JavaRuntimeStage = 0
F FMCore.Java.JavaRuntimeStage.Recovering : FMCore.Java.JavaRuntimeStage = 1
F FMCore.Java.JavaRuntimeStage.Reused : FMCore.Java.JavaRuntimeStage = 8
F FMCore.Java.JavaRuntimeStage.Validating : FMCore.Java.JavaRuntimeStage = 5
F FMCore.Java.JavaRuntimeStage.Verifying : FMCore.Java.JavaRuntimeStage = 2
M FMCore.Java.JavaCompatibility.Check(FMCore.Java.JavaRuntime runtime, FMCore.Versions.VersionMetadata version, FMCore.Java.JavaSelectionOptions options = null) -> FMCore.Java.JavaCompatibilityResult | static public
M FMCore.Java.JavaCompatibilityResult..ctor(System.Boolean IsCompatible, System.Nullable`1[FMCore.Java.JavaErrorCode] ErrorCode, System.Nullable`1[System.Int32] RequiredMajorVersion, FMCore.Java.JavaArchitecture RequiredArchitecture) -> constructor | instance public
M FMCore.Java.JavaCompatibilityResult.<Clone>$() -> FMCore.Java.JavaCompatibilityResult | instance public
M FMCore.Java.JavaCompatibilityResult.Deconstruct(System.Boolean& IsCompatible, System.Nullable`1[FMCore.Java.JavaErrorCode]& ErrorCode, System.Nullable`1[System.Int32]& RequiredMajorVersion, FMCore.Java.JavaArchitecture& RequiredArchitecture) -> System.Void | instance public
M FMCore.Java.JavaCompatibilityResult.Equals(FMCore.Java.JavaCompatibilityResult other) -> System.Boolean | instance public
M FMCore.Java.JavaCompatibilityResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Java.JavaCompatibilityResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Java.JavaCompatibilityResult.ToString() -> System.String | instance public
M FMCore.Java.JavaCompatibilityResult.get_ErrorCode() -> System.Nullable`1[FMCore.Java.JavaErrorCode] | instance public
M FMCore.Java.JavaCompatibilityResult.get_IsCompatible() -> System.Boolean | instance public
M FMCore.Java.JavaCompatibilityResult.get_RequiredArchitecture() -> FMCore.Java.JavaArchitecture | instance public
M FMCore.Java.JavaCompatibilityResult.get_RequiredMajorVersion() -> System.Nullable`1[System.Int32] | instance public
M FMCore.Java.JavaCompatibilityResult.op_Equality(FMCore.Java.JavaCompatibilityResult left, FMCore.Java.JavaCompatibilityResult right) -> System.Boolean | static public
M FMCore.Java.JavaCompatibilityResult.op_Inequality(FMCore.Java.JavaCompatibilityResult left, FMCore.Java.JavaCompatibilityResult right) -> System.Boolean | static public
M FMCore.Java.JavaCompatibilityResult.set_ErrorCode(System.Nullable`1[FMCore.Java.JavaErrorCode] value) -> System.Void | instance public
M FMCore.Java.JavaCompatibilityResult.set_IsCompatible(System.Boolean value) -> System.Void | instance public
M FMCore.Java.JavaCompatibilityResult.set_RequiredArchitecture(FMCore.Java.JavaArchitecture value) -> System.Void | instance public
M FMCore.Java.JavaCompatibilityResult.set_RequiredMajorVersion(System.Nullable`1[System.Int32] value) -> System.Void | instance public
M FMCore.Java.JavaDetectionIssue..ctor(System.String Path, FMCore.Java.JavaErrorCode Code, System.Nullable`1[System.Int32] ExitCode = default) -> constructor | instance public
M FMCore.Java.JavaDetectionIssue.<Clone>$() -> FMCore.Java.JavaDetectionIssue | instance public
M FMCore.Java.JavaDetectionIssue.Deconstruct(System.String& Path, FMCore.Java.JavaErrorCode& Code, System.Nullable`1[System.Int32]& ExitCode) -> System.Void | instance public
M FMCore.Java.JavaDetectionIssue.Equals(FMCore.Java.JavaDetectionIssue other) -> System.Boolean | instance public
M FMCore.Java.JavaDetectionIssue.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Java.JavaDetectionIssue.GetHashCode() -> System.Int32 | instance public
M FMCore.Java.JavaDetectionIssue.ToString() -> System.String | instance public
M FMCore.Java.JavaDetectionIssue.get_Code() -> FMCore.Java.JavaErrorCode | instance public
M FMCore.Java.JavaDetectionIssue.get_ExitCode() -> System.Nullable`1[System.Int32] | instance public
M FMCore.Java.JavaDetectionIssue.get_Path() -> System.String | instance public
M FMCore.Java.JavaDetectionIssue.op_Equality(FMCore.Java.JavaDetectionIssue left, FMCore.Java.JavaDetectionIssue right) -> System.Boolean | static public
M FMCore.Java.JavaDetectionIssue.op_Inequality(FMCore.Java.JavaDetectionIssue left, FMCore.Java.JavaDetectionIssue right) -> System.Boolean | static public
M FMCore.Java.JavaDetectionIssue.set_Code(FMCore.Java.JavaErrorCode value) -> System.Void | instance public
M FMCore.Java.JavaDetectionIssue.set_ExitCode(System.Nullable`1[System.Int32] value) -> System.Void | instance public
M FMCore.Java.JavaDetectionIssue.set_Path(System.String value) -> System.Void | instance public
M FMCore.Java.JavaDetectionOptions..ctor() -> constructor | instance public
M FMCore.Java.JavaDetectionOptions.<Clone>$() -> FMCore.Java.JavaDetectionOptions | instance public
M FMCore.Java.JavaDetectionOptions.Equals(FMCore.Java.JavaDetectionOptions other) -> System.Boolean | instance public
M FMCore.Java.JavaDetectionOptions.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Java.JavaDetectionOptions.GetHashCode() -> System.Int32 | instance public
M FMCore.Java.JavaDetectionOptions.ToString() -> System.String | instance public
M FMCore.Java.JavaDetectionOptions.get_JavaHome() -> System.String | instance public
M FMCore.Java.JavaDetectionOptions.get_MaxCandidates() -> System.Int32 | instance public
M FMCore.Java.JavaDetectionOptions.get_MaxOutputCharacters() -> System.Int32 | instance public
M FMCore.Java.JavaDetectionOptions.get_PathDirectories() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Java.JavaDetectionOptions.get_ProbeTimeout() -> System.TimeSpan | instance public
M FMCore.Java.JavaDetectionOptions.get_SearchDirectories() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Java.JavaDetectionOptions.op_Equality(FMCore.Java.JavaDetectionOptions left, FMCore.Java.JavaDetectionOptions right) -> System.Boolean | static public
M FMCore.Java.JavaDetectionOptions.op_Inequality(FMCore.Java.JavaDetectionOptions left, FMCore.Java.JavaDetectionOptions right) -> System.Boolean | static public
M FMCore.Java.JavaDetectionOptions.set_JavaHome(System.String value) -> System.Void | instance public
M FMCore.Java.JavaDetectionOptions.set_MaxCandidates(System.Int32 value) -> System.Void | instance public
M FMCore.Java.JavaDetectionOptions.set_MaxOutputCharacters(System.Int32 value) -> System.Void | instance public
M FMCore.Java.JavaDetectionOptions.set_PathDirectories(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Java.JavaDetectionOptions.set_ProbeTimeout(System.TimeSpan value) -> System.Void | instance public
M FMCore.Java.JavaDetectionOptions.set_SearchDirectories(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Java.JavaDetectionProgress..ctor(System.Int32 Completed, System.Int32 Total, System.String ExecutablePath, System.Nullable`1[FMCore.Java.JavaErrorCode] ErrorCode) -> constructor | instance public
M FMCore.Java.JavaDetectionProgress.<Clone>$() -> FMCore.Java.JavaDetectionProgress | instance public
M FMCore.Java.JavaDetectionProgress.Deconstruct(System.Int32& Completed, System.Int32& Total, System.String& ExecutablePath, System.Nullable`1[FMCore.Java.JavaErrorCode]& ErrorCode) -> System.Void | instance public
M FMCore.Java.JavaDetectionProgress.Equals(FMCore.Java.JavaDetectionProgress other) -> System.Boolean | instance public
M FMCore.Java.JavaDetectionProgress.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Java.JavaDetectionProgress.GetHashCode() -> System.Int32 | instance public
M FMCore.Java.JavaDetectionProgress.ToString() -> System.String | instance public
M FMCore.Java.JavaDetectionProgress.get_Completed() -> System.Int32 | instance public
M FMCore.Java.JavaDetectionProgress.get_ErrorCode() -> System.Nullable`1[FMCore.Java.JavaErrorCode] | instance public
M FMCore.Java.JavaDetectionProgress.get_ExecutablePath() -> System.String | instance public
M FMCore.Java.JavaDetectionProgress.get_Total() -> System.Int32 | instance public
M FMCore.Java.JavaDetectionProgress.op_Equality(FMCore.Java.JavaDetectionProgress left, FMCore.Java.JavaDetectionProgress right) -> System.Boolean | static public
M FMCore.Java.JavaDetectionProgress.op_Inequality(FMCore.Java.JavaDetectionProgress left, FMCore.Java.JavaDetectionProgress right) -> System.Boolean | static public
M FMCore.Java.JavaDetectionProgress.set_Completed(System.Int32 value) -> System.Void | instance public
M FMCore.Java.JavaDetectionProgress.set_ErrorCode(System.Nullable`1[FMCore.Java.JavaErrorCode] value) -> System.Void | instance public
M FMCore.Java.JavaDetectionProgress.set_ExecutablePath(System.String value) -> System.Void | instance public
M FMCore.Java.JavaDetectionProgress.set_Total(System.Int32 value) -> System.Void | instance public
M FMCore.Java.JavaDetectionResult..ctor(System.Collections.Generic.IReadOnlyList`1[FMCore.Java.JavaRuntime] Runtimes, System.Collections.Generic.IReadOnlyList`1[FMCore.Java.JavaDetectionIssue] Issues, System.Boolean IsTruncated) -> constructor | instance public
M FMCore.Java.JavaDetectionResult.<Clone>$() -> FMCore.Java.JavaDetectionResult | instance public
M FMCore.Java.JavaDetectionResult.Deconstruct(System.Collections.Generic.IReadOnlyList`1[FMCore.Java.JavaRuntime]& Runtimes, System.Collections.Generic.IReadOnlyList`1[FMCore.Java.JavaDetectionIssue]& Issues, System.Boolean& IsTruncated) -> System.Void | instance public
M FMCore.Java.JavaDetectionResult.Equals(FMCore.Java.JavaDetectionResult other) -> System.Boolean | instance public
M FMCore.Java.JavaDetectionResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Java.JavaDetectionResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Java.JavaDetectionResult.ToString() -> System.String | instance public
M FMCore.Java.JavaDetectionResult.get_IsTruncated() -> System.Boolean | instance public
M FMCore.Java.JavaDetectionResult.get_Issues() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Java.JavaDetectionIssue] | instance public
M FMCore.Java.JavaDetectionResult.get_Runtimes() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Java.JavaRuntime] | instance public
M FMCore.Java.JavaDetectionResult.op_Equality(FMCore.Java.JavaDetectionResult left, FMCore.Java.JavaDetectionResult right) -> System.Boolean | static public
M FMCore.Java.JavaDetectionResult.op_Inequality(FMCore.Java.JavaDetectionResult left, FMCore.Java.JavaDetectionResult right) -> System.Boolean | static public
M FMCore.Java.JavaDetectionResult.set_IsTruncated(System.Boolean value) -> System.Void | instance public
M FMCore.Java.JavaDetectionResult.set_Issues(System.Collections.Generic.IReadOnlyList`1[FMCore.Java.JavaDetectionIssue] value) -> System.Void | instance public
M FMCore.Java.JavaDetectionResult.set_Runtimes(System.Collections.Generic.IReadOnlyList`1[FMCore.Java.JavaRuntime] value) -> System.Void | instance public
M FMCore.Java.JavaException..ctor(FMCore.Java.JavaErrorCode code, System.String message, System.Nullable`1[System.Int32] exitCode = default) -> constructor | instance public
M FMCore.Java.JavaException.get_Code() -> FMCore.Java.JavaErrorCode | instance public
M FMCore.Java.JavaException.get_ExitCode() -> System.Nullable`1[System.Int32] | instance public
M FMCore.Java.JavaManager..ctor(FMCore.Java.JavaDetectionOptions options = null) -> constructor | instance public
M FMCore.Java.JavaManager.DetectAsync(System.IProgress`1[FMCore.Java.JavaDetectionProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Java.JavaDetectionResult] | instance public
M FMCore.Java.JavaManager.ProbeAsync(System.String path, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Java.JavaRuntime] | instance public
M FMCore.Java.JavaManager.Select(System.Collections.Generic.IEnumerable`1[FMCore.Java.JavaRuntime] runtimes, FMCore.Versions.VersionMetadata version, FMCore.Java.JavaSelectionOptions options = null) -> FMCore.Java.JavaRuntime | instance public
M FMCore.Java.JavaManager.SelectAsync(FMCore.Versions.VersionMetadata version, System.String customPath = null, FMCore.Java.JavaSelectionOptions options = null, System.IProgress`1[FMCore.Java.JavaDetectionProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Java.JavaRuntime] | instance public
M FMCore.Java.JavaRuntime..ctor(System.String ExecutablePath, System.String HomeDirectory, FMCore.Java.JavaVersion Version, FMCore.Java.JavaArchitecture Architecture, System.String Vendor) -> constructor | instance public
M FMCore.Java.JavaRuntime.<Clone>$() -> FMCore.Java.JavaRuntime | instance public
M FMCore.Java.JavaRuntime.Deconstruct(System.String& ExecutablePath, System.String& HomeDirectory, FMCore.Java.JavaVersion& Version, FMCore.Java.JavaArchitecture& Architecture, System.String& Vendor) -> System.Void | instance public
M FMCore.Java.JavaRuntime.Equals(FMCore.Java.JavaRuntime other) -> System.Boolean | instance public
M FMCore.Java.JavaRuntime.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Java.JavaRuntime.GetHashCode() -> System.Int32 | instance public
M FMCore.Java.JavaRuntime.GetRuleContext(System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.Boolean] features = null) -> FMCore.Versions.RuleContext | instance public
M FMCore.Java.JavaRuntime.ToString() -> System.String | instance public
M FMCore.Java.JavaRuntime.get_Architecture() -> FMCore.Java.JavaArchitecture | instance public
M FMCore.Java.JavaRuntime.get_ExecutablePath() -> System.String | instance public
M FMCore.Java.JavaRuntime.get_HomeDirectory() -> System.String | instance public
M FMCore.Java.JavaRuntime.get_Vendor() -> System.String | instance public
M FMCore.Java.JavaRuntime.get_Version() -> FMCore.Java.JavaVersion | instance public
M FMCore.Java.JavaRuntime.op_Equality(FMCore.Java.JavaRuntime left, FMCore.Java.JavaRuntime right) -> System.Boolean | static public
M FMCore.Java.JavaRuntime.op_Inequality(FMCore.Java.JavaRuntime left, FMCore.Java.JavaRuntime right) -> System.Boolean | static public
M FMCore.Java.JavaRuntime.set_Architecture(FMCore.Java.JavaArchitecture value) -> System.Void | instance public
M FMCore.Java.JavaRuntime.set_ExecutablePath(System.String value) -> System.Void | instance public
M FMCore.Java.JavaRuntime.set_HomeDirectory(System.String value) -> System.Void | instance public
M FMCore.Java.JavaRuntime.set_Vendor(System.String value) -> System.Void | instance public
M FMCore.Java.JavaRuntime.set_Version(FMCore.Java.JavaVersion value) -> System.Void | instance public
M FMCore.Java.JavaRuntimeOptions..ctor() -> constructor | instance public
M FMCore.Java.JavaRuntimeOptions.<Clone>$() -> FMCore.Java.JavaRuntimeOptions | instance public
M FMCore.Java.JavaRuntimeOptions.Equals(FMCore.Java.JavaRuntimeOptions other) -> System.Boolean | instance public
M FMCore.Java.JavaRuntimeOptions.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Java.JavaRuntimeOptions.GetHashCode() -> System.Int32 | instance public
M FMCore.Java.JavaRuntimeOptions.ToString() -> System.String | instance public
M FMCore.Java.JavaRuntimeOptions.get_Architecture() -> FMCore.Java.JavaArchitecture | instance public
M FMCore.Java.JavaRuntimeOptions.get_Distribution() -> FMCore.Java.JavaDistribution | instance public
M FMCore.Java.JavaRuntimeOptions.get_Downloads() -> FMCore.Downloader.DownloadOptions | instance public
M FMCore.Java.JavaRuntimeOptions.get_FullVersion() -> System.String | instance public
M FMCore.Java.JavaRuntimeOptions.get_HttpClient() -> System.Net.Http.HttpClient | instance public
M FMCore.Java.JavaRuntimeOptions.get_InstallationDirectory() -> System.String | instance public
M FMCore.Java.JavaRuntimeOptions.get_PackageType() -> FMCore.Java.JavaPackageType | instance public
M FMCore.Java.JavaRuntimeOptions.get_ProbeTimeout() -> System.TimeSpan | instance public
M FMCore.Java.JavaRuntimeOptions.get_QueryTimeout() -> System.TimeSpan | instance public
M FMCore.Java.JavaRuntimeOptions.op_Equality(FMCore.Java.JavaRuntimeOptions left, FMCore.Java.JavaRuntimeOptions right) -> System.Boolean | static public
M FMCore.Java.JavaRuntimeOptions.op_Inequality(FMCore.Java.JavaRuntimeOptions left, FMCore.Java.JavaRuntimeOptions right) -> System.Boolean | static public
M FMCore.Java.JavaRuntimeOptions.set_Architecture(FMCore.Java.JavaArchitecture value) -> System.Void | instance public
M FMCore.Java.JavaRuntimeOptions.set_Distribution(FMCore.Java.JavaDistribution value) -> System.Void | instance public
M FMCore.Java.JavaRuntimeOptions.set_Downloads(FMCore.Downloader.DownloadOptions value) -> System.Void | instance public
M FMCore.Java.JavaRuntimeOptions.set_FullVersion(System.String value) -> System.Void | instance public
M FMCore.Java.JavaRuntimeOptions.set_HttpClient(System.Net.Http.HttpClient value) -> System.Void | instance public
M FMCore.Java.JavaRuntimeOptions.set_InstallationDirectory(System.String value) -> System.Void | instance public
M FMCore.Java.JavaRuntimeOptions.set_PackageType(FMCore.Java.JavaPackageType value) -> System.Void | instance public
M FMCore.Java.JavaRuntimeOptions.set_ProbeTimeout(System.TimeSpan value) -> System.Void | instance public
M FMCore.Java.JavaRuntimeOptions.set_QueryTimeout(System.TimeSpan value) -> System.Void | instance public
M FMCore.Java.JavaRuntimeProgress..ctor(FMCore.Java.JavaRuntimeStage Stage, System.Int32 CompletedFiles = 0, System.Int32 TotalFiles = 0, FMCore.Downloader.DownloadProgress Download = null) -> constructor | instance public
M FMCore.Java.JavaRuntimeProgress.<Clone>$() -> FMCore.Java.JavaRuntimeProgress | instance public
M FMCore.Java.JavaRuntimeProgress.Deconstruct(FMCore.Java.JavaRuntimeStage& Stage, System.Int32& CompletedFiles, System.Int32& TotalFiles, FMCore.Downloader.DownloadProgress& Download) -> System.Void | instance public
M FMCore.Java.JavaRuntimeProgress.Equals(FMCore.Java.JavaRuntimeProgress other) -> System.Boolean | instance public
M FMCore.Java.JavaRuntimeProgress.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Java.JavaRuntimeProgress.GetHashCode() -> System.Int32 | instance public
M FMCore.Java.JavaRuntimeProgress.ToString() -> System.String | instance public
M FMCore.Java.JavaRuntimeProgress.get_CompletedFiles() -> System.Int32 | instance public
M FMCore.Java.JavaRuntimeProgress.get_Download() -> FMCore.Downloader.DownloadProgress | instance public
M FMCore.Java.JavaRuntimeProgress.get_Stage() -> FMCore.Java.JavaRuntimeStage | instance public
M FMCore.Java.JavaRuntimeProgress.get_TotalFiles() -> System.Int32 | instance public
M FMCore.Java.JavaRuntimeProgress.op_Equality(FMCore.Java.JavaRuntimeProgress left, FMCore.Java.JavaRuntimeProgress right) -> System.Boolean | static public
M FMCore.Java.JavaRuntimeProgress.op_Inequality(FMCore.Java.JavaRuntimeProgress left, FMCore.Java.JavaRuntimeProgress right) -> System.Boolean | static public
M FMCore.Java.JavaRuntimeProgress.set_CompletedFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Java.JavaRuntimeProgress.set_Download(FMCore.Downloader.DownloadProgress value) -> System.Void | instance public
M FMCore.Java.JavaRuntimeProgress.set_Stage(FMCore.Java.JavaRuntimeStage value) -> System.Void | instance public
M FMCore.Java.JavaRuntimeProgress.set_TotalFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Java.JavaRuntimeUtil.GetAvailableVersionsAsync(System.Nullable`1[System.Int32] majorVersion = default, FMCore.Java.JavaRuntimeOptions options = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Java.JavaRuntimeVersion]] | static public
M FMCore.Java.JavaRuntimeUtil.InstallAsync(FMCore.Java.JavaRuntimeVersion version, FMCore.Java.JavaRuntimeOptions options = null, System.IProgress`1[FMCore.Java.JavaRuntimeProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Java.JavaRuntime] | static public
M FMCore.Java.JavaRuntimeUtil.InstallAsync(System.Int32 majorVersion, FMCore.Java.JavaRuntimeOptions options = null, System.IProgress`1[FMCore.Java.JavaRuntimeProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Java.JavaRuntime] | static public
M FMCore.Java.JavaRuntimeUtil.get_DefaultInstallationDirectory() -> System.String | static public
M FMCore.Java.JavaRuntimeVersion.ToString() -> System.String | instance public
M FMCore.Java.JavaRuntimeVersion.get_Architecture() -> FMCore.Java.JavaArchitecture | instance public
M FMCore.Java.JavaRuntimeVersion.get_Distribution() -> FMCore.Java.JavaDistribution | instance public
M FMCore.Java.JavaRuntimeVersion.get_DownloadUri() -> System.Uri | instance public
M FMCore.Java.JavaRuntimeVersion.get_FullVersion() -> System.String | instance public
M FMCore.Java.JavaRuntimeVersion.get_Id() -> System.String | instance public
M FMCore.Java.JavaRuntimeVersion.get_MajorVersion() -> System.Int32 | instance public
M FMCore.Java.JavaRuntimeVersion.get_PackageType() -> FMCore.Java.JavaPackageType | instance public
M FMCore.Java.JavaRuntimeVersion.get_Sha256() -> System.String | instance public
M FMCore.Java.JavaRuntimeVersion.get_Size() -> System.Int64 | instance public
M FMCore.Java.JavaRuntimeVersion.get_SizeIsApproximate() -> System.Boolean | instance public
M FMCore.Java.JavaSelectionOptions..ctor() -> constructor | instance public
M FMCore.Java.JavaSelectionOptions.<Clone>$() -> FMCore.Java.JavaSelectionOptions | instance public
M FMCore.Java.JavaSelectionOptions.Equals(FMCore.Java.JavaSelectionOptions other) -> System.Boolean | instance public
M FMCore.Java.JavaSelectionOptions.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Java.JavaSelectionOptions.GetHashCode() -> System.Int32 | instance public
M FMCore.Java.JavaSelectionOptions.ToString() -> System.String | instance public
M FMCore.Java.JavaSelectionOptions.get_AllowEarlyAccess() -> System.Boolean | instance public
M FMCore.Java.JavaSelectionOptions.get_FallbackMajorVersion() -> System.Nullable`1[System.Int32] | instance public
M FMCore.Java.JavaSelectionOptions.get_TargetArchitecture() -> FMCore.Java.JavaArchitecture | instance public
M FMCore.Java.JavaSelectionOptions.op_Equality(FMCore.Java.JavaSelectionOptions left, FMCore.Java.JavaSelectionOptions right) -> System.Boolean | static public
M FMCore.Java.JavaSelectionOptions.op_Inequality(FMCore.Java.JavaSelectionOptions left, FMCore.Java.JavaSelectionOptions right) -> System.Boolean | static public
M FMCore.Java.JavaSelectionOptions.set_AllowEarlyAccess(System.Boolean value) -> System.Void | instance public
M FMCore.Java.JavaSelectionOptions.set_FallbackMajorVersion(System.Nullable`1[System.Int32] value) -> System.Void | instance public
M FMCore.Java.JavaSelectionOptions.set_TargetArchitecture(FMCore.Java.JavaArchitecture value) -> System.Void | instance public
M FMCore.Java.JavaUtil.GetJavaInfo(System.String javaPath) -> FMCore.Java.JavaRuntime | static public
M FMCore.Java.JavaUtil.GetJavaInfoAsync(System.String javaPath, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Java.JavaRuntime] | static public
M FMCore.Java.JavaUtil.GetJavas(FMCore.Java.JavaDetectionOptions options = null) -> System.Collections.Generic.IReadOnlyList`1[FMCore.Java.JavaRuntime] | static public
M FMCore.Java.JavaUtil.GetJavasAsync(FMCore.Java.JavaDetectionOptions options = null, System.IProgress`1[FMCore.Java.JavaDetectionProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Java.JavaRuntime]] | static public
M FMCore.Java.JavaVersion.<Clone>$() -> FMCore.Java.JavaVersion | instance public
M FMCore.Java.JavaVersion.CompareTo(FMCore.Java.JavaVersion other) -> System.Int32 | instance public
M FMCore.Java.JavaVersion.Equals(FMCore.Java.JavaVersion other) -> System.Boolean | instance public
M FMCore.Java.JavaVersion.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Java.JavaVersion.GetHashCode() -> System.Int32 | instance public
M FMCore.Java.JavaVersion.Parse(System.String text) -> FMCore.Java.JavaVersion | static public
M FMCore.Java.JavaVersion.ToString() -> System.String | instance public
M FMCore.Java.JavaVersion.get_Build() -> System.Int32 | instance public
M FMCore.Java.JavaVersion.get_IsEarlyAccess() -> System.Boolean | instance public
M FMCore.Java.JavaVersion.get_Major() -> System.Int32 | instance public
M FMCore.Java.JavaVersion.get_Minor() -> System.Int32 | instance public
M FMCore.Java.JavaVersion.get_Patch() -> System.Int32 | instance public
M FMCore.Java.JavaVersion.get_Security() -> System.Int32 | instance public
M FMCore.Java.JavaVersion.get_Text() -> System.String | instance public
M FMCore.Java.JavaVersion.op_Equality(FMCore.Java.JavaVersion left, FMCore.Java.JavaVersion right) -> System.Boolean | static public
M FMCore.Java.JavaVersion.op_Inequality(FMCore.Java.JavaVersion left, FMCore.Java.JavaVersion right) -> System.Boolean | static public
P FMCore.Java.JavaCompatibilityResult.ErrorCode : System.Nullable`1[FMCore.Java.JavaErrorCode] | get_ErrorCode,set_ErrorCode[init]
P FMCore.Java.JavaCompatibilityResult.IsCompatible : System.Boolean | get_IsCompatible,set_IsCompatible[init]
P FMCore.Java.JavaCompatibilityResult.RequiredArchitecture : FMCore.Java.JavaArchitecture | get_RequiredArchitecture,set_RequiredArchitecture[init]
P FMCore.Java.JavaCompatibilityResult.RequiredMajorVersion : System.Nullable`1[System.Int32] | get_RequiredMajorVersion,set_RequiredMajorVersion[init]
P FMCore.Java.JavaDetectionIssue.Code : FMCore.Java.JavaErrorCode | get_Code,set_Code[init]
P FMCore.Java.JavaDetectionIssue.ExitCode : System.Nullable`1[System.Int32] | get_ExitCode,set_ExitCode[init]
P FMCore.Java.JavaDetectionIssue.Path : System.String | get_Path,set_Path[init]
P FMCore.Java.JavaDetectionOptions.JavaHome : System.String | get_JavaHome,set_JavaHome[init]
P FMCore.Java.JavaDetectionOptions.MaxCandidates : System.Int32 | get_MaxCandidates,set_MaxCandidates[init]
P FMCore.Java.JavaDetectionOptions.MaxOutputCharacters : System.Int32 | get_MaxOutputCharacters,set_MaxOutputCharacters[init]
P FMCore.Java.JavaDetectionOptions.PathDirectories : System.Collections.Generic.IReadOnlyList`1[System.String] | get_PathDirectories,set_PathDirectories[init]
P FMCore.Java.JavaDetectionOptions.ProbeTimeout : System.TimeSpan | get_ProbeTimeout,set_ProbeTimeout[init]
P FMCore.Java.JavaDetectionOptions.SearchDirectories : System.Collections.Generic.IReadOnlyList`1[System.String] | get_SearchDirectories,set_SearchDirectories[init]
P FMCore.Java.JavaDetectionProgress.Completed : System.Int32 | get_Completed,set_Completed[init]
P FMCore.Java.JavaDetectionProgress.ErrorCode : System.Nullable`1[FMCore.Java.JavaErrorCode] | get_ErrorCode,set_ErrorCode[init]
P FMCore.Java.JavaDetectionProgress.ExecutablePath : System.String | get_ExecutablePath,set_ExecutablePath[init]
P FMCore.Java.JavaDetectionProgress.Total : System.Int32 | get_Total,set_Total[init]
P FMCore.Java.JavaDetectionResult.IsTruncated : System.Boolean | get_IsTruncated,set_IsTruncated[init]
P FMCore.Java.JavaDetectionResult.Issues : System.Collections.Generic.IReadOnlyList`1[FMCore.Java.JavaDetectionIssue] | get_Issues,set_Issues[init]
P FMCore.Java.JavaDetectionResult.Runtimes : System.Collections.Generic.IReadOnlyList`1[FMCore.Java.JavaRuntime] | get_Runtimes,set_Runtimes[init]
P FMCore.Java.JavaException.Code : FMCore.Java.JavaErrorCode | get_Code
P FMCore.Java.JavaException.ExitCode : System.Nullable`1[System.Int32] | get_ExitCode
P FMCore.Java.JavaRuntime.Architecture : FMCore.Java.JavaArchitecture | get_Architecture,set_Architecture[init]
P FMCore.Java.JavaRuntime.ExecutablePath : System.String | get_ExecutablePath,set_ExecutablePath[init]
P FMCore.Java.JavaRuntime.HomeDirectory : System.String | get_HomeDirectory,set_HomeDirectory[init]
P FMCore.Java.JavaRuntime.Vendor : System.String | get_Vendor,set_Vendor[init]
P FMCore.Java.JavaRuntime.Version : FMCore.Java.JavaVersion | get_Version,set_Version[init]
P FMCore.Java.JavaRuntimeOptions.Architecture : FMCore.Java.JavaArchitecture | get_Architecture,set_Architecture[init]
P FMCore.Java.JavaRuntimeOptions.Distribution : FMCore.Java.JavaDistribution | get_Distribution,set_Distribution[init]
P FMCore.Java.JavaRuntimeOptions.Downloads : FMCore.Downloader.DownloadOptions | get_Downloads,set_Downloads[init]
P FMCore.Java.JavaRuntimeOptions.FullVersion : System.String | get_FullVersion,set_FullVersion[init]
P FMCore.Java.JavaRuntimeOptions.HttpClient : System.Net.Http.HttpClient | get_HttpClient,set_HttpClient[init]
P FMCore.Java.JavaRuntimeOptions.InstallationDirectory : System.String | get_InstallationDirectory,set_InstallationDirectory[init]
P FMCore.Java.JavaRuntimeOptions.PackageType : FMCore.Java.JavaPackageType | get_PackageType,set_PackageType[init]
P FMCore.Java.JavaRuntimeOptions.ProbeTimeout : System.TimeSpan | get_ProbeTimeout,set_ProbeTimeout[init]
P FMCore.Java.JavaRuntimeOptions.QueryTimeout : System.TimeSpan | get_QueryTimeout,set_QueryTimeout[init]
P FMCore.Java.JavaRuntimeProgress.CompletedFiles : System.Int32 | get_CompletedFiles,set_CompletedFiles[init]
P FMCore.Java.JavaRuntimeProgress.Download : FMCore.Downloader.DownloadProgress | get_Download,set_Download[init]
P FMCore.Java.JavaRuntimeProgress.Stage : FMCore.Java.JavaRuntimeStage | get_Stage,set_Stage[init]
P FMCore.Java.JavaRuntimeProgress.TotalFiles : System.Int32 | get_TotalFiles,set_TotalFiles[init]
P FMCore.Java.JavaRuntimeUtil.DefaultInstallationDirectory : System.String | get_DefaultInstallationDirectory
P FMCore.Java.JavaRuntimeVersion.Architecture : FMCore.Java.JavaArchitecture | get_Architecture
P FMCore.Java.JavaRuntimeVersion.Distribution : FMCore.Java.JavaDistribution | get_Distribution
P FMCore.Java.JavaRuntimeVersion.DownloadUri : System.Uri | get_DownloadUri
P FMCore.Java.JavaRuntimeVersion.FullVersion : System.String | get_FullVersion
P FMCore.Java.JavaRuntimeVersion.Id : System.String | get_Id
P FMCore.Java.JavaRuntimeVersion.MajorVersion : System.Int32 | get_MajorVersion
P FMCore.Java.JavaRuntimeVersion.PackageType : FMCore.Java.JavaPackageType | get_PackageType
P FMCore.Java.JavaRuntimeVersion.Sha256 : System.String | get_Sha256
P FMCore.Java.JavaRuntimeVersion.Size : System.Int64 | get_Size
P FMCore.Java.JavaRuntimeVersion.SizeIsApproximate : System.Boolean | get_SizeIsApproximate
P FMCore.Java.JavaSelectionOptions.AllowEarlyAccess : System.Boolean | get_AllowEarlyAccess,set_AllowEarlyAccess[init]
P FMCore.Java.JavaSelectionOptions.FallbackMajorVersion : System.Nullable`1[System.Int32] | get_FallbackMajorVersion,set_FallbackMajorVersion[init]
P FMCore.Java.JavaSelectionOptions.TargetArchitecture : FMCore.Java.JavaArchitecture | get_TargetArchitecture,set_TargetArchitecture[init]
P FMCore.Java.JavaVersion.Build : System.Int32 | get_Build
P FMCore.Java.JavaVersion.IsEarlyAccess : System.Boolean | get_IsEarlyAccess
P FMCore.Java.JavaVersion.Major : System.Int32 | get_Major
P FMCore.Java.JavaVersion.Minor : System.Int32 | get_Minor
P FMCore.Java.JavaVersion.Patch : System.Int32 | get_Patch
P FMCore.Java.JavaVersion.Security : System.Int32 | get_Security
P FMCore.Java.JavaVersion.Text : System.String | get_Text
T FMCore.Java.JavaArchitecture : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Java.JavaCompatibility : System.Object |
T FMCore.Java.JavaCompatibilityResult : System.Object | System.IEquatable`1[FMCore.Java.JavaCompatibilityResult]
T FMCore.Java.JavaDetectionIssue : System.Object | System.IEquatable`1[FMCore.Java.JavaDetectionIssue]
T FMCore.Java.JavaDetectionOptions : System.Object | System.IEquatable`1[FMCore.Java.JavaDetectionOptions]
T FMCore.Java.JavaDetectionProgress : System.Object | System.IEquatable`1[FMCore.Java.JavaDetectionProgress]
T FMCore.Java.JavaDetectionResult : System.Object | System.IEquatable`1[FMCore.Java.JavaDetectionResult]
T FMCore.Java.JavaDistribution : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Java.JavaErrorCode : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Java.JavaException : System.Exception | System.Runtime.Serialization.ISerializable
T FMCore.Java.JavaManager : System.Object |
T FMCore.Java.JavaPackageType : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Java.JavaRuntime : System.Object | System.IEquatable`1[FMCore.Java.JavaRuntime]
T FMCore.Java.JavaRuntimeOptions : System.Object | System.IEquatable`1[FMCore.Java.JavaRuntimeOptions]
T FMCore.Java.JavaRuntimeProgress : System.Object | System.IEquatable`1[FMCore.Java.JavaRuntimeProgress]
T FMCore.Java.JavaRuntimeStage : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Java.JavaRuntimeUtil : System.Object |
T FMCore.Java.JavaRuntimeVersion : System.Object |
T FMCore.Java.JavaSelectionOptions : System.Object | System.IEquatable`1[FMCore.Java.JavaSelectionOptions]
T FMCore.Java.JavaUtil : System.Object |
T FMCore.Java.JavaVersion : System.Object | System.IComparable`1[FMCore.Java.JavaVersion], System.IEquatable`1[FMCore.Java.JavaVersion]
```

## Launch

```text
E FMCore.Launch.MinecraftLauncher.ErrorReceived : System.EventHandler`1[FMCore.Launch.GameLogEventArgs]
E FMCore.Launch.MinecraftLauncher.Exited : System.EventHandler`1[FMCore.Launch.GameExitedEventArgs]
E FMCore.Launch.MinecraftLauncher.OutputReceived : System.EventHandler`1[FMCore.Launch.GameLogEventArgs]
E FMCore.Launch.MinecraftLauncher.StateChanged : System.EventHandler`1[FMCore.Launch.LaunchStateChangedEventArgs]
F FMCore.Launch.LaunchErrorCode.AlreadyRunning : FMCore.Launch.LaunchErrorCode = 1
F FMCore.Launch.LaunchErrorCode.AuthenticationError : FMCore.Launch.LaunchErrorCode = 5
F FMCore.Launch.LaunchErrorCode.InstallationError : FMCore.Launch.LaunchErrorCode = 6
F FMCore.Launch.LaunchErrorCode.InstanceBusy : FMCore.Launch.LaunchErrorCode = 2
F FMCore.Launch.LaunchErrorCode.InvalidArguments : FMCore.Launch.LaunchErrorCode = 8
F FMCore.Launch.LaunchErrorCode.InvalidConfig : FMCore.Launch.LaunchErrorCode = 0
F FMCore.Launch.LaunchErrorCode.IoError : FMCore.Launch.LaunchErrorCode = 12
F FMCore.Launch.LaunchErrorCode.JavaError : FMCore.Launch.LaunchErrorCode = 4
F FMCore.Launch.LaunchErrorCode.MissingFiles : FMCore.Launch.LaunchErrorCode = 7
F FMCore.Launch.LaunchErrorCode.ProcessControlFailed : FMCore.Launch.LaunchErrorCode = 11
F FMCore.Launch.LaunchErrorCode.ProcessStartFailed : FMCore.Launch.LaunchErrorCode = 10
F FMCore.Launch.LaunchErrorCode.UnexpectedError : FMCore.Launch.LaunchErrorCode = 13
F FMCore.Launch.LaunchErrorCode.UnsafePath : FMCore.Launch.LaunchErrorCode = 9
F FMCore.Launch.LaunchErrorCode.UnsupportedGameSettings : FMCore.Launch.LaunchErrorCode = 14
F FMCore.Launch.LaunchErrorCode.VersionError : FMCore.Launch.LaunchErrorCode = 3
F FMCore.Launch.LaunchStage.Account : FMCore.Launch.LaunchStage = 2
F FMCore.Launch.LaunchStage.Arguments : FMCore.Launch.LaunchStage = 4
F FMCore.Launch.LaunchStage.Installation : FMCore.Launch.LaunchStage = 3
F FMCore.Launch.LaunchStage.Java : FMCore.Launch.LaunchStage = 1
F FMCore.Launch.LaunchStage.Metadata : FMCore.Launch.LaunchStage = 0
F FMCore.Launch.LaunchStage.Process : FMCore.Launch.LaunchStage = 5
F FMCore.Launch.LaunchState.Canceled : FMCore.Launch.LaunchState = 7
F FMCore.Launch.LaunchState.Exited : FMCore.Launch.LaunchState = 5
F FMCore.Launch.LaunchState.Failed : FMCore.Launch.LaunchState = 6
F FMCore.Launch.LaunchState.Idle : FMCore.Launch.LaunchState = 0
F FMCore.Launch.LaunchState.Preparing : FMCore.Launch.LaunchState = 1
F FMCore.Launch.LaunchState.Running : FMCore.Launch.LaunchState = 3
F FMCore.Launch.LaunchState.Starting : FMCore.Launch.LaunchState = 2
F FMCore.Launch.LaunchState.Stopping : FMCore.Launch.LaunchState = 4
M FMCore.Launch.GameCoreConfig..ctor() -> constructor | instance public
M FMCore.Launch.GameCoreConfig.<Clone>$() -> FMCore.Launch.GameCoreConfig | instance public
M FMCore.Launch.GameCoreConfig.Equals(FMCore.Launch.GameCoreConfig other) -> System.Boolean | instance public
M FMCore.Launch.GameCoreConfig.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Launch.GameCoreConfig.GetHashCode() -> System.Int32 | instance public
M FMCore.Launch.GameCoreConfig.ToString() -> System.String | instance public
M FMCore.Launch.GameCoreConfig.get_AutoInstall() -> System.Boolean | instance public
M FMCore.Launch.GameCoreConfig.get_CachePolicy() -> FMCore.Versions.VersionCachePolicy | instance public
M FMCore.Launch.GameCoreConfig.get_GameDirectory() -> System.String | instance public
M FMCore.Launch.GameCoreConfig.get_InstanceId() -> System.String | instance public
M FMCore.Launch.GameCoreConfig.get_Isolate() -> System.Boolean | instance public
M FMCore.Launch.GameCoreConfig.get_RootDirectory() -> System.String | instance public
M FMCore.Launch.GameCoreConfig.get_VersionId() -> System.String | instance public
M FMCore.Launch.GameCoreConfig.op_Equality(FMCore.Launch.GameCoreConfig left, FMCore.Launch.GameCoreConfig right) -> System.Boolean | static public
M FMCore.Launch.GameCoreConfig.op_Inequality(FMCore.Launch.GameCoreConfig left, FMCore.Launch.GameCoreConfig right) -> System.Boolean | static public
M FMCore.Launch.GameCoreConfig.set_AutoInstall(System.Boolean value) -> System.Void | instance public
M FMCore.Launch.GameCoreConfig.set_CachePolicy(FMCore.Versions.VersionCachePolicy value) -> System.Void | instance public
M FMCore.Launch.GameCoreConfig.set_GameDirectory(System.String value) -> System.Void | instance public
M FMCore.Launch.GameCoreConfig.set_InstanceId(System.String value) -> System.Void | instance public
M FMCore.Launch.GameCoreConfig.set_Isolate(System.Boolean value) -> System.Void | instance public
M FMCore.Launch.GameCoreConfig.set_RootDirectory(System.String value) -> System.Void | instance public
M FMCore.Launch.GameCoreConfig.set_VersionId(System.String value) -> System.Void | instance public
M FMCore.Launch.GameExitResult..ctor(System.Int32 ProcessId, System.Int32 ExitCode, System.Boolean StoppedByCaller, System.DateTimeOffset ExitedAt) -> constructor | instance public
M FMCore.Launch.GameExitResult.<Clone>$() -> FMCore.Launch.GameExitResult | instance public
M FMCore.Launch.GameExitResult.Deconstruct(System.Int32& ProcessId, System.Int32& ExitCode, System.Boolean& StoppedByCaller, System.DateTimeOffset& ExitedAt) -> System.Void | instance public
M FMCore.Launch.GameExitResult.Equals(FMCore.Launch.GameExitResult other) -> System.Boolean | instance public
M FMCore.Launch.GameExitResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Launch.GameExitResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Launch.GameExitResult.ToString() -> System.String | instance public
M FMCore.Launch.GameExitResult.get_Diagnostics() -> FMCore.Logging.GameDiagnosticReport | instance public
M FMCore.Launch.GameExitResult.get_ExitCode() -> System.Int32 | instance public
M FMCore.Launch.GameExitResult.get_ExitedAt() -> System.DateTimeOffset | instance public
M FMCore.Launch.GameExitResult.get_ProcessId() -> System.Int32 | instance public
M FMCore.Launch.GameExitResult.get_StoppedByCaller() -> System.Boolean | instance public
M FMCore.Launch.GameExitResult.op_Equality(FMCore.Launch.GameExitResult left, FMCore.Launch.GameExitResult right) -> System.Boolean | static public
M FMCore.Launch.GameExitResult.op_Inequality(FMCore.Launch.GameExitResult left, FMCore.Launch.GameExitResult right) -> System.Boolean | static public
M FMCore.Launch.GameExitResult.set_Diagnostics(FMCore.Logging.GameDiagnosticReport value) -> System.Void | instance public
M FMCore.Launch.GameExitResult.set_ExitCode(System.Int32 value) -> System.Void | instance public
M FMCore.Launch.GameExitResult.set_ExitedAt(System.DateTimeOffset value) -> System.Void | instance public
M FMCore.Launch.GameExitResult.set_ProcessId(System.Int32 value) -> System.Void | instance public
M FMCore.Launch.GameExitResult.set_StoppedByCaller(System.Boolean value) -> System.Void | instance public
M FMCore.Launch.GameExitedEventArgs..ctor(FMCore.Launch.GameExitResult result) -> constructor | instance public
M FMCore.Launch.GameExitedEventArgs.get_Result() -> FMCore.Launch.GameExitResult | instance public
M FMCore.Launch.GameLogEventArgs..ctor(System.String text, System.Boolean isTruncated = False) -> constructor | instance public
M FMCore.Launch.GameLogEventArgs.get_IsTruncated() -> System.Boolean | instance public
M FMCore.Launch.GameLogEventArgs.get_Text() -> System.String | instance public
M FMCore.Launch.GameLogEventArgs.get_Timestamp() -> System.DateTimeOffset | instance public
M FMCore.Launch.GameServerConfig..ctor() -> constructor | instance public
M FMCore.Launch.GameServerConfig.<Clone>$() -> FMCore.Launch.GameServerConfig | instance public
M FMCore.Launch.GameServerConfig.Equals(FMCore.Launch.GameServerConfig other) -> System.Boolean | instance public
M FMCore.Launch.GameServerConfig.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Launch.GameServerConfig.GetHashCode() -> System.Int32 | instance public
M FMCore.Launch.GameServerConfig.ToString() -> System.String | instance public
M FMCore.Launch.GameServerConfig.get_Host() -> System.String | instance public
M FMCore.Launch.GameServerConfig.get_Port() -> System.Int32 | instance public
M FMCore.Launch.GameServerConfig.op_Equality(FMCore.Launch.GameServerConfig left, FMCore.Launch.GameServerConfig right) -> System.Boolean | static public
M FMCore.Launch.GameServerConfig.op_Inequality(FMCore.Launch.GameServerConfig left, FMCore.Launch.GameServerConfig right) -> System.Boolean | static public
M FMCore.Launch.GameServerConfig.set_Host(System.String value) -> System.Void | instance public
M FMCore.Launch.GameServerConfig.set_Port(System.Int32 value) -> System.Void | instance public
M FMCore.Launch.GameWindowConfig..ctor() -> constructor | instance public
M FMCore.Launch.GameWindowConfig.<Clone>$() -> FMCore.Launch.GameWindowConfig | instance public
M FMCore.Launch.GameWindowConfig.Equals(FMCore.Launch.GameWindowConfig other) -> System.Boolean | instance public
M FMCore.Launch.GameWindowConfig.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Launch.GameWindowConfig.GetHashCode() -> System.Int32 | instance public
M FMCore.Launch.GameWindowConfig.ToString() -> System.String | instance public
M FMCore.Launch.GameWindowConfig.get_Fullscreen() -> System.Boolean | instance public
M FMCore.Launch.GameWindowConfig.get_Height() -> System.Int32 | instance public
M FMCore.Launch.GameWindowConfig.get_Width() -> System.Int32 | instance public
M FMCore.Launch.GameWindowConfig.op_Equality(FMCore.Launch.GameWindowConfig left, FMCore.Launch.GameWindowConfig right) -> System.Boolean | static public
M FMCore.Launch.GameWindowConfig.op_Inequality(FMCore.Launch.GameWindowConfig left, FMCore.Launch.GameWindowConfig right) -> System.Boolean | static public
M FMCore.Launch.GameWindowConfig.set_Fullscreen(System.Boolean value) -> System.Void | instance public
M FMCore.Launch.GameWindowConfig.set_Height(System.Int32 value) -> System.Void | instance public
M FMCore.Launch.GameWindowConfig.set_Width(System.Int32 value) -> System.Void | instance public
M FMCore.Launch.JavaConfig..ctor() -> constructor | instance public
M FMCore.Launch.JavaConfig.<Clone>$() -> FMCore.Launch.JavaConfig | instance public
M FMCore.Launch.JavaConfig.Equals(FMCore.Launch.JavaConfig other) -> System.Boolean | instance public
M FMCore.Launch.JavaConfig.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Launch.JavaConfig.GetHashCode() -> System.Int32 | instance public
M FMCore.Launch.JavaConfig.ToString() -> System.String | instance public
M FMCore.Launch.JavaConfig.get_Arguments() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Launch.JavaConfig.get_Detection() -> FMCore.Java.JavaDetectionOptions | instance public
M FMCore.Launch.JavaConfig.get_JavaPath() -> System.String | instance public
M FMCore.Launch.JavaConfig.get_MaxMemoryMb() -> System.Int32 | instance public
M FMCore.Launch.JavaConfig.get_MinMemoryMb() -> System.Int32 | instance public
M FMCore.Launch.JavaConfig.get_Selection() -> FMCore.Java.JavaSelectionOptions | instance public
M FMCore.Launch.JavaConfig.op_Equality(FMCore.Launch.JavaConfig left, FMCore.Launch.JavaConfig right) -> System.Boolean | static public
M FMCore.Launch.JavaConfig.op_Inequality(FMCore.Launch.JavaConfig left, FMCore.Launch.JavaConfig right) -> System.Boolean | static public
M FMCore.Launch.JavaConfig.set_Arguments(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Launch.JavaConfig.set_Detection(FMCore.Java.JavaDetectionOptions value) -> System.Void | instance public
M FMCore.Launch.JavaConfig.set_JavaPath(System.String value) -> System.Void | instance public
M FMCore.Launch.JavaConfig.set_MaxMemoryMb(System.Int32 value) -> System.Void | instance public
M FMCore.Launch.JavaConfig.set_MinMemoryMb(System.Int32 value) -> System.Void | instance public
M FMCore.Launch.JavaConfig.set_Selection(FMCore.Java.JavaSelectionOptions value) -> System.Void | instance public
M FMCore.Launch.LaunchConfig..ctor() -> constructor | instance public
M FMCore.Launch.LaunchConfig.<Clone>$() -> FMCore.Launch.LaunchConfig | instance public
M FMCore.Launch.LaunchConfig.Create(System.String versionId, System.String rootDirectory, FMCore.Authentication.LaunchIdentity account, FMCore.Java.JavaRuntime java, System.Int32 maxMemoryMb, System.Int32 minMemoryMb = 0) -> FMCore.Launch.LaunchConfig | static public
M FMCore.Launch.LaunchConfig.Equals(FMCore.Launch.LaunchConfig other) -> System.Boolean | instance public
M FMCore.Launch.LaunchConfig.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Launch.LaunchConfig.GetHashCode() -> System.Int32 | instance public
M FMCore.Launch.LaunchConfig.ToString() -> System.String | instance public
M FMCore.Launch.LaunchConfig.get_Account() -> FMCore.Authentication.LaunchIdentity | instance public
M FMCore.Launch.LaunchConfig.get_Accounts() -> FMCore.Authentication.AccountManager | instance public
M FMCore.Launch.LaunchConfig.get_Downloads() -> FMCore.Downloader.DownloadOptions | instance public
M FMCore.Launch.LaunchConfig.get_Features() -> System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.Boolean] | instance public
M FMCore.Launch.LaunchConfig.get_GameArguments() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Launch.LaunchConfig.get_GameCore() -> FMCore.Launch.GameCoreConfig | instance public
M FMCore.Launch.LaunchConfig.get_Java() -> FMCore.Launch.JavaConfig | instance public
M FMCore.Launch.LaunchConfig.get_LauncherName() -> System.String | instance public
M FMCore.Launch.LaunchConfig.get_LauncherVersion() -> System.String | instance public
M FMCore.Launch.LaunchConfig.get_Logging() -> FMCore.Logging.LoggingOptions | instance public
M FMCore.Launch.LaunchConfig.get_Server() -> FMCore.Launch.GameServerConfig | instance public
M FMCore.Launch.LaunchConfig.get_Window() -> FMCore.Launch.GameWindowConfig | instance public
M FMCore.Launch.LaunchConfig.op_Equality(FMCore.Launch.LaunchConfig left, FMCore.Launch.LaunchConfig right) -> System.Boolean | static public
M FMCore.Launch.LaunchConfig.op_Inequality(FMCore.Launch.LaunchConfig left, FMCore.Launch.LaunchConfig right) -> System.Boolean | static public
M FMCore.Launch.LaunchConfig.set_Account(FMCore.Authentication.LaunchIdentity value) -> System.Void | instance public
M FMCore.Launch.LaunchConfig.set_Accounts(FMCore.Authentication.AccountManager value) -> System.Void | instance public
M FMCore.Launch.LaunchConfig.set_Downloads(FMCore.Downloader.DownloadOptions value) -> System.Void | instance public
M FMCore.Launch.LaunchConfig.set_Features(System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.Boolean] value) -> System.Void | instance public
M FMCore.Launch.LaunchConfig.set_GameArguments(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Launch.LaunchConfig.set_GameCore(FMCore.Launch.GameCoreConfig value) -> System.Void | instance public
M FMCore.Launch.LaunchConfig.set_Java(FMCore.Launch.JavaConfig value) -> System.Void | instance public
M FMCore.Launch.LaunchConfig.set_LauncherName(System.String value) -> System.Void | instance public
M FMCore.Launch.LaunchConfig.set_LauncherVersion(System.String value) -> System.Void | instance public
M FMCore.Launch.LaunchConfig.set_Logging(FMCore.Logging.LoggingOptions value) -> System.Void | instance public
M FMCore.Launch.LaunchConfig.set_Server(FMCore.Launch.GameServerConfig value) -> System.Void | instance public
M FMCore.Launch.LaunchConfig.set_Window(FMCore.Launch.GameWindowConfig value) -> System.Void | instance public
M FMCore.Launch.LaunchError..ctor(FMCore.Launch.LaunchErrorCode Code, System.String Message, System.String DetailCode = null) -> constructor | instance public
M FMCore.Launch.LaunchError.<Clone>$() -> FMCore.Launch.LaunchError | instance public
M FMCore.Launch.LaunchError.Deconstruct(FMCore.Launch.LaunchErrorCode& Code, System.String& Message, System.String& DetailCode) -> System.Void | instance public
M FMCore.Launch.LaunchError.Equals(FMCore.Launch.LaunchError other) -> System.Boolean | instance public
M FMCore.Launch.LaunchError.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Launch.LaunchError.GetHashCode() -> System.Int32 | instance public
M FMCore.Launch.LaunchError.ToString() -> System.String | instance public
M FMCore.Launch.LaunchError.get_Code() -> FMCore.Launch.LaunchErrorCode | instance public
M FMCore.Launch.LaunchError.get_DetailCode() -> System.String | instance public
M FMCore.Launch.LaunchError.get_Message() -> System.String | instance public
M FMCore.Launch.LaunchError.op_Equality(FMCore.Launch.LaunchError left, FMCore.Launch.LaunchError right) -> System.Boolean | static public
M FMCore.Launch.LaunchError.op_Inequality(FMCore.Launch.LaunchError left, FMCore.Launch.LaunchError right) -> System.Boolean | static public
M FMCore.Launch.LaunchError.set_Code(FMCore.Launch.LaunchErrorCode value) -> System.Void | instance public
M FMCore.Launch.LaunchError.set_DetailCode(System.String value) -> System.Void | instance public
M FMCore.Launch.LaunchError.set_Message(System.String value) -> System.Void | instance public
M FMCore.Launch.LaunchException..ctor(FMCore.Launch.LaunchErrorCode code, System.String message) -> constructor | instance public
M FMCore.Launch.LaunchException.get_Code() -> FMCore.Launch.LaunchErrorCode | instance public
M FMCore.Launch.LaunchProgress..ctor(FMCore.Launch.LaunchStage Stage, FMCore.Installer.InstallationProgress Installation = null) -> constructor | instance public
M FMCore.Launch.LaunchProgress.<Clone>$() -> FMCore.Launch.LaunchProgress | instance public
M FMCore.Launch.LaunchProgress.Deconstruct(FMCore.Launch.LaunchStage& Stage, FMCore.Installer.InstallationProgress& Installation) -> System.Void | instance public
M FMCore.Launch.LaunchProgress.Equals(FMCore.Launch.LaunchProgress other) -> System.Boolean | instance public
M FMCore.Launch.LaunchProgress.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Launch.LaunchProgress.GetHashCode() -> System.Int32 | instance public
M FMCore.Launch.LaunchProgress.ToString() -> System.String | instance public
M FMCore.Launch.LaunchProgress.get_Installation() -> FMCore.Installer.InstallationProgress | instance public
M FMCore.Launch.LaunchProgress.get_Stage() -> FMCore.Launch.LaunchStage | instance public
M FMCore.Launch.LaunchProgress.op_Equality(FMCore.Launch.LaunchProgress left, FMCore.Launch.LaunchProgress right) -> System.Boolean | static public
M FMCore.Launch.LaunchProgress.op_Inequality(FMCore.Launch.LaunchProgress left, FMCore.Launch.LaunchProgress right) -> System.Boolean | static public
M FMCore.Launch.LaunchProgress.set_Installation(FMCore.Installer.InstallationProgress value) -> System.Void | instance public
M FMCore.Launch.LaunchProgress.set_Stage(FMCore.Launch.LaunchStage value) -> System.Void | instance public
M FMCore.Launch.LaunchResult..ctor(System.Boolean Success, System.Nullable`1[System.Int32] ProcessId, FMCore.Launch.LaunchError Error) -> constructor | instance public
M FMCore.Launch.LaunchResult.<Clone>$() -> FMCore.Launch.LaunchResult | instance public
M FMCore.Launch.LaunchResult.Deconstruct(System.Boolean& Success, System.Nullable`1[System.Int32]& ProcessId, FMCore.Launch.LaunchError& Error) -> System.Void | instance public
M FMCore.Launch.LaunchResult.Equals(FMCore.Launch.LaunchResult other) -> System.Boolean | instance public
M FMCore.Launch.LaunchResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Launch.LaunchResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Launch.LaunchResult.ToString() -> System.String | instance public
M FMCore.Launch.LaunchResult.get_Diagnostics() -> FMCore.Logging.GameDiagnosticReport | instance public
M FMCore.Launch.LaunchResult.get_Error() -> FMCore.Launch.LaunchError | instance public
M FMCore.Launch.LaunchResult.get_ProcessId() -> System.Nullable`1[System.Int32] | instance public
M FMCore.Launch.LaunchResult.get_Success() -> System.Boolean | instance public
M FMCore.Launch.LaunchResult.op_Equality(FMCore.Launch.LaunchResult left, FMCore.Launch.LaunchResult right) -> System.Boolean | static public
M FMCore.Launch.LaunchResult.op_Inequality(FMCore.Launch.LaunchResult left, FMCore.Launch.LaunchResult right) -> System.Boolean | static public
M FMCore.Launch.LaunchResult.set_Diagnostics(FMCore.Logging.GameDiagnosticReport value) -> System.Void | instance public
M FMCore.Launch.LaunchResult.set_Error(FMCore.Launch.LaunchError value) -> System.Void | instance public
M FMCore.Launch.LaunchResult.set_ProcessId(System.Nullable`1[System.Int32] value) -> System.Void | instance public
M FMCore.Launch.LaunchResult.set_Success(System.Boolean value) -> System.Void | instance public
M FMCore.Launch.LaunchStateChangedEventArgs..ctor(FMCore.Launch.LaunchState state, FMCore.Launch.LaunchError error = null) -> constructor | instance public
M FMCore.Launch.LaunchStateChangedEventArgs.get_Error() -> FMCore.Launch.LaunchError | instance public
M FMCore.Launch.LaunchStateChangedEventArgs.get_State() -> FMCore.Launch.LaunchState | instance public
M FMCore.Launch.MinecraftLauncher..ctor(FMCore.Launch.LaunchConfig config) -> constructor | instance public
M FMCore.Launch.MinecraftLauncher.DisposeAsync() -> System.Threading.Tasks.ValueTask | instance public
M FMCore.Launch.MinecraftLauncher.LaunchAsync(System.IProgress`1[FMCore.Launch.LaunchProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Launch.LaunchResult] | instance public
M FMCore.Launch.MinecraftLauncher.StopAsync(System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task | instance public
M FMCore.Launch.MinecraftLauncher.WaitForExitAsync(System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Launch.GameExitResult] | instance public
M FMCore.Launch.MinecraftLauncher.add_ErrorReceived(System.EventHandler`1[FMCore.Launch.GameLogEventArgs] value) -> System.Void | instance public
M FMCore.Launch.MinecraftLauncher.add_Exited(System.EventHandler`1[FMCore.Launch.GameExitedEventArgs] value) -> System.Void | instance public
M FMCore.Launch.MinecraftLauncher.add_OutputReceived(System.EventHandler`1[FMCore.Launch.GameLogEventArgs] value) -> System.Void | instance public
M FMCore.Launch.MinecraftLauncher.add_StateChanged(System.EventHandler`1[FMCore.Launch.LaunchStateChangedEventArgs] value) -> System.Void | instance public
M FMCore.Launch.MinecraftLauncher.get_ExitCode() -> System.Nullable`1[System.Int32] | instance public
M FMCore.Launch.MinecraftLauncher.get_LastDiagnostic() -> FMCore.Logging.GameDiagnosticReport | instance public
M FMCore.Launch.MinecraftLauncher.get_Logger() -> FMCore.Logging.LauncherLogger | instance public
M FMCore.Launch.MinecraftLauncher.get_ProcessId() -> System.Nullable`1[System.Int32] | instance public
M FMCore.Launch.MinecraftLauncher.get_State() -> FMCore.Launch.LaunchState | instance public
M FMCore.Launch.MinecraftLauncher.remove_ErrorReceived(System.EventHandler`1[FMCore.Launch.GameLogEventArgs] value) -> System.Void | instance public
M FMCore.Launch.MinecraftLauncher.remove_Exited(System.EventHandler`1[FMCore.Launch.GameExitedEventArgs] value) -> System.Void | instance public
M FMCore.Launch.MinecraftLauncher.remove_OutputReceived(System.EventHandler`1[FMCore.Launch.GameLogEventArgs] value) -> System.Void | instance public
M FMCore.Launch.MinecraftLauncher.remove_StateChanged(System.EventHandler`1[FMCore.Launch.LaunchStateChangedEventArgs] value) -> System.Void | instance public
P FMCore.Launch.GameCoreConfig.AutoInstall : System.Boolean | get_AutoInstall,set_AutoInstall[init]
P FMCore.Launch.GameCoreConfig.CachePolicy : FMCore.Versions.VersionCachePolicy | get_CachePolicy,set_CachePolicy[init]
P FMCore.Launch.GameCoreConfig.GameDirectory : System.String | get_GameDirectory,set_GameDirectory[init]
P FMCore.Launch.GameCoreConfig.InstanceId : System.String | get_InstanceId,set_InstanceId[init]
P FMCore.Launch.GameCoreConfig.Isolate : System.Boolean | get_Isolate,set_Isolate[init]
P FMCore.Launch.GameCoreConfig.RootDirectory : System.String | get_RootDirectory,set_RootDirectory[init]
P FMCore.Launch.GameCoreConfig.VersionId : System.String | get_VersionId,set_VersionId[init]
P FMCore.Launch.GameExitResult.Diagnostics : FMCore.Logging.GameDiagnosticReport | get_Diagnostics,set_Diagnostics[init]
P FMCore.Launch.GameExitResult.ExitCode : System.Int32 | get_ExitCode,set_ExitCode[init]
P FMCore.Launch.GameExitResult.ExitedAt : System.DateTimeOffset | get_ExitedAt,set_ExitedAt[init]
P FMCore.Launch.GameExitResult.ProcessId : System.Int32 | get_ProcessId,set_ProcessId[init]
P FMCore.Launch.GameExitResult.StoppedByCaller : System.Boolean | get_StoppedByCaller,set_StoppedByCaller[init]
P FMCore.Launch.GameExitedEventArgs.Result : FMCore.Launch.GameExitResult | get_Result
P FMCore.Launch.GameLogEventArgs.IsTruncated : System.Boolean | get_IsTruncated
P FMCore.Launch.GameLogEventArgs.Text : System.String | get_Text
P FMCore.Launch.GameLogEventArgs.Timestamp : System.DateTimeOffset | get_Timestamp
P FMCore.Launch.GameServerConfig.Host : System.String | get_Host,set_Host[init]
P FMCore.Launch.GameServerConfig.Port : System.Int32 | get_Port,set_Port[init]
P FMCore.Launch.GameWindowConfig.Fullscreen : System.Boolean | get_Fullscreen,set_Fullscreen[init]
P FMCore.Launch.GameWindowConfig.Height : System.Int32 | get_Height,set_Height[init]
P FMCore.Launch.GameWindowConfig.Width : System.Int32 | get_Width,set_Width[init]
P FMCore.Launch.JavaConfig.Arguments : System.Collections.Generic.IReadOnlyList`1[System.String] | get_Arguments,set_Arguments[init]
P FMCore.Launch.JavaConfig.Detection : FMCore.Java.JavaDetectionOptions | get_Detection,set_Detection[init]
P FMCore.Launch.JavaConfig.JavaPath : System.String | get_JavaPath,set_JavaPath[init]
P FMCore.Launch.JavaConfig.MaxMemoryMb : System.Int32 | get_MaxMemoryMb,set_MaxMemoryMb[init]
P FMCore.Launch.JavaConfig.MinMemoryMb : System.Int32 | get_MinMemoryMb,set_MinMemoryMb[init]
P FMCore.Launch.JavaConfig.Selection : FMCore.Java.JavaSelectionOptions | get_Selection,set_Selection[init]
P FMCore.Launch.LaunchConfig.Account : FMCore.Authentication.LaunchIdentity | get_Account,set_Account[init]
P FMCore.Launch.LaunchConfig.Accounts : FMCore.Authentication.AccountManager | get_Accounts,set_Accounts[init]
P FMCore.Launch.LaunchConfig.Downloads : FMCore.Downloader.DownloadOptions | get_Downloads,set_Downloads[init]
P FMCore.Launch.LaunchConfig.Features : System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.Boolean] | get_Features,set_Features[init]
P FMCore.Launch.LaunchConfig.GameArguments : System.Collections.Generic.IReadOnlyList`1[System.String] | get_GameArguments,set_GameArguments[init]
P FMCore.Launch.LaunchConfig.GameCore : FMCore.Launch.GameCoreConfig | get_GameCore,set_GameCore[init]
P FMCore.Launch.LaunchConfig.Java : FMCore.Launch.JavaConfig | get_Java,set_Java[init]
P FMCore.Launch.LaunchConfig.LauncherName : System.String | get_LauncherName,set_LauncherName[init]
P FMCore.Launch.LaunchConfig.LauncherVersion : System.String | get_LauncherVersion,set_LauncherVersion[init]
P FMCore.Launch.LaunchConfig.Logging : FMCore.Logging.LoggingOptions | get_Logging,set_Logging[init]
P FMCore.Launch.LaunchConfig.Server : FMCore.Launch.GameServerConfig | get_Server,set_Server[init]
P FMCore.Launch.LaunchConfig.Window : FMCore.Launch.GameWindowConfig | get_Window,set_Window[init]
P FMCore.Launch.LaunchError.Code : FMCore.Launch.LaunchErrorCode | get_Code,set_Code[init]
P FMCore.Launch.LaunchError.DetailCode : System.String | get_DetailCode,set_DetailCode[init]
P FMCore.Launch.LaunchError.Message : System.String | get_Message,set_Message[init]
P FMCore.Launch.LaunchException.Code : FMCore.Launch.LaunchErrorCode | get_Code
P FMCore.Launch.LaunchProgress.Installation : FMCore.Installer.InstallationProgress | get_Installation,set_Installation[init]
P FMCore.Launch.LaunchProgress.Stage : FMCore.Launch.LaunchStage | get_Stage,set_Stage[init]
P FMCore.Launch.LaunchResult.Diagnostics : FMCore.Logging.GameDiagnosticReport | get_Diagnostics,set_Diagnostics[init]
P FMCore.Launch.LaunchResult.Error : FMCore.Launch.LaunchError | get_Error,set_Error[init]
P FMCore.Launch.LaunchResult.ProcessId : System.Nullable`1[System.Int32] | get_ProcessId,set_ProcessId[init]
P FMCore.Launch.LaunchResult.Success : System.Boolean | get_Success,set_Success[init]
P FMCore.Launch.LaunchStateChangedEventArgs.Error : FMCore.Launch.LaunchError | get_Error
P FMCore.Launch.LaunchStateChangedEventArgs.State : FMCore.Launch.LaunchState | get_State
P FMCore.Launch.MinecraftLauncher.ExitCode : System.Nullable`1[System.Int32] | get_ExitCode
P FMCore.Launch.MinecraftLauncher.LastDiagnostic : FMCore.Logging.GameDiagnosticReport | get_LastDiagnostic
P FMCore.Launch.MinecraftLauncher.Logger : FMCore.Logging.LauncherLogger | get_Logger
P FMCore.Launch.MinecraftLauncher.ProcessId : System.Nullable`1[System.Int32] | get_ProcessId
P FMCore.Launch.MinecraftLauncher.State : FMCore.Launch.LaunchState | get_State
T FMCore.Launch.GameCoreConfig : System.Object | System.IEquatable`1[FMCore.Launch.GameCoreConfig]
T FMCore.Launch.GameExitResult : System.Object | System.IEquatable`1[FMCore.Launch.GameExitResult]
T FMCore.Launch.GameExitedEventArgs : System.EventArgs |
T FMCore.Launch.GameLogEventArgs : System.EventArgs |
T FMCore.Launch.GameServerConfig : System.Object | System.IEquatable`1[FMCore.Launch.GameServerConfig]
T FMCore.Launch.GameWindowConfig : System.Object | System.IEquatable`1[FMCore.Launch.GameWindowConfig]
T FMCore.Launch.JavaConfig : System.Object | System.IEquatable`1[FMCore.Launch.JavaConfig]
T FMCore.Launch.LaunchConfig : System.Object | System.IEquatable`1[FMCore.Launch.LaunchConfig]
T FMCore.Launch.LaunchError : System.Object | System.IEquatable`1[FMCore.Launch.LaunchError]
T FMCore.Launch.LaunchErrorCode : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Launch.LaunchException : System.Exception | System.Runtime.Serialization.ISerializable
T FMCore.Launch.LaunchProgress : System.Object | System.IEquatable`1[FMCore.Launch.LaunchProgress]
T FMCore.Launch.LaunchResult : System.Object | System.IEquatable`1[FMCore.Launch.LaunchResult]
T FMCore.Launch.LaunchStage : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Launch.LaunchState : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Launch.LaunchStateChangedEventArgs : System.EventArgs |
T FMCore.Launch.MinecraftLauncher : System.Object | System.IAsyncDisposable
```

## Loaders

```text
F FMCore.Loaders.FabricErrorCode.InstallationBusy : FMCore.Loaders.FabricErrorCode = 6
F FMCore.Loaders.FabricErrorCode.InvalidMetadata : FMCore.Loaders.FabricErrorCode = 0
F FMCore.Loaders.FabricErrorCode.IoError : FMCore.Loaders.FabricErrorCode = 8
F FMCore.Loaders.FabricErrorCode.NotCached : FMCore.Loaders.FabricErrorCode = 5
F FMCore.Loaders.FabricErrorCode.ProfileConflict : FMCore.Loaders.FabricErrorCode = 4
F FMCore.Loaders.FabricErrorCode.UnsafePath : FMCore.Loaders.FabricErrorCode = 7
F FMCore.Loaders.FabricErrorCode.UnsupportedCombination : FMCore.Loaders.FabricErrorCode = 2
F FMCore.Loaders.FabricErrorCode.UnsupportedFormat : FMCore.Loaders.FabricErrorCode = 1
F FMCore.Loaders.FabricErrorCode.UnsupportedJavaRequirement : FMCore.Loaders.FabricErrorCode = 3
F FMCore.Loaders.FabricInstallationStage.Checksums : FMCore.Loaders.FabricInstallationStage = 1
F FMCore.Loaders.FabricInstallationStage.Completed : FMCore.Loaders.FabricInstallationStage = 3
F FMCore.Loaders.FabricInstallationStage.Installing : FMCore.Loaders.FabricInstallationStage = 2
F FMCore.Loaders.FabricInstallationStage.Metadata : FMCore.Loaders.FabricInstallationStage = 0
F FMCore.Loaders.LoaderErrorCode.DownloadUnavailable : FMCore.Loaders.LoaderErrorCode = 13
F FMCore.Loaders.LoaderErrorCode.HashMismatch : FMCore.Loaders.LoaderErrorCode = 12
F FMCore.Loaders.LoaderErrorCode.InstallationBusy : FMCore.Loaders.LoaderErrorCode = 5
F FMCore.Loaders.LoaderErrorCode.InvalidArchive : FMCore.Loaders.LoaderErrorCode = 8
F FMCore.Loaders.LoaderErrorCode.InvalidMetadata : FMCore.Loaders.LoaderErrorCode = 0
F FMCore.Loaders.LoaderErrorCode.IoError : FMCore.Loaders.LoaderErrorCode = 7
F FMCore.Loaders.LoaderErrorCode.MissingOutput : FMCore.Loaders.LoaderErrorCode = 11
F FMCore.Loaders.LoaderErrorCode.NotCached : FMCore.Loaders.LoaderErrorCode = 4
F FMCore.Loaders.LoaderErrorCode.ProcessFailed : FMCore.Loaders.LoaderErrorCode = 9
F FMCore.Loaders.LoaderErrorCode.ProcessTimeout : FMCore.Loaders.LoaderErrorCode = 10
F FMCore.Loaders.LoaderErrorCode.ProfileConflict : FMCore.Loaders.LoaderErrorCode = 3
F FMCore.Loaders.LoaderErrorCode.UnsafePath : FMCore.Loaders.LoaderErrorCode = 6
F FMCore.Loaders.LoaderErrorCode.UnsupportedCombination : FMCore.Loaders.LoaderErrorCode = 2
F FMCore.Loaders.LoaderErrorCode.UnsupportedFormat : FMCore.Loaders.LoaderErrorCode = 1
F FMCore.Loaders.LoaderInstallationStage.Completed : FMCore.Loaders.LoaderInstallationStage = 5
F FMCore.Loaders.LoaderInstallationStage.Dependencies : FMCore.Loaders.LoaderInstallationStage = 2
F FMCore.Loaders.LoaderInstallationStage.InstallerDownload : FMCore.Loaders.LoaderInstallationStage = 1
F FMCore.Loaders.LoaderInstallationStage.Metadata : FMCore.Loaders.LoaderInstallationStage = 0
F FMCore.Loaders.LoaderInstallationStage.Processors : FMCore.Loaders.LoaderInstallationStage = 3
F FMCore.Loaders.LoaderInstallationStage.Verification : FMCore.Loaders.LoaderInstallationStage = 4
M FMCore.Loaders.FabricException.get_Code() -> FMCore.Loaders.FabricErrorCode | instance public
M FMCore.Loaders.FabricGameVersion..ctor(System.String Version, System.Boolean Stable) -> constructor | instance public
M FMCore.Loaders.FabricGameVersion.<Clone>$() -> FMCore.Loaders.FabricGameVersion | instance public
M FMCore.Loaders.FabricGameVersion.Deconstruct(System.String& Version, System.Boolean& Stable) -> System.Void | instance public
M FMCore.Loaders.FabricGameVersion.Equals(FMCore.Loaders.FabricGameVersion other) -> System.Boolean | instance public
M FMCore.Loaders.FabricGameVersion.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Loaders.FabricGameVersion.GetHashCode() -> System.Int32 | instance public
M FMCore.Loaders.FabricGameVersion.ToString() -> System.String | instance public
M FMCore.Loaders.FabricGameVersion.get_Stable() -> System.Boolean | instance public
M FMCore.Loaders.FabricGameVersion.get_Version() -> System.String | instance public
M FMCore.Loaders.FabricGameVersion.op_Equality(FMCore.Loaders.FabricGameVersion left, FMCore.Loaders.FabricGameVersion right) -> System.Boolean | static public
M FMCore.Loaders.FabricGameVersion.op_Inequality(FMCore.Loaders.FabricGameVersion left, FMCore.Loaders.FabricGameVersion right) -> System.Boolean | static public
M FMCore.Loaders.FabricGameVersion.set_Stable(System.Boolean value) -> System.Void | instance public
M FMCore.Loaders.FabricGameVersion.set_Version(System.String value) -> System.Void | instance public
M FMCore.Loaders.FabricInstallOptions..ctor() -> constructor | instance public
M FMCore.Loaders.FabricInstallOptions.<Clone>$() -> FMCore.Loaders.FabricInstallOptions | instance public
M FMCore.Loaders.FabricInstallOptions.Equals(FMCore.Loaders.FabricInstallOptions other) -> System.Boolean | instance public
M FMCore.Loaders.FabricInstallOptions.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Loaders.FabricInstallOptions.GetHashCode() -> System.Int32 | instance public
M FMCore.Loaders.FabricInstallOptions.ToString() -> System.String | instance public
M FMCore.Loaders.FabricInstallOptions.get_Installation() -> FMCore.Installer.InstallationOptions | instance public
M FMCore.Loaders.FabricInstallOptions.get_VersionId() -> System.String | instance public
M FMCore.Loaders.FabricInstallOptions.op_Equality(FMCore.Loaders.FabricInstallOptions left, FMCore.Loaders.FabricInstallOptions right) -> System.Boolean | static public
M FMCore.Loaders.FabricInstallOptions.op_Inequality(FMCore.Loaders.FabricInstallOptions left, FMCore.Loaders.FabricInstallOptions right) -> System.Boolean | static public
M FMCore.Loaders.FabricInstallOptions.set_Installation(FMCore.Installer.InstallationOptions value) -> System.Void | instance public
M FMCore.Loaders.FabricInstallOptions.set_VersionId(System.String value) -> System.Void | instance public
M FMCore.Loaders.FabricInstallationProgress..ctor(FMCore.Loaders.FabricInstallationStage Stage, System.Int32 CompletedFiles, System.Int32 TotalFiles, FMCore.Installer.InstallationProgress Installation = null, FMCore.Downloader.DownloadProgress Download = null) -> constructor | instance public
M FMCore.Loaders.FabricInstallationProgress.<Clone>$() -> FMCore.Loaders.FabricInstallationProgress | instance public
M FMCore.Loaders.FabricInstallationProgress.Deconstruct(FMCore.Loaders.FabricInstallationStage& Stage, System.Int32& CompletedFiles, System.Int32& TotalFiles, FMCore.Installer.InstallationProgress& Installation, FMCore.Downloader.DownloadProgress& Download) -> System.Void | instance public
M FMCore.Loaders.FabricInstallationProgress.Equals(FMCore.Loaders.FabricInstallationProgress other) -> System.Boolean | instance public
M FMCore.Loaders.FabricInstallationProgress.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Loaders.FabricInstallationProgress.GetHashCode() -> System.Int32 | instance public
M FMCore.Loaders.FabricInstallationProgress.ToString() -> System.String | instance public
M FMCore.Loaders.FabricInstallationProgress.get_CompletedFiles() -> System.Int32 | instance public
M FMCore.Loaders.FabricInstallationProgress.get_Download() -> FMCore.Downloader.DownloadProgress | instance public
M FMCore.Loaders.FabricInstallationProgress.get_Installation() -> FMCore.Installer.InstallationProgress | instance public
M FMCore.Loaders.FabricInstallationProgress.get_Stage() -> FMCore.Loaders.FabricInstallationStage | instance public
M FMCore.Loaders.FabricInstallationProgress.get_TotalFiles() -> System.Int32 | instance public
M FMCore.Loaders.FabricInstallationProgress.op_Equality(FMCore.Loaders.FabricInstallationProgress left, FMCore.Loaders.FabricInstallationProgress right) -> System.Boolean | static public
M FMCore.Loaders.FabricInstallationProgress.op_Inequality(FMCore.Loaders.FabricInstallationProgress left, FMCore.Loaders.FabricInstallationProgress right) -> System.Boolean | static public
M FMCore.Loaders.FabricInstallationProgress.set_CompletedFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Loaders.FabricInstallationProgress.set_Download(FMCore.Downloader.DownloadProgress value) -> System.Void | instance public
M FMCore.Loaders.FabricInstallationProgress.set_Installation(FMCore.Installer.InstallationProgress value) -> System.Void | instance public
M FMCore.Loaders.FabricInstallationProgress.set_Stage(FMCore.Loaders.FabricInstallationStage value) -> System.Void | instance public
M FMCore.Loaders.FabricInstallationProgress.set_TotalFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Loaders.FabricInstallationResult..ctor(System.String MinecraftVersion, System.String LoaderVersion, System.String VersionId, FMCore.Installer.InstallationResult Installation) -> constructor | instance public
M FMCore.Loaders.FabricInstallationResult.<Clone>$() -> FMCore.Loaders.FabricInstallationResult | instance public
M FMCore.Loaders.FabricInstallationResult.Deconstruct(System.String& MinecraftVersion, System.String& LoaderVersion, System.String& VersionId, FMCore.Installer.InstallationResult& Installation) -> System.Void | instance public
M FMCore.Loaders.FabricInstallationResult.Equals(FMCore.Loaders.FabricInstallationResult other) -> System.Boolean | instance public
M FMCore.Loaders.FabricInstallationResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Loaders.FabricInstallationResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Loaders.FabricInstallationResult.ToString() -> System.String | instance public
M FMCore.Loaders.FabricInstallationResult.get_Installation() -> FMCore.Installer.InstallationResult | instance public
M FMCore.Loaders.FabricInstallationResult.get_LoaderVersion() -> System.String | instance public
M FMCore.Loaders.FabricInstallationResult.get_MinecraftVersion() -> System.String | instance public
M FMCore.Loaders.FabricInstallationResult.get_VersionId() -> System.String | instance public
M FMCore.Loaders.FabricInstallationResult.op_Equality(FMCore.Loaders.FabricInstallationResult left, FMCore.Loaders.FabricInstallationResult right) -> System.Boolean | static public
M FMCore.Loaders.FabricInstallationResult.op_Inequality(FMCore.Loaders.FabricInstallationResult left, FMCore.Loaders.FabricInstallationResult right) -> System.Boolean | static public
M FMCore.Loaders.FabricInstallationResult.set_Installation(FMCore.Installer.InstallationResult value) -> System.Void | instance public
M FMCore.Loaders.FabricInstallationResult.set_LoaderVersion(System.String value) -> System.Void | instance public
M FMCore.Loaders.FabricInstallationResult.set_MinecraftVersion(System.String value) -> System.Void | instance public
M FMCore.Loaders.FabricInstallationResult.set_VersionId(System.String value) -> System.Void | instance public
M FMCore.Loaders.FabricInstaller..ctor(FMCore.Downloader.DownloadManager downloads, FMCore.Versions.VersionManager versions = null, System.Uri metaUri = null) -> constructor | instance public
M FMCore.Loaders.FabricInstaller..ctor(System.String rootDirectory = ".minecraft") -> constructor | instance public
M FMCore.Loaders.FabricInstaller.GetGameVersionsAsync(FMCore.Versions.VersionCachePolicy cachePolicy = Refresh, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Loaders.FabricGameVersion]] | instance public
M FMCore.Loaders.FabricInstaller.GetLoaderVersionsAsync(System.String minecraftVersion, FMCore.Versions.VersionCachePolicy cachePolicy = Refresh, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Loaders.FabricLoaderVersion]] | instance public
M FMCore.Loaders.FabricInstaller.InstallAsync(System.String minecraftVersion, System.String loaderVersion = null, FMCore.Loaders.FabricInstallOptions options = null, System.IProgress`1[FMCore.Loaders.FabricInstallationProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Loaders.FabricInstallationResult] | instance public
M FMCore.Loaders.FabricInstaller.get_OfficialMetaUri() -> System.Uri | static public
M FMCore.Loaders.FabricInstaller.get_RootDirectory() -> System.String | instance public
M FMCore.Loaders.FabricLoaderVersion..ctor(System.String Version, System.Boolean Stable, System.String IntermediaryVersion) -> constructor | instance public
M FMCore.Loaders.FabricLoaderVersion.<Clone>$() -> FMCore.Loaders.FabricLoaderVersion | instance public
M FMCore.Loaders.FabricLoaderVersion.Deconstruct(System.String& Version, System.Boolean& Stable, System.String& IntermediaryVersion) -> System.Void | instance public
M FMCore.Loaders.FabricLoaderVersion.Equals(FMCore.Loaders.FabricLoaderVersion other) -> System.Boolean | instance public
M FMCore.Loaders.FabricLoaderVersion.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Loaders.FabricLoaderVersion.GetHashCode() -> System.Int32 | instance public
M FMCore.Loaders.FabricLoaderVersion.ToString() -> System.String | instance public
M FMCore.Loaders.FabricLoaderVersion.get_IntermediaryVersion() -> System.String | instance public
M FMCore.Loaders.FabricLoaderVersion.get_Stable() -> System.Boolean | instance public
M FMCore.Loaders.FabricLoaderVersion.get_Version() -> System.String | instance public
M FMCore.Loaders.FabricLoaderVersion.op_Equality(FMCore.Loaders.FabricLoaderVersion left, FMCore.Loaders.FabricLoaderVersion right) -> System.Boolean | static public
M FMCore.Loaders.FabricLoaderVersion.op_Inequality(FMCore.Loaders.FabricLoaderVersion left, FMCore.Loaders.FabricLoaderVersion right) -> System.Boolean | static public
M FMCore.Loaders.FabricLoaderVersion.set_IntermediaryVersion(System.String value) -> System.Void | instance public
M FMCore.Loaders.FabricLoaderVersion.set_Stable(System.Boolean value) -> System.Void | instance public
M FMCore.Loaders.FabricLoaderVersion.set_Version(System.String value) -> System.Void | instance public
M FMCore.Loaders.ForgeInstaller..ctor(FMCore.Downloader.DownloadManager downloads, FMCore.Versions.VersionManager versions = null, System.Uri mavenUri = null) -> constructor | instance public
M FMCore.Loaders.ForgeInstaller..ctor(System.String rootDirectory = ".minecraft") -> constructor | instance public
M FMCore.Loaders.ForgeInstaller.GetVersionsAsync(System.String minecraftVersion, FMCore.Versions.VersionCachePolicy cachePolicy = Refresh, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Loaders.MavenLoaderVersion]] | instance public
M FMCore.Loaders.ForgeInstaller.InstallAsync(System.String minecraftVersion, System.String forgeVersion = null, FMCore.Loaders.LoaderInstallOptions options = null, System.IProgress`1[FMCore.Loaders.LoaderInstallationProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Loaders.LoaderInstallationResult] | instance public
M FMCore.Loaders.ForgeInstaller.get_OfficialMavenUri() -> System.Uri | static public
M FMCore.Loaders.ForgeInstaller.get_RootDirectory() -> System.String | instance public
M FMCore.Loaders.LoaderException.get_Code() -> FMCore.Loaders.LoaderErrorCode | instance public
M FMCore.Loaders.LoaderException.get_ExitCode() -> System.Nullable`1[System.Int32] | instance public
M FMCore.Loaders.LoaderInstallOptions..ctor() -> constructor | instance public
M FMCore.Loaders.LoaderInstallOptions.<Clone>$() -> FMCore.Loaders.LoaderInstallOptions | instance public
M FMCore.Loaders.LoaderInstallOptions.Equals(FMCore.Loaders.LoaderInstallOptions other) -> System.Boolean | instance public
M FMCore.Loaders.LoaderInstallOptions.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Loaders.LoaderInstallOptions.GetHashCode() -> System.Int32 | instance public
M FMCore.Loaders.LoaderInstallOptions.ToString() -> System.String | instance public
M FMCore.Loaders.LoaderInstallOptions.get_Installation() -> FMCore.Installer.InstallationOptions | instance public
M FMCore.Loaders.LoaderInstallOptions.get_InstallerTimeout() -> System.TimeSpan | instance public
M FMCore.Loaders.LoaderInstallOptions.get_JavaDetection() -> FMCore.Java.JavaDetectionOptions | instance public
M FMCore.Loaders.LoaderInstallOptions.get_JavaPath() -> System.String | instance public
M FMCore.Loaders.LoaderInstallOptions.get_VersionId() -> System.String | instance public
M FMCore.Loaders.LoaderInstallOptions.op_Equality(FMCore.Loaders.LoaderInstallOptions left, FMCore.Loaders.LoaderInstallOptions right) -> System.Boolean | static public
M FMCore.Loaders.LoaderInstallOptions.op_Inequality(FMCore.Loaders.LoaderInstallOptions left, FMCore.Loaders.LoaderInstallOptions right) -> System.Boolean | static public
M FMCore.Loaders.LoaderInstallOptions.set_Installation(FMCore.Installer.InstallationOptions value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallOptions.set_InstallerTimeout(System.TimeSpan value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallOptions.set_JavaDetection(FMCore.Java.JavaDetectionOptions value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallOptions.set_JavaPath(System.String value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallOptions.set_VersionId(System.String value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallationProgress..ctor(FMCore.Loaders.LoaderInstallationStage Stage, System.Int32 CompletedFiles = 0, System.Int32 TotalFiles = 0, FMCore.Installer.InstallationProgress Installation = null, FMCore.Downloader.DownloadProgress Download = null, System.String Message = null) -> constructor | instance public
M FMCore.Loaders.LoaderInstallationProgress.<Clone>$() -> FMCore.Loaders.LoaderInstallationProgress | instance public
M FMCore.Loaders.LoaderInstallationProgress.Deconstruct(FMCore.Loaders.LoaderInstallationStage& Stage, System.Int32& CompletedFiles, System.Int32& TotalFiles, FMCore.Installer.InstallationProgress& Installation, FMCore.Downloader.DownloadProgress& Download, System.String& Message) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallationProgress.Equals(FMCore.Loaders.LoaderInstallationProgress other) -> System.Boolean | instance public
M FMCore.Loaders.LoaderInstallationProgress.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Loaders.LoaderInstallationProgress.GetHashCode() -> System.Int32 | instance public
M FMCore.Loaders.LoaderInstallationProgress.ToString() -> System.String | instance public
M FMCore.Loaders.LoaderInstallationProgress.get_CompletedFiles() -> System.Int32 | instance public
M FMCore.Loaders.LoaderInstallationProgress.get_Download() -> FMCore.Downloader.DownloadProgress | instance public
M FMCore.Loaders.LoaderInstallationProgress.get_Installation() -> FMCore.Installer.InstallationProgress | instance public
M FMCore.Loaders.LoaderInstallationProgress.get_Message() -> System.String | instance public
M FMCore.Loaders.LoaderInstallationProgress.get_Stage() -> FMCore.Loaders.LoaderInstallationStage | instance public
M FMCore.Loaders.LoaderInstallationProgress.get_TotalFiles() -> System.Int32 | instance public
M FMCore.Loaders.LoaderInstallationProgress.op_Equality(FMCore.Loaders.LoaderInstallationProgress left, FMCore.Loaders.LoaderInstallationProgress right) -> System.Boolean | static public
M FMCore.Loaders.LoaderInstallationProgress.op_Inequality(FMCore.Loaders.LoaderInstallationProgress left, FMCore.Loaders.LoaderInstallationProgress right) -> System.Boolean | static public
M FMCore.Loaders.LoaderInstallationProgress.set_CompletedFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallationProgress.set_Download(FMCore.Downloader.DownloadProgress value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallationProgress.set_Installation(FMCore.Installer.InstallationProgress value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallationProgress.set_Message(System.String value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallationProgress.set_Stage(FMCore.Loaders.LoaderInstallationStage value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallationProgress.set_TotalFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallationResult..ctor(System.String Loader, System.String MinecraftVersion, System.String LoaderVersion, System.String VersionId, FMCore.Installer.InstallationResult Installation, System.Nullable`1[System.Int32] InstallerExitCode = default) -> constructor | instance public
M FMCore.Loaders.LoaderInstallationResult.<Clone>$() -> FMCore.Loaders.LoaderInstallationResult | instance public
M FMCore.Loaders.LoaderInstallationResult.Deconstruct(System.String& Loader, System.String& MinecraftVersion, System.String& LoaderVersion, System.String& VersionId, FMCore.Installer.InstallationResult& Installation, System.Nullable`1[System.Int32]& InstallerExitCode) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallationResult.Equals(FMCore.Loaders.LoaderInstallationResult other) -> System.Boolean | instance public
M FMCore.Loaders.LoaderInstallationResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Loaders.LoaderInstallationResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Loaders.LoaderInstallationResult.ToString() -> System.String | instance public
M FMCore.Loaders.LoaderInstallationResult.get_Installation() -> FMCore.Installer.InstallationResult | instance public
M FMCore.Loaders.LoaderInstallationResult.get_InstallerExitCode() -> System.Nullable`1[System.Int32] | instance public
M FMCore.Loaders.LoaderInstallationResult.get_Loader() -> System.String | instance public
M FMCore.Loaders.LoaderInstallationResult.get_LoaderVersion() -> System.String | instance public
M FMCore.Loaders.LoaderInstallationResult.get_MinecraftVersion() -> System.String | instance public
M FMCore.Loaders.LoaderInstallationResult.get_VersionId() -> System.String | instance public
M FMCore.Loaders.LoaderInstallationResult.op_Equality(FMCore.Loaders.LoaderInstallationResult left, FMCore.Loaders.LoaderInstallationResult right) -> System.Boolean | static public
M FMCore.Loaders.LoaderInstallationResult.op_Inequality(FMCore.Loaders.LoaderInstallationResult left, FMCore.Loaders.LoaderInstallationResult right) -> System.Boolean | static public
M FMCore.Loaders.LoaderInstallationResult.set_Installation(FMCore.Installer.InstallationResult value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallationResult.set_InstallerExitCode(System.Nullable`1[System.Int32] value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallationResult.set_Loader(System.String value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallationResult.set_LoaderVersion(System.String value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallationResult.set_MinecraftVersion(System.String value) -> System.Void | instance public
M FMCore.Loaders.LoaderInstallationResult.set_VersionId(System.String value) -> System.Void | instance public
M FMCore.Loaders.MavenLoaderVersion..ctor(System.String MinecraftVersion, System.String Version, System.String MavenVersion, System.Boolean IsPrerelease) -> constructor | instance public
M FMCore.Loaders.MavenLoaderVersion.<Clone>$() -> FMCore.Loaders.MavenLoaderVersion | instance public
M FMCore.Loaders.MavenLoaderVersion.Deconstruct(System.String& MinecraftVersion, System.String& Version, System.String& MavenVersion, System.Boolean& IsPrerelease) -> System.Void | instance public
M FMCore.Loaders.MavenLoaderVersion.Equals(FMCore.Loaders.MavenLoaderVersion other) -> System.Boolean | instance public
M FMCore.Loaders.MavenLoaderVersion.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Loaders.MavenLoaderVersion.GetHashCode() -> System.Int32 | instance public
M FMCore.Loaders.MavenLoaderVersion.ToString() -> System.String | instance public
M FMCore.Loaders.MavenLoaderVersion.get_IsPrerelease() -> System.Boolean | instance public
M FMCore.Loaders.MavenLoaderVersion.get_MavenVersion() -> System.String | instance public
M FMCore.Loaders.MavenLoaderVersion.get_MinecraftVersion() -> System.String | instance public
M FMCore.Loaders.MavenLoaderVersion.get_Version() -> System.String | instance public
M FMCore.Loaders.MavenLoaderVersion.op_Equality(FMCore.Loaders.MavenLoaderVersion left, FMCore.Loaders.MavenLoaderVersion right) -> System.Boolean | static public
M FMCore.Loaders.MavenLoaderVersion.op_Inequality(FMCore.Loaders.MavenLoaderVersion left, FMCore.Loaders.MavenLoaderVersion right) -> System.Boolean | static public
M FMCore.Loaders.MavenLoaderVersion.set_IsPrerelease(System.Boolean value) -> System.Void | instance public
M FMCore.Loaders.MavenLoaderVersion.set_MavenVersion(System.String value) -> System.Void | instance public
M FMCore.Loaders.MavenLoaderVersion.set_MinecraftVersion(System.String value) -> System.Void | instance public
M FMCore.Loaders.MavenLoaderVersion.set_Version(System.String value) -> System.Void | instance public
M FMCore.Loaders.NeoForgeInstaller..ctor(FMCore.Downloader.DownloadManager downloads, FMCore.Versions.VersionManager versions = null, System.Uri mavenUri = null) -> constructor | instance public
M FMCore.Loaders.NeoForgeInstaller..ctor(System.String rootDirectory = ".minecraft") -> constructor | instance public
M FMCore.Loaders.NeoForgeInstaller.GetVersionsAsync(System.String minecraftVersion, FMCore.Versions.VersionCachePolicy cachePolicy = Refresh, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Loaders.MavenLoaderVersion]] | instance public
M FMCore.Loaders.NeoForgeInstaller.InstallAsync(System.String minecraftVersion, System.String neoForgeVersion = null, FMCore.Loaders.LoaderInstallOptions options = null, System.IProgress`1[FMCore.Loaders.LoaderInstallationProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Loaders.LoaderInstallationResult] | instance public
M FMCore.Loaders.NeoForgeInstaller.get_OfficialMavenUri() -> System.Uri | static public
M FMCore.Loaders.NeoForgeInstaller.get_RootDirectory() -> System.String | instance public
M FMCore.Loaders.OptiFineInstaller..ctor(FMCore.Downloader.DownloadManager downloads, FMCore.Versions.VersionManager versions = null) -> constructor | instance public
M FMCore.Loaders.OptiFineInstaller..ctor(System.String rootDirectory = ".minecraft") -> constructor | instance public
M FMCore.Loaders.OptiFineInstaller.GetVersionsAsync(System.String minecraftVersion, FMCore.Versions.VersionCachePolicy cachePolicy = Refresh, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Loaders.OptiFineVersion]] | instance public
M FMCore.Loaders.OptiFineInstaller.InstallAsync(System.String minecraftVersion, System.String optiFineVersion = null, FMCore.Loaders.LoaderInstallOptions options = null, System.IProgress`1[FMCore.Loaders.LoaderInstallationProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Loaders.LoaderInstallationResult] | instance public
M FMCore.Loaders.OptiFineInstaller.InstallLocalAsync(System.String installerPath, FMCore.Loaders.LoaderInstallOptions options = null, System.IProgress`1[FMCore.Loaders.LoaderInstallationProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Loaders.LoaderInstallationResult] | instance public
M FMCore.Loaders.OptiFineInstaller.get_OfficialSiteUri() -> System.Uri | static public
M FMCore.Loaders.OptiFineInstaller.get_RootDirectory() -> System.String | instance public
M FMCore.Loaders.OptiFineVersion..ctor(System.String MinecraftVersion, System.String Version, System.String FileName, System.Boolean IsPreview, System.Uri DownloadPage) -> constructor | instance public
M FMCore.Loaders.OptiFineVersion.<Clone>$() -> FMCore.Loaders.OptiFineVersion | instance public
M FMCore.Loaders.OptiFineVersion.Deconstruct(System.String& MinecraftVersion, System.String& Version, System.String& FileName, System.Boolean& IsPreview, System.Uri& DownloadPage) -> System.Void | instance public
M FMCore.Loaders.OptiFineVersion.Equals(FMCore.Loaders.OptiFineVersion other) -> System.Boolean | instance public
M FMCore.Loaders.OptiFineVersion.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Loaders.OptiFineVersion.GetHashCode() -> System.Int32 | instance public
M FMCore.Loaders.OptiFineVersion.ToString() -> System.String | instance public
M FMCore.Loaders.OptiFineVersion.get_DownloadPage() -> System.Uri | instance public
M FMCore.Loaders.OptiFineVersion.get_FileName() -> System.String | instance public
M FMCore.Loaders.OptiFineVersion.get_IsPreview() -> System.Boolean | instance public
M FMCore.Loaders.OptiFineVersion.get_MinecraftVersion() -> System.String | instance public
M FMCore.Loaders.OptiFineVersion.get_Version() -> System.String | instance public
M FMCore.Loaders.OptiFineVersion.op_Equality(FMCore.Loaders.OptiFineVersion left, FMCore.Loaders.OptiFineVersion right) -> System.Boolean | static public
M FMCore.Loaders.OptiFineVersion.op_Inequality(FMCore.Loaders.OptiFineVersion left, FMCore.Loaders.OptiFineVersion right) -> System.Boolean | static public
M FMCore.Loaders.OptiFineVersion.set_DownloadPage(System.Uri value) -> System.Void | instance public
M FMCore.Loaders.OptiFineVersion.set_FileName(System.String value) -> System.Void | instance public
M FMCore.Loaders.OptiFineVersion.set_IsPreview(System.Boolean value) -> System.Void | instance public
M FMCore.Loaders.OptiFineVersion.set_MinecraftVersion(System.String value) -> System.Void | instance public
M FMCore.Loaders.OptiFineVersion.set_Version(System.String value) -> System.Void | instance public
P FMCore.Loaders.FabricException.Code : FMCore.Loaders.FabricErrorCode | get_Code
P FMCore.Loaders.FabricGameVersion.Stable : System.Boolean | get_Stable,set_Stable[init]
P FMCore.Loaders.FabricGameVersion.Version : System.String | get_Version,set_Version[init]
P FMCore.Loaders.FabricInstallOptions.Installation : FMCore.Installer.InstallationOptions | get_Installation,set_Installation[init]
P FMCore.Loaders.FabricInstallOptions.VersionId : System.String | get_VersionId,set_VersionId[init]
P FMCore.Loaders.FabricInstallationProgress.CompletedFiles : System.Int32 | get_CompletedFiles,set_CompletedFiles[init]
P FMCore.Loaders.FabricInstallationProgress.Download : FMCore.Downloader.DownloadProgress | get_Download,set_Download[init]
P FMCore.Loaders.FabricInstallationProgress.Installation : FMCore.Installer.InstallationProgress | get_Installation,set_Installation[init]
P FMCore.Loaders.FabricInstallationProgress.Stage : FMCore.Loaders.FabricInstallationStage | get_Stage,set_Stage[init]
P FMCore.Loaders.FabricInstallationProgress.TotalFiles : System.Int32 | get_TotalFiles,set_TotalFiles[init]
P FMCore.Loaders.FabricInstallationResult.Installation : FMCore.Installer.InstallationResult | get_Installation,set_Installation[init]
P FMCore.Loaders.FabricInstallationResult.LoaderVersion : System.String | get_LoaderVersion,set_LoaderVersion[init]
P FMCore.Loaders.FabricInstallationResult.MinecraftVersion : System.String | get_MinecraftVersion,set_MinecraftVersion[init]
P FMCore.Loaders.FabricInstallationResult.VersionId : System.String | get_VersionId,set_VersionId[init]
P FMCore.Loaders.FabricInstaller.OfficialMetaUri : System.Uri | get_OfficialMetaUri
P FMCore.Loaders.FabricInstaller.RootDirectory : System.String | get_RootDirectory
P FMCore.Loaders.FabricLoaderVersion.IntermediaryVersion : System.String | get_IntermediaryVersion,set_IntermediaryVersion[init]
P FMCore.Loaders.FabricLoaderVersion.Stable : System.Boolean | get_Stable,set_Stable[init]
P FMCore.Loaders.FabricLoaderVersion.Version : System.String | get_Version,set_Version[init]
P FMCore.Loaders.ForgeInstaller.OfficialMavenUri : System.Uri | get_OfficialMavenUri
P FMCore.Loaders.ForgeInstaller.RootDirectory : System.String | get_RootDirectory
P FMCore.Loaders.LoaderException.Code : FMCore.Loaders.LoaderErrorCode | get_Code
P FMCore.Loaders.LoaderException.ExitCode : System.Nullable`1[System.Int32] | get_ExitCode
P FMCore.Loaders.LoaderInstallOptions.Installation : FMCore.Installer.InstallationOptions | get_Installation,set_Installation[init]
P FMCore.Loaders.LoaderInstallOptions.InstallerTimeout : System.TimeSpan | get_InstallerTimeout,set_InstallerTimeout[init]
P FMCore.Loaders.LoaderInstallOptions.JavaDetection : FMCore.Java.JavaDetectionOptions | get_JavaDetection,set_JavaDetection[init]
P FMCore.Loaders.LoaderInstallOptions.JavaPath : System.String | get_JavaPath,set_JavaPath[init]
P FMCore.Loaders.LoaderInstallOptions.VersionId : System.String | get_VersionId,set_VersionId[init]
P FMCore.Loaders.LoaderInstallationProgress.CompletedFiles : System.Int32 | get_CompletedFiles,set_CompletedFiles[init]
P FMCore.Loaders.LoaderInstallationProgress.Download : FMCore.Downloader.DownloadProgress | get_Download,set_Download[init]
P FMCore.Loaders.LoaderInstallationProgress.Installation : FMCore.Installer.InstallationProgress | get_Installation,set_Installation[init]
P FMCore.Loaders.LoaderInstallationProgress.Message : System.String | get_Message,set_Message[init]
P FMCore.Loaders.LoaderInstallationProgress.Stage : FMCore.Loaders.LoaderInstallationStage | get_Stage,set_Stage[init]
P FMCore.Loaders.LoaderInstallationProgress.TotalFiles : System.Int32 | get_TotalFiles,set_TotalFiles[init]
P FMCore.Loaders.LoaderInstallationResult.Installation : FMCore.Installer.InstallationResult | get_Installation,set_Installation[init]
P FMCore.Loaders.LoaderInstallationResult.InstallerExitCode : System.Nullable`1[System.Int32] | get_InstallerExitCode,set_InstallerExitCode[init]
P FMCore.Loaders.LoaderInstallationResult.Loader : System.String | get_Loader,set_Loader[init]
P FMCore.Loaders.LoaderInstallationResult.LoaderVersion : System.String | get_LoaderVersion,set_LoaderVersion[init]
P FMCore.Loaders.LoaderInstallationResult.MinecraftVersion : System.String | get_MinecraftVersion,set_MinecraftVersion[init]
P FMCore.Loaders.LoaderInstallationResult.VersionId : System.String | get_VersionId,set_VersionId[init]
P FMCore.Loaders.MavenLoaderVersion.IsPrerelease : System.Boolean | get_IsPrerelease,set_IsPrerelease[init]
P FMCore.Loaders.MavenLoaderVersion.MavenVersion : System.String | get_MavenVersion,set_MavenVersion[init]
P FMCore.Loaders.MavenLoaderVersion.MinecraftVersion : System.String | get_MinecraftVersion,set_MinecraftVersion[init]
P FMCore.Loaders.MavenLoaderVersion.Version : System.String | get_Version,set_Version[init]
P FMCore.Loaders.NeoForgeInstaller.OfficialMavenUri : System.Uri | get_OfficialMavenUri
P FMCore.Loaders.NeoForgeInstaller.RootDirectory : System.String | get_RootDirectory
P FMCore.Loaders.OptiFineInstaller.OfficialSiteUri : System.Uri | get_OfficialSiteUri
P FMCore.Loaders.OptiFineInstaller.RootDirectory : System.String | get_RootDirectory
P FMCore.Loaders.OptiFineVersion.DownloadPage : System.Uri | get_DownloadPage,set_DownloadPage[init]
P FMCore.Loaders.OptiFineVersion.FileName : System.String | get_FileName,set_FileName[init]
P FMCore.Loaders.OptiFineVersion.IsPreview : System.Boolean | get_IsPreview,set_IsPreview[init]
P FMCore.Loaders.OptiFineVersion.MinecraftVersion : System.String | get_MinecraftVersion,set_MinecraftVersion[init]
P FMCore.Loaders.OptiFineVersion.Version : System.String | get_Version,set_Version[init]
T FMCore.Loaders.FabricErrorCode : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Loaders.FabricException : System.Exception | System.Runtime.Serialization.ISerializable
T FMCore.Loaders.FabricGameVersion : System.Object | System.IEquatable`1[FMCore.Loaders.FabricGameVersion]
T FMCore.Loaders.FabricInstallOptions : System.Object | System.IEquatable`1[FMCore.Loaders.FabricInstallOptions]
T FMCore.Loaders.FabricInstallationProgress : System.Object | System.IEquatable`1[FMCore.Loaders.FabricInstallationProgress]
T FMCore.Loaders.FabricInstallationResult : System.Object | System.IEquatable`1[FMCore.Loaders.FabricInstallationResult]
T FMCore.Loaders.FabricInstallationStage : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Loaders.FabricInstaller : System.Object |
T FMCore.Loaders.FabricLoaderVersion : System.Object | System.IEquatable`1[FMCore.Loaders.FabricLoaderVersion]
T FMCore.Loaders.ForgeInstaller : System.Object |
T FMCore.Loaders.LoaderErrorCode : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Loaders.LoaderException : System.Exception | System.Runtime.Serialization.ISerializable
T FMCore.Loaders.LoaderInstallOptions : System.Object | System.IEquatable`1[FMCore.Loaders.LoaderInstallOptions]
T FMCore.Loaders.LoaderInstallationProgress : System.Object | System.IEquatable`1[FMCore.Loaders.LoaderInstallationProgress]
T FMCore.Loaders.LoaderInstallationResult : System.Object | System.IEquatable`1[FMCore.Loaders.LoaderInstallationResult]
T FMCore.Loaders.LoaderInstallationStage : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Loaders.MavenLoaderVersion : System.Object | System.IEquatable`1[FMCore.Loaders.MavenLoaderVersion]
T FMCore.Loaders.NeoForgeInstaller : System.Object |
T FMCore.Loaders.OptiFineInstaller : System.Object |
T FMCore.Loaders.OptiFineVersion : System.Object | System.IEquatable`1[FMCore.Loaders.OptiFineVersion]
```

## Logging

```text
E FMCore.Logging.LauncherLogger.EntryReceived : System.EventHandler`1[FMCore.Logging.LogEntry]
F FMCore.Logging.DiagnosticCode.JavaIncompatible : FMCore.Logging.DiagnosticCode = 2
F FMCore.Logging.DiagnosticCode.MissingDependency : FMCore.Logging.DiagnosticCode = 3
F FMCore.Logging.DiagnosticCode.NativeLibraryError : FMCore.Logging.DiagnosticCode = 4
F FMCore.Logging.DiagnosticCode.OutOfMemory : FMCore.Logging.DiagnosticCode = 1
F FMCore.Logging.DiagnosticCode.Unknown : FMCore.Logging.DiagnosticCode = 0
F FMCore.Logging.LogLevel.Debug : FMCore.Logging.LogLevel = 0
F FMCore.Logging.LogLevel.Error : FMCore.Logging.LogLevel = 3
F FMCore.Logging.LogLevel.Information : FMCore.Logging.LogLevel = 1
F FMCore.Logging.LogLevel.Warning : FMCore.Logging.LogLevel = 2
F FMCore.Logging.LogSource.CrashReport : FMCore.Logging.LogSource = 3
F FMCore.Logging.LogSource.Launcher : FMCore.Logging.LogSource = 0
F FMCore.Logging.LogSource.StandardError : FMCore.Logging.LogSource = 2
F FMCore.Logging.LogSource.StandardOutput : FMCore.Logging.LogSource = 1
F FMCore.Logging.LoggingErrorCode.InvalidOptions : FMCore.Logging.LoggingErrorCode = 0
F FMCore.Logging.LoggingErrorCode.IoError : FMCore.Logging.LoggingErrorCode = 2
F FMCore.Logging.LoggingErrorCode.UnsafePath : FMCore.Logging.LoggingErrorCode = 1
M FMCore.Logging.CrashReportInfo..ctor(System.String RelativePath, System.DateTimeOffset LastWriteTime, System.Boolean IsTruncated, System.Boolean ProcessIdMatched) -> constructor | instance public
M FMCore.Logging.CrashReportInfo.<Clone>$() -> FMCore.Logging.CrashReportInfo | instance public
M FMCore.Logging.CrashReportInfo.Deconstruct(System.String& RelativePath, System.DateTimeOffset& LastWriteTime, System.Boolean& IsTruncated, System.Boolean& ProcessIdMatched) -> System.Void | instance public
M FMCore.Logging.CrashReportInfo.Equals(FMCore.Logging.CrashReportInfo other) -> System.Boolean | instance public
M FMCore.Logging.CrashReportInfo.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Logging.CrashReportInfo.GetHashCode() -> System.Int32 | instance public
M FMCore.Logging.CrashReportInfo.ToString() -> System.String | instance public
M FMCore.Logging.CrashReportInfo.get_IsTruncated() -> System.Boolean | instance public
M FMCore.Logging.CrashReportInfo.get_LastWriteTime() -> System.DateTimeOffset | instance public
M FMCore.Logging.CrashReportInfo.get_ProcessIdMatched() -> System.Boolean | instance public
M FMCore.Logging.CrashReportInfo.get_RelativePath() -> System.String | instance public
M FMCore.Logging.CrashReportInfo.op_Equality(FMCore.Logging.CrashReportInfo left, FMCore.Logging.CrashReportInfo right) -> System.Boolean | static public
M FMCore.Logging.CrashReportInfo.op_Inequality(FMCore.Logging.CrashReportInfo left, FMCore.Logging.CrashReportInfo right) -> System.Boolean | static public
M FMCore.Logging.CrashReportInfo.set_IsTruncated(System.Boolean value) -> System.Void | instance public
M FMCore.Logging.CrashReportInfo.set_LastWriteTime(System.DateTimeOffset value) -> System.Void | instance public
M FMCore.Logging.CrashReportInfo.set_ProcessIdMatched(System.Boolean value) -> System.Void | instance public
M FMCore.Logging.CrashReportInfo.set_RelativePath(System.String value) -> System.Void | instance public
M FMCore.Logging.DiagnosticFinding..ctor(FMCore.Logging.DiagnosticCode Code, System.String Message, System.String Recommendation, System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.LogEntry] Evidence) -> constructor | instance public
M FMCore.Logging.DiagnosticFinding.<Clone>$() -> FMCore.Logging.DiagnosticFinding | instance public
M FMCore.Logging.DiagnosticFinding.Deconstruct(FMCore.Logging.DiagnosticCode& Code, System.String& Message, System.String& Recommendation, System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.LogEntry]& Evidence) -> System.Void | instance public
M FMCore.Logging.DiagnosticFinding.Equals(FMCore.Logging.DiagnosticFinding other) -> System.Boolean | instance public
M FMCore.Logging.DiagnosticFinding.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Logging.DiagnosticFinding.GetHashCode() -> System.Int32 | instance public
M FMCore.Logging.DiagnosticFinding.ToString() -> System.String | instance public
M FMCore.Logging.DiagnosticFinding.get_Code() -> FMCore.Logging.DiagnosticCode | instance public
M FMCore.Logging.DiagnosticFinding.get_Evidence() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.LogEntry] | instance public
M FMCore.Logging.DiagnosticFinding.get_Message() -> System.String | instance public
M FMCore.Logging.DiagnosticFinding.get_Recommendation() -> System.String | instance public
M FMCore.Logging.DiagnosticFinding.op_Equality(FMCore.Logging.DiagnosticFinding left, FMCore.Logging.DiagnosticFinding right) -> System.Boolean | static public
M FMCore.Logging.DiagnosticFinding.op_Inequality(FMCore.Logging.DiagnosticFinding left, FMCore.Logging.DiagnosticFinding right) -> System.Boolean | static public
M FMCore.Logging.DiagnosticFinding.set_Code(FMCore.Logging.DiagnosticCode value) -> System.Void | instance public
M FMCore.Logging.DiagnosticFinding.set_Evidence(System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.LogEntry] value) -> System.Void | instance public
M FMCore.Logging.DiagnosticFinding.set_Message(System.String value) -> System.Void | instance public
M FMCore.Logging.DiagnosticFinding.set_Recommendation(System.String value) -> System.Void | instance public
M FMCore.Logging.GameDiagnosticReport..ctor(System.Nullable`1[System.Int32] ExitCode, System.Boolean StoppedByCaller, System.String Summary, System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.DiagnosticFinding] Findings, System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.LogEntry] Evidence) -> constructor | instance public
M FMCore.Logging.GameDiagnosticReport.<Clone>$() -> FMCore.Logging.GameDiagnosticReport | instance public
M FMCore.Logging.GameDiagnosticReport.Deconstruct(System.Nullable`1[System.Int32]& ExitCode, System.Boolean& StoppedByCaller, System.String& Summary, System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.DiagnosticFinding]& Findings, System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.LogEntry]& Evidence) -> System.Void | instance public
M FMCore.Logging.GameDiagnosticReport.Equals(FMCore.Logging.GameDiagnosticReport other) -> System.Boolean | instance public
M FMCore.Logging.GameDiagnosticReport.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Logging.GameDiagnosticReport.GetHashCode() -> System.Int32 | instance public
M FMCore.Logging.GameDiagnosticReport.ToString() -> System.String | instance public
M FMCore.Logging.GameDiagnosticReport.get_CrashReports() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.CrashReportInfo] | instance public
M FMCore.Logging.GameDiagnosticReport.get_DroppedEntries() -> System.Int64 | instance public
M FMCore.Logging.GameDiagnosticReport.get_Evidence() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.LogEntry] | instance public
M FMCore.Logging.GameDiagnosticReport.get_ExitCode() -> System.Nullable`1[System.Int32] | instance public
M FMCore.Logging.GameDiagnosticReport.get_Findings() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.DiagnosticFinding] | instance public
M FMCore.Logging.GameDiagnosticReport.get_PreparationError() -> FMCore.Launch.LaunchError | instance public
M FMCore.Logging.GameDiagnosticReport.get_StoppedByCaller() -> System.Boolean | instance public
M FMCore.Logging.GameDiagnosticReport.get_Summary() -> System.String | instance public
M FMCore.Logging.GameDiagnosticReport.get_Warnings() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Logging.GameDiagnosticReport.op_Equality(FMCore.Logging.GameDiagnosticReport left, FMCore.Logging.GameDiagnosticReport right) -> System.Boolean | static public
M FMCore.Logging.GameDiagnosticReport.op_Inequality(FMCore.Logging.GameDiagnosticReport left, FMCore.Logging.GameDiagnosticReport right) -> System.Boolean | static public
M FMCore.Logging.GameDiagnosticReport.set_CrashReports(System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.CrashReportInfo] value) -> System.Void | instance public
M FMCore.Logging.GameDiagnosticReport.set_DroppedEntries(System.Int64 value) -> System.Void | instance public
M FMCore.Logging.GameDiagnosticReport.set_Evidence(System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.LogEntry] value) -> System.Void | instance public
M FMCore.Logging.GameDiagnosticReport.set_ExitCode(System.Nullable`1[System.Int32] value) -> System.Void | instance public
M FMCore.Logging.GameDiagnosticReport.set_Findings(System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.DiagnosticFinding] value) -> System.Void | instance public
M FMCore.Logging.GameDiagnosticReport.set_PreparationError(FMCore.Launch.LaunchError value) -> System.Void | instance public
M FMCore.Logging.GameDiagnosticReport.set_StoppedByCaller(System.Boolean value) -> System.Void | instance public
M FMCore.Logging.GameDiagnosticReport.set_Summary(System.String value) -> System.Void | instance public
M FMCore.Logging.GameDiagnosticReport.set_Warnings(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Logging.GameDiagnostics.Analyze(System.Collections.Generic.IEnumerable`1[FMCore.Logging.LogEntry] entries, System.Nullable`1[System.Int32] exitCode = default, System.Boolean stoppedByCaller = False, FMCore.Launch.LaunchError launchError = null, System.Threading.CancellationToken cancellationToken = default) -> FMCore.Logging.GameDiagnosticReport | static public
M FMCore.Logging.LauncherLogger..ctor(FMCore.Logging.LoggingOptions options = null, System.Collections.Generic.IEnumerable`1[System.String] sensitiveValues = null) -> constructor | instance public
M FMCore.Logging.LauncherLogger.GetEntries() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.LogEntry] | instance public
M FMCore.Logging.LauncherLogger.SaveAsync(System.String path, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task | instance public
M FMCore.Logging.LauncherLogger.Write(FMCore.Logging.LogLevel level, System.String message, FMCore.Logging.LogSource source = Launcher) -> FMCore.Logging.LogEntry | instance public
M FMCore.Logging.LauncherLogger.add_EntryReceived(System.EventHandler`1[FMCore.Logging.LogEntry] value) -> System.Void | instance public
M FMCore.Logging.LauncherLogger.get_DroppedEntries() -> System.Int64 | instance public
M FMCore.Logging.LauncherLogger.remove_EntryReceived(System.EventHandler`1[FMCore.Logging.LogEntry] value) -> System.Void | instance public
M FMCore.Logging.LogEntry.get_FileName() -> System.String | instance public
M FMCore.Logging.LogEntry.get_IsTruncated() -> System.Boolean | instance public
M FMCore.Logging.LogEntry.get_Level() -> FMCore.Logging.LogLevel | instance public
M FMCore.Logging.LogEntry.get_Message() -> System.String | instance public
M FMCore.Logging.LogEntry.get_Source() -> FMCore.Logging.LogSource | instance public
M FMCore.Logging.LogEntry.get_Timestamp() -> System.DateTimeOffset | instance public
M FMCore.Logging.LoggingException..ctor(FMCore.Logging.LoggingErrorCode code, System.String message) -> constructor | instance public
M FMCore.Logging.LoggingException.get_Code() -> FMCore.Logging.LoggingErrorCode | instance public
M FMCore.Logging.LoggingOptions..ctor() -> constructor | instance public
M FMCore.Logging.LoggingOptions.<Clone>$() -> FMCore.Logging.LoggingOptions | instance public
M FMCore.Logging.LoggingOptions.Equals(FMCore.Logging.LoggingOptions other) -> System.Boolean | instance public
M FMCore.Logging.LoggingOptions.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Logging.LoggingOptions.GetHashCode() -> System.Int32 | instance public
M FMCore.Logging.LoggingOptions.ToString() -> System.String | instance public
M FMCore.Logging.LoggingOptions.get_CollectCrashReports() -> System.Boolean | instance public
M FMCore.Logging.LoggingOptions.get_MaxCrashReportBytes() -> System.Int32 | instance public
M FMCore.Logging.LoggingOptions.get_MaxCrashReports() -> System.Int32 | instance public
M FMCore.Logging.LoggingOptions.get_MaxEntries() -> System.Int32 | instance public
M FMCore.Logging.LoggingOptions.get_MaxReportCandidates() -> System.Int32 | instance public
M FMCore.Logging.LoggingOptions.op_Equality(FMCore.Logging.LoggingOptions left, FMCore.Logging.LoggingOptions right) -> System.Boolean | static public
M FMCore.Logging.LoggingOptions.op_Inequality(FMCore.Logging.LoggingOptions left, FMCore.Logging.LoggingOptions right) -> System.Boolean | static public
M FMCore.Logging.LoggingOptions.set_CollectCrashReports(System.Boolean value) -> System.Void | instance public
M FMCore.Logging.LoggingOptions.set_MaxCrashReportBytes(System.Int32 value) -> System.Void | instance public
M FMCore.Logging.LoggingOptions.set_MaxCrashReports(System.Int32 value) -> System.Void | instance public
M FMCore.Logging.LoggingOptions.set_MaxEntries(System.Int32 value) -> System.Void | instance public
M FMCore.Logging.LoggingOptions.set_MaxReportCandidates(System.Int32 value) -> System.Void | instance public
P FMCore.Logging.CrashReportInfo.IsTruncated : System.Boolean | get_IsTruncated,set_IsTruncated[init]
P FMCore.Logging.CrashReportInfo.LastWriteTime : System.DateTimeOffset | get_LastWriteTime,set_LastWriteTime[init]
P FMCore.Logging.CrashReportInfo.ProcessIdMatched : System.Boolean | get_ProcessIdMatched,set_ProcessIdMatched[init]
P FMCore.Logging.CrashReportInfo.RelativePath : System.String | get_RelativePath,set_RelativePath[init]
P FMCore.Logging.DiagnosticFinding.Code : FMCore.Logging.DiagnosticCode | get_Code,set_Code[init]
P FMCore.Logging.DiagnosticFinding.Evidence : System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.LogEntry] | get_Evidence,set_Evidence[init]
P FMCore.Logging.DiagnosticFinding.Message : System.String | get_Message,set_Message[init]
P FMCore.Logging.DiagnosticFinding.Recommendation : System.String | get_Recommendation,set_Recommendation[init]
P FMCore.Logging.GameDiagnosticReport.CrashReports : System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.CrashReportInfo] | get_CrashReports,set_CrashReports[init]
P FMCore.Logging.GameDiagnosticReport.DroppedEntries : System.Int64 | get_DroppedEntries,set_DroppedEntries[init]
P FMCore.Logging.GameDiagnosticReport.Evidence : System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.LogEntry] | get_Evidence,set_Evidence[init]
P FMCore.Logging.GameDiagnosticReport.ExitCode : System.Nullable`1[System.Int32] | get_ExitCode,set_ExitCode[init]
P FMCore.Logging.GameDiagnosticReport.Findings : System.Collections.Generic.IReadOnlyList`1[FMCore.Logging.DiagnosticFinding] | get_Findings,set_Findings[init]
P FMCore.Logging.GameDiagnosticReport.PreparationError : FMCore.Launch.LaunchError | get_PreparationError,set_PreparationError[init]
P FMCore.Logging.GameDiagnosticReport.StoppedByCaller : System.Boolean | get_StoppedByCaller,set_StoppedByCaller[init]
P FMCore.Logging.GameDiagnosticReport.Summary : System.String | get_Summary,set_Summary[init]
P FMCore.Logging.GameDiagnosticReport.Warnings : System.Collections.Generic.IReadOnlyList`1[System.String] | get_Warnings,set_Warnings[init]
P FMCore.Logging.LauncherLogger.DroppedEntries : System.Int64 | get_DroppedEntries
P FMCore.Logging.LogEntry.FileName : System.String | get_FileName
P FMCore.Logging.LogEntry.IsTruncated : System.Boolean | get_IsTruncated
P FMCore.Logging.LogEntry.Level : FMCore.Logging.LogLevel | get_Level
P FMCore.Logging.LogEntry.Message : System.String | get_Message
P FMCore.Logging.LogEntry.Source : FMCore.Logging.LogSource | get_Source
P FMCore.Logging.LogEntry.Timestamp : System.DateTimeOffset | get_Timestamp
P FMCore.Logging.LoggingException.Code : FMCore.Logging.LoggingErrorCode | get_Code
P FMCore.Logging.LoggingOptions.CollectCrashReports : System.Boolean | get_CollectCrashReports,set_CollectCrashReports[init]
P FMCore.Logging.LoggingOptions.MaxCrashReportBytes : System.Int32 | get_MaxCrashReportBytes,set_MaxCrashReportBytes[init]
P FMCore.Logging.LoggingOptions.MaxCrashReports : System.Int32 | get_MaxCrashReports,set_MaxCrashReports[init]
P FMCore.Logging.LoggingOptions.MaxEntries : System.Int32 | get_MaxEntries,set_MaxEntries[init]
P FMCore.Logging.LoggingOptions.MaxReportCandidates : System.Int32 | get_MaxReportCandidates,set_MaxReportCandidates[init]
T FMCore.Logging.CrashReportInfo : System.Object | System.IEquatable`1[FMCore.Logging.CrashReportInfo]
T FMCore.Logging.DiagnosticCode : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Logging.DiagnosticFinding : System.Object | System.IEquatable`1[FMCore.Logging.DiagnosticFinding]
T FMCore.Logging.GameDiagnosticReport : System.Object | System.IEquatable`1[FMCore.Logging.GameDiagnosticReport]
T FMCore.Logging.GameDiagnostics : System.Object |
T FMCore.Logging.LauncherLogger : System.Object |
T FMCore.Logging.LogEntry : System.EventArgs |
T FMCore.Logging.LogLevel : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Logging.LogSource : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Logging.LoggingErrorCode : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Logging.LoggingException : System.Exception | System.Runtime.Serialization.ISerializable
T FMCore.Logging.LoggingOptions : System.Object | System.IEquatable`1[FMCore.Logging.LoggingOptions]
```

## Modpacks

```text
F FMCore.Modpacks.ModpackChangeKind.Add : FMCore.Modpacks.ModpackChangeKind = 0
F FMCore.Modpacks.ModpackChangeKind.Conflict : FMCore.Modpacks.ModpackChangeKind = 3
F FMCore.Modpacks.ModpackChangeKind.Delete : FMCore.Modpacks.ModpackChangeKind = 1
F FMCore.Modpacks.ModpackChangeKind.Preserve : FMCore.Modpacks.ModpackChangeKind = 4
F FMCore.Modpacks.ModpackChangeKind.Replace : FMCore.Modpacks.ModpackChangeKind = 2
F FMCore.Modpacks.ModpackErrorCode.ArchiveLimitExceeded : FMCore.Modpacks.ModpackErrorCode = 2
F FMCore.Modpacks.ModpackErrorCode.Busy : FMCore.Modpacks.ModpackErrorCode = 6
F FMCore.Modpacks.ModpackErrorCode.ConfirmationRequired : FMCore.Modpacks.ModpackErrorCode = 13
F FMCore.Modpacks.ModpackErrorCode.Conflict : FMCore.Modpacks.ModpackErrorCode = 5
F FMCore.Modpacks.ModpackErrorCode.DependencyConflict : FMCore.Modpacks.ModpackErrorCode = 10
F FMCore.Modpacks.ModpackErrorCode.DownloadFailed : FMCore.Modpacks.ModpackErrorCode = 8
F FMCore.Modpacks.ModpackErrorCode.DownloadRestricted : FMCore.Modpacks.ModpackErrorCode = 9
F FMCore.Modpacks.ModpackErrorCode.InvalidArchive : FMCore.Modpacks.ModpackErrorCode = 1
F FMCore.Modpacks.ModpackErrorCode.InvalidRequest : FMCore.Modpacks.ModpackErrorCode = 0
F FMCore.Modpacks.ModpackErrorCode.IoError : FMCore.Modpacks.ModpackErrorCode = 16
F FMCore.Modpacks.ModpackErrorCode.NotFound : FMCore.Modpacks.ModpackErrorCode = 7
F FMCore.Modpacks.ModpackErrorCode.RecoveryRequired : FMCore.Modpacks.ModpackErrorCode = 11
F FMCore.Modpacks.ModpackErrorCode.RuntimeFailed : FMCore.Modpacks.ModpackErrorCode = 15
F FMCore.Modpacks.ModpackErrorCode.StorageCorrupted : FMCore.Modpacks.ModpackErrorCode = 12
F FMCore.Modpacks.ModpackErrorCode.UnsafePath : FMCore.Modpacks.ModpackErrorCode = 4
F FMCore.Modpacks.ModpackErrorCode.Unsupported : FMCore.Modpacks.ModpackErrorCode = 3
F FMCore.Modpacks.ModpackErrorCode.UpdateLocked : FMCore.Modpacks.ModpackErrorCode = 14
F FMCore.Modpacks.ModpackFormat.CurseForge : FMCore.Modpacks.ModpackFormat = 1
F FMCore.Modpacks.ModpackFormat.Modrinth : FMCore.Modpacks.ModpackFormat = 0
F FMCore.Modpacks.ModpackStage.BackingUp : FMCore.Modpacks.ModpackStage = 4
F FMCore.Modpacks.ModpackStage.Committing : FMCore.Modpacks.ModpackStage = 5
F FMCore.Modpacks.ModpackStage.Completed : FMCore.Modpacks.ModpackStage = 6
F FMCore.Modpacks.ModpackStage.Downloading : FMCore.Modpacks.ModpackStage = 3
F FMCore.Modpacks.ModpackStage.Reading : FMCore.Modpacks.ModpackStage = 0
F FMCore.Modpacks.ModpackStage.Resolving : FMCore.Modpacks.ModpackStage = 1
F FMCore.Modpacks.ModpackStage.Runtime : FMCore.Modpacks.ModpackStage = 2
M FMCore.Modpacks.InstalledModpack..ctor(System.Guid Revision, FMCore.Modpacks.ModpackInfo Pack, System.String SourceId, System.DateTimeOffset InstalledAt, System.Nullable`1[System.Guid] PendingUpdateId, System.Boolean RuntimeInstalled, System.Boolean IsModified) -> constructor | instance public
M FMCore.Modpacks.InstalledModpack.<Clone>$() -> FMCore.Modpacks.InstalledModpack | instance public
M FMCore.Modpacks.InstalledModpack.Deconstruct(System.Guid& Revision, FMCore.Modpacks.ModpackInfo& Pack, System.String& SourceId, System.DateTimeOffset& InstalledAt, System.Nullable`1[System.Guid]& PendingUpdateId, System.Boolean& RuntimeInstalled, System.Boolean& IsModified) -> System.Void | instance public
M FMCore.Modpacks.InstalledModpack.Equals(FMCore.Modpacks.InstalledModpack other) -> System.Boolean | instance public
M FMCore.Modpacks.InstalledModpack.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Modpacks.InstalledModpack.GetHashCode() -> System.Int32 | instance public
M FMCore.Modpacks.InstalledModpack.ToString() -> System.String | instance public
M FMCore.Modpacks.InstalledModpack.get_InstalledAt() -> System.DateTimeOffset | instance public
M FMCore.Modpacks.InstalledModpack.get_IsModified() -> System.Boolean | instance public
M FMCore.Modpacks.InstalledModpack.get_Pack() -> FMCore.Modpacks.ModpackInfo | instance public
M FMCore.Modpacks.InstalledModpack.get_PendingUpdateId() -> System.Nullable`1[System.Guid] | instance public
M FMCore.Modpacks.InstalledModpack.get_Revision() -> System.Guid | instance public
M FMCore.Modpacks.InstalledModpack.get_RuntimeInstalled() -> System.Boolean | instance public
M FMCore.Modpacks.InstalledModpack.get_SourceId() -> System.String | instance public
M FMCore.Modpacks.InstalledModpack.op_Equality(FMCore.Modpacks.InstalledModpack left, FMCore.Modpacks.InstalledModpack right) -> System.Boolean | static public
M FMCore.Modpacks.InstalledModpack.op_Inequality(FMCore.Modpacks.InstalledModpack left, FMCore.Modpacks.InstalledModpack right) -> System.Boolean | static public
M FMCore.Modpacks.InstalledModpack.set_InstalledAt(System.DateTimeOffset value) -> System.Void | instance public
M FMCore.Modpacks.InstalledModpack.set_IsModified(System.Boolean value) -> System.Void | instance public
M FMCore.Modpacks.InstalledModpack.set_Pack(FMCore.Modpacks.ModpackInfo value) -> System.Void | instance public
M FMCore.Modpacks.InstalledModpack.set_PendingUpdateId(System.Nullable`1[System.Guid] value) -> System.Void | instance public
M FMCore.Modpacks.InstalledModpack.set_Revision(System.Guid value) -> System.Void | instance public
M FMCore.Modpacks.InstalledModpack.set_RuntimeInstalled(System.Boolean value) -> System.Void | instance public
M FMCore.Modpacks.InstalledModpack.set_SourceId(System.String value) -> System.Void | instance public
M FMCore.Modpacks.ModpackArchive.ReadAsync(System.String archivePath, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Modpacks.ModpackInfo] | static public
M FMCore.Modpacks.ModpackChange..ctor(System.String Path, FMCore.Modpacks.ModpackChangeKind Kind, System.String Reason) -> constructor | instance public
M FMCore.Modpacks.ModpackChange.<Clone>$() -> FMCore.Modpacks.ModpackChange | instance public
M FMCore.Modpacks.ModpackChange.Deconstruct(System.String& Path, FMCore.Modpacks.ModpackChangeKind& Kind, System.String& Reason) -> System.Void | instance public
M FMCore.Modpacks.ModpackChange.Equals(FMCore.Modpacks.ModpackChange other) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackChange.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackChange.GetHashCode() -> System.Int32 | instance public
M FMCore.Modpacks.ModpackChange.ToString() -> System.String | instance public
M FMCore.Modpacks.ModpackChange.get_Kind() -> FMCore.Modpacks.ModpackChangeKind | instance public
M FMCore.Modpacks.ModpackChange.get_Path() -> System.String | instance public
M FMCore.Modpacks.ModpackChange.get_Reason() -> System.String | instance public
M FMCore.Modpacks.ModpackChange.op_Equality(FMCore.Modpacks.ModpackChange left, FMCore.Modpacks.ModpackChange right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackChange.op_Inequality(FMCore.Modpacks.ModpackChange left, FMCore.Modpacks.ModpackChange right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackChange.set_Kind(FMCore.Modpacks.ModpackChangeKind value) -> System.Void | instance public
M FMCore.Modpacks.ModpackChange.set_Path(System.String value) -> System.Void | instance public
M FMCore.Modpacks.ModpackChange.set_Reason(System.String value) -> System.Void | instance public
M FMCore.Modpacks.ModpackCleanupResult..ctor(System.Guid UpdateId, System.Int32 DeletedFiles, System.Collections.Generic.IReadOnlyList`1[System.String] FailedPaths) -> constructor | instance public
M FMCore.Modpacks.ModpackCleanupResult.<Clone>$() -> FMCore.Modpacks.ModpackCleanupResult | instance public
M FMCore.Modpacks.ModpackCleanupResult.Deconstruct(System.Guid& UpdateId, System.Int32& DeletedFiles, System.Collections.Generic.IReadOnlyList`1[System.String]& FailedPaths) -> System.Void | instance public
M FMCore.Modpacks.ModpackCleanupResult.Equals(FMCore.Modpacks.ModpackCleanupResult other) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackCleanupResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackCleanupResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Modpacks.ModpackCleanupResult.ToString() -> System.String | instance public
M FMCore.Modpacks.ModpackCleanupResult.get_DeletedFiles() -> System.Int32 | instance public
M FMCore.Modpacks.ModpackCleanupResult.get_FailedPaths() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Modpacks.ModpackCleanupResult.get_UpdateId() -> System.Guid | instance public
M FMCore.Modpacks.ModpackCleanupResult.op_Equality(FMCore.Modpacks.ModpackCleanupResult left, FMCore.Modpacks.ModpackCleanupResult right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackCleanupResult.op_Inequality(FMCore.Modpacks.ModpackCleanupResult left, FMCore.Modpacks.ModpackCleanupResult right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackCleanupResult.set_DeletedFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Modpacks.ModpackCleanupResult.set_FailedPaths(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Modpacks.ModpackCleanupResult.set_UpdateId(System.Guid value) -> System.Void | instance public
M FMCore.Modpacks.ModpackException..ctor(FMCore.Modpacks.ModpackErrorCode code, System.String message) -> constructor | instance public
M FMCore.Modpacks.ModpackException.get_Code() -> FMCore.Modpacks.ModpackErrorCode | instance public
M FMCore.Modpacks.ModpackException.get_DownloadError() -> System.Nullable`1[FMCore.Downloader.DownloadErrorCode] | instance public
M FMCore.Modpacks.ModpackException.get_ResourceError() -> System.Nullable`1[FMCore.Resources.ResourceErrorCode] | instance public
M FMCore.Modpacks.ModpackException.set_DownloadError(System.Nullable`1[FMCore.Downloader.DownloadErrorCode] value) -> System.Void | instance public
M FMCore.Modpacks.ModpackException.set_ResourceError(System.Nullable`1[FMCore.Resources.ResourceErrorCode] value) -> System.Void | instance public
M FMCore.Modpacks.ModpackExportOptions..ctor() -> constructor | instance public
M FMCore.Modpacks.ModpackExportOptions.<Clone>$() -> FMCore.Modpacks.ModpackExportOptions | instance public
M FMCore.Modpacks.ModpackExportOptions.Equals(FMCore.Modpacks.ModpackExportOptions other) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackExportOptions.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackExportOptions.GetHashCode() -> System.Int32 | instance public
M FMCore.Modpacks.ModpackExportOptions.ToString() -> System.String | instance public
M FMCore.Modpacks.ModpackExportOptions.get_AllowBinaryOverrides() -> System.Boolean | instance public
M FMCore.Modpacks.ModpackExportOptions.get_IncludeOverrides() -> System.Boolean | instance public
M FMCore.Modpacks.ModpackExportOptions.op_Equality(FMCore.Modpacks.ModpackExportOptions left, FMCore.Modpacks.ModpackExportOptions right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackExportOptions.op_Inequality(FMCore.Modpacks.ModpackExportOptions left, FMCore.Modpacks.ModpackExportOptions right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackExportOptions.set_AllowBinaryOverrides(System.Boolean value) -> System.Void | instance public
M FMCore.Modpacks.ModpackExportOptions.set_IncludeOverrides(System.Boolean value) -> System.Void | instance public
M FMCore.Modpacks.ModpackFile..ctor(System.String Path, System.Int64 Size, System.String Sha512, System.String Sha1, System.Collections.Generic.IReadOnlyList`1[System.Uri] Downloads, System.Boolean Optional = False, System.Nullable`1[System.Int32] ProjectId = default, System.Nullable`1[System.Int32] FileId = default, System.Boolean IsOverride = False) -> constructor | instance public
M FMCore.Modpacks.ModpackFile.<Clone>$() -> FMCore.Modpacks.ModpackFile | instance public
M FMCore.Modpacks.ModpackFile.Deconstruct(System.String& Path, System.Int64& Size, System.String& Sha512, System.String& Sha1, System.Collections.Generic.IReadOnlyList`1[System.Uri]& Downloads, System.Boolean& Optional, System.Nullable`1[System.Int32]& ProjectId, System.Nullable`1[System.Int32]& FileId, System.Boolean& IsOverride) -> System.Void | instance public
M FMCore.Modpacks.ModpackFile.Equals(FMCore.Modpacks.ModpackFile other) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackFile.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackFile.GetHashCode() -> System.Int32 | instance public
M FMCore.Modpacks.ModpackFile.ToString() -> System.String | instance public
M FMCore.Modpacks.ModpackFile.get_Downloads() -> System.Collections.Generic.IReadOnlyList`1[System.Uri] | instance public
M FMCore.Modpacks.ModpackFile.get_FileId() -> System.Nullable`1[System.Int32] | instance public
M FMCore.Modpacks.ModpackFile.get_IsOverride() -> System.Boolean | instance public
M FMCore.Modpacks.ModpackFile.get_Optional() -> System.Boolean | instance public
M FMCore.Modpacks.ModpackFile.get_Path() -> System.String | instance public
M FMCore.Modpacks.ModpackFile.get_ProjectId() -> System.Nullable`1[System.Int32] | instance public
M FMCore.Modpacks.ModpackFile.get_Sha1() -> System.String | instance public
M FMCore.Modpacks.ModpackFile.get_Sha512() -> System.String | instance public
M FMCore.Modpacks.ModpackFile.get_Size() -> System.Int64 | instance public
M FMCore.Modpacks.ModpackFile.op_Equality(FMCore.Modpacks.ModpackFile left, FMCore.Modpacks.ModpackFile right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackFile.op_Inequality(FMCore.Modpacks.ModpackFile left, FMCore.Modpacks.ModpackFile right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackFile.set_Downloads(System.Collections.Generic.IReadOnlyList`1[System.Uri] value) -> System.Void | instance public
M FMCore.Modpacks.ModpackFile.set_FileId(System.Nullable`1[System.Int32] value) -> System.Void | instance public
M FMCore.Modpacks.ModpackFile.set_IsOverride(System.Boolean value) -> System.Void | instance public
M FMCore.Modpacks.ModpackFile.set_Optional(System.Boolean value) -> System.Void | instance public
M FMCore.Modpacks.ModpackFile.set_Path(System.String value) -> System.Void | instance public
M FMCore.Modpacks.ModpackFile.set_ProjectId(System.Nullable`1[System.Int32] value) -> System.Void | instance public
M FMCore.Modpacks.ModpackFile.set_Sha1(System.String value) -> System.Void | instance public
M FMCore.Modpacks.ModpackFile.set_Sha512(System.String value) -> System.Void | instance public
M FMCore.Modpacks.ModpackFile.set_Size(System.Int64 value) -> System.Void | instance public
M FMCore.Modpacks.ModpackInfo..ctor(FMCore.Modpacks.ModpackFormat Format, System.String Name, System.String Version, System.String MinecraftVersion, System.String Loader, System.String LoaderVersion, System.Collections.Generic.IReadOnlyList`1[FMCore.Modpacks.ModpackFile] Files) -> constructor | instance public
M FMCore.Modpacks.ModpackInfo.<Clone>$() -> FMCore.Modpacks.ModpackInfo | instance public
M FMCore.Modpacks.ModpackInfo.Deconstruct(FMCore.Modpacks.ModpackFormat& Format, System.String& Name, System.String& Version, System.String& MinecraftVersion, System.String& Loader, System.String& LoaderVersion, System.Collections.Generic.IReadOnlyList`1[FMCore.Modpacks.ModpackFile]& Files) -> System.Void | instance public
M FMCore.Modpacks.ModpackInfo.Equals(FMCore.Modpacks.ModpackInfo other) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackInfo.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackInfo.GetHashCode() -> System.Int32 | instance public
M FMCore.Modpacks.ModpackInfo.ToString() -> System.String | instance public
M FMCore.Modpacks.ModpackInfo.get_CompatibilityWarnings() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Modpacks.ModpackInfo.get_Files() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Modpacks.ModpackFile] | instance public
M FMCore.Modpacks.ModpackInfo.get_Format() -> FMCore.Modpacks.ModpackFormat | instance public
M FMCore.Modpacks.ModpackInfo.get_Loader() -> System.String | instance public
M FMCore.Modpacks.ModpackInfo.get_LoaderVersion() -> System.String | instance public
M FMCore.Modpacks.ModpackInfo.get_MinecraftVersion() -> System.String | instance public
M FMCore.Modpacks.ModpackInfo.get_Name() -> System.String | instance public
M FMCore.Modpacks.ModpackInfo.get_Version() -> System.String | instance public
M FMCore.Modpacks.ModpackInfo.op_Equality(FMCore.Modpacks.ModpackInfo left, FMCore.Modpacks.ModpackInfo right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackInfo.op_Inequality(FMCore.Modpacks.ModpackInfo left, FMCore.Modpacks.ModpackInfo right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackInfo.set_CompatibilityWarnings(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Modpacks.ModpackInfo.set_Files(System.Collections.Generic.IReadOnlyList`1[FMCore.Modpacks.ModpackFile] value) -> System.Void | instance public
M FMCore.Modpacks.ModpackInfo.set_Format(FMCore.Modpacks.ModpackFormat value) -> System.Void | instance public
M FMCore.Modpacks.ModpackInfo.set_Loader(System.String value) -> System.Void | instance public
M FMCore.Modpacks.ModpackInfo.set_LoaderVersion(System.String value) -> System.Void | instance public
M FMCore.Modpacks.ModpackInfo.set_MinecraftVersion(System.String value) -> System.Void | instance public
M FMCore.Modpacks.ModpackInfo.set_Name(System.String value) -> System.Void | instance public
M FMCore.Modpacks.ModpackInfo.set_Version(System.String value) -> System.Void | instance public
M FMCore.Modpacks.ModpackInstallResult..ctor(FMCore.Modpacks.InstalledModpack Installed, System.Guid UpdateId, System.Boolean Reused) -> constructor | instance public
M FMCore.Modpacks.ModpackInstallResult.<Clone>$() -> FMCore.Modpacks.ModpackInstallResult | instance public
M FMCore.Modpacks.ModpackInstallResult.Deconstruct(FMCore.Modpacks.InstalledModpack& Installed, System.Guid& UpdateId, System.Boolean& Reused) -> System.Void | instance public
M FMCore.Modpacks.ModpackInstallResult.Equals(FMCore.Modpacks.ModpackInstallResult other) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackInstallResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackInstallResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Modpacks.ModpackInstallResult.ToString() -> System.String | instance public
M FMCore.Modpacks.ModpackInstallResult.get_Installed() -> FMCore.Modpacks.InstalledModpack | instance public
M FMCore.Modpacks.ModpackInstallResult.get_Reused() -> System.Boolean | instance public
M FMCore.Modpacks.ModpackInstallResult.get_UpdateId() -> System.Guid | instance public
M FMCore.Modpacks.ModpackInstallResult.op_Equality(FMCore.Modpacks.ModpackInstallResult left, FMCore.Modpacks.ModpackInstallResult right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackInstallResult.op_Inequality(FMCore.Modpacks.ModpackInstallResult left, FMCore.Modpacks.ModpackInstallResult right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackInstallResult.set_Installed(FMCore.Modpacks.InstalledModpack value) -> System.Void | instance public
M FMCore.Modpacks.ModpackInstallResult.set_Reused(System.Boolean value) -> System.Void | instance public
M FMCore.Modpacks.ModpackInstallResult.set_UpdateId(System.Guid value) -> System.Void | instance public
M FMCore.Modpacks.ModpackInstaller..ctor(System.String rootDirectory, System.String instanceId, FMCore.Modpacks.ModpackOptions options = null) -> constructor | instance public
M FMCore.Modpacks.ModpackInstaller.CheckModUpdatesAsync(System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Modpacks.ModpackModUpdate]] | instance public
M FMCore.Modpacks.ModpackInstaller.CleanupAsync(System.Guid updateId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Modpacks.ModpackCleanupResult] | instance public
M FMCore.Modpacks.ModpackInstaller.ConfirmUpdateAsync(System.Guid updateId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Modpacks.ModpackCleanupResult] | instance public
M FMCore.Modpacks.ModpackInstaller.CreateValidationLauncherAsync(FMCore.Authentication.LaunchIdentity account, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Launch.MinecraftLauncher] | instance public
M FMCore.Modpacks.ModpackInstaller.ExportAsync(System.String destination, FMCore.Modpacks.ModpackExportOptions exportOptions = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task | instance public
M FMCore.Modpacks.ModpackInstaller.GetInstalledAsync(System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Modpacks.InstalledModpack] | instance public
M FMCore.Modpacks.ModpackInstaller.GetUpdatePlanAsync(System.String archivePath, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Modpacks.ModpackUpdatePlan] | instance public
M FMCore.Modpacks.ModpackInstaller.ImportAsync(System.String archivePath, System.IProgress`1[FMCore.Modpacks.ModpackProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Modpacks.ModpackInstallResult] | instance public
M FMCore.Modpacks.ModpackInstaller.InstallAsync(System.String archivePath, System.IProgress`1[FMCore.Modpacks.ModpackProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Modpacks.ModpackInstallResult] | instance public
M FMCore.Modpacks.ModpackInstaller.RollbackAsync(System.Guid updateId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task | instance public
M FMCore.Modpacks.ModpackInstaller.UpdateAsync(System.String archivePath, System.IProgress`1[FMCore.Modpacks.ModpackProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Modpacks.ModpackInstallResult] | instance public
M FMCore.Modpacks.ModpackInstaller.UpdateModAsync(System.String projectId, System.Boolean authorize = False, System.String source = "modrinth", System.IProgress`1[FMCore.Modpacks.ModpackProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Modpacks.ModpackInstallResult] | instance public
M FMCore.Modpacks.ModpackInstaller.get_GameDirectory() -> System.String | instance public
M FMCore.Modpacks.ModpackModUpdate..ctor(FMCore.Resources.InstalledMod Installed, System.String AvailableVersionId, System.String VersionNumber, System.Boolean IsLocked = True) -> constructor | instance public
M FMCore.Modpacks.ModpackModUpdate.<Clone>$() -> FMCore.Modpacks.ModpackModUpdate | instance public
M FMCore.Modpacks.ModpackModUpdate.Deconstruct(FMCore.Resources.InstalledMod& Installed, System.String& AvailableVersionId, System.String& VersionNumber, System.Boolean& IsLocked) -> System.Void | instance public
M FMCore.Modpacks.ModpackModUpdate.Equals(FMCore.Modpacks.ModpackModUpdate other) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackModUpdate.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackModUpdate.GetHashCode() -> System.Int32 | instance public
M FMCore.Modpacks.ModpackModUpdate.ToString() -> System.String | instance public
M FMCore.Modpacks.ModpackModUpdate.get_AvailableVersionId() -> System.String | instance public
M FMCore.Modpacks.ModpackModUpdate.get_Installed() -> FMCore.Resources.InstalledMod | instance public
M FMCore.Modpacks.ModpackModUpdate.get_IsLocked() -> System.Boolean | instance public
M FMCore.Modpacks.ModpackModUpdate.get_VersionNumber() -> System.String | instance public
M FMCore.Modpacks.ModpackModUpdate.op_Equality(FMCore.Modpacks.ModpackModUpdate left, FMCore.Modpacks.ModpackModUpdate right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackModUpdate.op_Inequality(FMCore.Modpacks.ModpackModUpdate left, FMCore.Modpacks.ModpackModUpdate right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackModUpdate.set_AvailableVersionId(System.String value) -> System.Void | instance public
M FMCore.Modpacks.ModpackModUpdate.set_Installed(FMCore.Resources.InstalledMod value) -> System.Void | instance public
M FMCore.Modpacks.ModpackModUpdate.set_IsLocked(System.Boolean value) -> System.Void | instance public
M FMCore.Modpacks.ModpackModUpdate.set_VersionNumber(System.String value) -> System.Void | instance public
M FMCore.Modpacks.ModpackOptions..ctor() -> constructor | instance public
M FMCore.Modpacks.ModpackOptions.<Clone>$() -> FMCore.Modpacks.ModpackOptions | instance public
M FMCore.Modpacks.ModpackOptions.Equals(FMCore.Modpacks.ModpackOptions other) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackOptions.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackOptions.GetHashCode() -> System.Int32 | instance public
M FMCore.Modpacks.ModpackOptions.ToString() -> System.String | instance public
M FMCore.Modpacks.ModpackOptions.get_AutoCleanupAfterConfirmation() -> System.Boolean | instance public
M FMCore.Modpacks.ModpackOptions.get_CurseForge() -> FMCore.Resources.CurseForgeClient | instance public
M FMCore.Modpacks.ModpackOptions.get_DownloadClient() -> System.Net.Http.HttpClient | instance public
M FMCore.Modpacks.ModpackOptions.get_Downloads() -> FMCore.Downloader.DownloadOptions | instance public
M FMCore.Modpacks.ModpackOptions.get_IncludeOptionalFiles() -> System.Boolean | instance public
M FMCore.Modpacks.ModpackOptions.get_InstallRuntime() -> System.Boolean | instance public
M FMCore.Modpacks.ModpackOptions.get_JavaPath() -> System.String | instance public
M FMCore.Modpacks.ModpackOptions.get_Modrinth() -> FMCore.Resources.ModrinthClient | instance public
M FMCore.Modpacks.ModpackOptions.get_PreserveModifiedConfigurations() -> System.Boolean | instance public
M FMCore.Modpacks.ModpackOptions.get_RequireDeclaredModCompatibility() -> System.Boolean | instance public
M FMCore.Modpacks.ModpackOptions.get_SourceId() -> System.String | instance public
M FMCore.Modpacks.ModpackOptions.op_Equality(FMCore.Modpacks.ModpackOptions left, FMCore.Modpacks.ModpackOptions right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackOptions.op_Inequality(FMCore.Modpacks.ModpackOptions left, FMCore.Modpacks.ModpackOptions right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackOptions.set_AutoCleanupAfterConfirmation(System.Boolean value) -> System.Void | instance public
M FMCore.Modpacks.ModpackOptions.set_CurseForge(FMCore.Resources.CurseForgeClient value) -> System.Void | instance public
M FMCore.Modpacks.ModpackOptions.set_DownloadClient(System.Net.Http.HttpClient value) -> System.Void | instance public
M FMCore.Modpacks.ModpackOptions.set_Downloads(FMCore.Downloader.DownloadOptions value) -> System.Void | instance public
M FMCore.Modpacks.ModpackOptions.set_IncludeOptionalFiles(System.Boolean value) -> System.Void | instance public
M FMCore.Modpacks.ModpackOptions.set_InstallRuntime(System.Boolean value) -> System.Void | instance public
M FMCore.Modpacks.ModpackOptions.set_JavaPath(System.String value) -> System.Void | instance public
M FMCore.Modpacks.ModpackOptions.set_Modrinth(FMCore.Resources.ModrinthClient value) -> System.Void | instance public
M FMCore.Modpacks.ModpackOptions.set_PreserveModifiedConfigurations(System.Boolean value) -> System.Void | instance public
M FMCore.Modpacks.ModpackOptions.set_RequireDeclaredModCompatibility(System.Boolean value) -> System.Void | instance public
M FMCore.Modpacks.ModpackOptions.set_SourceId(System.String value) -> System.Void | instance public
M FMCore.Modpacks.ModpackProgress..ctor(FMCore.Modpacks.ModpackStage Stage, System.Int32 CompletedFiles = 0, System.Int32 TotalFiles = 0, FMCore.Downloader.DownloadProgress Download = null, FMCore.Installer.InstallationProgress Installation = null) -> constructor | instance public
M FMCore.Modpacks.ModpackProgress.<Clone>$() -> FMCore.Modpacks.ModpackProgress | instance public
M FMCore.Modpacks.ModpackProgress.Deconstruct(FMCore.Modpacks.ModpackStage& Stage, System.Int32& CompletedFiles, System.Int32& TotalFiles, FMCore.Downloader.DownloadProgress& Download, FMCore.Installer.InstallationProgress& Installation) -> System.Void | instance public
M FMCore.Modpacks.ModpackProgress.Equals(FMCore.Modpacks.ModpackProgress other) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackProgress.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackProgress.GetHashCode() -> System.Int32 | instance public
M FMCore.Modpacks.ModpackProgress.ToString() -> System.String | instance public
M FMCore.Modpacks.ModpackProgress.get_CompletedFiles() -> System.Int32 | instance public
M FMCore.Modpacks.ModpackProgress.get_Download() -> FMCore.Downloader.DownloadProgress | instance public
M FMCore.Modpacks.ModpackProgress.get_Installation() -> FMCore.Installer.InstallationProgress | instance public
M FMCore.Modpacks.ModpackProgress.get_Stage() -> FMCore.Modpacks.ModpackStage | instance public
M FMCore.Modpacks.ModpackProgress.get_TotalFiles() -> System.Int32 | instance public
M FMCore.Modpacks.ModpackProgress.op_Equality(FMCore.Modpacks.ModpackProgress left, FMCore.Modpacks.ModpackProgress right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackProgress.op_Inequality(FMCore.Modpacks.ModpackProgress left, FMCore.Modpacks.ModpackProgress right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackProgress.set_CompletedFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Modpacks.ModpackProgress.set_Download(FMCore.Downloader.DownloadProgress value) -> System.Void | instance public
M FMCore.Modpacks.ModpackProgress.set_Installation(FMCore.Installer.InstallationProgress value) -> System.Void | instance public
M FMCore.Modpacks.ModpackProgress.set_Stage(FMCore.Modpacks.ModpackStage value) -> System.Void | instance public
M FMCore.Modpacks.ModpackProgress.set_TotalFiles(System.Int32 value) -> System.Void | instance public
M FMCore.Modpacks.ModpackUpdatePlan..ctor(System.Nullable`1[System.Guid] CurrentRevision, FMCore.Modpacks.ModpackInfo Target, System.Collections.Generic.IReadOnlyList`1[FMCore.Modpacks.ModpackChange] Changes) -> constructor | instance public
M FMCore.Modpacks.ModpackUpdatePlan.<Clone>$() -> FMCore.Modpacks.ModpackUpdatePlan | instance public
M FMCore.Modpacks.ModpackUpdatePlan.Deconstruct(System.Nullable`1[System.Guid]& CurrentRevision, FMCore.Modpacks.ModpackInfo& Target, System.Collections.Generic.IReadOnlyList`1[FMCore.Modpacks.ModpackChange]& Changes) -> System.Void | instance public
M FMCore.Modpacks.ModpackUpdatePlan.Equals(FMCore.Modpacks.ModpackUpdatePlan other) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackUpdatePlan.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Modpacks.ModpackUpdatePlan.GetHashCode() -> System.Int32 | instance public
M FMCore.Modpacks.ModpackUpdatePlan.ToString() -> System.String | instance public
M FMCore.Modpacks.ModpackUpdatePlan.get_CanApply() -> System.Boolean | instance public
M FMCore.Modpacks.ModpackUpdatePlan.get_Changes() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Modpacks.ModpackChange] | instance public
M FMCore.Modpacks.ModpackUpdatePlan.get_CurrentRevision() -> System.Nullable`1[System.Guid] | instance public
M FMCore.Modpacks.ModpackUpdatePlan.get_Target() -> FMCore.Modpacks.ModpackInfo | instance public
M FMCore.Modpacks.ModpackUpdatePlan.op_Equality(FMCore.Modpacks.ModpackUpdatePlan left, FMCore.Modpacks.ModpackUpdatePlan right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackUpdatePlan.op_Inequality(FMCore.Modpacks.ModpackUpdatePlan left, FMCore.Modpacks.ModpackUpdatePlan right) -> System.Boolean | static public
M FMCore.Modpacks.ModpackUpdatePlan.set_Changes(System.Collections.Generic.IReadOnlyList`1[FMCore.Modpacks.ModpackChange] value) -> System.Void | instance public
M FMCore.Modpacks.ModpackUpdatePlan.set_CurrentRevision(System.Nullable`1[System.Guid] value) -> System.Void | instance public
M FMCore.Modpacks.ModpackUpdatePlan.set_Target(FMCore.Modpacks.ModpackInfo value) -> System.Void | instance public
P FMCore.Modpacks.InstalledModpack.InstalledAt : System.DateTimeOffset | get_InstalledAt,set_InstalledAt[init]
P FMCore.Modpacks.InstalledModpack.IsModified : System.Boolean | get_IsModified,set_IsModified[init]
P FMCore.Modpacks.InstalledModpack.Pack : FMCore.Modpacks.ModpackInfo | get_Pack,set_Pack[init]
P FMCore.Modpacks.InstalledModpack.PendingUpdateId : System.Nullable`1[System.Guid] | get_PendingUpdateId,set_PendingUpdateId[init]
P FMCore.Modpacks.InstalledModpack.Revision : System.Guid | get_Revision,set_Revision[init]
P FMCore.Modpacks.InstalledModpack.RuntimeInstalled : System.Boolean | get_RuntimeInstalled,set_RuntimeInstalled[init]
P FMCore.Modpacks.InstalledModpack.SourceId : System.String | get_SourceId,set_SourceId[init]
P FMCore.Modpacks.ModpackChange.Kind : FMCore.Modpacks.ModpackChangeKind | get_Kind,set_Kind[init]
P FMCore.Modpacks.ModpackChange.Path : System.String | get_Path,set_Path[init]
P FMCore.Modpacks.ModpackChange.Reason : System.String | get_Reason,set_Reason[init]
P FMCore.Modpacks.ModpackCleanupResult.DeletedFiles : System.Int32 | get_DeletedFiles,set_DeletedFiles[init]
P FMCore.Modpacks.ModpackCleanupResult.FailedPaths : System.Collections.Generic.IReadOnlyList`1[System.String] | get_FailedPaths,set_FailedPaths[init]
P FMCore.Modpacks.ModpackCleanupResult.UpdateId : System.Guid | get_UpdateId,set_UpdateId[init]
P FMCore.Modpacks.ModpackException.Code : FMCore.Modpacks.ModpackErrorCode | get_Code
P FMCore.Modpacks.ModpackException.DownloadError : System.Nullable`1[FMCore.Downloader.DownloadErrorCode] | get_DownloadError,set_DownloadError[init]
P FMCore.Modpacks.ModpackException.ResourceError : System.Nullable`1[FMCore.Resources.ResourceErrorCode] | get_ResourceError,set_ResourceError[init]
P FMCore.Modpacks.ModpackExportOptions.AllowBinaryOverrides : System.Boolean | get_AllowBinaryOverrides,set_AllowBinaryOverrides[init]
P FMCore.Modpacks.ModpackExportOptions.IncludeOverrides : System.Boolean | get_IncludeOverrides,set_IncludeOverrides[init]
P FMCore.Modpacks.ModpackFile.Downloads : System.Collections.Generic.IReadOnlyList`1[System.Uri] | get_Downloads,set_Downloads[init]
P FMCore.Modpacks.ModpackFile.FileId : System.Nullable`1[System.Int32] | get_FileId,set_FileId[init]
P FMCore.Modpacks.ModpackFile.IsOverride : System.Boolean | get_IsOverride,set_IsOverride[init]
P FMCore.Modpacks.ModpackFile.Optional : System.Boolean | get_Optional,set_Optional[init]
P FMCore.Modpacks.ModpackFile.Path : System.String | get_Path,set_Path[init]
P FMCore.Modpacks.ModpackFile.ProjectId : System.Nullable`1[System.Int32] | get_ProjectId,set_ProjectId[init]
P FMCore.Modpacks.ModpackFile.Sha1 : System.String | get_Sha1,set_Sha1[init]
P FMCore.Modpacks.ModpackFile.Sha512 : System.String | get_Sha512,set_Sha512[init]
P FMCore.Modpacks.ModpackFile.Size : System.Int64 | get_Size,set_Size[init]
P FMCore.Modpacks.ModpackInfo.CompatibilityWarnings : System.Collections.Generic.IReadOnlyList`1[System.String] | get_CompatibilityWarnings,set_CompatibilityWarnings[init]
P FMCore.Modpacks.ModpackInfo.Files : System.Collections.Generic.IReadOnlyList`1[FMCore.Modpacks.ModpackFile] | get_Files,set_Files[init]
P FMCore.Modpacks.ModpackInfo.Format : FMCore.Modpacks.ModpackFormat | get_Format,set_Format[init]
P FMCore.Modpacks.ModpackInfo.Loader : System.String | get_Loader,set_Loader[init]
P FMCore.Modpacks.ModpackInfo.LoaderVersion : System.String | get_LoaderVersion,set_LoaderVersion[init]
P FMCore.Modpacks.ModpackInfo.MinecraftVersion : System.String | get_MinecraftVersion,set_MinecraftVersion[init]
P FMCore.Modpacks.ModpackInfo.Name : System.String | get_Name,set_Name[init]
P FMCore.Modpacks.ModpackInfo.Version : System.String | get_Version,set_Version[init]
P FMCore.Modpacks.ModpackInstallResult.Installed : FMCore.Modpacks.InstalledModpack | get_Installed,set_Installed[init]
P FMCore.Modpacks.ModpackInstallResult.Reused : System.Boolean | get_Reused,set_Reused[init]
P FMCore.Modpacks.ModpackInstallResult.UpdateId : System.Guid | get_UpdateId,set_UpdateId[init]
P FMCore.Modpacks.ModpackInstaller.GameDirectory : System.String | get_GameDirectory
P FMCore.Modpacks.ModpackModUpdate.AvailableVersionId : System.String | get_AvailableVersionId,set_AvailableVersionId[init]
P FMCore.Modpacks.ModpackModUpdate.Installed : FMCore.Resources.InstalledMod | get_Installed,set_Installed[init]
P FMCore.Modpacks.ModpackModUpdate.IsLocked : System.Boolean | get_IsLocked,set_IsLocked[init]
P FMCore.Modpacks.ModpackModUpdate.VersionNumber : System.String | get_VersionNumber,set_VersionNumber[init]
P FMCore.Modpacks.ModpackOptions.AutoCleanupAfterConfirmation : System.Boolean | get_AutoCleanupAfterConfirmation,set_AutoCleanupAfterConfirmation[init]
P FMCore.Modpacks.ModpackOptions.CurseForge : FMCore.Resources.CurseForgeClient | get_CurseForge,set_CurseForge[init]
P FMCore.Modpacks.ModpackOptions.DownloadClient : System.Net.Http.HttpClient | get_DownloadClient,set_DownloadClient[init]
P FMCore.Modpacks.ModpackOptions.Downloads : FMCore.Downloader.DownloadOptions | get_Downloads,set_Downloads[init]
P FMCore.Modpacks.ModpackOptions.IncludeOptionalFiles : System.Boolean | get_IncludeOptionalFiles,set_IncludeOptionalFiles[init]
P FMCore.Modpacks.ModpackOptions.InstallRuntime : System.Boolean | get_InstallRuntime,set_InstallRuntime[init]
P FMCore.Modpacks.ModpackOptions.JavaPath : System.String | get_JavaPath,set_JavaPath[init]
P FMCore.Modpacks.ModpackOptions.Modrinth : FMCore.Resources.ModrinthClient | get_Modrinth,set_Modrinth[init]
P FMCore.Modpacks.ModpackOptions.PreserveModifiedConfigurations : System.Boolean | get_PreserveModifiedConfigurations,set_PreserveModifiedConfigurations[init]
P FMCore.Modpacks.ModpackOptions.RequireDeclaredModCompatibility : System.Boolean | get_RequireDeclaredModCompatibility,set_RequireDeclaredModCompatibility[init]
P FMCore.Modpacks.ModpackOptions.SourceId : System.String | get_SourceId,set_SourceId[init]
P FMCore.Modpacks.ModpackProgress.CompletedFiles : System.Int32 | get_CompletedFiles,set_CompletedFiles[init]
P FMCore.Modpacks.ModpackProgress.Download : FMCore.Downloader.DownloadProgress | get_Download,set_Download[init]
P FMCore.Modpacks.ModpackProgress.Installation : FMCore.Installer.InstallationProgress | get_Installation,set_Installation[init]
P FMCore.Modpacks.ModpackProgress.Stage : FMCore.Modpacks.ModpackStage | get_Stage,set_Stage[init]
P FMCore.Modpacks.ModpackProgress.TotalFiles : System.Int32 | get_TotalFiles,set_TotalFiles[init]
P FMCore.Modpacks.ModpackUpdatePlan.CanApply : System.Boolean | get_CanApply
P FMCore.Modpacks.ModpackUpdatePlan.Changes : System.Collections.Generic.IReadOnlyList`1[FMCore.Modpacks.ModpackChange] | get_Changes,set_Changes[init]
P FMCore.Modpacks.ModpackUpdatePlan.CurrentRevision : System.Nullable`1[System.Guid] | get_CurrentRevision,set_CurrentRevision[init]
P FMCore.Modpacks.ModpackUpdatePlan.Target : FMCore.Modpacks.ModpackInfo | get_Target,set_Target[init]
T FMCore.Modpacks.InstalledModpack : System.Object | System.IEquatable`1[FMCore.Modpacks.InstalledModpack]
T FMCore.Modpacks.ModpackArchive : System.Object |
T FMCore.Modpacks.ModpackChange : System.Object | System.IEquatable`1[FMCore.Modpacks.ModpackChange]
T FMCore.Modpacks.ModpackChangeKind : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Modpacks.ModpackCleanupResult : System.Object | System.IEquatable`1[FMCore.Modpacks.ModpackCleanupResult]
T FMCore.Modpacks.ModpackErrorCode : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Modpacks.ModpackException : System.Exception | System.Runtime.Serialization.ISerializable
T FMCore.Modpacks.ModpackExportOptions : System.Object | System.IEquatable`1[FMCore.Modpacks.ModpackExportOptions]
T FMCore.Modpacks.ModpackFile : System.Object | System.IEquatable`1[FMCore.Modpacks.ModpackFile]
T FMCore.Modpacks.ModpackFormat : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Modpacks.ModpackInfo : System.Object | System.IEquatable`1[FMCore.Modpacks.ModpackInfo]
T FMCore.Modpacks.ModpackInstallResult : System.Object | System.IEquatable`1[FMCore.Modpacks.ModpackInstallResult]
T FMCore.Modpacks.ModpackInstaller : System.Object |
T FMCore.Modpacks.ModpackModUpdate : System.Object | System.IEquatable`1[FMCore.Modpacks.ModpackModUpdate]
T FMCore.Modpacks.ModpackOptions : System.Object | System.IEquatable`1[FMCore.Modpacks.ModpackOptions]
T FMCore.Modpacks.ModpackProgress : System.Object | System.IEquatable`1[FMCore.Modpacks.ModpackProgress]
T FMCore.Modpacks.ModpackStage : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Modpacks.ModpackUpdatePlan : System.Object | System.IEquatable`1[FMCore.Modpacks.ModpackUpdatePlan]
```

## Resources

```text
F FMCore.Resources.CurseForgeRelationType.EmbeddedLibrary : FMCore.Resources.CurseForgeRelationType = 1
F FMCore.Resources.CurseForgeRelationType.Include : FMCore.Resources.CurseForgeRelationType = 6
F FMCore.Resources.CurseForgeRelationType.Incompatible : FMCore.Resources.CurseForgeRelationType = 5
F FMCore.Resources.CurseForgeRelationType.OptionalDependency : FMCore.Resources.CurseForgeRelationType = 2
F FMCore.Resources.CurseForgeRelationType.RequiredDependency : FMCore.Resources.CurseForgeRelationType = 3
F FMCore.Resources.CurseForgeRelationType.Tool : FMCore.Resources.CurseForgeRelationType = 4
F FMCore.Resources.ModDependencyType.Embedded : FMCore.Resources.ModDependencyType = 3
F FMCore.Resources.ModDependencyType.Incompatible : FMCore.Resources.ModDependencyType = 2
F FMCore.Resources.ModDependencyType.Optional : FMCore.Resources.ModDependencyType = 1
F FMCore.Resources.ModDependencyType.Required : FMCore.Resources.ModDependencyType = 0
F FMCore.Resources.ResourceErrorCode.AuthenticationFailed : FMCore.Resources.ResourceErrorCode = 18
F FMCore.Resources.ResourceErrorCode.Busy : FMCore.Resources.ResourceErrorCode = 12
F FMCore.Resources.ResourceErrorCode.Conflict : FMCore.Resources.ResourceErrorCode = 10
F FMCore.Resources.ResourceErrorCode.DependencyConflict : FMCore.Resources.ResourceErrorCode = 9
F FMCore.Resources.ResourceErrorCode.DownloadFailed : FMCore.Resources.ResourceErrorCode = 15
F FMCore.Resources.ResourceErrorCode.DownloadRestricted : FMCore.Resources.ResourceErrorCode = 17
F FMCore.Resources.ResourceErrorCode.HttpError : FMCore.Resources.ResourceErrorCode = 2
F FMCore.Resources.ResourceErrorCode.Incompatible : FMCore.Resources.ResourceErrorCode = 8
F FMCore.Resources.ResourceErrorCode.InvalidMetadata : FMCore.Resources.ResourceErrorCode = 6
F FMCore.Resources.ResourceErrorCode.InvalidRequest : FMCore.Resources.ResourceErrorCode = 0
F FMCore.Resources.ResourceErrorCode.IoError : FMCore.Resources.ResourceErrorCode = 16
F FMCore.Resources.ResourceErrorCode.ModpackLocked : FMCore.Resources.ResourceErrorCode = 19
F FMCore.Resources.ResourceErrorCode.NetworkError : FMCore.Resources.ResourceErrorCode = 5
F FMCore.Resources.ResourceErrorCode.NotFound : FMCore.Resources.ResourceErrorCode = 1
F FMCore.Resources.ResourceErrorCode.RateLimited : FMCore.Resources.ResourceErrorCode = 3
F FMCore.Resources.ResourceErrorCode.RecoveryRequired : FMCore.Resources.ResourceErrorCode = 14
F FMCore.Resources.ResourceErrorCode.StorageCorrupted : FMCore.Resources.ResourceErrorCode = 13
F FMCore.Resources.ResourceErrorCode.Timeout : FMCore.Resources.ResourceErrorCode = 4
F FMCore.Resources.ResourceErrorCode.UnsafePath : FMCore.Resources.ResourceErrorCode = 11
F FMCore.Resources.ResourceErrorCode.Unsupported : FMCore.Resources.ResourceErrorCode = 7
F FMCore.Resources.ResourceStage.Committing : FMCore.Resources.ResourceStage = 2
F FMCore.Resources.ResourceStage.Completed : FMCore.Resources.ResourceStage = 3
F FMCore.Resources.ResourceStage.Downloading : FMCore.Resources.ResourceStage = 1
F FMCore.Resources.ResourceStage.Resolving : FMCore.Resources.ResourceStage = 0
M FMCore.Resources.CurseForgeClient..ctor(System.String apiKey, System.Net.Http.HttpMessageHandler handler, System.Nullable`1[System.TimeSpan] timeout = default) -> constructor | instance public
M FMCore.Resources.CurseForgeClient..ctor(System.String apiKey, System.Nullable`1[System.TimeSpan] timeout = default) -> constructor | instance public
M FMCore.Resources.CurseForgeClient.Dispose() -> System.Void | instance public
M FMCore.Resources.CurseForgeClient.GetFileAsync(System.Int32 modId, System.Int32 fileId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Resources.CurseForgeFile] | instance public
M FMCore.Resources.CurseForgeClient.GetFilesAsync(System.Int32 modId, FMCore.Resources.ModEnvironment environment = null, System.Int32 index = 0, System.Int32 pageSize = 50, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Resources.CurseForgeFilePage] | instance public
M FMCore.Resources.CurseForgeClient.GetProjectAsync(System.Int32 modId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Resources.CurseForgeProject] | instance public
M FMCore.Resources.CurseForgeClient.SearchAsync(System.String query = "", FMCore.Resources.CurseForgeSearchOptions options = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Resources.CurseForgeSearchResult] | instance public
M FMCore.Resources.CurseForgeClient.ToString() -> System.String | instance public
M FMCore.Resources.CurseForgeDependency..ctor(System.Int32 ModId, FMCore.Resources.CurseForgeRelationType RelationType) -> constructor | instance public
M FMCore.Resources.CurseForgeDependency.<Clone>$() -> FMCore.Resources.CurseForgeDependency | instance public
M FMCore.Resources.CurseForgeDependency.Deconstruct(System.Int32& ModId, FMCore.Resources.CurseForgeRelationType& RelationType) -> System.Void | instance public
M FMCore.Resources.CurseForgeDependency.Equals(FMCore.Resources.CurseForgeDependency other) -> System.Boolean | instance public
M FMCore.Resources.CurseForgeDependency.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.CurseForgeDependency.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeDependency.ToString() -> System.String | instance public
M FMCore.Resources.CurseForgeDependency.get_ModId() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeDependency.get_RelationType() -> FMCore.Resources.CurseForgeRelationType | instance public
M FMCore.Resources.CurseForgeDependency.op_Equality(FMCore.Resources.CurseForgeDependency left, FMCore.Resources.CurseForgeDependency right) -> System.Boolean | static public
M FMCore.Resources.CurseForgeDependency.op_Inequality(FMCore.Resources.CurseForgeDependency left, FMCore.Resources.CurseForgeDependency right) -> System.Boolean | static public
M FMCore.Resources.CurseForgeDependency.set_ModId(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeDependency.set_RelationType(FMCore.Resources.CurseForgeRelationType value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile..ctor(System.Int32 Id, System.Int32 ModId, System.Int32 GameId, System.String DisplayName, System.String FileName, System.Int32 ReleaseType, System.DateTimeOffset PublishedAt, System.Int64 Size, System.String Sha1, System.String Md5, System.Uri DownloadUrl, System.Boolean IsAvailable, System.Boolean IsServerPack, System.Boolean IsEarlyAccessContent, System.Boolean ExposeAsAlternative, System.Collections.Generic.IReadOnlyList`1[System.String] GameVersions, System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeDependency] Dependencies) -> constructor | instance public
M FMCore.Resources.CurseForgeFile.<Clone>$() -> FMCore.Resources.CurseForgeFile | instance public
M FMCore.Resources.CurseForgeFile.Deconstruct(System.Int32& Id, System.Int32& ModId, System.Int32& GameId, System.String& DisplayName, System.String& FileName, System.Int32& ReleaseType, System.DateTimeOffset& PublishedAt, System.Int64& Size, System.String& Sha1, System.String& Md5, System.Uri& DownloadUrl, System.Boolean& IsAvailable, System.Boolean& IsServerPack, System.Boolean& IsEarlyAccessContent, System.Boolean& ExposeAsAlternative, System.Collections.Generic.IReadOnlyList`1[System.String]& GameVersions, System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeDependency]& Dependencies) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.Equals(FMCore.Resources.CurseForgeFile other) -> System.Boolean | instance public
M FMCore.Resources.CurseForgeFile.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.CurseForgeFile.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeFile.ToString() -> System.String | instance public
M FMCore.Resources.CurseForgeFile.get_Dependencies() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeDependency] | instance public
M FMCore.Resources.CurseForgeFile.get_DisplayName() -> System.String | instance public
M FMCore.Resources.CurseForgeFile.get_DownloadUrl() -> System.Uri | instance public
M FMCore.Resources.CurseForgeFile.get_ExposeAsAlternative() -> System.Boolean | instance public
M FMCore.Resources.CurseForgeFile.get_FileName() -> System.String | instance public
M FMCore.Resources.CurseForgeFile.get_GameId() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeFile.get_GameVersions() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Resources.CurseForgeFile.get_Id() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeFile.get_IsAvailable() -> System.Boolean | instance public
M FMCore.Resources.CurseForgeFile.get_IsEarlyAccessContent() -> System.Boolean | instance public
M FMCore.Resources.CurseForgeFile.get_IsServerPack() -> System.Boolean | instance public
M FMCore.Resources.CurseForgeFile.get_Md5() -> System.String | instance public
M FMCore.Resources.CurseForgeFile.get_ModId() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeFile.get_PublishedAt() -> System.DateTimeOffset | instance public
M FMCore.Resources.CurseForgeFile.get_ReleaseType() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeFile.get_Sha1() -> System.String | instance public
M FMCore.Resources.CurseForgeFile.get_Size() -> System.Int64 | instance public
M FMCore.Resources.CurseForgeFile.op_Equality(FMCore.Resources.CurseForgeFile left, FMCore.Resources.CurseForgeFile right) -> System.Boolean | static public
M FMCore.Resources.CurseForgeFile.op_Inequality(FMCore.Resources.CurseForgeFile left, FMCore.Resources.CurseForgeFile right) -> System.Boolean | static public
M FMCore.Resources.CurseForgeFile.set_Dependencies(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeDependency] value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_DisplayName(System.String value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_DownloadUrl(System.Uri value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_ExposeAsAlternative(System.Boolean value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_FileName(System.String value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_GameId(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_GameVersions(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_Id(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_IsAvailable(System.Boolean value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_IsEarlyAccessContent(System.Boolean value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_IsServerPack(System.Boolean value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_Md5(System.String value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_ModId(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_PublishedAt(System.DateTimeOffset value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_ReleaseType(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_Sha1(System.String value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFile.set_Size(System.Int64 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFilePage..ctor(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeFile] Files, System.Int32 Index, System.Int32 PageSize, System.Int32 TotalCount, System.Int32 ResultCount) -> constructor | instance public
M FMCore.Resources.CurseForgeFilePage.<Clone>$() -> FMCore.Resources.CurseForgeFilePage | instance public
M FMCore.Resources.CurseForgeFilePage.Deconstruct(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeFile]& Files, System.Int32& Index, System.Int32& PageSize, System.Int32& TotalCount, System.Int32& ResultCount) -> System.Void | instance public
M FMCore.Resources.CurseForgeFilePage.Equals(FMCore.Resources.CurseForgeFilePage other) -> System.Boolean | instance public
M FMCore.Resources.CurseForgeFilePage.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.CurseForgeFilePage.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeFilePage.ToString() -> System.String | instance public
M FMCore.Resources.CurseForgeFilePage.get_Files() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeFile] | instance public
M FMCore.Resources.CurseForgeFilePage.get_Index() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeFilePage.get_PageSize() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeFilePage.get_ResultCount() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeFilePage.get_TotalCount() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeFilePage.op_Equality(FMCore.Resources.CurseForgeFilePage left, FMCore.Resources.CurseForgeFilePage right) -> System.Boolean | static public
M FMCore.Resources.CurseForgeFilePage.op_Inequality(FMCore.Resources.CurseForgeFilePage left, FMCore.Resources.CurseForgeFilePage right) -> System.Boolean | static public
M FMCore.Resources.CurseForgeFilePage.set_Files(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeFile] value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFilePage.set_Index(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFilePage.set_PageSize(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFilePage.set_ResultCount(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeFilePage.set_TotalCount(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeInstaller..ctor(FMCore.Instances.InstanceInfo instance, FMCore.Resources.CurseForgeClient client, FMCore.Downloader.DownloadOptions downloadOptions = null, System.Net.Http.HttpClient downloadClient = null) -> constructor | instance public
M FMCore.Resources.CurseForgeInstaller..ctor(System.String rootDirectory, System.String instanceId, FMCore.Resources.ModEnvironment environment, FMCore.Resources.CurseForgeClient client, FMCore.Downloader.DownloadOptions downloadOptions = null, System.Net.Http.HttpClient downloadClient = null) -> constructor | instance public
M FMCore.Resources.CurseForgeInstaller.CheckUpdatesAsync(System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeUpdate]] | instance public
M FMCore.Resources.CurseForgeInstaller.GetInstalledAsync(System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.InstalledMod]] | instance public
M FMCore.Resources.CurseForgeInstaller.InstallAsync(System.Int32 modId, System.Nullable`1[System.Int32] fileId = default, System.IProgress`1[FMCore.Resources.ResourceProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Resources.ModInstallResult] | instance public
M FMCore.Resources.CurseForgeInstaller.UpdateAsync(System.Int32 modId, System.IProgress`1[FMCore.Resources.ResourceProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Resources.ModInstallResult] | instance public
M FMCore.Resources.CurseForgeInstaller.get_GameDirectory() -> System.String | instance public
M FMCore.Resources.CurseForgeProject..ctor(System.Int32 Id, System.Int32 GameId, System.String Name, System.String Slug, System.String Summary, System.Nullable`1[System.Int32] ClassId, System.Nullable`1[System.Boolean] AllowModDistribution, System.Boolean IsAvailable) -> constructor | instance public
M FMCore.Resources.CurseForgeProject.<Clone>$() -> FMCore.Resources.CurseForgeProject | instance public
M FMCore.Resources.CurseForgeProject.Deconstruct(System.Int32& Id, System.Int32& GameId, System.String& Name, System.String& Slug, System.String& Summary, System.Nullable`1[System.Int32]& ClassId, System.Nullable`1[System.Boolean]& AllowModDistribution, System.Boolean& IsAvailable) -> System.Void | instance public
M FMCore.Resources.CurseForgeProject.Equals(FMCore.Resources.CurseForgeProject other) -> System.Boolean | instance public
M FMCore.Resources.CurseForgeProject.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.CurseForgeProject.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeProject.ToString() -> System.String | instance public
M FMCore.Resources.CurseForgeProject.get_AllowModDistribution() -> System.Nullable`1[System.Boolean] | instance public
M FMCore.Resources.CurseForgeProject.get_ClassId() -> System.Nullable`1[System.Int32] | instance public
M FMCore.Resources.CurseForgeProject.get_GameId() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeProject.get_Id() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeProject.get_IsAvailable() -> System.Boolean | instance public
M FMCore.Resources.CurseForgeProject.get_Name() -> System.String | instance public
M FMCore.Resources.CurseForgeProject.get_Slug() -> System.String | instance public
M FMCore.Resources.CurseForgeProject.get_Summary() -> System.String | instance public
M FMCore.Resources.CurseForgeProject.op_Equality(FMCore.Resources.CurseForgeProject left, FMCore.Resources.CurseForgeProject right) -> System.Boolean | static public
M FMCore.Resources.CurseForgeProject.op_Inequality(FMCore.Resources.CurseForgeProject left, FMCore.Resources.CurseForgeProject right) -> System.Boolean | static public
M FMCore.Resources.CurseForgeProject.set_AllowModDistribution(System.Nullable`1[System.Boolean] value) -> System.Void | instance public
M FMCore.Resources.CurseForgeProject.set_ClassId(System.Nullable`1[System.Int32] value) -> System.Void | instance public
M FMCore.Resources.CurseForgeProject.set_GameId(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeProject.set_Id(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeProject.set_IsAvailable(System.Boolean value) -> System.Void | instance public
M FMCore.Resources.CurseForgeProject.set_Name(System.String value) -> System.Void | instance public
M FMCore.Resources.CurseForgeProject.set_Slug(System.String value) -> System.Void | instance public
M FMCore.Resources.CurseForgeProject.set_Summary(System.String value) -> System.Void | instance public
M FMCore.Resources.CurseForgeSearchOptions..ctor() -> constructor | instance public
M FMCore.Resources.CurseForgeSearchOptions.<Clone>$() -> FMCore.Resources.CurseForgeSearchOptions | instance public
M FMCore.Resources.CurseForgeSearchOptions.Equals(FMCore.Resources.CurseForgeSearchOptions other) -> System.Boolean | instance public
M FMCore.Resources.CurseForgeSearchOptions.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.CurseForgeSearchOptions.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeSearchOptions.ToString() -> System.String | instance public
M FMCore.Resources.CurseForgeSearchOptions.get_CategoryId() -> System.Nullable`1[System.Int32] | instance public
M FMCore.Resources.CurseForgeSearchOptions.get_ClassId() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeSearchOptions.get_Index() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeSearchOptions.get_Loader() -> System.String | instance public
M FMCore.Resources.CurseForgeSearchOptions.get_MinecraftVersion() -> System.String | instance public
M FMCore.Resources.CurseForgeSearchOptions.get_PageSize() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeSearchOptions.get_SortField() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeSearchOptions.get_SortOrder() -> System.String | instance public
M FMCore.Resources.CurseForgeSearchOptions.op_Equality(FMCore.Resources.CurseForgeSearchOptions left, FMCore.Resources.CurseForgeSearchOptions right) -> System.Boolean | static public
M FMCore.Resources.CurseForgeSearchOptions.op_Inequality(FMCore.Resources.CurseForgeSearchOptions left, FMCore.Resources.CurseForgeSearchOptions right) -> System.Boolean | static public
M FMCore.Resources.CurseForgeSearchOptions.set_CategoryId(System.Nullable`1[System.Int32] value) -> System.Void | instance public
M FMCore.Resources.CurseForgeSearchOptions.set_ClassId(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeSearchOptions.set_Index(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeSearchOptions.set_Loader(System.String value) -> System.Void | instance public
M FMCore.Resources.CurseForgeSearchOptions.set_MinecraftVersion(System.String value) -> System.Void | instance public
M FMCore.Resources.CurseForgeSearchOptions.set_PageSize(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeSearchOptions.set_SortField(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeSearchOptions.set_SortOrder(System.String value) -> System.Void | instance public
M FMCore.Resources.CurseForgeSearchResult..ctor(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeProject] Mods, System.Int32 Index, System.Int32 PageSize, System.Int32 TotalCount) -> constructor | instance public
M FMCore.Resources.CurseForgeSearchResult.<Clone>$() -> FMCore.Resources.CurseForgeSearchResult | instance public
M FMCore.Resources.CurseForgeSearchResult.Deconstruct(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeProject]& Mods, System.Int32& Index, System.Int32& PageSize, System.Int32& TotalCount) -> System.Void | instance public
M FMCore.Resources.CurseForgeSearchResult.Equals(FMCore.Resources.CurseForgeSearchResult other) -> System.Boolean | instance public
M FMCore.Resources.CurseForgeSearchResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.CurseForgeSearchResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeSearchResult.ToString() -> System.String | instance public
M FMCore.Resources.CurseForgeSearchResult.get_Index() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeSearchResult.get_Mods() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeProject] | instance public
M FMCore.Resources.CurseForgeSearchResult.get_PageSize() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeSearchResult.get_TotalCount() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeSearchResult.op_Equality(FMCore.Resources.CurseForgeSearchResult left, FMCore.Resources.CurseForgeSearchResult right) -> System.Boolean | static public
M FMCore.Resources.CurseForgeSearchResult.op_Inequality(FMCore.Resources.CurseForgeSearchResult left, FMCore.Resources.CurseForgeSearchResult right) -> System.Boolean | static public
M FMCore.Resources.CurseForgeSearchResult.set_Index(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeSearchResult.set_Mods(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeProject] value) -> System.Void | instance public
M FMCore.Resources.CurseForgeSearchResult.set_PageSize(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeSearchResult.set_TotalCount(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.CurseForgeUpdate..ctor(FMCore.Resources.InstalledMod Installed, FMCore.Resources.CurseForgeFile Available) -> constructor | instance public
M FMCore.Resources.CurseForgeUpdate.<Clone>$() -> FMCore.Resources.CurseForgeUpdate | instance public
M FMCore.Resources.CurseForgeUpdate.Deconstruct(FMCore.Resources.InstalledMod& Installed, FMCore.Resources.CurseForgeFile& Available) -> System.Void | instance public
M FMCore.Resources.CurseForgeUpdate.Equals(FMCore.Resources.CurseForgeUpdate other) -> System.Boolean | instance public
M FMCore.Resources.CurseForgeUpdate.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.CurseForgeUpdate.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.CurseForgeUpdate.ToString() -> System.String | instance public
M FMCore.Resources.CurseForgeUpdate.get_Available() -> FMCore.Resources.CurseForgeFile | instance public
M FMCore.Resources.CurseForgeUpdate.get_Installed() -> FMCore.Resources.InstalledMod | instance public
M FMCore.Resources.CurseForgeUpdate.op_Equality(FMCore.Resources.CurseForgeUpdate left, FMCore.Resources.CurseForgeUpdate right) -> System.Boolean | static public
M FMCore.Resources.CurseForgeUpdate.op_Inequality(FMCore.Resources.CurseForgeUpdate left, FMCore.Resources.CurseForgeUpdate right) -> System.Boolean | static public
M FMCore.Resources.CurseForgeUpdate.set_Available(FMCore.Resources.CurseForgeFile value) -> System.Void | instance public
M FMCore.Resources.CurseForgeUpdate.set_Installed(FMCore.Resources.InstalledMod value) -> System.Void | instance public
M FMCore.Resources.InstalledMod..ctor(System.String ProjectId, System.String VersionId, System.String VersionNumber, System.DateTimeOffset PublishedAt, System.String FileName, System.String Sha512, System.Int64 Size, System.DateTimeOffset InstalledAt, System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModDependency] Dependencies) -> constructor | instance public
M FMCore.Resources.InstalledMod.<Clone>$() -> FMCore.Resources.InstalledMod | instance public
M FMCore.Resources.InstalledMod.Deconstruct(System.String& ProjectId, System.String& VersionId, System.String& VersionNumber, System.DateTimeOffset& PublishedAt, System.String& FileName, System.String& Sha512, System.Int64& Size, System.DateTimeOffset& InstalledAt, System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModDependency]& Dependencies) -> System.Void | instance public
M FMCore.Resources.InstalledMod.Equals(FMCore.Resources.InstalledMod other) -> System.Boolean | instance public
M FMCore.Resources.InstalledMod.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.InstalledMod.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.InstalledMod.ToString() -> System.String | instance public
M FMCore.Resources.InstalledMod.get_Dependencies() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModDependency] | instance public
M FMCore.Resources.InstalledMod.get_FileName() -> System.String | instance public
M FMCore.Resources.InstalledMod.get_InstalledAt() -> System.DateTimeOffset | instance public
M FMCore.Resources.InstalledMod.get_ProjectId() -> System.String | instance public
M FMCore.Resources.InstalledMod.get_PublishedAt() -> System.DateTimeOffset | instance public
M FMCore.Resources.InstalledMod.get_Sha512() -> System.String | instance public
M FMCore.Resources.InstalledMod.get_Size() -> System.Int64 | instance public
M FMCore.Resources.InstalledMod.get_Source() -> System.String | instance public
M FMCore.Resources.InstalledMod.get_VersionId() -> System.String | instance public
M FMCore.Resources.InstalledMod.get_VersionNumber() -> System.String | instance public
M FMCore.Resources.InstalledMod.op_Equality(FMCore.Resources.InstalledMod left, FMCore.Resources.InstalledMod right) -> System.Boolean | static public
M FMCore.Resources.InstalledMod.op_Inequality(FMCore.Resources.InstalledMod left, FMCore.Resources.InstalledMod right) -> System.Boolean | static public
M FMCore.Resources.InstalledMod.set_Dependencies(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModDependency] value) -> System.Void | instance public
M FMCore.Resources.InstalledMod.set_FileName(System.String value) -> System.Void | instance public
M FMCore.Resources.InstalledMod.set_InstalledAt(System.DateTimeOffset value) -> System.Void | instance public
M FMCore.Resources.InstalledMod.set_ProjectId(System.String value) -> System.Void | instance public
M FMCore.Resources.InstalledMod.set_PublishedAt(System.DateTimeOffset value) -> System.Void | instance public
M FMCore.Resources.InstalledMod.set_Sha512(System.String value) -> System.Void | instance public
M FMCore.Resources.InstalledMod.set_Size(System.Int64 value) -> System.Void | instance public
M FMCore.Resources.InstalledMod.set_Source(System.String value) -> System.Void | instance public
M FMCore.Resources.InstalledMod.set_VersionId(System.String value) -> System.Void | instance public
M FMCore.Resources.InstalledMod.set_VersionNumber(System.String value) -> System.Void | instance public
M FMCore.Resources.ModDependency..ctor(System.String ProjectId, System.String VersionId, System.String FileName, FMCore.Resources.ModDependencyType Type) -> constructor | instance public
M FMCore.Resources.ModDependency.<Clone>$() -> FMCore.Resources.ModDependency | instance public
M FMCore.Resources.ModDependency.Deconstruct(System.String& ProjectId, System.String& VersionId, System.String& FileName, FMCore.Resources.ModDependencyType& Type) -> System.Void | instance public
M FMCore.Resources.ModDependency.Equals(FMCore.Resources.ModDependency other) -> System.Boolean | instance public
M FMCore.Resources.ModDependency.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.ModDependency.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.ModDependency.ToString() -> System.String | instance public
M FMCore.Resources.ModDependency.get_FileName() -> System.String | instance public
M FMCore.Resources.ModDependency.get_ProjectId() -> System.String | instance public
M FMCore.Resources.ModDependency.get_Type() -> FMCore.Resources.ModDependencyType | instance public
M FMCore.Resources.ModDependency.get_VersionId() -> System.String | instance public
M FMCore.Resources.ModDependency.op_Equality(FMCore.Resources.ModDependency left, FMCore.Resources.ModDependency right) -> System.Boolean | static public
M FMCore.Resources.ModDependency.op_Inequality(FMCore.Resources.ModDependency left, FMCore.Resources.ModDependency right) -> System.Boolean | static public
M FMCore.Resources.ModDependency.set_FileName(System.String value) -> System.Void | instance public
M FMCore.Resources.ModDependency.set_ProjectId(System.String value) -> System.Void | instance public
M FMCore.Resources.ModDependency.set_Type(FMCore.Resources.ModDependencyType value) -> System.Void | instance public
M FMCore.Resources.ModDependency.set_VersionId(System.String value) -> System.Void | instance public
M FMCore.Resources.ModEnvironment..ctor(System.String MinecraftVersion, System.String Loader) -> constructor | instance public
M FMCore.Resources.ModEnvironment.<Clone>$() -> FMCore.Resources.ModEnvironment | instance public
M FMCore.Resources.ModEnvironment.Deconstruct(System.String& MinecraftVersion, System.String& Loader) -> System.Void | instance public
M FMCore.Resources.ModEnvironment.Equals(FMCore.Resources.ModEnvironment other) -> System.Boolean | instance public
M FMCore.Resources.ModEnvironment.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.ModEnvironment.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.ModEnvironment.ToString() -> System.String | instance public
M FMCore.Resources.ModEnvironment.get_IncludePrereleases() -> System.Boolean | instance public
M FMCore.Resources.ModEnvironment.get_Loader() -> System.String | instance public
M FMCore.Resources.ModEnvironment.get_MinecraftVersion() -> System.String | instance public
M FMCore.Resources.ModEnvironment.op_Equality(FMCore.Resources.ModEnvironment left, FMCore.Resources.ModEnvironment right) -> System.Boolean | static public
M FMCore.Resources.ModEnvironment.op_Inequality(FMCore.Resources.ModEnvironment left, FMCore.Resources.ModEnvironment right) -> System.Boolean | static public
M FMCore.Resources.ModEnvironment.set_IncludePrereleases(System.Boolean value) -> System.Void | instance public
M FMCore.Resources.ModEnvironment.set_Loader(System.String value) -> System.Void | instance public
M FMCore.Resources.ModEnvironment.set_MinecraftVersion(System.String value) -> System.Void | instance public
M FMCore.Resources.ModInstallResult..ctor(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.InstalledMod] Installed, System.Boolean Reused) -> constructor | instance public
M FMCore.Resources.ModInstallResult.<Clone>$() -> FMCore.Resources.ModInstallResult | instance public
M FMCore.Resources.ModInstallResult.Deconstruct(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.InstalledMod]& Installed, System.Boolean& Reused) -> System.Void | instance public
M FMCore.Resources.ModInstallResult.Equals(FMCore.Resources.ModInstallResult other) -> System.Boolean | instance public
M FMCore.Resources.ModInstallResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.ModInstallResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.ModInstallResult.ToString() -> System.String | instance public
M FMCore.Resources.ModInstallResult.get_Installed() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.InstalledMod] | instance public
M FMCore.Resources.ModInstallResult.get_Reused() -> System.Boolean | instance public
M FMCore.Resources.ModInstallResult.op_Equality(FMCore.Resources.ModInstallResult left, FMCore.Resources.ModInstallResult right) -> System.Boolean | static public
M FMCore.Resources.ModInstallResult.op_Inequality(FMCore.Resources.ModInstallResult left, FMCore.Resources.ModInstallResult right) -> System.Boolean | static public
M FMCore.Resources.ModInstallResult.set_Installed(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.InstalledMod] value) -> System.Void | instance public
M FMCore.Resources.ModInstallResult.set_Reused(System.Boolean value) -> System.Void | instance public
M FMCore.Resources.ModUpdate..ctor(FMCore.Resources.InstalledMod Installed, FMCore.Resources.ModrinthVersion Available) -> constructor | instance public
M FMCore.Resources.ModUpdate.<Clone>$() -> FMCore.Resources.ModUpdate | instance public
M FMCore.Resources.ModUpdate.Deconstruct(FMCore.Resources.InstalledMod& Installed, FMCore.Resources.ModrinthVersion& Available) -> System.Void | instance public
M FMCore.Resources.ModUpdate.Equals(FMCore.Resources.ModUpdate other) -> System.Boolean | instance public
M FMCore.Resources.ModUpdate.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.ModUpdate.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.ModUpdate.ToString() -> System.String | instance public
M FMCore.Resources.ModUpdate.get_Available() -> FMCore.Resources.ModrinthVersion | instance public
M FMCore.Resources.ModUpdate.get_Installed() -> FMCore.Resources.InstalledMod | instance public
M FMCore.Resources.ModUpdate.op_Equality(FMCore.Resources.ModUpdate left, FMCore.Resources.ModUpdate right) -> System.Boolean | static public
M FMCore.Resources.ModUpdate.op_Inequality(FMCore.Resources.ModUpdate left, FMCore.Resources.ModUpdate right) -> System.Boolean | static public
M FMCore.Resources.ModUpdate.set_Available(FMCore.Resources.ModrinthVersion value) -> System.Void | instance public
M FMCore.Resources.ModUpdate.set_Installed(FMCore.Resources.InstalledMod value) -> System.Void | instance public
M FMCore.Resources.ModrinthClient..ctor(System.Net.Http.HttpClient httpClient, System.String userAgent = "Voltucit/FMCore (https://github.com/Voltucit/FocusMinecraftCore)", System.Nullable`1[System.TimeSpan] timeout = default) -> constructor | instance public
M FMCore.Resources.ModrinthClient..ctor(System.String userAgent = "Voltucit/FMCore (https://github.com/Voltucit/FocusMinecraftCore)") -> constructor | instance public
M FMCore.Resources.ModrinthClient.GetProjectAsync(System.String projectId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Resources.ModrinthProject] | instance public
M FMCore.Resources.ModrinthClient.GetVersionAsync(System.String versionId, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Resources.ModrinthVersion] | instance public
M FMCore.Resources.ModrinthClient.GetVersionByHashAsync(System.String sha512, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Resources.ModrinthVersion] | instance public
M FMCore.Resources.ModrinthClient.GetVersionsAsync(System.String projectId, FMCore.Resources.ModEnvironment environment = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModrinthVersion]] | instance public
M FMCore.Resources.ModrinthClient.SearchAsync(System.String query = "", FMCore.Resources.ModrinthSearchOptions options = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Resources.ModrinthSearchResult] | instance public
M FMCore.Resources.ModrinthFile..ctor(System.String FileName, System.Uri Url, System.String Sha512, System.String Sha1, System.Int64 Size, System.Boolean Primary, System.String FileType) -> constructor | instance public
M FMCore.Resources.ModrinthFile.<Clone>$() -> FMCore.Resources.ModrinthFile | instance public
M FMCore.Resources.ModrinthFile.Deconstruct(System.String& FileName, System.Uri& Url, System.String& Sha512, System.String& Sha1, System.Int64& Size, System.Boolean& Primary, System.String& FileType) -> System.Void | instance public
M FMCore.Resources.ModrinthFile.Equals(FMCore.Resources.ModrinthFile other) -> System.Boolean | instance public
M FMCore.Resources.ModrinthFile.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.ModrinthFile.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.ModrinthFile.ToString() -> System.String | instance public
M FMCore.Resources.ModrinthFile.get_FileName() -> System.String | instance public
M FMCore.Resources.ModrinthFile.get_FileType() -> System.String | instance public
M FMCore.Resources.ModrinthFile.get_Primary() -> System.Boolean | instance public
M FMCore.Resources.ModrinthFile.get_Sha1() -> System.String | instance public
M FMCore.Resources.ModrinthFile.get_Sha512() -> System.String | instance public
M FMCore.Resources.ModrinthFile.get_Size() -> System.Int64 | instance public
M FMCore.Resources.ModrinthFile.get_Url() -> System.Uri | instance public
M FMCore.Resources.ModrinthFile.op_Equality(FMCore.Resources.ModrinthFile left, FMCore.Resources.ModrinthFile right) -> System.Boolean | static public
M FMCore.Resources.ModrinthFile.op_Inequality(FMCore.Resources.ModrinthFile left, FMCore.Resources.ModrinthFile right) -> System.Boolean | static public
M FMCore.Resources.ModrinthFile.set_FileName(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthFile.set_FileType(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthFile.set_Primary(System.Boolean value) -> System.Void | instance public
M FMCore.Resources.ModrinthFile.set_Sha1(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthFile.set_Sha512(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthFile.set_Size(System.Int64 value) -> System.Void | instance public
M FMCore.Resources.ModrinthFile.set_Url(System.Uri value) -> System.Void | instance public
M FMCore.Resources.ModrinthInstaller..ctor(FMCore.Instances.InstanceInfo instance, FMCore.Resources.ModrinthClient client = null, FMCore.Downloader.DownloadOptions downloadOptions = null) -> constructor | instance public
M FMCore.Resources.ModrinthInstaller..ctor(System.String rootDirectory, System.String instanceId, FMCore.Resources.ModEnvironment environment, FMCore.Resources.ModrinthClient client = null, FMCore.Downloader.DownloadOptions downloadOptions = null) -> constructor | instance public
M FMCore.Resources.ModrinthInstaller.CheckUpdatesAsync(System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModUpdate]] | instance public
M FMCore.Resources.ModrinthInstaller.GetInstalledAsync(System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.InstalledMod]] | instance public
M FMCore.Resources.ModrinthInstaller.InstallAsync(System.String projectId, System.String versionId = null, System.IProgress`1[FMCore.Resources.ResourceProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Resources.ModInstallResult] | instance public
M FMCore.Resources.ModrinthInstaller.UpdateAsync(System.String projectId, System.IProgress`1[FMCore.Resources.ResourceProgress] progress = null, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Resources.ModInstallResult] | instance public
M FMCore.Resources.ModrinthInstaller.get_GameDirectory() -> System.String | instance public
M FMCore.Resources.ModrinthProject..ctor(System.String Id, System.String Slug, System.String Title, System.String Description, System.String ProjectType) -> constructor | instance public
M FMCore.Resources.ModrinthProject.<Clone>$() -> FMCore.Resources.ModrinthProject | instance public
M FMCore.Resources.ModrinthProject.Deconstruct(System.String& Id, System.String& Slug, System.String& Title, System.String& Description, System.String& ProjectType) -> System.Void | instance public
M FMCore.Resources.ModrinthProject.Equals(FMCore.Resources.ModrinthProject other) -> System.Boolean | instance public
M FMCore.Resources.ModrinthProject.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.ModrinthProject.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.ModrinthProject.ToString() -> System.String | instance public
M FMCore.Resources.ModrinthProject.get_Description() -> System.String | instance public
M FMCore.Resources.ModrinthProject.get_Id() -> System.String | instance public
M FMCore.Resources.ModrinthProject.get_ProjectType() -> System.String | instance public
M FMCore.Resources.ModrinthProject.get_Slug() -> System.String | instance public
M FMCore.Resources.ModrinthProject.get_Title() -> System.String | instance public
M FMCore.Resources.ModrinthProject.op_Equality(FMCore.Resources.ModrinthProject left, FMCore.Resources.ModrinthProject right) -> System.Boolean | static public
M FMCore.Resources.ModrinthProject.op_Inequality(FMCore.Resources.ModrinthProject left, FMCore.Resources.ModrinthProject right) -> System.Boolean | static public
M FMCore.Resources.ModrinthProject.set_Description(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthProject.set_Id(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthProject.set_ProjectType(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthProject.set_Slug(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthProject.set_Title(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthSearchOptions..ctor() -> constructor | instance public
M FMCore.Resources.ModrinthSearchOptions.<Clone>$() -> FMCore.Resources.ModrinthSearchOptions | instance public
M FMCore.Resources.ModrinthSearchOptions.Equals(FMCore.Resources.ModrinthSearchOptions other) -> System.Boolean | instance public
M FMCore.Resources.ModrinthSearchOptions.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.ModrinthSearchOptions.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.ModrinthSearchOptions.ToString() -> System.String | instance public
M FMCore.Resources.ModrinthSearchOptions.get_Limit() -> System.Int32 | instance public
M FMCore.Resources.ModrinthSearchOptions.get_Loader() -> System.String | instance public
M FMCore.Resources.ModrinthSearchOptions.get_MinecraftVersion() -> System.String | instance public
M FMCore.Resources.ModrinthSearchOptions.get_Offset() -> System.Int32 | instance public
M FMCore.Resources.ModrinthSearchOptions.get_ProjectType() -> System.String | instance public
M FMCore.Resources.ModrinthSearchOptions.get_Sort() -> System.String | instance public
M FMCore.Resources.ModrinthSearchOptions.op_Equality(FMCore.Resources.ModrinthSearchOptions left, FMCore.Resources.ModrinthSearchOptions right) -> System.Boolean | static public
M FMCore.Resources.ModrinthSearchOptions.op_Inequality(FMCore.Resources.ModrinthSearchOptions left, FMCore.Resources.ModrinthSearchOptions right) -> System.Boolean | static public
M FMCore.Resources.ModrinthSearchOptions.set_Limit(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.ModrinthSearchOptions.set_Loader(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthSearchOptions.set_MinecraftVersion(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthSearchOptions.set_Offset(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.ModrinthSearchOptions.set_ProjectType(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthSearchOptions.set_Sort(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthSearchResult..ctor(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModrinthProject] Hits, System.Int32 Offset, System.Int32 Limit, System.Int32 TotalHits) -> constructor | instance public
M FMCore.Resources.ModrinthSearchResult.<Clone>$() -> FMCore.Resources.ModrinthSearchResult | instance public
M FMCore.Resources.ModrinthSearchResult.Deconstruct(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModrinthProject]& Hits, System.Int32& Offset, System.Int32& Limit, System.Int32& TotalHits) -> System.Void | instance public
M FMCore.Resources.ModrinthSearchResult.Equals(FMCore.Resources.ModrinthSearchResult other) -> System.Boolean | instance public
M FMCore.Resources.ModrinthSearchResult.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.ModrinthSearchResult.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.ModrinthSearchResult.ToString() -> System.String | instance public
M FMCore.Resources.ModrinthSearchResult.get_Hits() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModrinthProject] | instance public
M FMCore.Resources.ModrinthSearchResult.get_Limit() -> System.Int32 | instance public
M FMCore.Resources.ModrinthSearchResult.get_Offset() -> System.Int32 | instance public
M FMCore.Resources.ModrinthSearchResult.get_TotalHits() -> System.Int32 | instance public
M FMCore.Resources.ModrinthSearchResult.op_Equality(FMCore.Resources.ModrinthSearchResult left, FMCore.Resources.ModrinthSearchResult right) -> System.Boolean | static public
M FMCore.Resources.ModrinthSearchResult.op_Inequality(FMCore.Resources.ModrinthSearchResult left, FMCore.Resources.ModrinthSearchResult right) -> System.Boolean | static public
M FMCore.Resources.ModrinthSearchResult.set_Hits(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModrinthProject] value) -> System.Void | instance public
M FMCore.Resources.ModrinthSearchResult.set_Limit(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.ModrinthSearchResult.set_Offset(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.ModrinthSearchResult.set_TotalHits(System.Int32 value) -> System.Void | instance public
M FMCore.Resources.ModrinthVersion..ctor(System.String Id, System.String ProjectId, System.String Name, System.String VersionNumber, System.String VersionType, System.DateTimeOffset PublishedAt, System.Collections.Generic.IReadOnlyList`1[System.String] GameVersions, System.Collections.Generic.IReadOnlyList`1[System.String] Loaders, System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModrinthFile] Files, System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModDependency] Dependencies) -> constructor | instance public
M FMCore.Resources.ModrinthVersion.<Clone>$() -> FMCore.Resources.ModrinthVersion | instance public
M FMCore.Resources.ModrinthVersion.Deconstruct(System.String& Id, System.String& ProjectId, System.String& Name, System.String& VersionNumber, System.String& VersionType, System.DateTimeOffset& PublishedAt, System.Collections.Generic.IReadOnlyList`1[System.String]& GameVersions, System.Collections.Generic.IReadOnlyList`1[System.String]& Loaders, System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModrinthFile]& Files, System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModDependency]& Dependencies) -> System.Void | instance public
M FMCore.Resources.ModrinthVersion.Equals(FMCore.Resources.ModrinthVersion other) -> System.Boolean | instance public
M FMCore.Resources.ModrinthVersion.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.ModrinthVersion.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.ModrinthVersion.ToString() -> System.String | instance public
M FMCore.Resources.ModrinthVersion.get_Dependencies() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModDependency] | instance public
M FMCore.Resources.ModrinthVersion.get_Files() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModrinthFile] | instance public
M FMCore.Resources.ModrinthVersion.get_GameVersions() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Resources.ModrinthVersion.get_Id() -> System.String | instance public
M FMCore.Resources.ModrinthVersion.get_Loaders() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Resources.ModrinthVersion.get_Name() -> System.String | instance public
M FMCore.Resources.ModrinthVersion.get_ProjectId() -> System.String | instance public
M FMCore.Resources.ModrinthVersion.get_PublishedAt() -> System.DateTimeOffset | instance public
M FMCore.Resources.ModrinthVersion.get_VersionNumber() -> System.String | instance public
M FMCore.Resources.ModrinthVersion.get_VersionType() -> System.String | instance public
M FMCore.Resources.ModrinthVersion.op_Equality(FMCore.Resources.ModrinthVersion left, FMCore.Resources.ModrinthVersion right) -> System.Boolean | static public
M FMCore.Resources.ModrinthVersion.op_Inequality(FMCore.Resources.ModrinthVersion left, FMCore.Resources.ModrinthVersion right) -> System.Boolean | static public
M FMCore.Resources.ModrinthVersion.set_Dependencies(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModDependency] value) -> System.Void | instance public
M FMCore.Resources.ModrinthVersion.set_Files(System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModrinthFile] value) -> System.Void | instance public
M FMCore.Resources.ModrinthVersion.set_GameVersions(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Resources.ModrinthVersion.set_Id(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthVersion.set_Loaders(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Resources.ModrinthVersion.set_Name(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthVersion.set_ProjectId(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthVersion.set_PublishedAt(System.DateTimeOffset value) -> System.Void | instance public
M FMCore.Resources.ModrinthVersion.set_VersionNumber(System.String value) -> System.Void | instance public
M FMCore.Resources.ModrinthVersion.set_VersionType(System.String value) -> System.Void | instance public
M FMCore.Resources.ResourceException..ctor(FMCore.Resources.ResourceErrorCode code, System.String message) -> constructor | instance public
M FMCore.Resources.ResourceException.get_Code() -> FMCore.Resources.ResourceErrorCode | instance public
M FMCore.Resources.ResourceException.get_DownloadError() -> System.Nullable`1[FMCore.Downloader.DownloadErrorCode] | instance public
M FMCore.Resources.ResourceException.get_RetryAfter() -> System.Nullable`1[System.TimeSpan] | instance public
M FMCore.Resources.ResourceException.get_StatusCode() -> System.Nullable`1[System.Int32] | instance public
M FMCore.Resources.ResourceException.set_DownloadError(System.Nullable`1[FMCore.Downloader.DownloadErrorCode] value) -> System.Void | instance public
M FMCore.Resources.ResourceException.set_RetryAfter(System.Nullable`1[System.TimeSpan] value) -> System.Void | instance public
M FMCore.Resources.ResourceException.set_StatusCode(System.Nullable`1[System.Int32] value) -> System.Void | instance public
M FMCore.Resources.ResourceProgress..ctor(FMCore.Resources.ResourceStage Stage, FMCore.Downloader.DownloadProgress Download = null) -> constructor | instance public
M FMCore.Resources.ResourceProgress.<Clone>$() -> FMCore.Resources.ResourceProgress | instance public
M FMCore.Resources.ResourceProgress.Deconstruct(FMCore.Resources.ResourceStage& Stage, FMCore.Downloader.DownloadProgress& Download) -> System.Void | instance public
M FMCore.Resources.ResourceProgress.Equals(FMCore.Resources.ResourceProgress other) -> System.Boolean | instance public
M FMCore.Resources.ResourceProgress.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Resources.ResourceProgress.GetHashCode() -> System.Int32 | instance public
M FMCore.Resources.ResourceProgress.ToString() -> System.String | instance public
M FMCore.Resources.ResourceProgress.get_Download() -> FMCore.Downloader.DownloadProgress | instance public
M FMCore.Resources.ResourceProgress.get_Stage() -> FMCore.Resources.ResourceStage | instance public
M FMCore.Resources.ResourceProgress.op_Equality(FMCore.Resources.ResourceProgress left, FMCore.Resources.ResourceProgress right) -> System.Boolean | static public
M FMCore.Resources.ResourceProgress.op_Inequality(FMCore.Resources.ResourceProgress left, FMCore.Resources.ResourceProgress right) -> System.Boolean | static public
M FMCore.Resources.ResourceProgress.set_Download(FMCore.Downloader.DownloadProgress value) -> System.Void | instance public
M FMCore.Resources.ResourceProgress.set_Stage(FMCore.Resources.ResourceStage value) -> System.Void | instance public
P FMCore.Resources.CurseForgeDependency.ModId : System.Int32 | get_ModId,set_ModId[init]
P FMCore.Resources.CurseForgeDependency.RelationType : FMCore.Resources.CurseForgeRelationType | get_RelationType,set_RelationType[init]
P FMCore.Resources.CurseForgeFile.Dependencies : System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeDependency] | get_Dependencies,set_Dependencies[init]
P FMCore.Resources.CurseForgeFile.DisplayName : System.String | get_DisplayName,set_DisplayName[init]
P FMCore.Resources.CurseForgeFile.DownloadUrl : System.Uri | get_DownloadUrl,set_DownloadUrl[init]
P FMCore.Resources.CurseForgeFile.ExposeAsAlternative : System.Boolean | get_ExposeAsAlternative,set_ExposeAsAlternative[init]
P FMCore.Resources.CurseForgeFile.FileName : System.String | get_FileName,set_FileName[init]
P FMCore.Resources.CurseForgeFile.GameId : System.Int32 | get_GameId,set_GameId[init]
P FMCore.Resources.CurseForgeFile.GameVersions : System.Collections.Generic.IReadOnlyList`1[System.String] | get_GameVersions,set_GameVersions[init]
P FMCore.Resources.CurseForgeFile.Id : System.Int32 | get_Id,set_Id[init]
P FMCore.Resources.CurseForgeFile.IsAvailable : System.Boolean | get_IsAvailable,set_IsAvailable[init]
P FMCore.Resources.CurseForgeFile.IsEarlyAccessContent : System.Boolean | get_IsEarlyAccessContent,set_IsEarlyAccessContent[init]
P FMCore.Resources.CurseForgeFile.IsServerPack : System.Boolean | get_IsServerPack,set_IsServerPack[init]
P FMCore.Resources.CurseForgeFile.Md5 : System.String | get_Md5,set_Md5[init]
P FMCore.Resources.CurseForgeFile.ModId : System.Int32 | get_ModId,set_ModId[init]
P FMCore.Resources.CurseForgeFile.PublishedAt : System.DateTimeOffset | get_PublishedAt,set_PublishedAt[init]
P FMCore.Resources.CurseForgeFile.ReleaseType : System.Int32 | get_ReleaseType,set_ReleaseType[init]
P FMCore.Resources.CurseForgeFile.Sha1 : System.String | get_Sha1,set_Sha1[init]
P FMCore.Resources.CurseForgeFile.Size : System.Int64 | get_Size,set_Size[init]
P FMCore.Resources.CurseForgeFilePage.Files : System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeFile] | get_Files,set_Files[init]
P FMCore.Resources.CurseForgeFilePage.Index : System.Int32 | get_Index,set_Index[init]
P FMCore.Resources.CurseForgeFilePage.PageSize : System.Int32 | get_PageSize,set_PageSize[init]
P FMCore.Resources.CurseForgeFilePage.ResultCount : System.Int32 | get_ResultCount,set_ResultCount[init]
P FMCore.Resources.CurseForgeFilePage.TotalCount : System.Int32 | get_TotalCount,set_TotalCount[init]
P FMCore.Resources.CurseForgeInstaller.GameDirectory : System.String | get_GameDirectory
P FMCore.Resources.CurseForgeProject.AllowModDistribution : System.Nullable`1[System.Boolean] | get_AllowModDistribution,set_AllowModDistribution[init]
P FMCore.Resources.CurseForgeProject.ClassId : System.Nullable`1[System.Int32] | get_ClassId,set_ClassId[init]
P FMCore.Resources.CurseForgeProject.GameId : System.Int32 | get_GameId,set_GameId[init]
P FMCore.Resources.CurseForgeProject.Id : System.Int32 | get_Id,set_Id[init]
P FMCore.Resources.CurseForgeProject.IsAvailable : System.Boolean | get_IsAvailable,set_IsAvailable[init]
P FMCore.Resources.CurseForgeProject.Name : System.String | get_Name,set_Name[init]
P FMCore.Resources.CurseForgeProject.Slug : System.String | get_Slug,set_Slug[init]
P FMCore.Resources.CurseForgeProject.Summary : System.String | get_Summary,set_Summary[init]
P FMCore.Resources.CurseForgeSearchOptions.CategoryId : System.Nullable`1[System.Int32] | get_CategoryId,set_CategoryId[init]
P FMCore.Resources.CurseForgeSearchOptions.ClassId : System.Int32 | get_ClassId,set_ClassId[init]
P FMCore.Resources.CurseForgeSearchOptions.Index : System.Int32 | get_Index,set_Index[init]
P FMCore.Resources.CurseForgeSearchOptions.Loader : System.String | get_Loader,set_Loader[init]
P FMCore.Resources.CurseForgeSearchOptions.MinecraftVersion : System.String | get_MinecraftVersion,set_MinecraftVersion[init]
P FMCore.Resources.CurseForgeSearchOptions.PageSize : System.Int32 | get_PageSize,set_PageSize[init]
P FMCore.Resources.CurseForgeSearchOptions.SortField : System.Int32 | get_SortField,set_SortField[init]
P FMCore.Resources.CurseForgeSearchOptions.SortOrder : System.String | get_SortOrder,set_SortOrder[init]
P FMCore.Resources.CurseForgeSearchResult.Index : System.Int32 | get_Index,set_Index[init]
P FMCore.Resources.CurseForgeSearchResult.Mods : System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.CurseForgeProject] | get_Mods,set_Mods[init]
P FMCore.Resources.CurseForgeSearchResult.PageSize : System.Int32 | get_PageSize,set_PageSize[init]
P FMCore.Resources.CurseForgeSearchResult.TotalCount : System.Int32 | get_TotalCount,set_TotalCount[init]
P FMCore.Resources.CurseForgeUpdate.Available : FMCore.Resources.CurseForgeFile | get_Available,set_Available[init]
P FMCore.Resources.CurseForgeUpdate.Installed : FMCore.Resources.InstalledMod | get_Installed,set_Installed[init]
P FMCore.Resources.InstalledMod.Dependencies : System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModDependency] | get_Dependencies,set_Dependencies[init]
P FMCore.Resources.InstalledMod.FileName : System.String | get_FileName,set_FileName[init]
P FMCore.Resources.InstalledMod.InstalledAt : System.DateTimeOffset | get_InstalledAt,set_InstalledAt[init]
P FMCore.Resources.InstalledMod.ProjectId : System.String | get_ProjectId,set_ProjectId[init]
P FMCore.Resources.InstalledMod.PublishedAt : System.DateTimeOffset | get_PublishedAt,set_PublishedAt[init]
P FMCore.Resources.InstalledMod.Sha512 : System.String | get_Sha512,set_Sha512[init]
P FMCore.Resources.InstalledMod.Size : System.Int64 | get_Size,set_Size[init]
P FMCore.Resources.InstalledMod.Source : System.String | get_Source,set_Source[init]
P FMCore.Resources.InstalledMod.VersionId : System.String | get_VersionId,set_VersionId[init]
P FMCore.Resources.InstalledMod.VersionNumber : System.String | get_VersionNumber,set_VersionNumber[init]
P FMCore.Resources.ModDependency.FileName : System.String | get_FileName,set_FileName[init]
P FMCore.Resources.ModDependency.ProjectId : System.String | get_ProjectId,set_ProjectId[init]
P FMCore.Resources.ModDependency.Type : FMCore.Resources.ModDependencyType | get_Type,set_Type[init]
P FMCore.Resources.ModDependency.VersionId : System.String | get_VersionId,set_VersionId[init]
P FMCore.Resources.ModEnvironment.IncludePrereleases : System.Boolean | get_IncludePrereleases,set_IncludePrereleases[init]
P FMCore.Resources.ModEnvironment.Loader : System.String | get_Loader,set_Loader[init]
P FMCore.Resources.ModEnvironment.MinecraftVersion : System.String | get_MinecraftVersion,set_MinecraftVersion[init]
P FMCore.Resources.ModInstallResult.Installed : System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.InstalledMod] | get_Installed,set_Installed[init]
P FMCore.Resources.ModInstallResult.Reused : System.Boolean | get_Reused,set_Reused[init]
P FMCore.Resources.ModUpdate.Available : FMCore.Resources.ModrinthVersion | get_Available,set_Available[init]
P FMCore.Resources.ModUpdate.Installed : FMCore.Resources.InstalledMod | get_Installed,set_Installed[init]
P FMCore.Resources.ModrinthFile.FileName : System.String | get_FileName,set_FileName[init]
P FMCore.Resources.ModrinthFile.FileType : System.String | get_FileType,set_FileType[init]
P FMCore.Resources.ModrinthFile.Primary : System.Boolean | get_Primary,set_Primary[init]
P FMCore.Resources.ModrinthFile.Sha1 : System.String | get_Sha1,set_Sha1[init]
P FMCore.Resources.ModrinthFile.Sha512 : System.String | get_Sha512,set_Sha512[init]
P FMCore.Resources.ModrinthFile.Size : System.Int64 | get_Size,set_Size[init]
P FMCore.Resources.ModrinthFile.Url : System.Uri | get_Url,set_Url[init]
P FMCore.Resources.ModrinthInstaller.GameDirectory : System.String | get_GameDirectory
P FMCore.Resources.ModrinthProject.Description : System.String | get_Description,set_Description[init]
P FMCore.Resources.ModrinthProject.Id : System.String | get_Id,set_Id[init]
P FMCore.Resources.ModrinthProject.ProjectType : System.String | get_ProjectType,set_ProjectType[init]
P FMCore.Resources.ModrinthProject.Slug : System.String | get_Slug,set_Slug[init]
P FMCore.Resources.ModrinthProject.Title : System.String | get_Title,set_Title[init]
P FMCore.Resources.ModrinthSearchOptions.Limit : System.Int32 | get_Limit,set_Limit[init]
P FMCore.Resources.ModrinthSearchOptions.Loader : System.String | get_Loader,set_Loader[init]
P FMCore.Resources.ModrinthSearchOptions.MinecraftVersion : System.String | get_MinecraftVersion,set_MinecraftVersion[init]
P FMCore.Resources.ModrinthSearchOptions.Offset : System.Int32 | get_Offset,set_Offset[init]
P FMCore.Resources.ModrinthSearchOptions.ProjectType : System.String | get_ProjectType,set_ProjectType[init]
P FMCore.Resources.ModrinthSearchOptions.Sort : System.String | get_Sort,set_Sort[init]
P FMCore.Resources.ModrinthSearchResult.Hits : System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModrinthProject] | get_Hits,set_Hits[init]
P FMCore.Resources.ModrinthSearchResult.Limit : System.Int32 | get_Limit,set_Limit[init]
P FMCore.Resources.ModrinthSearchResult.Offset : System.Int32 | get_Offset,set_Offset[init]
P FMCore.Resources.ModrinthSearchResult.TotalHits : System.Int32 | get_TotalHits,set_TotalHits[init]
P FMCore.Resources.ModrinthVersion.Dependencies : System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModDependency] | get_Dependencies,set_Dependencies[init]
P FMCore.Resources.ModrinthVersion.Files : System.Collections.Generic.IReadOnlyList`1[FMCore.Resources.ModrinthFile] | get_Files,set_Files[init]
P FMCore.Resources.ModrinthVersion.GameVersions : System.Collections.Generic.IReadOnlyList`1[System.String] | get_GameVersions,set_GameVersions[init]
P FMCore.Resources.ModrinthVersion.Id : System.String | get_Id,set_Id[init]
P FMCore.Resources.ModrinthVersion.Loaders : System.Collections.Generic.IReadOnlyList`1[System.String] | get_Loaders,set_Loaders[init]
P FMCore.Resources.ModrinthVersion.Name : System.String | get_Name,set_Name[init]
P FMCore.Resources.ModrinthVersion.ProjectId : System.String | get_ProjectId,set_ProjectId[init]
P FMCore.Resources.ModrinthVersion.PublishedAt : System.DateTimeOffset | get_PublishedAt,set_PublishedAt[init]
P FMCore.Resources.ModrinthVersion.VersionNumber : System.String | get_VersionNumber,set_VersionNumber[init]
P FMCore.Resources.ModrinthVersion.VersionType : System.String | get_VersionType,set_VersionType[init]
P FMCore.Resources.ResourceException.Code : FMCore.Resources.ResourceErrorCode | get_Code
P FMCore.Resources.ResourceException.DownloadError : System.Nullable`1[FMCore.Downloader.DownloadErrorCode] | get_DownloadError,set_DownloadError[init]
P FMCore.Resources.ResourceException.RetryAfter : System.Nullable`1[System.TimeSpan] | get_RetryAfter,set_RetryAfter[init]
P FMCore.Resources.ResourceException.StatusCode : System.Nullable`1[System.Int32] | get_StatusCode,set_StatusCode[init]
P FMCore.Resources.ResourceProgress.Download : FMCore.Downloader.DownloadProgress | get_Download,set_Download[init]
P FMCore.Resources.ResourceProgress.Stage : FMCore.Resources.ResourceStage | get_Stage,set_Stage[init]
T FMCore.Resources.CurseForgeClient : System.Object | System.IDisposable
T FMCore.Resources.CurseForgeDependency : System.Object | System.IEquatable`1[FMCore.Resources.CurseForgeDependency]
T FMCore.Resources.CurseForgeFile : System.Object | System.IEquatable`1[FMCore.Resources.CurseForgeFile]
T FMCore.Resources.CurseForgeFilePage : System.Object | System.IEquatable`1[FMCore.Resources.CurseForgeFilePage]
T FMCore.Resources.CurseForgeInstaller : System.Object |
T FMCore.Resources.CurseForgeProject : System.Object | System.IEquatable`1[FMCore.Resources.CurseForgeProject]
T FMCore.Resources.CurseForgeRelationType : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Resources.CurseForgeSearchOptions : System.Object | System.IEquatable`1[FMCore.Resources.CurseForgeSearchOptions]
T FMCore.Resources.CurseForgeSearchResult : System.Object | System.IEquatable`1[FMCore.Resources.CurseForgeSearchResult]
T FMCore.Resources.CurseForgeUpdate : System.Object | System.IEquatable`1[FMCore.Resources.CurseForgeUpdate]
T FMCore.Resources.InstalledMod : System.Object | System.IEquatable`1[FMCore.Resources.InstalledMod]
T FMCore.Resources.ModDependency : System.Object | System.IEquatable`1[FMCore.Resources.ModDependency]
T FMCore.Resources.ModDependencyType : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Resources.ModEnvironment : System.Object | System.IEquatable`1[FMCore.Resources.ModEnvironment]
T FMCore.Resources.ModInstallResult : System.Object | System.IEquatable`1[FMCore.Resources.ModInstallResult]
T FMCore.Resources.ModUpdate : System.Object | System.IEquatable`1[FMCore.Resources.ModUpdate]
T FMCore.Resources.ModrinthClient : System.Object |
T FMCore.Resources.ModrinthFile : System.Object | System.IEquatable`1[FMCore.Resources.ModrinthFile]
T FMCore.Resources.ModrinthInstaller : System.Object |
T FMCore.Resources.ModrinthProject : System.Object | System.IEquatable`1[FMCore.Resources.ModrinthProject]
T FMCore.Resources.ModrinthSearchOptions : System.Object | System.IEquatable`1[FMCore.Resources.ModrinthSearchOptions]
T FMCore.Resources.ModrinthSearchResult : System.Object | System.IEquatable`1[FMCore.Resources.ModrinthSearchResult]
T FMCore.Resources.ModrinthVersion : System.Object | System.IEquatable`1[FMCore.Resources.ModrinthVersion]
T FMCore.Resources.ResourceErrorCode : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Resources.ResourceException : System.Exception | System.Runtime.Serialization.ISerializable
T FMCore.Resources.ResourceProgress : System.Object | System.IEquatable`1[FMCore.Resources.ResourceProgress]
T FMCore.Resources.ResourceStage : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
```

## Versions

```text
F FMCore.Versions.VersionCachePolicy.CacheOnly : FMCore.Versions.VersionCachePolicy = 2
F FMCore.Versions.VersionCachePolicy.PreferCache : FMCore.Versions.VersionCachePolicy = 0
F FMCore.Versions.VersionCachePolicy.Refresh : FMCore.Versions.VersionCachePolicy = 1
F FMCore.Versions.VersionErrorCode.InheritanceCycle : FMCore.Versions.VersionErrorCode = 4
F FMCore.Versions.VersionErrorCode.InheritanceTooDeep : FMCore.Versions.VersionErrorCode = 5
F FMCore.Versions.VersionErrorCode.InvalidMetadata : FMCore.Versions.VersionErrorCode = 0
F FMCore.Versions.VersionErrorCode.IoError : FMCore.Versions.VersionErrorCode = 7
F FMCore.Versions.VersionErrorCode.NotCached : FMCore.Versions.VersionErrorCode = 3
F FMCore.Versions.VersionErrorCode.NotFound : FMCore.Versions.VersionErrorCode = 2
F FMCore.Versions.VersionErrorCode.RuleEvaluation : FMCore.Versions.VersionErrorCode = 6
F FMCore.Versions.VersionErrorCode.UnsafePath : FMCore.Versions.VersionErrorCode = 1
M FMCore.Versions.ArgumentValue..ctor(System.Collections.Generic.IReadOnlyList`1[System.String] Values, System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionRule] Rules) -> constructor | instance public
M FMCore.Versions.ArgumentValue.<Clone>$() -> FMCore.Versions.ArgumentValue | instance public
M FMCore.Versions.ArgumentValue.Deconstruct(System.Collections.Generic.IReadOnlyList`1[System.String]& Values, System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionRule]& Rules) -> System.Void | instance public
M FMCore.Versions.ArgumentValue.Equals(FMCore.Versions.ArgumentValue other) -> System.Boolean | instance public
M FMCore.Versions.ArgumentValue.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.ArgumentValue.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.ArgumentValue.ToString() -> System.String | instance public
M FMCore.Versions.ArgumentValue.get_Rules() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionRule] | instance public
M FMCore.Versions.ArgumentValue.get_Values() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Versions.ArgumentValue.op_Equality(FMCore.Versions.ArgumentValue left, FMCore.Versions.ArgumentValue right) -> System.Boolean | static public
M FMCore.Versions.ArgumentValue.op_Inequality(FMCore.Versions.ArgumentValue left, FMCore.Versions.ArgumentValue right) -> System.Boolean | static public
M FMCore.Versions.ArgumentValue.set_Rules(System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionRule] value) -> System.Void | instance public
M FMCore.Versions.ArgumentValue.set_Values(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Versions.AssetIndexInfo..ctor(System.String Id, FMCore.Versions.DownloadArtifact Download, System.Nullable`1[System.Int64] TotalSize) -> constructor | instance public
M FMCore.Versions.AssetIndexInfo.<Clone>$() -> FMCore.Versions.AssetIndexInfo | instance public
M FMCore.Versions.AssetIndexInfo.Deconstruct(System.String& Id, FMCore.Versions.DownloadArtifact& Download, System.Nullable`1[System.Int64]& TotalSize) -> System.Void | instance public
M FMCore.Versions.AssetIndexInfo.Equals(FMCore.Versions.AssetIndexInfo other) -> System.Boolean | instance public
M FMCore.Versions.AssetIndexInfo.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.AssetIndexInfo.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.AssetIndexInfo.ToString() -> System.String | instance public
M FMCore.Versions.AssetIndexInfo.get_Download() -> FMCore.Versions.DownloadArtifact | instance public
M FMCore.Versions.AssetIndexInfo.get_Id() -> System.String | instance public
M FMCore.Versions.AssetIndexInfo.get_TotalSize() -> System.Nullable`1[System.Int64] | instance public
M FMCore.Versions.AssetIndexInfo.op_Equality(FMCore.Versions.AssetIndexInfo left, FMCore.Versions.AssetIndexInfo right) -> System.Boolean | static public
M FMCore.Versions.AssetIndexInfo.op_Inequality(FMCore.Versions.AssetIndexInfo left, FMCore.Versions.AssetIndexInfo right) -> System.Boolean | static public
M FMCore.Versions.AssetIndexInfo.set_Download(FMCore.Versions.DownloadArtifact value) -> System.Void | instance public
M FMCore.Versions.AssetIndexInfo.set_Id(System.String value) -> System.Void | instance public
M FMCore.Versions.AssetIndexInfo.set_TotalSize(System.Nullable`1[System.Int64] value) -> System.Void | instance public
M FMCore.Versions.DownloadArtifact..ctor(System.Uri Url, System.String Sha1, System.Nullable`1[System.Int64] Size, System.String Path) -> constructor | instance public
M FMCore.Versions.DownloadArtifact.<Clone>$() -> FMCore.Versions.DownloadArtifact | instance public
M FMCore.Versions.DownloadArtifact.Deconstruct(System.Uri& Url, System.String& Sha1, System.Nullable`1[System.Int64]& Size, System.String& Path) -> System.Void | instance public
M FMCore.Versions.DownloadArtifact.Equals(FMCore.Versions.DownloadArtifact other) -> System.Boolean | instance public
M FMCore.Versions.DownloadArtifact.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.DownloadArtifact.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.DownloadArtifact.ToString() -> System.String | instance public
M FMCore.Versions.DownloadArtifact.get_IsLocalOnly() -> System.Boolean | instance public
M FMCore.Versions.DownloadArtifact.get_Path() -> System.String | instance public
M FMCore.Versions.DownloadArtifact.get_Sha1() -> System.String | instance public
M FMCore.Versions.DownloadArtifact.get_Size() -> System.Nullable`1[System.Int64] | instance public
M FMCore.Versions.DownloadArtifact.get_Url() -> System.Uri | instance public
M FMCore.Versions.DownloadArtifact.op_Equality(FMCore.Versions.DownloadArtifact left, FMCore.Versions.DownloadArtifact right) -> System.Boolean | static public
M FMCore.Versions.DownloadArtifact.op_Inequality(FMCore.Versions.DownloadArtifact left, FMCore.Versions.DownloadArtifact right) -> System.Boolean | static public
M FMCore.Versions.DownloadArtifact.set_Path(System.String value) -> System.Void | instance public
M FMCore.Versions.DownloadArtifact.set_Sha1(System.String value) -> System.Void | instance public
M FMCore.Versions.DownloadArtifact.set_Size(System.Nullable`1[System.Int64] value) -> System.Void | instance public
M FMCore.Versions.DownloadArtifact.set_Url(System.Uri value) -> System.Void | instance public
M FMCore.Versions.GameCoreInfo..ctor(System.String Id, System.String Type, System.String MinecraftVersion, System.String MainClass, FMCore.Versions.JavaRequirement JavaRequirement, System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.GameLoaderInfo] Loaders, System.String ClientJarPath, System.Boolean HasClientJar, FMCore.Versions.ResolvedVersion ResolvedVersion, System.Nullable`1[FMCore.Versions.VersionErrorCode] ErrorCode) -> constructor | instance public
M FMCore.Versions.GameCoreInfo.<Clone>$() -> FMCore.Versions.GameCoreInfo | instance public
M FMCore.Versions.GameCoreInfo.Deconstruct(System.String& Id, System.String& Type, System.String& MinecraftVersion, System.String& MainClass, FMCore.Versions.JavaRequirement& JavaRequirement, System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.GameLoaderInfo]& Loaders, System.String& ClientJarPath, System.Boolean& HasClientJar, FMCore.Versions.ResolvedVersion& ResolvedVersion, System.Nullable`1[FMCore.Versions.VersionErrorCode]& ErrorCode) -> System.Void | instance public
M FMCore.Versions.GameCoreInfo.Equals(FMCore.Versions.GameCoreInfo other) -> System.Boolean | instance public
M FMCore.Versions.GameCoreInfo.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.GameCoreInfo.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.GameCoreInfo.ToString() -> System.String | instance public
M FMCore.Versions.GameCoreInfo.get_ClientJarPath() -> System.String | instance public
M FMCore.Versions.GameCoreInfo.get_ErrorCode() -> System.Nullable`1[FMCore.Versions.VersionErrorCode] | instance public
M FMCore.Versions.GameCoreInfo.get_HasClientJar() -> System.Boolean | instance public
M FMCore.Versions.GameCoreInfo.get_Id() -> System.String | instance public
M FMCore.Versions.GameCoreInfo.get_JavaRequirement() -> FMCore.Versions.JavaRequirement | instance public
M FMCore.Versions.GameCoreInfo.get_Loaders() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.GameLoaderInfo] | instance public
M FMCore.Versions.GameCoreInfo.get_MainClass() -> System.String | instance public
M FMCore.Versions.GameCoreInfo.get_MinecraftVersion() -> System.String | instance public
M FMCore.Versions.GameCoreInfo.get_ResolvedVersion() -> FMCore.Versions.ResolvedVersion | instance public
M FMCore.Versions.GameCoreInfo.get_Type() -> System.String | instance public
M FMCore.Versions.GameCoreInfo.op_Equality(FMCore.Versions.GameCoreInfo left, FMCore.Versions.GameCoreInfo right) -> System.Boolean | static public
M FMCore.Versions.GameCoreInfo.op_Inequality(FMCore.Versions.GameCoreInfo left, FMCore.Versions.GameCoreInfo right) -> System.Boolean | static public
M FMCore.Versions.GameCoreInfo.set_ClientJarPath(System.String value) -> System.Void | instance public
M FMCore.Versions.GameCoreInfo.set_ErrorCode(System.Nullable`1[FMCore.Versions.VersionErrorCode] value) -> System.Void | instance public
M FMCore.Versions.GameCoreInfo.set_HasClientJar(System.Boolean value) -> System.Void | instance public
M FMCore.Versions.GameCoreInfo.set_Id(System.String value) -> System.Void | instance public
M FMCore.Versions.GameCoreInfo.set_JavaRequirement(FMCore.Versions.JavaRequirement value) -> System.Void | instance public
M FMCore.Versions.GameCoreInfo.set_Loaders(System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.GameLoaderInfo] value) -> System.Void | instance public
M FMCore.Versions.GameCoreInfo.set_MainClass(System.String value) -> System.Void | instance public
M FMCore.Versions.GameCoreInfo.set_MinecraftVersion(System.String value) -> System.Void | instance public
M FMCore.Versions.GameCoreInfo.set_ResolvedVersion(FMCore.Versions.ResolvedVersion value) -> System.Void | instance public
M FMCore.Versions.GameCoreInfo.set_Type(System.String value) -> System.Void | instance public
M FMCore.Versions.GameCoreUtil.GetAvailableVersionsAsync(System.String root = ".minecraft", FMCore.Versions.VersionCachePolicy cachePolicy = PreferCache, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Versions.VersionManifest] | static public
M FMCore.Versions.GameCoreUtil.GetGameCore(System.String versionId, System.String root = ".minecraft") -> FMCore.Versions.GameCoreInfo | static public
M FMCore.Versions.GameCoreUtil.GetGameCoreAsync(System.String versionId, System.String root = ".minecraft", System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Versions.GameCoreInfo] | static public
M FMCore.Versions.GameCoreUtil.GetGameCores(System.String root = ".minecraft") -> System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.GameCoreInfo] | static public
M FMCore.Versions.GameCoreUtil.GetGameCoresAsync(System.String root = ".minecraft", System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.GameCoreInfo]] | static public
M FMCore.Versions.GameLoaderInfo..ctor(System.String Name, System.String Version) -> constructor | instance public
M FMCore.Versions.GameLoaderInfo.<Clone>$() -> FMCore.Versions.GameLoaderInfo | instance public
M FMCore.Versions.GameLoaderInfo.Deconstruct(System.String& Name, System.String& Version) -> System.Void | instance public
M FMCore.Versions.GameLoaderInfo.Equals(FMCore.Versions.GameLoaderInfo other) -> System.Boolean | instance public
M FMCore.Versions.GameLoaderInfo.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.GameLoaderInfo.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.GameLoaderInfo.ToString() -> System.String | instance public
M FMCore.Versions.GameLoaderInfo.get_Name() -> System.String | instance public
M FMCore.Versions.GameLoaderInfo.get_Version() -> System.String | instance public
M FMCore.Versions.GameLoaderInfo.op_Equality(FMCore.Versions.GameLoaderInfo left, FMCore.Versions.GameLoaderInfo right) -> System.Boolean | static public
M FMCore.Versions.GameLoaderInfo.op_Inequality(FMCore.Versions.GameLoaderInfo left, FMCore.Versions.GameLoaderInfo right) -> System.Boolean | static public
M FMCore.Versions.GameLoaderInfo.set_Name(System.String value) -> System.Void | instance public
M FMCore.Versions.GameLoaderInfo.set_Version(System.String value) -> System.Void | instance public
M FMCore.Versions.GeneratedFile..ctor(System.String RelativePath, System.String Sha1, System.Int64 Size) -> constructor | instance public
M FMCore.Versions.GeneratedFile.<Clone>$() -> FMCore.Versions.GeneratedFile | instance public
M FMCore.Versions.GeneratedFile.Deconstruct(System.String& RelativePath, System.String& Sha1, System.Int64& Size) -> System.Void | instance public
M FMCore.Versions.GeneratedFile.Equals(FMCore.Versions.GeneratedFile other) -> System.Boolean | instance public
M FMCore.Versions.GeneratedFile.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.GeneratedFile.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.GeneratedFile.ToString() -> System.String | instance public
M FMCore.Versions.GeneratedFile.get_RelativePath() -> System.String | instance public
M FMCore.Versions.GeneratedFile.get_Sha1() -> System.String | instance public
M FMCore.Versions.GeneratedFile.get_Size() -> System.Int64 | instance public
M FMCore.Versions.GeneratedFile.op_Equality(FMCore.Versions.GeneratedFile left, FMCore.Versions.GeneratedFile right) -> System.Boolean | static public
M FMCore.Versions.GeneratedFile.op_Inequality(FMCore.Versions.GeneratedFile left, FMCore.Versions.GeneratedFile right) -> System.Boolean | static public
M FMCore.Versions.GeneratedFile.set_RelativePath(System.String value) -> System.Void | instance public
M FMCore.Versions.GeneratedFile.set_Sha1(System.String value) -> System.Void | instance public
M FMCore.Versions.GeneratedFile.set_Size(System.Int64 value) -> System.Void | instance public
M FMCore.Versions.JavaRequirement..ctor(System.String Component, System.Int32 MajorVersion) -> constructor | instance public
M FMCore.Versions.JavaRequirement.<Clone>$() -> FMCore.Versions.JavaRequirement | instance public
M FMCore.Versions.JavaRequirement.Deconstruct(System.String& Component, System.Int32& MajorVersion) -> System.Void | instance public
M FMCore.Versions.JavaRequirement.Equals(FMCore.Versions.JavaRequirement other) -> System.Boolean | instance public
M FMCore.Versions.JavaRequirement.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.JavaRequirement.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.JavaRequirement.ToString() -> System.String | instance public
M FMCore.Versions.JavaRequirement.get_Component() -> System.String | instance public
M FMCore.Versions.JavaRequirement.get_MajorVersion() -> System.Int32 | instance public
M FMCore.Versions.JavaRequirement.op_Equality(FMCore.Versions.JavaRequirement left, FMCore.Versions.JavaRequirement right) -> System.Boolean | static public
M FMCore.Versions.JavaRequirement.op_Inequality(FMCore.Versions.JavaRequirement left, FMCore.Versions.JavaRequirement right) -> System.Boolean | static public
M FMCore.Versions.JavaRequirement.set_Component(System.String value) -> System.Void | instance public
M FMCore.Versions.JavaRequirement.set_MajorVersion(System.Int32 value) -> System.Void | instance public
M FMCore.Versions.LibraryMetadata..ctor(System.String Name, FMCore.Versions.MavenCoordinate Coordinate, FMCore.Versions.DownloadArtifact Artifact, System.Collections.Generic.IReadOnlyDictionary`2[System.String,FMCore.Versions.DownloadArtifact] Classifiers, System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.String] Natives, System.Collections.Generic.IReadOnlyList`1[System.String] ExtractExcludes, System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionRule] Rules) -> constructor | instance public
M FMCore.Versions.LibraryMetadata.<Clone>$() -> FMCore.Versions.LibraryMetadata | instance public
M FMCore.Versions.LibraryMetadata.Deconstruct(System.String& Name, FMCore.Versions.MavenCoordinate& Coordinate, FMCore.Versions.DownloadArtifact& Artifact, System.Collections.Generic.IReadOnlyDictionary`2[System.String,FMCore.Versions.DownloadArtifact]& Classifiers, System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.String]& Natives, System.Collections.Generic.IReadOnlyList`1[System.String]& ExtractExcludes, System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionRule]& Rules) -> System.Void | instance public
M FMCore.Versions.LibraryMetadata.Equals(FMCore.Versions.LibraryMetadata other) -> System.Boolean | instance public
M FMCore.Versions.LibraryMetadata.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.LibraryMetadata.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.LibraryMetadata.GetNativeClassifier(FMCore.Versions.RuleContext context) -> System.String | instance public
M FMCore.Versions.LibraryMetadata.ToString() -> System.String | instance public
M FMCore.Versions.LibraryMetadata.get_Artifact() -> FMCore.Versions.DownloadArtifact | instance public
M FMCore.Versions.LibraryMetadata.get_Classifiers() -> System.Collections.Generic.IReadOnlyDictionary`2[System.String,FMCore.Versions.DownloadArtifact] | instance public
M FMCore.Versions.LibraryMetadata.get_Coordinate() -> FMCore.Versions.MavenCoordinate | instance public
M FMCore.Versions.LibraryMetadata.get_ExtractExcludes() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Versions.LibraryMetadata.get_Name() -> System.String | instance public
M FMCore.Versions.LibraryMetadata.get_Natives() -> System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.String] | instance public
M FMCore.Versions.LibraryMetadata.get_Rules() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionRule] | instance public
M FMCore.Versions.LibraryMetadata.op_Equality(FMCore.Versions.LibraryMetadata left, FMCore.Versions.LibraryMetadata right) -> System.Boolean | static public
M FMCore.Versions.LibraryMetadata.op_Inequality(FMCore.Versions.LibraryMetadata left, FMCore.Versions.LibraryMetadata right) -> System.Boolean | static public
M FMCore.Versions.LibraryMetadata.set_Artifact(FMCore.Versions.DownloadArtifact value) -> System.Void | instance public
M FMCore.Versions.LibraryMetadata.set_Classifiers(System.Collections.Generic.IReadOnlyDictionary`2[System.String,FMCore.Versions.DownloadArtifact] value) -> System.Void | instance public
M FMCore.Versions.LibraryMetadata.set_Coordinate(FMCore.Versions.MavenCoordinate value) -> System.Void | instance public
M FMCore.Versions.LibraryMetadata.set_ExtractExcludes(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Versions.LibraryMetadata.set_Name(System.String value) -> System.Void | instance public
M FMCore.Versions.LibraryMetadata.set_Natives(System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.String] value) -> System.Void | instance public
M FMCore.Versions.LibraryMetadata.set_Rules(System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionRule] value) -> System.Void | instance public
M FMCore.Versions.LocalVersionInfo..ctor(System.String Id, System.String Type, System.String MetadataPath, System.String ClientJarId, System.String ClientJarPath, System.Boolean HasClientJar, System.Nullable`1[FMCore.Versions.VersionErrorCode] ErrorCode) -> constructor | instance public
M FMCore.Versions.LocalVersionInfo.<Clone>$() -> FMCore.Versions.LocalVersionInfo | instance public
M FMCore.Versions.LocalVersionInfo.Deconstruct(System.String& Id, System.String& Type, System.String& MetadataPath, System.String& ClientJarId, System.String& ClientJarPath, System.Boolean& HasClientJar, System.Nullable`1[FMCore.Versions.VersionErrorCode]& ErrorCode) -> System.Void | instance public
M FMCore.Versions.LocalVersionInfo.Equals(FMCore.Versions.LocalVersionInfo other) -> System.Boolean | instance public
M FMCore.Versions.LocalVersionInfo.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.LocalVersionInfo.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.LocalVersionInfo.ToString() -> System.String | instance public
M FMCore.Versions.LocalVersionInfo.get_ClientJarId() -> System.String | instance public
M FMCore.Versions.LocalVersionInfo.get_ClientJarPath() -> System.String | instance public
M FMCore.Versions.LocalVersionInfo.get_ErrorCode() -> System.Nullable`1[FMCore.Versions.VersionErrorCode] | instance public
M FMCore.Versions.LocalVersionInfo.get_HasClientJar() -> System.Boolean | instance public
M FMCore.Versions.LocalVersionInfo.get_Id() -> System.String | instance public
M FMCore.Versions.LocalVersionInfo.get_MetadataPath() -> System.String | instance public
M FMCore.Versions.LocalVersionInfo.get_Type() -> System.String | instance public
M FMCore.Versions.LocalVersionInfo.op_Equality(FMCore.Versions.LocalVersionInfo left, FMCore.Versions.LocalVersionInfo right) -> System.Boolean | static public
M FMCore.Versions.LocalVersionInfo.op_Inequality(FMCore.Versions.LocalVersionInfo left, FMCore.Versions.LocalVersionInfo right) -> System.Boolean | static public
M FMCore.Versions.LocalVersionInfo.set_ClientJarId(System.String value) -> System.Void | instance public
M FMCore.Versions.LocalVersionInfo.set_ClientJarPath(System.String value) -> System.Void | instance public
M FMCore.Versions.LocalVersionInfo.set_ErrorCode(System.Nullable`1[FMCore.Versions.VersionErrorCode] value) -> System.Void | instance public
M FMCore.Versions.LocalVersionInfo.set_HasClientJar(System.Boolean value) -> System.Void | instance public
M FMCore.Versions.LocalVersionInfo.set_Id(System.String value) -> System.Void | instance public
M FMCore.Versions.LocalVersionInfo.set_MetadataPath(System.String value) -> System.Void | instance public
M FMCore.Versions.LocalVersionInfo.set_Type(System.String value) -> System.Void | instance public
M FMCore.Versions.LoggingConfiguration..ctor(System.String Argument, System.String Type, System.String FileId, FMCore.Versions.DownloadArtifact File) -> constructor | instance public
M FMCore.Versions.LoggingConfiguration.<Clone>$() -> FMCore.Versions.LoggingConfiguration | instance public
M FMCore.Versions.LoggingConfiguration.Deconstruct(System.String& Argument, System.String& Type, System.String& FileId, FMCore.Versions.DownloadArtifact& File) -> System.Void | instance public
M FMCore.Versions.LoggingConfiguration.Equals(FMCore.Versions.LoggingConfiguration other) -> System.Boolean | instance public
M FMCore.Versions.LoggingConfiguration.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.LoggingConfiguration.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.LoggingConfiguration.ToString() -> System.String | instance public
M FMCore.Versions.LoggingConfiguration.get_Argument() -> System.String | instance public
M FMCore.Versions.LoggingConfiguration.get_File() -> FMCore.Versions.DownloadArtifact | instance public
M FMCore.Versions.LoggingConfiguration.get_FileId() -> System.String | instance public
M FMCore.Versions.LoggingConfiguration.get_Type() -> System.String | instance public
M FMCore.Versions.LoggingConfiguration.op_Equality(FMCore.Versions.LoggingConfiguration left, FMCore.Versions.LoggingConfiguration right) -> System.Boolean | static public
M FMCore.Versions.LoggingConfiguration.op_Inequality(FMCore.Versions.LoggingConfiguration left, FMCore.Versions.LoggingConfiguration right) -> System.Boolean | static public
M FMCore.Versions.LoggingConfiguration.set_Argument(System.String value) -> System.Void | instance public
M FMCore.Versions.LoggingConfiguration.set_File(FMCore.Versions.DownloadArtifact value) -> System.Void | instance public
M FMCore.Versions.LoggingConfiguration.set_FileId(System.String value) -> System.Void | instance public
M FMCore.Versions.LoggingConfiguration.set_Type(System.String value) -> System.Void | instance public
M FMCore.Versions.MavenCoordinate..ctor(System.String Group, System.String Artifact, System.String Version, System.String Classifier, System.String Extension) -> constructor | instance public
M FMCore.Versions.MavenCoordinate.<Clone>$() -> FMCore.Versions.MavenCoordinate | instance public
M FMCore.Versions.MavenCoordinate.Deconstruct(System.String& Group, System.String& Artifact, System.String& Version, System.String& Classifier, System.String& Extension) -> System.Void | instance public
M FMCore.Versions.MavenCoordinate.Equals(FMCore.Versions.MavenCoordinate other) -> System.Boolean | instance public
M FMCore.Versions.MavenCoordinate.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.MavenCoordinate.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.MavenCoordinate.Parse(System.String name) -> FMCore.Versions.MavenCoordinate | static public
M FMCore.Versions.MavenCoordinate.ToString() -> System.String | instance public
M FMCore.Versions.MavenCoordinate.get_Artifact() -> System.String | instance public
M FMCore.Versions.MavenCoordinate.get_Classifier() -> System.String | instance public
M FMCore.Versions.MavenCoordinate.get_Extension() -> System.String | instance public
M FMCore.Versions.MavenCoordinate.get_Group() -> System.String | instance public
M FMCore.Versions.MavenCoordinate.get_RelativePath() -> System.String | instance public
M FMCore.Versions.MavenCoordinate.get_Version() -> System.String | instance public
M FMCore.Versions.MavenCoordinate.op_Equality(FMCore.Versions.MavenCoordinate left, FMCore.Versions.MavenCoordinate right) -> System.Boolean | static public
M FMCore.Versions.MavenCoordinate.op_Inequality(FMCore.Versions.MavenCoordinate left, FMCore.Versions.MavenCoordinate right) -> System.Boolean | static public
M FMCore.Versions.MavenCoordinate.set_Artifact(System.String value) -> System.Void | instance public
M FMCore.Versions.MavenCoordinate.set_Classifier(System.String value) -> System.Void | instance public
M FMCore.Versions.MavenCoordinate.set_Extension(System.String value) -> System.Void | instance public
M FMCore.Versions.MavenCoordinate.set_Group(System.String value) -> System.Void | instance public
M FMCore.Versions.MavenCoordinate.set_Version(System.String value) -> System.Void | instance public
M FMCore.Versions.ResolvedVersion..ctor(FMCore.Versions.VersionMetadata Metadata, System.String ClientJarId, FMCore.Versions.DownloadArtifact ClientDownload, System.Collections.Generic.IReadOnlyList`1[System.String] InheritanceChain) -> constructor | instance public
M FMCore.Versions.ResolvedVersion.<Clone>$() -> FMCore.Versions.ResolvedVersion | instance public
M FMCore.Versions.ResolvedVersion.Deconstruct(FMCore.Versions.VersionMetadata& Metadata, System.String& ClientJarId, FMCore.Versions.DownloadArtifact& ClientDownload, System.Collections.Generic.IReadOnlyList`1[System.String]& InheritanceChain) -> System.Void | instance public
M FMCore.Versions.ResolvedVersion.Equals(FMCore.Versions.ResolvedVersion other) -> System.Boolean | instance public
M FMCore.Versions.ResolvedVersion.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.ResolvedVersion.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.ResolvedVersion.ToString() -> System.String | instance public
M FMCore.Versions.ResolvedVersion.get_ClientDownload() -> FMCore.Versions.DownloadArtifact | instance public
M FMCore.Versions.ResolvedVersion.get_ClientJarId() -> System.String | instance public
M FMCore.Versions.ResolvedVersion.get_InheritanceChain() -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Versions.ResolvedVersion.get_Metadata() -> FMCore.Versions.VersionMetadata | instance public
M FMCore.Versions.ResolvedVersion.op_Equality(FMCore.Versions.ResolvedVersion left, FMCore.Versions.ResolvedVersion right) -> System.Boolean | static public
M FMCore.Versions.ResolvedVersion.op_Inequality(FMCore.Versions.ResolvedVersion left, FMCore.Versions.ResolvedVersion right) -> System.Boolean | static public
M FMCore.Versions.ResolvedVersion.set_ClientDownload(FMCore.Versions.DownloadArtifact value) -> System.Void | instance public
M FMCore.Versions.ResolvedVersion.set_ClientJarId(System.String value) -> System.Void | instance public
M FMCore.Versions.ResolvedVersion.set_InheritanceChain(System.Collections.Generic.IReadOnlyList`1[System.String] value) -> System.Void | instance public
M FMCore.Versions.ResolvedVersion.set_Metadata(FMCore.Versions.VersionMetadata value) -> System.Void | instance public
M FMCore.Versions.RuleContext..ctor(System.String OperatingSystem, System.String Architecture, System.String OperatingSystemVersion, System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.Boolean] Features = null) -> constructor | instance public
M FMCore.Versions.RuleContext.<Clone>$() -> FMCore.Versions.RuleContext | instance public
M FMCore.Versions.RuleContext.Deconstruct(System.String& OperatingSystem, System.String& Architecture, System.String& OperatingSystemVersion, System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.Boolean]& Features) -> System.Void | instance public
M FMCore.Versions.RuleContext.Equals(FMCore.Versions.RuleContext other) -> System.Boolean | instance public
M FMCore.Versions.RuleContext.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.RuleContext.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.RuleContext.ToString() -> System.String | instance public
M FMCore.Versions.RuleContext.get_Architecture() -> System.String | instance public
M FMCore.Versions.RuleContext.get_Current() -> FMCore.Versions.RuleContext | static public
M FMCore.Versions.RuleContext.get_Features() -> System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.Boolean] | instance public
M FMCore.Versions.RuleContext.get_Is64Bit() -> System.Boolean | instance public
M FMCore.Versions.RuleContext.get_OperatingSystem() -> System.String | instance public
M FMCore.Versions.RuleContext.get_OperatingSystemVersion() -> System.String | instance public
M FMCore.Versions.RuleContext.op_Equality(FMCore.Versions.RuleContext left, FMCore.Versions.RuleContext right) -> System.Boolean | static public
M FMCore.Versions.RuleContext.op_Inequality(FMCore.Versions.RuleContext left, FMCore.Versions.RuleContext right) -> System.Boolean | static public
M FMCore.Versions.RuleContext.set_Architecture(System.String value) -> System.Void | instance public
M FMCore.Versions.RuleContext.set_Features(System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.Boolean] value) -> System.Void | instance public
M FMCore.Versions.RuleContext.set_OperatingSystem(System.String value) -> System.Void | instance public
M FMCore.Versions.RuleContext.set_OperatingSystemVersion(System.String value) -> System.Void | instance public
M FMCore.Versions.RuleEvaluator.IsAllowed(System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionRule] rules, FMCore.Versions.RuleContext context) -> System.Boolean | static public
M FMCore.Versions.VersionArguments..ctor(System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.ArgumentValue] Jvm, System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.ArgumentValue] Game, System.Boolean HasJvmArguments) -> constructor | instance public
M FMCore.Versions.VersionArguments.<Clone>$() -> FMCore.Versions.VersionArguments | instance public
M FMCore.Versions.VersionArguments.Deconstruct(System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.ArgumentValue]& Jvm, System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.ArgumentValue]& Game, System.Boolean& HasJvmArguments) -> System.Void | instance public
M FMCore.Versions.VersionArguments.Equals(FMCore.Versions.VersionArguments other) -> System.Boolean | instance public
M FMCore.Versions.VersionArguments.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.VersionArguments.GetGameArguments(FMCore.Versions.RuleContext context) -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Versions.VersionArguments.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.VersionArguments.GetJvmArguments(FMCore.Versions.RuleContext context) -> System.Collections.Generic.IReadOnlyList`1[System.String] | instance public
M FMCore.Versions.VersionArguments.ToString() -> System.String | instance public
M FMCore.Versions.VersionArguments.get_Game() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.ArgumentValue] | instance public
M FMCore.Versions.VersionArguments.get_HasJvmArguments() -> System.Boolean | instance public
M FMCore.Versions.VersionArguments.get_Jvm() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.ArgumentValue] | instance public
M FMCore.Versions.VersionArguments.op_Equality(FMCore.Versions.VersionArguments left, FMCore.Versions.VersionArguments right) -> System.Boolean | static public
M FMCore.Versions.VersionArguments.op_Inequality(FMCore.Versions.VersionArguments left, FMCore.Versions.VersionArguments right) -> System.Boolean | static public
M FMCore.Versions.VersionArguments.set_Game(System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.ArgumentValue] value) -> System.Void | instance public
M FMCore.Versions.VersionArguments.set_HasJvmArguments(System.Boolean value) -> System.Void | instance public
M FMCore.Versions.VersionArguments.set_Jvm(System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.ArgumentValue] value) -> System.Void | instance public
M FMCore.Versions.VersionEntry..ctor(System.String Id, System.String Type, System.Uri Url, System.String Sha1, System.Nullable`1[System.DateTimeOffset] ReleaseTime) -> constructor | instance public
M FMCore.Versions.VersionEntry.<Clone>$() -> FMCore.Versions.VersionEntry | instance public
M FMCore.Versions.VersionEntry.Deconstruct(System.String& Id, System.String& Type, System.Uri& Url, System.String& Sha1, System.Nullable`1[System.DateTimeOffset]& ReleaseTime) -> System.Void | instance public
M FMCore.Versions.VersionEntry.Equals(FMCore.Versions.VersionEntry other) -> System.Boolean | instance public
M FMCore.Versions.VersionEntry.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.VersionEntry.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.VersionEntry.ToString() -> System.String | instance public
M FMCore.Versions.VersionEntry.get_Id() -> System.String | instance public
M FMCore.Versions.VersionEntry.get_ReleaseTime() -> System.Nullable`1[System.DateTimeOffset] | instance public
M FMCore.Versions.VersionEntry.get_Sha1() -> System.String | instance public
M FMCore.Versions.VersionEntry.get_Type() -> System.String | instance public
M FMCore.Versions.VersionEntry.get_Url() -> System.Uri | instance public
M FMCore.Versions.VersionEntry.op_Equality(FMCore.Versions.VersionEntry left, FMCore.Versions.VersionEntry right) -> System.Boolean | static public
M FMCore.Versions.VersionEntry.op_Inequality(FMCore.Versions.VersionEntry left, FMCore.Versions.VersionEntry right) -> System.Boolean | static public
M FMCore.Versions.VersionEntry.set_Id(System.String value) -> System.Void | instance public
M FMCore.Versions.VersionEntry.set_ReleaseTime(System.Nullable`1[System.DateTimeOffset] value) -> System.Void | instance public
M FMCore.Versions.VersionEntry.set_Sha1(System.String value) -> System.Void | instance public
M FMCore.Versions.VersionEntry.set_Type(System.String value) -> System.Void | instance public
M FMCore.Versions.VersionEntry.set_Url(System.Uri value) -> System.Void | instance public
M FMCore.Versions.VersionException.get_Code() -> FMCore.Versions.VersionErrorCode | instance public
M FMCore.Versions.VersionManager..ctor(FMCore.Downloader.DownloadManager downloads, System.Uri manifestUri = null) -> constructor | instance public
M FMCore.Versions.VersionManager.GetLocalVersionsAsync(System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.LocalVersionInfo]] | instance public
M FMCore.Versions.VersionManager.GetManifestAsync(FMCore.Versions.VersionCachePolicy cachePolicy = PreferCache, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Versions.VersionManifest] | instance public
M FMCore.Versions.VersionManager.GetMetadataPath(System.String id) -> System.String | instance public
M FMCore.Versions.VersionManager.GetVersionAsync(System.String id, FMCore.Versions.VersionCachePolicy cachePolicy = PreferCache, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Versions.VersionMetadata] | instance public
M FMCore.Versions.VersionManager.ResolveAsync(System.String id, FMCore.Versions.VersionCachePolicy cachePolicy = PreferCache, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task`1[FMCore.Versions.ResolvedVersion] | instance public
M FMCore.Versions.VersionManager.SaveLocalAsync(FMCore.Versions.VersionMetadata metadata, System.Threading.CancellationToken cancellationToken = default) -> System.Threading.Tasks.Task | instance public
M FMCore.Versions.VersionManager.get_OfficialManifestUri() -> System.Uri | static public
M FMCore.Versions.VersionManager.get_RootDirectory() -> System.String | instance public
M FMCore.Versions.VersionManifest..ctor(System.String LatestRelease, System.String LatestSnapshot, System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionEntry] Versions) -> constructor | instance public
M FMCore.Versions.VersionManifest.<Clone>$() -> FMCore.Versions.VersionManifest | instance public
M FMCore.Versions.VersionManifest.Deconstruct(System.String& LatestRelease, System.String& LatestSnapshot, System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionEntry]& Versions) -> System.Void | instance public
M FMCore.Versions.VersionManifest.Equals(FMCore.Versions.VersionManifest other) -> System.Boolean | instance public
M FMCore.Versions.VersionManifest.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.VersionManifest.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.VersionManifest.ToString() -> System.String | instance public
M FMCore.Versions.VersionManifest.get_LatestRelease() -> System.String | instance public
M FMCore.Versions.VersionManifest.get_LatestSnapshot() -> System.String | instance public
M FMCore.Versions.VersionManifest.get_Versions() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionEntry] | instance public
M FMCore.Versions.VersionManifest.op_Equality(FMCore.Versions.VersionManifest left, FMCore.Versions.VersionManifest right) -> System.Boolean | static public
M FMCore.Versions.VersionManifest.op_Inequality(FMCore.Versions.VersionManifest left, FMCore.Versions.VersionManifest right) -> System.Boolean | static public
M FMCore.Versions.VersionManifest.set_LatestRelease(System.String value) -> System.Void | instance public
M FMCore.Versions.VersionManifest.set_LatestSnapshot(System.String value) -> System.Void | instance public
M FMCore.Versions.VersionManifest.set_Versions(System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionEntry] value) -> System.Void | instance public
M FMCore.Versions.VersionMetadata.GetLibraries(FMCore.Versions.RuleContext context) -> System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.LibraryMetadata] | instance public
M FMCore.Versions.VersionMetadata.ToJson() -> System.Text.Json.Nodes.JsonObject | instance public
M FMCore.Versions.VersionMetadata.ToString() -> System.String | instance public
M FMCore.Versions.VersionMetadata.get_Arguments() -> FMCore.Versions.VersionArguments | instance public
M FMCore.Versions.VersionMetadata.get_AssetIndex() -> FMCore.Versions.AssetIndexInfo | instance public
M FMCore.Versions.VersionMetadata.get_Assets() -> System.String | instance public
M FMCore.Versions.VersionMetadata.get_Downloads() -> System.Collections.Generic.IReadOnlyDictionary`2[System.String,FMCore.Versions.DownloadArtifact] | instance public
M FMCore.Versions.VersionMetadata.get_GeneratedFiles() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.GeneratedFile] | instance public
M FMCore.Versions.VersionMetadata.get_Id() -> System.String | instance public
M FMCore.Versions.VersionMetadata.get_InheritsFrom() -> System.String | instance public
M FMCore.Versions.VersionMetadata.get_Jar() -> System.String | instance public
M FMCore.Versions.VersionMetadata.get_Java() -> FMCore.Versions.JavaRequirement | instance public
M FMCore.Versions.VersionMetadata.get_LegacyGameArguments() -> System.String | instance public
M FMCore.Versions.VersionMetadata.get_Libraries() -> System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.LibraryMetadata] | instance public
M FMCore.Versions.VersionMetadata.get_Logging() -> FMCore.Versions.LoggingConfiguration | instance public
M FMCore.Versions.VersionMetadata.get_MainClass() -> System.String | instance public
M FMCore.Versions.VersionMetadata.get_Type() -> System.String | instance public
M FMCore.Versions.VersionParser.Parse(System.String json) -> FMCore.Versions.VersionMetadata | static public
M FMCore.Versions.VersionParser.ParseManifest(System.String json) -> FMCore.Versions.VersionManifest | static public
M FMCore.Versions.VersionRule..ctor(System.Boolean Allow, System.String OperatingSystem, System.String Architecture, System.String OperatingSystemVersion, System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.Boolean] Features) -> constructor | instance public
M FMCore.Versions.VersionRule.<Clone>$() -> FMCore.Versions.VersionRule | instance public
M FMCore.Versions.VersionRule.Deconstruct(System.Boolean& Allow, System.String& OperatingSystem, System.String& Architecture, System.String& OperatingSystemVersion, System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.Boolean]& Features) -> System.Void | instance public
M FMCore.Versions.VersionRule.Equals(FMCore.Versions.VersionRule other) -> System.Boolean | instance public
M FMCore.Versions.VersionRule.Equals(System.Object obj) -> System.Boolean | instance public
M FMCore.Versions.VersionRule.GetHashCode() -> System.Int32 | instance public
M FMCore.Versions.VersionRule.ToString() -> System.String | instance public
M FMCore.Versions.VersionRule.get_Allow() -> System.Boolean | instance public
M FMCore.Versions.VersionRule.get_Architecture() -> System.String | instance public
M FMCore.Versions.VersionRule.get_Features() -> System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.Boolean] | instance public
M FMCore.Versions.VersionRule.get_OperatingSystem() -> System.String | instance public
M FMCore.Versions.VersionRule.get_OperatingSystemVersion() -> System.String | instance public
M FMCore.Versions.VersionRule.op_Equality(FMCore.Versions.VersionRule left, FMCore.Versions.VersionRule right) -> System.Boolean | static public
M FMCore.Versions.VersionRule.op_Inequality(FMCore.Versions.VersionRule left, FMCore.Versions.VersionRule right) -> System.Boolean | static public
M FMCore.Versions.VersionRule.set_Allow(System.Boolean value) -> System.Void | instance public
M FMCore.Versions.VersionRule.set_Architecture(System.String value) -> System.Void | instance public
M FMCore.Versions.VersionRule.set_Features(System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.Boolean] value) -> System.Void | instance public
M FMCore.Versions.VersionRule.set_OperatingSystem(System.String value) -> System.Void | instance public
M FMCore.Versions.VersionRule.set_OperatingSystemVersion(System.String value) -> System.Void | instance public
P FMCore.Versions.ArgumentValue.Rules : System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionRule] | get_Rules,set_Rules[init]
P FMCore.Versions.ArgumentValue.Values : System.Collections.Generic.IReadOnlyList`1[System.String] | get_Values,set_Values[init]
P FMCore.Versions.AssetIndexInfo.Download : FMCore.Versions.DownloadArtifact | get_Download,set_Download[init]
P FMCore.Versions.AssetIndexInfo.Id : System.String | get_Id,set_Id[init]
P FMCore.Versions.AssetIndexInfo.TotalSize : System.Nullable`1[System.Int64] | get_TotalSize,set_TotalSize[init]
P FMCore.Versions.DownloadArtifact.IsLocalOnly : System.Boolean | get_IsLocalOnly
P FMCore.Versions.DownloadArtifact.Path : System.String | get_Path,set_Path[init]
P FMCore.Versions.DownloadArtifact.Sha1 : System.String | get_Sha1,set_Sha1[init]
P FMCore.Versions.DownloadArtifact.Size : System.Nullable`1[System.Int64] | get_Size,set_Size[init]
P FMCore.Versions.DownloadArtifact.Url : System.Uri | get_Url,set_Url[init]
P FMCore.Versions.GameCoreInfo.ClientJarPath : System.String | get_ClientJarPath,set_ClientJarPath[init]
P FMCore.Versions.GameCoreInfo.ErrorCode : System.Nullable`1[FMCore.Versions.VersionErrorCode] | get_ErrorCode,set_ErrorCode[init]
P FMCore.Versions.GameCoreInfo.HasClientJar : System.Boolean | get_HasClientJar,set_HasClientJar[init]
P FMCore.Versions.GameCoreInfo.Id : System.String | get_Id,set_Id[init]
P FMCore.Versions.GameCoreInfo.JavaRequirement : FMCore.Versions.JavaRequirement | get_JavaRequirement,set_JavaRequirement[init]
P FMCore.Versions.GameCoreInfo.Loaders : System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.GameLoaderInfo] | get_Loaders,set_Loaders[init]
P FMCore.Versions.GameCoreInfo.MainClass : System.String | get_MainClass,set_MainClass[init]
P FMCore.Versions.GameCoreInfo.MinecraftVersion : System.String | get_MinecraftVersion,set_MinecraftVersion[init]
P FMCore.Versions.GameCoreInfo.ResolvedVersion : FMCore.Versions.ResolvedVersion | get_ResolvedVersion,set_ResolvedVersion[init]
P FMCore.Versions.GameCoreInfo.Type : System.String | get_Type,set_Type[init]
P FMCore.Versions.GameLoaderInfo.Name : System.String | get_Name,set_Name[init]
P FMCore.Versions.GameLoaderInfo.Version : System.String | get_Version,set_Version[init]
P FMCore.Versions.GeneratedFile.RelativePath : System.String | get_RelativePath,set_RelativePath[init]
P FMCore.Versions.GeneratedFile.Sha1 : System.String | get_Sha1,set_Sha1[init]
P FMCore.Versions.GeneratedFile.Size : System.Int64 | get_Size,set_Size[init]
P FMCore.Versions.JavaRequirement.Component : System.String | get_Component,set_Component[init]
P FMCore.Versions.JavaRequirement.MajorVersion : System.Int32 | get_MajorVersion,set_MajorVersion[init]
P FMCore.Versions.LibraryMetadata.Artifact : FMCore.Versions.DownloadArtifact | get_Artifact,set_Artifact[init]
P FMCore.Versions.LibraryMetadata.Classifiers : System.Collections.Generic.IReadOnlyDictionary`2[System.String,FMCore.Versions.DownloadArtifact] | get_Classifiers,set_Classifiers[init]
P FMCore.Versions.LibraryMetadata.Coordinate : FMCore.Versions.MavenCoordinate | get_Coordinate,set_Coordinate[init]
P FMCore.Versions.LibraryMetadata.ExtractExcludes : System.Collections.Generic.IReadOnlyList`1[System.String] | get_ExtractExcludes,set_ExtractExcludes[init]
P FMCore.Versions.LibraryMetadata.Name : System.String | get_Name,set_Name[init]
P FMCore.Versions.LibraryMetadata.Natives : System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.String] | get_Natives,set_Natives[init]
P FMCore.Versions.LibraryMetadata.Rules : System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionRule] | get_Rules,set_Rules[init]
P FMCore.Versions.LocalVersionInfo.ClientJarId : System.String | get_ClientJarId,set_ClientJarId[init]
P FMCore.Versions.LocalVersionInfo.ClientJarPath : System.String | get_ClientJarPath,set_ClientJarPath[init]
P FMCore.Versions.LocalVersionInfo.ErrorCode : System.Nullable`1[FMCore.Versions.VersionErrorCode] | get_ErrorCode,set_ErrorCode[init]
P FMCore.Versions.LocalVersionInfo.HasClientJar : System.Boolean | get_HasClientJar,set_HasClientJar[init]
P FMCore.Versions.LocalVersionInfo.Id : System.String | get_Id,set_Id[init]
P FMCore.Versions.LocalVersionInfo.MetadataPath : System.String | get_MetadataPath,set_MetadataPath[init]
P FMCore.Versions.LocalVersionInfo.Type : System.String | get_Type,set_Type[init]
P FMCore.Versions.LoggingConfiguration.Argument : System.String | get_Argument,set_Argument[init]
P FMCore.Versions.LoggingConfiguration.File : FMCore.Versions.DownloadArtifact | get_File,set_File[init]
P FMCore.Versions.LoggingConfiguration.FileId : System.String | get_FileId,set_FileId[init]
P FMCore.Versions.LoggingConfiguration.Type : System.String | get_Type,set_Type[init]
P FMCore.Versions.MavenCoordinate.Artifact : System.String | get_Artifact,set_Artifact[init]
P FMCore.Versions.MavenCoordinate.Classifier : System.String | get_Classifier,set_Classifier[init]
P FMCore.Versions.MavenCoordinate.Extension : System.String | get_Extension,set_Extension[init]
P FMCore.Versions.MavenCoordinate.Group : System.String | get_Group,set_Group[init]
P FMCore.Versions.MavenCoordinate.RelativePath : System.String | get_RelativePath
P FMCore.Versions.MavenCoordinate.Version : System.String | get_Version,set_Version[init]
P FMCore.Versions.ResolvedVersion.ClientDownload : FMCore.Versions.DownloadArtifact | get_ClientDownload,set_ClientDownload[init]
P FMCore.Versions.ResolvedVersion.ClientJarId : System.String | get_ClientJarId,set_ClientJarId[init]
P FMCore.Versions.ResolvedVersion.InheritanceChain : System.Collections.Generic.IReadOnlyList`1[System.String] | get_InheritanceChain,set_InheritanceChain[init]
P FMCore.Versions.ResolvedVersion.Metadata : FMCore.Versions.VersionMetadata | get_Metadata,set_Metadata[init]
P FMCore.Versions.RuleContext.Architecture : System.String | get_Architecture,set_Architecture[init]
P FMCore.Versions.RuleContext.Current : FMCore.Versions.RuleContext | get_Current
P FMCore.Versions.RuleContext.Features : System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.Boolean] | get_Features,set_Features[init]
P FMCore.Versions.RuleContext.Is64Bit : System.Boolean | get_Is64Bit
P FMCore.Versions.RuleContext.OperatingSystem : System.String | get_OperatingSystem,set_OperatingSystem[init]
P FMCore.Versions.RuleContext.OperatingSystemVersion : System.String | get_OperatingSystemVersion,set_OperatingSystemVersion[init]
P FMCore.Versions.VersionArguments.Game : System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.ArgumentValue] | get_Game,set_Game[init]
P FMCore.Versions.VersionArguments.HasJvmArguments : System.Boolean | get_HasJvmArguments,set_HasJvmArguments[init]
P FMCore.Versions.VersionArguments.Jvm : System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.ArgumentValue] | get_Jvm,set_Jvm[init]
P FMCore.Versions.VersionEntry.Id : System.String | get_Id,set_Id[init]
P FMCore.Versions.VersionEntry.ReleaseTime : System.Nullable`1[System.DateTimeOffset] | get_ReleaseTime,set_ReleaseTime[init]
P FMCore.Versions.VersionEntry.Sha1 : System.String | get_Sha1,set_Sha1[init]
P FMCore.Versions.VersionEntry.Type : System.String | get_Type,set_Type[init]
P FMCore.Versions.VersionEntry.Url : System.Uri | get_Url,set_Url[init]
P FMCore.Versions.VersionException.Code : FMCore.Versions.VersionErrorCode | get_Code
P FMCore.Versions.VersionManager.OfficialManifestUri : System.Uri | get_OfficialManifestUri
P FMCore.Versions.VersionManager.RootDirectory : System.String | get_RootDirectory
P FMCore.Versions.VersionManifest.LatestRelease : System.String | get_LatestRelease,set_LatestRelease[init]
P FMCore.Versions.VersionManifest.LatestSnapshot : System.String | get_LatestSnapshot,set_LatestSnapshot[init]
P FMCore.Versions.VersionManifest.Versions : System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.VersionEntry] | get_Versions,set_Versions[init]
P FMCore.Versions.VersionMetadata.Arguments : FMCore.Versions.VersionArguments | get_Arguments
P FMCore.Versions.VersionMetadata.AssetIndex : FMCore.Versions.AssetIndexInfo | get_AssetIndex
P FMCore.Versions.VersionMetadata.Assets : System.String | get_Assets
P FMCore.Versions.VersionMetadata.Downloads : System.Collections.Generic.IReadOnlyDictionary`2[System.String,FMCore.Versions.DownloadArtifact] | get_Downloads
P FMCore.Versions.VersionMetadata.GeneratedFiles : System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.GeneratedFile] | get_GeneratedFiles
P FMCore.Versions.VersionMetadata.Id : System.String | get_Id
P FMCore.Versions.VersionMetadata.InheritsFrom : System.String | get_InheritsFrom
P FMCore.Versions.VersionMetadata.Jar : System.String | get_Jar
P FMCore.Versions.VersionMetadata.Java : FMCore.Versions.JavaRequirement | get_Java
P FMCore.Versions.VersionMetadata.LegacyGameArguments : System.String | get_LegacyGameArguments
P FMCore.Versions.VersionMetadata.Libraries : System.Collections.Generic.IReadOnlyList`1[FMCore.Versions.LibraryMetadata] | get_Libraries
P FMCore.Versions.VersionMetadata.Logging : FMCore.Versions.LoggingConfiguration | get_Logging
P FMCore.Versions.VersionMetadata.MainClass : System.String | get_MainClass
P FMCore.Versions.VersionMetadata.Type : System.String | get_Type
P FMCore.Versions.VersionRule.Allow : System.Boolean | get_Allow,set_Allow[init]
P FMCore.Versions.VersionRule.Architecture : System.String | get_Architecture,set_Architecture[init]
P FMCore.Versions.VersionRule.Features : System.Collections.Generic.IReadOnlyDictionary`2[System.String,System.Boolean] | get_Features,set_Features[init]
P FMCore.Versions.VersionRule.OperatingSystem : System.String | get_OperatingSystem,set_OperatingSystem[init]
P FMCore.Versions.VersionRule.OperatingSystemVersion : System.String | get_OperatingSystemVersion,set_OperatingSystemVersion[init]
T FMCore.Versions.ArgumentValue : System.Object | System.IEquatable`1[FMCore.Versions.ArgumentValue]
T FMCore.Versions.AssetIndexInfo : System.Object | System.IEquatable`1[FMCore.Versions.AssetIndexInfo]
T FMCore.Versions.DownloadArtifact : System.Object | System.IEquatable`1[FMCore.Versions.DownloadArtifact]
T FMCore.Versions.GameCoreInfo : System.Object | System.IEquatable`1[FMCore.Versions.GameCoreInfo]
T FMCore.Versions.GameCoreUtil : System.Object |
T FMCore.Versions.GameLoaderInfo : System.Object | System.IEquatable`1[FMCore.Versions.GameLoaderInfo]
T FMCore.Versions.GeneratedFile : System.Object | System.IEquatable`1[FMCore.Versions.GeneratedFile]
T FMCore.Versions.JavaRequirement : System.Object | System.IEquatable`1[FMCore.Versions.JavaRequirement]
T FMCore.Versions.LibraryMetadata : System.Object | System.IEquatable`1[FMCore.Versions.LibraryMetadata]
T FMCore.Versions.LocalVersionInfo : System.Object | System.IEquatable`1[FMCore.Versions.LocalVersionInfo]
T FMCore.Versions.LoggingConfiguration : System.Object | System.IEquatable`1[FMCore.Versions.LoggingConfiguration]
T FMCore.Versions.MavenCoordinate : System.Object | System.IEquatable`1[FMCore.Versions.MavenCoordinate]
T FMCore.Versions.ResolvedVersion : System.Object | System.IEquatable`1[FMCore.Versions.ResolvedVersion]
T FMCore.Versions.RuleContext : System.Object | System.IEquatable`1[FMCore.Versions.RuleContext]
T FMCore.Versions.RuleEvaluator : System.Object |
T FMCore.Versions.VersionArguments : System.Object | System.IEquatable`1[FMCore.Versions.VersionArguments]
T FMCore.Versions.VersionCachePolicy : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Versions.VersionEntry : System.Object | System.IEquatable`1[FMCore.Versions.VersionEntry]
T FMCore.Versions.VersionErrorCode : System.Enum | System.IComparable, System.IConvertible, System.IFormattable, System.ISpanFormattable
T FMCore.Versions.VersionException : System.Exception | System.Runtime.Serialization.ISerializable
T FMCore.Versions.VersionManager : System.Object |
T FMCore.Versions.VersionManifest : System.Object | System.IEquatable`1[FMCore.Versions.VersionManifest]
T FMCore.Versions.VersionMetadata : System.Object |
T FMCore.Versions.VersionParser : System.Object |
T FMCore.Versions.VersionRule : System.Object | System.IEquatable`1[FMCore.Versions.VersionRule]
```

## 根入口

```text
T FMCore.FmCore : System.Object |
```
