using Corely.Common.Extensions;
using Corely.IAM.Extensions;
using Corely.IAM.Models;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Services;

internal class AuthenticationServiceLoggingDecorator(
    IAuthenticationService inner,
    ILogger<AuthenticationServiceLoggingDecorator> logger
) : IAuthenticationService
{
    private readonly IAuthenticationService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly ILogger<AuthenticationServiceLoggingDecorator> _logger = logger.ThrowIfNull(
        nameof(logger)
    );

    public async Task<SignInResult> SignInAsync(SignInRequest request) =>
        await _logger.ExecuteWithLogging(
            nameof(AuthenticationService),
            request,
            () => _inner.SignInAsync(request),
            logResult: true
        );

    public async Task<SignInResult> SwitchAccountAsync(SwitchAccountRequest request) =>
        await _logger.ExecuteWithLogging(
            nameof(AuthenticationService),
            request,
            () => _inner.SwitchAccountAsync(request),
            logResult: true
        );

    public async Task<bool> SignOutAsync(int userId, string tokenId) =>
        await _logger.ExecuteWithLogging(
            nameof(AuthenticationService),
            new { userId, tokenId },
            () => _inner.SignOutAsync(userId, tokenId),
            logResult: true
        );

    public async Task SignOutAllAsync(int userId) =>
        await _logger.ExecuteWithLogging(
            nameof(AuthenticationService),
            userId,
            () => _inner.SignOutAllAsync(userId)
        );
}
