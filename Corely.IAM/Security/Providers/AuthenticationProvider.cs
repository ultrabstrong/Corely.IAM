using Corely.Common.Extensions;
using Corely.DataAccess.Interfaces.Repos;
using Corely.IAM.Accounts.Entities;
using Corely.IAM.Accounts.Mappers;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Platform.Providers;
using Corely.IAM.Security.Enums;
using Corely.IAM.Security.Mappers;
using Corely.IAM.Security.Models;
using Corely.IAM.TotpAuths.Entities;
using Corely.IAM.Users.Entities;
using Corely.IAM.Users.Mappers;
using Corely.IAM.Users.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Corely.IAM.Security.Providers;

internal class AuthenticationProvider(
    IRepo<UserEntity> userRepo,
    IReadonlyRepo<AccountEntity> accountRepo,
    IReadonlyRepo<TotpAuthEntity> totpAuthRepo,
    IPlatformAccessProvider platformAccessProvider,
    ISessionTokenProvider sessionTokenProvider,
    ILogger<AuthenticationProvider> logger,
    TimeProvider timeProvider
) : IAuthenticationProvider
{
    private readonly IRepo<UserEntity> _userRepo = userRepo.ThrowIfNull(nameof(userRepo));
    private readonly IReadonlyRepo<AccountEntity> _accountRepo = accountRepo.ThrowIfNull(
        nameof(accountRepo)
    );
    private readonly IReadonlyRepo<TotpAuthEntity> _totpAuthRepo = totpAuthRepo.ThrowIfNull(
        nameof(totpAuthRepo)
    );
    private readonly IPlatformAccessProvider _platformAccessProvider =
        platformAccessProvider.ThrowIfNull(nameof(platformAccessProvider));
    private readonly ISessionTokenProvider _sessionTokenProvider = sessionTokenProvider.ThrowIfNull(
        nameof(sessionTokenProvider)
    );
    private readonly ILogger<AuthenticationProvider> _logger = logger.ThrowIfNull(nameof(logger));
    private readonly TimeProvider _timeProvider = timeProvider.ThrowIfNull(nameof(timeProvider));

    public async Task<UserAuthTokenResult> GetUserAuthTokenAsync(GetUserAuthTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var tokenIssueContext = await GetTokenIssueContextAsync(request.UserId, request.AccountId);
        if (tokenIssueContext.ResultCode.HasValue)
            return UserAuthTokenResult.Failed(tokenIssueContext.ResultCode.Value);

        var tokenId = Guid.CreateVersion7();
        var sessionId = Guid.CreateVersion7();
        var sessionStartedUtc = _timeProvider.GetUtcNow().UtcDateTime;

        if (request.ContinuedTokenId is { } continuedTokenId)
        {
            var continued = await _sessionTokenProvider.ClaimContinuationAsync(
                continuedTokenId,
                request.UserId,
                tokenId
            );
            if (continued == null)
            {
                _logger.LogInformation(
                    "Session of token {TokenId} is no longer active and cannot be continued",
                    continuedTokenId
                );
                return UserAuthTokenResult.Failed(UserAuthTokenResultCode.SessionNotFoundError);
            }

            sessionId = continued.SessionId;
            sessionStartedUtc = continued.SessionStartedUtc;
        }

        var signedInAccountId = tokenIssueContext.Context!.SignedInAccount?.Id;
        await _sessionTokenProvider.RevokeActiveAsync(t =>
            t.UserId == request.UserId
            && t.AccountId == signedInAccountId
            && t.DeviceId == request.DeviceId
        );

        return await IssueAsync(
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
        return await RenewUserAuthTokenAsync(
            await _sessionTokenProvider.BeginRenewalAsync(refreshToken)
        );
    }

    public async Task<RenewUserAuthTokenResult> RenewUserAuthTokenAsync(SessionTokenRenewal renewal)
    {
        if (
            renewal.Code != SessionTokenRenewalCode.Ready
            || renewal.Token?.UserId is not { } userId
        )
            return RenewUserAuthTokenResult.Failed(
                renewal.Code switch
                {
                    SessionTokenRenewalCode.SessionExpired =>
                        RenewUserAuthTokenResultCode.SessionExpiredError,
                    SessionTokenRenewalCode.AlreadyRotated =>
                        RenewUserAuthTokenResultCode.RefreshTokenAlreadyRotatedError,
                    SessionTokenRenewalCode.Reused =>
                        RenewUserAuthTokenResultCode.RefreshTokenReusedError,
                    _ => RenewUserAuthTokenResultCode.TokenValidationFailed,
                }
            );

        var token = renewal.Token;
        var tokenIssueContext = await GetTokenIssueContextAsync(userId, token.AccountId);
        if (tokenIssueContext.ResultCode.HasValue)
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

        if (await _sessionTokenProvider.ClaimRenewalAsync(token) is not { } renewedTokenId)
            return RenewUserAuthTokenResult.Failed(
                RenewUserAuthTokenResultCode.RefreshTokenAlreadyRotatedError
            );

        var renewed = await IssueAsync(
            tokenIssueContext.Context!,
            token.DeviceId,
            renewedTokenId,
            token.SessionId,
            token.SessionStartedUtc
        );
        return new RenewUserAuthTokenResult(
            RenewUserAuthTokenResultCode.Success,
            renewed.Token,
            renewed.TokenId,
            renewed.User,
            renewed.CurrentAccount,
            token.DeviceId,
            renewed.AvailableAccounts,
            renewed.EnteredAsPlatformMember,
            renewed.RefreshToken,
            renewed.RefreshExpiresUtc
        );
    }

    public async Task<UserAuthTokenValidationResult> ValidateUserAuthTokenAsync(string authToken)
    {
        ArgumentNullException.ThrowIfNull(authToken, nameof(authToken));

        var claims = _sessionTokenProvider.ReadClaims(authToken, out var failure);
        if (claims == null)
            return UserAuthTokenValidationResult.Failed(failure);
        if (claims.PrincipalType != TokenPrincipalType.User)
            return UserAuthTokenValidationResult.Failed(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );

        var userEntity = await GetUserWithKeysAndAccountsAsync(claims.SubjectId);
        if (
            userEntity == null
            || !await _sessionTokenProvider.IsActiveAsync(claims)
            || !_sessionTokenProvider.IsSignedBy(
                authToken,
                userEntity.AsymmetricKeys.Versions(KeyUsedFor.Signature)
            )
        )
        {
            _logger.LogInformation(
                "Auth token {TokenId} for user {UserId} is unknown, inactive or not signed by the user",
                claims.TokenId,
                claims.SubjectId
            );
            return UserAuthTokenValidationResult.Failed(
                UserAuthTokenValidationResultCode.TokenValidationFailed
            );
        }

        var signedInAccount = await SignedInAccountAsync(claims.SignedInAccountId, userEntity);
        return new UserAuthTokenValidationResult(
            UserAuthTokenValidationResultCode.Success,
            userEntity.ToModel(),
            signedInAccount,
            claims.DeviceId,
            claims.TokenId,
            WithEnteredAccount(userEntity.AccountModels(), signedInAccount),
            IsEnteredAsPlatformMember(userEntity, signedInAccount)
        );
    }

    public async Task<bool> RevokeUserAuthTokenAsync(RevokeUserAuthTokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        if (!Guid.TryParse(request.TokenId, out var tokenId))
        {
            _logger.LogInformation("Auth token {TokenId} is not a valid GUID", request.TokenId);
            return false;
        }

        var revoked =
            await _sessionTokenProvider.RevokeActiveAsync(t =>
                t.Id == tokenId
                && t.UserId == request.UserId
                && t.AccountId == request.AccountId
                && t.DeviceId == request.DeviceId
            ) > 0;
        _logger.LogInformation(
            "Auth token {TokenId} for user {UserId}, account {AccountId}, device {DeviceId} revoked: {Revoked}",
            request.TokenId,
            request.UserId,
            request.AccountId,
            request.DeviceId,
            revoked
        );
        return revoked;
    }

    public async Task<List<UserSession>> ListUserSessionsAsync(
        Guid userId,
        Guid? currentSessionId
    ) =>
        (await _sessionTokenProvider.ListActiveAsync(t => t.UserId == userId))
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

    public async Task<bool> RevokeUserAuthTokenByIdAsync(Guid userId, Guid tokenId) =>
        await _sessionTokenProvider.RevokeActiveAsync(t => t.Id == tokenId && t.UserId == userId)
        > 0;

    public async Task<bool> RevokeOtherUserAuthTokensAsync(Guid userId, Guid currentTokenId)
    {
        var current = await _sessionTokenProvider.ListActiveAsync(t =>
            t.Id == currentTokenId && t.UserId == userId
        );
        if (current.Count == 0)
            return false;

        await _sessionTokenProvider.RevokeActiveAsync(t =>
            t.UserId == userId && t.Id != currentTokenId
        );
        return true;
    }

    public async Task RevokeAllUserAuthTokensAsync(Guid userId, Guid? exceptTokenId = null)
    {
        var revokedCount = await _sessionTokenProvider.RevokeActiveAsync(t =>
            t.UserId == userId && t.Id != exceptTokenId
        );
        if (revokedCount > 0)
            _logger.LogInformation(
                "Revoked {Count} auth tokens for user {UserId}",
                revokedCount,
                userId
            );
    }

    private async Task<UserEntity?> GetUserWithKeysAndAccountsAsync(Guid userId) =>
        await _userRepo.GetAsync(
            u => u.Id == userId,
            include: q => q.Include(u => u.AsymmetricKeys).Include(u => u.Accounts)
        );

    private async Task<(
        UserAuthTokenResultCode? ResultCode,
        TokenIssueContext? Context
    )> GetTokenIssueContextAsync(Guid userId, Guid? accountId)
    {
        var userEntity = await GetUserWithKeysAndAccountsAsync(userId);
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
            var membership = accounts.FirstOrDefault(a => a.Id == accountId.Value);
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

    private async Task<UserAuthTokenResult> IssueAsync(
        TokenIssueContext tokenIssueContext,
        string deviceId,
        Guid tokenId,
        Guid sessionId,
        DateTime sessionStartedUtc
    )
    {
        var issued = await _sessionTokenProvider.IssueAsync(
            new SessionTokenSubject(
                TokenPrincipalType.User,
                tokenIssueContext.UserEntity.Id,
                tokenIssueContext.SignatureKey,
                deviceId,
                [.. tokenIssueContext.Accounts.Select(a => a.Id)],
                tokenIssueContext.SignedInAccount?.Id
            ),
            tokenId,
            sessionId,
            sessionStartedUtc
        );

        return new UserAuthTokenResult(
            UserAuthTokenResultCode.Success,
            issued.Token,
            issued.TokenId,
            tokenIssueContext.UserEntity.ToModel(),
            tokenIssueContext.SignedInAccount,
            tokenIssueContext.Accounts,
            IsEnteredAsPlatformMember(
                tokenIssueContext.UserEntity,
                tokenIssueContext.SignedInAccount
            ),
            issued.RefreshToken,
            issued.RefreshExpiresUtc
        );
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

    private async Task<Account?> SignedInAccountAsync(Guid? accountId, UserEntity userEntity)
    {
        if (accountId is not { } id)
            return null;

        var account =
            userEntity.Accounts?.FirstOrDefault(a => a.Id == id)?.ToModel()
            ?? await FindPlatformEnterableAccountAsync(userEntity.Id, id);
        if (account == null)
            _logger.LogWarning(
                "Account with Id {AccountId} not found in user's accounts during token validation",
                id
            );
        return account;
    }
}
