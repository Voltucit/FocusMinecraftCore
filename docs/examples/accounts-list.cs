using FMCore.Authentication;

using var accounts = new AccountManager(".fmcore/accounts");
var snapshot = await accounts.GetAccountsAsync();
foreach (var account in snapshot.Accounts) Console.WriteLine($"{account.Id}: {account.Name}");
