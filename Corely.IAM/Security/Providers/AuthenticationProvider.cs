using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Mappers;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Constants;
using Corely.IAM.Users.Entities;
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
    ILogger<AuthenticationProvider> logger
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

    public async Task<UserAuthTokenResult> GetUserAuthTokenAsync(UserAuthTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var userEntity = await _userRepo.GetAsync(
            u => u.Id == request.UserId,
            include: q => q.Include(u => u.AsymmetricKeys).Include(u => u.Accounts)
        );

        if (userEntity == null)
        {
            _logger.LogWarning("User with Id {UserId} not found", request.UserId);
            return new UserAuthTokenResult(UserAuthTokenResultCode.UserNotFound, null, [], null);
        }

        var signatureKey = userEntity.AsymmetricKeys?.FirstOrDefault(k =>
            k.KeyUsedFor == KeyUsedFor.Signature
        );
        if (signatureKey == null)
        {
            _logger.LogWarning(
                "User with Id {UserId} does not have an asymmetric signature key",
                request.UserId
            );
            return new UserAuthTokenResult(
                UserAuthTokenResultCode.SignatureKeyNotFound,
                null,
                [],
                null
            );
        }

        var accounts = userEntity.Accounts?.Select(a => a.ToModel()).ToList() ?? [];

        int? signedInAccountId = null;
        if (request.AccountId.HasValue)
        {
            if (accounts.Any(a => a.Id == request.AccountId.Value))
                signedInAccountId = request.AccountId.Value;
            else
            {
                _logger.LogWarning(
                    "User with Id {UserId} does not have access to account {AccountId}",
                    request.UserId,
                    request.AccountId.Value
                );
                return new UserAuthTokenResult(
                    UserAuthTokenResultCode.AccountNotFound,
                    null,
                    accounts,
                    null
                );
            }
        }

        var privateKey = _securityProcessor.DecryptWithSystemKey(signatureKey.EncryptedPrivateKey);
        var credentials = _securityProcessor.GetAsymmetricSigningCredentials(
            signatureKey.ProviderTypeCode,
            privateKey,
            true
        );

        var now = DateTime.UtcNow;
        var expires = now.AddSeconds(_securityOptions.AuthTokenTtlSeconds);
        var jti = Guid.NewGuid().ToString();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.UserId.ToString()),
            new(JwtRegisteredClaimNames.Jti, jti),
            new(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
        };

        foreach (var account in accounts)
        {
            claims.Add(new Claim(UserConstants.ACCOUNT_ID_CLAIM, account.Id.ToString()));
        }

        if (signedInAccountId.HasValue)
            claims.Add(
                new Claim(
                    UserConstants.SIGNED_IN_ACCOUNT_ID_CLAIM,
                    signedInAccountId.Value.ToString()
                )
            );

        var token = new JwtSecurityToken(
            issuer: typeof(AuthenticationProvider).FullName,
            audience: UserConstants.JWT_AUDIENCE,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        var authTokenEntity = new UserAuthTokenEntity
        {
            UserId = request.UserId,
            Jti = jti,
            IssuedUtc = now,
            ExpiresUtc = expires,
        };
        await _authTokenRepo.CreateAsync(authTokenEntity);

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
        return new UserAuthTokenResult(
            UserAuthTokenResultCode.Success,
            tokenString,
            accounts,
            signedInAccountId
        );
    }

    public async Task<UserAuthTokenValidationResult> ValidateUserAuthTokenAsync(string authToken)
    {
        ArgumentNullException.ThrowIfNull(authToken, nameof(authToken));

        var tokenHandler = new JwtSecurityTokenHandler();

        if (!tokenHandler.CanReadToken(authToken))
        {
            _logger.LogInformation("Auth token is in invalid format");
            return new UserAuthTokenValidationResult(
                UserAuthTokenValidationResultCode.InvalidTokenFormat,
                null,
                null
            );
        }

        var jwtToken = tokenHandler.ReadJwtToken(authToken);

        var subClaim = jwtToken
            .Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)
            ?.Value;
        if (string.IsNullOrEmpty(subClaim) || !int.TryParse(subClaim, out var userId))
        {
            _logger.LogInformation("Auth token does not contain valid sub (userId) claim");
            return new UserAuthTokenValidationResult(
                UserAuthTokenValidationResultCode.MissingUserIdClaim,
                null,
                null
            );
        }

        var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrEmpty(jti))
        {
            _logger.LogInformation("Auth token does not contain jti claim");
            return new UserAuthTokenValidationResult(
                UserAuthTokenValidationResultCode.TokenValidationFailed,
                null,
                null
            );
        }

        var trackedToken = await _authTokenRepo.GetAsync(t =>
            t.Jti == jti
            && t.UserId == userId
            && t.RevokedUtc == null
            && t.ExpiresUtc > DateTime.UtcNow
        );

        if (trackedToken == null)
        {
            _logger.LogInformation("Auth token not found, revoked, or expired in server tracking");
            return new UserAuthTokenValidationResult(
                UserAuthTokenValidationResultCode.TokenValidationFailed,
                null,
                null
            );
        }

        var signatureKey = await GetUserAsymmetricKeyAsync(userId, KeyUsedFor.Signature);
        if (signatureKey == null)
        {
            return new UserAuthTokenValidationResult(
                UserAuthTokenValidationResultCode.TokenValidationFailed,
                null,
                null
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
            return new UserAuthTokenValidationResult(
                UserAuthTokenValidationResultCode.TokenValidationFailed,
                null,
                null
            );
        }

        int? signedInAccountId = null;
        var signedInAccountIdClaim = jwtToken
            .Claims.FirstOrDefault(c => c.Type == UserConstants.SIGNED_IN_ACCOUNT_ID_CLAIM)
            ?.Value;
        if (
            !string.IsNullOrEmpty(signedInAccountIdClaim)
            && int.TryParse(signedInAccountIdClaim, out var accountId)
        )
        {
            signedInAccountId = accountId;
        }

        return new UserAuthTokenValidationResult(
            UserAuthTokenValidationResultCode.Success,
            userId,
            signedInAccountId
        );
    }

    public async Task<bool> RevokeUserAuthTokenAsync(int userId, string tokenId)
    {
        var trackedToken = await _authTokenRepo.GetAsync(t =>
            t.Jti == tokenId
            && t.UserId == userId
            && t.RevokedUtc == null
            && t.ExpiresUtc > DateTime.UtcNow
        );

        if (trackedToken == null)
        {
            _logger.LogInformation(
                "Auth token {TokenId} not found, already revoked, or expired",
                tokenId
            );
            return false;
        }

        trackedToken.RevokedUtc = DateTime.UtcNow;
        await _authTokenRepo.UpdateAsync(trackedToken);

        _logger.LogInformation("Auth token {TokenId} revoked for user {UserId}", tokenId, userId);
        return true;
    }

    public async Task RevokeAllUserAuthTokensAsync(int userId)
    {
        var activeTokens = await _authTokenRepo.ListAsync(t =>
            t.UserId == userId && t.RevokedUtc == null && t.ExpiresUtc > DateTime.UtcNow
        );

        var now = DateTime.UtcNow;
        foreach (var token in activeTokens)
        {
            token.RevokedUtc = now;
            await _authTokenRepo.UpdateAsync(token);
        }

        if (activeTokens.Count > 0)
        {
            _logger.LogInformation(
                "Revoked {Count} auth tokens for user {UserId}",
                activeTokens.Count,
                userId
            );
        }
    }

    private async Task<UserAsymmetricKeyEntity?> GetUserAsymmetricKeyAsync(
        int userId,
        KeyUsedFor keyUse
    )
    {
        var userEntity = await _userRepo.GetAsync(
            u => u.Id == userId,
            include: q => q.Include(u => u.AsymmetricKeys)
        );

        if (userEntity == null)
        {
            _logger.LogWarning("User with Id {UserId} not found", userId);
            return null;
        }

        var key = userEntity.AsymmetricKeys?.FirstOrDefault(k => k.KeyUsedFor == keyUse);
        if (key == null)
        {
            _logger.LogWarning(
                "User with Id {UserId} does not have an asymmetric key for {KeyUse}",
                userId,
                keyUse
            );
            return null;
        }

        return key;
    }
}
