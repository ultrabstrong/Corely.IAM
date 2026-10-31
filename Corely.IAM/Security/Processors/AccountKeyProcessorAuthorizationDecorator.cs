using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.Security.Processors;

internal class AccountKeyProcessorAuthorizationDecorator(
    IAccountKeyProcessor inner,
    IAuthorizationProvider authorizationProvider,
    IUserContextProvider userContextProvider
) : IAccountKeyProcessor
{
    private readonly IAccountKeyProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );

    public Task<GetResult<AccountJsonWebKeySet>> GetJsonWebKeySetAsync(Guid accountId) =>
        _inner.GetJsonWebKeySetAsync(accountId);

    public async Task<RevealAccountKeyResult> RevealKeyAsync(RevealAccountKeyRequest request) =>
        _authorizationProvider.IsNonSystemUserContext()
        && _authorizationProvider.HasAccountContext(request.AccountId)
        && _userContextProvider.GetUserContext()?.EnteredAsPlatformMember == false
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Execute,
            Account.SECRET_KEYS_RESOURCE_TYPE,
            request.AccountId
        )
            ? await _inner.RevealKeyAsync(request)
            : new RevealAccountKeyResult(
                RevealAccountKeyResultCode.UnauthorizedError,
                $"Unauthorized to reveal keys of account {request.AccountId}"
            );
}
