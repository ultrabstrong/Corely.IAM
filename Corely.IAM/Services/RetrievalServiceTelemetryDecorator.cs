using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Extensions;
using Corely.IAM.Filtering;
using Corely.IAM.Filtering.Ordering;
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
        FilterBuilder<Permission>? filter = null,
        OrderBuilder<Permission>? order = null,
        int skip = 0,
        int take = 25
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            () => _inner.ListPermissionsAsync(filter, order, skip, take),
            logResult: true
        );

    public async Task<RetrieveSingleResult<Permission>> GetPermissionAsync(
        Guid permissionId,
        bool hydrate = false
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            () => _inner.GetPermissionAsync(permissionId, hydrate),
            logResult: true
        );

    public async Task<RetrieveListResult<Group>> ListGroupsAsync(
        FilterBuilder<Group>? filter = null,
        OrderBuilder<Group>? order = null,
        int skip = 0,
        int take = 25
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            () => _inner.ListGroupsAsync(filter, order, skip, take),
            logResult: true
        );

    public async Task<RetrieveSingleResult<Group>> GetGroupAsync(
        Guid groupId,
        bool hydrate = false
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            () => _inner.GetGroupAsync(groupId, hydrate),
            logResult: true
        );

    public async Task<RetrieveListResult<Role>> ListRolesAsync(
        FilterBuilder<Role>? filter = null,
        OrderBuilder<Role>? order = null,
        int skip = 0,
        int take = 25
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            () => _inner.ListRolesAsync(filter, order, skip, take),
            logResult: true
        );

    public async Task<RetrieveSingleResult<Role>> GetRoleAsync(Guid roleId, bool hydrate = false) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            () => _inner.GetRoleAsync(roleId, hydrate),
            logResult: true
        );

    public async Task<RetrieveListResult<User>> ListUsersAsync(
        FilterBuilder<User>? filter = null,
        OrderBuilder<User>? order = null,
        int skip = 0,
        int take = 25
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            () => _inner.ListUsersAsync(filter, order, skip, take),
            logResult: true
        );

    public async Task<RetrieveSingleResult<User>> GetUserAsync(Guid userId, bool hydrate = false) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            () => _inner.GetUserAsync(userId, hydrate),
            logResult: true
        );

    public async Task<RetrieveListResult<Account>> ListAccountsAsync(
        FilterBuilder<Account>? filter = null,
        OrderBuilder<Account>? order = null,
        int skip = 0,
        int take = 25
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            () => _inner.ListAccountsAsync(filter, order, skip, take),
            logResult: true
        );

    public async Task<RetrieveSingleResult<Account>> GetAccountAsync(
        Guid accountId,
        bool hydrate = false
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(RetrievalService),
            () => _inner.GetAccountAsync(accountId, hydrate),
            logResult: true
        );
}
