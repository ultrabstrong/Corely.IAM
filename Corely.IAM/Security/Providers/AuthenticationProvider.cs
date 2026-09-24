using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Mappers;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Extensions;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Constants;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Mappers;
using Corely.IAM.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Corely.IAM.Security.Providers;

internal class AuthenticationProvider(
    IRepo<UserEntity> userRepo,
    IRepo<UserAuthTokenEntity> authTokenRepo,
    ISecurityProvider securityProcessor,
    IOptions<SecurityOptions> securityOptions,
    ILogger<AuthenticationProvider> logger,
    TimeProvider timeProvider
) : IAuthenticationProvider
{
    private readonly IRepo<UserEntity> _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
    private readonly IRepo<UserAuthTokenEntity> _authTokenRepo = authTokenRepo.ThrowIfNull(
        nameof(authTokenRepo)
    );
    private readonly ISecurityProvider _securityProcessor = securityProcessor.ThrowIfNull(
        nameof(securityProcessor)
    );
    private readonly SecurityOptions _securityOptions = securityOptions
        .ThrowIfNull(nameof(securityOptions))
        .Value;
    private readonly ILogger<AuthenticationProvider> _logger = logger.ThrowIfNull(nameof(logger));
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));

    public async Task<UserAuthTokenResult> GetUserAuthTokenAsync(GetUserAuthTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var tokenIssueContext = await GetTokenIssueContextAsync(request.UserId, request.AccountId);
        if (tokenIssueContext.ResultCode.HasValue)
            return UserAuthTokenResult.Failed(tokenIssueContext.ResultCode.Value);

        await RevokeExistingTokensForUserAccountDeviceAsync(
            request.UserId,
            tokenIssueContext.Context!.SignedInAccount?.Id,
            request.DeviceId
        );

        return await CreateUserAuthTokenAsync(
            tokenIssueContext.Context,
            request.DeviceId,
            request.SessionStartedUtc
        );
    }

    public async Task<RenewUserAuthTokenResult> RenewUserAuthTokenAsync(string authToken)
    {
        ArgumentNullException.ThrowIfNull(authToken, nameof(authToken));

        var tokenHandler = new JwtSecurityTokenHandler();
        if (!tokenHandler.CanReadToken(authToken))
        {
            _logger.LogInformation("Auth token is in invalid format");
            return RenewUserAuthTokenResult.Failed(RenewUserAuthTokenResultCode.InvalidTokenFormat);
        }

        var jwtToken = tokenHandler.ReadJwtToken(authToken);

        var subClaim = jwtToken.ClaimValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrWhiteSpace(subClaim) || !Guid.TryParse(subClaim, out var userId))
        {
            _logger.LogInformation("Auth token does not contain valid sub (userId) claim");
            return RenewUserAuthTokenResult.Failed(RenewUserAuthTokenResultCode.MissingUserIdClaim);
        }

        var deviceId = jwtToken.ClaimValue(UserConstants.DEVICE_ID_CLAIM);
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            _logger.LogInformation("Auth token does not contain device ID");
            return RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.MissingDeviceIdClaim
            );
        }

        var jti = jwtToken.ClaimValue(JwtRegisteredClaimNames.Jti);
        if (string.IsNullOrWhiteSpace(jti) || !Guid.TryParse(jti, out var tokenId))
        {
            _logger.LogInformation("Auth token does not contain a valid jti claim");
            return RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.TokenValidationFailed
            );
        }

        var trackedToken = await _authTokenRepo.GetAsync(t =>
            t.Id == tokenId && t.UserId == userId
        );
        if (trackedToken == null || trackedToken.RevokedUtc != null)
        {
            _logger.LogInformation("Auth token not found or already revoked in server tracking");
            return RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.TokenValidationFailed
            );
        }

        if (!string.Equals(trackedToken.DeviceId, deviceId, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Auth token device ID mismatch for tracked token {TokenId}",
                trackedToken.Id
            );
            return RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.TokenValidationFailed
            );
        }

        var tokenIssueContext = await GetTokenIssueContextAsync(userId, trackedToken.AccountId);
        if (tokenIssueContext.ResultCode.HasValue)
        {
            return RenewUserAuthTokenResult.Failed(
                tokenIssueContext.ResultCode.Value switch
                {
                    UserAuthTokenResultCode.UserNotFoundError =>
                        RenewUserAuthTokenResultCode.UserNotFoundError,
                    UserAuthTokenResultCode.SignatureKeyNotFoundError =>
                        RenewUserAuthTokenResultCode.SignatureKeyNotFoundError,
                    UserAuthTokenResultCode.AccountNotFoundError =>
                        RenewUserAuthTokenResultCode.AccountNotFoundError,
                    _ => RenewUserAuthTokenResultCode.TokenValidationFailed,
                }
            );
        }

        if (!ValidateJwtToken(authToken, tokenIssueContext.Context!.SignatureKey, false))
            return RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.TokenValidationFailed
            );

        var sessionStartedUtc = jwtToken.SessionStartedUtc();
        if (!sessionStartedUtc.HasValue)
        {
            _logger.LogInformation("Auth token does not contain a valid session start timestamp");
            return RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.TokenValidationFailed
            );
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (sessionStartedUtc.Value.AddSeconds(_securityOptions.AuthSessionTtlSeconds) <= now)
        {
            trackedToken.RevokedUtc = now;
            await _authTokenRepo.UpdateAsync(trackedToken);
            return RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.SessionExpiredError
            );
        }

        trackedToken.RevokedUtc = now;
        await _authTokenRepo.UpdateAsync(trackedToken);

        await RevokeExistingTokensForUserAccountDeviceAsync(
            userId,
            tokenIssueContext.Context.SignedInAccount?.Id,
            deviceId
        );

        var renewedTokenResult = await CreateUserAuthTokenAsync(
            tokenIssueContext.Context,
            deviceId,
            sessionStartedUtc
        );
        return renewedTokenResult.ResultCode switch
        {
            UserAuthTokenResultCode.Success => new RenewUserAuthTokenResult(
                RenewUserAuthTokenResultCode.Success,
                renewedTokenResult.Token,
                renewedTokenResult.TokenId,
                renewedTokenResult.User,
                renewedTokenResult.CurrentAccount,
                deviceId,
                renewedTokenResult.AvailableAccounts
            ),
            UserAuthTokenResultCode.UserNotFoundError => RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.UserNotFoundError
            ),
            UserAuthTokenResultCode.SignatureKeyNotFoundError => RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.SignatureKeyNotFoundError
            ),
            UserAuthTokenResultCode.AccountNotFoundError => RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.AccountNotFoundError
            ),
            _ => RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.TokenValidationFailed
            ),
        };
    }

    public async Task<UserAuthTokenValidationResult> ValidateUserAuthTokenAsync(string authToken)
    {
        ArgumentNullException.ThrowIfNull(authToken, nameof(authToken));

        var tokenHandler = new JwtSecurityTokenHandler();

        if (!tokenHandler.CanReadToken(authToken))
        {
            _logger.LogInformation("Auth token is in invalid format");
            return UserAuthTokenValidationResult.Failed(
                UserAuthTokenValidationResultCode.InvalidTokenFormat
            );
        }

        var jwtToken = tokenHandler.ReadJwtToken(authToken);

        var subClaim = jwtToken.ClaimValue(JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var userId))
        {
            _logger.LogInformation("Auth token does not contain valid sub (userId) claim");
            return UserAuthTokenValidationResult.Failed(
                UserAuthTokenValidationResultCode.MissingUserIdClaim
            );
        }

        var userEntity = await GetUserWithKeysAndAccountsAsync(u => u.Id == userId);
        if (userEntity == null)
        {
            _logger.LogInformation("User with Id {UserId} not found", userId);
            return UserAuthTokenValidationResult.Failed(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );
        }

        var jti = jwtToken.ClaimValue(JwtRegisteredClaimNames.Jti);
        if (string.IsNullOrWhiteSpace(jti))
        {
            _logger.LogInformation("Auth token does not contain jti claim");
            return UserAuthTokenValidationResult.Failed(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );
        }

        if (!Guid.TryParse(jti, out var tokenId))
        {
            _logger.LogInformation("Auth token jti claim is not a valid GUID");
            return UserAuthTokenValidationResult.Failed(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var trackedToken = await _authTokenRepo.GetAsync(t =>
            t.Id == tokenId
            && t.UserId == userEntity.Id
            && t.RevokedUtc == null
            && t.ExpiresUtc > now
        );

        if (trackedToken == null)
        {
            _logger.LogInformation("Auth token not found, revoked, or expired in server tracking");
            return UserAuthTokenValidationResult.Failed(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );
        }

        var signatureKey = userEntity.SignatureKey();
        if (signatureKey == null)
        {
            _logger.LogWarning(
                "User with Id {UserId} does not have an asymmetric key for {KeyUse}",
                userEntity.Id,
                KeyUsedFor.Signature
            );
            return UserAuthTokenValidationResult.Failed(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );
        }

        var credentials = _securityProcessor.GetAsymmetricSigningCredentials(
            signatureKey.ProviderName,
            signatureKey.PublicKey,
            false
        );

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = credentials.Key,
            ValidateIssuer = true,
            ValidIssuer = typeof(AuthenticationProvider).FullName,
            ValidateAudience = true,
            ValidAudience = UserConstants.JWT_AUDIENCE,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            LifetimeValidator = IsWithinLifetime,
        };

        try
        {
            tokenHandler.ValidateToken(authToken, validationParameters, out _);
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Token validation failed: {Error}", ex.Message);
            return UserAuthTokenValidationResult.Failed(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );
        }

        var deviceId = jwtToken.ClaimValue(UserConstants.DEVICE_ID_CLAIM);
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            _logger.LogInformation("Auth token does not contain device ID");
            return UserAuthTokenValidationResult.Failed(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );
        }

        var accounts = userEntity.AccountModels();
        var signedInAccount = ExtractSignedInAccountFromToken(jwtToken, userEntity);

        return new UserAuthTokenValidationResult(
            UserAuthTokenValidationResultCode.Success,
            userEntity.ToModel(),
            signedInAccount,
            deviceId,
            tokenId,
            accounts
        );
    }

    public async Task<bool> RevokeUserAuthTokenAsync(RevokeUserAuthTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (!Guid.TryParse(request.TokenId, out var tokenId))
        {
            _logger.LogInformation("Auth token {TokenId} is not a valid GUID", request.TokenId);
            return false;
        }
        var trackedToken = await _authTokenRepo.GetAsync(t =>
            t.Id == tokenId
            && t.UserId == request.UserId
            && t.AccountId == request.AccountId
            && t.DeviceId == request.DeviceId
            && t.RevokedUtc == null
            && t.ExpiresUtc > now
        );

        if (trackedToken == null)
        {
            _logger.LogInformation(
                "Auth token {TokenId} not found, already revoked, or expired for user {UserId}, account {AccountId}, device {DeviceId}",
                request.TokenId,
                request.UserId,
                request.AccountId,
                request.DeviceId
            );
            return false;
        }

        trackedToken.RevokedUtc = now;
        await _authTokenRepo.UpdateAsync(trackedToken);

        _logger.LogInformation(
            "Auth token {TokenId} revoked for user {UserId}, account {AccountId}, device {DeviceId}",
            request.TokenId,
            request.UserId,
            request.AccountId,
            request.DeviceId
        );
        return true;
    }

    public async Task<List<UserSession>> ListUserSessionsAsync(Guid userId, Guid? currentSessionId)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var activeTokens = await _authTokenRepo.ListAsync(t =>
            t.UserId == userId && t.RevokedUtc == null && t.ExpiresUtc > now
        );

        return activeTokens
            .Select(t => new UserSession(
                t.Id,
                t.DeviceId,
                t.AccountId,
                t.IssuedUtc,
                t.ExpiresUtc,
                currentSessionId.HasValue && t.Id == currentSessionId.Value
            ))
            .OrderByDescending(s => s.IsCurrentSession)
            .ThenByDescending(s => s.IssuedUtc)
            .ToList();
    }

    public async Task<bool> RevokeUserAuthTokenByIdAsync(Guid userId, Guid tokenId)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var trackedToken = await _authTokenRepo.GetAsync(t =>
            t.Id == tokenId && t.UserId == userId && t.RevokedUtc == null && t.ExpiresUtc > now
        );

        if (trackedToken == null)
            return false;

        trackedToken.RevokedUtc = now;
        await _authTokenRepo.UpdateAsync(trackedToken);
        return true;
    }

    public async Task<bool> RevokeOtherUserAuthTokensAsync(Guid userId, Guid currentTokenId)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var currentTokenIsActive = await _authTokenRepo.AnyAsync(t =>
            t.Id == currentTokenId
            && t.UserId == userId
            && t.RevokedUtc == null
            && t.ExpiresUtc > now
        );

        if (!currentTokenIsActive)
            return false;

        await _authTokenRepo.ExecuteUpdateAsync(
            t =>
                t.UserId == userId
                && t.Id != currentTokenId
                && t.RevokedUtc == null
                && t.ExpiresUtc > now,
            s => s.SetProperty(t => t.RevokedUtc, now)
        );

        return true;
    }

    public async Task RevokeAllUserAuthTokensAsync(Guid userId)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var revokedCount = await _authTokenRepo.ExecuteUpdateAsync(
            t => t.UserId == userId && t.RevokedUtc == null && t.ExpiresUtc > now,
            s => s.SetProperty(t => t.RevokedUtc, now)
        );

        if (revokedCount == 0)
            return;

        _logger.LogInformation(
            "Revoked {Count} auth tokens for user {UserId}",
            revokedCount,
            userId
        );
    }

    private async Task<UserEntity?> GetUserWithKeysAndAccountsAsync(
        System.Linq.Expressions.Expression<Func<UserEntity, bool>> predicate
    ) =>
        await _userRepo.GetAsync(
            predicate,
            include: q => q.Include(u => u.AsymmetricKeys).Include(u => u.Accounts)
        );

    private static Account? FindAccountById(List<Account> accounts, Guid accountId) =>
        accounts.FirstOrDefault(a => a.Id == accountId);

    private async Task<(
        UserAuthTokenResultCode? ResultCode,
        TokenIssueContext? Context
    )> GetTokenIssueContextAsync(Guid userId, Guid? accountId)
    {
        var userEntity = await GetUserWithKeysAndAccountsAsync(u => u.Id == userId);
        if (userEntity == null)
        {
            _logger.LogWarning("User with Id {UserId} not found", userId);
            return (UserAuthTokenResultCode.UserNotFoundError, null);
        }

        var signatureKey = userEntity.SignatureKey();
        if (signatureKey == null)
        {
            _logger.LogWarning(
                "User with Id {UserId} does not have an asymmetric signature key",
                userId
            );
            return (UserAuthTokenResultCode.SignatureKeyNotFoundError, null);
        }

        var accounts = userEntity.AccountModels();
        Account? signedInAccount = null;
        if (accountId.HasValue)
        {
            signedInAccount = FindAccountById(accounts, accountId.Value);
            if (signedInAccount == null)
            {
                _logger.LogWarning(
                    "User with Id {UserId} does not have access to account {AccountId}",
                    userId,
                    accountId.Value
                );
                return (UserAuthTokenResultCode.AccountNotFoundError, null);
            }
        }

        return (null, new TokenIssueContext(userEntity, signatureKey, accounts, signedInAccount));
    }

    private async Task<UserAuthTokenResult> CreateUserAuthTokenAsync(
        TokenIssueContext tokenIssueContext,
        string deviceId,
        DateTime? sessionStartedUtc
    )
    {
        var privateKey = _securityProcessor.DecryptWithSystemKey(
            tokenIssueContext.SignatureKey.EncryptedPrivateKey
        );
        var credentials = _securityProcessor.GetAsymmetricSigningCredentials(
            tokenIssueContext.SignatureKey.ProviderName,
            privateKey,
            true
        );

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var resolvedSessionStartedUtc = sessionStartedUtc ?? now;
        var sessionExpiresUtc = resolvedSessionStartedUtc.AddSeconds(
            _securityOptions.AuthSessionTtlSeconds
        );
        var expires = now.AddSeconds(_securityOptions.AuthTokenTtlSeconds);
        if (expires > sessionExpiresUtc)
            expires = sessionExpiresUtc;

        var tokenId = Guid.CreateVersion7();
        var claims = tokenIssueContext.Claims(
            tokenId.ToString(),
            now,
            resolvedSessionStartedUtc,
            deviceId
        );

        var token = new JwtSecurityToken(
            issuer: typeof(AuthenticationProvider).FullName,
            audience: UserConstants.JWT_AUDIENCE,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        await _authTokenRepo.CreateAsync(
            new UserAuthTokenEntity
            {
                Id = tokenId,
                UserId = tokenIssueContext.UserEntity.Id,
                AccountId = tokenIssueContext.SignedInAccount?.Id,
                DeviceId = deviceId,
                IssuedUtc = now,
                ExpiresUtc = expires,
            }
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return new UserAuthTokenResult(
            UserAuthTokenResultCode.Success,
            tokenString,
            tokenId,
            tokenIssueContext.UserEntity.ToModel(),
            tokenIssueContext.SignedInAccount,
            tokenIssueContext.Accounts
        );
    }

    private bool IsWithinLifetime(
        DateTime? notBefore,
        DateTime? expires,
        SecurityToken? securityToken,
        TokenValidationParameters validationParameters
    )
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        return (notBefore == null || notBefore <= now) && expires != null && expires > now;
    }

    private bool ValidateJwtToken(
        string authToken,
        UserAsymmetricKeyEntity signatureKey,
        bool validateLifetime
    )
    {
        var credentials = _securityProcessor.GetAsymmetricSigningCredentials(
            signatureKey.ProviderName,
            signatureKey.PublicKey,
            false
        );

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = credentials.Key,
            ValidateIssuer = true,
            ValidIssuer = typeof(AuthenticationProvider).FullName,
            ValidateAudience = true,
            ValidAudience = UserConstants.JWT_AUDIENCE,
            ValidateLifetime = validateLifetime,
            ClockSkew = TimeSpan.Zero,
            LifetimeValidator = validateLifetime ? IsWithinLifetime : null,
        };

        try
        {
            new JwtSecurityTokenHandler().ValidateToken(authToken, validationParameters, out _);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Token validation failed: {Error}", ex.Message);
            return false;
        }
    }

    private Account? ExtractSignedInAccountFromToken(
        JwtSecurityToken jwtToken,
        UserEntity userEntity
    )
    {
        if (jwtToken.SignedInAccountId() is not { } accountId)
            return null;

        var matchingAccount = userEntity.Accounts?.FirstOrDefault(a => a.Id == accountId);
        if (matchingAccount != null)
        {
            return matchingAccount.ToModel();
        }

        _logger.LogWarning(
            "Account with Id {AccountId} not found in user's accounts during token validation",
            accountId
        );
        return null;
    }

    private async Task RevokeExistingTokensForUserAccountDeviceAsync(
        Guid userId,
        Guid? accountId,
        string deviceId
    )
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var revokedCount = await _authTokenRepo.ExecuteUpdateAsync(
            t =>
                t.UserId == userId
                && t.AccountId == accountId
                && t.DeviceId == deviceId
                && t.RevokedUtc == null
                && t.ExpiresUtc > now,
            s => s.SetProperty(t => t.RevokedUtc, now)
        );

        if (revokedCount == 0)
            return;

        _logger.LogDebug(
            "Revoked {Count} existing token(s) for user {UserId}, account {AccountId}, and device {DeviceId}",
            revokedCount,
            userId,
            accountId,
            deviceId
        );
    }
}
