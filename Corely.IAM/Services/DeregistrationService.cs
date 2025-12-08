using Corely.Common.Extensions;
using Corely.IAM.Groups.Models;
using Corely.IAM.Groups.Processors;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Permissions.Processors;
using Corely.IAM.Roles.Models;
using Corely.IAM.Roles.Processors;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class DeregistrationService : IDeregistrationService
{
    private readonly ILogger<DeregistrationService> _logger;
    private readonly IPermissionProcessor _permissionProcessor;
    private readonly IRoleProcessor _roleProcessor;
    private readonly IGroupProcessor _groupProcessor;

    public DeregistrationService(
        ILogger<DeregistrationService> logger,
        IPermissionProcessor permissionProcessor,
        IRoleProcessor roleProcessor,
        IGroupProcessor groupProcessor
    )
    {
        _logger = logger.ThrowIfNull(nameof(logger));
        _permissionProcessor = permissionProcessor.ThrowIfNull(nameof(permissionProcessor));
        _roleProcessor = roleProcessor.ThrowIfNull(nameof(roleProcessor));
        _groupProcessor = groupProcessor.ThrowIfNull(nameof(groupProcessor));
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

    public Task<DeregisterUserResult> DegisterUserAsync(DeregisterUserRequest request)
    {
        _logger.LogDebug(
            "Need to implement permissions so we can check if the user is the owner of any account"
        );
        throw new NotImplementedException();
    }

    public Task<DeregisterAccountResult> DegisterAccountAsync(DeregisterAccountRequest request)
    {
        _logger.LogDebug(
            "Need to implement permissions so we can check if the user is the owner of this account"
        );
        throw new NotImplementedException();
    }
}
