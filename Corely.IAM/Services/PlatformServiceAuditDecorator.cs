using Corely.Common.Extensions;
using Corely.IAM.Accounts.Models;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Models;
using Corely.IAM.Permissions.Models;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Enums;

namespace Corely.IAM.Services;

internal class PlatformServiceAuditDecorator(IPlatformService inner, IAuditProvider auditProvider)
    : IPlatformService
{
    private const string SERVICE = nameof(IPlatformService);

    private readonly IPlatformService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditProvider _auditProvider = auditProvider.ThrowIfNull(
        nameof(auditProvider)
    );

    public Task<BootstrapPlatformResult> BootstrapPlatformAsync(BootstrapPlatformRequest request) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Create, Account.RESOURCE_TYPE)
            {
                AccountId = Guid.Empty,
            },
            () => _inner.BootstrapPlatformAsync(request),
            r =>
                AuditOutcome.Of(r.ResultCode, r.Credentials?.AccountId) with
                {
                    AccountId = r.Credentials?.AccountId,
                }
        );

    public Task<int> CompletePlatformOwnerPermissionsAsync() =>
        _inner.CompletePlatformOwnerPermissionsAsync();

    public Task<GetSecuritySettingsResult> GetSecuritySettingsAsync() =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Read, PlatformSettings.RESOURCE_TYPE),
            () => _inner.GetSecuritySettingsAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ModifyResult> UpdateSecuritySettingsAsync(SecurityPolicy settings) =>
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PlatformSettings.RESOURCE_TYPE),
            () => _inner.UpdateSecuritySettingsAsync(settings),
            r => AuditOutcome.Of(r.ResultCode)
        );
}
