using FMCore.Authentication;

using var accounts = new AccountManager(".fmcore/accounts");
var identity = await accounts.LoginOfflineAsync("Player");
Console.WriteLine(identity.Account.Name);
