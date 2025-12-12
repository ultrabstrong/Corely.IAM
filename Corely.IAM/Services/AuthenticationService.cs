using Corely.Common.Extensions;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Processors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Services;

internal class AuthenticationService(
    ILogger<AuthenticationService> logger,
    IUserProcessor userProcessor,
    IBasicAuthProcessor basicAuthProcessor,
    IOptions<SecurityOptions> securityOptions
) : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger = logger.ThrowIfNull(nameof(logger));
    private readonly IUserProcessor _userProcessor = userProcessor.ThrowIfNull(
        nameof(userProcessor)
    );
    private readonly IBasicAuthProcessor _basicAuthProcessor = basicAuthProcessor.ThrowIfNull(
        nameof(basicAuthProcessor)
    );
    private readonly SecurityOptions _securityOptions = securityOptions
        .ThrowIfNull(nameof(securityOptions))
        .Value;

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
                string.Empty,
                [],
                null
            );
        }

        if (user.FailedLoginsSinceLastSuccess >= _securityOptions.MaxLoginAttempts)
        {
            _logger.LogDebug("User {Username} is locked out", request.Username);
            return new SignInResult(
                SignInResultCode.UserLockedError,
                "User is locked out",
                string.Empty,
                [],
                null
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
                string.Empty,
                [],
                null
            );
        }
        user.TotalSuccessfulLogins++;
        user.FailedLoginsSinceLastSuccess = 0;
        user.LastSuccessfulLoginUtc = DateTime.UtcNow;
        await _userProcessor.UpdateUserAsync(user);

        var authTokenResult = await _userProcessor.GetUserAuthTokenAsync(
            new UserAuthTokenRequest(user.Id, request.AccountId)
        );

        _logger.LogDebug("User {Username} signed in", request.Username);

        return new SignInResult(
            SignInResultCode.Success,
            string.Empty,
            authTokenResult?.Token,
            authTokenResult?.Accounts ?? [],
            authTokenResult?.SignedInAccountId
        );
    }

    public async Task<bool> ValidateAuthTokenAsync(int userId, string authToken)
    {
        ArgumentNullException.ThrowIfNull(authToken, nameof(authToken));
        _logger.LogDebug("Validating auth token for user {UserId}", userId);
        return await _userProcessor.IsUserAuthTokenValidAsync(userId, authToken);
    }

    public async Task<bool> SignOutAsync(int userId, string jti)
    {
        ArgumentNullException.ThrowIfNull(jti, nameof(jti));
        _logger.LogDebug("Signing out user {UserId} with jti {Jti}", userId, jti);
        return await _userProcessor.RevokeUserAuthTokenAsync(userId, jti);
    }

    public async Task SignOutAllAsync(int userId)
    {
        _logger.LogDebug("Signing out all sessions for user {UserId}", userId);
        await _userProcessor.RevokeAllUserAuthTokensAsync(userId);
    }
}
