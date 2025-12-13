using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Models.Extensions;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Models.Extensions;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Models.Extensions;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Models.Extensions;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Models.Extensions;
using Corely.IAM.Users.Processors;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class DeregistrationService(
    ILogger<DeregistrationService> logger,
    IPermissionProcessor permissionProcessor,
    IRoleProcessor roleProcessor,
    IGroupProcessor groupProcessor,
    IAccountProcessor accountProcessor,
    IUserProcessor userProcessor,
    IIamUserContextProvider userContextProvider
) : IDeregistrationService
{
    private readonly ILogger<DeregistrationService> _logger = logger.ThrowIfNull(nameof(logger));
    private readonly IPermissionProcessor _permissionProcessor = permissionProcessor.ThrowIfNull(
        nameof(permissionProcessor)
    );
    private readonly IRoleProcessor _roleProcessor = roleProcessor.ThrowIfNull(
        nameof(roleProcessor)
    );
    private readonly IGroupProcessor _groupProcessor = groupProcessor.ThrowIfNull(
        nameof(groupProcessor)
    );
    private readonly IAccountProcessor _accountProcessor = accountProcessor.ThrowIfNull(
        nameof(accountProcessor)
    );
    private readonly IUserProcessor _userProcessor = userProcessor.ThrowIfNull(
        nameof(userProcessor)
    );
    private readonly IIamUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );

    public async Task<DeregisterUserResult> DeregisterUserAsync(DeregisterUserRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Deregistering user {UserId}", request.UserId);

        var result = await _userProcessor.DeleteUserAsync(request.UserId);

        if (result.ResultCode != DeleteUserResultCode.Success)
        {
            _logger.LogInformation(
                "Deregistering user failed for user id {UserId}",
                request.UserId
            );
            return new DeregisterUserResult(
                result.ResultCode.ToDeregisterUserResultCode(),
                result.Message
            );
        }

        _logger.LogInformation("User {UserId} deregistered", request.UserId);
        return new DeregisterUserResult(DeregisterUserResultCode.Success, string.Empty);
    }

    public async Task<DeregisterAccountResult> DeregisterAccountAsync(
        DeregisterAccountRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Deregistering account {AccountId}", request.AccountId);

        var result = await _accountProcessor.DeleteAccountAsync(request.AccountId);

        if (result.ResultCode != DeleteAccountResultCode.Success)
        {
            _logger.LogInformation(
                "Deregistering account failed for account id {AccountId}",
                request.AccountId
            );
            return new DeregisterAccountResult(
                result.ResultCode.ToDeregisterAccountResultCode(),
                result.Message
            );
        }

        _logger.LogInformation("Account {AccountId} deregistered", request.AccountId);
        return new DeregisterAccountResult(DeregisterAccountResultCode.Success, string.Empty);
    }

    public async Task<DeregisterGroupResult> DeregisterGroupAsync(DeregisterGroupRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Deregistering group {GroupId}", request.GroupId);

        var result = await _groupProcessor.DeleteGroupAsync(request.GroupId);

        if (result.ResultCode != DeleteGroupResultCode.Success)
        {
            _logger.LogInformation(
                "Deregistering group failed for group id {GroupId}",
                request.GroupId
            );
            return new DeregisterGroupResult(
                result.ResultCode.ToDeregisterGroupResultCode(),
                result.Message
            );
        }

        _logger.LogInformation("Group {GroupId} deregistered", request.GroupId);
        return new DeregisterGroupResult(DeregisterGroupResultCode.Success, string.Empty);
    }

    public async Task<DeregisterRoleResult> DeregisterRoleAsync(DeregisterRoleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Deregistering role {RoleId}", request.RoleId);

        var result = await _roleProcessor.DeleteRoleAsync(request.RoleId);

        if (result.ResultCode != DeleteRoleResultCode.Success)
        {
            _logger.LogInformation(
                "Deregistering role failed for role id {RoleId}",
                request.RoleId
            );
            return new DeregisterRoleResult(
                result.ResultCode.ToDeregisterRoleResultCode(),
                result.Message
            );
        }

        _logger.LogInformation("Role {RoleId} deregistered", request.RoleId);
        return new DeregisterRoleResult(DeregisterRoleResultCode.Success, string.Empty);
    }

    public async Task<DeregisterPermissionResult> DeregisterPermissionAsync(
        DeregisterPermissionRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation("Deregistering permission {PermissionId}", request.PermissionId);

        var result = await _permissionProcessor.DeletePermissionAsync(request.PermissionId);

        if (result.ResultCode != DeletePermissionResultCode.Success)
        {
            _logger.LogInformation(
                "Deregistering permission failed for permission id {PermissionId}",
                request.PermissionId
            );
            return new DeregisterPermissionResult(
                result.ResultCode.ToDeregisterPermissionResultCode(),
                result.Message
            );
        }

        _logger.LogInformation("Permission {PermissionId} deregistered", request.PermissionId);
        return new DeregisterPermissionResult(DeregisterPermissionResultCode.Success, string.Empty);
    }

    public async Task<DeregisterUserFromAccountResult> DeregisterUserFromAccountAsync(
        DeregisterUserFromAccountRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        var accountId = _userContextProvider.GetUserContext()!.AccountId!.Value;
        _logger.LogInformation(
            "Deregistering user {UserId} from account {AccountId}",
            request.UserId,
            accountId
        );

        var result = await _accountProcessor.RemoveUserFromAccountAsync(
            new(request.UserId, accountId)
        );

        if (result.ResultCode != RemoveUserFromAccountResultCode.Success)
        {
            _logger.LogInformation(
                "Deregistering user {UserId} from account {AccountId} failed",
                request.UserId,
                accountId
            );
            return new DeregisterUserFromAccountResult(
                result.ResultCode.ToDeregisterUserFromAccountResultCode(),
                result.Message
            );
        }

        _logger.LogInformation(
            "User {UserId} deregistered from account {AccountId}",
            request.UserId,
            accountId
        );
        return new DeregisterUserFromAccountResult(
            DeregisterUserFromAccountResultCode.Success,
            string.Empty
        );
    }

    public async Task<DeregisterUsersFromGroupResult> DeregisterUsersFromGroupAsync(
        DeregisterUsersFromGroupRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation(
            "Deregistering user ids {@UserIds} from group id {GroupId}",
            request.UserIds,
            request.GroupId
        );

        var result = await _groupProcessor.RemoveUsersFromGroupAsync(
            new(request.UserIds, request.GroupId)
        );

        if (result.ResultCode == RemoveUsersFromGroupResultCode.GroupNotFoundError)
        {
            _logger.LogInformation(
                "Deregistering users from group failed for group id {GroupId}",
                request.GroupId
            );
            return new DeregisterUsersFromGroupResult(
                DeregisterUsersFromGroupResultCode.GroupNotFoundError,
                result.Message ?? string.Empty,
                0,
                request.UserIds
            );
        }

        if (result.ResultCode == RemoveUsersFromGroupResultCode.UserIsSoleOwnerError)
        {
            _logger.LogInformation(
                "Deregistering users from group failed - users are sole owners: {@SoleOwnerUserIds}",
                result.SoleOwnerUserIds
            );
            return new DeregisterUsersFromGroupResult(
                DeregisterUsersFromGroupResultCode.UserIsSoleOwnerError,
                result.Message ?? string.Empty,
                0,
                result.InvalidUserIds,
                result.SoleOwnerUserIds
            );
        }

        using (
            _logger.BeginScope(
                new Dictionary<string, object> { ["@InvalidUserIds"] = result.InvalidUserIds }
            )
        )
        {
            _logger.LogInformation(
                "Deregistered {RemovedUserCount} users from group id {GroupId}",
                result.RemovedUserCount,
                request.GroupId
            );
        }

        return new DeregisterUsersFromGroupResult(
            result.ResultCode.ToDeregisterUsersFromGroupResultCode(),
            result.Message ?? string.Empty,
            result.RemovedUserCount,
            result.InvalidUserIds,
            result.SoleOwnerUserIds
        );
    }

    public async Task<DeregisterRolesFromGroupResult> DeregisterRolesFromGroupAsync(
        DeregisterRolesFromGroupRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation(
            "Deregistering role ids {@RoleIds} from group id {GroupId}",
            request.RoleIds,
            request.GroupId
        );

        var result = await _groupProcessor.RemoveRolesFromGroupAsync(
            new(request.RoleIds, request.GroupId)
        );

        if (result.ResultCode == RemoveRolesFromGroupResultCode.GroupNotFoundError)
        {
            _logger.LogInformation(
                "Deregistering roles from group failed for group id {GroupId}",
                request.GroupId
            );
            return new DeregisterRolesFromGroupResult(
                DeregisterRolesFromGroupResultCode.GroupNotFoundError,
                result.Message ?? string.Empty,
                0,
                request.RoleIds
            );
        }

        if (result.ResultCode == RemoveRolesFromGroupResultCode.InvalidRoleIdsError)
        {
            _logger.LogInformation(
                "Deregistering roles from group failed - all role ids invalid: {@InvalidRoleIds}",
                result.InvalidRoleIds
            );
            return new DeregisterRolesFromGroupResult(
                DeregisterRolesFromGroupResultCode.InvalidRoleIdsError,
                result.Message ?? string.Empty,
                0,
                result.InvalidRoleIds
            );
        }

        if (result.ResultCode == RemoveRolesFromGroupResultCode.OwnerRoleRemovalBlockedError)
        {
            _logger.LogInformation(
                "Deregistering roles from group failed - owner role removal blocked: {@BlockedOwnerRoleIds}",
                result.BlockedOwnerRoleIds
            );
            return new DeregisterRolesFromGroupResult(
                DeregisterRolesFromGroupResultCode.OwnerRoleRemovalBlockedError,
                result.Message ?? string.Empty,
                0,
                result.InvalidRoleIds,
                result.BlockedOwnerRoleIds
            );
        }

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["@InvalidRoleIds"] = result.InvalidRoleIds,
                    ["@BlockedOwnerRoleIds"] = result.BlockedOwnerRoleIds,
                }
            )
        )
        {
            _logger.LogInformation(
                "Deregistered {RemovedRoleCount} roles from group id {GroupId}",
                result.RemovedRoleCount,
                request.GroupId
            );
        }

        return new DeregisterRolesFromGroupResult(
            result.ResultCode.ToDeregisterRolesFromGroupResultCode(),
            result.Message ?? string.Empty,
            result.RemovedRoleCount,
            result.InvalidRoleIds,
            result.BlockedOwnerRoleIds
        );
    }

    public async Task<DeregisterRolesFromUserResult> DeregisterRolesFromUserAsync(
        DeregisterRolesFromUserRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation(
            "Deregistering role ids {@RoleIds} from user id {UserId}",
            request.RoleIds,
            request.UserId
        );

        var result = await _userProcessor.RemoveRolesFromUserAsync(
            new(request.RoleIds, request.UserId)
        );

        if (result.ResultCode == RemoveRolesFromUserResultCode.UserNotFoundError)
        {
            _logger.LogInformation(
                "Deregistering roles from user failed for user id {UserId}",
                request.UserId
            );
            return new DeregisterRolesFromUserResult(
                DeregisterRolesFromUserResultCode.UserNotFoundError,
                result.Message ?? string.Empty,
                0,
                request.RoleIds
            );
        }

        if (result.ResultCode == RemoveRolesFromUserResultCode.InvalidRoleIdsError)
        {
            _logger.LogInformation(
                "Deregistering roles from user failed - all role ids invalid: {@InvalidRoleIds}",
                result.InvalidRoleIds
            );
            return new DeregisterRolesFromUserResult(
                DeregisterRolesFromUserResultCode.InvalidRoleIdsError,
                result.Message ?? string.Empty,
                0,
                result.InvalidRoleIds
            );
        }

        if (result.ResultCode == RemoveRolesFromUserResultCode.UserIsSoleOwnerError)
        {
            _logger.LogInformation(
                "Deregistering roles from user failed - user is sole owner: {@BlockedOwnerRoleIds}",
                result.BlockedOwnerRoleIds
            );
            return new DeregisterRolesFromUserResult(
                DeregisterRolesFromUserResultCode.UserIsSoleOwnerError,
                result.Message ?? string.Empty,
                0,
                result.InvalidRoleIds,
                result.BlockedOwnerRoleIds
            );
        }

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["@InvalidRoleIds"] = result.InvalidRoleIds,
                    ["@BlockedOwnerRoleIds"] = result.BlockedOwnerRoleIds,
                }
            )
        )
        {
            _logger.LogInformation(
                "Deregistered {RemovedRoleCount} roles from user id {UserId}",
                result.RemovedRoleCount,
                request.UserId
            );
        }

        return new DeregisterRolesFromUserResult(
            result.ResultCode.ToDeregisterRolesFromUserResultCode(),
            result.Message ?? string.Empty,
            result.RemovedRoleCount,
            result.InvalidRoleIds,
            result.BlockedOwnerRoleIds
        );
    }

    public async Task<DeregisterPermissionsFromRoleResult> DeregisterPermissionsFromRoleAsync(
        DeregisterPermissionsFromRoleRequest request
    )
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));
        _logger.LogInformation(
            "Deregistering permission ids {@PermissionIds} from role id {RoleId}",
            request.PermissionIds,
            request.RoleId
        );

        var result = await _roleProcessor.RemovePermissionsFromRoleAsync(
            new(request.PermissionIds, request.RoleId)
        );

        if (result.ResultCode == RemovePermissionsFromRoleResultCode.RoleNotFoundError)
        {
            _logger.LogInformation(
                "Deregistering permissions from role failed for role id {RoleId}",
                request.RoleId
            );
            return new DeregisterPermissionsFromRoleResult(
                DeregisterPermissionsFromRoleResultCode.RoleNotFoundError,
                result.Message ?? string.Empty,
                0,
                request.PermissionIds
            );
        }

        if (result.ResultCode == RemovePermissionsFromRoleResultCode.InvalidPermissionIdsError)
        {
            _logger.LogInformation(
                "Deregistering permissions from role failed - all permission ids invalid: {@InvalidPermissionIds}",
                result.InvalidPermissionIds
            );
            return new DeregisterPermissionsFromRoleResult(
                DeregisterPermissionsFromRoleResultCode.InvalidPermissionIdsError,
                result.Message ?? string.Empty,
                0,
                result.InvalidPermissionIds
            );
        }

        if (result.ResultCode == RemovePermissionsFromRoleResultCode.SystemPermissionRemovalError)
        {
            _logger.LogInformation(
                "Deregistering permissions from role failed - cannot remove system permissions from system role: {@SystemPermissionIds}",
                result.SystemPermissionIds
            );
            return new DeregisterPermissionsFromRoleResult(
                DeregisterPermissionsFromRoleResultCode.SystemPermissionRemovalError,
                result.Message ?? string.Empty,
                0,
                result.InvalidPermissionIds,
                result.SystemPermissionIds
            );
        }

        using (
            _logger.BeginScope(
                new Dictionary<string, object>
                {
                    ["@InvalidPermissionIds"] = result.InvalidPermissionIds,
                    ["@SystemPermissionIds"] = result.SystemPermissionIds,
                }
            )
        )
        {
            _logger.LogInformation(
                "Deregistered {RemovedPermissionCount} permissions from role id {RoleId}",
                result.RemovedPermissionCount,
                request.RoleId
            );
        }

        return new DeregisterPermissionsFromRoleResult(
            result.ResultCode.ToDeregisterPermissionsFromRoleResultCode(),
            result.Message ?? string.Empty,
            result.RemovedPermissionCount,
            result.InvalidPermissionIds,
            result.SystemPermissionIds
        );
    }
}
