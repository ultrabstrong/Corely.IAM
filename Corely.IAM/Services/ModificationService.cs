using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Models.Extensions;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Models.Extensions;
using Corely.IAM.Security.Processors;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class ModificationService(
    ILogger<ModificationService> logger,
    IAccountProcessor accountProcessor,
    IUserProcessor userProcessor,
    IGroupProcessor groupProcessor,
    IRoleProcessor roleProcessor,
    IPermissionProcessor permissionProcessor,
    IKeyRotationProcessor keyRotationProcessor,
    IBasicAuthProcessor basicAuthProcessor,
    IReauthenticationProvider reauthenticationProvider,
    IUserContextProvider userContextProvider,
    IAuthenticationProvider authenticationProvider
) : IModificationService
{
    private readonly IAuthenticationProvider _authenticationProvider =
        authenticationProvider.ThrowIfNull(nameof(authenticationProvider));
    private readonly ILogger<ModificationService> _logger = logger.ThrowIfNull(nameof(logger));
    private readonly IAccountProcessor _accountProcessor = accountProcessor.ThrowIfNull(
        nameof(accountProcessor)
    );
    private readonly IUserProcessor _userProcessor = userProcessor.ThrowIfNull(
        nameof(userProcessor)
    );
    private readonly IGroupProcessor _groupProcessor = groupProcessor.ThrowIfNull(
        nameof(groupProcessor)
    );
    private readonly IRoleProcessor _roleProcessor = roleProcessor.ThrowIfNull(
        nameof(roleProcessor)
    );
    private readonly IPermissionProcessor _permissionProcessor = permissionProcessor.ThrowIfNull(
        nameof(permissionProcessor)
    );
    private readonly IKeyRotationProcessor _keyRotationProcessor = keyRotationProcessor.ThrowIfNull(
        nameof(keyRotationProcessor)
    );
    private readonly IBasicAuthProcessor _basicAuthProcessor = basicAuthProcessor.ThrowIfNull(
        nameof(basicAuthProcessor)
    );
    private readonly IReauthenticationProvider _reauthenticationProvider =
        reauthenticationProvider.ThrowIfNull(nameof(reauthenticationProvider));
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );

    public async Task<ModifyResult> ModifyAccountAsync(UpdateAccountRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Modifying account {AccountId}", request.AccountId);
        return await _accountProcessor.UpdateAccountAsync(request);
    }

    public async Task<ModifyResult> ModifyUserAsync(UpdateUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Modifying user {UserId}", request.UserId);
        return await _userProcessor.UpdateUserAsync(request);
    }

    public async Task<ModifyResult> ModifyGroupAsync(UpdateGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Modifying group {GroupId}", request.GroupId);
        return await _groupProcessor.UpdateGroupAsync(request);
    }

    public async Task<ModifyResult> ModifyRoleAsync(UpdateRoleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Modifying role {RoleId}", request.RoleId);
        return await _roleProcessor.UpdateRoleAsync(request);
    }

    public async Task<ModifyResult> ModifyPermissionAsync(UpdatePermissionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Modifying permission {PermissionId}", request.PermissionId);
        return await _permissionProcessor.UpdatePermissionAsync(request);
    }

    public async Task<ModifyResult> RotateAccountKeyAsync(RotateAccountKeyRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation(
            "Rotating {KeyType} key for account {AccountId}",
            request.KeyType,
            request.AccountId
        );
        return await _keyRotationProcessor.RotateAccountKeyAsync(request);
    }

    public async Task<ModifyResult> RotateCurrentUserKeyAsync(KeyType keyType)
    {
        _logger.LogInformation("Rotating {KeyType} key for the current user", keyType);
        return await _keyRotationProcessor.RotateCurrentUserKeyAsync(keyType);
    }

    public async Task<ModifyPasswordResult> ModifyPasswordAsync(ModifyPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var context = _userContextProvider.GetUserContext()!;
        var userId = context.User!.Id;
        _logger.LogInformation("Modifying password for user {UserId}", userId);

        var passwordResult = await _reauthenticationProvider.VerifyCurrentPasswordAsync(
            userId,
            request.CurrentPassword
        );
        if (passwordResult.ResultCode != ReauthenticationResultCode.Success)
        {
            return new ModifyPasswordResult(
                passwordResult.ResultCode.ToModifyPasswordResultCode(),
                passwordResult.Message
            );
        }

        var totpResult = await _reauthenticationProvider.VerifyTotpCodeAsync(
            userId,
            request.TotpCode
        );
        if (totpResult.ResultCode != ReauthenticationResultCode.Success)
        {
            return new ModifyPasswordResult(
                totpResult.ResultCode.ToModifyPasswordResultCode(),
                totpResult.Message
            );
        }

        var updateResult = await _basicAuthProcessor.UpdateBasicAuthAsync(
            new(userId, request.NewPassword)
        );
        if (updateResult.ResultCode == UpdateBasicAuthResultCode.Success)
            await _authenticationProvider.RevokeAllUserAuthTokensAsync(userId, context.AuthTokenId);
        return new ModifyPasswordResult(
            updateResult.ResultCode.ToModifyPasswordResultCode(),
            updateResult.Message
        );
    }
}
