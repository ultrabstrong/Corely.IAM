using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Mappers;
using Corely.IAM.ApiKeys.Entities;
using Corely.IAM.ApiKeys.Enums;
using Corely.IAM.ApiKeys.Mappers;
using Corely.IAM.ApiKeys.Models;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Security.Providers;
using Corely.Security.Hashing.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.ApiKeys.Providers;

internal class ApiKeyAuthenticationProvider(
    IRepo<ApiKeyEntity> apiKeyRepo,
    IRepo<ApiKeySecretEntity> secretRepo,
    ISessionTokenProvider sessionTokenProvider,
    ISecurityPolicyProvider securityPolicyProvider,
    IHashProviderFactory hashProviderFactory,
    ILogger<ApiKeyAuthenticationProvider> logger,
    TimeProvider timeProvider
) : IApiKeyAuthenticationProvider
{
    private readonly IRepo<ApiKeyEntity> _apiKeyRepo = apiKeyRepo.ThrowIfNull(nameof(apiKeyRepo));
    private readonly IRepo<ApiKeySecretEntity> _secretRepo = secretRepo.ThrowIfNull(
        nameof(secretRepo)
    );
    private readonly ISessionTokenProvider _sessionTokenProvider = sessionTokenProvider.ThrowIfNull(
        nameof(sessionTokenProvider)
    );
    private readonly ISecurityPolicyProvider _securityPolicyProvider =
        securityPolicyProvider.ThrowIfNull(nameof(securityPolicyProvider));
    private readonly IHashProviderFactory _hashProviderFactory = hashProviderFactory.ThrowIfNull(
        nameof(hashProviderFactory)
    );
    private readonly ILogger<ApiKeyAuthenticationProvider> _logger = logger.ThrowIfNull(
        nameof(logger)
    );
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));

    public async Task<ApiKeyAuthTokenResult> GetApiKeyAuthTokenAsync(
        GetApiKeyAuthTokenRequest request
    )
    {
        var policy = await _securityPolicyProvider.GetPolicyAsync();
        if (
            !policy.ApiKeysEnabled || !ApiKeyCredential.TryParse(request.ApiKey, out var credential)
        )
            return Invalid("API keys are disabled or the key is malformed");

        var secret = await _secretRepo.GetAsync(
            s => s.Prefix == credential.Prefix,
            include: q =>
                q.Include(s => s.ApiKey!)
                        .ThenInclude(k => k.Account!)
                            .ThenInclude(a => a.AsymmetricKeys)
        );
        if (secret?.ApiKey?.Account is not { } account)
            return Invalid("No API key has the prefix");

        var key = secret.ApiKey;
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (!policy.AllowsApiKeysIn(account.IsPlatformAccount))
            return Invalid($"API keys are disabled for the platform account, key {key.Id}");

        if (key.LockedUtc is { } lockedUtc)
        {
            if (lockedUtc.AddSeconds(policy.LockoutCooldownSeconds) > now)
                return Invalid($"API key {key.Id} is locked out");

            key.LockedUtc = null;
            key.FailedLoginsSinceLastSuccess = 0;
        }

        if (secret.ExpiresUtc <= now)
            return Invalid($"The {secret.Slot} secret of API key {key.Id} has expired");

        if (!secret.SecretHash.ToHashedValue(_hashProviderFactory).Verify(credential.Secret))
        {
            key.FailedLoginsSinceLastSuccess++;
            key.TotalFailedLogins++;
            key.LastFailedLoginUtc = now;
            if (key.FailedLoginsSinceLastSuccess >= policy.MaxLoginAttempts)
                key.LockedUtc = now;
            await _apiKeyRepo.UpdateAsync(key);
            return Invalid($"Wrong secret for API key {key.Id}");
        }

        key.TotalSuccessfulLogins++;
        key.FailedLoginsSinceLastSuccess = 0;
        key.LastSuccessfulLoginUtc = now;
        await _apiKeyRepo.UpdateAsync(key);
        secret.LastUsedUtc = now;
        await _secretRepo.UpdateAsync(secret);

        await _sessionTokenProvider.RevokeActiveAsync(t =>
            t.ApiKeyId == key.Id && t.DeviceId == request.DeviceId
        );

        return await IssueAsync(
            key,
            account,
            secret.Slot,
            request.DeviceId,
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            now
        );
    }

    public async Task<ApiKeyAuthTokenResult> ValidateApiKeyAuthTokenAsync(string authToken)
    {
        var claims = _sessionTokenProvider.ReadClaims(authToken, out _);
        if (
            claims?.PrincipalType != TokenPrincipalType.ApiKey
            || !await _sessionTokenProvider.IsActiveAsync(claims)
            || !(await _securityPolicyProvider.GetPolicyAsync()).ApiKeysEnabled
        )
            return ApiKeyAuthTokenResult.Failed(ApiKeyAuthTokenResultCode.TokenValidationFailed);

        var key = await LoadKeyAsync(claims.SubjectId);
        if (
            key?.Account is not { } account
            || !_sessionTokenProvider.IsSignedBy(
                authToken,
                account.AsymmetricKeys.Versions(KeyUsedFor.Signature)
            )
        )
            return ApiKeyAuthTokenResult.Failed(ApiKeyAuthTokenResultCode.TokenValidationFailed);

        return new(
            ApiKeyAuthTokenResultCode.Success,
            authToken,
            claims.TokenId,
            key.ToModel(),
            account.ToModel(),
            claims.DeviceId,
            null,
            null
        );
    }

    public async Task<ApiKeyAuthTokenResult> RenewApiKeyAuthTokenAsync(SessionTokenRenewal renewal)
    {
        if (
            renewal.Code != SessionTokenRenewalCode.Ready
            || renewal.Token?.ApiKeyId is not { } keyId
        )
            return ApiKeyAuthTokenResult.Failed(
                renewal.Code switch
                {
                    SessionTokenRenewalCode.SessionExpired =>
                        ApiKeyAuthTokenResultCode.SessionExpiredError,
                    SessionTokenRenewalCode.AlreadyRotated =>
                        ApiKeyAuthTokenResultCode.RefreshTokenAlreadyRotatedError,
                    SessionTokenRenewalCode.Reused =>
                        ApiKeyAuthTokenResultCode.RefreshTokenReusedError,
                    _ => ApiKeyAuthTokenResultCode.TokenValidationFailed,
                }
            );

        var token = renewal.Token;
        var policy = await _securityPolicyProvider.GetPolicyAsync();
        if (!policy.ApiKeysEnabled)
            return ApiKeyAuthTokenResult.Failed(ApiKeyAuthTokenResultCode.TokenValidationFailed);

        var key = await LoadKeyAsync(keyId);
        var secret = key?.Secrets?.FirstOrDefault(s => s.Slot == token.Slot);
        if (secret == null || secret.ExpiresUtc <= _timeProvider.GetUtcNow().UtcDateTime)
        {
            await _sessionTokenProvider.EndAsync(token);
            return ApiKeyAuthTokenResult.Failed(ApiKeyAuthTokenResultCode.SessionExpiredError);
        }

        if (key!.Account is not { } account || !policy.AllowsApiKeysIn(account.IsPlatformAccount))
            return ApiKeyAuthTokenResult.Failed(ApiKeyAuthTokenResultCode.TokenValidationFailed);

        if (await _sessionTokenProvider.ClaimRenewalAsync(token) is not { } renewedTokenId)
            return ApiKeyAuthTokenResult.Failed(
                ApiKeyAuthTokenResultCode.RefreshTokenAlreadyRotatedError
            );

        return await IssueAsync(
            key,
            account,
            secret.Slot,
            token.DeviceId,
            renewedTokenId,
            token.SessionId,
            token.SessionStartedUtc
        );
    }

    public async Task<bool> RevokeApiKeyAuthTokenByIdAsync(Guid apiKeyId, Guid tokenId) =>
        await _sessionTokenProvider.RevokeActiveAsync(t =>
            t.Id == tokenId && t.ApiKeyId == apiKeyId
        ) > 0;

    public async Task RevokeAllApiKeyAuthTokensAsync(Guid apiKeyId) =>
        await _sessionTokenProvider.RevokeActiveAsync(t => t.ApiKeyId == apiKeyId);

    public async Task RevokeApiKeySlotAuthTokensAsync(Guid apiKeyId, ApiKeySlot slot) =>
        await _sessionTokenProvider.RevokeActiveAsync(t =>
            t.ApiKeyId == apiKeyId && t.Slot == slot
        );

    private ApiKeyAuthTokenResult Invalid(string reason)
    {
        _logger.LogInformation("API key sign in refused: {Reason}", reason);
        return ApiKeyAuthTokenResult.Failed(ApiKeyAuthTokenResultCode.InvalidApiKeyError);
    }

    private async Task<ApiKeyEntity?> LoadKeyAsync(Guid apiKeyId) =>
        await _apiKeyRepo.GetAsync(
            k => k.Id == apiKeyId,
            include: q =>
                q.Include(k => k.Secrets)
                    .Include(k => k.Account!)
                        .ThenInclude(a => a.AsymmetricKeys)
        );

    private async Task<ApiKeyAuthTokenResult> IssueAsync(
        ApiKeyEntity key,
        AccountEntity account,
        ApiKeySlot slot,
        string deviceId,
        Guid tokenId,
        Guid sessionId,
        DateTime sessionStartedUtc
    )
    {
        var signatureKey = account.AsymmetricKeys.CurrentVersion(KeyUsedFor.Signature);
        if (signatureKey == null)
            return Invalid($"Account {account.Id} has no signature key");

        var issued = await _sessionTokenProvider.IssueAsync(
            new SessionTokenSubject(
                TokenPrincipalType.ApiKey,
                key.Id,
                signatureKey,
                deviceId,
                [account.Id],
                account.Id,
                slot
            ),
            tokenId,
            sessionId,
            sessionStartedUtc
        );

        return new(
            ApiKeyAuthTokenResultCode.Success,
            issued.Token,
            issued.TokenId,
            key.ToModel(),
            account.ToModel(),
            deviceId,
            issued.RefreshToken,
            issued.RefreshExpiresUtc
        );
    }
}
