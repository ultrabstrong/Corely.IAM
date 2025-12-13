using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.Security.Providers;

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

    public async Task<GetAccountResult> GetAccountAsync(int accountId) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            accountId
        )
            ? await _inner.GetAccountAsync(accountId)
            : new GetAccountResult(
                GetAccountResultCode.UnauthorizedError,
                $"Unauthorized to read account {accountId}",
                null
            );

    public async Task<GetAccountResult> GetAccountAsync(string accountName) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.ACCOUNT_RESOURCE_TYPE
        )
            ? await _inner.GetAccountAsync(accountName)
            : new GetAccountResult(
                GetAccountResultCode.UnauthorizedError,
                $"Unauthorized to read account {accountName}",
                null
            );

    public async Task<ListAccountsForUserResult> ListAccountsForUserAsync(int userId) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.ACCOUNT_RESOURCE_TYPE
        )
            ? await _inner.ListAccountsForUserAsync(userId)
            : new ListAccountsForUserResult(
                ListAccountsForUserResultCode.UnauthorizedError,
                $"Unauthorized to list accounts for user {userId}",
                []
            );

    public async Task<DeleteAccountResult> DeleteAccountAsync(int accountId) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Delete,
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            accountId
        )
            ? await _inner.DeleteAccountAsync(accountId)
            : new DeleteAccountResult(
                DeleteAccountResultCode.UnauthorizedError,
                $"Unauthorized to delete account {accountId}"
            );

    public async Task<AddUserToAccountResult> AddUserToAccountAsync(
        AddUserToAccountRequest request
    ) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            request.AccountId
        )
            ? await _inner.AddUserToAccountAsync(request)
            : new AddUserToAccountResult(
                AddUserToAccountResultCode.UnauthorizedError,
                $"Unauthorized to update account {request.AccountId}"
            );

    public async Task<RemoveUserFromAccountResult> RemoveUserFromAccountAsync(
        RemoveUserFromAccountRequest request
    ) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            request.AccountId
        )
            ? await _inner.RemoveUserFromAccountAsync(request)
            : new RemoveUserFromAccountResult(
                RemoveUserFromAccountResultCode.UnauthorizedError,
                $"Unauthorized to update account {request.AccountId}"
            );
}
