using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.Accounts.Processors;

internal class AccountClosureProcessorAuthorizationDecorator(
    IAccountClosureProcessor inner,
    IAuthorizationProvider authorizationProvider
) : IAccountClosureProcessor
{
    private readonly IAccountClosureProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<CloseAccountResult> RemoveAccountAccessAsync(Guid accountId) =>
        await MayDeleteAsync(accountId)
            ? await _inner.RemoveAccountAccessAsync(accountId)
            : Unauthorized(accountId);

    public async Task<CloseAccountResult> PurgeAccountDataAsync(Guid accountId) =>
        await MayDeleteAsync(accountId)
            ? await _inner.PurgeAccountDataAsync(accountId)
            : Unauthorized(accountId);

    private async Task<bool> MayDeleteAsync(Guid accountId) =>
        _authorizationProvider.HasAccountContext(accountId)
        && await _authorizationProvider.IsAuthorizedAsync(
            AuthAction.Delete,
            Account.RESOURCE_TYPE,
            accountId
        );

    private static CloseAccountResult Unauthorized(Guid accountId) =>
        new(
            CloseAccountResultCode.UnauthorizedError,
            $"Unauthorized to delete account {accountId}"
        );
}
