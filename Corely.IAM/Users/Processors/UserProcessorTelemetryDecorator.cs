using Corely.Common.Extensions;
using Corely.Common.Filtering;
using Corely.Common.Filtering.Ordering;
using Corely.IAM.Extensions;
using Corely.IAM.Models;
using Corely.IAM.Users.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Users.Processors;

internal class UserProcessorTelemetryDecorator(
    IUserProcessor inner,
    ILogger<UserProcessorTelemetryDecorator> logger
) : IUserProcessor
{
    private readonly IUserProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<UserProcessorTelemetryDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<CreateUserResult> CreateUserAsync(CreateUserRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(UserProcessor),
            request,
            () => _inner.CreateUserAsync(request),
            logResult: true
        );

    public async Task<GetUserResult> GetUserAsync(Guid userId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(UserProcessor),
            userId,
            () => _inner.GetUserAsync(userId),
            logResult: true
        );

    public async Task<ModifyResult> UpdateUserAsync(UpdateUserRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(UserProcessor),
            request,
            () => _inner.UpdateUserAsync(request),
            logResult: true
        );

    public async Task<GetAsymmetricKeyResult> GetAsymmetricSignatureVerificationKeyAsync(
        Guid userId
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(UserProcessor),
            userId,
            () => _inner.GetAsymmetricSignatureVerificationKeyAsync(userId),
            logResult: true
        );

    public async Task<AssignRolesToUserResult> AssignRolesToUserAsync(
        AssignRolesToUserRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(UserProcessor),
            request,
            () => _inner.AssignRolesToUserAsync(request),
            logResult: true
        );

    public async Task<RemoveRolesFromUserResult> RemoveRolesFromUserAsync(
        RemoveRolesFromUserRequest request
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(UserProcessor),
            request,
            () => _inner.RemoveRolesFromUserAsync(request),
            logResult: true
        );

    public async Task<DeleteUserResult> DeleteUserAsync(Guid userId) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(UserProcessor),
            userId,
            () => _inner.DeleteUserAsync(userId),
            logResult: true
        );

    public async Task<ListResult<User>> ListUsersAsync(
        FilterBuilder<User>? filter,
        OrderBuilder<User>? order,
        int skip,
        int take
    ) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(UserProcessor),
            () => _inner.ListUsersAsync(filter, order, skip, take),
            logResult: true
        );

    public async Task<GetResult<User>> GetUserByIdAsync(Guid userId, bool hydrate) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(UserProcessor),
            userId,
            () => _inner.GetUserByIdAsync(userId, hydrate),
            logResult: true
        );
}
