using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Filtering;
using Corely.IAM.Filtering.Ordering;
using Corely.IAM.Models;
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

    public async Task<GetAccountResult> GetAccountAsync(Guid accountId) =>
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

    public async Task<DeleteAccountResult> DeleteAccountAsync(Guid accountId) =>
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
        _authorizationProvider.IsAuthorizedForOwnUser(request.UserId, true) // users can de-register themselves
        || await _authorizationProvider.IsAuthorizedAsync( // users with update access to account can de-register other users
            AuthAction.Update,
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            request.AccountId
        )
            ? await _inner.RemoveUserFromAccountAsync(request)
            : new RemoveUserFromAccountResult(
                RemoveUserFromAccountResultCode.UnauthorizedError,
                $"Unauthorized to update account {request.AccountId}"
            );

    public async Task<ListResult<Account>> ListAccountsAsync(
        FilterBuilder<Account>? filter,
        OrderBuilder<Account>? order,
        int skip,
        int take
    ) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.ACCOUNT_RESOURCE_TYPE
        )
            ? await _inner.ListAccountsAsync(filter, order, skip, take)
            : new ListResult<Account>(
                RetrieveResultCode.UnauthorizedError,
                "Unauthorized to list accounts",
                null
            );

    public async Task<GetResult<Account>> GetAccountByIdAsync(Guid accountId, bool hydrate) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            PermissionConstants.ACCOUNT_RESOURCE_TYPE,
            accountId
        )
            ? await _inner.GetAccountByIdAsync(accountId, hydrate)
            : new GetResult<Account>(
                RetrieveResultCode.UnauthorizedError,
                $"Unauthorized to read account {accountId}",
                null
            );
}
