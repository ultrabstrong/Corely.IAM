using Corely.Common.Extensions;
using Corely.IAM.Audits.Constants;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Platform.Models;
using Corely.IAM.Security.Constants;

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
            new AuditCall(SERVICE, AuthAction.Create, PermissionConstants.ACCOUNT_RESOURCE_TYPE)
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
        _auditProvider.RecordAsync(
            new AuditCall(SERVICE, AuthAction.Update, PermissionConstants.PERMISSION_RESOURCE_TYPE),
            () => _inner.CompletePlatformOwnerPermissionsAsync(),
            _ => new AuditOutcome(AuditConstants.SUCCESS_RESULT_CODE)
        );
}
