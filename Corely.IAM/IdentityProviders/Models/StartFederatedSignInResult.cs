namespace Corely.IAM.IdentityProviders.Models;

public enum StartFederatedSignInResultCode
{
    Success,
    ProviderNotAvailableError,
    UnauthorizedError,
    ProviderUnreachableError,
}

public record StartFederatedSignInResult(
    StartFederatedSignInResultCode ResultCode,
    string Message,
    string? AuthorizationUrl,
    FederatedSignInCorrelation? Correlation
);
