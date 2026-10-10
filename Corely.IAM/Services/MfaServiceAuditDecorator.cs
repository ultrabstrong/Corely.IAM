using Corely.Common.Extensions;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;
using Corely.IAM.TotpAuths.Models;

namespace Corely.IAM.Services;

internal class MfaServiceAuditDecorator(IMfaService inner, IAuditProvider auditProvider)
    : IMfaService
{
    private const string SERVICE = nameof(IMfaService);

    private readonly IMfaService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditProvider _auditProvider = auditProvider.ThrowIfNull(
        nameof(auditProvider)
    );

    public Task<EnableTotpResult> EnableTotpAsync() =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Create),
            () => _inner.EnableTotpAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ConfirmTotpResult> ConfirmTotpAsync(ConfirmTotpRequest request) =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Update),
            () => _inner.ConfirmTotpAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<DisableTotpResult> DisableTotpAsync(DisableTotpRequest request) =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Delete),
            () => _inner.DisableTotpAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<RegenerateTotpRecoveryCodesResult> RegenerateTotpRecoveryCodesAsync() =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Update),
            () => _inner.RegenerateTotpRecoveryCodesAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<TotpStatusResult> GetTotpStatusAsync() =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Read),
            () => _inner.GetTotpStatusAsync(),
            r => AuditOutcome.Of(r.ResultCode)
        );

    private static AuditCall OnUser(AuthAction action) =>
        new(SERVICE, action, PermissionConstants.USER_RESOURCE_TYPE) { AccountId = Guid.Empty };
}
