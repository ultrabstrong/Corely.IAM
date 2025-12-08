using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Accounts.Processors;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class DeregistrationService : IDeregistrationService
{
    private readonly ILogger<DeregistrationService> _logger;
    private readonly IPermissionProcessor _permissionProcessor;
    private readonly IRoleProcessor _roleProcessor;
    private readonly IGroupProcessor _groupProcessor;
    private readonly IAccountProcessor _accountProcessor;
    private readonly IUserProcessor _userProcessor;

    public DeregistrationService(
        ILogger<DeregistrationService> logger,
        IPermissionProcessor permissionProcessor,
        IRoleProcessor roleProcessor,
        IGroupProcessor groupProcessor,
        IAccountProcessor accountProcessor,
        IUserProcessor userProcessor
    )
    {
        _logger = logger.ThrowIfNull(nameof(logger));
        _permissionProcessor = permissionProcessor.ThrowIfNull(nameof(permissionProcessor));
        _roleProcessor = roleProcessor.ThrowIfNull(nameof(roleProcessor));
        _groupProcessor = groupProcessor.ThrowIfNull(nameof(groupProcessor));
        _accountProcessor = accountProcessor.ThrowIfNull(nameof(accountProcessor));
        _userProcessor = userProcessor.ThrowIfNull(nameof(userProcessor));
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
                (DeregisterPermissionResultCode)result.ResultCode,
                result.Message
            );
        }

        _logger.LogInformation("Permission {PermissionId} deregistered", request.PermissionId);
        return new DeregisterPermissionResult(DeregisterPermissionResultCode.Success, string.Empty);
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
                (DeregisterRoleResultCode)result.ResultCode,
                result.Message
            );
        }

        _logger.LogInformation("Role {RoleId} deregistered", request.RoleId);
        return new DeregisterRoleResult(DeregisterRoleResultCode.Success, string.Empty);
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
                (DeregisterGroupResultCode)result.ResultCode,
                result.Message
            );
        }

        _logger.LogInformation("Group {GroupId} deregistered", request.GroupId);
        return new DeregisterGroupResult(DeregisterGroupResultCode.Success, string.Empty);
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
                (DeregisterAccountResultCode)result.ResultCode,
                result.Message
            );
        }

        _logger.LogInformation("Account {AccountId} deregistered", request.AccountId);
        return new DeregisterAccountResult(DeregisterAccountResultCode.Success, string.Empty);
    }

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
                (DeregisterUserResultCode)result.ResultCode,
                result.Message
            );
        }

        _logger.LogInformation("User {UserId} deregistered", request.UserId);
        return new DeregisterUserResult(DeregisterUserResultCode.Success, string.Empty);
    }
}
