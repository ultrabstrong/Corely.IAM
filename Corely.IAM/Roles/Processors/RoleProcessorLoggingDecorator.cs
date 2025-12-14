using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.Roles.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Roles.Processors;

internal class RoleProcessorLoggingDecorator(
    IRoleProcessor inner,
    ILogger<RoleProcessorLoggingDecorator> logger
) : IRoleProcessor
{
    private readonly IRoleProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<RoleProcessorLoggingDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<CreateRoleResult> CreateRoleAsync(CreateRoleRequest createRoleRequest) =>
        await _logger.ExecuteWithLogging(
            nameof(RoleProcessor),
            createRoleRequest,
            () => _inner.CreateRoleAsync(createRoleRequest),
            logResult: true
        );

    public async Task<CreateDefaultSystemRolesResult> CreateDefaultSystemRolesAsync(
        int ownerAccountId
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(RoleProcessor),
            ownerAccountId,
            () => _inner.CreateDefaultSystemRolesAsync(ownerAccountId),
            logResult: true
        );

    public async Task<GetRoleResult> GetRoleAsync(int roleId) =>
        await _logger.ExecuteWithLogging(
            nameof(RoleProcessor),
            roleId,
            () => _inner.GetRoleAsync(roleId),
            logResult: true
        );

    public async Task<GetRoleResult> GetRoleAsync(string roleName, int ownerAccountId) =>
        await _logger.ExecuteWithLogging(
            nameof(RoleProcessor),
            new { roleName, ownerAccountId },
            () => _inner.GetRoleAsync(roleName, ownerAccountId),
            logResult: true
        );

    public async Task<AssignPermissionsToRoleResult> AssignPermissionsToRoleAsync(
        AssignPermissionsToRoleRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(RoleProcessor),
            request,
            () => _inner.AssignPermissionsToRoleAsync(request),
            logResult: true
        );

    public async Task<RemovePermissionsFromRoleResult> RemovePermissionsFromRoleAsync(
        RemovePermissionsFromRoleRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(RoleProcessor),
            request,
            () => _inner.RemovePermissionsFromRoleAsync(request),
            logResult: true
        );

    public async Task<DeleteRoleResult> DeleteRoleAsync(int roleId) =>
        await _logger.ExecuteWithLogging(
            nameof(RoleProcessor),
            roleId,
            () => _inner.DeleteRoleAsync(roleId),
            logResult: true
        );
}
