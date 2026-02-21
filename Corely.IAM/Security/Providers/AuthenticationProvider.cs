using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Mappers;
using Corely.IAM.Accounts.Models;
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

        var userEntity = await GetUserWithKeysAndAccountsAsync(u => u.Id == request.UserId);

        if (userEntity == null)
        {
            _logger.LogWarning("User with Id {UserId} not found", request.UserId);
            return CreateFailedTokenResult(UserAuthTokenResultCode.UserNotFound);
        }

        var signatureKey = GetSignatureKey(userEntity);
        if (signatureKey == null)
        {
            _logger.LogWarning(
                "User with Id {UserId} does not have an asymmetric signature key",
                request.UserId
            );
            return CreateFailedTokenResult(UserAuthTokenResultCode.SignatureKeyNotFound);
        }

        var accounts = GetAccountModels(userEntity);

        Account? signedInAccount = null;
        if (request.AccountId.HasValue)
        {
            signedInAccount = FindAccountById(accounts, request.AccountId.Value);
            if (signedInAccount == null)
            {
                _logger.LogWarning(
                    "User with Id {UserId} does not have access to account {AccountId}",
                    request.UserId,
                    request.AccountId.Value
                );
                return CreateFailedTokenResult(UserAuthTokenResultCode.AccountNotFound);
            }
        }

        var privateKey = _securityProcessor.DecryptWithSystemKey(signatureKey.EncryptedPrivateKey);
        var credentials = _securityProcessor.GetAsymmetricSigningCredentials(
            signatureKey.ProviderTypeCode,
            privateKey,
            true
        );

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddSeconds(_securityOptions.AuthTokenTtlSeconds);
        var tokenId = Guid.CreateVersion7();

        var claims = BuildTokenClaims(
            userEntity.Id,
            tokenId.ToString(),
            now,
            request.DeviceId,
            accounts,
            request.AccountId
        );

        var token = new JwtSecurityToken(
            issuer: typeof(AuthenticationProvider).FullName,
            audience: UserConstants.JWT_AUDIENCE,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        await RevokeExistingTokensForUserAccountDeviceAsync(
            request.UserId,
            signedInAccount?.Id,
            request.DeviceId
        );

        var authTokenEntity = new UserAuthTokenEntity
        {
            Id = tokenId,
            UserId = request.UserId,
            AccountId = signedInAccount?.Id,
            DeviceId = request.DeviceId,
            IssuedUtc = now,
            ExpiresUtc = expires,
        };
        await _authTokenRepo.CreateAsync(authTokenEntity);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return new UserAuthTokenResult(
            UserAuthTokenResultCode.Success,
            tokenString,
            tokenId,
            userEntity.ToModel(),
            signedInAccount,
            accounts
        );
    }

    public async Task<UserAuthTokenValidationResult> ValidateUserAuthTokenAsync(string authToken)
    {
        ArgumentNullException.ThrowIfNull(authToken, nameof(authToken));

        var tokenHandler = new JwtSecurityTokenHandler();

        if (!tokenHandler.CanReadToken(authToken))
        {
            _logger.LogInformation("Auth token is in invalid format");
            return CreateFailedValidationResult(
                UserAuthTokenValidationResultCode.InvalidTokenFormat
            );
        }

        var jwtToken = tokenHandler.ReadJwtToken(authToken);

        var subClaim = GetClaimValue(jwtToken, JwtRegisteredClaimNames.Sub);
        if (string.IsNullOrEmpty(subClaim) || !Guid.TryParse(subClaim, out var userId))
        {
            _logger.LogInformation("Auth token does not contain valid sub (userId) claim");
            return CreateFailedValidationResult(
                UserAuthTokenValidationResultCode.MissingUserIdClaim
            );
        }

        var userEntity = await GetUserWithKeysAndAccountsAsync(u => u.Id == userId);
        if (userEntity == null)
        {
            _logger.LogInformation("User with Id {UserId} not found", userId);
            return CreateFailedValidationResult(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );
        }

        var jti = GetClaimValue(jwtToken, JwtRegisteredClaimNames.Jti);
        if (string.IsNullOrEmpty(jti))
        {
            _logger.LogInformation("Auth token does not contain jti claim");
            return CreateFailedValidationResult(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );
        }

        if (!Guid.TryParse(jti, out var tokenId))
        {
            _logger.LogInformation("Auth token jti claim is not a valid GUID");
            return CreateFailedValidationResult(
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
            return CreateFailedValidationResult(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );
        }

        var signatureKey = GetSignatureKey(userEntity);
        if (signatureKey == null)
        {
            _logger.LogWarning(
                "User with Id {UserId} does not have an asymmetric key for {KeyUse}",
                userEntity.Id,
                KeyUsedFor.Signature
            );
            return CreateFailedValidationResult(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );
        }

        var credentials = _securityProcessor.GetAsymmetricSigningCredentials(
            signatureKey.ProviderTypeCode,
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
        };

        try
        {
            tokenHandler.ValidateToken(authToken, validationParameters, out _);
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Token validation failed: {Error}", ex.Message);
            return CreateFailedValidationResult(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );
        }

        var deviceId = GetClaimValue(jwtToken, UserConstants.DEVICE_ID_CLAIM);
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            _logger.LogInformation("Auth token does not contain device ID");
            return CreateFailedValidationResult(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );
        }

        var accounts = GetAccountModels(userEntity);
        var signedInAccount = ExtractSignedInAccountFromToken(jwtToken, userEntity);

        return new UserAuthTokenValidationResult(
            UserAuthTokenValidationResultCode.Success,
            userEntity.ToModel(),
            signedInAccount,
            deviceId,
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

    public async Task RevokeAllUserAuthTokensAsync(Guid userId)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var activeTokens = await _authTokenRepo.ListAsync(t =>
            t.UserId == userId && t.RevokedUtc == null && t.ExpiresUtc > now
        );

        if (activeTokens.Count == 0)
            return;

        foreach (var token in activeTokens)
            token.RevokedUtc = now;

        foreach (var token in activeTokens)
            await _authTokenRepo.UpdateAsync(token);

        _logger.LogInformation(
            "Revoked {Count} auth tokens for user {UserId}",
            activeTokens.Count,
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

    private static UserAsymmetricKeyEntity? GetSignatureKey(UserEntity userEntity) =>
        userEntity.AsymmetricKeys?.FirstOrDefault(k => k.KeyUsedFor == KeyUsedFor.Signature);

    private static List<Account> GetAccountModels(UserEntity userEntity) =>
        userEntity.Accounts?.Select(a => a.ToModel()).ToList() ?? [];

    private static Account? FindAccountById(List<Account> accounts, Guid accountId) =>
        accounts.FirstOrDefault(a => a.Id == accountId);

    private static string? GetClaimValue(JwtSecurityToken token, string claimType) =>
        token.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;

    private static List<Claim> BuildTokenClaims(
        Guid userId,
        string jti,
        DateTime issuedAt,
        string deviceId,
        List<Account> accounts,
        Guid? signedInAccountId
    )
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
            new(UserConstants.DEVICE_ID_CLAIM, deviceId),
        };

        foreach (var account in accounts)
        {
            claims.Add(new Claim(UserConstants.ACCOUNT_ID_CLAIM, account.Id.ToString()));
        }

        if (signedInAccountId.HasValue)
        {
            claims.Add(
                new Claim(
                    UserConstants.SIGNED_IN_ACCOUNT_ID_CLAIM,
                    signedInAccountId.Value.ToString()
                )
            );
        }

        return claims;
    }

    private Account? ExtractSignedInAccountFromToken(
        JwtSecurityToken jwtToken,
        UserEntity userEntity
    )
    {
        var signedInAccountIdClaim = GetClaimValue(
            jwtToken,
            UserConstants.SIGNED_IN_ACCOUNT_ID_CLAIM
        );
        if (
            !string.IsNullOrEmpty(signedInAccountIdClaim)
            && Guid.TryParse(signedInAccountIdClaim, out var accountId)
        )
        {
            var matchingAccount = userEntity.Accounts?.FirstOrDefault(a => a.Id == accountId);
            if (matchingAccount != null)
            {
                return matchingAccount.ToModel();
            }

            _logger.LogWarning(
                "Account with Id {AccountId} not found in user's accounts during token validation",
                accountId
            );
        }

        return null;
    }

    private async Task RevokeExistingTokensForUserAccountDeviceAsync(
        Guid userId,
        Guid? accountId,
        string deviceId
    )
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var activeTokens = await _authTokenRepo.ListAsync(t =>
            t.UserId == userId
            && t.AccountId == accountId
            && t.DeviceId == deviceId
            && t.RevokedUtc == null
            && t.ExpiresUtc > now
        );

        if (activeTokens.Count == 0)
            return;

        foreach (var token in activeTokens)
        {
            token.RevokedUtc = now;
            await _authTokenRepo.UpdateAsync(token);
        }

        _logger.LogDebug(
            "Revoked {Count} existing token(s) for user {UserId}, account {AccountId}, and device {DeviceId}",
            activeTokens.Count,
            userId,
            accountId,
            deviceId
        );
    }

    private static UserAuthTokenResult CreateFailedTokenResult(
        UserAuthTokenResultCode resultCode
    ) => new(resultCode, null, null, null, null, []);

    private static UserAuthTokenValidationResult CreateFailedValidationResult(
        UserAuthTokenValidationResultCode resultCode
    ) => new(resultCode, null, null, null, []);
}
