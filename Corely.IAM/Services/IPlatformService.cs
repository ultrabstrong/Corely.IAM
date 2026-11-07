using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;

namespace Corely.IAM.Services;

public interface IPlatformService
{
    Task<BootstrapPlatformResult> BootstrapPlatformAsync(BootstrapPlatformRequest request);
    Task<int> CompletePlatformOwnerPermissionsAsync();
    Task<GetSecuritySettingsResult> GetSecuritySettingsAsync();
    Task<ModifyResult> UpdateSecuritySettingsAsync(SecurityPolicy settings);
    Task<GetIdentityProvidersResult> GetIdentityProvidersAsync();
    Task<ModifyResult> UpdateIdentityProviderAsync(UpdateIdentityProviderRequest request);
    Task<CountSoleSignInUsersResult> CountSoleSignInUsersAsync(IdentityProviderType type);
}
