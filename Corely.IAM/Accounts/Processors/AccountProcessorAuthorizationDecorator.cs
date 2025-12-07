using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Auth.Constants;
using Corely.IAM.Auth.Providers;
using Corely.IAM.Permissions.Constants;

namespace Corely.IAM.Accounts.Processors;

internal class AccountProcessorAuthorizationDecorator(
    IAccountProcessor inner,
    IAuthorizationProvider authorizationProvider
) : IAccountProcessor
{
    private readonly IAccountProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public Task<CreateAccountResult> CreateAccountAsync(CreateAccountRequest request) =>
        _inner.CreateAccountAsync(request);

    public async Task<Account?> GetAccountAsync(int accountId)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            AuthAction.Read,
            accountId
        );
        return await _inner.GetAccountAsync(accountId);
    }

    public async Task<Account?> GetAccountAsync(string accountName)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            AuthAction.Read
        );
        return await _inner.GetAccountAsync(accountName);
    }
}
