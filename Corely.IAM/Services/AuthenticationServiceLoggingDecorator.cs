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
        await _logger.ExecuteWithLoggingAsync(
            nameof(AuthenticationService),
            request,
            () => _inner.SignInAsync(request),
            logResult: true
        );

    public async Task<SignInResult> SwitchAccountAsync(SwitchAccountRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AuthenticationService),
            request,
            () => _inner.SwitchAccountAsync(request),
            logResult: true
        );

    public async Task<bool> SignOutAsync(SignOutRequest request) =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AuthenticationService),
            request,
            () => _inner.SignOutAsync(request),
            logResult: true
        );

    public async Task SignOutAllAsync() =>
        await _logger.ExecuteWithLoggingAsync(
            nameof(AuthenticationService),
            _inner.SignOutAllAsync
        );
}
