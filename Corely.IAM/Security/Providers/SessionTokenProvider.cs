using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Extensions;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Security.Entities;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Extensions;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Models;
using Corely.Security.Hashing.Factories;
using Corely.Security.Secrets;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Corely.IAM.Security.Providers;

internal class SessionTokenProvider(
    IRepo<AuthTokenEntity> authTokenRepo,
    ISecurityProvider securityProvider,
    ISecurityPolicyProvider securityPolicyProvider,
    IHashProviderFactory hashProviderFactory,
    IamHashCodes hashCodes,
    ISecretProvider secretProvider,
    ILogger<SessionTokenProvider> logger,
    TimeProvider timeProvider
) : ISessionTokenProvider
{
    public const string ISSUER = "Corely.IAM.Security.Providers.AuthenticationProvider";
    public const string AUDIENCE = "Corely.IAM";
    public const string ACCOUNT_ID_CLAIM = "account_id";
    public const string SIGNED_IN_ACCOUNT_ID_CLAIM = "signed_in_account_id";
    public const string DEVICE_ID_CLAIM = "device_id";
    public const string PRINCIPAL_TYPE_CLAIM = "principal_type";
    public const string USER_PRINCIPAL_TYPE = "user";
    public const string API_KEY_PRINCIPAL_TYPE = "api_key";

    private readonly IRepo<AuthTokenEntity> _authTokenRepo = authTokenRepo.ThrowIfNull(
        nameof(authTokenRepo)
    );
    private readonly ISecurityProvider _securityProvider = securityProvider.ThrowIfNull(
        nameof(securityProvider)
    );
    private readonly ISecurityPolicyProvider _securityPolicyProvider =
        securityPolicyProvider.ThrowIfNull(nameof(securityPolicyProvider));
    private readonly IHashProviderFactory _hashProviderFactory = hashProviderFactory.ThrowIfNull(
        nameof(hashProviderFactory)
    );
    private readonly IamHashCodes _hashCodes = hashCodes.ThrowIfNull(nameof(hashCodes));
    private readonly ISecretProvider _secretProvider = secretProvider.ThrowIfNull(
        nameof(secretProvider)
    );
    private readonly ILogger<SessionTokenProvider> _logger = logger.ThrowIfNull(nameof(logger));
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    public async Task<IssuedSessionToken> IssueAsync(
        SessionTokenSubject subject,
        Guid tokenId,
        Guid sessionId,
        DateTime sessionStartedUtc
    )
    {
        var credentials = _securityProvider.GetAsymmetricSigningCredentials(
            subject.SignatureKey.ProviderName,
            _securityProvider.DecryptWithSystemKey(subject.SignatureKey.EncryptedPrivateKey),
            true
        );

        var policy = await _securityPolicyProvider.GetPolicyAsync();
        var now = UtcNow;
        sessionStartedUtc = DateTime.SpecifyKind(sessionStartedUtc, DateTimeKind.Utc);
        var refreshExpiresUtc = policy.RefreshExpiresUtc(sessionStartedUtc, now);
        var expiresUtc = policy.AccessTokenExpiresUtc(refreshExpiresUtc, now);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, subject.SubjectId.ToString()),
            new(JwtRegisteredClaimNames.Jti, tokenId.ToString()),
            new(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64
            ),
            new(DEVICE_ID_CLAIM, subject.DeviceId),
            new(
                PRINCIPAL_TYPE_CLAIM,
                subject.PrincipalType == TokenPrincipalType.ApiKey
                    ? API_KEY_PRINCIPAL_TYPE
                    : USER_PRINCIPAL_TYPE
            ),
            .. subject.AccountIds.Select(id => new Claim(ACCOUNT_ID_CLAIM, id.ToString())),
        ];
        if (subject.SignedInAccountId is { } signedInAccountId)
            claims.Add(new Claim(SIGNED_IN_ACCOUNT_ID_CLAIM, signedInAccountId.ToString()));

        var token = new JwtSecurityToken(
            issuer: ISSUER,
            audience: AUDIENCE,
            claims: claims,
            expires: expiresUtc,
            signingCredentials: credentials
        );

        var refreshSecret = _secretProvider.CreateSecret();
        await _authTokenRepo.CreateAsync(
            new AuthTokenEntity
            {
                Id = tokenId,
                UserId =
                    subject.PrincipalType == TokenPrincipalType.User ? subject.SubjectId : null,
                ApiKeyId =
                    subject.PrincipalType == TokenPrincipalType.ApiKey ? subject.SubjectId : null,
                Slot = subject.Slot,
                AccountId = subject.SignedInAccountId,
                DeviceId = subject.DeviceId,
                SessionId = sessionId,
                SessionStartedUtc = sessionStartedUtc,
                IssuedUtc = now,
                ExpiresUtc = expiresUtc,
                RefreshSecretHash = refreshSecret
                    .ToHashedValueFromPlainText(_hashProviderFactory, _hashCodes.TokenHashCode)
                    .ToHashString()!,
                RefreshExpiresUtc = refreshExpiresUtc,
            }
        );

        return new IssuedSessionToken(
            new JwtSecurityTokenHandler().WriteToken(token),
            tokenId,
            new RefreshToken(tokenId, refreshSecret).ToString(),
            refreshExpiresUtc
        );
    }

    public SessionTokenClaims? ReadClaims(
        string authToken,
        out UserAuthTokenValidationResultCode failure
    )
    {
        var handler = new JwtSecurityTokenHandler();
        if (string.IsNullOrWhiteSpace(authToken) || !handler.CanReadToken(authToken))
        {
            failure = UserAuthTokenValidationResultCode.InvalidTokenFormat;
            return null;
        }

        var jwt = handler.ReadJwtToken(authToken);
        if (!Guid.TryParse(jwt.ClaimValue(JwtRegisteredClaimNames.Sub), out var subjectId))
        {
            failure = UserAuthTokenValidationResultCode.MissingUserIdClaim;
            return null;
        }

        var deviceId = jwt.ClaimValue(DEVICE_ID_CLAIM);
        if (
            !Guid.TryParse(jwt.ClaimValue(JwtRegisteredClaimNames.Jti), out var tokenId)
            || string.IsNullOrWhiteSpace(deviceId)
        )
        {
            failure = UserAuthTokenValidationResultCode.TokenValidationFailed;
            return null;
        }

        failure = UserAuthTokenValidationResultCode.Success;
        return new SessionTokenClaims(
            jwt.ClaimValue(PRINCIPAL_TYPE_CLAIM) == API_KEY_PRINCIPAL_TYPE
                ? TokenPrincipalType.ApiKey
                : TokenPrincipalType.User,
            subjectId,
            tokenId,
            deviceId,
            jwt.SignedInAccountId()
        );
    }

    public async Task<bool> IsActiveAsync(SessionTokenClaims claims)
    {
        var now = UtcNow;
        return claims.PrincipalType == TokenPrincipalType.ApiKey
            ? await _authTokenRepo.AnyAsync(t =>
                t.Id == claims.TokenId
                && t.ApiKeyId == claims.SubjectId
                && t.RevokedUtc == null
                && t.ExpiresUtc > now
            )
            : await _authTokenRepo.AnyAsync(t =>
                t.Id == claims.TokenId
                && t.UserId == claims.SubjectId
                && t.RevokedUtc == null
                && t.ExpiresUtc > now
            );
    }

    public bool IsSignedBy(string authToken, IEnumerable<AsymmetricKeyEntity> signatureKeys)
    {
        var keys = signatureKeys
            .Select(k =>
                _securityProvider
                    .GetAsymmetricSigningCredentials(k.ProviderName, k.PublicKey, false)
                    .Key
            )
            .ToList();
        if (keys.Count == 0)
        {
            _logger.LogWarning("No signature key to validate the auth token against");
            return false;
        }

        var parameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = keys,
            ValidateIssuer = true,
            ValidIssuer = ISSUER,
            ValidateAudience = true,
            ValidAudience = AUDIENCE,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            LifetimeValidator = (notBefore, expires, _, _) =>
                (notBefore == null || notBefore <= UtcNow) && expires > UtcNow,
        };

        try
        {
            new JwtSecurityTokenHandler().ValidateToken(authToken, parameters, out _);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Auth token validation failed: {Error}", ex.Message);
            return false;
        }
    }

    public async Task<SessionTokenRenewal> BeginRenewalAsync(string refreshToken)
    {
        if (!RefreshToken.TryParse(refreshToken, out var parsed))
        {
            _logger.LogInformation("Refresh token is in an invalid format");
            return new SessionTokenRenewal(SessionTokenRenewalCode.InvalidToken, null);
        }

        var token = await _authTokenRepo.GetAsync(t => t.Id == parsed.TokenId);
        if (token == null || !token.HasRefreshSecret(parsed.Secret, _hashProviderFactory))
        {
            _logger.LogInformation("Refresh token not found in server tracking");
            return new SessionTokenRenewal(SessionTokenRenewalCode.InvalidToken, null);
        }

        var now = UtcNow;
        switch (token.RenewalStateAt(now, await _securityPolicyProvider.GetPolicyAsync()))
        {
            case AuthTokenRenewalState.RecentlyReplaced:
                _logger.LogInformation(
                    "Refresh token {TokenId} was replaced moments ago; refusing without ending its session",
                    token.Id
                );
                return new SessionTokenRenewal(SessionTokenRenewalCode.AlreadyRotated, token);
            case AuthTokenRenewalState.Reused:
                var revokedCount = await _authTokenRepo.ExecuteUpdateAsync(
                    t => t.SessionId == token.SessionId && t.RevokedUtc == null,
                    s => s.SetProperty(t => t.RevokedUtc, now)
                );
                _logger.LogWarning(
                    "Replaced refresh token {TokenId} was reused; ended session {SessionId}, revoking {Count} token(s)",
                    token.Id,
                    token.SessionId,
                    revokedCount
                );
                return new SessionTokenRenewal(SessionTokenRenewalCode.Reused, token);
            case AuthTokenRenewalState.Revoked:
                _logger.LogInformation("Refresh token {TokenId} was revoked", token.Id);
                return new SessionTokenRenewal(SessionTokenRenewalCode.InvalidToken, token);
            case AuthTokenRenewalState.Expired:
                await EndAsync(token);
                return new SessionTokenRenewal(SessionTokenRenewalCode.SessionExpired, token);
            default:
                return new SessionTokenRenewal(SessionTokenRenewalCode.Ready, token);
        }
    }

    public async Task EndAsync(AuthTokenEntity token)
    {
        token.RevokedUtc = UtcNow;
        await _authTokenRepo.UpdateAsync(token);
    }

    public async Task<Guid?> ClaimRenewalAsync(AuthTokenEntity token)
    {
        var now = UtcNow;
        var renewedTokenId = Guid.CreateVersion7();
        var claimed = await _authTokenRepo.ExecuteUpdateAsync(
            t => t.Id == token.Id && t.RevokedUtc == null && t.ReplacedByTokenId == null,
            s =>
                s.SetProperty(t => t.RevokedUtc, now)
                    .SetProperty(t => t.ReplacedByTokenId, renewedTokenId)
        );
        if (claimed > 0)
            return renewedTokenId;

        _logger.LogInformation(
            "Refresh token {TokenId} was replaced by a concurrent renewal",
            token.Id
        );
        return null;
    }

    public async Task<AuthTokenEntity?> ClaimContinuationAsync(
        Guid tokenId,
        Guid userId,
        Guid continuingTokenId
    )
    {
        var now = UtcNow;
        var token = await _authTokenRepo.GetAsync(t =>
            t.Id == tokenId
            && t.UserId == userId
            && t.RevokedUtc == null
            && t.RefreshExpiresUtc > now
        );
        if (token == null)
            return null;

        var claimed = await _authTokenRepo.ExecuteUpdateAsync(
            t => t.Id == tokenId && t.RevokedUtc == null && t.ReplacedByTokenId == null,
            s =>
                s.SetProperty(t => t.RevokedUtc, now)
                    .SetProperty(t => t.ReplacedByTokenId, continuingTokenId)
        );
        return claimed > 0 ? token : null;
    }

    public async Task<int> RevokeActiveAsync(Expression<Func<AuthTokenEntity, bool>> which)
    {
        var now = UtcNow;
        return await _authTokenRepo.ExecuteUpdateAsync(
            ActiveAnd(which, now),
            s => s.SetProperty(t => t.RevokedUtc, now)
        );
    }

    public async Task<List<AuthTokenEntity>> ListActiveAsync(
        Expression<Func<AuthTokenEntity, bool>> which
    ) => await _authTokenRepo.ListAsync(ActiveAnd(which, UtcNow));

    public async Task<int> DeleteExpiredAsync()
    {
        var now = UtcNow;
        var deletedCount = await _authTokenRepo.ExecuteDeleteAsync(t => t.RefreshExpiresUtc <= now);
        _logger.LogInformation("Deleted {Count} expired auth tokens", deletedCount);
        return deletedCount;
    }

    private static Expression<Func<AuthTokenEntity, bool>> ActiveAnd(
        Expression<Func<AuthTokenEntity, bool>> which,
        DateTime now
    )
    {
        Expression<Func<AuthTokenEntity, bool>> active = t =>
            t.RevokedUtc == null && t.RefreshExpiresUtc > now;
        var body = new ParameterSwap(active.Parameters[0], which.Parameters[0]).Visit(active.Body);
        return Expression.Lambda<Func<AuthTokenEntity, bool>>(
            Expression.AndAlso(which.Body, body),
            which.Parameters
        );
    }

    private sealed class ParameterSwap(ParameterExpression from, ParameterExpression to)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) =>
            node == from ? to : base.VisitParameter(node);
    }
}
