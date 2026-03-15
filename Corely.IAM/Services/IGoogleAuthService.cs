using Corely.IAM.GoogleAuths.Models;

namespace Corely.IAM.Services;

public interface IGoogleAuthService
{
    Task<LinkGoogleAuthResult> LinkGoogleAuthAsync(LinkGoogleAuthRequest request);
    Task<UnlinkGoogleAuthResult> UnlinkGoogleAuthAsync();
    Task<AuthMethodsResult> GetAuthMethodsAsync();
}
