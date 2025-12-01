using Corely.Common.Extensions;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Processors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Services;

internal class SignInService : ISignInService
{
    private readonly ILogger<SignInService> _logger;
    private readonly IUserProcessor _userProcessor;
    private readonly IBasicAuthProcessor _basicAuthProcessor;
    private readonly SecurityOptions _securityOptions;

    public SignInService(
        ILogger<SignInService> logger,
        IUserProcessor userProcessor,
        IBasicAuthProcessor basicAuthProcessor,
        IOptions<SecurityOptions> securityOptions
    )
    {
        _logger = logger.ThrowIfNull(nameof(logger));
        _userProcessor = userProcessor.ThrowIfNull(nameof(userProcessor));
        _basicAuthProcessor = basicAuthProcessor.ThrowIfNull(nameof(basicAuthProcessor));
        _securityOptions = securityOptions.ThrowIfNull(nameof(securityOptions)).Value;
    }

    public async Task<SignInResult> SignInAsync(SignInRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        _logger.LogDebug("Signing in user {Username}", request.Username);

        var user = await _userProcessor.GetUserAsync(request.Username);
        if (user == null)
        {
            _logger.LogDebug("User {Username} not found", request.Username);
            return new SignInResult(
                SignInResultCode.UserNotFoundError,
                "User not found",
                string.Empty
            );
        }

        if (user.FailedLoginsSinceLastSuccess >= _securityOptions.MaxLoginAttempts)
        {
            _logger.LogDebug("User {Username} is locked out", request.Username);
            return new SignInResult(
                SignInResultCode.UserLockedError,
                "User is locked out",
                string.Empty
            );
        }

        var isValidPassword = await _basicAuthProcessor.VerifyBasicAuthAsync(
            new(user.Id, request.Password)
        );
        if (!isValidPassword)
        {
            user.FailedLoginsSinceLastSuccess++;
            user.TotalFailedLogins++;
            user.LastFailedLoginUtc = DateTime.UtcNow;

            await _userProcessor.UpdateUserAsync(user);

            _logger.LogDebug(
                "User {Username} failed to sign in (invalid password)",
                request.Username
            );

            return new SignInResult(
                SignInResultCode.PasswordMismatchError,
                "Invalid password",
                string.Empty
            );
        }
        user.TotalSuccessfulLogins++;
        user.FailedLoginsSinceLastSuccess = 0;
        user.LastSuccessfulLoginUtc = DateTime.UtcNow;
        await _userProcessor.UpdateUserAsync(user);

        var authToken = await _userProcessor.GetUserAuthTokenAsync(user.Id);

        _logger.LogDebug("User {Username} signed in", request.Username);

        return new SignInResult(SignInResultCode.Success, string.Empty, authToken);
    }
}
