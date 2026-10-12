using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.BasicAuths.Models;
using Corely.IAM.BasicAuths.Processors;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Security.Models;
using Corely.IAM.TotpAuths.Models;
using Corely.IAM.TotpAuths.Processors;
using Corely.IAM.Users.Entities;

namespace Corely.IAM.Security.Providers;

internal class ReauthenticationProvider(
    IRepo<UserEntity> userRepo,
    IBasicAuthProcessor basicAuthProcessor,
    ITotpAuthProcessor totpAuthProcessor,
    ISecurityPolicyProvider securityPolicyProvider,
    TimeProvider timeProvider
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
