using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.Security.Processors;

internal class KeyRotationProcessorAuthorizationDecorator(
    IKeyRotationProcessor inner,
    IAuthorizationProvider authorizationProvider
) : IKeyRotationProcessor
{
    private readonly IKeyRotationProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<ModifyResult> RotateAccountKeyAsync(RotateAccountKeyRequest request) =>
        _authorizationProvider.HasAccountContext(request.AccountId)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            Account.RESOURCE_TYPE,
            request.AccountId
        )
            ? await _inner.RotateAccountKeyAsync(request)
            : new ModifyResult(
                ModifyResultCode.UnauthorizedError,
                $"Unauthorized to rotate keys for account {request.AccountId}"
            );

    public async Task<CompromiseAccountKeyResult> RotateCompromisedAccountKeyAsync(
        RotateCompromisedAccountKeyRequest request
    ) =>
        await MayCompromiseKeysOfAsync(request.AccountId)
            ? await _inner.RotateCompromisedAccountKeyAsync(request)
            : Refused(request.AccountId);

    public async Task<CompromiseAccountKeyResult> MarkAccountKeyCompromisedAsync(
        MarkAccountKeyCompromisedRequest request
    ) =>
        await MayCompromiseKeysOfAsync(request.AccountId)
            ? await _inner.MarkAccountKeyCompromisedAsync(request)
            : Refused(request.AccountId);

    private async Task<bool> MayCompromiseKeysOfAsync(Guid accountId) =>
        _authorizationProvider.IsNonSystemUserContext()
        && _authorizationProvider.HasAccountContext(accountId)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Update,
            Account.RESOURCE_TYPE,
            accountId
        );

    private static CompromiseAccountKeyResult Refused(Guid accountId) =>
        new(
            CompromiseAccountKeyResultCode.UnauthorizedError,
            $"Unauthorized to mark keys of account {accountId} compromised"
        );

    public async Task<ModifyResult> RotateCurrentUserKeyAsync(KeyType keyType) =>
        _authorizationProvider.IsNonSystemUserContext()
            ? await _inner.RotateCurrentUserKeyAsync(keyType)
            : new ModifyResult(
                ModifyResultCode.UnauthorizedError,
                "Unauthorized to rotate user keys"
            );
}
