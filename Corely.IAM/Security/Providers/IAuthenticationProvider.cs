using Corely.IAM.Users.Models;

namespace Corely.IAM.Security.Providers;

internal interface IAuthenticationProvider
{
    Task<UserAuthTokenResult> GetUserAuthTokenAsync(UserAuthTokenRequest request);
    Task<UserAuthTokenValidationResult> ValidateUserAuthTokenAsync(string authToken);
    Task<bool> RevokeUserAuthTokenAsync(int userId, string tokenId);
    Task RevokeAllUserAuthTokensAsync(int userId);
}
