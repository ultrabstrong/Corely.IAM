using Corely.IAM.IdentityProviders.Models;

namespace Corely.IAM.Services;

public interface IFederatedSignInService
{
    Task<IReadOnlyList<IdentityProviderOption>> ListIdentityProvidersAsync();
    Task<StartFederatedSignInResult> StartFederatedSignInAsync(StartFederatedSignInRequest request);
    Task<CompleteFederatedSignInResult> CompleteFederatedSignInAsync(
        CompleteFederatedSignInRequest request
    );
}
