using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.Filtering;
using Corely.IAM.Filtering.Ordering;
using Corely.IAM.Models;
using Corely.IAM.Roles.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Roles.Processors;

internal class RoleProcessorTelemetryDecorator(
    IRoleProcessor inner,
    ILogger<RoleProcessorTelemetryDecorator> logger
) : IRoleProcessor
{
    private readonly IRoleProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<RoleProcessorTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<CreateRoleResult> CreateRoleAsync(CreateRoleRequest createRoleRequest) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RoleProcessor),
            createRoleRequest,
            () => _inner.CreateRoleAsync(createRoleRequest),
            logResult: true
        );

    public async Task<CreateDefaultSystemRolesResult> CreateDefaultSystemRolesAsync(
        Guid ownerAccountId
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RoleProcessor),
            ownerAccountId,
            () => _inner.CreateDefaultSystemRolesAsync(ownerAccountId),
            logResult: true
        );

    public async Task<GetRoleResult> GetRoleAsync(Guid roleId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RoleProcessor),
            roleId,
            () => _inner.GetRoleAsync(roleId),
            logResult: true
        );

    public async Task<GetRoleResult> GetRoleAsync(string roleName, Guid ownerAccountId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RoleProcessor),
            new { roleName, ownerAccountId },
            () => _inner.GetRoleAsync(roleName, ownerAccountId),
            logResult: true
        );

    public async Task<ListResult<Role>> ListRolesAsync(
        FilterBuilder<Role>? filter,
        OrderBuilder<Role>? order,
        int skip,
        int take
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RoleProcessor),
            new { skip, take },
            () => _inner.ListRolesAsync(filter, order, skip, take),
            logResult: true
        );

    public async Task<GetResult<Role>> GetRoleByIdAsync(Guid roleId, bool hydrate) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RoleProcessor),
            new { roleId, hydrate },
            () => _inner.GetRoleByIdAsync(roleId, hydrate),
            logResult: true
        );

    public async Task<AssignPermissionsToRoleResult> AssignPermissionsToRoleAsync(
        AssignPermissionsToRoleRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RoleProcessor),
            request,
            () => _inner.AssignPermissionsToRoleAsync(request),
            logResult: true
        );

    public async Task<RemovePermissionsFromRoleResult> RemovePermissionsFromRoleAsync(
        RemovePermissionsFromRoleRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RoleProcessor),
            request,
            () => _inner.RemovePermissionsFromRoleAsync(request),
            logResult: true
        );

    public async Task<DeleteRoleResult> DeleteRoleAsync(Guid roleId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RoleProcessor),
            roleId,
            () => _inner.DeleteRoleAsync(roleId),
            logResult: true
        );
}
