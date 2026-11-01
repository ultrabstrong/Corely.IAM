using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Entities;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.GoogleAuths.Entities;
using Corely.IAM.GoogleAuths.Providers;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Security.Models;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Processors;
using Corely.IAM.Users.Entities;
using Microsoft.Extensions.Options;

namespace Corely.IAM.Security.Providers;

internal class ReauthenticationProvider(
    IRepo<UserEntity> userRepo,
    IBasicAuthProcessor basicAuthProcessor,
    ITotpAuthProcessor totpAuthProcessor,
    ISecurityPolicyProvider securityPolicyProvider,
    TimeProvider timeProvider,
    IReadonlyRepo<BasicAuthEntity> basicAuthRepo,
    IReadonlyRepo<GoogleAuthEntity> googleAuthRepo,
    IGoogleIdTokenValidator googleIdTokenValidator,
    IOptions<SecurityOptions> securityOptions
) : IReauthenticationProvider
{
    private readonly IRepo<UserEntity> _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
    private readonly IBasicAuthProcessor _basicAuthProcessor = basicAuthProcessor.ThrowIfNull(
        nameof(basicAuthProcessor)
    );
    private readonly ITotpAuthProcessor _totpAuthProcessor = totpAuthProcessor.ThrowIfNull(
        nameof(totpAuthProcessor)
    );
    private readonly ISecurityPolicyProvider _securityPolicyProvider =
        securityPolicyProvider.ThrowIfNull(nameof(securityPolicyProvider));
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));
    private readonly IReadonlyRepo<BasicAuthEntity> _basicAuthRepo = basicAuthRepo.ThrowIfNull(
        nameof(basicAuthRepo)
    );
    private readonly IReadonlyRepo<GoogleAuthEntity> _googleAuthRepo = googleAuthRepo.ThrowIfNull(
        nameof(googleAuthRepo)
    );
    private readonly IGoogleIdTokenValidator _googleIdTokenValidator =
        googleIdTokenValidator.ThrowIfNull(nameof(googleIdTokenValidator));
    private readonly SecurityOptions _securityOptions = securityOptions
        .ThrowIfNull(nameof(securityOptions))
        .Value;

    public async Task<ReauthenticationResult> VerifyIdentityAsync(
        Guid userId,
        string? currentPassword,
        string? googleIdToken,
        string? googleNonce
    )
    {
        if (await _basicAuthRepo.AnyAsync(b => b.UserId == userId))
        {
            return await VerifyCurrentPasswordAsync(userId, currentPassword);
        }

        var googleAuth = await _googleAuthRepo.GetAsync(g => g.UserId == userId);
        if (googleAuth == null || string.IsNullOrWhiteSpace(_securityOptions.GoogleClientId))
        {
            return new(
                ReauthenticationResultCode.NoReauthenticationMethodError,
                "This needs proof that it is you, and you have no way to give it yet. Set a password on your profile, then try again."
            );
        }

        var userEntity = await _userRepo.GetAsync(u => u.Id == userId);
        var policy = await _securityPolicyProvider.GetPolicyAsync();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (userEntity?.LockedUtc?.AddSeconds(policy.LockoutCooldownSeconds) > now)
        {
            return new(ReauthenticationResultCode.UserLockedError, "User is locked out");
        }

        if (string.IsNullOrWhiteSpace(googleIdToken))
        {
            return new(
                ReauthenticationResultCode.GoogleSignInRequiredError,
                "Sign in with Google again to continue"
            );
        }

        var payload = await _googleIdTokenValidator.ValidateAsync(googleIdToken);
        return
            payload?.Subject == googleAuth.GoogleSubjectId
            && payload.ProvesAFreshSignIn(googleNonce, now)
            ? ReauthenticationResult.Succeeded
            : new(
                ReauthenticationResultCode.InvalidGoogleSignInError,
                "That Google sign in could not be used. Sign in with Google again, using the account linked to you."
            );
    }

    public async Task<ReauthenticationResult> VerifyCurrentPasswordAsync(
        Guid userId,
        string? currentPassword
    )
    {
        if (string.IsNullOrWhiteSpace(currentPassword))
        {
            return new(
                ReauthenticationResultCode.InvalidCurrentPasswordError,
                "Current password is required"
            );
        }

        var userEntity = await _userRepo.GetAsync(u => u.Id == userId);
        if (userEntity == null)
        {
            return new(
                ReauthenticationResultCode.InvalidCurrentPasswordError,
                "Current password is incorrect"
            );
        }

        var policy = await _securityPolicyProvider.GetPolicyAsync();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (userEntity.LockedUtc != null)
        {
            if (userEntity.LockedUtc.Value.AddSeconds(policy.LockoutCooldownSeconds) > now)
            {
                return new(ReauthenticationResultCode.UserLockedError, "User is locked out");
            }

            userEntity.LockedUtc = null;
            userEntity.FailedLoginsSinceLastSuccess = 0;
        }

        var verifyResult = await _basicAuthProcessor.VerifyBasicAuthAsync(
            new(userId, currentPassword)
        );
        if (verifyResult.ResultCode == VerifyBasicAuthResultCode.UserNotFoundError)
        {
            return new(
                ReauthenticationResultCode.BasicAuthNotFoundError,
                "No password set for this user"
            );
        }

        if (verifyResult.ResultCode == VerifyBasicAuthResultCode.Success && verifyResult.IsValid)
        {
            return ReauthenticationResult.Succeeded;
        }

        userEntity.FailedLoginsSinceLastSuccess++;
        userEntity.TotalFailedLogins++;
        userEntity.LastFailedLoginUtc = now;
        if (userEntity.FailedLoginsSinceLastSuccess >= policy.MaxLoginAttempts)
        {
            userEntity.LockedUtc = now;
        }
        await _userRepo.UpdateAsync(userEntity);

        return new(
            ReauthenticationResultCode.InvalidCurrentPasswordError,
            "Current password is incorrect"
        );
    }

    public async Task<ReauthenticationResult> VerifyTotpCodeAsync(Guid userId, string? totpCode)
    {
        if (!await _totpAuthProcessor.IsTotpEnabledAsync(userId))
        {
            return ReauthenticationResult.Succeeded;
        }

        if (string.IsNullOrWhiteSpace(totpCode))
        {
            return new(
                ReauthenticationResultCode.TotpRequiredError,
                "A TOTP or recovery code is required"
            );
        }

        var verifyResult = await _totpAuthProcessor.VerifyTotpOrRecoveryCodeAsync(
            new(userId, totpCode)
        );
        return
            verifyResult.ResultCode
                is VerifyTotpOrRecoveryCodeResultCode.TotpCodeValid
                    or VerifyTotpOrRecoveryCodeResultCode.RecoveryCodeValid
            ? ReauthenticationResult.Succeeded
            : new(ReauthenticationResultCode.InvalidTotpCodeError, "Invalid TOTP or recovery code");
    }
}
