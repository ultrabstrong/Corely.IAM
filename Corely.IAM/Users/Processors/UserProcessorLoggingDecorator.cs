using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.Users.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Users.Processors;

internal class UserProcessorLoggingDecorator(
    IUserProcessor inner,
    ILogger<UserProcessorLoggingDecorator> logger
) : IUserProcessor
{
    private readonly IUserProcessor _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<UserProcessorLoggingDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<CreateUserResult> CreateUserAsync(CreateUserRequest request) =>
        await _logger.ExecuteWithLogging(
            nameof(UserProcessor),
            request,
            () => _inner.CreateUserAsync(request),
            logResult: true
        );

    public async Task<User?> GetUserAsync(int userId) =>
        await _logger.ExecuteWithLogging(
            nameof(UserProcessor),
            userId,
            () => _inner.GetUserAsync(userId),
            logResult: false
        );

    public async Task<User?> GetUserAsync(string userName) =>
        await _logger.ExecuteWithLogging(
            nameof(UserProcessor),
            userName,
            () => _inner.GetUserAsync(userName),
            logResult: false
        );

    public async Task UpdateUserAsync(User user) =>
        await _logger.ExecuteWithLogging(
            nameof(UserProcessor),
            user,
            () => _inner.UpdateUserAsync(user)
        );

    public async Task<string?> GetAsymmetricSignatureVerificationKeyAsync(int userId) =>
        await _logger.ExecuteWithLogging(
            nameof(UserProcessor),
            userId,
            () => _inner.GetAsymmetricSignatureVerificationKeyAsync(userId),
            logResult: true
        );

    public async Task<AssignRolesToUserResult> AssignRolesToUserAsync(
        AssignRolesToUserRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(UserProcessor),
            request,
            () => _inner.AssignRolesToUserAsync(request),
            logResult: true
        );

    public async Task<RemoveRolesFromUserResult> RemoveRolesFromUserAsync(
        RemoveRolesFromUserRequest request
    ) =>
        await _logger.ExecuteWithLogging(
            nameof(UserProcessor),
            request,
            () => _inner.RemoveRolesFromUserAsync(request),
            logResult: true
        );

    public async Task<DeleteUserResult> DeleteUserAsync(int userId) =>
        await _logger.ExecuteWithLogging(
            nameof(UserProcessor),
            userId,
            () => _inner.DeleteUserAsync(userId),
            logResult: true
        );
}
