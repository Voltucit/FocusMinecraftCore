using FMCore.Authentication;

using var accounts = new AccountManager(".fmcore/accounts");
var id = Guid.Parse(Environment.GetEnvironmentVariable("FMCORE_ACCOUNT_ID") ?? throw new InvalidOperationException("请设置 FMCORE_ACCOUNT_ID。"));
await accounts.RefreshAsync(id);
