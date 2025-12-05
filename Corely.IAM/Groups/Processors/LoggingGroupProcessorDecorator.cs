using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.Groups.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Groups.Processors;

internal class LoggingGroupProcessorDecorator : IGroupProcessor
{
    private readonly IGroupProcessor _inner;
    private readonly ILogger<LoggingGroupProcessorDecorator> _logger;

    public LoggingGroupProcessorDecorator(
        IGroupProcessor inner,
        ILogger<LoggingGroupProcessorDecorator> logger
    )
    {
        _inner = inner.ThrowIfNull(nameof(inner));
        _logger = logger.ThrowIfNull(nameof(logger));
    }

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

    public async Task<AssignRolesToGroupResult> AssignRolesToGroupAsync(
        AssignRolesToGroupRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(GroupProcessor),
            request,
            () => _inner.AssignRolesToGroupAsync(request),
            logResult: true
        );
}
