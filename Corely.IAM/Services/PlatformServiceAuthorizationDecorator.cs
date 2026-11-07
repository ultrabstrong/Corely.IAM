using Corely.Common.Extensions;
using Corely.IAM.Audits.Providers;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.IdentityProviders.Models;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Enums;
using Corely.IAM.Users.Providers;

namespace Corely.IAM.Services;

internal class PlatformServiceAuthorizationDecorator(
    IPlatformService inner,
    IAuditAccessProvider auditAccessProvider,
    IUserContextProvider userContextProvider
) : IPlatformService
{
    private const string UNAUTHORIZED = "Unauthorized";

    private readonly IPlatformService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditAccessProvider _auditAccessProvider = auditAccessProvider.ThrowIfNull(
        nameof(auditAccessProvider)
    );
    private readonly IUserContextProvider _userContextProvider = userContextProvider.ThrowIfNull(
        nameof(userContextProvider)
    );

    public Task<BootstrapPlatformResult> BootstrapPlatformAsync(BootstrapPlatformRequest request) =>
        _userContextProvider.GetUserContext() is null or { IsSystemContext: true }
            ? _inner.BootstrapPlatformAsync(request)
            : Task.FromResult(
                new BootstrapPlatformResult(
                    BootstrapPlatformResultCode.UnauthorizedError,
                    "The platform is bootstrapped from the migration CLI or system context, never by a signed in caller",
                    null
                )
            );

    public Task<int> CompletePlatformOwnerPermissionsAsync() =>
        _inner.CompletePlatformOwnerPermissionsAsync();

    public async Task<GetSecuritySettingsResult> GetSecuritySettingsAsync() =>
        await _auditAccessProvider.HoldsInPlatformAccountAsync(
            AuthAction.Read,
            SecurityPolicy.RESOURCE_TYPE
        )
            ? await _inner.GetSecuritySettingsAsync()
            : new GetSecuritySettingsResult(
                RetrieveResultCode.UnauthorizedError,
                UNAUTHORIZED,
                null
            );

    public async Task<ModifyResult> UpdateSecuritySettingsAsync(SecurityPolicy settings) =>
        await _auditAccessProvider.HoldsInPlatformAccountAsync(
            AuthAction.Update,
            SecurityPolicy.RESOURCE_TYPE
        )
            ? await _inner.UpdateSecuritySettingsAsync(settings)
            : new ModifyResult(ModifyResultCode.UnauthorizedError, UNAUTHORIZED);

    public async Task<GetIdentityProvidersResult> GetIdentityProvidersAsync() =>
        await HoldsSecuritySettingsAsync(AuthAction.Read)
            ? await _inner.GetIdentityProvidersAsync()
            : new GetIdentityProvidersResult(
                RetrieveResultCode.UnauthorizedError,
                UNAUTHORIZED,
                null
            );

    public async Task<ModifyResult> UpdateIdentityProviderAsync(
        UpdateIdentityProviderRequest request
    ) =>
        await HoldsSecuritySettingsAsync(AuthAction.Update)
            ? await _inner.UpdateIdentityProviderAsync(request)
            : new ModifyResult(ModifyResultCode.UnauthorizedError, UNAUTHORIZED);

    public async Task<CountSoleSignInUsersResult> CountSoleSignInUsersAsync(
        IdentityProviderType type
    ) =>
        await HoldsSecuritySettingsAsync(AuthAction.Read)
            ? await _inner.CountSoleSignInUsersAsync(type)
            : new CountSoleSignInUsersResult(RetrieveResultCode.UnauthorizedError, UNAUTHORIZED, 0);

    private Task<bool> HoldsSecuritySettingsAsync(AuthAction action) =>
        _auditAccessProvider.HoldsInPlatformAccountAsync(action, SecurityPolicy.RESOURCE_TYPE);
}
