using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Models;
using Corely.IAM.Users.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Services;

internal class AuthenticationService(
    ILogger<AuthenticationService> logger,
    IRepo<UserEntity> userRepo,
    IAuthenticationProvider authenticationProvider,
    IUserContextSetter userContextSetter,
    IBasicAuthProcessor basicAuthProcessor,
    IOptions<SecurityOptions> securityOptions
) : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger = logger.ThrowIfNull(nameof(logger));
    private readonly IRepo<UserEntity> _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
    private readonly IAuthenticationProvider _authenticationProvider =
        authenticationProvider.ThrowIfNull(nameof(authenticationProvider));
    private readonly IUserContextSetter _userContextSetter = userContextSetter.ThrowIfNull(
        nameof(userContextSetter)
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

        var userEntity = await _userRepo.GetAsync(u => u.Username == request.Username);
        if (userEntity == null)
        {
            _logger.LogDebug("User {Username} not found", request.Username);
            return new SignInResult(
                SignInResultCode.UserNotFoundError,
                "User not found",
                null,
                null,
                [],
                null
            );
        }

        if (userEntity.FailedLoginsSinceLastSuccess >= _securityOptions.MaxLoginAttempts)
        {
            _logger.LogDebug("User {Username} is locked out", request.Username);
            return new SignInResult(
                SignInResultCode.UserLockedError,
                "User is locked out",
                null,
                null,
                [],
                null
            );
        }

        var verifyResult = await _basicAuthProcessor.VerifyBasicAuthAsync(
            new(userEntity.Id, request.Password)
        );
        if (verifyResult.ResultCode != VerifyBasicAuthResultCode.Success || !verifyResult.IsValid)
        {
            userEntity.FailedLoginsSinceLastSuccess++;
            userEntity.TotalFailedLogins++;
            userEntity.LastFailedLoginUtc = DateTime.UtcNow;

            await _userRepo.UpdateAsync(userEntity);

            _logger.LogDebug(
                "User {Username} failed to sign in (invalid password)",
                request.Username
            );

            return new SignInResult(
                SignInResultCode.PasswordMismatchError,
                "Invalid password",
                null,
                null,
                [],
                null
            );
        }

        userEntity.TotalSuccessfulLogins++;
        userEntity.FailedLoginsSinceLastSuccess = 0;
        userEntity.LastSuccessfulLoginUtc = DateTime.UtcNow;
        await _userRepo.UpdateAsync(userEntity);

        var authTokenResult = await _authenticationProvider.GetUserAuthTokenAsync(
            new UserAuthTokenRequest(userEntity.Id, request.AccountPublicId)
        );

        if (authTokenResult.ResultCode != UserAuthTokenResultCode.Success)
        {
            var (signInResultCode, message) = authTokenResult.ResultCode switch
            {
                UserAuthTokenResultCode.UserNotFound => (
                    SignInResultCode.UserNotFoundError,
                    "User not found"
                ),
                UserAuthTokenResultCode.SignatureKeyNotFound => (
                    SignInResultCode.SignatureKeyNotFoundError,
                    "User signature key not found"
                ),
                UserAuthTokenResultCode.AccountNotFound => (
                    SignInResultCode.AccountNotFoundError,
                    $"Account {request.AccountPublicId} not found for user"
                ),
                _ => (SignInResultCode.UserNotFoundError, "Unknown error creating auth token"),
            };

            _logger.LogWarning(
                "Failed to create auth token for user {Username}: {ResultCode}",
                request.Username,
                authTokenResult.ResultCode
            );

            return new SignInResult(signInResultCode, message, null, null, [], null);
        }

        _userContextSetter.SetUserContext(
            new UserContext(userEntity.Id, authTokenResult.SignedInAccountId)
        );

        _logger.LogDebug("User {Username} signed in", request.Username);

        return new SignInResult(
            SignInResultCode.Success,
            null,
            authTokenResult.Token,
            authTokenResult.TokenId,
            authTokenResult.Accounts,
            authTokenResult.SignedInAccountId
        );
    }

    public async Task<bool> SignOutAsync(int userId, string tokenId)
    {
        ArgumentNullException.ThrowIfNull(tokenId, nameof(tokenId));
        _logger.LogDebug("Signing out user {UserId} with token {TokenId}", userId, tokenId);

        var result = await _authenticationProvider.RevokeUserAuthTokenAsync(userId, tokenId);
        _userContextSetter.ClearUserContext(userId);

        _logger.LogDebug(
            "User {UserId} signed out with token {TokenId}: {Result}",
            userId,
            tokenId,
            result
        );

        return result;
    }

    public async Task SignOutAllAsync(int userId)
    {
        _logger.LogDebug("Signing out all sessions for user {UserId}", userId);

        await _authenticationProvider.RevokeAllUserAuthTokensAsync(userId);
        _userContextSetter.ClearUserContext(userId);

        _logger.LogDebug("All sessions signed out for user {UserId}", userId);
    }
}
