using FMCore.Authentication;

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };
var ct = cancellation.Token;
using var accounts = new AccountManager(".fmcore/accounts");
var clientId = Environment.GetEnvironmentVariable("FMCORE_MICROSOFT_CLIENT_ID") ?? throw new InvalidOperationException("请设置 FMCORE_MICROSOFT_CLIENT_ID。");
var progress = new Progress<AuthenticationProgress>(p => Console.WriteLine(p.Stage));
try { var identity = await accounts.LoginMicrosoftAsync(new(clientId), (code, token) => { Console.WriteLine($"打开 {code.VerificationUri}，输入设备授权码 {code.UserCode}"); return Task.CompletedTask; }, progress, ct); Console.WriteLine(identity.Account.Name); }
catch (OperationCanceledException) { Console.WriteLine("已取消。"); }
catch (AuthenticationException e) { Console.WriteLine($"{e.Code}: {e.Message}"); }
