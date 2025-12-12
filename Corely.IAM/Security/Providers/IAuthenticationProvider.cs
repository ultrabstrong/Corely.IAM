using Corely.IAM.Users.Models;

namespace Corely.IAM.Security.Providers;

public interface IAuthenticationProvider
{
    Task<UserAuthTokenResult> GetUserAuthTokenAsync(UserAuthTokenRequest request);
    Task<UserAuthTokenValidationResult> ValidateUserAuthTokenAsync(string authToken);
    Task<bool> RevokeUserAuthTokenAsync(int userId, string jti);
    Task RevokeAllUserAuthTokensAsync(int userId);
}
