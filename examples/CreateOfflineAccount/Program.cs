using FMCore.Accounts;

if (args.Length != 1)
{
    Console.Error.WriteLine("用法：CreateOfflineAccount <用户名>");
    return 2;
}
OfflineAccount account = OfflineAccount.Create(args[0]);
Console.WriteLine($"用户名：{account.Username}\nUUID：{account.Uuid}");
return 0;
