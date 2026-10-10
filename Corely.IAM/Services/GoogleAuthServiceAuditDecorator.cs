using Corely.Common.Extensions;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.GoogleAuths.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.Services;

internal class GoogleAuthServiceAuditDecorator(
    IGoogleAuthService inner,
    IAuditProvider auditProvider
) : IGoogleAuthService
{
    private const string SERVICE = nameof(IGoogleAuthService);

    private readonly IGoogleAuthService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditProvider _auditProvider = auditProvider.ThrowIfNull(
        nameof(auditProvider)
    );

    public Task<LinkGoogleAuthResult> LinkGoogleAuthAsync(LinkGoogleAuthRequest request) =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Create),
            () => _inner.LinkGoogleAuthAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<UnlinkGoogleAuthResult> UnlinkGoogleAuthAsync() =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Delete),
            () => _inner.UnlinkGoogleAuthAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<AuthMethodsResult> GetAuthMethodsAsync() =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Read),
            () => _inner.GetAuthMethodsAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );

    private static AuditCall OnUser(AuthAction action) =>
        new(SERVICE, action, PermissionConstants.USER_RESOURCE_TYPE) { AccountId = Guid.Empty };
}
