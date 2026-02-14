using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Services;

internal class ModificationServiceAuthorizationDecorator(
    IModificationService inner,
    IAuthorizationProvider authorizationProvider
) : IModificationService
{
    private readonly IModificationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<ModifyResult> ModifyAccountAsync(UpdateAccountRequest request) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.ModifyAccountAsync(request)
            : new ModifyResult(
                ModifyResultCode.UnauthorizedError,
                "Unauthorized to modify account"
            );

    public async Task<ModifyResult> ModifyUserAsync(UpdateUserRequest request) =>
        _authorizationProvider.HasUserContext()
            ? await _inner.ModifyUserAsync(request)
            : new ModifyResult(ModifyResultCode.UnauthorizedError, "Unauthorized to modify user");

    public async Task<ModifyResult> ModifyGroupAsync(UpdateGroupRequest request) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.ModifyGroupAsync(request)
            : new ModifyResult(ModifyResultCode.UnauthorizedError, "Unauthorized to modify group");

    public async Task<ModifyResult> ModifyRoleAsync(UpdateRoleRequest request) =>
        _authorizationProvider.HasAccountContext()
            ? await _inner.ModifyRoleAsync(request)
            : new ModifyResult(ModifyResultCode.UnauthorizedError, "Unauthorized to modify role");
}
