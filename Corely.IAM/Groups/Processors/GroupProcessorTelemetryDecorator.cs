using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.Filtering;
using Corely.IAM.Filtering.Ordering;
using Corely.IAM.Groups.Models;
using Corely.IAM.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Groups.Processors;

internal class GroupProcessorTelemetryDecorator(
    IGroupProcessor inner,
    ILogger<GroupProcessorTelemetryDecorator> logger
) : IGroupProcessor
{
    private readonly IGroupProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<GroupProcessorTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<CreateGroupResult> CreateGroupAsync(CreateGroupRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(GroupProcessor),
            request,
            () => _inner.CreateGroupAsync(request),
            logResult: true
        );

    public async Task<AddUsersToGroupResult> AddUsersToGroupAsync(AddUsersToGroupRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(GroupProcessor),
            request,
            () => _inner.AddUsersToGroupAsync(request),
            logResult: true
        );

    public async Task<RemoveUsersFromGroupResult> RemoveUsersFromGroupAsync(
        RemoveUsersFromGroupRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(GroupProcessor),
            request,
            () => _inner.RemoveUsersFromGroupAsync(request),
            logResult: true
        );

    public async Task<AssignRolesToGroupResult> AssignRolesToGroupAsync(
        AssignRolesToGroupRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(GroupProcessor),
            request,
            () => _inner.AssignRolesToGroupAsync(request),
            logResult: true
        );

    public async Task<RemoveRolesFromGroupResult> RemoveRolesFromGroupAsync(
        RemoveRolesFromGroupRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(GroupProcessor),
            request,
            () => _inner.RemoveRolesFromGroupAsync(request),
            logResult: true
        );

    public async Task<DeleteGroupResult> DeleteGroupAsync(Guid groupId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(GroupProcessor),
            groupId,
            () => _inner.DeleteGroupAsync(groupId),
            logResult: true
        );

    public async Task<ListResult<Group>> ListGroupsAsync(
        FilterBuilder<Group>? filter,
        OrderBuilder<Group>? order,
        int skip,
        int take
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(GroupProcessor),
            () => _inner.ListGroupsAsync(filter, order, skip, take),
            logResult: true
        );

    public async Task<GetResult<Group>> GetGroupByIdAsync(Guid groupId, bool hydrate) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(GroupProcessor),
            groupId,
            () => _inner.GetGroupByIdAsync(groupId, hydrate),
            logResult: true
        );
}
