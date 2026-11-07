using Corely.Common.Extensions;
using Corely.IAM.Audits.Models;
using Corely.IAM.Audits.Providers;
using Corely.IAM.FederatedIdentities.Models;
using Corely.IAM.IdentityProviders.Enums;
using Corely.IAM.Security.Enums;
using Corely.IAM.Users.Models;

namespace Corely.IAM.Services;

internal class FederatedIdentityServiceAuditDecorator(
    IFederatedIdentityService inner,
    IAuditProvider auditProvider
) : IFederatedIdentityService
{
    private const string SERVICE = nameof(IFederatedIdentityService);

    private readonly IFederatedIdentityService _inner = inner.ThrowIfNull(nameof(inner));
    private readonly IAuditProvider _auditProvider = auditProvider.ThrowIfNull(
        nameof(auditProvider)
    );

    public Task<AuthMethodsResult> GetAuthMethodsAsync() =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Read),
            _inner.GetAuthMethodsAsync,
            r => AuditOutcome.Of(r.ResultCode)
        );

    public Task<UnlinkFederatedIdentityResult> UnlinkFederatedIdentityAsync(
        IdentityProviderType type
    ) =>
        _auditProvider.RecordAsync(
            OnUser(AuthAction.Delete),
            () => _inner.UnlinkFederatedIdentityAsync(type),
            r => AuditOutcome.Of(r.ResultCode)
        );

    private static AuditCall OnUser(AuthAction action) =>
        new(SERVICE, action, User.RESOURCE_TYPE) { AccountId = Guid.Empty };
}
