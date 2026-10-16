using Corely.Common.Extensions;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Enums;

namespace Corely.IAM.Services;

internal class PlatformServiceAuthorizationDecorator(
    IPlatformService inner,
    IAuditAccessProvider auditAccessProvider
) : IPlatformService
{
    private const string UNAUTHORIZED = "Unauthorized";

    private readonly IPlatformService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditAccessProvider _auditAccessProvider = auditAccessProvider.ThrowIfNull(
        nameof(auditAccessProvider)
    );

    public Task<BootstrapPlatformResult> BootstrapPlatformAsync(BootstrapPlatformRequest request) =>
        _inner.BootstrapPlatformAsync(request);

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
}
