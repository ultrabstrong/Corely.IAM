using Corely.Common.Extensions;
using Corely.IAM.FederatedIdentities.Models;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.Security.Providers;

namespace Corely.IAM.Services;

internal class FederatedIdentityServiceAuthorizationDecorator(
    IFederatedIdentityService inner,
    IAuthorizationProvider authorizationProvider
) : IFederatedIdentityService
{
    private const string UNAUTHORIZED = "Unauthorized";

    private readonly IFederatedIdentityService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuthorizationProvider _authorizationProvider =
        authorizationProvider.ThrowIfNull(nameof(authorizationProvider));

    public async Task<AuthMethodsResult> GetAuthMethodsAsync() =>
        _authorizationProvider.IsNonSystemUserContext()
            ? await _inner.GetAuthMethodsAsync()
            : new AuthMethodsResult(
                AuthMethodsResultCode.UnauthorizedError,
                UNAUTHORIZED,
                false,
                []
            );

    public async Task<UnlinkFederatedIdentityResult> UnlinkFederatedIdentityAsync(
        IdentityProviderType type
    ) =>
        _authorizationProvider.IsNonSystemUserContext()
            ? await _inner.UnlinkFederatedIdentityAsync(type)
            : new UnlinkFederatedIdentityResult(
                UnlinkFederatedIdentityResultCode.UnauthorizedError,
                UNAUTHORIZED
            );
}
