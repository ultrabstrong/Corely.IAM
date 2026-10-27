using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
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

    public Task<ModifyResult> ModifyAccountAsync(UpdateAccountRequest request) =>
        _inner.ModifyAccountAsync(request);

    public Task<ModifyResult> ModifyUserAsync(UpdateUserRequest request) =>
        _inner.ModifyUserAsync(request);

    public Task<ModifyResult> ModifyGroupAsync(UpdateGroupRequest request) =>
        _inner.ModifyGroupAsync(request);

    public Task<ModifyResult> ModifyRoleAsync(UpdateRoleRequest request) =>
        _inner.ModifyRoleAsync(request);

    public Task<ModifyResult> ModifyPermissionAsync(UpdatePermissionRequest request) =>
        _inner.ModifyPermissionAsync(request);

    public Task<ModifyResult> ModifyApiKeyAsync(UpdateApiKeyRequest request) =>
        _inner.ModifyApiKeyAsync(request);

    public Task<RotateApiKeySecretResult> RotateApiKeySecretAsync(
        RotateApiKeySecretRequest request
    ) => _inner.RotateApiKeySecretAsync(request);

    public Task<ModifyResult> RotateAccountKeyAsync(RotateAccountKeyRequest request) =>
        _inner.RotateAccountKeyAsync(request);

    public Task<ModifyResult> RotateCurrentUserKeyAsync(KeyType keyType) =>
        _inner.RotateCurrentUserKeyAsync(keyType);

    public async Task<ModifyPasswordResult> ModifyPasswordAsync(ModifyPasswordRequest request) =>
        _authorizationProvider.IsNonSystemUserContext()
            ? await _inner.ModifyPasswordAsync(request)
            : new ModifyPasswordResult(ModifyPasswordResultCode.UnauthorizedError, "Unauthorized");
}
