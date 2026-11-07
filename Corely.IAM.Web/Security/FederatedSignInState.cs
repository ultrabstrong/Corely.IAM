using Corely.IAM.IdentityProviders.Models;

namespace Corely.IAM.Web.Security;

internal sealed record FederatedSignInState(
    FederatedSignInCorrelation Correlation,
    string? ReturnUrl
);
