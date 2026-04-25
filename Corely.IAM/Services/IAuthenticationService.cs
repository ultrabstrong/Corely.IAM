using Corely.IAM.Models;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Services;

public interface IAuthenticationService
{
    Task<SignInResult> SignInAsync(SignInRequest request);
    Task<SignInResult> SignInWithGoogleAsync(SignInWithGoogleRequest request);
    Task<SignInResult> VerifyMfaAsync(VerifyMfaRequest request);
    Task<SignInResult> SwitchAccountAsync(SwitchAccountRequest request);
    Task<bool> SignOutAsync(SignOutRequest request);
    Task SignOutAllAsync();
    Task<UserAuthTokenValidationResultCode> AuthenticateWithTokenAsync(string authToken);
    void AuthenticateAsSystem(string deviceId);
}
