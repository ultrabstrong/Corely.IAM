using Corely.IAM.Models;

namespace Corely.IAM.IdentityProviders.Models;

public record GetIdentityProvidersResult(
    RetrieveResultCode ResultCode,
    string Message,
    IReadOnlyList<IdentityProviderSettings>? Providers
);
