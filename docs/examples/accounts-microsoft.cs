using FMCore.Authentication;

using var accounts = new AccountManager(".fmcore/accounts");
var clientId = Environment.GetEnvironmentVariable("FMCORE_MICROSOFT_CLIENT_ID") ?? throw new InvalidOperationException("请设置 FMCORE_MICROSOFT_CLIENT_ID。");
var identity = await accounts.LoginMicrosoftAsync(new(clientId), (code, token) => { Console.WriteLine($"打开 {code.VerificationUri}，输入设备授权码 {code.UserCode}"); return Task.CompletedTask; });
Console.WriteLine(identity.Account.Name);
