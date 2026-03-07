using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Extensions;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Roles.Models;
using Corely.IAM.Users.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class RetrievalServiceTelemetryDecorator(
    IRetrievalService inner,
    ILogger<RetrievalServiceTelemetryDecorator> logger
) : IRetrievalService
{
    private readonly IRetrievalService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<RetrievalServiceTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<RetrieveListResult<Permission>> ListPermissionsAsync(
        ListPermissionsRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            request,
            () => _inner.ListPermissionsAsync(request),
            logResult: true
        );

    public async Task<RetrieveSingleResult<Permission>> GetPermissionAsync(
        Guid permissionId,
        bool hydrate = false
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            new { permissionId, hydrate },
            () => _inner.GetPermissionAsync(permissionId, hydrate),
            logResult: true
        );

    public async Task<RetrieveListResult<Group>> ListGroupsAsync(ListGroupsRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            request,
            () => _inner.ListGroupsAsync(request),
            logResult: true
        );

    public async Task<RetrieveSingleResult<Group>> GetGroupAsync(
        Guid groupId,
        bool hydrate = false
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            new { groupId, hydrate },
            () => _inner.GetGroupAsync(groupId, hydrate),
            logResult: true
        );

    public async Task<RetrieveListResult<Role>> ListRolesAsync(ListRolesRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            request,
            () => _inner.ListRolesAsync(request),
            logResult: true
        );

    public async Task<RetrieveSingleResult<Role>> GetRoleAsync(Guid roleId, bool hydrate = false) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            new { roleId, hydrate },
            () => _inner.GetRoleAsync(roleId, hydrate),
            logResult: true
        );

    public async Task<RetrieveListResult<User>> ListUsersAsync(ListUsersRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            request,
            () => _inner.ListUsersAsync(request),
            logResult: true
        );

    public async Task<RetrieveSingleResult<User>> GetUserAsync(Guid userId, bool hydrate = false) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            new { userId, hydrate },
            () => _inner.GetUserAsync(userId, hydrate),
            logResult: true
        );

    public async Task<RetrieveListResult<Account>> ListAccountsAsync(ListAccountsRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            request,
            () => _inner.ListAccountsAsync(request),
            logResult: true
        );

    public async Task<RetrieveSingleResult<Account>> GetAccountAsync(
        Guid accountId,
        bool hydrate = false
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            new { accountId, hydrate },
            () => _inner.GetAccountAsync(accountId, hydrate),
            logResult: true
        );
}
