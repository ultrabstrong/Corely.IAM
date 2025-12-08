using Corely.IAM.Models;

namespace Corely.IAM.Services;

public interface IAuthenticationService
{
    Task<SignInResult> SignInAsync(SignInRequest request);
    Task<bool> ValidateAuthTokenAsync(int userId, string authToken);
    Task<bool> SignOutAsync(int userId, string jti);
    Task SignOutAllAsync(int userId);
}
