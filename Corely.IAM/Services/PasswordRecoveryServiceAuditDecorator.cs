using Corely.Common.Extensions;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.PasswordRecoveries.Models;
using Corely.IAM.Permissions.Constants;
using Corely.IAM.Security.Constants;

namespace Corely.IAM.Services;

internal class PasswordRecoveryServiceAuditDecorator(
    IPasswordRecoveryService inner,
    IAuditProvider auditProvider
) : IPasswordRecoveryService
{
    private const string SERVICE = nameof(IPasswordRecoveryService);

    private readonly IPasswordRecoveryService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditProvider _auditProvider = auditProvider.ThrowIfNull(
        nameof(auditProvider)
    );

    public Task<RequestPasswordRecoveryResult> RequestPasswordRecoveryAsync(
        RequestPasswordRecoveryRequest request
    ) =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Execute),
            () => _inner.RequestPasswordRecoveryAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ValidatePasswordRecoveryTokenResult> ValidatePasswordRecoveryTokenAsync(
        ValidatePasswordRecoveryTokenRequest request
    ) =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Read),
            () => _inner.ValidatePasswordRecoveryTokenAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<ResetPasswordWithRecoveryResult> ResetPasswordWithRecoveryAsync(
        ResetPasswordWithRecoveryRequest request
    ) =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Update),
            () => _inner.ResetPasswordWithRecoveryAsync(request),
            r => AuditOutcome.Of(r.ResultCode)
        );

    private static AuditCall OnUser(AuthAction action) =>
        new(SERVICE, action, PermissionConstants.USER_RESOURCE_TYPE) { AccountId = Guid.Empty };
}
