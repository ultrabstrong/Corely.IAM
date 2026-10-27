using System.Linq.Expressions;
using Corely.IAM.Security.Entities;
using Corely.IAM.Security.Models;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Security.Providers;

internal interface ISessionTokenProvider
{
    Task<IssuedSessionToken> IssueAsync(
        SessionTokenSubject subject,
        Guid tokenId,
        Guid sessionId,
        DateTime sessionStartedUtc
    );
    SessionTokenClaims? ReadClaims(string authToken, out UserAuthTokenValidationResultCode failure);
    Task<bool> IsActiveAsync(SessionTokenClaims claims);
    bool IsSignedBy(string authToken, IEnumerable<AsymmetricKeyEntity> signatureKeys);
    Task<SessionTokenRenewal> BeginRenewalAsync(string refreshToken);
    Task EndAsync(AuthTokenEntity token);
    Task<Guid?> ClaimRenewalAsync(AuthTokenEntity token);
    Task<AuthTokenEntity?> ClaimContinuationAsync(
        Guid tokenId,
        Guid userId,
        Guid continuingTokenId
    );
    Task<int> RevokeActiveAsync(Expression<Func<AuthTokenEntity, bool>> which);
    Task<List<AuthTokenEntity>> ListActiveAsync(Expression<Func<AuthTokenEntity, bool>> which);
    Task<int> DeleteExpiredAsync();
}
