using Corely.IAM.Users.Models;

namespace Corely.IAM.Security.Providers;

internal interface IAuthenticationProvider
{
    Task<UserAuthTokenResult> GetUserAuthTokenAsync(GetUserAuthTokenRequest request);
    Task<UserAuthTokenValidationResult> ValidateUserAuthTokenAsync(string authToken);
    Task<bool> RevokeUserAuthTokenAsync(RevokeUserAuthTokenRequest request);
    Task<List<UserSession>> ListUserSessionsAsync(Guid userId, Guid? currentSessionId);
    Task<bool> RevokeUserAuthTokenByIdAsync(Guid userId, Guid tokenId);
    Task<bool> RevokeOtherUserAuthTokensAsync(Guid userId, Guid currentTokenId);
    Task RevokeAllUserAuthTokensAsync(Guid userId);
}
