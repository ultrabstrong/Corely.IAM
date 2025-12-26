using Corely.IAM.Users.Models;

namespace Corely.IAM.Security.Providers;

internal interface IAuthenticationProvider
{
    Task<UserAuthTokenResult> GetUserAuthTokenAsync(GetUserAuthTokenRequest request);
    Task<UserAuthTokenValidationResult> ValidateUserAuthTokenAsync(string authToken);
    Task<bool> RevokeUserAuthTokenAsync(RevokeUserAuthTokenRequest request);
    Task RevokeAllUserAuthTokensAsync(int userId);
}
