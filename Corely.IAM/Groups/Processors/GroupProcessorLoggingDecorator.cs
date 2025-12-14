using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.Groups.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Groups.Processors;

internal class GroupProcessorLoggingDecorator(
    IGroupProcessor inner,
    ILogger<GroupProcessorLoggingDecorator> logger
) : IGroupProcessor
{
    private readonly IGroupProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<GroupProcessorLoggingDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<CreateGroupResult> CreateGroupAsync(CreateGroupRequest request) =>
        await _logger.ExecuteWithLogging(
            nameof(GroupProcessor),
            request,
            () => _inner.CreateGroupAsync(request),
            logResult: true
        );

    public async Task<AddUsersToGroupResult> AddUsersToGroupAsync(AddUsersToGroupRequest request) =>
        await _logger.ExecuteWithLogging(
            nameof(GroupProcessor),
            request,
            () => _inner.AddUsersToGroupAsync(request),
            logResult: true
        );

    public async Task<RemoveUsersFromGroupResult> RemoveUsersFromGroupAsync(
        RemoveUsersFromGroupRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(GroupProcessor),
            request,
            () => _inner.RemoveUsersFromGroupAsync(request),
            logResult: true
        );

    public async Task<AssignRolesToGroupResult> AssignRolesToGroupAsync(
        AssignRolesToGroupRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(GroupProcessor),
            request,
            () => _inner.AssignRolesToGroupAsync(request),
            logResult: true
        );

    public async Task<RemoveRolesFromGroupResult> RemoveRolesFromGroupAsync(
        RemoveRolesFromGroupRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(GroupProcessor),
            request,
            () => _inner.RemoveRolesFromGroupAsync(request),
            logResult: true
        );

    public async Task<DeleteGroupResult> DeleteGroupAsync(int groupId) =>
        await _logger.ExecuteWithLogging(
            nameof(GroupProcessor),
            groupId,
            () => _inner.DeleteGroupAsync(groupId),
            logResult: true
        );
}
