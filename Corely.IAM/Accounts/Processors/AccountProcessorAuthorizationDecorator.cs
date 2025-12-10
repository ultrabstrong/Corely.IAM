using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Processors;

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

    public async Task<DeleteAccountResult> DeleteAccountAsync(int accountId)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            AuthAction.Delete,
            accountId
        );
        return await _inner.DeleteAccountAsync(accountId);
    }

    public async Task<AddUserToAccountResult> AddUserToAccountAsync(AddUserToAccountRequest request)
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            AuthAction.Update,
            request.AccountId
        );
        return await _inner.AddUserToAccountAsync(request);
    }

    public async Task<RemoveUserFromAccountResult> RemoveUserFromAccountAsync(
        RemoveUserFromAccountRequest request
    )
    {
        await _authorizationProvider.AuthorizeAsync(
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            AuthAction.Update,
            request.AccountId
        );
        return await _inner.RemoveUserFromAccountAsync(request);
    }
}
