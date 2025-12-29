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
    IUserContextProvider userContextProvider,
    IUserContextSetter userContextSetter,
    IAuthorizationCacheClearer authorizationCacheClearer,
    IBasicAuthProcessor basicAuthProcessor,
    IOptions<SecurityOptions> securityOptions,
    TimeProvider timeProvider
) : IAuthenticationService
{
    private readonly ILogger<AuthenticationService> _logger = logger.ThrowIfNull(nameof(logger));
    private readonly IRepo<UserEntity> _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
    private readonly IAuthenticationProvider _authenticationProvider =
        authenticationProvider.ThrowIfNull(nameof(authenticationProvider));
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );
    private readonly IUserContextSetter _userContextSetter = userContextSetter.ThrowIfNull(
        nameof(userContextSetter)
    );
    private readonly IAuthorizationCacheClearer _authorizationCacheClearer =
        authorizationCacheClearer.ThrowIfNull(nameof(authorizationCacheClearer));
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
            request.AccountId,
            request.DeviceId,
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

        var context = _userContextProvider.GetUserContext();
        if (context == null)
        {
            _logger.LogDebug("No user context available for account switch");
            return CreateFailedSignInResult(
                SignInResultCode.InvalidAuthTokenError,
                "No user context available"
            );
        }

        _logger.LogDebug(
            "User {UserId} switching to account {AccountId}",
            context.User.Id,
            request.AccountId
        );

        var result = await GenerateAuthTokenAndSetContextAsync(
            context.User.Id,
            request.AccountId,
            context.DeviceId,
            "account switch"
        );

        if (result.ResultCode == SignInResultCode.Success)
        {
            var newContext = _userContextProvider.GetUserContext();
            _logger.LogDebug(
                "User {UserId} switched to account {AccountId}",
                context.User.Id,
                newContext?.CurrentAccount?.Id
            );
        }

        return result;
    }

    public async Task<bool> SignOutAsync(SignOutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var context = _userContextProvider.GetUserContext();
        if (context == null)
        {
            _logger.LogDebug("No user context available for sign out");
            return false;
        }

        _logger.LogDebug(
            "Signing out user {UserId} with token {TokenId}, account {AccountId}, device {DeviceId}",
            context.User.Id,
            request.TokenId,
            context.CurrentAccount?.Id,
            context.DeviceId
        );

        var revokeRequest = new RevokeUserAuthTokenRequest(
            context.User.Id,
            request.TokenId,
            context.DeviceId,
            context.CurrentAccount?.Id
        );
        var result = await _authenticationProvider.RevokeUserAuthTokenAsync(revokeRequest);
        _userContextSetter.ClearUserContext(context.User.Id);
        _authorizationCacheClearer.ClearCache();

        _logger.LogDebug(
            "User {UserId} signed out with token {TokenId}: {Result}",
            context.User.Id,
            request.TokenId,
            result
        );

        return result;
    }

    public async Task SignOutAllAsync()
    {
        var context = _userContextProvider.GetUserContext();
        if (context == null)
        {
            _logger.LogDebug("No user context available for sign out");
            return;
        }

        var userId = context.User.Id;
        _logger.LogDebug("Signing out all sessions for user {UserId}", userId);

        await _authenticationProvider.RevokeAllUserAuthTokensAsync(userId);
        _userContextSetter.ClearUserContext(userId);
        _authorizationCacheClearer.ClearCache();

        _logger.LogDebug("All sessions signed out for user {UserId}", userId);
    }

    private async Task<SignInResult> GenerateAuthTokenAndSetContextAsync(
        Guid userId,
        Guid? accountId,
        string deviceId,
        string operationName
    )
    {
        var authTokenResult = await _authenticationProvider.GetUserAuthTokenAsync(
            new GetUserAuthTokenRequest(userId, deviceId, accountId)
        );

        if (authTokenResult.ResultCode != UserAuthTokenResultCode.Success)
        {
            var (signInResultCode, message) = MapAuthTokenResultCode(
                authTokenResult.ResultCode,
                accountId
            );

            _logger.LogWarning(
                "Failed to create auth token for {OperationName}: {ResultCode}",
                operationName,
                authTokenResult.ResultCode
            );

            return CreateFailedSignInResult(signInResultCode, message);
        }

        var userContext = new UserContext(
            authTokenResult.User!,
            authTokenResult.CurrentAccount,
            deviceId,
            authTokenResult.AvailableAccounts
        );

        _userContextSetter.SetUserContext(userContext);

        return new SignInResult(
            SignInResultCode.Success,
            null,
            authTokenResult.Token,
            authTokenResult.TokenId
        );
    }

    private static (SignInResultCode, string) MapAuthTokenResultCode(
        UserAuthTokenResultCode resultCode,
        Guid? accountId
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
                $"Account {accountId} not found for user"
            ),
            _ => (SignInResultCode.UserNotFoundError, "Unknown error creating auth token"),
        };

    private static SignInResult CreateFailedSignInResult(
        SignInResultCode resultCode,
        string message
    ) => new(resultCode, message, null, null);
}
