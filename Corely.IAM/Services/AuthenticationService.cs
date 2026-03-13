using System.Security.Cryptography;
using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.GoogleAuths.Processors;
using Corely.IAM.GoogleAuths.Providers;
using Corely.IAM.MfaChallenges.Constants;
using Corely.IAM.MfaChallenges.Entities;
using Corely.IAM.Models;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Processors;
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
    ITotpAuthProcessor totpAuthProcessor,
    IGoogleAuthProcessor googleAuthProcessor,
    IGoogleIdTokenValidator googleIdTokenValidator,
    IRepo<MfaChallengeEntity> mfaChallengeRepo,
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
    private readonly ITotpAuthProcessor _totpAuthProcessor = totpAuthProcessor.ThrowIfNull(
        nameof(totpAuthProcessor)
    );
    private readonly IGoogleAuthProcessor _googleAuthProcessor = googleAuthProcessor.ThrowIfNull(
        nameof(googleAuthProcessor)
    );
    private readonly IGoogleIdTokenValidator _googleIdTokenValidator =
        googleIdTokenValidator.ThrowIfNull(nameof(googleIdTokenValidator));
    private readonly IRepo<MfaChallengeEntity> _mfaChallengeRepo = mfaChallengeRepo.ThrowIfNull(
        nameof(mfaChallengeRepo)
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

        if (userEntity.LockedUtc != null)
        {
            var cooldownExpiry = userEntity.LockedUtc.Value.AddSeconds(
                _securityOptions.LockoutCooldownSeconds
            );
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            if (cooldownExpiry > now)
            {
                _logger.LogDebug("User {Username} is locked out", request.Username);
                return CreateFailedSignInResult(
                    SignInResultCode.UserLockedError,
                    "User is locked out"
                );
            }

            userEntity.LockedUtc = null;
            userEntity.FailedLoginsSinceLastSuccess = 0;
        }

        var verifyResult = await _basicAuthProcessor.VerifyBasicAuthAsync(
            new(userEntity.Id, request.Password)
        );
        if (verifyResult.ResultCode != VerifyBasicAuthResultCode.Success || !verifyResult.IsValid)
        {
            var failNow = _timeProvider.GetUtcNow().UtcDateTime;
            userEntity.FailedLoginsSinceLastSuccess++;
            userEntity.TotalFailedLogins++;
            userEntity.LastFailedLoginUtc = failNow;

            if (userEntity.FailedLoginsSinceLastSuccess >= _securityOptions.MaxLoginAttempts)
            {
                userEntity.LockedUtc = failNow;
            }

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

        if (await _totpAuthProcessor.IsTotpEnabledAsync(userEntity.Id))
        {
            _logger.LogDebug(
                "User {Username} has TOTP enabled, creating MFA challenge",
                request.Username
            );
            return await CreateMfaChallengeAsync(
                userEntity.Id,
                request.DeviceId,
                request.AccountId
            );
        }

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

    public async Task<SignInResult> SignInWithGoogleAsync(SignInWithGoogleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        _logger.LogDebug("Signing in with Google ID token");

        var payload = await _googleIdTokenValidator.ValidateAsync(request.GoogleIdToken);
        if (payload == null)
        {
            _logger.LogDebug("Google ID token validation failed");
            return CreateFailedSignInResult(
                SignInResultCode.InvalidGoogleTokenError,
                "Invalid Google ID token"
            );
        }

        var userId = await _googleAuthProcessor.GetUserIdByGoogleSubjectAsync(payload.Subject);
        if (userId == null)
        {
            _logger.LogDebug("No user linked to Google subject {Subject}", payload.Subject);
            return CreateFailedSignInResult(
                SignInResultCode.GoogleAuthNotLinkedError,
                "No account linked to this Google identity"
            );
        }

        var userEntity = await _userRepo.GetAsync(u => u.Id == userId.Value);
        if (userEntity == null)
        {
            _logger.LogDebug("User {UserId} not found for Google sign-in", userId.Value);
            return CreateFailedSignInResult(SignInResultCode.UserNotFoundError, "User not found");
        }

        if (userEntity.LockedUtc != null)
        {
            var cooldownExpiry = userEntity.LockedUtc.Value.AddSeconds(
                _securityOptions.LockoutCooldownSeconds
            );
            var now = _timeProvider.GetUtcNow().UtcDateTime;

            if (cooldownExpiry > now)
            {
                _logger.LogDebug("User {UserId} is locked out", userId.Value);
                return CreateFailedSignInResult(
                    SignInResultCode.UserLockedError,
                    "User is locked out"
                );
            }

            userEntity.LockedUtc = null;
            userEntity.FailedLoginsSinceLastSuccess = 0;
        }

        var successNow = _timeProvider.GetUtcNow().UtcDateTime;
        userEntity.TotalSuccessfulLogins++;
        userEntity.FailedLoginsSinceLastSuccess = 0;
        userEntity.LastSuccessfulLoginUtc = successNow;
        await _userRepo.UpdateAsync(userEntity);

        if (await _totpAuthProcessor.IsTotpEnabledAsync(userEntity.Id))
        {
            _logger.LogDebug(
                "User {UserId} has TOTP enabled, creating MFA challenge for Google sign-in",
                userEntity.Id
            );
            return await CreateMfaChallengeAsync(
                userEntity.Id,
                request.DeviceId,
                request.AccountId
            );
        }

        var result = await GenerateAuthTokenAndSetContextAsync(
            userEntity.Id,
            request.AccountId,
            request.DeviceId,
            "Google sign in"
        );

        if (result.ResultCode == SignInResultCode.Success)
        {
            _logger.LogDebug("User {UserId} signed in with Google", userEntity.Id);
        }

        return result;
    }

    public async Task<SignInResult> VerifyMfaAsync(VerifyMfaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        _logger.LogDebug("Verifying MFA challenge");

        var challenge = await _mfaChallengeRepo.GetAsync(c =>
            c.ChallengeToken == request.MfaChallengeToken
        );
        if (challenge == null)
        {
            _logger.LogDebug("MFA challenge not found");
            return CreateFailedSignInResult(
                SignInResultCode.MfaChallengeExpiredError,
                "MFA challenge not found or expired"
            );
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (challenge.CompletedUtc != null)
        {
            _logger.LogDebug("MFA challenge already completed");
            return CreateFailedSignInResult(
                SignInResultCode.MfaChallengeExpiredError,
                "MFA challenge already completed"
            );
        }

        if (challenge.ExpiresUtc <= now)
        {
            _logger.LogDebug("MFA challenge expired");
            return CreateFailedSignInResult(
                SignInResultCode.MfaChallengeExpiredError,
                "MFA challenge expired"
            );
        }

        if (challenge.FailedAttempts >= MfaChallengeConstants.MAX_ATTEMPTS)
        {
            _logger.LogDebug("MFA challenge max attempts exceeded");
            return CreateFailedSignInResult(
                SignInResultCode.MfaChallengeExpiredError,
                "MFA challenge max attempts exceeded"
            );
        }

        var verifyResult = await _totpAuthProcessor.VerifyTotpOrRecoveryCodeAsync(
            new VerifyTotpOrRecoveryCodeRequest(challenge.UserId, request.Code)
        );

        if (
            verifyResult.ResultCode != VerifyTotpOrRecoveryCodeResultCode.TotpCodeValid
            && verifyResult.ResultCode != VerifyTotpOrRecoveryCodeResultCode.RecoveryCodeValid
        )
        {
            challenge.FailedAttempts++;
            await _mfaChallengeRepo.UpdateAsync(challenge);

            _logger.LogDebug(
                "MFA verification failed for user {UserId}, attempt {Attempt}",
                challenge.UserId,
                challenge.FailedAttempts
            );

            return CreateFailedSignInResult(
                SignInResultCode.InvalidMfaCodeError,
                "Invalid MFA code"
            );
        }

        challenge.CompletedUtc = now;
        await _mfaChallengeRepo.UpdateAsync(challenge);

        _logger.LogDebug("MFA challenge verified for user {UserId}", challenge.UserId);

        var result = await GenerateAuthTokenAndSetContextAsync(
            challenge.UserId,
            challenge.AccountId,
            challenge.DeviceId,
            "MFA verification"
        );

        if (result.ResultCode == SignInResultCode.Success)
        {
            _logger.LogDebug("User {UserId} signed in after MFA verification", challenge.UserId);
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

    private async Task<SignInResult> CreateMfaChallengeAsync(
        Guid userId,
        string deviceId,
        Guid? accountId
    )
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(
            MfaChallengeConstants.CHALLENGE_TOKEN_BYTES
        );
        var challengeToken = Convert.ToBase64String(tokenBytes);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var entity = new MfaChallengeEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            ChallengeToken = challengeToken,
            DeviceId = deviceId,
            AccountId = accountId,
            ExpiresUtc = now.AddSeconds(_securityOptions.MfaChallengeTimeoutSeconds),
        };
        await _mfaChallengeRepo.CreateAsync(entity);

        return new SignInResult(
            SignInResultCode.MfaRequiredChallenge,
            "MFA verification required",
            null,
            null,
            challengeToken
        );
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
            UserAuthTokenResultCode.UserNotFoundError => (
                SignInResultCode.UserNotFoundError,
                "User not found"
            ),
            UserAuthTokenResultCode.SignatureKeyNotFoundError => (
                SignInResultCode.SignatureKeyNotFoundError,
                "User signature key not found"
            ),
            UserAuthTokenResultCode.AccountNotFoundError => (
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
