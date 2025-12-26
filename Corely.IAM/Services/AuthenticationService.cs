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
    IOptions<SecurityOptions> securityOptions,
    TimeProvider timeProvider
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
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));

    public async Task<SignInResult> SignInAsync(SignInRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        _logger.LogDebug("Signing in user {Username}", request.Username);

        var userEntity = await _userRepo.GetAsync(u => u.Username == request.Username);
        if (userEntity == null)
        {
            _logger.LogDebug("User {Username} not found", request.Username);
            return CreateFailedSignInResult(SignInResultCode.UserNotFoundError, "User not found");
        }

        if (userEntity.FailedLoginsSinceLastSuccess >= _securityOptions.MaxLoginAttempts)
        {
            _logger.LogDebug("User {Username} is locked out", request.Username);
            return CreateFailedSignInResult(SignInResultCode.UserLockedError, "User is locked out");
        }

        var verifyResult = await _basicAuthProcessor.VerifyBasicAuthAsync(
            new(userEntity.Id, request.Password)
        );
        if (verifyResult.ResultCode != VerifyBasicAuthResultCode.Success || !verifyResult.IsValid)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            userEntity.FailedLoginsSinceLastSuccess++;
            userEntity.TotalFailedLogins++;
            userEntity.LastFailedLoginUtc = now;

            await _userRepo.UpdateAsync(userEntity);

            _logger.LogDebug(
                "User {Username} failed to sign in (invalid password)",
                request.Username
            );

            return CreateFailedSignInResult(
                SignInResultCode.PasswordMismatchError,
                "Invalid password"
            );
        }

        var successNow = _timeProvider.GetUtcNow().UtcDateTime;
        userEntity.TotalSuccessfulLogins++;
        userEntity.FailedLoginsSinceLastSuccess = 0;
        userEntity.LastSuccessfulLoginUtc = successNow;
        await _userRepo.UpdateAsync(userEntity);

        var result = await GenerateAuthTokenAndSetContextAsync(
            userEntity.Id,
            request.AccountPublicId,
            "sign in"
        );

        if (result.ResultCode == SignInResultCode.Success)
        {
            _logger.LogDebug("User {Username} signed in", request.Username);
        }

        return result;
    }

    public async Task<SignInResult> SwitchAccountAsync(SwitchAccountRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        _logger.LogDebug("Switching to account {AccountPublicId}", request.AccountPublicId);

        var validationResult = await _authenticationProvider.ValidateUserAuthTokenAsync(
            request.AuthToken
        );

        if (validationResult.ResultCode != UserAuthTokenValidationResultCode.Success)
        {
            _logger.LogDebug(
                "Auth token validation failed: {ResultCode}",
                validationResult.ResultCode
            );
            return CreateFailedSignInResult(
                SignInResultCode.InvalidAuthTokenError,
                $"Auth token validation failed: {validationResult.ResultCode}"
            );
        }

        if (!validationResult.UserId.HasValue)
        {
            _logger.LogDebug("Auth token does not contain user ID");
            return CreateFailedSignInResult(
                SignInResultCode.InvalidAuthTokenError,
                "Auth token does not contain user ID"
            );
        }

        var result = await GenerateAuthTokenAndSetContextAsync(
            validationResult.UserId.Value,
            request.AccountPublicId,
            "account switch"
        );

        if (result.ResultCode == SignInResultCode.Success)
        {
            _logger.LogDebug(
                "User {UserId} switched to account {AccountId}",
                validationResult.UserId.Value,
                result.SignedInAccountId
            );
        }

        return result;
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

    private async Task<SignInResult> GenerateAuthTokenAndSetContextAsync(
        int userId,
        Guid? accountPublicId,
        string operationName
    )
    {
        var authTokenResult = await _authenticationProvider.GetUserAuthTokenAsync(
            new UserAuthTokenRequest(userId, accountPublicId)
        );

        if (authTokenResult.ResultCode != UserAuthTokenResultCode.Success)
        {
            var (signInResultCode, message) = MapAuthTokenResultCode(
                authTokenResult.ResultCode,
                accountPublicId
            );

            _logger.LogWarning(
                "Failed to create auth token for {OperationName}: {ResultCode}",
                operationName,
                authTokenResult.ResultCode
            );

            return CreateFailedSignInResult(signInResultCode, message);
        }

        _userContextSetter.SetUserContext(
            new UserContext(userId, authTokenResult.SignedInAccountId, authTokenResult.Accounts)
        );

        return new SignInResult(
            SignInResultCode.Success,
            null,
            authTokenResult.Token,
            authTokenResult.TokenId,
            authTokenResult.Accounts,
            authTokenResult.SignedInAccountId
        );
    }

    private static (SignInResultCode, string) MapAuthTokenResultCode(
        UserAuthTokenResultCode resultCode,
        Guid? accountPublicId
    ) =>
        resultCode switch
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
                $"Account {accountPublicId} not found for user"
            ),
            _ => (SignInResultCode.UserNotFoundError, "Unknown error creating auth token"),
        };

    private static SignInResult CreateFailedSignInResult(
        SignInResultCode resultCode,
        string message
    ) => new(resultCode, message, null, null, [], null);
}
