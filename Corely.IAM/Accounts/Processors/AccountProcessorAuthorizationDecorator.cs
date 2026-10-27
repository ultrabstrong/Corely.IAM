using Corely.Common.Extensions;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.Accounts.Processors;

internal class AccountProcessorAuthorizationDecorator(
    IAccountProcessor inner,
    IAuthorizationProvider authorizationProvider,
    IUserContextProvider userContextProvider
) : IAccountProcessor
{
    private readonly IAccountProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );

    public Task<CreateAccountResult> CreateAccountAsync(CreateAccountRequest request) =>
        _authorizationProvider.HasUserContext()
        && _authorizationProvider.IsAuthorizedForOwnUser(request.OwnerUserId)
            ? _inner.CreateAccountAsync(request)
            : Task.FromResult(
                new CreateAccountResult(
                    CreateAccountResultCode.UnauthorizedError,
                    $"Unauthorized to create account for user {request.OwnerUserId}",
                    Guid.Empty
                )
            );

    public async Task<GetAccountResult> GetAccountAsync(Guid accountId) =>
        await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            Account.RESOURCE_TYPE,
            accountId
        )
            ? await _inner.GetAccountAsync(accountId)
            : new GetAccountResult(
                GetAccountResultCode.UnauthorizedError,
                $"Unauthorized to read account {accountId}",
                null
            );

    public async Task<ModifyResult> UpdateAccountAsync(UpdateAccountRequest request) =>
        _authorizationProvider.HasAccountContext(request.AccountId)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            Account.RESOURCE_TYPE,
            request.AccountId
        )
            ? await _inner.UpdateAccountAsync(request)
            : new ModifyResult(
                ModifyResultCode.UnauthorizedError,
                $"Unauthorized to update account {request.AccountId}"
            );

    public async Task<DeleteAccountResult> DeleteAccountAsync(Guid accountId) =>
        _authorizationProvider.HasAccountContext(accountId)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Delete,
            Account.RESOURCE_TYPE,
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
        _userContextProvider.GetUserContext()?.IsSystemContext == true
            ? await _inner.AddUserToAccountAsync(request)
            : new AddUserToAccountResult(
                AddUserToAccountResultCode.UnauthorizedError,
                "Only system context adds a user to an account directly; anyone else joins by invitation"
            );

    public Task<AddUserToAccountResult> AddUserToAccountForInvitationAsync(
        AddUserToAccountRequest request
    ) => _inner.AddUserToAccountForInvitationAsync(request);

    public async Task<RemoveUserFromAccountResult> RemoveUserFromAccountAsync(
        RemoveUserFromAccountRequest request
    ) =>
        _authorizationProvider.IsAuthorizedForOwnUser(request.UserId, true)
        || await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            Account.RESOURCE_TYPE,
            request.AccountId
        )
            ? await _inner.RemoveUserFromAccountAsync(request)
            : new RemoveUserFromAccountResult(
                RemoveUserFromAccountResultCode.UnauthorizedError,
                $"Unauthorized to update account {request.AccountId}"
            );

    public Task<ListResult<Account>> ListAccountsAsync(
        ListAccountsRequest request,
        IReadOnlySet<Guid>? authorizedResourceIds = null
    ) =>
        _authorizationProvider.HasUserContext()
            ? _inner.ListAccountsAsync(request, authorizedResourceIds)
            : Task.FromResult(
                new ListResult<Account>(
                    RetrieveResultCode.UnauthorizedError,
                    "Unauthorized to list accounts",
                    null
                )
            );

    public async Task<GetResult<Account>> GetAccountByIdAsync(Guid accountId, bool hydrate) =>
        _authorizationProvider.HasAccountContext(accountId)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            Account.RESOURCE_TYPE,
            accountId
        )
            ? await _inner.GetAccountByIdAsync(accountId, hydrate)
            : new GetResult<Account>(
                RetrieveResultCode.UnauthorizedError,
                $"Unauthorized to read account {accountId}",
                null
            );

    public async Task<GetResult<AccountEntity>> GetAccountKeysAsync(Guid accountId) =>
        _authorizationProvider.HasAccountContext(accountId)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Read,
            Account.RESOURCE_TYPE,
            accountId
        )
            ? await _inner.GetAccountKeysAsync(accountId)
            : new GetResult<AccountEntity>(
                RetrieveResultCode.UnauthorizedError,
                $"Unauthorized to read account {accountId}",
                null
            );
}
