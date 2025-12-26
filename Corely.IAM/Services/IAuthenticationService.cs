using Corely.IAM.Models;

namespace Corely.IAM.Services;

public interface IAuthenticationService
{
    Task<SignInResult> SignInAsync(SignInRequest request);
    Task<SignInResult> SwitchAccountAsync(SwitchAccountRequest request);
    Task<bool> SignOutAsync(int userId, string tokenId);
    Task SignOutAllAsync(int userId);
}
