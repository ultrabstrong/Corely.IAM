using Corely.IAM.FederatedIdentities.Models;
using Corely.IAM.IdentityProviders.Enums;

namespace Corely.IAM.Services;

public interface IFederatedIdentityService
{
    Task<AuthMethodsResult> GetAuthMethodsAsync();
    Task<UnlinkFederatedIdentityResult> UnlinkFederatedIdentityAsync(IdentityProviderType type);
}
