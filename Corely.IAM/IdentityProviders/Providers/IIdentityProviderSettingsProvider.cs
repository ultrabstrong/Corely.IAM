using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;

namespace Corely.IAM.IdentityProviders.Providers;

internal interface IIdentityProviderSettingsProvider
{
    Task<IReadOnlyList<IdentityProviderSettings>> GetAllAsync();
    Task<ResolvedIdentityProvider?> GetUsableAsync(IdentityProviderType type);
}
