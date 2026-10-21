using System.Text.Json.Nodes;
using FMCore.Authentication;
using FMCore.Tests.Fixtures;
using static FMCore.Tests.Fixtures.AuthenticationTestContext;

namespace FMCore.Tests.Integration;

public sealed class AccountManagementTests
{
    [Fact]
    public async Task OnlineAndOfflineAccountsCoexistAndDefaultSurvivesRestart()
    {
        using var context = new AuthenticationTestContext();
        var offline = await context.Manager.CreateOfflineAccountAsync("MockPlayer", Ct);
        var online = await context.LoginAsync();
        Assert.Equal(offline.Name, online.Account.Name);
        Assert.NotEqual(offline.Uuid, online.Account.Uuid);
        Assert.NotEqual(offline.Id, online.Account.Id);
        var state = await context.Manager.GetAccountsAsync(Ct);
        Assert.Equal(2, state.Accounts.Count);
        Assert.Equal(offline.Id, state.DefaultAccountId);
        Assert.Equal(online.Account.Id, state.SelectedAccountId);
        await context.Manager.SetDefaultAsync(online.Account.Id, Ct);
        await context.Manager.SwitchAsync(offline.Id, Ct);
        Assert.Equal(offline.Id, (await context.Manager.GetAccountsAsync(Ct)).SelectedAccountId);
        using var reopened = context.Reopen();
        Assert.Equal(online.Account.Id, (await reopened.GetAccountsAsync(Ct)).SelectedAccountId);
        var requests = context.Requests.Count;
        await context.Manager.SwitchAsync(online.Account.Id, Ct);
        Assert.Equal(requests, context.Requests.Count);
    }

    [Fact]
    public async Task DeletesSecretsAndReselectsRemainingDefaultThenHandlesEmptyState()
    {
        using var context = new AuthenticationTestContext();
        var offline = await context.Manager.CreateOfflineAccountAsync("OfflinePlayer", Ct);
        var online = await context.LoginAsync();
        Assert.True(await context.Manager.DeleteAsync(online.Account.Id, Ct));
        Assert.Empty(context.Tokens.Values);
        Assert.Equal(offline.Id, (await context.Manager.GetAccountsAsync(Ct)).SelectedAccountId);
        Assert.False((await context.Manager.GetLaunchIdentityAsync(cancellationToken: Ct)).IsAuthenticated);
        Assert.True(await context.Manager.DeleteAsync(offline.Id, Ct));
        var empty = await context.Manager.GetAccountsAsync(Ct);
        Assert.Empty(empty.Accounts);
        Assert.Null(empty.SelectedAccountId);
        Assert.Null(empty.DefaultAccountId);
        Assert.False(await context.Manager.DeleteAsync(offline.Id, Ct));
        Assert.Equal(AuthenticationErrorCode.AccountNotFound, (await Assert.ThrowsAsync<AuthenticationException>(() =>
            context.Manager.GetLaunchIdentityAsync(cancellationToken: Ct))).Code);
    }

    [Fact]
    public async Task FailedSecretDeletionKeepsAccountAvailableForRetry()
    {
        using var context = new AuthenticationTestContext();
        var online = await context.LoginAsync();
        context.Tokens.FailDelete = true;
        Assert.Equal(AuthenticationErrorCode.StorageError, (await Assert.ThrowsAsync<AuthenticationException>(() =>
            context.Manager.DeleteAsync(online.Account.Id, Ct))).Code);
        Assert.Single((await context.Manager.GetAccountsAsync(Ct)).Accounts);
        context.Tokens.FailDelete = false;
        Assert.True(await context.Manager.DeleteAsync(online.Account.Id, Ct));
    }

    [Fact]
    public async Task ConcurrentOfflineCreatesDoNotLoseAccounts()
    {
        using var context = new AuthenticationTestContext();
        await Task.WhenAll(Enumerable.Range(0, 12).Select(i => context.Manager.CreateOfflineAccountAsync("Player_" + i, Ct)));
        Assert.Equal(12, (await context.Manager.GetAccountsAsync(Ct)).Accounts.Count);
        context.AssertNoTemporaryFiles();
    }

    [Fact]
    public async Task DeviceCodeWaitDoesNotLockOutLocalAccountOperations()
    {
        using var context = new AuthenticationTestContext();
        var displaying = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var login = context.Manager.LoginMicrosoftAsync(Options, async (_, ct) =>
        { displaying.TrySetResult(); await release.Task.WaitAsync(ct); }, cancellationToken: Ct);
        await displaying.Task.WaitAsync(TimeSpan.FromSeconds(5), Ct);
        try
        {
            await context.Manager.CreateOfflineAccountAsync("LocalPlayer", Ct);
            Assert.Single((await context.Manager.GetAccountsAsync(Ct)).Accounts);
        }
        finally { release.TrySetResult(); }
        await login;
        Assert.Equal(2, (await context.Manager.GetAccountsAsync(Ct)).Accounts.Count);
    }

    [Fact]
    public async Task EventsDescribeCommittedStateAndFaultySubscriberCannotBreakSave()
    {
        using var context = new AuthenticationTestContext();
        var events = new List<AccountChangedEventArgs>();
        context.Manager.AccountsChanged += (_, _) => throw new InvalidOperationException("host UI failure");
        context.Manager.AccountsChanged += (_, args) => events.Add(args);
        var account = await context.Manager.CreateOfflineAccountAsync("Player", Ct);
        await context.Manager.SwitchAsync(account.Id, Ct);
        await context.Manager.SetDefaultAsync(null, Ct);
        await context.Manager.DeleteAsync(account.Id, Ct);
        Assert.Equal([AccountChangeKind.Added, AccountChangeKind.Selected, AccountChangeKind.DefaultChanged, AccountChangeKind.Removed], events.Select(e => e.Change));
        Assert.Single(events[0].Snapshot.Accounts);
        Assert.Empty(events[^1].Snapshot.Accounts);
        Assert.Empty((await context.Manager.GetAccountsAsync(Ct)).Accounts);
    }

    [Fact]
    public async Task UnknownAccountIdsHaveExplicitErrors()
    {
        using var context = new AuthenticationTestContext();
        var id = Guid.NewGuid();
        Assert.Equal(AuthenticationErrorCode.AccountNotFound, (await Assert.ThrowsAsync<AuthenticationException>(() => context.Manager.SwitchAsync(id, Ct))).Code);
        Assert.Equal(AuthenticationErrorCode.AccountNotFound, (await Assert.ThrowsAsync<AuthenticationException>(() => context.Manager.SetDefaultAsync(id, Ct))).Code);
        Assert.Equal(AuthenticationErrorCode.AccountNotFound, (await Assert.ThrowsAsync<AuthenticationException>(() => context.Manager.RefreshAsync(id, cancellationToken: Ct))).Code);
    }

    [Fact]
    public async Task CorruptPublicStoreIsNotOverwrittenWithAnEmptyList()
    {
        using var context = new AuthenticationTestContext();
        await context.Manager.CreateOfflineAccountAsync("Player", Ct);
        var original = await File.ReadAllTextAsync(context.At("accounts.json"), Ct);
        var document = JsonNode.Parse(original)!;
        document["accounts"]![0]!["uuid"] = Guid.NewGuid().ToString();
        var corrupted = document.ToJsonString();
        await File.WriteAllTextAsync(context.At("accounts.json"), corrupted, Ct);
        Assert.Equal(AuthenticationErrorCode.StorageCorrupted, (await Assert.ThrowsAsync<AuthenticationException>(() => context.Manager.GetAccountsAsync(Ct))).Code);
        Assert.Equal(corrupted, await File.ReadAllTextAsync(context.At("accounts.json"), Ct));
    }

    [Fact]
    public async Task ExclusiveStoreLeaseRejectsConcurrentManagerMutation()
    {
        using var context = new AuthenticationTestContext();
        using var lease = new FileStream(context.At("accounts.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        Assert.Equal(AuthenticationErrorCode.StoreBusy, (await Assert.ThrowsAsync<AuthenticationException>(() =>
            context.Manager.CreateOfflineAccountAsync("Player", Ct))).Code);
    }

    [Fact]
    public async Task CancellationBeforeMutationDoesNotCreateOrModifyAnAccount()
    {
        using var context = new AuthenticationTestContext();
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Manager.CreateOfflineAccountAsync("Player", cancellation.Token));
        Assert.Empty((await context.Manager.GetAccountsAsync(Ct)).Accounts);
    }
}
