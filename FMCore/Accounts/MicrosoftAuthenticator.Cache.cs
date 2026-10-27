using Microsoft.Identity.Client;

namespace FMCore.Accounts;

public sealed partial class MicrosoftAuthenticator
{
    private async Task<AuthenticationResult?>
        TryRestoreAuthorizationAsync(
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IAccount[] accounts =
            (await _application.GetAccountsAsync()
                .ConfigureAwait(false))
            .Take(2)
            .ToArray();

        cancellationToken.ThrowIfCancellationRequested();

        // 多账号时交给用户重新授权，不擅自选中一个。
        if (accounts.Length != 1)
            return null;

        try
        {
            return await _application.AcquireTokenSilent(
                    ["XboxLive.signin"],
                    accounts[0])
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (MsalUiRequiredException)
        {
            return null;
        }
    }
}
