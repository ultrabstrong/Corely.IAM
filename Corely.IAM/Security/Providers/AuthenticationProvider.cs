using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Mappers;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Extensions;
using Corely.IAM.Platform.Models;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.TotpAuths.Entities;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Mappers;
using Corely.IAM.Users.Models;
using Corely.Security.Hashing.Factories;
using Corely.Security.Secrets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Corely.IAM.Security.Providers;

internal class AuthenticationProvider(
    IRepo<UserEntity> userRepo,
    IRepo<UserAuthTokenEntity> authTokenRepo,
    IReadonlyRepo<AccountEntity> accountRepo,
    IReadonlyRepo<TotpAuthEntity> totpAuthRepo,
    IPlatformAccessProvider platformAccessProvider,
    ISecurityProvider securityProcessor,
    ISecurityPolicyProvider securityPolicyProvider,
    IHashProviderFactory hashProviderFactory,
    IamHashCodes hashCodes,
    ISecretProvider secretProvider,
    ILogger<AuthenticationProvider> logger,
    TimeProvider timeProvider
) : IAuthenticationProvider
{
    private readonly IHashProviderFactory _hashProviderFactory = hashProviderFactory.ThrowIfNull(
        nameof(hashProviderFactory)
    );
    private readonly IamHashCodes _hashCodes = hashCodes.ThrowIfNull(nameof(hashCodes));
    private readonly ISecretProvider _secretProvider = secretProvider.ThrowIfNull(
        nameof(secretProvider)
    );
    private readonly IRepo<UserEntity> _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
    private readonly IRepo<UserAuthTokenEntity> _authTokenRepo = authTokenRepo.ThrowIfNull(
        nameof(authTokenRepo)
    );
    private readonly IReadonlyRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly IReadonlyRepo<TotpAuthEntity> _totpAuthRepo = totpAuthRepo.ThrowIfNull(
        nameof(totpAuthRepo)
    );
    private readonly IPlatformAccessProvider _platformAccessProvider =
        platformAccessProvider.ThrowIfNull(nameof(platformAccessProvider));
    private readonly ISecurityProvider _securityProcessor = securityProcessor.ThrowIfNull(
        nameof(securityProcessor)
    );
    private readonly ISecurityPolicyProvider _securityPolicyProvider =
        securityPolicyProvider.ThrowIfNull(nameof(securityPolicyProvider));
    private readonly ILogger<AuthenticationProvider> _logger = logger.ThrowIfNull(nameof(logger));
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));

    public async Task<UserAuthTokenResult> GetUserAuthTokenAsync(GetUserAuthTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var tokenIssueContext = await GetTokenIssueContextAsync(request.UserId, request.AccountId);
        if (tokenIssueContext.ResultCode.HasValue)
            return UserAuthTokenResult.Failed(tokenIssueContext.ResultCode.Value);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var tokenId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        var sessionStartedUtc = now;

        if (request.ContinuedTokenId is { } continuedTokenId)
        {
            var continuedToken = await _authTokenRepo.GetAsync(t =>
                t.Id == continuedTokenId
                && t.UserId == request.UserId
                && t.RevokedUtc == null
                && t.RefreshExpiresUtc > now
            );
            var claimed =
                continuedToken == null
                    ? 0
                    : await _authTokenRepo.ExecuteUpdateAsync(
                        t =>
                            t.Id == continuedTokenId
                            && t.RevokedUtc == null
                            && t.ReplacedByTokenId == null,
                        s =>
                            s.SetProperty(t => t.RevokedUtc, now)
                                .SetProperty(t => t.ReplacedByTokenId, tokenId)
                    );
            if (claimed == 0)
            {
                _logger.LogInformation(
                    "Session of token {TokenId} is no longer active and cannot be continued",
                    continuedTokenId
                );
                return UserAuthTokenResult.Failed(UserAuthTokenResultCode.SessionNotFoundError);
            }

            sessionId = continuedToken!.SessionId;
            sessionStartedUtc = continuedToken.SessionStartedUtc;
        }

        await RevokeExistingTokensForUserAccountDeviceAsync(
            request.UserId,
            tokenIssueContext.Context!.SignedInAccount?.Id,
            request.DeviceId
        );

        return await CreateUserAuthTokenAsync(
            tokenIssueContext.Context,
            request.DeviceId,
            tokenId,
            sessionId,
            sessionStartedUtc
        );
    }

    public async Task<RenewUserAuthTokenResult> RenewUserAuthTokenAsync(string refreshToken)
    {
        ArgumentNullException.ThrowIfNull(refreshToken, nameof(refreshToken));

        if (!RefreshToken.TryParse(refreshToken, out var parsedRefreshToken))
        {
            _logger.LogInformation("Refresh token is in an invalid format");
            return RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.TokenValidationFailed
            );
        }

        var trackedToken = await _authTokenRepo.GetAsync(t => t.Id == parsedRefreshToken.TokenId);
        if (
            trackedToken == null
            || !trackedToken
                .RefreshSecretHash.ToHashedValue(_hashProviderFactory)
                .Verify(parsedRefreshToken.Secret)
        )
        {
            _logger.LogInformation("Refresh token not found in server tracking");
            return RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.TokenValidationFailed
            );
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var policy = await _securityPolicyProvider.GetPolicyAsync();

        if (trackedToken.ReplacedByTokenId.HasValue)
            return await RefusedReplacedRefreshTokenAsync(trackedToken, now, policy);

        if (trackedToken.RevokedUtc != null)
        {
            _logger.LogInformation("Refresh token {TokenId} was revoked", trackedToken.Id);
            return RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.TokenValidationFailed
            );
        }

        if (
            trackedToken.RefreshExpiresUtc <= now
            || trackedToken.SessionStartedUtc.AddSeconds(policy.AuthSessionTtlSeconds) <= now
        )
        {
            trackedToken.RevokedUtc = now;
            await _authTokenRepo.UpdateAsync(trackedToken);
            return RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.SessionExpiredError
            );
        }

        var userId = trackedToken.UserId;
        var deviceId = trackedToken.DeviceId;
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
                    UserAuthTokenResultCode.TwoFactorRequiredError =>
                        RenewUserAuthTokenResultCode.TwoFactorRequiredError,
                    _ => RenewUserAuthTokenResultCode.TokenValidationFailed,
                }
            );
        }

        var renewedTokenId = Guid.CreateVersion7();
        var claimed = await _authTokenRepo.ExecuteUpdateAsync(
            t => t.Id == trackedToken.Id && t.RevokedUtc == null && t.ReplacedByTokenId == null,
            s =>
                s.SetProperty(t => t.RevokedUtc, now)
                    .SetProperty(t => t.ReplacedByTokenId, renewedTokenId)
        );
        if (claimed == 0)
        {
            _logger.LogInformation(
                "Refresh token {TokenId} was replaced by a concurrent renewal",
                trackedToken.Id
            );
            return RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.RefreshTokenAlreadyRotatedError
            );
        }

        var renewedTokenResult = await CreateUserAuthTokenAsync(
            tokenIssueContext.Context!,
            deviceId,
            renewedTokenId,
            trackedToken.SessionId,
            trackedToken.SessionStartedUtc
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
                renewedTokenResult.AvailableAccounts,
                renewedTokenResult.EnteredAsPlatformMember,
                renewedTokenResult.RefreshToken,
                renewedTokenResult.RefreshExpiresUtc
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

        var signingKeys = SignatureVerificationKeys(userEntity);
        if (signingKeys.Count == 0)
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

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,
            ValidateIssuer = true,
            ValidIssuer = typeof(AuthenticationProvider).FullName,
            ValidateAudience = true,
            ValidAudience = TokenIssueContext.JWT_AUDIENCE,
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

        var deviceId = jwtToken.ClaimValue(TokenIssueContext.DEVICE_ID_CLAIM);
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            _logger.LogInformation("Auth token does not contain device ID");
            return UserAuthTokenValidationResult.Failed(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );
        }

        var signedInAccount = await ExtractSignedInAccountFromTokenAsync(jwtToken, userEntity);
        var accounts = WithEnteredAccount(userEntity.AccountModels(), signedInAccount);

        return new UserAuthTokenValidationResult(
            UserAuthTokenValidationResultCode.Success,
            userEntity.ToModel(),
            signedInAccount,
            deviceId,
            tokenId,
            accounts,
            IsEnteredAsPlatformMember(userEntity, signedInAccount)
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
            && t.RefreshExpiresUtc > now
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
            t.UserId == userId && t.RevokedUtc == null && t.RefreshExpiresUtc > now
        );

        return activeTokens
            .Select(t => new UserSession(
                t.Id,
                t.DeviceId,
                t.AccountId,
                t.IssuedUtc,
                t.RefreshExpiresUtc,
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
            t.Id == tokenId
            && t.UserId == userId
            && t.RevokedUtc == null
            && t.RefreshExpiresUtc > now
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
            && t.RefreshExpiresUtc > now
        );

        if (!currentTokenIsActive)
            return false;

        await _authTokenRepo.ExecuteUpdateAsync(
            t =>
                t.UserId == userId
                && t.Id != currentTokenId
                && t.RevokedUtc == null
                && t.RefreshExpiresUtc > now,
            s => s.SetProperty(t => t.RevokedUtc, now)
        );

        return true;
    }

    public async Task RevokeAllUserAuthTokensAsync(Guid userId, Guid? exceptTokenId = null)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var revokedCount = await _authTokenRepo.ExecuteUpdateAsync(
            t =>
                t.UserId == userId
                && t.Id != exceptTokenId
                && t.RevokedUtc == null
                && t.RefreshExpiresUtc > now,
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

    public async Task<int> DeleteExpiredAuthTokensAsync()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var deletedCount = await _authTokenRepo.ExecuteDeleteAsync(t => t.RefreshExpiresUtc <= now);

        _logger.LogInformation("Deleted {Count} expired auth tokens", deletedCount);
        return deletedCount;
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
            var membership = FindAccountById(accounts, accountId.Value);
            signedInAccount =
                membership ?? await FindPlatformEnterableAccountAsync(userId, accountId.Value);
            if (signedInAccount == null)
            {
                _logger.LogWarning(
                    "User with Id {UserId} does not have access to account {AccountId}",
                    userId,
                    accountId.Value
                );
                return (UserAuthTokenResultCode.AccountNotFoundError, null);
            }

            if (
                (membership is null || signedInAccount.IsPlatformAccount)
                && !await _totpAuthRepo.AnyAsync(t => t.UserId == userId && t.IsEnabled)
            )
            {
                _logger.LogWarning(
                    "User with Id {UserId} needs two factor sign in to enter account {AccountId} as a platform member",
                    userId,
                    accountId.Value
                );
                return (UserAuthTokenResultCode.TwoFactorRequiredError, null);
            }
        }

        return (
            null,
            new TokenIssueContext(
                userEntity,
                signatureKey,
                WithEnteredAccount(accounts, signedInAccount),
                signedInAccount
            )
        );
    }

    private async Task<RenewUserAuthTokenResult> RefusedReplacedRefreshTokenAsync(
        UserAuthTokenEntity trackedToken,
        DateTime now,
        SecurityPolicy policy
    )
    {
        if (trackedToken.RevokedUtc?.AddSeconds(policy.RefreshReuseGraceSeconds) > now)
        {
            _logger.LogInformation(
                "Refresh token {TokenId} was replaced moments ago; refusing without ending its session",
                trackedToken.Id
            );
            return RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.RefreshTokenAlreadyRotatedError
            );
        }

        var revokedCount = await _authTokenRepo.ExecuteUpdateAsync(
            t => t.SessionId == trackedToken.SessionId && t.RevokedUtc == null,
            s => s.SetProperty(t => t.RevokedUtc, now)
        );
        _logger.LogWarning(
            "Replaced refresh token {TokenId} was reused; ended session {SessionId} for user {UserId}, revoking {Count} token(s)",
            trackedToken.Id,
            trackedToken.SessionId,
            trackedToken.UserId,
            revokedCount
        );
        return RenewUserAuthTokenResult.Failed(
            RenewUserAuthTokenResultCode.RefreshTokenReusedError
        );
    }

    private async Task<UserAuthTokenResult> CreateUserAuthTokenAsync(
        TokenIssueContext tokenIssueContext,
        string deviceId,
        Guid tokenId,
        Guid sessionId,
        DateTime sessionStartedUtc
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

        var policy = await _securityPolicyProvider.GetPolicyAsync();
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        sessionStartedUtc = DateTime.SpecifyKind(sessionStartedUtc, DateTimeKind.Utc);
        var refreshExpiresUtc = policy.RefreshExpiresUtc(sessionStartedUtc, now);
        var expires = policy.AccessTokenExpiresUtc(refreshExpiresUtc, now);

        var claims = tokenIssueContext.Claims(tokenId.ToString(), now, deviceId);

        var token = new JwtSecurityToken(
            issuer: typeof(AuthenticationProvider).FullName,
            audience: TokenIssueContext.JWT_AUDIENCE,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        var refreshSecret = _secretProvider.CreateSecret();
        await _authTokenRepo.CreateAsync(
            new UserAuthTokenEntity
            {
                Id = tokenId,
                UserId = tokenIssueContext.UserEntity.Id,
                AccountId = tokenIssueContext.SignedInAccount?.Id,
                DeviceId = deviceId,
                SessionId = sessionId,
                SessionStartedUtc = sessionStartedUtc,
                IssuedUtc = now,
                ExpiresUtc = expires,
                RefreshSecretHash = refreshSecret
                    .ToHashedValueFromPlainText(_hashProviderFactory, _hashCodes.TokenHashCode)
                    .ToHashString()!,
                RefreshExpiresUtc = refreshExpiresUtc,
            }
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return new UserAuthTokenResult(
            UserAuthTokenResultCode.Success,
            tokenString,
            tokenId,
            tokenIssueContext.UserEntity.ToModel(),
            tokenIssueContext.SignedInAccount,
            tokenIssueContext.Accounts,
            IsEnteredAsPlatformMember(
                tokenIssueContext.UserEntity,
                tokenIssueContext.SignedInAccount
            ),
            new RefreshToken(tokenId, refreshSecret).ToString(),
            refreshExpiresUtc
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

    private List<SecurityKey> SignatureVerificationKeys(UserEntity userEntity) =>
        [
            .. userEntity
                .AsymmetricKeys.Versions(KeyUsedFor.Signature)
                .Select(k =>
                    _securityProcessor
                        .GetAsymmetricSigningCredentials(k.ProviderName, k.PublicKey, false)
                        .Key
                ),
        ];

    private bool ValidateJwtToken(string authToken, UserEntity userEntity, bool validateLifetime)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = SignatureVerificationKeys(userEntity),
            ValidateIssuer = true,
            ValidIssuer = typeof(AuthenticationProvider).FullName,
            ValidateAudience = true,
            ValidAudience = TokenIssueContext.JWT_AUDIENCE,
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

    private static List<Account> WithEnteredAccount(
        List<Account> accounts,
        Account? signedInAccount
    ) =>
        signedInAccount is null || accounts.Any(a => a.Id == signedInAccount.Id)
            ? accounts
            : [.. accounts, signedInAccount];

    private static bool IsEnteredAsPlatformMember(
        UserEntity userEntity,
        Account? signedInAccount
    ) =>
        signedInAccount is not null
        && userEntity.Accounts?.Any(a => a.Id == signedInAccount.Id) != true;

    private async Task<Account?> FindPlatformEnterableAccountAsync(Guid userId, Guid accountId) =>
        await _platformAccessProvider.CanEnterAnyAccountAsync(userId)
            ? (await _accountRepo.GetAsync(a => a.Id == accountId))?.ToModel()
            : null;

    private async Task<Account?> ExtractSignedInAccountFromTokenAsync(
        JwtSecurityToken jwtToken,
        UserEntity userEntity
    )
    {
        if (jwtToken.SignedInAccountId() is not { } accountId)
            return null;

        var matchingAccount =
            userEntity.Accounts?.FirstOrDefault(a => a.Id == accountId)?.ToModel()
            ?? await FindPlatformEnterableAccountAsync(userEntity.Id, accountId);
        if (matchingAccount != null)
        {
            return matchingAccount;
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
                && t.RefreshExpiresUtc > now,
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
